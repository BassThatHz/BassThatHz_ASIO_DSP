#nullable enable

namespace BassThatHz_ASIO_DSP_Processor.DSP.Filters;

#region Usings
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
        TransformInPlace(input.AsSpan(), currentStream);
        return input;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public void TransformInPlace(Span<double> input, DSP_Stream currentStream)
    {
        if (currentStream == null)
            return;

        var numAux = DSP_Stream.NumberOfAuxBuffers;
        int idx = this.AuxSetIndex;

        if (idx < 0 || idx >= numAux)
            return;

        var auxBuffers = currentStream.AuxBuffer;

        // Ensure top-level aux buffer array exists and has expected length. Try to reuse existing inner arrays where possible.
        if (auxBuffers == null || auxBuffers.Length != numAux)
        {
            var newAux = new double[numAux][];
            if (auxBuffers != null)
            {
                int copyLen = Math.Min(auxBuffers.Length, newAux.Length);
                for (int i = 0; i < copyLen; i++)
                    newAux[i] = auxBuffers[i];
            }
            currentStream.AuxBuffer = newAux;
            auxBuffers = newAux;
        }

        // Ensure the specific aux row exists and is the right length. Allocate only if necessary.
        var row = auxBuffers[idx];
        if (row == null || row.Length != input.Length)
        {
            row = new double[input.Length];
            auxBuffers[idx] = row;
        }

        // Copy input into aux buffer using Span.CopyTo (optimized)
        input.CopyTo(row.AsSpan());

        if (this.MuteAfter)
        {
            input.Clear();
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

    public FilterTypes FilterType { get; } = FilterTypes.AuxSet;
    public FilterProcessingTypes FilterProcessingType { get; } = FilterProcessingTypes.WholeBlock;

    public IFilter DeepClone()
    {
        return CommonFunctions.DeepClone(this);
    }
    #endregion
}
