
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
/// BassThatHz ASIO DSP Processor Engine
/// Copyright (c) 2026 BassThatHz
///
/// Permission is hereby granted to use this software
/// and its associated documentation files (the "Software"),
/// for educational purposess, scientific purposess or private purposess
/// or as part of an open-source community project,
/// and (NOT for commerical use or resale in substaintial part or whole without prior authorization)
/// and all copies of the Software subject to the following conditions:
///
/// The copyright notice and this permission notice shall be included in all
/// copies or substantial portions of the Software.
///
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

    public double Ratio { get; set; } = 240;

    public double AttackTime_ms { get; set; } = 1;

    public double ReleaseTime_ms { get; set; } = 1;

    public double KneeWidth_dB { get; set; } = 24;

    public bool UseSoftKnee { get; set; } = true;

    #endregion

    #region Public Functions

    [MethodImpl(MethodImplOptions.AggressiveOptimization |
                MethodImplOptions.AggressiveInlining)]
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

            input.AsSpan().CopyTo(tmp);

            // Measure the signal level in the selected frequency region.
            switch (this.Biquad_Type)
            {
                case BiquadType.PEQ:
                    this.InputBandPassFilteringBiQuad
                        .TransformInPlace(tmp, currentStream);
                    break;

                case BiquadType.High_Shelf:
                    this.InputHighPassFilteringBiQuad
                        .TransformInPlace(tmp, currentStream);
                    break;

                case BiquadType.Low_Shelf:
                    this.InputLowPassFilteringBiQuad
                        .TransformInPlace(tmp, currentStream);
                    break;

                default:
                    throw new NotSupportedException();
            }

            if (this.Threshold_Type == ThresholdType.Peak)
            {
                double max = 0.0;

                for (int i = 0; i < len; i++)
                {
                    double v = Math.Abs(tmp[i]);

                    if (v > max)
                        max = v;
                }

                amplitudeAtFrequencyLinear = max;
            }
            else
            {
                double sum = 0.0;

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

        // Dispatch the measured level to the selected dynamic mode.
        Span<double> inputSpan = input.AsSpan();

        switch (this.DEQ_Type)
        {
            case DEQType.CutAbove:
                CutAbove(
                    inputSpan,
                    amplitudeAtFrequencyLinear,
                    currentStream);
                break;

            case DEQType.CutBelow:
                CutBelow(
                    inputSpan,
                    amplitudeAtFrequencyLinear,
                    currentStream);
                break;

            case DEQType.BoostAbove:
                BoostAbove(
                    inputSpan,
                    amplitudeAtFrequencyLinear,
                    currentStream);
                break;

            case DEQType.BoostBelow:
                BoostBelow(
                    inputSpan,
                    amplitudeAtFrequencyLinear,
                    currentStream);
                break;

            default:
                throw new NotSupportedException();
        }

        return input;
    }

    public void ResetSampleRate(int sampleRate)
    {
        if (sampleRate <= 0)
            throw new ArgumentOutOfRangeException(nameof(sampleRate));

        this.SampleRate = sampleRate;

        this.BiQuad.ChangeSampleRate(sampleRate);

        this.CalculateCoeffs(sampleRate);
    }

    public void ApplySettings()
    {
        // Normalize the configured gain sign to match the selected mode.
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
                this.BiQuad.PeakingEQ(
                    this.SampleRate,
                    this.TargetFrequency,
                    this.TargetQ,
                    this.TargetGain_dB);
                break;

            case BiquadType.High_Shelf:
                this.BiQuad.HighShelf(
                    this.SampleRate,
                    this.TargetFrequency,
                    this.TargetSlope,
                    this.TargetGain_dB);
                break;

            case BiquadType.Low_Shelf:
                this.BiQuad.LowShelf(
                    this.SampleRate,
                    this.TargetFrequency,
                    this.TargetSlope,
                    this.TargetGain_dB);
                break;

            default:
                throw new NotSupportedException();
        }

        this.InputBandPassFilteringBiQuad.BandPassFilterConstantPeakGain(
            this.SampleRate,
            this.TargetFrequency,
            this.TargetQ);

        this.InputHighPassFilteringBiQuad.HighPassFilter(
            this.SampleRate,
            this.TargetFrequency,
            this.TargetQ);

        this.InputLowPassFilteringBiQuad.LowPassFilter(
            this.SampleRate,
            this.TargetFrequency,
            this.TargetQ);

        this.CalculateCoeffs(this.SampleRate);
    }

    public void CalculateCoeffs(double sampleRate)
    {
        if (!double.IsFinite(sampleRate) || sampleRate <= 0.0)
            throw new ArgumentOutOfRangeException(nameof(sampleRate));

        double attackMs = Math.Max(0.0, this.AttackTime_ms);
        double releaseMs = Math.Max(0.0, this.ReleaseTime_ms);

        AttackCoeff = attackMs <= 0.0
            ? 0.0
            : Math.Exp(-1.0 / (0.001 * attackMs * sampleRate));

        ReleaseCoeff = releaseMs <= 0.0
            ? 0.0
            : Math.Exp(-1.0 / (0.001 * releaseMs * sampleRate));
    }

    #endregion

    #region Protected Functions - Dynamic EQ modes

    /// <summary>
    /// Calculates the desired dynamic gain from the measured level,
    /// smooths the gain using attack/release coefficients, updates
    /// GainApplied, and applies the resulting EQ filter.
    ///
    /// direction:
    ///   +1 = activate above the threshold
    ///   -1 = activate below the threshold
    ///
    /// gainSign:
    ///   +1 = boost
    ///   -1 = cut
    /// </summary>
    private void ApplyDynamicMode(
        Span<double> input,
        double amplitudeAtFrequency_Linear,
        DSP_Stream currentStream,
        int direction,
        int gainSign)
    {
        double amplitude = double.IsFinite(amplitudeAtFrequency_Linear)
            ? Math.Max(0.0, amplitudeAtFrequency_Linear)
            : 0.0;

        double levelDb = amplitude > 0.0
            ? Decibels.LinearToDecibels(amplitude)
            : double.NegativeInfinity;

        double thresholdDb = double.IsFinite(Threshold_dB)
            ? Threshold_dB
            : -40.0;

        double maxGainDb = double.IsFinite(TargetGain_dB)
            ? Math.Abs(TargetGain_dB)
            : 0.0;

        double ratio = double.IsFinite(Ratio)
            ? Math.Max(1.0, Ratio)
            : 1.0;

        // Positive distance means the detector is on the active side.
        // direction = +1: activate above threshold.
        // direction = -1: activate below threshold.
        double distanceDb = direction * (levelDb - thresholdDb);

        double kneeDb = UseSoftKnee && double.IsFinite(KneeWidth_dB)
            ? Math.Max(0.0, KneeWidth_dB)
            : 0.0;

        double activation;

        if (kneeDb > 0.0)
        {
            double halfKnee = kneeDb * 0.5;
            double kneeStart = -halfKnee;
            double kneeEnd = halfKnee;

            if (distanceDb <= kneeStart)
            {
                activation = 0.0;
            }
            else if (distanceDb >= kneeEnd)
            {
                activation = 1.0;
            }
            else
            {
                double t = (distanceDb - kneeStart) / kneeDb;

                // Smooth transition through the knee.
                activation = t * t * (3.0 - 2.0 * t);
            }
        }
        else
        {
            activation = distanceDb > 0.0 ? 1.0 : 0.0;
        }

        // Ratio controls the shape of the activation curve.
        // Larger ratios make the response more gradual near the knee.
        double ratioExponent = Math.Max(1.0, ratio / 10.0);
        double gainFraction = Math.Pow(
            Math.Clamp(activation, 0.0, 1.0),
            ratioExponent);

        double requestedMagnitudeDb = maxGainDb * gainFraction;
        double requestedGainDb = gainSign * requestedMagnitudeDb;

        double requestedGainLinear =
            Decibels.DecibelsToLinear(requestedGainDb);

        if (!double.IsFinite(Gain_Linear) || Gain_Linear <= 0.0)
            Gain_Linear = 1.0;

        double currentGainDb =
            Decibels.LinearToDecibels(Gain_Linear);

        bool movingAwayFromUnity =
            Math.Abs(requestedGainDb) > Math.Abs(currentGainDb);

        // If the signal is on the inactive side of the threshold and the
        // requested EQ magnitude is lower, remove the existing dynamic
        // gain immediately. Do not wait for ReleaseTime_ms.
        bool thresholdRequiresImmediateReduction =
            distanceDb <= 0.0 &&
            Math.Abs(requestedGainDb) < Math.Abs(currentGainDb);

        if (thresholdRequiresImmediateReduction)
        {
            Gain_Linear = 1.0;
        }
        else
        {
            // Attack smooths increases in the dynamic EQ effect.
            // Release smooths reductions while the detector remains active.
            double coeff = movingAwayFromUnity
                ? AttackCoeff
                : ReleaseCoeff;

            coeff = double.IsFinite(coeff)
                ? Math.Clamp(coeff, 0.0, 1.0)
                : 0.0;

            Gain_Linear =
                coeff * Gain_Linear +
                (1.0 - coeff) * requestedGainLinear;
        }

        if (!double.IsFinite(Gain_Linear) || Gain_Linear <= 0.0)
            Gain_Linear = 1.0;

        // Publish the actual applied dynamic gain.
        GainApplied = Decibels.LinearToDecibels(Gain_Linear);

        // Update the biquad for the current dynamic gain.
        switch (Biquad_Type)
        {
            case BiquadType.PEQ:
                BiQuad.UpdateGain(GainApplied);
                break;

            case BiquadType.High_Shelf:
                BiQuad.UpdateGain_HighShelf(GainApplied);
                break;

            case BiquadType.Low_Shelf:
                BiQuad.UpdateGain_LowShelf(GainApplied);
                break;

            default:
                throw new NotSupportedException();
        }

        BiQuad.TransformInPlace(input, currentStream);
    }

    /// <summary>Attenuates when the measured level is above threshold.</summary>
    protected void CutAbove(
        Span<double> input,
        double amplitudeAtFrequency_Linear,
        DSP_Stream currentStream)
        => ApplyDynamicMode(
            input,
            amplitudeAtFrequency_Linear,
            currentStream,
            direction: +1,
            gainSign: -1);

    /// <summary>Attenuates when the measured level is below threshold.</summary>
    protected void CutBelow(
        Span<double> input,
        double amplitudeAtFrequency_Linear,
        DSP_Stream currentStream)
        => ApplyDynamicMode(
            input,
            amplitudeAtFrequency_Linear,
            currentStream,
            direction: -1,
            gainSign: -1);

    /// <summary>Boosts when the measured level is above threshold.</summary>
    protected void BoostAbove(
        Span<double> input,
        double amplitudeAtFrequency_Linear,
        DSP_Stream currentStream)
        => ApplyDynamicMode(
            input,
            amplitudeAtFrequency_Linear,
            currentStream,
            direction: +1,
            gainSign: +1);

    /// <summary>Boosts when the measured level is below threshold.</summary>
    protected void BoostBelow(
        Span<double> input,
        double amplitudeAtFrequency_Linear,
        DSP_Stream currentStream)
        => ApplyDynamicMode(
            input,
            amplitudeAtFrequency_Linear,
            currentStream,
            direction: -1,
            gainSign: +1);

    #endregion

    #region IFilter Interface

    public bool FilterEnabled { get; set; }

    public IFilter GetFilter => this;

    public FilterTypes FilterType { get; } = FilterTypes.DEQ;

    public FilterProcessingTypes FilterProcessingType { get; } =
        FilterProcessingTypes.WholeBlock;

    public IFilter DeepClone()
    {
        return CommonFunctions.DeepClone(this);
    }

    #endregion
}
