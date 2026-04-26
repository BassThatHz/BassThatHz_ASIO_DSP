#nullable enable

namespace BassThatHz_ASIO_DSP_Processor;

#region Usings
using NAudio.Dsp;
using NAudio.Utils;
using System;
using System.Buffers;
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
public class DEQ : IFilter
{
    public enum ThresholdType
    {
        Peak,
        RMS
    }
    public enum DEQType
    {
        CutAbove,
        CutBelow,
        BoostAbove,
        BoostBelow
    }
    public enum BiquadType
    {
        PEQ,
        High_Shelf,
        Low_Shelf
    }

    #region Variables
    [IgnoreDataMember]
    protected double SampleRate = 1;
    [IgnoreDataMember]
    public double GainApplied = 0;
    [IgnoreDataMember]
    protected double AttackCoeff;
    [IgnoreDataMember]
    protected double ReleaseCoeff;
    [IgnoreDataMember]
    protected double Gain_Linear = 1.0;

    [IgnoreDataMember]
    protected BiQuadFilter BiQuad = new();

    [IgnoreDataMember]
    protected BiQuadFilter InputBandPassFilteringBiQuad = new();

    [IgnoreDataMember]
    protected BiQuadFilter InputHighPassFilteringBiQuad = new();
    
    [IgnoreDataMember]
    protected BiQuadFilter InputLowPassFilteringBiQuad = new();
    #endregion

    #region Public Properties
    public DEQType DEQ_Type { get; set; } = DEQType.BoostBelow;
    public BiquadType Biquad_Type { get; set; } = BiquadType.PEQ;

    public ThresholdType Threshold_Type { get; set; } = ThresholdType.Peak;
    public double TargetFrequency { get; set; } = 1000;
    public double TargetGain_dB { get; set; } = 0;
    public double TargetQ { get; set; } = 1;
    public double TargetSlope { get; set; } = 1;
    public double Threshold_dB { get; set; } = -40;
    public double Ratio { get; set; } = 240; // e.g. 24db 10:1
    public double AttackTime_ms { get; set; } = 1;
    public double ReleaseTime_ms { get; set; } = 1;
    public double KneeWidth_dB { get; set; } = 24;
    public bool UseSoftKnee { get; set; } = true;
    #endregion

    #region Public Functions
    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public double[] Transform(double[] input, DSP_Stream currentStream)
    {
        int len = input.Length;

        if (len == 0)
            return input;

        double amplitudeAtFrequencyLinear;

        var pool = ArrayPool<double>.Shared;
        double[]? rented = null;
        try
        {
            rented = pool.Rent(len);
            var tmp = rented.AsSpan(0, len);

            // copy input into rented buffer
            input.AsSpan().CopyTo(tmp);

            // Filter it with a band pass to calculate level at that frequency (faster than FFT or DCT)
            switch (this.Biquad_Type)
            {
                case BiquadType.PEQ:
                    this.InputBandPassFilteringBiQuad.TransformInPlace(tmp, currentStream);
                    break;
                case BiquadType.High_Shelf:
                    this.InputHighPassFilteringBiQuad.TransformInPlace(tmp, currentStream);
                    break;
                case BiquadType.Low_Shelf:
                    this.InputLowPassFilteringBiQuad.TransformInPlace(tmp, currentStream);
                    break;
            }

            if (this.Threshold_Type == ThresholdType.Peak)
            {
                double max = 0;
                for (int i = 0; i < len; i++)
                {
                    double v = Math.Abs(tmp[i]);
                    if (v > max) max = v;
                }
                amplitudeAtFrequencyLinear = max;
            }
            else // RMS
            {
                double sum = 0;
                for (int i = 0; i < len; i++)
                {
                    double v = tmp[i];
                    sum += v * v;
                }
                amplitudeAtFrequencyLinear = Math.Sqrt(sum / len);
            }
        }
        finally
        {
            if (rented != null)
                pool.Return(rented, clearArray: false);
        }

        // Dispatch to appropriate action using Span-based in-place processing on input
        var inputSpan = input.AsSpan();
        switch (this.DEQ_Type)
        {
            case DEQType.CutAbove:
                CutAbove(inputSpan, amplitudeAtFrequencyLinear, currentStream);
                break;
            case DEQType.BoostAbove:
                BoostAbove(inputSpan, amplitudeAtFrequencyLinear, currentStream);
                break;
            case DEQType.BoostBelow:
                BoostBelow(inputSpan, amplitudeAtFrequencyLinear, currentStream);
                break;
            case DEQType.CutBelow:
                CutBelow(inputSpan, amplitudeAtFrequencyLinear, currentStream);
                break;
        }

        return input;
    }

