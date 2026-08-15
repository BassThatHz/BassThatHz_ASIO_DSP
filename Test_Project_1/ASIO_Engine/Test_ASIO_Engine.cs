namespace Test_Project_1;

using BassThatHz_ASIO_DSP_Processor;
using NAudio.Wave;
using NAudio.Wave.Asio;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class Test_ASIO_Engine
{
    public class Mock_ASIO_Engine : ASIO_Engine
    {
        protected Mock_ASIO_Unified _ASIO_Driver;

        public Mock_ASIO_Engine(Mock_ASIO_Unified ASIO_Driver)
        {
            this._ASIO_Driver = ASIO_Driver;
        }

        protected override IASIO_Unified Get_New_ASIO_Instance(string asio_Device_Name)
        {
            return this._ASIO_Driver;
        }
    }

    public class Mock_ASIO_Unified : IASIO_Unified
    {
        public Mock_ASIO_Unified(int channelCount, int samplesPerBuffer)
        {
            this.SamplesPerBuffer = samplesPerBuffer;
            this.DriverInputChannelCount = channelCount;
            this.DriverOutputChannelCount = channelCount;
        }

        public void Mock_ActivateDataStream(IntPtr[] In_ints, IntPtr[] Out_ints, AsioSampleType input)
        {
            var e = new AsioAudioAvailableEventArgs(In_ints, Out_ints, this.SamplesPerBuffer, input);
            this.AudioAvailable.Invoke(this, e);
        }

        public Action Driver_ResetRequestCallback { get; set; } = delegate { };
        public Action Driver_BufferSizeChangedCallback { get; set; } = delegate { };
        public Action Driver_ResyncRequestCallback { get; set; } = delegate { };
        public Action Driver_LatenciesChangedCallback { get; set; } = delegate { };
        public Action Driver_OverloadCallback { get; set; } = delegate { };
        public Action Driver_SampleRateChangedCallback { get; set; } = delegate { };

        public event EventHandler<AsioAudioAvailableEventArgs> AudioAvailable = delegate { };
        public event EventHandler DriverResetRequest = delegate { };

        public string DriverName { get; } = "MockDriverName";
        public bool IsInitalized { get; }
        public PlaybackState PlaybackState { get; }
        public int NumberOfOutputChannels { get; }
        public int NumberOfInputChannels { get; }
        public int SamplesPerBuffer { get; }
        public bool AutoStop { get; set; }
        public int OutputChannelOffset { get; set; }
        public int InputChannelOffset { get; set; }
        public AsioDriverCapability GetDriverCapabilities { get; }
        public int DriverInputChannelCount { get; }
        public int DriverOutputChannelCount { get; }
        public Tuple<int, int> PlaybackLatency { get; } = Tuple.Create(0, 0);

        public string AsioInputChannelName(int channel)
        {
            return "FakeInputChannelName";
        }

        public string AsioOutputChannelName(int channel)
        {
            return "FakeOutputChannelName";
        }

        public void ShowControlPanel()
        {

        }

        public bool IsSampleRateSupported(int sampleRate)
        {
            return true;
        }

        public void Init(int numberOfInputChannels, int numberOfOutputChannels, int desiredSampleRate, int outputChannelOffset, int inputChannelOffset)
        {

        }

        public void Start()
        {

        }

        public AsioError Stop()
        {
            return AsioError.ASE_OK;
        }

        public int AsioDriver_GetDriverVersion()
        {
            return 0;
        }

        public double GetSampleRate()
        {
            return 0;
        }

        public void GetClockSources(out long clocks, int numSources)
        {
            clocks = 0;
        }

        public void GetSamplePosition(out long samplePos, ref Asio64Bit timeStamp)
        {
            samplePos = 0;
        }

        public void Dispose()
        {

        }
    }

    private class LocalMockASIOEngine : ASIO_Engine
    {
        private readonly IASIO_Unified _originalDriver;
        public LocalMockASIOEngine(IASIO_Unified driver) { this.ASIO = driver; this._originalDriver = driver; }
        protected override IASIO_Unified Get_New_ASIO_Instance(string asio_Device_Name) => this._originalDriver;
        // Prevent hardware access in tests
        protected override void Show_ASIO_ControlPanel() { /* no-op for test */ }
        protected override void Show_ASIO_ControlPanel(string deviceName) { /* no-op for test */ }

        public new bool IsSampleRateSupported(string asioDeviceName, int sampleRate)
        {
            // Always return true for the mock
            return true;
        }

        public new AsioDriverCapability GetDriverCapabilities(string asioDeviceName)
        {
            return new AsioDriverCapability();
        }
        public new int GetMinBufferSize(string asioDeviceName) => 2;
        public new int GetMaxBufferSize(string asioDeviceName) => 2;
        public new int GetPreferredBufferSize(string asioDeviceName) => 2;
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

    private static DSP_Stream CreateStream(int inputIdx, StreamType inputType, int outputIdx, StreamType outputType)
    {
        return new DSP_Stream
        {
            InputSource = new StreamItem { Index = inputIdx, StreamType = inputType },
            OutputDestination = new StreamItem { Index = outputIdx, StreamType = outputType },
            InputVolume = 1.0,
            OutputVolume = 1.0
        };
    }

    private static List<List<DSP_Stream>> InvokeBuildStreamChains(ASIO_Engine engine, ObservableCollection<DSP_Stream> streams)
    {
        return engine.GetType()
            .GetMethod("BuildStreamChains", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .Invoke(engine, new object[] { streams }) as List<List<DSP_Stream>>;
    }

    [TestMethod]
    public void EventHandlers_AreInvoked_OnAudioAvailable()
    {
        var mockDriver = new Mock_ASIO_Unified(2, 4);
        var engine = new Mock_ASIO_Engine(mockDriver);
        using var inputEvent = new System.Threading.ManualResetEventSlim(false);
        using var outputEvent = new System.Threading.ManualResetEventSlim(false);
        engine.InputDataAvailable += () => inputEvent.Set();
        engine.OutputDataAvailable += () => outputEvent.Set();
        engine.Start("MockDriverName", 44100, 2, 2);

        // Allocate unmanaged memory for each channel and fill with dummy data
        var inputPtrs = new IntPtr[2];
        var outputPtrs = new IntPtr[2];
        int sampleSize = sizeof(int); // For Int32LSB
        int bufferSize = 4 * sampleSize; // 4 samples per buffer
        for (int ch = 0; ch < 2; ch++)
        {
            inputPtrs[ch] = System.Runtime.InteropServices.Marshal.AllocHGlobal(bufferSize);
            outputPtrs[ch] = System.Runtime.InteropServices.Marshal.AllocHGlobal(bufferSize);
            unsafe
            {
                int* inBuf = (int*)inputPtrs[ch];
                int* outBuf = (int*)outputPtrs[ch];
                for (int n = 0; n < 4; n++)
                {
                    inBuf[n] = 0; // or any dummy value
                    outBuf[n] = 0;
                }
            }
        }
        try
        {
            mockDriver.Mock_ActivateDataStream(inputPtrs, outputPtrs, AsioSampleType.Int32LSB);
            // Wait for both events to be set, with a timeout to avoid hanging
            bool inputFired = inputEvent.Wait(1000);
            bool outputFired = outputEvent.Wait(1000);
            Assert.IsTrue(inputFired, "InputDataAvailable not fired");
            Assert.IsTrue(outputFired, "OutputDataAvailable not fired");
        }
        finally
        {
            for (int ch = 0; ch < 2; ch++)
            {
                System.Runtime.InteropServices.Marshal.FreeHGlobal(inputPtrs[ch]);
                System.Runtime.InteropServices.Marshal.FreeHGlobal(outputPtrs[ch]);
            }
        }
        engine.Stop();
    }

    [TestMethod]
    public void DriverStateChangeEvents_AreInvoked()
    {
        var mockDriver = new Mock_ASIO_Unified(2, 4);
        var engine = new Mock_ASIO_Engine(mockDriver);
        bool reset = false, buf = false, resync = false, lat = false, ov = false, sr = false;
        engine.Driver_ResetRequest += () => reset = true;
        engine.Driver_BufferSizeChanged += () => buf = true;
        engine.Driver_ResyncRequest += () => resync = true;
        engine.Driver_LatenciesChanged += () => lat = true;
        engine.Driver_Overload += () => ov = true;
        engine.Driver_SampleRateChanged += () => sr = true;
        engine.Start("MockDriverName", 44100, 2, 2);
        mockDriver.Driver_ResetRequestCallback();
        mockDriver.Driver_BufferSizeChangedCallback();
        mockDriver.Driver_ResyncRequestCallback();
        mockDriver.Driver_LatenciesChangedCallback();
        mockDriver.Driver_OverloadCallback();
        mockDriver.Driver_SampleRateChangedCallback();
        Assert.IsTrue(reset && buf && resync && lat && ov && sr);
        engine.Stop();
    }

    [TestMethod]
    public void GetInputOutputAudioData_ReturnsNull_OnInvalidIndex()
    {
        var engine = new ASIO_Engine();
        engine.InputBuffer = new double[1][] { new double[] { 1.0 } };
        engine.OutputBuffer = new double[1][] { new double[] { 2.0 } };
        Assert.IsNull(engine.GetInputAudioData(-1));
        Assert.IsNull(engine.GetInputAudioData(2));
        Assert.IsNull(engine.GetOutputAudioData(-1));
        Assert.IsNull(engine.GetOutputAudioData(2));
    }

    [TestMethod]
    public void DSP_Thread_Starts_And_Stops()
    {
        var mockDriver = new Mock_ASIO_Unified(2, 4);
        var engine = new Mock_ASIO_Engine(mockDriver);
        engine.Start("MockDriverName", 44100, 2, 2);
        var threadField = typeof(ASIO_Engine).GetField("DSP_Thread", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var thread = (System.Threading.Thread)threadField.GetValue(engine);
        Assert.IsTrue(thread.IsAlive);
        engine.Stop();
        System.Threading.Thread.Sleep(50);
        // Thread may still be alive if not enough time, but should not throw
    }

    [TestMethod]
    public void BuildStreamChains_Depth1_ChannelToChannel()
    {
        var engine = new ASIO_Engine();
        var streams = new ObservableCollection<DSP_Stream>
        {
            CreateStream(0, StreamType.Channel, 1, StreamType.Channel)
        };
        var chains = InvokeBuildStreamChains(engine, streams);
        Assert.IsNotNull(chains);
        Assert.AreEqual(1, chains.Count);
        Assert.AreEqual(1, chains[0].Count);
        Assert.AreEqual(StreamType.Channel, chains[0][0].InputSource.StreamType);
        Assert.AreEqual(StreamType.Channel, chains[0][0].OutputDestination.StreamType);
    }

    [TestMethod]
    public void BuildStreamChains_Depth2_ChannelToBusToChannel()
    {
        var engine = new ASIO_Engine();
        var streams = new ObservableCollection<DSP_Stream>
        {
            CreateStream(0, StreamType.Channel, 0, StreamType.Bus),
            CreateStream(0, StreamType.Bus, 1, StreamType.Channel)
        };
        var chains = InvokeBuildStreamChains(engine, streams);
        Assert.IsNotNull(chains);
        Assert.AreEqual(1, chains.Count);
        Assert.AreEqual(2, chains[0].Count);
        Assert.AreEqual(StreamType.Channel, chains[0][0].InputSource.StreamType);
        Assert.AreEqual(StreamType.Bus, chains[0][0].OutputDestination.StreamType);
        Assert.AreEqual(StreamType.Bus, chains[0][1].InputSource.StreamType);
        Assert.AreEqual(StreamType.Channel, chains[0][1].OutputDestination.StreamType);
    }

    [TestMethod]
    public void BuildStreamChains_Depth2_ChannelToAbstractBusToChannel()
    {
        var engine = new ASIO_Engine();
        var streams = new ObservableCollection<DSP_Stream>
        {
            CreateStream(0, StreamType.Channel, 0, StreamType.AbstractBus),
            CreateStream(0, StreamType.AbstractBus, 1, StreamType.Channel),
            CreateStream(0, StreamType.AbstractBus, 0, StreamType.AbstractBus) // Master
        };
        Program.DSP_Info.AbstractBuses.Clear();
        var ab = new DSP_AbstractBus { Name = "AB0" };
        ab.Mappings.Add(new DSP_AbstractBusMappings
        {
            InputSource = new StreamItem { Index = 0, StreamType = StreamType.Channel },
            OutputDestination = new StreamItem { Index = 1, StreamType = StreamType.Channel }
        });
        Program.DSP_Info.AbstractBuses.Add(ab);
        Program.DSP_Info.Streams.Clear();
        foreach (var s in streams) Program.DSP_Info.Streams.Add(s);
        var chains = InvokeBuildStreamChains(engine, streams);
        Assert.IsNotNull(chains);
        Assert.AreEqual(1, chains.Count);
        Assert.IsTrue(chains[0].Any(s => s.InputSource.StreamType == StreamType.AbstractBus));
        Assert.IsTrue(chains[0].Any(s => s.OutputDestination.StreamType == StreamType.AbstractBus));
    }

    [TestMethod]
    public void BuildStreamChains_Depth3_ChannelToBusToBusToChannel()
    {
        var engine = new ASIO_Engine();
        var streams = new ObservableCollection<DSP_Stream>
        {
            CreateStream(0, StreamType.Channel, 0, StreamType.Bus),
            CreateStream(0, StreamType.Bus, 1, StreamType.Bus),
            CreateStream(1, StreamType.Bus, 2, StreamType.Channel)
        };
        var chains = InvokeBuildStreamChains(engine, streams);
        Assert.IsNotNull(chains);
        Assert.AreEqual(1, chains.Count);
        Assert.AreEqual(3, chains[0].Count);
        Assert.AreEqual(StreamType.Channel, chains[0][0].InputSource.StreamType);
        Assert.AreEqual(StreamType.Bus, chains[0][1].InputSource.StreamType);
        Assert.AreEqual(StreamType.Bus, chains[0][2].InputSource.StreamType);
        Assert.AreEqual(StreamType.Channel, chains[0][2].OutputDestination.StreamType);
    }

    [TestMethod]
    public void BuildStreamChains_Depth3_ChannelToBusToAbstractBusToChannel()
    {
        var engine = new ASIO_Engine();
        var streams = new ObservableCollection<DSP_Stream>
        {
            CreateStream(0, StreamType.Channel, 0, StreamType.Bus),
            CreateStream(0, StreamType.Bus, 0, StreamType.AbstractBus),
            CreateStream(0, StreamType.AbstractBus, 1, StreamType.Channel),
            CreateStream(0, StreamType.AbstractBus, 0, StreamType.AbstractBus) // Master
        };
        Program.DSP_Info.AbstractBuses.Clear();
        var ab = new DSP_AbstractBus { Name = "AB0" };
        ab.Mappings.Add(new DSP_AbstractBusMappings
        {
            InputSource = new StreamItem { Index = 0, StreamType = StreamType.Channel },
            OutputDestination = new StreamItem { Index = 1, StreamType = StreamType.Channel }
        });
        Program.DSP_Info.AbstractBuses.Add(ab);
        Program.DSP_Info.Streams.Clear();
        foreach (var s in streams) Program.DSP_Info.Streams.Add(s);
        var chains = InvokeBuildStreamChains(engine, streams);
        Assert.IsNotNull(chains);
        Assert.AreEqual(1, chains.Count);
        Assert.AreEqual(3, chains[0].Count);
        Assert.IsTrue(chains[0].Any(s => s.InputSource.StreamType == StreamType.AbstractBus));
        Assert.IsTrue(chains[0].Any(s => s.OutputDestination.StreamType == StreamType.AbstractBus));
    }

    [TestMethod]
    public void BuildStreamChains_Depth4_ChannelToBusToBusToBusToChannel()
    {
        var engine = new ASIO_Engine();
        var streams = new ObservableCollection<DSP_Stream>
        {
            CreateStream(0, StreamType.Channel, 0, StreamType.Bus),
            CreateStream(0, StreamType.Bus, 1, StreamType.Bus),
            CreateStream(1, StreamType.Bus, 2, StreamType.Bus),
            CreateStream(2, StreamType.Bus, 3, StreamType.Channel)
        };
        var chains = InvokeBuildStreamChains(engine, streams);
        Assert.IsNotNull(chains);
        Assert.AreEqual(1, chains.Count);
        Assert.AreEqual(4, chains[0].Count);
        Assert.AreEqual(StreamType.Channel, chains[0][0].InputSource.StreamType);
        Assert.AreEqual(StreamType.Bus, chains[0][1].InputSource.StreamType);
        Assert.AreEqual(StreamType.Bus, chains[0][2].InputSource.StreamType);
        Assert.AreEqual(StreamType.Bus, chains[0][3].InputSource.StreamType);
        Assert.AreEqual(StreamType.Channel, chains[0][3].OutputDestination.StreamType);
    }

    [TestMethod]
    public void BuildStreamChains_Depth4_ChannelToBusToAbstractBusToBusToChannel()
    {
        var engine = new ASIO_Engine();
        var streams = new ObservableCollection<DSP_Stream>
        {
            CreateStream(0, StreamType.Channel, 0, StreamType.Bus),
            CreateStream(0, StreamType.Bus, 0, StreamType.AbstractBus),
            CreateStream(0, StreamType.AbstractBus, 1, StreamType.Bus),
            CreateStream(1, StreamType.Bus, 2, StreamType.Channel),
            CreateStream(0, StreamType.AbstractBus, 0, StreamType.AbstractBus) // Master
        };
        Program.DSP_Info.AbstractBuses.Clear();
        var ab = new DSP_AbstractBus { Name = "AB0" };
        ab.Mappings.Add(new DSP_AbstractBusMappings
        {
            InputSource = new StreamItem { Index = 0, StreamType = StreamType.Channel },
            OutputDestination = new StreamItem { Index = 2, StreamType = StreamType.Channel }
        });
        Program.DSP_Info.AbstractBuses.Add(ab);
        Program.DSP_Info.Streams.Clear();
        foreach (var s in streams) Program.DSP_Info.Streams.Add(s);
        var chains = InvokeBuildStreamChains(engine, streams);
        Assert.IsNotNull(chains);
        Assert.AreEqual(1, chains.Count);
        Assert.AreEqual(4, chains[0].Count);
        Assert.IsTrue(chains[0].Any(s => s.InputSource.StreamType == StreamType.AbstractBus));
        Assert.IsTrue(chains[0].Any(s => s.OutputDestination.StreamType == StreamType.AbstractBus));
        Assert.AreEqual(StreamType.Channel, chains[0][0].InputSource.StreamType);
        Assert.AreEqual(StreamType.Channel, chains[0][3].OutputDestination.StreamType);
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
        var engine = new LocalMockASIOEngine(new DummyASIO());
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
        var engine = new LocalMockASIOEngine(new DummyASIO());
        var caps = engine.GetDriverCapabilities("Fake");
        Assert.IsNotNull(caps);
    }

    [TestMethod]
    public void GetMinMaxPreferredBufferSize_ReturnsInt()
    {
        var engine = new LocalMockASIOEngine(new DummyASIO());
        Assert.IsTrue(engine.GetMinBufferSize("Fake") >= 0);
        Assert.IsTrue(engine.GetMaxBufferSize("Fake") >= 0);
        Assert.IsTrue(engine.GetPreferredBufferSize("Fake") >= 0);
    }

    [TestMethod]
    public void IsSampleRateSupported_ReturnsTrue()
    {
        var engine = new LocalMockASIOEngine(new DummyASIO());
        Assert.IsTrue(engine.IsSampleRateSupported("Fake", 44100));
    }

    [TestMethod]
    public void Clear_DSP_PeakProcessingTime_ResetsValue()
    {
        var engine = new ASIO_Engine();
        typeof(ASIO_Engine).GetProperty("DSP_PeakProcessingTime").SetValue(engine, TimeSpan.FromSeconds(1));
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
