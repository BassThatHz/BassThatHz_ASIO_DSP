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
        // Forward to Span-based in-place implementation to avoid per-call allocations and repeated work
        TransformInPlace(input.AsSpan(), currentStream);
        return input;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public void TransformInPlace(Span<double> input, DSP_Stream currentStream)
    {
        var auxBuffers = currentStream.AuxBuffer;
        int auxIndex = this.AuxGetIndex;

        if (auxIndex < 0 || auxBuffers == null || auxIndex >= auxBuffers.Length)
            return;

        var auxBuffer = auxBuffers[auxIndex];
        if (auxBuffer == null)
            return;

        if (auxBuffer.Length != input.Length)
            return;

        // Precompute linear gains
        double streamLinear = Decibels.DecibelsToLinear(this.StreamAttenuation);
        double auxLinear = Decibels.DecibelsToLinear(this.AuxAttenuation);

        if (this.MuteBefore)
        {
            // Copy from aux to input efficiently via Span
            auxBuffer.AsSpan().CopyTo(input);
            return;
        }

        // Mix input and aux into input span
        var auxSpan = auxBuffer.AsSpan();
        int len = input.Length;
        for (int i = 0; i < len; i++)
        {
            input[i] = input[i] * streamLinear + auxSpan[i] * auxLinear;
        }
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