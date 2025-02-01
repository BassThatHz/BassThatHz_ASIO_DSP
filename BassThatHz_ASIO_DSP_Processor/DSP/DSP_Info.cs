#nullable enable

namespace BassThatHz_ASIO_DSP_Processor;

#region Usings
using System;
using System.Collections.Generic;
using System.Diagnostics;
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



// Holds a list of Serializable DSP states, including a reference to the list of Streams being DSPed
//This is converted to and from an XML based file.
//All object references must either be serializable types or excluded from serialization via XmlIgnoreAttribute attribute tags
[Serializable]
public class DSP_Info
{
    #region Application
    public int StartUpDelay { get; set; } = 0;
    public bool AutoStartDSP { get; set; } = false;
    public ProcessPriorityClass ProcessPriority { get; set; } = ProcessPriorityClass.High;
    public bool IsMultiThreadingEnabled { get; set; } = true;
    public bool IsBackgroundThreadEnabled { get; set; } = true;
    public bool EnableStats { get; set; } = false;

    public bool NetworkConfigAPI_Enabled { get; set; } = false;
    public string NetworkConfigAPI_Host { get; set; } = "localhost";
    public int NetworkConfigAPI_Port { get; set; } = 8080;
    #endregion

    #region Input Settings
    public string ASIO_InputDevice { get; set; } = string.Empty;
    public double InMasterVolume { get; set; } = 1;
    public int InChannelCount { get; set; } = 0;
    public int InSampleRate { get; set; } = 0;
    public int InBitDepth { get; set; } = 0;
    public string InBufferSize { get; set; } = "Hardware Recommended";
    #endregion

    #region Output Settings
    public string ASIO_OutputDevice { get; set; } = string.Empty;
    public double OutMasterVolume { get; set; } = 1;
    public int OutChannelCount { get; set; } = 0;
    public int OutSampleRate { get; set; } = 0;
    public int OutBitDepth { get; set; } = 0;
    public string OutBufferSize { get; set; } = "Hardware Recommended";
    #endregion

    #region Streams
    public List<DSP_Stream> Streams { get; set; } = new();
    #endregion
}