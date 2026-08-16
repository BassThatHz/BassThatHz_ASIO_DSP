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
        //Deliberately asymmetric so a transposed read of InputLatency/OutputLatency is detectable.
        public (int InputLatency, int OutputLatency) PlaybackLatency => (11, 22);

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

    /// <summary>
    /// PlaybackLatency used to be a Tuple&lt;int, int&gt; read positionally as .Item1/.Item2, which made
    /// an input/output transposition invisible. It is now a named ValueTuple; pin both the naming and
    /// the ordering with deliberately asymmetric values.
    /// </summary>
    [TestMethod]
    public void PlaybackLatency_ReturnsInputAndOutput_NotTransposed()
    {
        IASIO_Unified asio = new FakeASIO();
        var Local_Latency = asio.PlaybackLatency;

        Assert.AreEqual(11, Local_Latency.InputLatency, "InputLatency came back transposed.");
        Assert.AreEqual(22, Local_Latency.OutputLatency, "OutputLatency came back transposed.");
        Assert.AreEqual(11, Local_Latency.Item1);
        Assert.AreEqual(22, Local_Latency.Item2);
    }

    /// <summary>
    /// The stats page reads PlaybackLatency from a repeating ~1 Hz timer. As a ValueTuple it is a
    /// struct, so a steady-state read must not allocate on the managed heap the way the old
    /// reference-typed Tuple did.
    /// </summary>
    [TestMethod]
    public void PlaybackLatency_IsValueType_AndDoesNotAllocate()
    {
        Assert.IsTrue(typeof(IASIO_Unified).GetProperty(nameof(IASIO_Unified.PlaybackLatency))!.PropertyType.IsValueType,
            "PlaybackLatency must be a value type so that reading it cannot heap-allocate.");

        IASIO_Unified asio = new FakeASIO();

        //Warm up (JIT + any first-touch allocation) before measuring the steady state.
        long Local_Sink = 0;
        for (int i = 0; i < 1000; i++)
            Local_Sink += asio.PlaybackLatency.InputLatency;

        var Local_Before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 100000; i++)
            Local_Sink += asio.PlaybackLatency.InputLatency + asio.PlaybackLatency.OutputLatency;
        var Local_After = GC.GetAllocatedBytesForCurrentThread();

        Assert.AreNotEqual(0, Local_Sink);
        Assert.AreEqual(0, Local_After - Local_Before,
            "Reading PlaybackLatency must be allocation-free in steady state.");
    }
}
