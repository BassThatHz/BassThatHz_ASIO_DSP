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
                double limit = 1.0 - double.Epsilon;
                for (int i = 0; i < len; i++)
                {
                    double v = input[i] * gainReductionLinear2;
                    input[i] = v < limit ? v : limit;
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
                double limit = 1.0 - double.Epsilon;
                for (int i = 0; i < len; i++)
                {
                    double v = input[i] * gainLinearLocal;
                    input[i] = v < limit ? v : limit;
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