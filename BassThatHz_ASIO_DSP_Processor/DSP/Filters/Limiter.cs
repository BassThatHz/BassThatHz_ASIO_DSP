#nullable enable

namespace BassThatHz_ASIO_DSP_Processor;

#region Usings
using NAudio.Utils;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
#endregion

/// <summary>
///  BassThatHz ASIO DSP Processor Engine
///  Copyright (c) 2026 BassThatHz
/// 
/// Permission is hereby granted to use this software 
/// and associated documentation files (the "Software"), 
/// for educational purposess, scientific purposess or private purposess
/// or as part of an open-source community project, 
/// (and NOT for commerical use or resale in substaintial part or whole without prior authorization)
/// and all copies of the Software subject to the following conditions:
/// 
/// The copyright notice and this permission notice shall be included in all
/// copies or substantial portions of the Software.
// 
/// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
/// IMPLIED, INCLUDING BUT NOT LIMITED TO FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
/// IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
/// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
/// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
/// SOFTWARE. ENFORCEABLE PORTIONS SHALL REMAIN IF NOT FOUND CONTRARY UNDER LAW.
/// </summary>
[Serializable]
public class Limiter : IFilter
{
    #region Variables
    public double Threshold = 0.1000000000000000; //-20db (legacy, used for reference)
    public double MaxValue = 0.98855309465693886; // -0.1db (default, brickwall ceiling)
    public bool PeakHoldReleaseEnabled = true;
    public double PeakHoldRelease = 5;
    public bool PeakHoldAttackEnabled = true;
    public double PeakHoldAttack = 1;

        [IgnoreDataMember]
    public double CompressionApplied = 0;
    [IgnoreDataMember]
    public double PeakValue = 0;
    [IgnoreDataMember]
    public bool IsBrickwall = false;

    // Brickwall hold/release state machine (see Transform): once engaged, the brickwall gain may not
    // release for BrickwallHoldSeconds after its LAST engage (every new hit re-arms it), then ramps
    // down to unity over BrickwallReleaseSeconds. Timing is in real seconds from elapsed samples,
    // so it is sample-rate independent.
    private const double BrickwallHoldSeconds = 10.0;
    private const double BrickwallReleaseSeconds = 10.0;

    // Full soft-knee gain reduction in dB, reached exactly at the Limit. Holding (not extrapolating)
    // past the limit is what makes the brickwall relax revert to exactly the compression that was in
    // place when it engaged. At the crossing this also keeps output level continuous.
    private const double KneeDepthDb = 8.0;

    /// <summary>
    /// Peak-hold fall-back rate in dB per second, matching the metering convention used by
    /// <see cref="GUI.Controls.BTH_VolumeLevel_MonitorControl"/> (whose ruler spans MinDb..MaxDb and
    /// expects a held peak to slide back down into the track instead of staying pinned). The old
    /// fixed <c>exp(-1/300000)</c> factor was block-count based, not time based, so one transient
    /// pinned the reported peak for hours.
    /// </summary>
    private const double PeakFallDbPerSecond = 30.0;

    /// <summary>Same floor the monitor meter uses as its bottom of scale.</summary>
    private const double PeakFloorDb = -100.0;

    [IgnoreDataMember]
    private double BrickwallGain = 1.0;        // gain currently held by the brickwall state machine (<= 1)
    [IgnoreDataMember]
    private double HoldRemainingSeconds = 0.0; // mandatory no-release time left after last engage

    // True linear release bookkeeping. The ramp is expressed as an absolute gain SLOPE (gain units
    // per second) measured from the deepest gain seen in the current episode, so the ramp reaches
    // unity exactly BrickwallReleaseSeconds after the last engage regardless of how deep it went.
    [IgnoreDataMember]
    private double ReleaseElapsedSeconds = 0.0;
    [IgnoreDataMember]
    private double ReleaseStartGain = 1.0;
    protected double AttackCoeff = 1;
    protected double ReleaseCoeff = 1;
    protected double Gain_Linear = 1;
    protected double SampleRate = 1;
    #endregion

    /// <summary>
    /// The largest double strictly below 1.0 (that is, 1 - 2^-53): the "just below full scale"
    /// output ceiling both output clamps in <see cref="Transform"/> were written to express.
    /// </summary>
    private static readonly double OutputCeiling = Math.BitDecrement(1.0);

