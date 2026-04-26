#nullable enable

namespace BassThatHz_ASIO_DSP_Processor;

#region Usings
using NAudio.Utils;
using System;
using System.Buffers;
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
        // Forward to Span-based in-place implementation for zero-allocation processing
        TransformInPlace(input.AsSpan(), currentStream);
        return input;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public void TransformInPlace(Span<double> input, DSP_Stream currentStream)
    {
        int len = input.Length;
        if (len == 0)
            return;

        double inverseRatioFactor = 10D / this.Ratio;

        // Cache locals
        double attack = this.AttackCoeff;
        double release = this.ReleaseCoeff;
        double thresholdDb = this.Threshold_dB;
        double kneeDb = this.KneeWidth_dB;
        bool useSoft = this.UseSoftKnee;

        // Use stackalloc for small buffers to avoid renting
        const int StackallocLimit = 512;
        double[]? rented = null;
        Span<double> tmpSpan = default;
        if (len > StackallocLimit)
        {
            rented = ArrayPool<double>.Shared.Rent(len);
            tmpSpan = rented.AsSpan(0, len);
        }

        // Two code paths: small buffers use stackalloc, large buffers use ArrayPool
        double gainLinearLocal = this.Gain_Linear;
        double thresholdLinear = Decibels.DecibelsToLinear(thresholdDb);

        if (len <= StackallocLimit)
        {
            Span<double> tmp = stackalloc double[StackallocLimit];
            tmp = tmp.Slice(0, len);

            for (int i = 0; i < len; i++)
                tmp[i] = Math.Abs(input[i]);

            double runningMax = tmp[len - 1];
            for (int i = len - 1; i >= 0; i--)
            {
                if (tmp[i] > runningMax) runningMax = tmp[i];
                tmp[i] = runningMax;
            }

            for (int i = 0; i < len; i++)
            {
                double peakSuffix = tmp[i];
                double instAbs = Math.Abs(input[i]);
                double inputDb = Decibels.LinearToDecibels(instAbs + 1e-99);

                double gainReductionLinear = 0;
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
                    double desiredReductionDb = thresholdDb + (inputDb - thresholdDb) * inverseRatioFactor;
                    gainReductionLinear = Decibels.DecibelsToLinear(desiredReductionDb - inputDb);
                    if (peakSuffix > thresholdLinear)
                        gainReductionLinear = Math.Max(gainReductionLinear, peakSuffix - thresholdLinear);
                }

                if (thresholdExceeded)
                {
                    if (gainReductionLinear < gainLinearLocal)
                        gainLinearLocal = attack * (gainLinearLocal - gainReductionLinear) + gainReductionLinear;
                    else
                        gainLinearLocal = release * (gainLinearLocal - gainReductionLinear) + gainReductionLinear;

                    input[i] *= gainLinearLocal;
                }
            }
        }
        else
        {
            try
            {
                for (int i = 0; i < len; i++)
                    tmpSpan[i] = Math.Abs(input[i]);

                double runningMax = tmpSpan[len - 1];
                for (int i = len - 1; i >= 0; i--)
                {
                    if (tmpSpan[i] > runningMax) runningMax = tmpSpan[i];
                    tmpSpan[i] = runningMax;
                }

                for (int i = 0; i < len; i++)
                {
                    double peakSuffix = tmpSpan[i];
                    double instAbs = Math.Abs(input[i]);
                    double inputDb = Decibels.LinearToDecibels(instAbs + 1e-99);

                    double gainReductionLinear = 0;
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
                        double desiredReductionDb = thresholdDb + (inputDb - thresholdDb) * inverseRatioFactor;
                        gainReductionLinear = Decibels.DecibelsToLinear(desiredReductionDb - inputDb);
                        if (peakSuffix > thresholdLinear)
                            gainReductionLinear = Math.Max(gainReductionLinear, peakSuffix - thresholdLinear);
                    }

                    if (thresholdExceeded)
                    {
                        if (gainReductionLinear < gainLinearLocal)
                            gainLinearLocal = attack * (gainLinearLocal - gainReductionLinear) + gainReductionLinear;
                        else
                            gainLinearLocal = release * (gainLinearLocal - gainReductionLinear) + gainReductionLinear;

                        input[i] *= gainLinearLocal;
                    }
                }
            }
            finally
            {
                if (rented != null)
                    ArrayPool<double>.Shared.Return(rented, clearArray: false);
            }
        }

        this.Gain_Linear = gainLinearLocal;
        this.CompressionApplied = gainLinearLocal;
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

    public IFilter DeepClone()
    {
        return CommonFunctions.DeepClone(this);
    }
    #endregion

}