#nullable enable

namespace BassThatHz_ASIO_DSP_Processor;

#region Usings
using System;
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

public interface IBus
{
    #region Properties
    string Name { get; set; }

    double[] Buffer { get; set; }

    string DisplayMember { get; }

    bool IsBypassed { get; set; }
    #endregion
}

[Serializable]
public sealed class DSP_Bus : IBus
{
    #region IBus

    public string Name { get; set; } = string.Empty;

    // Lazily initialized backing field for Buffer to avoid unnecessary allocations.
    private double[]? _buffer;
    [XmlIgnoreAttribute]
    public double[] Buffer
    {
        get => _buffer ??= Array.Empty<double>();
        set => _buffer = value ?? Array.Empty<double>();
    }

    [XmlIgnoreAttribute]
    public string DisplayMember => string.Concat(this.Name, " | ", this.IsBypassed.ToString());

    public bool IsBypassed { get; set; } = false;
    #endregion

    public override string ToString() => this.DisplayMember;
}