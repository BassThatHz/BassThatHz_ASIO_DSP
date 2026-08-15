namespace Test_Project_1;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NAudio.Wave;
using NAudio.Wave.Asio;
using System;

[TestClass]
public class Test_IASIO_Unified
{
    private class FakeASIO : IASIO_Unified
    {
        public Action Driver_ResetRequestCallback { get; set; } = delegate { };
        public Action Driver_BufferSizeChangedCallback { get; set; } = delegate { };
        public Action Driver_ResyncRequestCallback { get; set; } = delegate { };
        public Action Driver_LatenciesChangedCallback { get; set; } = delegate { };
        public Action Driver_OverloadCallback { get; set; } = delegate { };
        public Action Driver_SampleRateChangedCallback { get; set; } = delegate { };

        public event EventHandler<AsioAudioAvailableEventArgs> AudioAvailable = delegate { };
        public event EventHandler DriverResetRequest = delegate { };

        public string DriverName => "Fake";
        public bool IsInitalized => true;
        public PlaybackState PlaybackState => PlaybackState.Stopped;
        public int NumberOfOutputChannels => 2;
        public int NumberOfInputChannels => 2;
        public int SamplesPerBuffer => 4;
        public bool AutoStop { get; set; }
        public int OutputChannelOffset { get; set; }
        public int InputChannelOffset { get; set; }
        public AsioDriverCapability GetDriverCapabilities => new AsioDriverCapability();
        public int DriverInputChannelCount => 2;
        public int DriverOutputChannelCount => 2;
        public Tuple<int, int> PlaybackLatency => Tuple.Create(1, 1);

        public string AsioInputChannelName(int channel) => "In" + channel;
        public string AsioOutputChannelName(int channel) => "Out" + channel;
        public void ShowControlPanel() { }
        public bool IsSampleRateSupported(int sampleRate) => sampleRate == 44100;
        public void Init(int numberOfInputChannels, int numberOfOutputChannels, int desiredSampleRate, int outputChannelOffset, int inputChannelOffset) { }
        public void Start() { }
        public AsioError Stop() => AsioError.ASE_OK;
        public int AsioDriver_GetDriverVersion() => 1;
        public double GetSampleRate() => 44100;
        public void GetClockSources(out long clocks, int numSources) { clocks = 0; }
        public void GetSamplePosition(out long samplePos, ref Asio64Bit timeStamp) { samplePos = 0; }
        public void Dispose() { }

        public void RaiseDriverResetRequest() => DriverResetRequest?.Invoke(this, EventArgs.Empty);
    }

    [TestMethod]
    public void FakeImplementation_ExposesExpectedDefaultValues()
    {
        var asio = new FakeASIO();
        Assert.AreEqual("Fake", asio.DriverName);
        Assert.IsTrue(asio.IsInitalized);
        Assert.AreEqual(2, asio.NumberOfInputChannels);
        Assert.AreEqual(2, asio.NumberOfOutputChannels);
        Assert.AreEqual(4, asio.SamplesPerBuffer);
    }

    [TestMethod]
    public void IsSampleRateSupported_ReturnsTrue_ForSupportedRate()
    {
        var asio = new FakeASIO();
        Assert.IsTrue(asio.IsSampleRateSupported(44100));
    }

    [TestMethod]
    public void IsSampleRateSupported_ReturnsFalse_ForUnsupportedRate()
    {
        var asio = new FakeASIO();
        Assert.IsFalse(asio.IsSampleRateSupported(12345));
    }

    [TestMethod]
    public void Stop_ReturnsOK()
    {
        var asio = new FakeASIO();
        Assert.AreEqual(AsioError.ASE_OK, asio.Stop());
    }

    [TestMethod]
    public void ChannelNames_ReturnExpectedFormat()
    {
        var asio = new FakeASIO();
        Assert.AreEqual("In0", asio.AsioInputChannelName(0));
        Assert.AreEqual("Out1", asio.AsioOutputChannelName(1));
    }

    [TestMethod]
    public void DriverResetRequest_Event_CanBeSubscribedAndRaised()
    {
        var asio = new FakeASIO();
        bool raised = false;
        asio.DriverResetRequest += (s, e) => raised = true;
        asio.RaiseDriverResetRequest();
        Assert.IsTrue(raised);
    }

    [TestMethod]
    public void Interface_IsAssignableToIDisposable()
    {
        Assert.IsTrue(typeof(IDisposable).IsAssignableFrom(typeof(IASIO_Unified)));
    }
}