    public void ResetSampleRate(int sampleRate)
    {
        this.SampleRate = sampleRate;
        this.BiQuad.ChangeSampleRate(sampleRate);
        this.CalculateCoeffs(sampleRate);
    }

    public void ApplySettings()
    {
        //Fix the gain magnitude sign by DEQ Type in case the user messed up
        switch (this.DEQ_Type)
        {
            case DEQType.CutAbove:
            case DEQType.CutBelow:
                this.TargetGain_dB = -Math.Abs(this.TargetGain_dB);
                break;
            case DEQType.BoostAbove:
            case DEQType.BoostBelow:
                this.TargetGain_dB = Math.Abs(this.TargetGain_dB);
                break;
            default:
                throw new NotSupportedException();
         }

        switch (this.Biquad_Type)
        {
            case BiquadType.PEQ:
                this.BiQuad.PeakingEQ(this.SampleRate, this.TargetFrequency, this.TargetQ, this.TargetGain_dB);
                break;
            case BiquadType.High_Shelf:
                this.BiQuad.HighShelf(this.SampleRate, this.TargetFrequency, this.TargetSlope, this.TargetGain_dB);
                break;
            case BiquadType.Low_Shelf:
                this.BiQuad.LowShelf(this.SampleRate, this.TargetFrequency, this.TargetSlope, this.TargetGain_dB);
                break;
            default:
                throw new NotSupportedException();
        }

        this.InputBandPassFilteringBiQuad.BandPassFilterConstantPeakGain(this.SampleRate, this.TargetFrequency, this.TargetQ);
        this.InputHighPassFilteringBiQuad.HighPassFilter(this.SampleRate, this.TargetFrequency, this.TargetQ);
        this.InputLowPassFilteringBiQuad.LowPassFilter(this.SampleRate, this.TargetFrequency, this.TargetQ);
        this.CalculateCoeffs(this.SampleRate);
    }

    public void CalculateCoeffs(double sampleRate)
    {
        // Calculate the attack coefficient for the compressor.
        // The attack coefficient determines how quickly the compressor responds to the signal exceeding the threshold.
        // 'AttackTime_ms' is the attack time in milliseconds, and 'SampleRate' is the sample rate of the audio signal.
        // The formula converts the attack time from milliseconds to seconds (by multiplying by 0.001)
        // and then calculates the number of samples over which the attack time extends.
        // The exponential function (Math.Exp) is used to create a smooth, logarithmic response curve.
        // A negative exponent is used so that the coefficient approaches 0 as the attack time increases,
        // resulting in a slower response to increasing signal levels.
        AttackCoeff = Math.Exp(-1.0 / (0.001 * AttackTime_ms * sampleRate));

        // Calculate the release coefficient for the compressor.
        // The release coefficient determines how quickly the compressor stops reducing the gain after the signal falls below the threshold.
        // 'ReleaseTime_ms' is the release time in milliseconds.
        // Similar to the attack coefficient, this formula converts the release time to seconds, 
        // then calculates the number of samples over this time period, and applies the exponential function.
        // The negative exponent means that a longer release time will result in a slower return to the uncompressed state.
        ReleaseCoeff = Math.Exp(-1.0 / (0.001 * ReleaseTime_ms * sampleRate));
    }
    #endregion

