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
public class Floor : IFilter
{
    #region Variables
    public double MinValue = 0;
    public TimeSpan HoldInMS = TimeSpan.FromMilliseconds(100);
    public double Ratio = 2.0d;

    // DC offset to prevent log(0) and avoid division by zero
    protected const double DC_OFFSET = 1.0E-25d;

    protected DateTime LastDetectionTime;
    protected bool IsGateClosed = false;
    #endregion

    #region Public Functions
    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public double[] Transform(double[] input, DSP_Stream currentStream)
    {
        // Range of input is -1 to +1
        int len = input.Length;
        if (len == 0)
            return input;

        // Cache frequently used fields to locals to avoid repeated field access
        double minValue = this.MinValue;
        double ratio = this.Ratio;
        TimeSpan hold = this.HoldInMS;

        // Use local state variables and write back at the end
        DateTime lastDetectionTime = this.LastDetectionTime;
        bool isGateClosed = this.IsGateClosed;

        for (int i = 0; i < len; i++)
        {
            double currentSample = input[i];
            double outSample = currentSample;

            // Compute now once per sample
            DateTime now = DateTime.UtcNow;

            // Detection: sample inside floor range (exclude exact zero)
            bool detected = (currentSample != 0.0) && (Math.Abs(currentSample) < minValue);

            // If signal is below threshold, close the gate
            if (detected)
            {
                lastDetectionTime = now;
                if (!isGateClosed)
                {
                    isGateClosed = true;
                }
            }
            else
            {
                // Signal above threshold — check release time to re-open gate
                if (isGateClosed && (now - lastDetectionTime) >= hold)
                {
                    isGateClosed = false;
                }
            }

            // Apply floor attenuation when gate is closed
            if (isGateClosed)
            {
                double absSample = Math.Abs(currentSample);
                // Compute how far below the floor in dB
                double excessDB = Math.Abs(Decibels.LinearToDecibels(DC_OFFSET + minValue / absSample));

                // Attenuation factor: higher ratio = more aggressive attenuation
                // gain = 10^(-excessDB * (ratio - 1) / (20 * ratio))
                double attenuationFactor = Math.Pow(10.0, -(excessDB * (ratio - 1.0) / (20.0 * ratio)));

                outSample *= attenuationFactor;
            }

            input[i] = outSample;
        }

        // Write back state
        this.LastDetectionTime = lastDetectionTime;
        this.IsGateClosed = isGateClosed;

        return input;
    }

    public void ResetSampleRate(int sampleRate)
    {
        // Non-Applicable
    }

    public void ApplySettings()
    {
        // Non-Applicable
    }
    #endregion

    #region IFilter Interface
    public bool FilterEnabled { get; set; }

    public IFilter GetFilter => this;

    public FilterTypes FilterType { get; } = FilterTypes.Floor;
    public FilterProcessingTypes FilterProcessingType { get; } = FilterProcessingTypes.WholeBlock;

    public IFilter DeepClone()
    {
        return CommonFunctions.DeepClone(this);
    }
    #endregion

}