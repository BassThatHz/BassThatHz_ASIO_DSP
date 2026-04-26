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
public class ClassicLimiter : IFilter
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
        int len = input.Length;

        if (len == 0)
            return input;

        // Cache settings to locals to reduce repeated field access
        double thresholdDb = this.Threshold_dB;
        double kneeDb = this.KneeWidth_dB;
        bool useSoft = this.UseSoftKnee;
        double attackCoeff = this.AttackCoeff;
        double releaseCoeff = this.ReleaseCoeff;

        // Precompute threshold linear for lookahead comparison
        double thresholdLinear = Decibels.DecibelsToLinear(thresholdDb);

        // Rent a temporary buffer for absolute values and suffix max (look-ahead peak) to avoid O(n^2)
        var pool = System.Buffers.ArrayPool<double>.Shared;
        double[]? temp = null;
        try
        {
            temp = pool.Rent(len);

            // fill absolute values
            for (int i = 0; i < len; i++)
                temp[i] = Math.Abs(input[i]);

            // build suffix max in-place (temp becomes suffixMax)
            double runningMax = temp[len - 1];
            for (int i = len - 1; i >= 0; i--)
            {
                if (temp[i] > runningMax)
                    runningMax = temp[i];
                temp[i] = runningMax;
            }

            // main processing loop
            for (int i = 0; i < len; i++)
            {
                double absVal = Math.Abs(input[i]);
                double inputDb = Decibels.LinearToDecibels(absVal + 1e-99);

                double gainReductionLinear = 1.0;
                bool thresholdExceeded = false;

                if (useSoft && inputDb > thresholdDb - kneeDb * 0.5 && inputDb < thresholdDb + kneeDb * 0.5)
                {
                    thresholdExceeded = true;
                    double kneeStart = thresholdDb - kneeDb * 0.5;
                    double ratio = (inputDb - kneeStart) / kneeDb;
                    double adjustedThreshold = kneeStart + ratio * kneeDb;
                    gainReductionLinear = Decibels.DecibelsToLinear(adjustedThreshold - inputDb);
                }
                else if (inputDb > thresholdDb)
                {
                    thresholdExceeded = true;
                    gainReductionLinear = Decibels.DecibelsToLinear(thresholdDb - inputDb);

                    // look-ahead peak using suffix max
                    double peakValue = temp[i];
                    if (peakValue > thresholdLinear)
                    {
                        gainReductionLinear = Math.Max(gainReductionLinear, peakValue - thresholdLinear);
                    }
                }

                if (thresholdExceeded)
                {
                    // smoothing
                    if (gainReductionLinear < this.Gain_Linear)
                        this.Gain_Linear = attackCoeff * (this.Gain_Linear - gainReductionLinear) + gainReductionLinear;
                    else
                        this.Gain_Linear = releaseCoeff * (this.Gain_Linear - gainReductionLinear) + gainReductionLinear;

                    this.CompressionApplied = this.Gain_Linear;
                    input[i] = Math.Min(1 - double.Epsilon, input[i] * this.Gain_Linear);
                }
            }
        }
        finally
        {
            if (temp != null)
                pool.Return(temp, clearArray: false);
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

    public FilterTypes FilterType { get; } = FilterTypes.ClassicLimiter
;
    public FilterProcessingTypes FilterProcessingType { get; } = FilterProcessingTypes.WholeBlock;

    public IFilter DeepClone()
    {
        return CommonFunctions.DeepClone(this);
    }
    #endregion

}