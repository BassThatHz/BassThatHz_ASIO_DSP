#nullable enable

namespace BassThatHz_ASIO_DSP_Processor
{
    using System;
    using System.Runtime.CompilerServices;

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
    public class Delay : IFilter
    {
        #region Protected Variables       
        protected object ResizeBufferLockObject = new();
        protected double[]? DelayBuffer;
        protected int DelayBufferLength = 0;

        protected int ReadIndex = 0;
        protected int WriteIndex = 0;

        protected int BufferSize = 0;
        protected int SampleRate = 0;

        protected int Delay_InSamples = 0;
        #endregion

        #region Public Properties
        protected decimal _DelayInMS = 0;
        public decimal DelayInMS
        {
            get
            {
                return this._DelayInMS;
            }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException("delayInMS cannot be negative");

                this._DelayInMS = value;
                this.Reset_DelayAndBufferSize();
            }

        }
        #endregion

        #region Public Functions
        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
        public double[] Transform(double[] input, DSP_Stream currentStream)
        {
            for (int i = 0; i < input.Length; i++)
            {
                double Result = input[i];
                if (this.DelayBuffer != null)
                {
                    if (this.ReadIndex >= this.DelayBufferLength)
                        this.ReadIndex = 0;

                    //WriteIndex starts with an index offset of Delay_InSamples, but then wraps around to 0
                    //i.e. it is always ahead of the ReadIndex by that many samples
                    if (this.WriteIndex >= this.DelayBufferLength)
                        this.WriteIndex = 0;

                    try
                    {
                        //Save the current input in the delayed position
                        this.DelayBuffer[this.WriteIndex] = input[i];
                        //Read the current value from the (now delayed) read position
                        Result = this.DelayBuffer[this.ReadIndex];

                        //Increment the indexes
                        this.WriteIndex++;
                        this.ReadIndex++;
                    }
                    catch (Exception ex)
                    {
                        //This might happen if the user has changed the
                        //sample rate or buffer size while the DSP is running.
                        //i.e. a very rare case.
                        //A lock and bounds checks are not performed for performance reasons.
                        //The indexes are reset to 0 by the ResizeBuffer function in this case.
                        //You can set a breakpoint here to debug it if you want/need
                        _ = ex;
                    }
                }
                input[i] = Result;
            }
            return input;
        }

        public void ResetSampleRate(int sampleRate)
        {
            if (sampleRate < 0)
                throw new ArgumentOutOfRangeException("sampleRate cannot be negative");

            this.SampleRate = sampleRate;
            this.Reset_DelayAndBufferSize();
        }

        public void ResetBufferSize(int bufferSize)
        {
            if (bufferSize < 0)
                throw new ArgumentOutOfRangeException("bufferSize cannot be negative");

            this.BufferSize = bufferSize;
            this.ResizeBuffer();
        }

        public void Initialize(decimal delayInMS, int bufferSize, int sampleRate)
        {
            if (bufferSize < 0)
                throw new ArgumentOutOfRangeException("bufferSize cannot be negative");

            if (sampleRate < 0)
                throw new ArgumentOutOfRangeException("sampleRate cannot be negative");

            if (delayInMS < 0)
                throw new ArgumentOutOfRangeException("delayInMS cannot be negative");

            this.SampleRate = sampleRate;
            this.DelayInMS = delayInMS;
            this.BufferSize = bufferSize;

            this.Reset_DelayAndBufferSize();
        }

        public void ApplySettings()
        {
            //Non-Applicable
        }
        #endregion

        #region Protected Functions

        protected void Reset_DelayAndBufferSize()
        {
            //Rounds the delay to the nearest sample
            this.Delay_InSamples = (int)(this.SampleRate / 1000 * this.DelayInMS);

            this.ResizeBuffer();
        }

        protected void ResizeBuffer()
        {
            lock (this.ResizeBufferLockObject) //The array is only resized via one thread
            {
                this.DelayBufferLength = this.Delay_InSamples + this.BufferSize;
                this.DelayBuffer = new double[this.DelayBufferLength];

                //Zero the array
                Array.Clear(this.DelayBuffer, 0, this.DelayBufferLength); 

                //Reset the Indexes
                this.ReadIndex = 0;
                //The index of the first intial sample that was delayed
                this.WriteIndex = this.Delay_InSamples;
            }
        }
        #endregion

        #region IFilter Interface

        public bool FilterEnabled { get; set; }
        
        public IFilter GetFilter => this;
        
        public FilterTypes FilterType { get; } = FilterTypes.Delay;
        public FilterProcessingTypes FilterProcessingType { get; } = FilterProcessingTypes.WholeBlock;
        #endregion
    }
}