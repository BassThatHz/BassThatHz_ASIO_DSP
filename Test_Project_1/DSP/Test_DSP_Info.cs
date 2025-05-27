using BassThatHz_ASIO_DSP_Processor;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using System.Collections.ObjectModel;

namespace Test_Project_1;

[TestClass]
public class Test_DSP_Info
{
    [TestMethod]
    public void DSP_Info_DefaultValues_AreCorrect()
    {
        var info = new DSP_Info();
        Assert.AreEqual(0, info.StartUpDelay);
        Assert.IsFalse(info.AutoStartDSP);
        Assert.AreEqual(ProcessPriorityClass.High, info.ProcessPriority);
        Assert.IsTrue(info.IsMultiThreadingEnabled);
        Assert.IsTrue(info.IsBackgroundThreadEnabled);
        Assert.IsFalse(info.EnableStats);
        Assert.IsFalse(info.NetworkConfigAPI_Enabled);
        Assert.AreEqual("localhost", info.NetworkConfigAPI_Host);
        Assert.AreEqual(8080, info.NetworkConfigAPI_Port);
        Assert.AreEqual(string.Empty, info.ASIO_InputDevice);
        Assert.AreEqual(1, info.InMasterVolume);
        Assert.AreEqual(0, info.InChannelCount);
        Assert.AreEqual(0, info.InSampleRate);
        Assert.AreEqual(0, info.InBitDepth);
        Assert.AreEqual("Hardware Recommended", info.InBufferSize);
        Assert.AreEqual(string.Empty, info.ASIO_OutputDevice);
        Assert.AreEqual(1, info.OutMasterVolume);
        Assert.AreEqual(0, info.OutChannelCount);
        Assert.AreEqual(0, info.OutSampleRate);
        Assert.AreEqual(0, info.OutBitDepth);
        Assert.AreEqual("Hardware Recommended", info.OutBufferSize);
        Assert.IsNotNull(info.Streams);
        Assert.IsNotNull(info.Buses);
        Assert.IsNotNull(info.AbstractBuses);
    }

    [TestMethod]
    public void DSP_Info_PropertySetters_WorkCorrectly()
    {
        var info = new DSP_Info
        {
            StartUpDelay = 10,
            AutoStartDSP = true,
            ProcessPriority = ProcessPriorityClass.RealTime,
            IsMultiThreadingEnabled = false,
            IsBackgroundThreadEnabled = false,
            EnableStats = true,
            NetworkConfigAPI_Enabled = true,
            NetworkConfigAPI_Host = "192.168.1.1",
            NetworkConfigAPI_Port = 1234,
            ASIO_InputDevice = "InputDev",
            InMasterVolume = 0.5,
            InChannelCount = 2,
            InSampleRate = 48000,
            InBitDepth = 24,
            InBufferSize = "Custom",
            ASIO_OutputDevice = "OutputDev",
            OutMasterVolume = 0.8,
            OutChannelCount = 2,
            OutSampleRate = 44100,
            OutBitDepth = 16,
            OutBufferSize = "Custom2"
        };
        Assert.AreEqual(10, info.StartUpDelay);
        Assert.IsTrue(info.AutoStartDSP);
        Assert.AreEqual(ProcessPriorityClass.RealTime, info.ProcessPriority);
        Assert.IsFalse(info.IsMultiThreadingEnabled);
        Assert.IsFalse(info.IsBackgroundThreadEnabled);
        Assert.IsTrue(info.EnableStats);
        Assert.IsTrue(info.NetworkConfigAPI_Enabled);
        Assert.AreEqual("192.168.1.1", info.NetworkConfigAPI_Host);
        Assert.AreEqual(1234, info.NetworkConfigAPI_Port);
        Assert.AreEqual("InputDev", info.ASIO_InputDevice);
        Assert.AreEqual(0.5, info.InMasterVolume);
        Assert.AreEqual(2, info.InChannelCount);
        Assert.AreEqual(48000, info.InSampleRate);
        Assert.AreEqual(24, info.InBitDepth);
        Assert.AreEqual("Custom", info.InBufferSize);
        Assert.AreEqual("OutputDev", info.ASIO_OutputDevice);
        Assert.AreEqual(0.8, info.OutMasterVolume);
        Assert.AreEqual(2, info.OutChannelCount);
        Assert.AreEqual(44100, info.OutSampleRate);
        Assert.AreEqual(16, info.OutBitDepth);
        Assert.AreEqual("Custom2", info.OutBufferSize);
    }

    [TestMethod]
    public void DSP_Info_Collections_CanBeModified()
    {
        var info = new DSP_Info();
        var stream = new DSP_Stream();
        var bus = new DSP_Bus();
        var abstractBus = new DSP_AbstractBus();
        info.Streams.Add(stream);
        info.Buses.Add(bus);
        info.AbstractBuses.Add(abstractBus);
        Assert.AreEqual(1, info.Streams.Count);
        Assert.AreEqual(1, info.Buses.Count);
        Assert.AreEqual(1, info.AbstractBuses.Count);
    }
}