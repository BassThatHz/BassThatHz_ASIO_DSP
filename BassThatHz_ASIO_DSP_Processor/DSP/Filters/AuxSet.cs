#nullable enable

namespace BassThatHz_ASIO_DSP_Processor.DSP.Filters;

#region Usings
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
public class AuxSet : IFilter
{
    #region Public Properties
    public bool MuteAfter = false;
    public int AuxSetIndex = 0;
    #endregion

    #region public Functions
    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public double[] Transform(double[] input, DSP_Stream currentStream)
    {
        var NumberOfAuxBuffers = currentStream.NumberOfAuxBuffers;

        if (currentStream.AuxBuffer == null || currentStream.AuxBuffer.Length != NumberOfAuxBuffers ||
            currentStream.AuxBuffer.Any(innerArray => innerArray.Length != input.Length))
        {
            currentStream.AuxBuffer = new double[NumberOfAuxBuffers][]; 
            for (int i = 0; i < NumberOfAuxBuffers; i++)
                currentStream.AuxBuffer[i] = new double[input.Length];
        }

        Array.Copy(input, currentStream.AuxBuffer[this.AuxSetIndex], input.Length);            

        if (this.MuteAfter)
            Array.Clear(input);

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

    public FilterTypes FilterType { get; } = FilterTypes.AuxSet;
    public FilterProcessingTypes FilterProcessingType { get; } = FilterProcessingTypes.WholeBlock;

    public IFilter DeepClone()
    {
        return CommonFunctions.DeepClone(this);
    }
    #endregion
}
