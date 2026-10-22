
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
/// Permission is hereby granted to use this software and associated
/// documentation files (the "Software"), for educational, scientific,
/// or private purposes, or as part of an open-source community project,
/// and NOT for commercial use or resale in substantial part or whole
/// without prior authorization.
///
/// The copyright notice and this permission notice shall be included
/// in all copies or substantial portions of the Software.
///
/// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
/// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO FITNESS FOR A
/// PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
/// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR
/// OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE,
/// ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE
/// OR OTHER DEALINGS IN THE SOFTWARE. ENFORCEABLE PORTIONS SHALL
/// REMAIN IF NOT FOUND CONTRARY UNDER LAW.
/// </summary>
[Serializable]
public class ClassicLimiter : IFilter
{
    #region Variables

    protected double Gain_Linear = 1.0;

    protected double AttackCoeff;
    protected double ReleaseCoeff;

    [XmlIgnore]
    [IgnoreDataMember]
    public double CompressionApplied = 1.0;

    /// <summary>
    /// Largest representable double below 1.0.
    /// This is a numerical full-scale safety bound, not a configurable
    /// dBFS limiter ceiling.
    /// </summary>
    private static readonly double OutputCeiling =
        Math.BitDecrement(1.0);

    #endregion

    #region Public Properties

    /// <summary>
    /// Compression threshold in dBFS.
    /// </summary>
    public double Threshold_dB { get; set; } = -20.0;

    /// <summary>
    /// Compression ratio. A value of 4 means 4:1.
    /// Values must be greater than or equal to 1.
    /// </summary>
    public double Ratio { get; set; } = 4.0;

    /// <summary>
    /// Attack time in milliseconds.
    /// Zero means instantaneous attack.
    /// </summary>
    public double AttackTime_ms { get; set; } = 5.0;

    /// <summary>
    /// Release time in milliseconds.
    /// Zero means instantaneous release.
    /// </summary>
    public double ReleaseTime_ms { get; set; } = 50.0;

    /// <summary>
    /// Soft-knee width in dB.
    /// Zero means a hard knee.
    /// </summary>
    public double KneeWidth_dB { get; set; } = 0.0;

    /// <summary>
    /// Enables the quadratic soft-knee compression curve.
    /// </summary>
    public bool UseSoftKnee { get; set; } = true;

    #endregion

    #region Public Functions

