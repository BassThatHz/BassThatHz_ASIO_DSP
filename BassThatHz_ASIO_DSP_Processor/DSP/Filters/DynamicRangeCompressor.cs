
#nullable enable

namespace BassThatHz_ASIO_DSP_Processor;

#region Usings
using NAudio.Utils;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Xml.Serialization;
#endregion

/// <summary>
/// BassThatHz ASIO DSP Processor Engine
/// Copyright (c) 2026 BassThatHz
///
/// Permission is hereby granted to use this software
/// for educational purposes, scientific purposes or private purposes
/// or as part of an open-source community project,
/// and NOT for commercial use or resale in substantial part or whole
/// without prior authorization.
///
/// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND.
/// </summary>
[Serializable]
public class DynamicRangeCompressor : IFilter
{
    #region Variables

    protected double Gain_Linear = 1.0;
    protected double AttackCoeff;
    protected double ReleaseCoeff;

    [XmlIgnore]
    [IgnoreDataMember]
    public double CompressionApplied = 1.0;

    #endregion

    #region Public Properties

    /// <summary>
    /// Compression threshold in dBFS.
    /// </summary>
    public double Threshold_dB { get; set; } = -20.0;

    /// <summary>
    /// Compression ratio. For example, 4 means 4:1.
    /// </summary>
    public double Ratio { get; set; } = 4.0;

    /// <summary>
    /// Attack time in milliseconds.
    /// </summary>
    public double AttackTime_ms { get; set; } = 5.0;

    /// <summary>
    /// Release time in milliseconds.
    /// </summary>
    public double ReleaseTime_ms { get; set; } = 50.0;

    /// <summary>
    /// Soft-knee width in dB.
    /// A value of zero produces a hard knee.
    /// </summary>
    public double KneeWidth_dB { get; set; } = 0.0;

    /// <summary>
    /// Enables soft-knee compression when KneeWidth_dB is positive.
    /// </summary>
    public bool UseSoftKnee { get; set; } = true;

    #endregion

    #region Public Functions

    /// <summary>
    /// Calculates the exponential smoothing coefficients for the
    /// configured attack and release times.
    /// </summary>
    public void CalculateCoeffs(double sampleRate)
    {
        if (!double.IsFinite(sampleRate) || sampleRate <= 0.0)
        {
            AttackCoeff = 0.0;
            ReleaseCoeff = 0.0;
            return;
        }

        AttackCoeff = CalculateTimeCoefficient(
            sampleRate, AttackTime_ms);

        ReleaseCoeff = CalculateTimeCoefficient(
            sampleRate, ReleaseTime_ms);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static double CalculateTimeCoefficient(
        double sampleRate,
        double timeMs)
    {
        if (!double.IsFinite(timeMs) || timeMs <= 0.0)
            return 0.0;

        return Math.Exp(-1000.0 / (sampleRate * timeMs));
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization |
                 MethodImplOptions.AggressiveInlining)]
    public double[] Transform(
        double[] input,
        DSP_Stream currentStream)
    {
        TransformInPlace(input.AsSpan(), currentStream);
        return input;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization |
                 MethodImplOptions.AggressiveInlining)]
    public void TransformInPlace(
        Span<double> input,
        DSP_Stream currentStream)
    {
        if (input.IsEmpty)
            return;

        // Sanitize settings so invalid values cannot poison the
        // persistent gain state.
        double thresholdDb = double.IsFinite(Threshold_dB)
            ? Threshold_dB
            : -20.0;

        double ratio = double.IsFinite(Ratio)
            ? Math.Max(1.0, Ratio)
            : 1.0;

        double kneeDb = double.IsFinite(KneeWidth_dB)
            ? Math.Max(0.0, KneeWidth_dB)
            : 0.0;

        bool softKnee = UseSoftKnee && kneeDb > 0.0;

        // For a compressor, the slope of the gain-reduction curve
        // above threshold is (1 / ratio) - 1.
        //
        // Ratio 1:1 -> slope 0 (no compression).
        // Ratio 4:1 -> slope -0.75.
        double ratioSlope = (1.0 / ratio) - 1.0;

        double attack = Math.Clamp(
            double.IsFinite(AttackCoeff) ? AttackCoeff : 0.0,
            0.0, 1.0);

        double release = Math.Clamp(
            double.IsFinite(ReleaseCoeff) ? ReleaseCoeff : 0.0,
            0.0, 1.0);

        double gain = double.IsFinite(Gain_Linear)
            ? Math.Clamp(Gain_Linear, 0.0, 1.0)
            : 1.0;

        const double MinimumLinear = 1e-300;

        double halfKnee = kneeDb * 0.5;

        for (int i = 0; i < input.Length; i++)
        {
            double sample = input[i];

            // Preserve non-finite input rather than allowing it to
            // corrupt the compressor's persistent gain state.
            if (!double.IsFinite(sample))
                continue;

            double magnitude = Math.Abs(sample);

            // Convert amplitude to dBFS.
            double inputDb = 20.0 * Math.Log10(
                Math.Max(magnitude, MinimumLinear));

            double overThresholdDb = inputDb - thresholdDb;

            // Target gain reduction in dB.
            // Zero means no compression.
            double gainReductionDb = 0.0;

            if (softKnee)
            {
                if (overThresholdDb <= -halfKnee)
                {
                    // Below the knee: unity target gain.
                    gainReductionDb = 0.0;
                }
                else if (overThresholdDb < halfKnee)
                {
                    // Standard quadratic soft-knee curve.
                    //
                    // The curve transitions continuously from
                    // unity gain to the configured compression
                    // slope across the knee.
                    double kneePosition =
                        overThresholdDb + halfKnee;

                    gainReductionDb =
                        ratioSlope *
                        kneePosition * kneePosition /
                        (2.0 * kneeDb);
                }
                else
                {
                    // Above the knee: full compression ratio.
                    gainReductionDb =
                        ratioSlope * overThresholdDb;
                }
            }
            else if (overThresholdDb > 0.0)
            {
                // Hard knee: compression starts at threshold.
                gainReductionDb =
                    ratioSlope * overThresholdDb;
            }

            // Convert target gain reduction from dB to linear gain.
            double targetGain = Math.Pow(
                10.0, gainReductionDb / 20.0);

            targetGain = Math.Clamp(targetGain, 0.0, 1.0);

            // Attack when more gain reduction is required.
            // Release when less gain reduction is required.
            //
            // This must run for every sample, including samples
            // below threshold, to allow the gain to recover.
            double coefficient = targetGain < gain
                ? attack
                : release;

            gain =
                coefficient * gain +
                (1.0 - coefficient) * targetGain;

            gain = Math.Clamp(gain, 0.0, 1.0);

            // Apply compressor gain without an additional brickwall
            // output clamp. The configured ratio defines the
            // compression curve; it is not a hard limiter.
            input[i] = sample * gain;
        }

        // Preserve compressor state across processing blocks.
        Gain_Linear = gain;

        // Linear gain remaining at the end of this block.
        CompressionApplied = gain;
    }

    public void ResetSampleRate(int sampleRate)
    {
        CalculateCoeffs(sampleRate);
    }

    public void ApplySettings()
    {
        // Non-applicable.
    }

    #endregion

    #region IFilter Interface

    public bool FilterEnabled { get; set; }

    public IFilter GetFilter => this;

    public FilterTypes FilterType { get; } =
        FilterTypes.DynamicRangeCompressor;

    public FilterProcessingTypes FilterProcessingType { get; } =
        FilterProcessingTypes.WholeBlock;

    public IFilter DeepClone()
    {
        return CommonFunctions.DeepClone(this);
    }

    #endregion
}
