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
public class DynamicRangeCompressor : IFilter
{
    #region Variables
    protected double Gain_Linear = 1.0;
    protected double AttackCoeff;
    protected double ReleaseCoeff;
    [XmlIgnoreAttribute]
    [IgnoreDataMember]
    public double CompressionApplied = 1;
    #endregion

    #region Public Properties 
    //Threshold is the level above which compression starts, in decibels (dB).
    //Ratio determines the amount of gain reduction. For instance,
    //a 4:1 ratio means that if the input level is 4 dB over the threshold, the output level will be 1 dB over the threshold.
    //AttackTime and ReleaseTime control how quickly the compressor responds to changes in the input level.
    public double Threshold_dB { get; set; } = -20; //-20db
    public double Ratio { get; set; } = 24; // e.g. 10:1
    public double AttackTime_ms { get; set; } = 99;
    public double ReleaseTime_ms { get; set; } = 1;

    /// <summary>
    /// The width of the knee area around the threshold in decibels (dB).
    /// </summary>
    /// <remarks>
    /// KneeWidth_dB specifies the range over which the compressor transitions from no compression to the full compression ratio.
    /// A wider knee width results in a more gradual increase in compression, leading to a smoother transition as the signal level crosses the threshold.
    /// A knee width of 0 dB implies a 'hard knee', where compression is applied abruptly as soon as the signal exceeds the threshold.
    /// A larger knee width implies a 'soft knee', where the onset of compression is more gradual, providing a more natural and less aggressive compression effect.
    /// This property is important for tailoring the compressor's response to different types of audio material and desired compression characteristics.
    /// </remarks>
    public double KneeWidth_dB { get; set; } = 24;
    public bool UseSoftKnee { get; set; } = true;
    #endregion

    #region Public Functions
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

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public double[] Transform(double[] input, DSP_Stream currentStream)
    {
        double InverseRatioFactor = 10D / this.Ratio;

        for (int i = 0; i < input.Length; i++)
        {
            double GainReduction_Linear = 0;
            bool ThresholdExceeded = false;
            // A small value (1e-99) is added to avoid taking the logarithm of zero.
            double Input_dB = Decibels.LinearToDecibels(Math.Abs(input[i]) + 1e-99);

            // Check if soft knee is enabled and if the input is within the knee region.
            if (this.UseSoftKnee && Input_dB > this.Threshold_dB - this.KneeWidth_dB * 0.5D 
                                 && Input_dB < this.Threshold_dB + this.KneeWidth_dB * 0.5D)
            {
                ThresholdExceeded = true;
                // Calculate the start and end points of the knee region in dB.
                double KneeStart_dB = this.Threshold_dB - this.KneeWidth_dB * 0.5D;
                double KneeEnd_dB = this.Threshold_dB + this.KneeWidth_dB * 0.5D;

                // Calculate the ratio of how far the input is into the knee region.
                double Ratio = (Input_dB - KneeStart_dB) / this.KneeWidth_dB;

                // Calculate an adjusted threshold based on the ratio,
                // creating a smooth transition through the knee region.
                double AdjustedThreshold_dB = KneeStart_dB + Ratio * (KneeEnd_dB - KneeStart_dB);

                // Calculate the gain reduction factor for this adjusted threshold.
                GainReduction_Linear = Decibels.DecibelsToLinear(AdjustedThreshold_dB - Input_dB);
            }
            // If soft knee is not used or input is above the knee region.
            else if (Input_dB > this.Threshold_dB)
            {
                ThresholdExceeded = true;
                // Calculate the desired reduction in dB based on the compressor's ratio.
                double DesiredReduction_dB = this.Threshold_dB + (Input_dB - this.Threshold_dB) * InverseRatioFactor;

                // Calculate the gain reduction factor needed to achieve this reduction.
                GainReduction_Linear = Decibels.DecibelsToLinear(DesiredReduction_dB - Input_dB);

                // Look-ahead processing
                double PeakValue = 0;
                //Calculate the Peak Amplitude look-ahead
                for (int j = i; j < input.Length; j++)
                    if (Math.Abs(input[j]) > PeakValue)
                        PeakValue = Math.Abs(input[j]);

                var Threshold_Linear = Decibels.DecibelsToLinear(this.Threshold_dB);
                if (PeakValue > Threshold_Linear)
                    GainReduction_Linear = Math.Max(GainReduction_Linear, PeakValue - Threshold_Linear);
            }

            if (ThresholdExceeded)
            {
                // Smoothing the gain changes
                // This part of the code ensures that changes in gain (compression level) are applied smoothly over time,
                // rather than abruptly, to avoid unnatural sounding artifacts.
                if (GainReduction_Linear < this.Gain_Linear)
                {
                    // Attack phase: If the calculated gain reduction is less than the current gain level (Gain_Linear),
                    // it indicates that the signal level has increased and more compression is needed.
                    // The gain is reduced (i.e., more compression is applied) using the attack coefficient.
                    // This creates a smooth transition during the attack phase of the compression.
                    this.Gain_Linear = this.AttackCoeff * (this.Gain_Linear - GainReduction_Linear) + GainReduction_Linear;
                }
                else
                {
                    // Release phase: If the calculated gain reduction is greater than the current gain level (Gain_Linear),
                    // it indicates that the signal level has decreased and less compression is needed.
                    // The gain is increased (i.e., less compression is applied) using the release coefficient.
                    // This creates a smooth transition during the release phase of the compression.
                    this.Gain_Linear = this.ReleaseCoeff * (this.Gain_Linear - GainReduction_Linear) + GainReduction_Linear;
                }
                this.CompressionApplied = this.Gain_Linear;
                input[i] *= this.Gain_Linear;
            }
        }
        return input;
    }

    public void ResetSampleRate(int sampleRate)
    {
        this.CalculateCoeffs(sampleRate);
    }

    public void ApplySettings()
    {
        //Non-Applicable
    }
    #endregion

    #region IFilter Interface

    public bool FilterEnabled { get; set; }

    public IFilter GetFilter => this;

    public FilterTypes FilterType { get; } = FilterTypes.DynamicRangeCompressor
;
    public FilterProcessingTypes FilterProcessingType { get; } = FilterProcessingTypes.WholeBlock;
    #endregion

}