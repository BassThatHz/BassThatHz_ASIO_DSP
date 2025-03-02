#nullable enable

namespace BassThatHz_ASIO_DSP_Processor;

#region Usings
using NAudio.Dsp;
using NAudio.Utils;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
#endregion

/// <summary>
///  BassThatHz ASIO DSP Processor Engine
///  Copyright (c) 2025 BassThatHz
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

        double AmplitudeAtFrequency_Linear;

        #region CalculateAmplitude
        double[] inputFiltered = new double[input.Length];
        Array.Copy(input, inputFiltered, input.Length);

        //Filter it with a band pass to calculate the level at that frequency (faster than FFT or DCT)
        switch (this.Biquad_Type)
        {
            case BiquadType.PEQ:
                inputFiltered = this.InputBandPassFilteringBiQuad.Transform(inputFiltered, currentStream);
                break;
            case BiquadType.High_Shelf:
                inputFiltered = this.InputHighPassFilteringBiQuad.Transform(inputFiltered, currentStream);
                break;
            case BiquadType.Low_Shelf:
                inputFiltered = this.InputLowPassFilteringBiQuad.Transform(inputFiltered, currentStream);
                break;
        }

        if (this.Threshold_Type == ThresholdType.Peak)
        {
            AmplitudeAtFrequency_Linear = 0;
            for (int i = 0; i < inputFiltered.Length; i++)
                AmplitudeAtFrequency_Linear = Math.Max(AmplitudeAtFrequency_Linear, Math.Abs(inputFiltered[i]));
        }
        else //ThresholdType.RMS
        {
            double sumOfSquares = 0;
            for (int i = 0; i < inputFiltered.Length; i++)
                sumOfSquares += inputFiltered[i] * inputFiltered[i];

            double rms = Math.Sqrt(sumOfSquares / inputFiltered.Length);
            AmplitudeAtFrequency_Linear = rms;
        }
        #endregion

        if (this.DEQ_Type == DEQType.CutAbove)
        {
            input = this.CutAbove(input, AmplitudeAtFrequency_Linear, currentStream);
        }
        else if (this.DEQ_Type == DEQType.BoostAbove)
        {
            input = this.BoostAbove(input, AmplitudeAtFrequency_Linear, currentStream);
        }
        else if (this.DEQ_Type == DEQType.BoostBelow)
        {
            input = this.BoostBelow(input, AmplitudeAtFrequency_Linear, currentStream);
        }
        else if (this.DEQ_Type == DEQType.CutBelow)
        {
            input = this.CutBelow(input, AmplitudeAtFrequency_Linear, currentStream);
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
    protected double[] CutAbove(double[] input, double amplitudeAtFrequency_Linear, DSP_Stream currentStream)
    {
        var NegativeGain = -Math.Abs(this.TargetGain_dB);
        var amplitudeAtFrequency_dB = Decibels.LinearToDecibels(amplitudeAtFrequency_Linear);
        double InverseCompressionRatio = 10D / this.Ratio;
        double GainReduction_Linear = 1;

        if (amplitudeAtFrequency_dB >= this.Threshold_dB) //Above Threshold
        {
            // Calculate the proportion of how far the amplitude is above the threshold
            double ratioAboveThreshold = (amplitudeAtFrequency_dB - this.Threshold_dB) / Math.Abs(NegativeGain - this.Threshold_dB);

            // Ensure the ratio is between 0 and 1
            ratioAboveThreshold = Math.Max(0, Math.Min(1, ratioAboveThreshold));

            // Apply an exponential scale to make the gain reduction more pronounced as the amplitude gets further above the threshold
            GainReduction_Linear = 1 - Math.Pow(ratioAboveThreshold, InverseCompressionRatio);
        }

        if (this.UseSoftKnee && amplitudeAtFrequency_dB > this.Threshold_dB - this.KneeWidth_dB * 0.5D
                     && amplitudeAtFrequency_dB < this.Threshold_dB + this.KneeWidth_dB * 0.5D)
        {
            // Calculate the start and end points of the knee region in dB.
            double KneeStart_dB = this.Threshold_dB - this.KneeWidth_dB * 0.5D;
            double KneeEnd_dB = this.Threshold_dB + this.KneeWidth_dB * 0.5D;

            // Calculate the ratio of how far the input is into the knee region.
            double Ratio = (amplitudeAtFrequency_dB - KneeStart_dB) / this.KneeWidth_dB;

            // Calculate an adjusted threshold based on the ratio,
            // creating a smooth transition through the knee region.
            double AdjustedThreshold_dB = KneeStart_dB + Ratio * (KneeEnd_dB - KneeStart_dB);

            // Calculate the gain reduction factor for this adjusted threshold.
            GainReduction_Linear = Decibels.DecibelsToLinear(AdjustedThreshold_dB - amplitudeAtFrequency_dB);
        }

        GainReduction_Linear = Math.Min(1, Math.Max(Decibels.DecibelsToLinear(NegativeGain), GainReduction_Linear));

        if (GainReduction_Linear < this.Gain_Linear)
            this.Gain_Linear = this.AttackCoeff * (this.Gain_Linear - GainReduction_Linear) + GainReduction_Linear;
        else
            this.Gain_Linear = this.ReleaseCoeff * (this.Gain_Linear - GainReduction_Linear) + GainReduction_Linear;

        if (amplitudeAtFrequency_dB < this.Threshold_dB) //Below Threshold
        {
            double ExcessdB = Math.Abs(this.Threshold_dB - amplitudeAtFrequency_dB);
            double a = 0.01; // Scale factor
            double b = 0.01; // Adjusts the rate of change
            double d = 1.0; // Final value to approach
            // Calculate the Gain_Linear
            this.Gain_Linear = a * Math.Log10(b * ExcessdB + double.Epsilon) + d;

            this.GainApplied = -Math.Abs(Decibels.LinearToDecibels(this.Gain_Linear));
            if (this.GainApplied > -0.00001d && this.GainApplied < 0.00001d) //Zero it
            {
                this.GainApplied = 0d;
                this.Gain_Linear = 1d;
            }
        }

        this.GainApplied = -Math.Abs(Decibels.LinearToDecibels(this.Gain_Linear));

        if (this.GainApplied < 0)
        {
            //Apply BiQuad
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
            input = this.BiQuad.Transform(input, currentStream);
        }

        return input;
    }

    protected double[] CutBelow(double[] input, double amplitudeAtFrequency_Linear, DSP_Stream currentStream)
    {
        var NegativeGain = -Math.Abs(this.TargetGain_dB);
        var amplitudeAtFrequency_dB = Decibels.LinearToDecibels(amplitudeAtFrequency_Linear);
        double InverseCompressionRatio = 10D / this.Ratio;
        double GainReduction_Linear = 1;

        if (amplitudeAtFrequency_dB <= this.Threshold_dB) //Below Threshold
        {
            // Calculate the proportion of how far the amplitude is below the threshold
            double ratioBelowThreshold = (this.Threshold_dB - amplitudeAtFrequency_dB) / Math.Abs(NegativeGain - this.Threshold_dB);

            // Ensure the ratio is between 0 and 1
            ratioBelowThreshold = Math.Max(0, Math.Min(1, ratioBelowThreshold));

            // Apply an exponential scale to make the gain reduction more pronounced as the amplitude gets further below the threshold
            GainReduction_Linear = 1 - Math.Pow(ratioBelowThreshold, InverseCompressionRatio);
        }

        if (this.UseSoftKnee && amplitudeAtFrequency_dB > this.Threshold_dB - this.KneeWidth_dB * 0.5D
                     && amplitudeAtFrequency_dB < this.Threshold_dB + this.KneeWidth_dB * 0.5D)
        {
            // Calculate the start and end points of the knee region in dB.
            double KneeStart_dB = this.Threshold_dB - this.KneeWidth_dB * 0.5D;
            double KneeEnd_dB = this.Threshold_dB + this.KneeWidth_dB * 0.5D;

            // Calculate the ratio of how far the input is into the knee region.
            double Ratio = (amplitudeAtFrequency_dB - KneeStart_dB) / this.KneeWidth_dB;

            // Calculate an adjusted threshold based on the ratio,
            // creating a smooth transition through the knee region.
            double AdjustedThreshold_dB = KneeStart_dB + Ratio * (KneeEnd_dB - KneeStart_dB);

            // Calculate the gain reduction factor for this adjusted threshold.
            GainReduction_Linear = Decibels.DecibelsToLinear(AdjustedThreshold_dB - amplitudeAtFrequency_dB);
        }

        // Limit the gain reduction to not exceed the maximum specified decrease
        GainReduction_Linear = Math.Min(1, Math.Max(Decibels.DecibelsToLinear(NegativeGain), GainReduction_Linear));

        if (GainReduction_Linear < this.Gain_Linear)
            this.Gain_Linear = this.AttackCoeff * (this.Gain_Linear - GainReduction_Linear) + GainReduction_Linear;
        else
            this.Gain_Linear = this.ReleaseCoeff * (this.Gain_Linear - GainReduction_Linear) + GainReduction_Linear;

        if (amplitudeAtFrequency_dB > this.Threshold_dB) //Above Threshold
        {
            double ExcessdB = Math.Abs(this.Threshold_dB - amplitudeAtFrequency_dB);
            double a = 0.01; // Scale factor
            double b = 0.01; // Adjusts the rate of change
            double d = 1.0; // Final value to approach
            // Calculate the Gain_Linear
            this.Gain_Linear = a * Math.Log10(b * ExcessdB + double.Epsilon) + d;

            this.GainApplied = -Math.Abs(Decibels.LinearToDecibels(this.Gain_Linear));
            if (this.GainApplied > -0.00001d && this.GainApplied < 0.00001d) //Zero it
            {
                this.GainApplied = 0d;
                this.Gain_Linear = 1d;
            }
        }

        this.GainApplied = -Math.Abs(Decibels.LinearToDecibels(this.Gain_Linear));

        if (this.GainApplied < 0)
        {
            //Apply BiQuad
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
            input = this.BiQuad.Transform(input, currentStream);
        }

        return input;
    }

    protected double[] BoostAbove(double[] input, double amplitudeAtFrequency_Linear, DSP_Stream currentStream)
    {
        var amplitudeAtFrequency_dB = Decibels.LinearToDecibels(amplitudeAtFrequency_Linear);
        var PositiveGain = Math.Abs(this.TargetGain_dB);
        double InverseCompressionRatio = 10D / this.Ratio;
        double GainBoost_Linear = 1;

        if (amplitudeAtFrequency_dB >= this.Threshold_dB) //Above Threshold
        {
            // Calculate the proportion of how far the amplitude is above the threshold
            double ratioAboveThreshold = (amplitudeAtFrequency_dB - this.Threshold_dB) / Math.Abs(PositiveGain - this.Threshold_dB);

            // Ensure the ratio is between 0 and 1
            ratioAboveThreshold = Math.Max(0, Math.Min(1, ratioAboveThreshold));

            // Apply a more pronounced gain boost as the amplitude gets further above the threshold
            double scaledBoost = Math.Pow(ratioAboveThreshold, InverseCompressionRatio);

            // Calculate the final GainBoost_Linear, scaling it appropriately
            GainBoost_Linear = 1 + scaledBoost * (Decibels.DecibelsToLinear(PositiveGain) - 1);
        }

        if (this.UseSoftKnee
         && amplitudeAtFrequency_dB > this.Threshold_dB - this.KneeWidth_dB * 0.5D
         && amplitudeAtFrequency_dB < this.Threshold_dB + this.KneeWidth_dB * 0.5D)
        {
            // Calculate the start and end points of the knee region in dB.
            double KneeStart_dB = this.Threshold_dB - this.KneeWidth_dB * 0.5D;
            double KneeEnd_dB = this.Threshold_dB + this.KneeWidth_dB * 0.5D;

            // Calculate the ratio of how far the input is into the knee region.
            double Ratio = (amplitudeAtFrequency_dB - KneeStart_dB) / this.KneeWidth_dB;

            // Calculate an adjusted threshold based on the ratio,
            // creating a smooth transition through the knee region.
            double AdjustedThreshold_dB = KneeStart_dB + Ratio * (KneeEnd_dB - KneeStart_dB);

            // Calculate the gain boost factor for this adjusted threshold.
            double proximityToMax = (AdjustedThreshold_dB - this.Threshold_dB) / (PositiveGain - this.Threshold_dB);
            GainBoost_Linear = 1 + Math.Log(Math.Max(0, proximityToMax) + 1) / Math.Log(2);

            // Ensure the gain boost is within the expected range.
            GainBoost_Linear = Math.Min(PositiveGain, Math.Max(1, GainBoost_Linear));
        }

        GainBoost_Linear = Math.Min(Decibels.DecibelsToLinear(PositiveGain), Math.Max(1, GainBoost_Linear));

        if (GainBoost_Linear < this.Gain_Linear)
            this.Gain_Linear = this.AttackCoeff * (this.Gain_Linear - GainBoost_Linear) + GainBoost_Linear;
        else
            this.Gain_Linear = this.ReleaseCoeff * (this.Gain_Linear - GainBoost_Linear) + GainBoost_Linear;

        if (amplitudeAtFrequency_dB < this.Threshold_dB) //Below Threshold
        {
            double ExcessdB = Math.Abs(this.Threshold_dB - amplitudeAtFrequency_dB);
            double a = 0.01; // Scale factor
            double b = 0.01; // Adjusts the rate of change
            double d = 1.0; // Final value to approach
            // Calculate the Gain_Linear
            this.Gain_Linear = a * Math.Log10(b * ExcessdB + double.Epsilon) + d;

            this.GainApplied = Math.Abs(Decibels.LinearToDecibels(this.Gain_Linear));
            if (this.GainApplied > -0.00001d && this.GainApplied < 0.00001d) //Zero it
            {
                this.GainApplied = 0d;
                this.Gain_Linear = 1d;
            }
        }

        this.GainApplied = Math.Abs(Decibels.LinearToDecibels(this.Gain_Linear));

        if (this.GainApplied > 0)
        {
            //Apply BiQuad
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
            input = this.BiQuad.Transform(input, currentStream);
        }

        return input;
    }

    protected double[] BoostBelow(double[] input, double amplitudeAtFrequency_Linear, DSP_Stream currentStream)
    {
        var amplitudeAtFrequency_dB = Decibels.LinearToDecibels(amplitudeAtFrequency_Linear);
        var PositiveGain = Math.Abs(this.TargetGain_dB);
        double InverseCompressionRatio = 10D / this.Ratio;
        double GainBoost_Linear = 1;

        if (amplitudeAtFrequency_dB <= this.Threshold_dB) //Below Threshold
        {
            // Calculate the proportion of how far the amplitude is below the threshold
            double ratioBelowThreshold = (this.Threshold_dB - amplitudeAtFrequency_dB) / Math.Abs(PositiveGain - this.Threshold_dB);
            
            // Ensure the ratio is between 0 and 1
            ratioBelowThreshold = Math.Max(0, Math.Min(1, ratioBelowThreshold));

            // Apply a more pronounced gain boost as the amplitude gets further below the threshold
            double scaledBoost = Math.Pow(ratioBelowThreshold, InverseCompressionRatio);
            
            // Calculate the final GainBoost_Linear, scaling it appropriately
            GainBoost_Linear = 1 + scaledBoost * (Decibels.DecibelsToLinear(PositiveGain) - 1);
        }

        if (this.UseSoftKnee
         && amplitudeAtFrequency_dB > this.Threshold_dB - this.KneeWidth_dB * 0.5D
         && amplitudeAtFrequency_dB < this.Threshold_dB + this.KneeWidth_dB * 0.5D)
        {
            // Calculate the start and end points of the knee region in dB.
            double KneeStart_dB = this.Threshold_dB - this.KneeWidth_dB * 0.5D;
            double KneeEnd_dB = this.Threshold_dB + this.KneeWidth_dB * 0.5D;

            // Calculate the ratio of how far the input is into the knee region.
            double Ratio = (amplitudeAtFrequency_dB - KneeStart_dB) / this.KneeWidth_dB;

            // Calculate an adjusted threshold based on the ratio,
            // creating a smooth transition through the knee region.
            double AdjustedThreshold_dB = KneeStart_dB + Ratio * (KneeEnd_dB - KneeStart_dB);

            // Calculate the gain boost factor for this adjusted threshold.
            double proximityToMax = (AdjustedThreshold_dB - this.Threshold_dB) / (PositiveGain - this.Threshold_dB);
            GainBoost_Linear = 1 + Math.Log(Math.Max(0, proximityToMax) + 1) / Math.Log(2);
        }

        GainBoost_Linear = Math.Min(Decibels.DecibelsToLinear(PositiveGain), Math.Max(1, GainBoost_Linear));

        if (GainBoost_Linear < this.Gain_Linear)
            this.Gain_Linear = this.AttackCoeff * (this.Gain_Linear - GainBoost_Linear) + GainBoost_Linear;
        else
            this.Gain_Linear = this.ReleaseCoeff * (this.Gain_Linear - GainBoost_Linear) + GainBoost_Linear;

        if (amplitudeAtFrequency_dB > this.Threshold_dB) //Above Threshold
        {
            double ExcessdB = Math.Abs(this.Threshold_dB - amplitudeAtFrequency_dB);
            double a = 0.01; // Scale factor
            double b = 0.01; // Adjusts the rate of change
            double d = 1.0; // Final value to approach
            // Calculate the Gain_Linear
            this.Gain_Linear = a * Math.Log10(b * ExcessdB + double.Epsilon) + d;

            this.GainApplied = Math.Abs(Decibels.LinearToDecibels(this.Gain_Linear));
            if (this.GainApplied > -0.00001d && this.GainApplied < 0.00001d) //Zero it
            {
                this.GainApplied = 0d;
                this.Gain_Linear = 1d;
            }
        }            

        this.GainApplied = Math.Abs(Decibels.LinearToDecibels(this.Gain_Linear));

        if (this.GainApplied > 0)
        {
            //Apply BiQuad
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

            input = this.BiQuad.Transform(input, currentStream);
        }

        return input;
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