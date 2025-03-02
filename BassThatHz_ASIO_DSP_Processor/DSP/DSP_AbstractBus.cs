#nullable enable

namespace BassThatHz_ASIO_DSP_Processor;

#region Usings
using System;
using System.Xml.Serialization;
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

public interface IAbstractBus : IBus
{
    #region Properties
    StreamItem InputSource { get; set; }
    StreamItem OutputDestination { get; set; }

    bool IsBypassed { get; set; }
    #endregion
}

[Serializable]
public class DSP_AbstractBus : IAbstractBus
{
    #region IAbstractBus
    public string Name { get; set; } = string.Empty;

    [XmlIgnoreAttribute]
    public double[] Buffer { get; set; } = Array.Empty<double>();

    public bool IsBypassed { get; set; } = false;

    [XmlIgnoreAttribute]
    public string DisplayMember => this.Name + " | " + 
                                    this.InputSource.DisplayMember + " | " + 
                                    this.OutputDestination.DisplayMember;

    public StreamItem InputSource { get; set; } = new();

    public StreamItem OutputDestination { get; set; } = new();

    #endregion

    public override string ToString() => this.DisplayMember;   
}