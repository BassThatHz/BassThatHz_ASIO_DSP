#nullable enable

namespace BassThatHz_ASIO_DSP_Processor;

#region Usings
using System;
using System.Collections.Generic;
using System.Xml.Serialization;
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

public interface IAbstractBus
{
    #region Properties
    bool IsBypassed { get; set; }

    string Name { get; set; }

    string DisplayMember { get; }

    List<IAbstractBusMappings> Mappings { get; set; }
    #endregion
}

[Serializable]
public sealed class DSP_AbstractBus : IAbstractBus
{
    #region IAbstractBus
    public bool IsBypassed { get; set; } = false;

    public string Name { get; set; } = string.Empty;

    [XmlIgnoreAttribute]
    public string DisplayMember => this.Name + " | " + this.IsBypassed;

    // Lazy-init the list to avoid allocating when not used.
    private List<IAbstractBusMappings>? _mappings;
    public List<IAbstractBusMappings> Mappings
    {
        get => _mappings ??= new List<IAbstractBusMappings>(0);
        set => _mappings = value ?? new List<IAbstractBusMappings>(0);
    }
    #endregion

    public override string ToString() => this.DisplayMember;

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(this, obj))
            return true;

        if (obj is not DSP_AbstractBus other)
            return false;

        return this.Name.Equals(other.Name);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(this.Name);
    }
}

public interface IAbstractBusMappings
{
    #region Properties
    bool IsBypassed { get; set; }

    StreamItem InputSource { get; set; }
    StreamItem OutputDestination { get; set; }

    string DisplayMember { get; }   
    #endregion
}

public sealed class DSP_AbstractBusMappings : IAbstractBusMappings
{
    public bool IsBypassed { get; set; } = false;

    // Lazily initialize the StreamItem instances to avoid allocations when defaults are not used.
    private StreamItem? _inputSource;
    public StreamItem InputSource
    {
        get => _inputSource ??= new StreamItem();
        set => _inputSource = value ?? new StreamItem();
    }

    private StreamItem? _outputDestination;
    public StreamItem OutputDestination
    {
        get => _outputDestination ??= new StreamItem();
        set => _outputDestination = value ?? new StreamItem();
    }

    [XmlIgnoreAttribute]
    public string DisplayMember => string.Concat(this.InputSource.DisplayMember, " | ", this.OutputDestination.DisplayMember, " | ", this.IsBypassed.ToString());

    [XmlIgnoreAttribute]
    public double[] Buffer { get; set; } = Array.Empty<double>();

    public override string ToString() => this.DisplayMember;

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(this, obj))
            return true;

        if (obj is not DSP_AbstractBusMappings other)
            return false;

        return this.InputSource.Equals(other.InputSource) &&
               this.OutputDestination.Equals(other.OutputDestination);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(this.InputSource, this.OutputDestination);
    }
}