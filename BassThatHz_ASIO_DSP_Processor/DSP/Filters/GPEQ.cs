#nullable enable

namespace BassThatHz_ASIO_DSP_Processor
{
    using System;
    using System.Collections.Generic;
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
    public class GPEQ : IFilter
    {
        #region Public Properties
        public List<IFilter> Filters { get; set; } = new();
        #endregion

        #region Public Functions
        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
        public double[] Transform(double[] input, DSP_Stream currentStream)
        {
            try
            {
                int FilterCount = this.Filters.Count;
                for (int i = 0; i < FilterCount; i++)
                    if (this.Filters[i] != null && this.Filters[i].FilterEnabled)
                        input = this.Filters[i].Transform(input, currentStream);
            }
            catch (Exception ex)
            {
                //We don't care if these two exceptions occur. It often happens because the user is 
                //deleting or adding filters while the DSP is on.
                if (ex is not IndexOutOfRangeException
                    && ex is not ArgumentOutOfRangeException
                    && ex is not NullReferenceException)
                    throw; //Throws all the remaining valid errors with stack trace info
            }
            return input;
        }

        public void ResetSampleRate(int sampleRate)
        {
            try
            {
                int FilterCount = this.Filters.Count;
                for (int i = 0; i < FilterCount; i++)
                    this.Filters[i]?.ResetSampleRate(sampleRate);
            }
            catch (NullReferenceException ex)
            {
                //Ignore errors, User probably deleted a filter while the DSP is running
                _ = ex;
            }
        }

        public void ApplySettings()
        {
            //Non-Applicable
        }
        #endregion

        #region IFilter Interface

        public bool FilterEnabled { get; set; }

        public IFilter GetFilter => this;

        public FilterTypes FilterType { get; } = FilterTypes.GPEQ;
        public FilterProcessingTypes FilterProcessingType { get; } = FilterProcessingTypes.WholeBlock;
        #endregion
    }
}