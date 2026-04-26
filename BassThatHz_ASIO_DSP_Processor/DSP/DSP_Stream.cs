#nullable enable

namespace BassThatHz_ASIO_DSP_Processor;

#region Usings
using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Windows.Foundation.Metadata;
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
public sealed class DSP_Stream 
{
    [XmlIgnoreAttribute]
    private double[]? _abstractBusBuffer;
    [XmlIgnoreAttribute]
    public double[] AbstractBusBuffer
    {
        get => _abstractBusBuffer ??= Array.Empty<double>();
        set => _abstractBusBuffer = value ?? Array.Empty<double>();
    }

    [XmlIgnoreAttribute]
    private double[][]? _auxBuffer;
    [XmlIgnoreAttribute]
    public double[][] AuxBuffer
    {
        get => _auxBuffer ??= Array.Empty<double[]>();
        set => _auxBuffer = value ?? Array.Empty<double[]>();
    }

    // Use const to avoid per-instance storage for this fixed limit.
    [XmlIgnoreAttribute]
    public const int NumberOfAuxBuffers = 256;

    #region Legacy - Remove these eventually
    [XmlIgnoreAttribute]
    [Deprecated("Use InputSource", DeprecationType.Deprecate, 0)]
    public string InputChannelName = string.Empty;

    [XmlIgnoreAttribute]
    [Deprecated("Use OutputDestination", DeprecationType.Deprecate, 0)]
    public string OutputChannelName = string.Empty;

    [Deprecated("Use InputSource", DeprecationType.Deprecate, 0)]
    public int InputChannelIndex = -1;

    [Deprecated("Use OutputDestination", DeprecationType.Deprecate, 0)]
    public int OutputChannelIndex = -1;
    #endregion

    // Lazily initialize stream item instances to avoid allocating StreamItem when defaults are not used.
    protected IStreamItem? _inputSource;
    public IStreamItem InputSource
    {
        get => _inputSource ??= new StreamItem();
        set
        {
            if (value.StreamType == StreamType.Stream)
                throw new InvalidOperationException("Stream type is not allowed as a Stream InputSource.");
            _inputSource = value ?? new StreamItem();
        }
    }

    protected IStreamItem? _outputDestination;
    public IStreamItem OutputDestination
    {
        get => _outputDestination ??= new StreamItem();
        set
        {
            if (value.StreamType == StreamType.Stream)
                throw new InvalidOperationException("Stream type is not allowed as a Stream OutputDestination.");
            _outputDestination = value ?? new StreamItem();
        }
    }

    // Use auto-properties for primitive values (cheap) and lazy-init collection for filters.
    public double InputVolume { get; set; }
    public double OutputVolume { get; set; }

    private List<IFilter>? _filters;
    public List<IFilter> Filters => _filters ??= new List<IFilter>(4);
}