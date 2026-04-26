#nullable enable

namespace BassThatHz_ASIO_DSP_Processor;

#region Usings
using System;
using System.Collections.Generic;
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
public class GPEQ : IFilter
{
    #region Public Properties
    public List<IFilter> Filters { get; set; } = new();
    #endregion

    #region Public Functions
    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public double[] Transform(double[] input, DSP_Stream currentStream)
    {
        var filters = this.Filters;
        if (filters == null || filters.Count == 0)
            return input;

        int count = filters.Count;
        for (int i = 0; i < count; i++)
        {
            var f = filters[i];
            if (f == null || !f.FilterEnabled)
                continue;

            try
            {
                input = f.Transform(input, currentStream);
            }
            catch (IndexOutOfRangeException) { /* ignore concurrent modifications */ }
            catch (ArgumentOutOfRangeException) { /* ignore concurrent modifications */ }
            catch (NullReferenceException) { /* ignore concurrent modifications */ }
        }

        return input;
    }

    public void ResetSampleRate(int sampleRate)
    {
        var filters = this.Filters;
        if (filters == null || filters.Count == 0)
            return;

        int count = filters.Count;
        for (int i = 0; i < count; i++)
        {
            try
            {
                filters[i]?.ResetSampleRate(sampleRate);
            }
            catch (NullReferenceException) { /* ignore concurrent modifications */ }
            catch (ArgumentOutOfRangeException) { /* ignore concurrent modifications */ }
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

    public IFilter DeepClone()
    {
        return CommonFunctions.DeepClone(this);
    }
    #endregion
}