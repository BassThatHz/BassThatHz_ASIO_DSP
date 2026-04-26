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
    public TimeSpan HoldInMS = TimeSpan.FromMilliseconds(1000);
    public double Ratio = 1.1d;

    // DC offset to prevent log(0)
    protected const double DC_OFFSET = 1.0E-25d;

    protected TimeSpan CurrentTotalDuration;
    protected DateTime StartTime;
    protected DateTime LastDetection;
    protected bool IsActive = false;
    protected bool IsDetected = false;
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
        DateTime lastDetection = this.LastDetection;
        DateTime startTime = this.StartTime;
        bool isActive = this.IsActive;

        for (int i = 0; i < len; i++)
        {
            double currentSample = input[i];
            double outSample = currentSample;

            // compute now once per sample
            DateTime now = DateTime.UtcNow;

            // Detection: sample inside floor range (exclude exact zero)
            bool detected = (currentSample != 0.0) && (Math.Abs(currentSample) < minValue);
            if (detected)
            {
                lastDetection = now;
            }

            // Activate on first detection
            if (detected && !isActive)
            {
                startTime = now;
                isActive = true;
            }

            // Only process when active and within hold window
            if (isActive && (now - lastDetection) < hold)
            {
                if (detected)
                {
                    // Compute compression based on distance from floor
                    double absSample = Math.Abs(currentSample);
                    // avoid division by zero; DC_OFFSET guards
                    double excessDB = Math.Abs(Decibels.LinearToDecibels(DC_OFFSET + minValue / absSample));
                    double compressionRatioDB = -excessDB * (ratio - 1.0) / ratio;
                    double inputCompressionAmount = Decibels.DecibelsToLinear(compressionRatioDB) - DC_OFFSET;
                    outSample = currentSample * inputCompressionAmount;
                }
            }
            else
            {
                isActive = false;
            }

            input[i] = outSample;
        }

        // Write back state
        this.LastDetection = lastDetection;
        this.StartTime = startTime;
        this.IsActive = isActive;
        this.CurrentTotalDuration = isActive ? (DateTime.UtcNow - startTime) : TimeSpan.Zero;

        return input;
    }

    public void ResetSampleRate(int sampleRate)
    {
        //Non Applicable
    }

    public void ApplySettings()
    {
        //Non-Applicable
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