    #region Protected Functions
    protected void CutAbove(Span<double> input, double amplitudeAtFrequency_Linear, DSP_Stream currentStream)
    {
        var negativeGain = -Math.Abs(this.TargetGain_dB);
        var amplitudeAtFrequencyDb = Decibels.LinearToDecibels(amplitudeAtFrequency_Linear);
        double inverseCompressionRatio = 10D / this.Ratio;
        double gainReductionLinear = 1;

        if (amplitudeAtFrequencyDb >= this.Threshold_dB) // Above threshold
        {
            double ratioAboveThreshold = (amplitudeAtFrequencyDb - this.Threshold_dB) / Math.Abs(negativeGain - this.Threshold_dB);
            if (ratioAboveThreshold < 0) ratioAboveThreshold = 0;
            else if (ratioAboveThreshold > 1) ratioAboveThreshold = 1;
            gainReductionLinear = 1 - Math.Pow(ratioAboveThreshold, inverseCompressionRatio);
        }

        if (this.UseSoftKnee && amplitudeAtFrequencyDb > this.Threshold_dB - this.KneeWidth_dB * 0.5D
            && amplitudeAtFrequencyDb < this.Threshold_dB + this.KneeWidth_dB * 0.5D)
        {
            double kneeStartDb = this.Threshold_dB - this.KneeWidth_dB * 0.5D;
            double ratio = (amplitudeAtFrequencyDb - kneeStartDb) / this.KneeWidth_dB;
            double adjustedThresholdDb = kneeStartDb + ratio * this.KneeWidth_dB;
            gainReductionLinear = Decibels.DecibelsToLinear(adjustedThresholdDb - amplitudeAtFrequencyDb);
        }

        gainReductionLinear = Math.Min(1, Math.Max(Decibels.DecibelsToLinear(negativeGain), gainReductionLinear));

        if (gainReductionLinear < this.Gain_Linear)
            this.Gain_Linear = this.AttackCoeff * (this.Gain_Linear - gainReductionLinear) + gainReductionLinear;
        else
            this.Gain_Linear = this.ReleaseCoeff * (this.Gain_Linear - gainReductionLinear) + gainReductionLinear;

        if (amplitudeAtFrequencyDb < this.Threshold_dB)
        {
            double excessDb = Math.Abs(this.Threshold_dB - amplitudeAtFrequencyDb);
            double a = 0.01, b = 0.01, d = 1.0;
            this.Gain_Linear = a * Math.Log10(b * excessDb + double.Epsilon) + d;

            this.GainApplied = -Math.Abs(Decibels.LinearToDecibels(this.Gain_Linear));
            if (this.GainApplied > -0.00001d && this.GainApplied < 0.00001d)
            {
                this.GainApplied = 0d;
                this.Gain_Linear = 1d;
            }
        }

        this.GainApplied = -Math.Abs(Decibels.LinearToDecibels(this.Gain_Linear));

        if (this.GainApplied < 0)
        {
            switch (this.Biquad_Type)
            {
                case BiquadType.PEQ:
                    this.BiQuad.UpdateGain(this.GainApplied);
                    break;
                case BiquadType.High_Shelf:
                    this.BiQuad.UpdateGain_HighShelf(this.GainApplied);
                    break;
                case BiquadType.Low_Shelf:
                    this.BiQuad.UpdateGain_LowShelf(this.GainApplied);
                    break;
            }
            this.BiQuad.TransformInPlace(input, currentStream);
        }
    }

    protected void CutBelow(Span<double> input, double amplitudeAtFrequency_Linear, DSP_Stream currentStream)
    {
        var negativeGain = -Math.Abs(this.TargetGain_dB);
        var amplitudeAtFrequencyDb = Decibels.LinearToDecibels(amplitudeAtFrequency_Linear);
        double inverseCompressionRatio = 10D / this.Ratio;
        double gainReductionLinear = 1;

        if (amplitudeAtFrequencyDb <= this.Threshold_dB)
        {
            double ratioBelowThreshold = (this.Threshold_dB - amplitudeAtFrequencyDb) / Math.Abs(negativeGain - this.Threshold_dB);
            if (ratioBelowThreshold < 0) ratioBelowThreshold = 0;
            else if (ratioBelowThreshold > 1) ratioBelowThreshold = 1;
            gainReductionLinear = 1 - Math.Pow(ratioBelowThreshold, inverseCompressionRatio);
        }

        if (this.UseSoftKnee && amplitudeAtFrequencyDb > this.Threshold_dB - this.KneeWidth_dB * 0.5D
            && amplitudeAtFrequencyDb < this.Threshold_dB + this.KneeWidth_dB * 0.5D)
        {
            double kneeStartDb = this.Threshold_dB - this.KneeWidth_dB * 0.5D;
            double ratio = (amplitudeAtFrequencyDb - kneeStartDb) / this.KneeWidth_dB;
            double adjustedThresholdDb = kneeStartDb + ratio * this.KneeWidth_dB;
            gainReductionLinear = Decibels.DecibelsToLinear(adjustedThresholdDb - amplitudeAtFrequencyDb);
        }

        gainReductionLinear = Math.Min(1, Math.Max(Decibels.DecibelsToLinear(negativeGain), gainReductionLinear));

        if (gainReductionLinear < this.Gain_Linear)
            this.Gain_Linear = this.AttackCoeff * (this.Gain_Linear - gainReductionLinear) + gainReductionLinear;
        else
            this.Gain_Linear = this.ReleaseCoeff * (this.Gain_Linear - gainReductionLinear) + gainReductionLinear;

        if (amplitudeAtFrequencyDb > this.Threshold_dB)
        {
            double excessDb = Math.Abs(this.Threshold_dB - amplitudeAtFrequencyDb);
            double a = 0.01, b = 0.01, d = 1.0;
            this.Gain_Linear = a * Math.Log10(b * excessDb + double.Epsilon) + d;

            this.GainApplied = -Math.Abs(Decibels.LinearToDecibels(this.Gain_Linear));
            if (this.GainApplied > -0.00001d && this.GainApplied < 0.00001d)
            {
                this.GainApplied = 0d;
                this.Gain_Linear = 1d;
            }
        }

        this.GainApplied = -Math.Abs(Decibels.LinearToDecibels(this.Gain_Linear));

        if (this.GainApplied < 0)
        {
            switch (this.Biquad_Type)
            {
                case BiquadType.PEQ:
                    this.BiQuad.UpdateGain(this.GainApplied);
                    break;
                case BiquadType.High_Shelf:
                    this.BiQuad.UpdateGain_HighShelf(this.GainApplied);
                    break;
                case BiquadType.Low_Shelf:
                    this.BiQuad.UpdateGain_LowShelf(this.GainApplied);
                    break;
            }
            this.BiQuad.TransformInPlace(input, currentStream);
        }
    }

