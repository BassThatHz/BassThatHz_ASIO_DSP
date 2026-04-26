#nullable enable

namespace BassThatHz_ASIO_DSP_Processor;

using System;

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

public interface IStreamItem
{
    #region Properties
    string Name { get; set; }
    string DisplayMember { get; set; }
    int Index { get; set; }
    StreamType StreamType { get; set; }
    IStreamItem DeepClone();
    #endregion
}

public enum StreamType : byte
{
    Channel,
    Stream,
    Bus,
    AbstractBus 
}
public sealed class StreamItem : IStreamItem
{
    #region IStreamItem
    // Backing fields so we can intern common strings and avoid duplicate allocations.
    private string _name = string.Empty;
    private string _displayMember = string.Empty;

    public string Name
    {
        get => _name;
        set => _name = string.IsNullOrEmpty(value) ? string.Empty : string.Intern(value!);
    }

    public string DisplayMember
    {
        get => _displayMember;
        set => _displayMember = string.IsNullOrEmpty(value) ? string.Empty : string.Intern(value!);
    }

    public int Index { get; set; } = -1;
    public StreamType StreamType { get; set; } = StreamType.Channel;

    public IStreamItem DeepClone()
    {
        // Create a shallow clone; string interning on set will avoid duplicating string instances.
        return new StreamItem
        {
            Name = this._name,
            DisplayMember = this._displayMember,
            Index = this.Index,
            StreamType = this.StreamType
        };
    }
    #endregion

    public override bool Equals(object? obj)
    {
        // Quick reference check
        if (ReferenceEquals(this, obj))
            return true;

        // Null or different type => not equal
        if (obj is not StreamItem other)
            return false;

        // Compare only StreamType + Index
        // If you also want to factor in Name, you can do so here.
        return this.StreamType == other.StreamType &&
               this.Index == other.Index;
    }

    public override int GetHashCode()
    {
        // Combine StreamType and Index in a compact integer-based hash to avoid allocations.
        unchecked
        {
            return ((int)this.StreamType * 397) ^ this.Index;
        }
    }
}