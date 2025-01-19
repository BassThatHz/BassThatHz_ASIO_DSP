#nullable enable

namespace BassThatHz_ASIO_DSP_Processor
{
    using NAudio.Utils;
    using System;
    using System.Runtime.CompilerServices;
    using WinRT;

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
    public class SmartGain : IFilter
    {
        #region Public Variables and Properties
        public bool PeakHold = false;
        public TimeSpan Duration = TimeSpan.FromMilliseconds(1000);

        public double PeakLevelLinear { get; protected set; } = 0;

        public double InputAbs { get; protected set; } = 0;

        public double HeadroomLinear { get; protected set; } = 0;

        public double ActualGainLinear { get; protected set; } = 0;

        public double ActualGaindB { get; protected set; } = 0;

        public double MaxAllowedLinearGain { get; protected set; } = 0;

        protected double _GaindB = 0;
        public double GaindB
        {
            get
            {
                return this._GaindB;
            }
            set
            {
                this.RequestedGainLinear = Decibels.DecibelsToLinear(value);
                this._GaindB = value;
            }
        }
        #endregion

        #region Protected Variables
        protected double RequestedGainLinear = 1;
        protected DateTime StartPeakDuration;
        #endregion

        #region Public Functions
        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
        public double[] Transform(double[] input, DSP_Stream currentStream)
        {
            for (int i = 0; i < input.Length; i++)
            {
                //Range of input is -1 to +1
                this.InputAbs = Math.Abs(input[i]);

                //Reset the peak level if the seek duration has expired and we aren't Peak-Holding
                if (!this.PeakHold &&
                    DateTime.Now - this.StartPeakDuration > this.Duration)
                {
                    //If the headroom is sufficient then allow the PeakLevel to be lowered over time
                    if (this.HeadroomLinear * 0.707d > this.RequestedGainLinear)
                        this.PeakLevelLinear *= 0.9999d;
                    this.StartPeakDuration = DateTime.Now;
                }

                //Record the Peak level
                if (this.InputAbs > this.PeakLevelLinear)
                {
                    this.PeakLevelLinear = this.InputAbs;
                    this.StartPeakDuration = DateTime.Now;
                }

                //Calculate how close to clipping the input has been/is
                this.MaxAllowedLinearGain = 1 / this.PeakLevelLinear;

                //Adjust the Actual Gain according to the input level and the Requested Gain
                if (this.RequestedGainLinear > this.MaxAllowedLinearGain) //Clip avoidance
                {
                    this.ActualGaindB = Decibels.LinearToDecibels(this.MaxAllowedLinearGain);
                    this.ActualGainLinear = this.MaxAllowedLinearGain;
                }
                else //won't clip
                {
                    this.ActualGaindB = this.GaindB;
                    this.ActualGainLinear = this.RequestedGainLinear;
                }

                //Apply the gain
                var Result = input[i] * this.ActualGainLinear;

                //Calculate the Headroom available
                var ReturnValueAbs = Math.Abs(Result);
                this.HeadroomLinear = 1 / ReturnValueAbs;

                //Ensure we haven't done anything stupid, i.e. ensure the output doesn't hard clip
                if (ReturnValueAbs >= 0.999d)
                    Result = Math.Sign(Result) * 0.707d;

                input[i] = Result;
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

        public FilterTypes FilterType { get; } = FilterTypes.SmartGain;
        public FilterProcessingTypes FilterProcessingType { get; } = FilterProcessingTypes.WholeBlock;
        #endregion

    }
}