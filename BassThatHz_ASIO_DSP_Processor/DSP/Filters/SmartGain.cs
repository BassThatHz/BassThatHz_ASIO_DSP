#nullable enable

namespace BassThatHz_ASIO_DSP_Processor;

#region Usings
using NAudio.Utils;
using System;
using System.Runtime.CompilerServices;
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
public class SmartGain : IFilter
{
    #region Public Variables and Properties
    public bool PeakHold = false;
    public TimeSpan Duration = TimeSpan.FromMilliseconds(1000);

    public double PeakLevelLinear { get; protected set; } = 0;

    public double InputAbs { get; protected set; } = 0;

    public double HeadroomLinear { get; protected set; } = 0;

    public double ActualGainLinear { get; protected set; } = 0;

    public double ActualGaindB { get; protected set; } = 0;

    public double MaxAllowedLinearGain { get; protected set; } = 0;

    protected double _GaindB = 0;
    public double GaindB
    {
        get
        {
            return this._GaindB;
        }
        set
        {
            this.RequestedGainLinear = Decibels.DecibelsToLinear(value);
            this._GaindB = value;
        }
    }
    #endregion

    #region Protected Variables
    protected double RequestedGainLinear = 1;
    protected DateTime StartPeakDuration;
    #endregion

    #region Public Functions
    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public double[] Transform(double[] input, DSP_Stream currentStream)
    {
        int len = input.Length;
        if (len == 0)
            return input;

        // Cache frequently used fields to locals
        double requestedGain = this.RequestedGainLinear;
        double gaindB = this.GaindB;
        bool peakHold = this.PeakHold;
        TimeSpan duration = this.Duration;

        double peakLevel = this.PeakLevelLinear;
        DateTime startPeak = this.StartPeakDuration;

        // Sample-local working variables
        double headroomLinear = this.HeadroomLinear;
        double actualGainLinear = this.ActualGainLinear;
        double actualGaindB = this.ActualGaindB;
        double maxAllowedLinearGain = this.MaxAllowedLinearGain;

        // Read current time once per block to reduce system calls
        DateTime now = DateTime.UtcNow;

        // If not peak-holding and duration expired, apply a gentle decay once per block
        if (!peakHold && now - startPeak > duration)
        {
            if (headroomLinear * 0.707d > requestedGain)
                peakLevel *= 0.9999d;
            startPeak = now;
        }

        // Main processing loop
        for (int i = 0; i < len; i++)
        {
            double sample = input[i];
            double instAbs = sample < 0 ? -sample : sample;

            // Update peak and timestamp
            if (instAbs > peakLevel)
            {
                peakLevel = instAbs;
                startPeak = now;
            }

            // Max allowed gain to avoid clipping
            maxAllowedLinearGain = peakLevel > 0 ? 1.0 / peakLevel : double.PositiveInfinity;

            // Choose actual gain based on clipping avoidance
            if (requestedGain > maxAllowedLinearGain)
            {
                actualGaindB = Decibels.LinearToDecibels(maxAllowedLinearGain);
                actualGainLinear = maxAllowedLinearGain;
            }
            else
            {
                actualGaindB = gaindB;
                actualGainLinear = requestedGain;
            }

            // Apply gain
            double result = sample * actualGainLinear;

            // Headroom and soft clipping prevention
            double rvAbs = result < 0 ? -result : result;
            headroomLinear = rvAbs > 0 ? 1.0 / rvAbs : double.PositiveInfinity;
            if (rvAbs >= 0.999d)
                result = Math.Sign(result) * 0.707d;

            input[i] = result;
        }

        // Write back state once per block
        this.InputAbs = Math.Abs(input[len - 1]);
        this.PeakLevelLinear = peakLevel;
        this.StartPeakDuration = startPeak;
        this.HeadroomLinear = headroomLinear;
        this.ActualGainLinear = actualGainLinear;
        this.ActualGaindB = actualGaindB;
        this.MaxAllowedLinearGain = maxAllowedLinearGain;

        return input;
    }

    public void ResetSampleRate(int sampleRate)
    {
        //Non Applicable
    }

    public void ApplySettings()
    {
        //Non-Applicable
    }
    #endregion

    #region IFilter Interface
    public bool FilterEnabled { get; set; }

    public IFilter GetFilter => this;

    public FilterTypes FilterType { get; } = FilterTypes.SmartGain;
    public FilterProcessingTypes FilterProcessingType { get; } = FilterProcessingTypes.WholeBlock;

    public IFilter DeepClone()
    {
        return CommonFunctions.DeepClone(this);
    }
    #endregion

}