    protected void BoostAbove(Span<double> input, double amplitudeAtFrequency_Linear, DSP_Stream currentStream)
    {
        var amplitudeAtFrequencyDb = Decibels.LinearToDecibels(amplitudeAtFrequency_Linear);
        var positiveGain = Math.Abs(this.TargetGain_dB);
        double inverseCompressionRatio = 10D / this.Ratio;
        double gainBoostLinear = 1;

        if (amplitudeAtFrequencyDb >= this.Threshold_dB)
        {
            double ratioAboveThreshold = (amplitudeAtFrequencyDb - this.Threshold_dB) / Math.Abs(positiveGain - this.Threshold_dB);
            if (ratioAboveThreshold < 0) ratioAboveThreshold = 0;
            else if (ratioAboveThreshold > 1) ratioAboveThreshold = 1;
            double scaledBoost = Math.Pow(ratioAboveThreshold, inverseCompressionRatio);
            gainBoostLinear = 1 + scaledBoost * (Decibels.DecibelsToLinear(positiveGain) - 1);
        }

        if (this.UseSoftKnee && amplitudeAtFrequencyDb > this.Threshold_dB - this.KneeWidth_dB * 0.5D
            && amplitudeAtFrequencyDb < this.Threshold_dB + this.KneeWidth_dB * 0.5D)
        {
            double kneeStartDb = this.Threshold_dB - this.KneeWidth_dB * 0.5D;
            double ratio = (amplitudeAtFrequencyDb - kneeStartDb) / this.KneeWidth_dB;
            double adjustedThresholdDb = kneeStartDb + ratio * this.KneeWidth_dB;
            double proximityToMax = (adjustedThresholdDb - this.Threshold_dB) / (positiveGain - this.Threshold_dB);
            gainBoostLinear = 1 + Math.Log(Math.Max(0, proximityToMax) + 1) / Math.Log(2);
            gainBoostLinear = Math.Min(positiveGain, Math.Max(1, gainBoostLinear));
        }

        gainBoostLinear = Math.Min(Decibels.DecibelsToLinear(positiveGain), Math.Max(1, gainBoostLinear));

        if (gainBoostLinear < this.Gain_Linear)
            this.Gain_Linear = this.AttackCoeff * (this.Gain_Linear - gainBoostLinear) + gainBoostLinear;
        else
            this.Gain_Linear = this.ReleaseCoeff * (this.Gain_Linear - gainBoostLinear) + gainBoostLinear;

        if (amplitudeAtFrequencyDb < this.Threshold_dB)
        {
            double excessDb = Math.Abs(this.Threshold_dB - amplitudeAtFrequencyDb);
            double a = 0.01, b = 0.01, d = 1.0;
            this.Gain_Linear = a * Math.Log10(b * excessDb + double.Epsilon) + d;

            this.GainApplied = Math.Abs(Decibels.LinearToDecibels(this.Gain_Linear));
            if (this.GainApplied > -0.00001d && this.GainApplied < 0.00001d)
            {
                this.GainApplied = 0d;
                this.Gain_Linear = 1d;
            }
        }

        this.GainApplied = Math.Abs(Decibels.LinearToDecibels(this.Gain_Linear));

        if (this.GainApplied > 0)
        {
            switch (this.Biquad_Type)
            {
                case BiquadType.PEQ:
                    this.BiQuad.UpdateGain(this.GainApplied);
                    break;
                case BiquadType.High_Shelf:
                    this.BiQuad.UpdateGain_HighShelf(this.GainApplied);
                    break;
                case BiquadType.Low_Shelf:
                    this.BiQuad.UpdateGain_LowShelf(this.GainApplied);
                    break;
            }
            this.BiQuad.TransformInPlace(input, currentStream);
        }
    }

