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
    public double Threshold = 0.1000000000000000; //-20db
    public double MaxValue = 0.98855309465693886; //-0.1db
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

    protected double AttackCoeff = 1;
    protected double ReleaseCoeff = 1;
    protected double Gain_Linear = 1;
    protected double SampleRate = 1;

    /// <summary>
    /// The largest double strictly below 1.0 (that is, 1 - 2^-53): the "just below full scale"
    /// output ceiling both output clamps in <see cref="Transform"/> were written to express.
    /// </summary>
    /// <remarks>
    /// Both clamps originally read <c>double limit = 1.0 - double.Epsilon;</c>, which does NOT
    /// express that. <c>double.Epsilon</c> is the smallest positive SUBNORMAL (about 4.9e-324),
    /// which is far below the ULP of 1.0 (about 2.2e-16), so <c>1.0 - double.Epsilon</c> rounds
    /// straight back to exactly 1.0 and the intended sub-unity ceiling never existed - the limiter
    /// was free to emit a sample of exactly full scale. <see cref="Math.BitDecrement(double)"/> of
    /// 1.0 is the value that was meant, and it keeps the output strictly inside full scale so a
    /// downstream converter cannot wrap at 0 dBFS.
    /// </remarks>
    private static readonly double OutputCeiling = Math.BitDecrement(1.0);
    #endregion

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
        double maxValue = this.MaxValue;
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

        double peakValueLocal = this.PeakValue;
        if (currentPeak > peakValueLocal)
            peakValueLocal = currentPeak;

        double gainReductionLinear = 1.0;
        bool applySmoothing = true;
        bool isBrickwall = false;

        // Near-brickwall: aggressive compression when very close to max
        if (currentPeak > maxValue || threshold == maxValue && currentPeak > maxValue - 0.8912509381 && currentPeak < maxValue)
        {
            isBrickwall = true;
            gainReductionLinear = maxValue / currentPeak;
            double closenessToMax = 1.0 - currentPeak;
            gainReductionLinear *= 1.0 - Math.Log(1.0 - closenessToMax + double.Epsilon);
            if (gainReductionLinear < 0) gainReductionLinear = 0;
            else if (gainReductionLinear > 1) gainReductionLinear = 1;
        }

        // Brickwall limiter section
        if (peakValueLocal > maxValue)
        {
            applySmoothing = false;
            double excessDb = Decibels.LinearToDecibels(maxValue) - Decibels.LinearToDecibels(peakValueLocal);
            double gainReductionLinear2 = Decibels.DecibelsToLinear(excessDb);
            this.CompressionApplied = gainReductionLinear2;
            this.Gain_Linear = gainReductionLinear2;

            // Apply a dynamic decay of the forced peak-hold
            double decayFactor = 300000.0;
            double closeness = Math.Abs(Decibels.LinearToDecibels(peakValueLocal) - Decibels.LinearToDecibels(currentPeak));
            if (closeness > 35.0) decayFactor = 100.0;
            peakValueLocal *= Math.Exp(-1.0 / decayFactor);

            gainReductionLinear = gainReductionLinear2;

            if (gainReductionLinear < 1.0)
            {
                // CLAMP FIX (brickwall path) - the previous lines were
                //     double limit = 1.0 - double.Epsilon;
                //     input[i] = v < limit ? v : limit;
                // which carried TWO defects:
                //  (a) the ceiling did not exist. 1.0 - double.Epsilon is exactly 1.0, because
                //      double.Epsilon is the smallest positive SUBNORMAL (~4.9e-324) and is far
                //      below the ULP of 1.0 (~2.2e-16), so the subtraction rounds straight back to
                //      1.0. The "just below full scale" guard was really a full-scale guard, and a
                //      sample of exactly 1.0 could leave the limiter. See OutputCeiling.
                //  (b) the clamp was ONE-SIDED. `v < limit ? v : limit` is Math.Min, which bounds
                //      only from above, so the NEGATIVE side had no bound at all.
                // This is a hard output ceiling, not a soft-knee target - the gain reduction itself
                // is gainReductionLinear2, applied by the multiply above - so the correct form is a
                // symmetric Math.Clamp against +/-OutputCeiling. Anything already inside the
                // ceiling is returned untouched, bit for bit, so normal audio is not perturbed.
                for (int i = 0; i < len; i++)
                {
                    double v = input[i] * gainReductionLinear2;
                    input[i] = Math.Clamp(v, -OutputCeiling, OutputCeiling);
                }
            }
        }

        if (applySmoothing)
        {
            // Dynamic compression threshold
            if (threshold < maxValue && currentPeak > threshold && currentPeak < maxValue)
            {
                double proximityToMax = (currentPeak - threshold) / (maxValue - threshold);
                if (proximityToMax < 0) proximityToMax = 0;
                if (proximityToMax > 1) proximityToMax = 1;
                gainReductionLinear = 1.0 - Math.Log(proximityToMax + 1.0) / Math.Log(2.0);
                if (gainReductionLinear < 0) gainReductionLinear = 0;
                else if (gainReductionLinear > 1) gainReductionLinear = 1;
            }

            double gainLinearLocal = this.Gain_Linear;
            if (gainReductionLinear < gainLinearLocal)
            {
                if (peakHoldAttackEnabled)
                    gainLinearLocal = attackCoeff * (gainLinearLocal - gainReductionLinear) + gainReductionLinear;
            }
            else
            {
                if (peakHoldReleaseEnabled)
                    gainLinearLocal = releaseCoeff * (gainLinearLocal - gainReductionLinear) + gainReductionLinear;
            }

            this.CompressionApplied = gainLinearLocal;

            if (gainLinearLocal < 1.0)
            {
                // CLAMP FIX (smoothed / soft-knee path) - identical defect to the brickwall clamp
                // above: 1.0 - double.Epsilon is exactly 1.0 (double.Epsilon is the smallest
                // positive SUBNORMAL, ~4.9e-324, far below the ULP of 1.0, ~2.2e-16), so no
                // sub-unity ceiling existed, and `v < limit ? v : limit` is Math.Min, so the
                // negative side was unbounded.
                // The soft-knee/attack-release shaping lives in gainLinearLocal and is applied by
                // the multiply below; `limit` was only ever the hard output bound applied AFTER it.
                // So the constant is corrected AND the bound made symmetric, exactly as in the
                // brickwall path. In-range audio is returned bit for bit unchanged.
                for (int i = 0; i < len; i++)
                {
                    double v = input[i] * gainLinearLocal;
                    input[i] = Math.Clamp(v, -OutputCeiling, OutputCeiling);
                }
            }

            this.Gain_Linear = gainLinearLocal;
        }

        this.IsBrickwall = isBrickwall;
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