#nullable enable

namespace BassThatHz_ASIO_DSP_Processor;

#region Usings
using NAudio.Wave;
using NAudio.Wave.Asio;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
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
public class ASIO_Engine : IDisposable
{
    #region Variables

    #region Object References
    //Partially Unmanaged\Unsafe NAudio ASIO ole32 Com Object wrapper
    //protected ASIO? ASIO;
    protected IASIO_Unified? ASIO;

    //The current ASIO Data in the running DSP cycle
    protected AsioAudioAvailableEventArgs? DSP_ASIO_Data;
    #endregion

    #region Buffers
    //An jagged array of ASIO sample data from DSP_ASIO_Data as processed by NAudio
    public double[][] InputBuffer = new double[0][];
    public double[][] OutputBuffer = new double[0][];
    #endregion

    #region MultiThreading
    public int ASIO_THreadID = -1;
    //We run the DSP in a dedicated thread so that the UI-Thread isn't blocked by Task Waits\Thread Joins
    protected readonly Thread DSP_Thread;
    //Indirectly turns MT on/off, see On_ASIO_AudioAvailable()
    public bool IsMultiThreadingEnabled = true;
    //Indirectly processes the DSP in a background thread (instead of the UI thread.)
    public bool IsMT_BackgroundThreadEnabled = true;
    //If set to false the DSP will gracefully exit if DSP_RunOnce_ARE is signaled
    protected bool DSP_AllowedToRun = true;
    //Blocks threads from entering DSP_Thread when it is already running, Call Set to run one cycle of DSP 
    protected readonly AutoResetEvent DSP_RunOnce_ARE = new(false);
    //Signals when the DSP_Thread has completed one cycle of DSP, Calling WaitOne waits the caller
    protected readonly AutoResetEvent DSP_PassCompleted_ARE = new(false);
    //Holds an array of Tasks, one per stream of DSP processing that is running in parallel
    protected Task[]? StreamTaskList = null;
    #endregion

    #region Data Events
    public event InputDataAvailableHandler? InputDataAvailable;
    public delegate void InputDataAvailableHandler();

    public event OutputDataAvailableHandler? OutputDataAvailable;
    public delegate void OutputDataAvailableHandler();
    #endregion

    #region Driver State Change Events
    public event Action Driver_ResetRequest = delegate {};
    public event Action Driver_BufferSizeChanged = delegate { };
    public event Action Driver_ResyncRequest = delegate { };
    public event Action Driver_LatenciesChanged = delegate { };
    public event Action Driver_Overload = delegate { };
    public event Action Driver_SampleRateChanged = delegate { };
    #endregion

    #region Misc
    //Holds a list of channelIndexes to clear in a ThreadSafe way
    protected ConcurrentStack<int> ChannelClearRequests = new();
    #endregion

    #endregion

    #region Properties

    #region States and Defaults
    public string DeviceName { get; protected set; } = "Device Not Found"; //The active ASIO device name
    public int NumberOf_IO_Channels_Default { get; protected set; } = 1; //mono is a safe default
    public int NumberOf_Input_Channels { get; protected set; } = 1; //In and Out must be the same (for now)
    public int NumberOf_Output_Channels { get; protected set; } = 1; //In and Out must be the same (for now)
    public int NumberOf_IO_Channels_Total => this.NumberOf_Input_Channels + this.NumberOf_Output_Channels;

    public int SampleRate_Default { get; protected set; } = 44100; //44.1k is a pretty safe default
    public int SampleRate_Current { get; protected set; } = 44100; //There is a function to set desired SampleRate

    public int SamplesPerChannel { get; protected set; } = 1; //This default value gets overwritten on ASIO start

    public double InputMasterVolume { get; set; } = 0.1f; //Default is -20db
    public double OutputMasterVolume { get; set; } = 0.1f; //Default is -20db

    #endregion

    #region DSP Delay Stats
    public Stopwatch DSP_ProcessingTime { get; protected set; } = new();
    public TimeSpan DSP_PeakProcessingTime { get; protected set; }

    public Stopwatch InputBufferConversion_ProcessingTime { get; protected set; } = new();

    public Stopwatch OutputBufferConversion_ProcessingTime { get; protected set; } = new();