    #region Public Functions
    public void CalculateCoeffs(double sampleRate)
    {
        this.SampleRate = sampleRate;
        this.AttackCoeff = Math.Exp(-1.0 / (0.001 * this.PeakHoldAttack * 0.5 * sampleRate));
        this.ReleaseCoeff = Math.Exp(-1.0 / (0.001 * this.PeakHoldRelease * sampleRate));
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public double[] Transform(double[] input, DSP_Stream currentStream)
    {
        // Range of input is -1 to +1
        int len = input.Length;
        if (len == 0)
            return input;

        // Cache fields to locals for performance
        double limit = this.MaxValue;
        double threshold = this.Threshold;
        double attackCoeff = this.AttackCoeff;
        double releaseCoeff = this.ReleaseCoeff;
        bool peakHoldAttackEnabled = this.PeakHoldAttackEnabled;
        bool peakHoldReleaseEnabled = this.PeakHoldReleaseEnabled;

        // Calculate the Peak Amplitude
        double currentPeak = 0.0;
        for (int i = 0; i < len; i++)
        {
            double a = input[i];
            double abs = a < 0 ? -a : a;
            if (abs > currentPeak) currentPeak = abs;
        }

        // Update peak-hold: attack snaps up to a new maximum instantly, otherwise the held value
        // falls back at a fixed dB/s rate derived from elapsed TIME (not block count), so it is
        // sample-rate and buffer-size independent and stops at the same floor the monitor meter uses.
        double peakValueLocal = this.PeakValue;
        if (currentPeak > peakValueLocal)
        {
            peakValueLocal = currentPeak;
        }
        else if (peakValueLocal > 0.0)
        {
            double fallDb = PeakFallDbPerSecond * (this.SampleRate > 0.0 ? len / this.SampleRate : 0.0);
            double heldDb = Decibels.LinearToDecibels(peakValueLocal) - fallDb;
            peakValueLocal = heldDb <= PeakFloorDb
                ? 0.0
                : Decibels.DecibelsToLinear(heldDb);
        }

        // --- Stage 1: brickwall limiter section: hard ceiling plus hold/release state machine. --
        // Engage: any sample above MaxValue drives the gain straight down to the exact ceiling -
        // ramp-up is immediate and can deepen further on every subsequent block. Once engaged, the
        // brickwall may not release for BrickwallHoldSeconds after its LAST engage (every new hit
        // re-arms it), then it ramps down over BrickwallReleaseSeconds. On highly dynamic input
        // this kills the pop: hits keep the reduction pinned instead of dropping in and out block
        // to block, and when they stop it eases back to unity slowly rather than snapping back.
        bool engagedThisBlock = currentPeak > limit;
        double brickwallGain = this.BrickwallGain;
        if (engagedThisBlock)
        {
            double requiredGain = limit / currentPeak;
            if (requiredGain < brickwallGain)
                brickwallGain = requiredGain; // ramp up: immediate, even stronger compression
            this.HoldRemainingSeconds = BrickwallHoldSeconds;
            // Re-arm the release clock on every hit so the ramp always starts from the deepest
            // gain of the episode and always takes the full BrickwallReleaseSeconds.
            this.ReleaseElapsedSeconds = 0.0;
            this.ReleaseStartGain = brickwallGain;
        }

        double blockSeconds = this.SampleRate > 0.0 ? len / this.SampleRate : 0.0;
        bool inBrickwallMode = false;
        if (this.HoldRemainingSeconds > 0.0)
        {
            // Mandatory hold: the reduction may deepen but never release.
            inBrickwallMode = true;
            this.HoldRemainingSeconds -= blockSeconds;
            if (this.HoldRemainingSeconds < 0.0)
                this.HoldRemainingSeconds = 0.0;
        }
        else if (brickwallGain < 1.0)
        {
            // TRUE LINEAR release. The previous form, gain += (1 - gain) * step, is exponential
            // smoothing: each block closes only a fixed FRACTION of the remaining distance, so the
            // tail asymptotically creeps toward unity and never finishes (measured ~137 s to reach
            // 0.999999 instead of the intended 10 s). Interpolating against ELAPSED TIME instead
            // lands exactly on unity after BrickwallReleaseSeconds.
            inBrickwallMode = true;
            this.ReleaseElapsedSeconds += blockSeconds;

            double fraction = this.ReleaseElapsedSeconds / BrickwallReleaseSeconds;
            if (fraction >= 1.0)
            {
                brickwallGain = 1.0;
                this.ReleaseElapsedSeconds = 0.0;
                this.ReleaseStartGain = 1.0;
            }
            else
            {
                brickwallGain = this.ReleaseStartGain + (1.0 - this.ReleaseStartGain) * fraction;
            }
        }

        this.BrickwallGain = brickwallGain;

        // --- Stage 2: soft-knee compression between the Threshold and the Limit --------------
        // Variable compression ratio based on Threshold-Limit span. When they're close (e.g.
        // -20 dB to -10 dB = 10 dB span) the ratio is high and compression is aggressive. When
        // they're far apart (e.g. -384 dB to -0.1 dB) the ratio starts low and only ramps up
        // exponentially as the signal approaches the Limit.
        double targetGain = 1.0;
        if (!inBrickwallMode && currentPeak > threshold && limit > threshold)
        {
            double peakDb = Decibels.LinearToDecibels(currentPeak);
            double thresholdDb = Decibels.LinearToDecibels(threshold);
            double limitDb = Decibels.LinearToDecibels(limit);
            double span = limitDb - thresholdDb;

            // How far the peak is into the span (0 at threshold, 1 at limit).
            double proximityToMax = span > 0.0 ? (peakDb - thresholdDb) / span : 1.0;
            if (proximityToMax > 1.0) proximityToMax = 1.0;
            if (proximityToMax < 0.0) proximityToMax = 0.0;

            // Variable ratio: inverse of the span in dB. A 10 dB span -> ratio ~4:1, a 384 dB
            // span -> ratio ~1.00006:1 (essentially unity until proximity kicks in). This gives
            // the high compression you want when T and L are close, and gentle compression that
            // ramps up exponentially near the limit when they're far apart.
            double baseRatio = 1.0 + 1000.0 / Math.Max(span, 1.0);
            if (baseRatio > 100.0) baseRatio = 100.0;  // cap at 100:1

            // Exponential ramp: ratio stays low near the threshold, then grows exponentially
            // as proximity approaches 1 (the Limit). At proximity=1 the reduction is exactly
            // KneeDepthDb (8 dB), keeping output level continuous with the brickwall.
            double effectiveRatio = 1.0 + (baseRatio - 1.0) * (proximityToMax * proximityToMax);
            double compressionDb = KneeDepthDb * (proximityToMax * proximityToMax);

            targetGain = Decibels.DecibelsToLinear(-compressionDb);
        }

        // Attack/release smoothing toward the target: compression deepening uses the attack
        // coefficient, releasing uses the release one. While the brickwall owns the gain this
        // envelope is left untouched, so when the brickwall finally relaxes the soft-knee resumes
        // from exactly where it left off - no corrective jump between modes.
        double gainLinearLocal = this.Gain_Linear;
        if (!inBrickwallMode)
        {
            if (targetGain < gainLinearLocal)
            {
                if (peakHoldAttackEnabled)
                    gainLinearLocal = attackCoeff * (gainLinearLocal - targetGain) + targetGain;
            }
            else
            {
                if (peakHoldReleaseEnabled)
                    gainLinearLocal = releaseCoeff * (gainLinearLocal - targetGain) + targetGain;
            }
        }

        this.Gain_Linear = gainLinearLocal;

        // CompressionApplied reports the total dB reduction (negative value).
        // Brickwall never reduces compression below what soft-knee was doing,
        // ensuring red mode always has >= compression than blue mode.
        double totalGain = inBrickwallMode ? Math.Min(brickwallGain, gainLinearLocal) : gainLinearLocal;

        this.CompressionApplied = totalGain;
        this.IsBrickwall = inBrickwallMode;

        if (totalGain < 1.0)
        {
            // CLAMP FIX: symmetric clamp against +/-OutputCeiling.
            for (int i = 0; i < len; i++)
            {
                double v = input[i] * totalGain;
                input[i] = Math.Clamp(v, -OutputCeiling, OutputCeiling);
            }
        }

        this.PeakValue = peakValueLocal;
        return input;
    }

    public void ResetSampleRate(int sampleRate)
    {
        this.CalculateCoeffs(sampleRate);
    }

    public void ApplySettings()
    {
        this.PeakValue = 0;
        // Reset brickwall state: a stale held gain would step the output on re-apply.
        this.BrickwallGain = 1.0;
        this.HoldRemainingSeconds = 0.0;
        this.ReleaseElapsedSeconds = 0.0;
        this.ReleaseStartGain = 1.0;
        // Reset soft-knee state too: a stale reduction would fade out over ~100-150 ms as an audible dip.
        this.Gain_Linear = 1;
    }
    #endregion

    #region IFilter Interface
    protected bool _FilterEnabled;
    public bool FilterEnabled
    {
        get
        {
            return this._FilterEnabled;
        }
        set
        {
            this.PeakValue = 0;
            // Reset brickwall state: a stale held gain would step the output on re-enable.
            this.BrickwallGain = 1.0;
            this.HoldRemainingSeconds = 0.0;
            this.ReleaseElapsedSeconds = 0.0;
            this.ReleaseStartGain = 1.0;
            // Reset soft-knee state too: a stale reduction would fade out over ~100-150 ms as an audible dip.
            this.Gain_Linear = 1;
            this._FilterEnabled = value;
        }
    }

    public IFilter GetFilter => this;
    public FilterTypes FilterType { get; } = FilterTypes.Limiter;
    public FilterProcessingTypes FilterProcessingType { get; } = FilterProcessingTypes.WholeBlock;
    
    public IFilter DeepClone()
    {
        return CommonFunctions.DeepClone(this);
    }
    #endregion
}