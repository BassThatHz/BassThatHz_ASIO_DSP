#nullable enable

namespace BassThatHz_ASIO_DSP_Processor.DSP.Filters;

#region Usings
using NAudio.Utils;
using System;
using System.CodeDom;
using System.Runtime.CompilerServices;
using System.Text.Json;
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
public class AuxGet : IFilter
{
    #region Public Properties
    public bool MuteBefore = false;
    public int AuxGetIndex = 0;
    public double StreamAttenuation = -6.0d;
    public double AuxAttenuation = -6.051d;
    #endregion

    #region Public Functions
    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public double[] Transform(double[] input, DSP_Stream currentStream)
    {
        var AuxBuffers = currentStream.AuxBuffer;

        if (this.AuxGetIndex >= 0 && AuxBuffers != null &&
            this.AuxGetIndex < AuxBuffers.Length)
        {
            var AuxBuffer = AuxBuffers[this.AuxGetIndex];
            if (AuxBuffer != null && input.Length == AuxBuffer.Length)
            {
                if (this.MuteBefore)
                {
                    Array.Copy(AuxBuffer, input, input.Length);
                }
                else
                {
                    for (int i = 0; i < input.Length; i++)
                        input[i] = input[i] * Decibels.DecibelsToLinear(this.StreamAttenuation)
                                   +
                                   AuxBuffer[i] * Decibels.DecibelsToLinear(this.AuxAttenuation);
                }
            }
        }

        return input;
    }

    public void ApplySettings()
    {
        //Non-Applicable
    }

    public void ResetSampleRate(int sampleRate)
    {
        //Non-Applicable
        _ = sampleRate;
    }
    #endregion

    #region IFilter Interface

    public bool FilterEnabled { get; set; }

    public IFilter GetFilter => this;

    public FilterTypes FilterType { get; } = FilterTypes.AuxGet;
    public FilterProcessingTypes FilterProcessingType { get; } = FilterProcessingTypes.WholeBlock;

    public IFilter DeepClone()
    {
        return CommonFunctions.DeepClone(this);
    }
    #endregion
}