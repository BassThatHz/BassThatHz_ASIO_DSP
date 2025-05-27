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

    //[TestMethod]
    //public void Test_ASIO_Engine_IsFast()
    //{
    //    //Init Test Data
    //    int ChannelCount = 256;
    //    int SamplesPerBuffer = 512;

    //    int[][] inputBuffers = new int[ChannelCount][];
    //    int[][] outputBuffers = new int[ChannelCount][];
    //    IntPtr[] In_ints = new IntPtr[ChannelCount];
    //    IntPtr[] Out_ints = new IntPtr[ChannelCount];

    //    for (int i = 0; i < ChannelCount; i++)
    //    {
    //        inputBuffers[i] = new int[SamplesPerBuffer];
    //        outputBuffers[i] = new int[SamplesPerBuffer];

    //        for (int j = 0; j < SamplesPerBuffer; j++)
    //        {
    //            inputBuffers[i][j] = 1;
    //            outputBuffers[i][j] = 0; 
    //        }

    //        unsafe
    //        {
    //            fixed (int* inPtr = inputBuffers[i])
    //            fixed (int* outPtr = outputBuffers[i])
    //            {
    //                In_ints[i] = (IntPtr)inPtr;
    //                Out_ints[i] = (IntPtr)outPtr;
    //            }
    //        }
    //    }

    //    var inputDataAvailable = new ManualResetEventSlim(false);
    //    var outputDataAvailable = new ManualResetEventSlim(false);

    //    //Construct Mocks
    //    var Mock_ASIO_Driver = new Mock_ASIO_Unified(ChannelCount, SamplesPerBuffer);
    //    var Mock_ASIO = new Mock_ASIO_Engine(Mock_ASIO_Driver);

    //    //Run Timed Test
    //    Stopwatch StopWatch1 = new();
    //    Stopwatch StopWatch2 = new();
    //    StopWatch1.Start();

    //    Mock_ASIO.Start("FakedDriverName", 96000, ChannelCount, ChannelCount);

    //    double StartUpTime_TotalMilliseconds = StopWatch1.Elapsed.TotalMilliseconds;
    //    StopWatch1.Restart();

    //    Mock_ASIO_Driver.Mock_ActivateDataStream(In_ints, Out_ints, AsioSampleType.Int32LSB);

    //    double Cycle1_TotalMilliseconds = StopWatch1.Elapsed.TotalMilliseconds;
    //    StopWatch1.Stop();

    //    //Wired up Test AudioAvailable Event detection
    //    Mock_ASIO.InputDataAvailable += () =>
    //        inputDataAvailable.Set();
    //    Mock_ASIO.OutputDataAvailable += () =>
    //        outputDataAvailable.Set();

    //    StopWatch2.Start();

    //    Mock_ASIO_Driver.Mock_ActivateDataStream(In_ints, Out_ints, AsioSampleType.Int32LSB);
    //    bool inputReceived = inputDataAvailable.Wait(100);
    //    bool outputReceived = outputDataAvailable.Wait(100);

    //    double Cycle2_TotalNanoseconds = StopWatch2.Elapsed.TotalNanoseconds;
        
    //    StopWatch2.Stop();
    //    StopWatch1.Start();

    //    //Stop ASIO Engine
    //    Mock_ASIO.Stop();

    //    double StopTime_TotalMilliseconds = StopWatch1.Elapsed.TotalMilliseconds;
    //    StopWatch1.Stop();

    //    //Assertions
    //    Assert.IsTrue(inputReceived, "Input not Received");
    //    Assert.IsTrue(outputReceived, "Output not Received");

    //    //Assert StartUp Under 200ms performance
    //    Assert.IsTrue(StartUpTime_TotalMilliseconds < 200, "StartUp over 200ms");

    //    //Assert StopTime Under 200ms performance
    //    Assert.IsTrue(StopTime_TotalMilliseconds < 200, "StopTime over 200ms");

    //    //Assert Cycle1 Under 200ms performance
    //    Assert.IsTrue(Cycle1_TotalMilliseconds < 200, "Cycle1 over 200ms");

    //    //Assert Cycle2 Under 5ms performance
    //    Assert.IsTrue(Cycle2_TotalNanoseconds < 5000000, "Cycle2 over 5ms");
    //}

    [TestMethod]
    public void TestMethod1()
    {
        throw new NotImplementedException();
    }
}