    public double BufferSize_Latency_ms { get; protected set; }

    public int Underruns => Underruns_Counter;
    protected int Underruns_Counter = 0;
    #endregion

    #region ASIO Info
    public AsioDriverCapability? DriverCapabilities
    {
        get
        {
            return this.ASIO?.GetDriverCapabilities;
        }
    }

    public bool? IsSampleRateSupported(int sampleRate) =>
                    this.ASIO?.IsSampleRateSupported(sampleRate);
    #endregion

    #endregion

    #region Constructor / Dispose
    public ASIO_Engine()
    {
        //Create the DSP Thread / DSP Callback
        this.DSP_Thread = new Thread(new ThreadStart(this.DSP_ManualBackgroundThread))
        {
            IsBackground = true,
            Priority = ThreadPriority.Highest
        };
        //Pre-start the thread, it ARE.WaitOne() "sleeps" when started
        this.DSP_Thread.Start();
    }

    ~ASIO_Engine()
    {
        this.Dispose();
    }
    public void Dispose()
    {
        try
        {
            //ASIO uses unmanaged Windows OLE com sub-system, we have to dispose it
            this.ASIO?.Dispose();
            this.ASIO = null;

            //Gracefully ask the DSP Thread to exit
            this.DSP_AllowedToRun = false;
            _ = this.DSP_RunOnce_ARE.Set();

            Thread.Sleep(50); //Give the DSP Thread time to exit gracefully
            if (this.DSP_Thread.IsAlive) //If it's still running at this point, we hard abort it
            {
                //we don't care about Thread errors, we are closing down
                try
                {
                    this.DSP_Thread.Interrupt();
                }
                catch (Exception ex)
                {
                    _ = ex;
                }
            }

            GC.SuppressFinalize(this);
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }
    #endregion

    #region Public Functions

    #region ClearOutputBuffer
    /// <summary>
    /// This mutes the output on a given output channel.
    /// Call this when the stream is changing assigned output channels
    /// to clear audio data from the assumed-abandoned previous output stream.
    /// Without calling this the last audio data just loops around fed into ASIO.
    /// </summary>
    /// <param name="channelIndex">The index of the channel to clear</param>
    public void RequestClearedOutputBuffer(int channelIndex)
    {
        if (channelIndex > 0 && channelIndex < this.OutputBuffer?.Length)
            this.ChannelClearRequests.Push(channelIndex);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void ClearRequestedOutputBuffers()
    {
        var Local_ChannelClearRequests = this.ChannelClearRequests;
        if (!Local_ChannelClearRequests.IsEmpty)
        {
            while (!Local_ChannelClearRequests.IsEmpty)
            {
                if (Local_ChannelClearRequests.TryPop(out var channelIndex))
                {
                    var Local_OutputBuffer = this.OutputBuffer[channelIndex];
                    Array.Clear(Local_OutputBuffer, 0, Local_OutputBuffer.Length);
                }
                else
                    break;
            }
            Local_ChannelClearRequests.Clear();
        }
    }
    #endregion

    #region Stop / CleanUp ASIO
    /// <summary>
    /// Stops ASIO by disposing it
    /// </summary>
    public void Stop()
    {
        this.Stop_ASIO();
    }

    /// <summary>
    /// Attempts to gracefully stop ASIO then disposes it
    /// </summary>
    public void CleanUp()
    {
        this.CleanUp_ASIO();
    }
    #endregion

    #region Start ASIO
    /// <summary>
    /// Starts the ASIO DSP engine
    /// </summary>
    /// <param name="asio_Device_Name">The ASIO device name.</param>
    /// <param name="sampleRate">The requested sampling rate.</param>
    /// <param name="numberOf_IO_Channels">The request number of IO channels. In/Out count must match.</param>
    public void Start(string asio_Device_Name, int sampleRate, int numberOf_Input_Channels, int numberOf_Output_Channels)
    {
        this.Start_ASIO(asio_Device_Name, sampleRate, numberOf_Input_Channels, numberOf_Output_Channels);
    }
    #endregion

    #region Show ASIO Control Panel
    /// <summary>
    /// Shows ASIO Control Panel for the active ASIO stream
    /// </summary>
    public void Show_ControlPanel()
    {
        this.Show_ASIO_ControlPanel();
    }

    /// <summary>
    /// Shows ASIO Control Panel for a given ASIO Device
    /// </summary>
    /// <param name="deviceName"></param>
    public void Show_ControlPanel(string deviceName)
    {
        this.Show_ASIO_ControlPanel(deviceName);
    }
    #endregion

    #region ASIO Info / Stats

    /// <summary>
    /// Gets a list of ASIO Driver names
    /// </summary>
    /// <returns>A string of ASIO Driver names</returns>
    public string[] GetDriverNames()
    {
        IASIO_GetDriverNames ASIO_GetDriverNames = new ASIO_GetDriverNames();
        return ASIO_GetDriverNames.GetDriverNames();
    }

    /// <summary>
    /// returns Tuple: int InputLatency, int OutputLatency
    /// </summary>
    public Tuple<int, int>? PlaybackLatency => this.ASIO?.PlaybackLatency;

    /// <summary>
    /// Gets the ASIO device's Capabilities.
    /// </summary>
    /// <param name="asioDeviceName"></param>
    /// <returns></returns>
    public AsioDriverCapability GetDriverCapabilities(string asioDeviceName)
    {
        if (string.IsNullOrEmpty(asioDeviceName))
            throw new ArgumentNullException(nameof(asioDeviceName));

        AsioDriverCapability ReturnValue = default;
        using var temp_ASIO = new ASIO_Unified(asioDeviceName);
            if (temp_ASIO != null)
                ReturnValue = temp_ASIO.GetDriverCapabilities;
        return ReturnValue;
    }

    /// <summary>
    /// Gets the Minimum BufferSize the ASIO Device supports
    /// </summary>
    /// <param name="asioDeviceName"></param>
    /// <returns></returns>
    public int GetMinBufferSize(string asioDeviceName)
    {
        if (string.IsNullOrEmpty(asioDeviceName))
            throw new ArgumentNullException(nameof(asioDeviceName));

        int ReturnValue = 0;
        using var temp_ASIO = new ASIO_Unified(asioDeviceName);
            if (temp_ASIO != null)
                ReturnValue = (int)temp_ASIO.GetDriverCapabilities.BufferMinSize;

        return ReturnValue;
    }

    /// <summary>
    /// Gets the Maximum BufferSize the ASIO Device supports
    /// </summary>
    /// <param name="asioDeviceName"></param>
    /// <returns></returns>
    public int GetMaxBufferSize(string asioDeviceName)
    {
        if (string.IsNullOrEmpty(asioDeviceName))
            throw new ArgumentNullException(nameof(asioDeviceName));

        int ReturnValue = 0;
        using var temp_ASIO = new ASIO_Unified(asioDeviceName);
            if (temp_ASIO != null)
                ReturnValue = (int)temp_ASIO.GetDriverCapabilities.BufferMaxSize;
        return ReturnValue;
    }

    /// <summary>
    /// Gets the Preffered BufferSize the ASIO Device supports
    /// </summary>
    /// <param name="asioDeviceName"></param>
    /// <returns></returns>
    public int GetPreferredBufferSize(string asioDeviceName)
    {
        if (string.IsNullOrEmpty(asioDeviceName))
            throw new ArgumentNullException(nameof(asioDeviceName));

        int ReturnValue = 0;
        using var temp_ASIO = new ASIO_Unified(asioDeviceName);
            if (temp_ASIO != null)
                ReturnValue = (int)temp_ASIO.GetDriverCapabilities.BufferPreferredSize;
        return ReturnValue;
    }

    /// <summary>
    /// Checks if an ASIO Devices supports a SampleRate
    /// </summary>
    /// <param name="asioDeviceName">The ASIO device to check</param>
    /// <param name="sampleRate">The samplerate in hz</param>
    /// <returns></returns>
    public bool IsSampleRateSupported(string asioDeviceName, int sampleRate)
    {
        if (string.IsNullOrEmpty(asioDeviceName))
            throw new ArgumentNullException(nameof(asioDeviceName));

        if (sampleRate < 1)
            throw new ArgumentOutOfRangeException(nameof(sampleRate), "sampleRate must be a postive number.");

        bool ReturnValue = false;
        using var temp_ASIO = new ASIO_Unified(asioDeviceName);
            if (temp_ASIO != null)
                ReturnValue = temp_ASIO.IsSampleRateSupported(sampleRate);
        return ReturnValue;
    }

    public void Clear_DSP_PeakProcessingTime()
    {
        this.DSP_PeakProcessingTime = TimeSpan.Zero;
    }

    public void Clear_UnderrunsCounter()
    {
        this.Underruns_Counter = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double[]? GetInputAudioData(int channelIndex)
    {
        //We don't check that the parameter is in range for performance reasons.
        //It will throw an exception if the calling logic isn't valid, it just won't be an ArgException
        return this.InputBuffer?[channelIndex].ToArray();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double[]? GetOutputAudioData(int channelIndex)
    {
        //We don't check that the parameter is in range for performance reasons.
        //It will throw an exception if the calling logic isn't valid, it just won't be an ArgException
        return this.OutputBuffer?[channelIndex].ToArray();
    }
    #endregion

    #endregion

    #region Protected Functions

    #region ASIO Start
    protected void Start_ASIO(string asio_Device_Name, int sampleRate, int numberOf_Input_Channels, int numberOf_Output_Channels)
    {
        if (numberOf_Input_Channels < 1)
            throw new ArgumentOutOfRangeException(nameof(numberOf_Input_Channels), "numberOf_Input_Channels must be a postive number.");

        if(numberOf_Output_Channels < 1)
            throw new ArgumentOutOfRangeException(nameof(numberOf_Output_Channels), "numberOf_Output_Channels must be a postive number.");

        if (sampleRate < 1)
            throw new ArgumentOutOfRangeException(nameof(sampleRate), "sampleRate must be a postive number.");

        if (String.IsNullOrEmpty(asio_Device_Name))
            throw new ArgumentNullException(nameof(asio_Device_Name));

        this.SampleRate_Current = sampleRate;
        this.NumberOf_Input_Channels = numberOf_Input_Channels;
        this.NumberOf_Output_Channels = numberOf_Output_Channels;
        this.DeviceName = asio_Device_Name;
        this.CleanUp_ASIO();

        // Create or Re-create ASIO device as necessary
        if (this.ASIO == null)
        {
            this.ASIO = this.Get_New_ASIO_Instance(asio_Device_Name);

            //Wire up the ASIO events
            this.WireUpASIO_Events();

            this.DSP_PeakProcessingTime = TimeSpan.Zero;
            this.Underruns_Counter = 0;
            var InputOffset = 0; var OutputOffset = 0; //Unused
            this.ASIO.Init(this.NumberOf_Input_Channels, this.NumberOf_Output_Channels, this.SampleRate_Current, OutputOffset, InputOffset);

            //Create the Input and Output buffers (default HW size * number of channels)
            this.SamplesPerChannel = this.ASIO.SamplesPerBuffer;
            this.BufferSize_Latency_ms = (double)SamplesPerChannel / (double)SampleRate_Current * 1000;

            //For performance reasons, only create the arrays once!
            this.InputBuffer = new double[this.NumberOf_Input_Channels][];
            for (var i = 0; i < this.NumberOf_Input_Channels; i++)
                this.InputBuffer[i] = new double[this.SamplesPerChannel];

            this.OutputBuffer = new double[this.NumberOf_Output_Channels][];
            for (var i = 0; i < this.NumberOf_Output_Channels; i++)
                this.OutputBuffer[i] = new double[this.SamplesPerChannel];
        }
        this.ASIO?.Start();
    }

    /// <summary>
    /// Function that gets a new instance of an intiated ASIO driver connector that is overridable
    /// </summary>
    /// <param name="asio_Device_Name">the registered ASIO Device name</param>
    /// <returns>a new instance of an intiated ASIO driver connector</returns>
    protected virtual IASIO_Unified Get_New_ASIO_Instance(string asio_Device_Name)
    {
        return new ASIO_Unified(asio_Device_Name);
    }

    protected void WireUpASIO_Events()
    {
        if (this.ASIO != null)
        {
            this.ASIO.AudioAvailable += this.On_ASIO_AudioAvailable;

            //All of the following are Stop Events
            this.ASIO.Driver_BufferSizeChangedCallback = () =>
            {
                this.Stop();
                this.Driver_BufferSizeChanged.Invoke();
            };
            this.ASIO.Driver_LatenciesChangedCallback = () =>
            {
                this.Stop();
                this.Driver_LatenciesChanged.Invoke();
            };
            this.ASIO.Driver_ResetRequestCallback = () =>
            {
                this.Stop();
                this.Driver_ResetRequest.Invoke();
            };
            this.ASIO.Driver_ResyncRequestCallback = () =>
            {
                this.Stop();
                this.Driver_ResyncRequest.Invoke();
            };
            this.ASIO.Driver_OverloadCallback = () =>
            {
                this.Stop();
                this.Driver_Overload.Invoke();
            };
            this.ASIO.Driver_SampleRateChangedCallback = () =>
            {
                this.Stop();
                this.Driver_SampleRateChanged.Invoke();
            };
        }
    }
    #endregion

    #region On_ASIO_AudioAvailable
    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    protected void On_ASIO_AudioAvailable(object? sender, AsioAudioAvailableEventArgs e)
    {
        //Assumes InputBuffer and OutputBuffer are pre-initialized for performance reasons
        
        //We can't log exceptions any, the frequency is too high.
        //Just allow the run-time to hard abort. Debug.cs has first chance and last chance handlers for debugging all errors. Put break points there.

        //Stats init
        this.ASIO_THreadID = Thread.CurrentThread.ManagedThreadId;
        this.DSP_ProcessingTime.Reset();
        this.InputBufferConversion_ProcessingTime.Reset();
        this.OutputBufferConversion_ProcessingTime.Reset();
        this.DSP_ProcessingTime.Start();

        this.DSP_ASIO_Data = e; //Pass the ASIO data to the DSP thread               
        if (this.IsMT_BackgroundThreadEnabled) //WaitAll()'s in a background thread
        {
            _ = this.DSP_RunOnce_ARE.Set(); //Run one pass of the DSP
            _ = this.DSP_PassCompleted_ARE.WaitOne(); //Wait until the DSP is done
        }
        else
        {
            if (this.IsMultiThreadingEnabled)
                this.DSP_MultiThreaded(); //WaitAll()'s on the UI thread directly.
            else
                this.DSP_SingleThreaded(); //ST on the UI thread directly.
        }

        //Process any queued Clear Output Buffer requests
        this.ClearRequestedOutputBuffers();

        //Allows any event listeners to get to be notified of Data Availability
        _ = Task.Run(() =>
        {
            this.InputDataAvailable?.Invoke();
            this.OutputDataAvailable?.Invoke();
        });

        //Stats
        this.DSP_ProcessingTime.Stop();
        if (this.DSP_PeakProcessingTime < this.DSP_ProcessingTime.Elapsed)
            this.DSP_PeakProcessingTime = this.DSP_ProcessingTime.Elapsed;

        //Underrun Detection, can produce false positives because .Net's clock isn't very precise (not sure if there is a better way)
        if (this.DSP_ProcessingTime.Elapsed.TotalNanoseconds * 0.000001d > this.BufferSize_Latency_ms)
            this.Underruns_Counter++;
    }
    #endregion

    #region DSP Init / Header / Multi-Threading

    #region Single Threaded
    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    protected void DSP_SingleThreaded()
    {
        if (this.DSP_ASIO_Data == null) //Nothing to process
            return;           

        this.InputBufferConversion_ProcessingTime.Start();
        //Assumes InputBuffer and OutputBuffer are pre-initialized for performance reasons
        //Get the ASIO input stream
        this.DSP_ASIO_Data.GetAsJaggedSamples(this.InputBuffer);
        this.InputBufferConversion_ProcessingTime.Stop();

        var DSP_Streams = Program.DSP_Info.Streams;
        int StreamCount = DSP_Streams.Count;
        if (StreamCount > 0)
        {
            try
            {
                //Process each channel sequentially (slow version)
                for (int StreamIndex = 0; StreamIndex < StreamCount; StreamIndex++)
                    this.DSP_Process_Channel(DSP_Streams[StreamIndex]);
            }
            catch (Exception ex)
            {
                //We don't care if these two exceptions occur. It often happens because the user is 
                //deleting or adding streams while the DSP is on. The remaining audio data will just be muted zeros for this block.
                //Adding an object lock would just slow things down and prevent multi-threading scalability.
                if (ex is not IndexOutOfRangeException && ex is not ArgumentOutOfRangeException)
                    throw; //Throws all the remaining valid errors with stack trace info

                //We can't log these errors, the frequency is too high. Just allow the run-time to hard abort.
            }
        }

        this.OutputBufferConversion_ProcessingTime.Start();
        //Send OutputBuffer to ASIO Output stream
        this.DSP_ASIO_Data.SetAsJaggedSamples(this.OutputBuffer);
        this.OutputBufferConversion_ProcessingTime.Stop();
    }
    #endregion

    #region Multi-Threaded
    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    protected void DSP_MultiThreaded()
    {
        if (this.DSP_ASIO_Data == null) //Nothing to process
            return;

        this.InputBufferConversion_ProcessingTime.Start();
        //Assumes InputBuffer and OutputBuffer are pre-initialized for performance reasons
        //Get the ASIO input stream
        this.DSP_ASIO_Data.GetAsJaggedSamples(this.InputBuffer);
        this.InputBufferConversion_ProcessingTime.Stop();

        var DSP_Streams = Program.DSP_Info.Streams;
        int StreamCount = DSP_Streams.Count;
        if (StreamCount > 0)
        {
            try
            {
                this.StreamTaskList = new Task[StreamCount];
                //Use AutoThreading via Task ThreadPool scheduler, one Task per stream
                for (int StreamIndex = 0; StreamIndex < StreamCount; StreamIndex++)
                {
                    var Stream = DSP_Streams[StreamIndex];
                    this.StreamTaskList[StreamIndex] = Task.Run(() =>
                                                        this.DSP_Process_Channel(Stream)
                                                    );
                }

                //Wait for the threads to complete DSPing all channels (up-to a max of 500ms)
                //Remaining data stream will be muted zeroes if it doesn't complete in time. Rather than freezing the owner thread.
                if (this.StreamTaskList.Length > 0)
                    _ = Task.WaitAll(this.StreamTaskList, 500);
            }
            catch (Exception ex)
            {
                //We don't care if these two exceptions occur. It often happens because the user is 
                //deleting or adding streams while the DSP is on. The remaining audio data will just be muted zeros for this block.
                //Adding an object lock would just slow things down and prevent multi-threading scalability.
                if (ex is not IndexOutOfRangeException && ex is not ArgumentOutOfRangeException)
                    throw; //Throws all the remaining valid errors with stack trace info

                //We can't log these errors, the frequency is too high. Just allow the run-time to hard abort.
            }
        }

        this.OutputBufferConversion_ProcessingTime.Start();
        //Send OutputBuffer to ASIO Output stream
        this.DSP_ASIO_Data.SetAsJaggedSamples(this.OutputBuffer);
        this.OutputBufferConversion_ProcessingTime.Stop();
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    protected void DSP_ManualBackgroundThread()
    {
        try
        {
            while (true) //Keep-alive
            {
                _ = this.DSP_RunOnce_ARE.WaitOne(); //Pause the thread until signaled
                if (!this.DSP_AllowedToRun) //Check if we should run
                    break; //Breaks out of keep-alive loop which ends the long-running background thread cleanly

                if (this.IsMultiThreadingEnabled)
                    this.DSP_MultiThreaded(); //MT on the background thread
                else
                    this.DSP_SingleThreaded(); //ST on the background thread

                _ = this.DSP_RunOnce_ARE.Reset(); //Tell the thread it is ready to pause
                _ = this.DSP_PassCompleted_ARE.Set(); //Signal that we are done
            }
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }
    #endregion

    #endregion

    #region DSP Processing

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    protected void DSP_Process_Channel(DSP_Stream CurrentStream)
    {
        //Function must be thread-safe

        //Make sure the Stream and Buffers and Channel Index are legit, otherwise return (i.e. output buffer is muted zeroes)
        if (
            CurrentStream == null
            ||
            this.OutputBuffer == null || CurrentStream.OutputChannelIndex == -1
            ||
            this.InputBuffer == null || CurrentStream.InputChannelIndex == -1
            )
            return; //Should probably throw Arg Exception instead. Caller provided an invalid stream.

        double[] Local_OutputBuffer = this.OutputBuffer[CurrentStream.OutputChannelIndex];
        double[] Local_InputBuffer = this.InputBuffer[CurrentStream.InputChannelIndex];

        #region Init
        int ChannelFilterCount = CurrentStream.Filters.Count;
        double Local_InputVolumeGain = this.InputMasterVolume * CurrentStream.InputVolume;
        double Local_OutputVolumeGain = this.OutputMasterVolume * CurrentStream.OutputVolume;
        int Local_SamplesPerChannel = this.SamplesPerChannel;
        IFilter? CurrentFilter;
        #endregion

        //Apply the InputMasterVolume and StreamInputVolume
        for (var SampleIndex = 0; SampleIndex < Local_SamplesPerChannel; SampleIndex++)
        //_ = Parallel.For(0, Local_SamplesPerChannel, (SampleIndex) =>
            //Make a byval copy of the sample value as array elements are byref and that
            //would couple ASIO output to ASIO input array (a bad thing!)
            Local_OutputBuffer[SampleIndex] = (double)(Local_InputVolumeGain * Local_InputBuffer[SampleIndex]);
        //);

        try
        {
            //Apply every DSP filter that exists (if any) in the stream to the samples
            for (int FilterIndex = 0; FilterIndex < ChannelFilterCount; FilterIndex++)
            {
                CurrentFilter = CurrentStream.Filters[FilterIndex];
                if (CurrentFilter is null || !CurrentFilter.FilterEnabled)
                    continue;

                //Processes a whole block of input channel samples
                Local_OutputBuffer = CurrentFilter.Transform(Local_OutputBuffer, CurrentStream);
            }
        }
        catch (Exception ex)
        {
            //We don't care if these two exceptions occur. It often happens because the user is 
            //deleting or adding streams while the DSP is on. The remaining audio data will just be muted zeros for this block.
            //Adding an object lock would just slow things down and prevent multi-threading scalability.
            if (ex is not IndexOutOfRangeException && ex is not ArgumentOutOfRangeException)
                throw; //Throws all the remaining valid errors with stack trace info

            //We can't log these errors, the frequency is too high. Just allow the run-time to hard abort.
        }

        //Apply the OutputMasterVolume and StreamOutputVolume
        for (var SampleIndex = 0; SampleIndex < Local_SamplesPerChannel; SampleIndex++)
        //_ = Parallel.For(0, Local_SamplesPerChannel, (SampleIndex) =>
            //Apply the stream Output Volume and master volume to the sample
            Local_OutputBuffer[SampleIndex] *= Local_OutputVolumeGain;
        //);
    }
    #endregion

    #region ASIO Control Panel
    protected void Show_ASIO_ControlPanel()
    {
        if (string.IsNullOrEmpty(this.DeviceName))
            throw new InvalidOperationException("DeviceName isn't set");

        using var asio = new ASIO_Unified(this.DeviceName);
        asio.ShowControlPanel();
    }

    protected void Show_ASIO_ControlPanel(string deviceName)
    {
        if (string.IsNullOrEmpty(deviceName))
            throw new ArgumentNullException(nameof(deviceName));

        using var asio = new ASIO_Unified(deviceName);
        asio.ShowControlPanel();
    }
    #endregion

    #region ASIO Stop / CleanUp
    protected void Stop_ASIO()
    {
        //Hard stop
        this.ASIO?.Dispose();
        this.ASIO = null;
    }

    protected void CleanUp_ASIO()
    {
        // allow change device
        if (this.ASIO != null)
        {
            this.ASIO.Stop();
            this.ASIO.AudioAvailable -= this.On_ASIO_AudioAvailable;
            this.ASIO.Dispose();
            this.ASIO = null;
        }
    }
    #endregion

    #endregion

    #region Error Handling
    protected void Error(Exception ex)
    {
        Debug.Error(ex);
    }
    #endregion
}