    protected void BoostBelow(Span<double> input, double amplitudeAtFrequency_Linear, DSP_Stream currentStream)
    {
        var amplitudeAtFrequencyDb = Decibels.LinearToDecibels(amplitudeAtFrequency_Linear);
        var positiveGain = Math.Abs(this.TargetGain_dB);
        double inverseCompressionRatio = 10D / this.Ratio;
        double gainBoostLinear = 1;

        if (amplitudeAtFrequencyDb <= this.Threshold_dB)
        {
            double ratioBelowThreshold = (this.Threshold_dB - amplitudeAtFrequencyDb) / Math.Abs(positiveGain - this.Threshold_dB);
            if (ratioBelowThreshold < 0) ratioBelowThreshold = 0;
            else if (ratioBelowThreshold > 1) ratioBelowThreshold = 1;
            double scaledBoost = Math.Pow(ratioBelowThreshold, inverseCompressionRatio);
            gainBoostLinear = 1 + scaledBoost * (Decibels.DecibelsToLinear(positiveGain) - 1);
        }

        if (this.UseSoftKnee && amplitudeAtFrequencyDb > this.Threshold_dB - this.KneeWidth_dB * 0.5D
            && amplitudeAtFrequencyDb < this.Threshold_dB + this.KneeWidth_dB * 0.5D)
        {
            double kneeStartDb = this.Threshold_dB - this.KneeWidth_dB * 0.5D;
            double ratio = (amplitudeAtFrequencyDb - kneeStartDb) / this.KneeWidth_dB;
            double adjustedThresholdDb = kneeStartDb + ratio * this.KneeWidth_dB;
            double proximityToMax = (adjustedThresholdDb - this.Threshold_dB) / (positiveGain - this.Threshold_dB);
            gainBoostLinear = 1 + Math.Log(Math.Max(0, proximityToMax) + 1) / Math.Log(2);
        }

        gainBoostLinear = Math.Min(Decibels.DecibelsToLinear(positiveGain), Math.Max(1, gainBoostLinear));

        if (gainBoostLinear < this.Gain_Linear)
            this.Gain_Linear = this.AttackCoeff * (this.Gain_Linear - gainBoostLinear) + gainBoostLinear;
        else
            this.Gain_Linear = this.ReleaseCoeff * (this.Gain_Linear - gainBoostLinear) + gainBoostLinear;

        if (amplitudeAtFrequencyDb > this.Threshold_dB)
        {
            double excessDb = Math.Abs(this.Threshold_dB - amplitudeAtFrequencyDb);
            double a = 0.01, b = 0.01, d = 1.0;
            this.Gain_Linear = a * Math.Log10(b * excessDb + double.Epsilon) + d;

            this.GainApplied = Math.Abs(Decibels.LinearToDecibels(this.Gain_Linear));
            if (this.GainApplied > -0.00001d && this.GainApplied < 0.00001d)
            {
                this.GainApplied = 0d;
                this.Gain_Linear = 1d;
            }
        }

        this.GainApplied = Math.Abs(Decibels.LinearToDecibels(this.Gain_Linear));

        if (this.GainApplied > 0)
        {
            switch (this.Biquad_Type)
            {
                case BiquadType.PEQ:
                    this.BiQuad.UpdateGain(this.GainApplied);
                    break;
                case BiquadType.High_Shelf:
                    this.BiQuad.UpdateGain_HighShelf(this.GainApplied);
                    break;
                case BiquadType.Low_Shelf:
                    this.BiQuad.UpdateGain_LowShelf(this.GainApplied);
                    break;
            }
            this.BiQuad.TransformInPlace(input, currentStream);
        }
    }

    #endregion

    #region IFilter Interface
    public bool FilterEnabled { get; set; }
    public IFilter GetFilter => this;

    public FilterTypes FilterType { get; } = FilterTypes.DEQ;
    public FilterProcessingTypes FilterProcessingType { get; } = FilterProcessingTypes.WholeBlock;

    public IFilter DeepClone()
    {
        return CommonFunctions.DeepClone(this);
    }

    #endregion
}