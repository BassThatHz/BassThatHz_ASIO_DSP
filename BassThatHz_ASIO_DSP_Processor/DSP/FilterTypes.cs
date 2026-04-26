#nullable enable

namespace BassThatHz_ASIO_DSP_Processor;

#region Usings
using System;
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
// Use a compact underlying type to reduce memory when many enum values are stored.
public enum FilterTypes : byte
{
    PEQ = 0,
    Basic_HPF_LPF = 1,
    Low_Shelf = 2,
    High_Shelf = 3,
    Notch = 4,
    Band_Pass = 5,
    All_Pass = 6,
    Adv_High_Pass = 7,
    Adv_Low_Pass = 8,
    Polarity = 9,
    Delay = 10,
    Floor = 11,
    Limiter = 12,
    SmartGain = 13,
    FIR = 14,
    Anti_DC = 15,
    Mixer = 16,
    DynamicRangeCompressor = 17,
    ClassicLimiter = 18,
    DEQ = 19,
    AuxSet = 20,
    AuxGet = 21,
    ULF_FIR = 22,
    GPEQ = 23
}