    /// <summary>
    /// Calculates the exponential attack and release coefficients.
    /// Call this after setting the sample rate or changing timing settings.
    /// </summary>
    public void CalculateCoeffs(double sampleRate)
    {
        if (!double.IsFinite(sampleRate) || sampleRate <= 0.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sampleRate),
                "Sample rate must be finite and greater than zero.");
        }

        if (!double.IsFinite(AttackTime_ms) || AttackTime_ms < 0.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(AttackTime_ms),
                "Attack time must be finite and non-negative.");
        }

        if (!double.IsFinite(ReleaseTime_ms) || ReleaseTime_ms < 0.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(ReleaseTime_ms),
                "Release time must be finite and non-negative.");
        }

        // a = exp(-1 / (sampleRate * timeInSeconds))
        //
        // Larger coefficients produce slower envelope movement.
        // A zero time constant produces immediate response.

        AttackCoeff = AttackTime_ms == 0.0
            ? 0.0
            : Math.Exp(
                -1000.0 / (sampleRate * AttackTime_ms));

        ReleaseCoeff = ReleaseTime_ms == 0.0
            ? 0.0
            : Math.Exp(
                -1000.0 / (sampleRate * ReleaseTime_ms));
    }

    /// <summary>
    /// Processes audio in place using a classic compressor envelope.
    ///
    /// This implementation uses sample-by-sample envelope smoothing.
    /// It does not implement a delayed look-ahead audio path.
    /// </summary>
    [MethodImpl(
        MethodImplOptions.AggressiveOptimization |
        MethodImplOptions.AggressiveInlining)]
    public double[] Transform(
        double[] input,
        DSP_Stream currentStream)
    {
        ArgumentNullException.ThrowIfNull(input);

        int len = input.Length;

        if (len == 0)
            return input;

        // Cache settings locally for the processing loop.

        double thresholdDb = Threshold_dB;
        double ratio = Ratio;

        double kneeDb = UseSoftKnee
            ? KneeWidth_dB
            : 0.0;

        double attackCoeff = AttackCoeff;
        double releaseCoeff = ReleaseCoeff;

        // Validate parameters.

        if (!double.IsFinite(thresholdDb))
        {
            throw new ArgumentOutOfRangeException(
                nameof(Threshold_dB));
        }

        if (!double.IsFinite(ratio) || ratio < 1.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(Ratio),
                "Ratio must be finite and greater than or equal to 1.");
        }

        if (!double.IsFinite(kneeDb) || kneeDb < 0.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(KneeWidth_dB),
                "Knee width must be finite and non-negative.");
        }

        if (!double.IsFinite(attackCoeff) ||
            attackCoeff < 0.0 || attackCoeff > 1.0)
        {
            throw new InvalidOperationException(
                "Attack coefficient is invalid. Call CalculateCoeffs().");
        }

        if (!double.IsFinite(releaseCoeff) ||
            releaseCoeff < 0.0 || releaseCoeff > 1.0)
        {
            throw new InvalidOperationException(
                "Release coefficient is invalid. Call CalculateCoeffs().");
        }

        double inverseRatio = 1.0 / ratio;

        double halfKnee = kneeDb * 0.5;

        double lowerKneeDb = thresholdDb - halfKnee;
        double upperKneeDb = thresholdDb + halfKnee;

        double gain = Math.Clamp(Gain_Linear, 0.0, 1.0);

        // Process every sample, including samples below threshold.
        // This is essential for correct release behavior.

        for (int i = 0; i < len; i++)
        {
            double sample = input[i];

            // Do not propagate NaN or infinity into the DSP output.

            if (!double.IsFinite(sample))
            {
                input[i] = 0.0;
                continue;
            }

            double absSample = Math.Abs(sample);

            // Convert the absolute input amplitude to dBFS.
            // Zero amplitude corresponds to negative infinity.

            double inputDb = absSample > 0.0
                ? 20.0 * Math.Log10(absSample)
                : double.NegativeInfinity;

            double gainReductionDb = 0.0;

            if (absSample > 0.0)
            {
                if (kneeDb > 0.0)
                {
                    // Standard quadratic soft-knee curve.
                    //
                    // Below the lower knee: no compression.
                    // Inside the knee: gradual compression.
                    // Above the upper knee: full compression ratio.

                    if (inputDb > lowerKneeDb)
                    {
                        if (inputDb < upperKneeDb)
                        {
                            double kneeOffset =
                                inputDb - lowerKneeDb;

                            gainReductionDb =
                                (inverseRatio - 1.0) *
                                kneeOffset *
                                kneeOffset /
                                (2.0 * kneeDb);
                        }
                        else
                        {
                            double overThresholdDb =
                                inputDb - thresholdDb;

                            gainReductionDb =
                                (inverseRatio - 1.0) *
                                overThresholdDb;
                        }
                    }
                }
                else
                {
                    // Hard-knee compression.

                    if (inputDb > thresholdDb)
                    {
                        double overThresholdDb =
                            inputDb - thresholdDb;

                        gainReductionDb =
                            (inverseRatio - 1.0) *
                            overThresholdDb;
                    }
                }
            }

            // Convert the desired gain reduction from dB to a
            // linear amplitude multiplier.
            //
            // Negative gainReductionDb means attenuation.

            double targetGain = gainReductionDb < 0.0
                ? Math.Pow(10.0, gainReductionDb / 20.0)
                : 1.0;

            targetGain = Math.Clamp(targetGain, 0.0, 1.0);

            // Attack when more attenuation is required.
            // Release when attenuation can be reduced.
            //
            // The envelope must update on EVERY sample, including
            // samples below threshold.

            double coeff = targetGain < gain
                ? attackCoeff
                : releaseCoeff;

            gain =
                coeff * gain +
                (1.0 - coeff) * targetGain;

            gain = Math.Clamp(gain, 0.0, 1.0);

            // Apply the envelope continuously. Do not bypass the
            // gain merely because the current sample is below
            // threshold; doing so would defeat release smoothing.

            double output = sample * gain;

            // Numerical full-scale safety clamp.
            //
            // This is NOT a true brick-wall limiter with look-ahead.

            input[i] = Math.Clamp(
                output,
                -OutputCeiling,
                OutputCeiling);
        }

        // Persist the envelope across processing blocks.

        Gain_Linear = gain;
        CompressionApplied = gain;

        return input;
    }

    /// <summary>
    /// Updates coefficients when the sample rate changes.
    /// </summary>
    public void ResetSampleRate(int sampleRate)
    {
        CalculateCoeffs(sampleRate);
    }

    /// <summary>
    /// No additional settings application is required.
    /// </summary>
    public void ApplySettings()
    {
        // Non-applicable.
    }

    #endregion

    #region IFilter Interface

    public bool FilterEnabled { get; set; }

    public IFilter GetFilter => this;

    public FilterTypes FilterType { get; } =
        FilterTypes.ClassicLimiter;

    public FilterProcessingTypes FilterProcessingType { get; } =
        FilterProcessingTypes.WholeBlock;

    public IFilter DeepClone()
    {
        return CommonFunctions.DeepClone(this);
    }

    #endregion
}
