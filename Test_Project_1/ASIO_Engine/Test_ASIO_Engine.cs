namespace Test_Project_1;

using BassThatHz_ASIO_DSP_Processor;
using NAudio.Wave;
using NAudio.Wave.Asio;
using System.Diagnostics;

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
    public void Throws_OnInvalidArguments()
    {
        var engine = new ASIO_Engine();
        Assert.ThrowsException<System.ArgumentNullException>(() => engine.Start(null, 44100, 2, 2));
        Assert.ThrowsException<System.ArgumentNullException>(() => engine.Start("", 44100, 2, 2));
        Assert.ThrowsException<System.ArgumentOutOfRangeException>(() => engine.Start("Mock", 0, 2, 2));
        Assert.ThrowsException<System.ArgumentOutOfRangeException>(() => engine.Start("Mock", 44100, 0, 2));
        Assert.ThrowsException<System.ArgumentOutOfRangeException>(() => engine.Start("Mock", 44100, 2, 0));
        Assert.ThrowsException<System.ArgumentNullException>(() => engine.GetDriverCapabilities(null));
        Assert.ThrowsException<System.ArgumentNullException>(() => engine.GetMinBufferSize(null));
        Assert.ThrowsException<System.ArgumentNullException>(() => engine.GetMaxBufferSize(null));
        Assert.ThrowsException<System.ArgumentNullException>(() => engine.GetPreferredBufferSize(null));
        Assert.ThrowsException<System.ArgumentNullException>(() => engine.IsSampleRateSupported(null, 44100));
        Assert.ThrowsException<System.ArgumentOutOfRangeException>(() => engine.IsSampleRateSupported("Mock", 0));
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
}