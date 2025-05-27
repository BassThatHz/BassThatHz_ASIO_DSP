using System;
using System.Threading;
using System.Collections.ObjectModel;
using BassThatHz_ASIO_DSP_Processor;
using NAudio.Wave.Asio;
using NAudio.Wave;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

[TestClass]
public class Test_ASIO_Engine_HappyPaths
{
    private class LocalMockASIOEngine : ASIO_Engine
    {
        public LocalMockASIOEngine(IASIO_Unified driver) { this.ASIO = driver; }
        protected override IASIO_Unified Get_New_ASIO_Instance(string asio_Device_Name) => this.ASIO;
    }

    private class DummyASIO : IASIO_Unified
    {
        public Action Driver_ResetRequestCallback { get; set; } = delegate { };
        public Action Driver_BufferSizeChangedCallback { get; set; } = delegate { };
        public Action Driver_ResyncRequestCallback { get; set; } = delegate { };
        public Action Driver_LatenciesChangedCallback { get; set; } = delegate { };
        public Action Driver_OverloadCallback { get; set; } = delegate { };
        public Action Driver_SampleRateChangedCallback { get; set; } = delegate { };
        public event EventHandler<AsioAudioAvailableEventArgs> AudioAvailable = delegate { };
        public event EventHandler DriverResetRequest = delegate { };
        public string DriverName => "Dummy";
        public bool IsInitalized => true;
        public PlaybackState PlaybackState => PlaybackState.Stopped;
        public int NumberOfOutputChannels => 2;
        public int NumberOfInputChannels => 2;
        public int SamplesPerBuffer => 2;
        public bool AutoStop { get; set; }
        public int OutputChannelOffset { get; set; }
        public int InputChannelOffset { get; set; }
        public AsioDriverCapability GetDriverCapabilities => new AsioDriverCapability();
        public int DriverInputChannelCount => 2;
        public int DriverOutputChannelCount => 2;
        public Tuple<int, int> PlaybackLatency => Tuple.Create(1, 1);
        public string AsioInputChannelName(int channel) => "I";
        public string AsioOutputChannelName(int channel) => "O";
        public void ShowControlPanel() { }
        public bool IsSampleRateSupported(int sampleRate) => true;
        public void Init(int a, int b, int c, int d, int e) { }
        public void Start() { }
        public AsioError Stop() => AsioError.ASE_OK;
        public int AsioDriver_GetDriverVersion() => 1;
        public double GetSampleRate() => 44100;
        public void GetClockSources(out long clocks, int numSources) { clocks = 0; }
        public void GetSamplePosition(out long samplePos, ref Asio64Bit timeStamp) { samplePos = 0; }
        public void Dispose() { }
    }

    [TestMethod]
    public void RequestClearedOutputBuffer_PushesIndexAndClearsBuffer()
    {
        var engine = new ASIO_Engine();
        engine.OutputBuffer = new double[2][] { new double[4] { 1, 2, 3, 4 }, new double[4] { 5, 6, 7, 8 } };
        engine.RequestClearedOutputBuffer(1);
        var method = typeof(ASIO_Engine).GetMethod("ClearRequestedOutputBuffers", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        method.Invoke(engine, null);
        Assert.IsTrue(engine.OutputBuffer[1].All(x => x == 0));
    }

    [TestMethod]
    public void Stop_CallsStopASIO_DisposesDriver()
    {
        var mock = new LocalMockASIOEngine(new DummyASIO());
        mock.Start("Fake", 44100, 2, 2);
        mock.Stop();
        var asioField = typeof(ASIO_Engine).GetField("ASIO", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.IsNull(asioField.GetValue(mock));
    }

    [TestMethod]
    public void CleanUp_CallsCleanUpASIO_DisposesDriver()
    {
        var mock = new LocalMockASIOEngine(new DummyASIO());
        mock.Start("Fake", 44100, 2, 2);
        mock.CleanUp();
        var asioField = typeof(ASIO_Engine).GetField("ASIO", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.IsNull(asioField.GetValue(mock));
    }

    [TestMethod]
    public void Show_ControlPanel_And_Show_ControlPanel_String_DoesNotThrow()
    {
        var engine = new ASIO_Engine();
        typeof(ASIO_Engine).GetProperty("DeviceName").SetValue(engine, "Fake");
        engine.Show_ControlPanel();
        engine.Show_ControlPanel("Fake");
    }

    [TestMethod]
    public void GetDriverNames_ReturnsArray()
    {
        var engine = new ASIO_Engine();
        var names = engine.GetDriverNames();
        Assert.IsNotNull(names);
    }

    [TestMethod]
    public void GetDriverCapabilities_ReturnsCapabilities()
    {
        var engine = new ASIO_Engine();
        var caps = engine.GetDriverCapabilities("Fake");
        Assert.IsNotNull(caps);
    }

    [TestMethod]
    public void GetMinMaxPreferredBufferSize_ReturnsInt()
    {
        var engine = new ASIO_Engine();
        Assert.IsTrue(engine.GetMinBufferSize("Fake") >= 0);
        Assert.IsTrue(engine.GetMaxBufferSize("Fake") >= 0);
        Assert.IsTrue(engine.GetPreferredBufferSize("Fake") >= 0);
    }

    [TestMethod]
    public void IsSampleRateSupported_ReturnsTrue()
    {
        var engine = new ASIO_Engine();
        Assert.IsTrue(engine.IsSampleRateSupported("Fake", 44100));
    }

    [TestMethod]
    public void Clear_DSP_PeakProcessingTime_ResetsValue()
    {
        var engine = new ASIO_Engine();
        typeof(ASIO_Engine).GetField("DSP_PeakProcessingTime", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(engine, TimeSpan.FromSeconds(1));
        engine.Clear_DSP_PeakProcessingTime();
        Assert.AreEqual(TimeSpan.Zero, engine.DSP_PeakProcessingTime);
    }

    [TestMethod]
    public void Clear_UnderrunsCounter_ResetsValue()
    {
        var engine = new ASIO_Engine();
        typeof(ASIO_Engine).GetField("Underruns_Counter", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(engine, 5);
        engine.Clear_UnderrunsCounter();
        Assert.AreEqual(0, engine.Underruns);
    }

    [TestMethod]
    public void GetInputOutputAudioData_ReturnsCorrectData()
    {
        var engine = new ASIO_Engine();
        engine.InputBuffer = new double[2][] { new double[] { 1.1, 2.2 }, new double[] { 3.3, 4.4 } };
        engine.OutputBuffer = new double[2][] { new double[] { 5.5, 6.6 }, new double[] { 7.7, 8.8 } };
        var in0 = engine.GetInputAudioData(0);
        var out1 = engine.GetOutputAudioData(1);
        Assert.AreEqual(1.1, in0[0]);
        Assert.AreEqual(8.8, out1[1]);
    }
}
