#nullable enable

namespace BassThatHz_ASIO_DSP_Processor
{
    using NAudio.Utils;
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Runtime.Serialization;

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
    public class Mixer : IFilter
    {
        #region Variables
        public List<MixerInput> MixerInputs = new();
        #endregion

        #region Public Functions
        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
        public double[] Transform(double[] input, DSP_Stream currentStream)
        {
            //Range of input is -1 to +1
            try
            {
                for (int i = 0; i < this.MixerInputs.Count; i++)
                {
                    var MixerItem = this.MixerInputs[i];
                    if (MixerItem != null && MixerItem.Enabled)
                    {
                        for (int j = 0; j < input.Length; j++)
                        {
                            input[j] = input[j] 
                                       * 
                                       Decibels.DecibelsToLinear(MixerItem.StreamAttenuation)
                                       +
                                       Program.ASIO.InputBuffer[MixerItem.ChannelIndex][j] 
                                       * 
                                       Decibels.DecibelsToLinear(MixerItem.Attenuation);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _ = ex;
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

        public FilterTypes FilterType { get; } = FilterTypes.Mixer;
        public FilterProcessingTypes FilterProcessingType { get; } = FilterProcessingTypes.WholeBlock;
        #endregion
    }

    [Serializable]
    public class MixerInput
    {
        public bool Enabled;
        public double Attenuation = -6.051d;
        public double StreamAttenuation = -6.000d;
        public int ChannelIndex;
        [IgnoreDataMember]
        public string ChannelName = string.Empty;
    }
}