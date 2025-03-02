#nullable enable

namespace BassThatHz_ASIO_DSP_Processor;

#region Usings
using NAudio.Utils;
using System;
using System.Linq;
using System.Runtime.CompilerServices;
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

        // Iterate over each sample in the input block
        foreach (var i in Enumerable.Range(0, input.Length))
        {
            double currentSample = input[i];
            double returnValue = currentSample;

            // Detection Logic - checks if the sample is within the floor range
            bool isInFloorRange = currentSample > 0 && currentSample < MinValue || currentSample < 0 && currentSample > -MinValue;

            // Update detection timestamp if sample is within floor range
            if (isInFloorRange)
            {
                this.LastDetection = DateTime.Now;
                this.IsDetected = true;
            }

            // Initialization Logic - activate the filter on first detection
            if (this.IsDetected && !this.IsActive)
            {
                this.StartTime = DateTime.Now;
                this.IsActive = true;
            }

            // Update total active duration
            this.CurrentTotalDuration = DateTime.Now - this.StartTime;

            // DSP (based on Attack/Hold/Release logic)
            // Apply compression if active and within hold duration
            if (this.IsActive && DateTime.Now - this.LastDetection < this.HoldInMS)
            {
                if (this.IsDetected) // Only process applicable samples
                {
                    // Calculate the amount by which the input exceeds the threshold in dB
                    var excessDB = Math.Abs(Decibels.LinearToDecibels(DC_OFFSET + this.MinValue / Math.Abs(currentSample)));
                    // Calculate compression amount in dB (negative value)
                    var compressionRatioDB = -excessDB * (this.Ratio - 1) / this.Ratio;
                    // Convert compression dB to a linear scale
                    var inputCompressionAmount = Decibels.DecibelsToLinear(compressionRatioDB) - DC_OFFSET;
                    // Apply compression to the input sample
                    returnValue = currentSample * (double)inputCompressionAmount;
                }
            }
            else
            {
                // Reset active state if hold duration expired
                this.IsActive = false;
            }

            // Reset detection state for next sample
            this.IsDetected = false;

            // Store processed sample back in input array
            input[i] = returnValue;
        }

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