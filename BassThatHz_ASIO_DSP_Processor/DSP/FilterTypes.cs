#nullable enable

namespace BassThatHz_ASIO_DSP_Processor
{
    using System;

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
    public enum FilterTypes
    {
        PEQ,
        Basic_HPF_LPF,
        Low_Shelf,
        High_Shelf,
        Notch,
        Band_Pass,
        All_Pass,
        Adv_High_Pass,
        Adv_Low_Pass,
        Polarity,
        Delay,
        Floor,
        Limiter,
        SmartGain,
        FIR,
        Anti_DC,
        Mixer,
        DynamicRangeCompressor,
        ClassicLimiter,
        DEQ,
        AuxSet,
        AuxGet,
        ULF_FIR,
        GPEQ
    }
}