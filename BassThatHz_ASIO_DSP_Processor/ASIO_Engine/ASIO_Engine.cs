#nullable enable

namespace BassThatHz_ASIO_DSP_Processor;

#region Usings
using NAudio.Wave;
using NAudio.Wave.Asio;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage.Streams;
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

    #region States
    // Persists the DSP Processing chains across calls.
    protected List<List<DSP_Stream>> ChainCache = new();
    // This cache will prevent AbstractBus master re-cloning if the upstream chain path hasn’t changed.
    protected Dictionary<(int abIndex, string chainSignature), DSP_Stream> AbstractBusCloneCache = new();
    //Uniquely identifies active AbstractBus clones
    protected HashSet<(int abIndex, string signature)> UsedCloneKeys = new HashSet<(int, string)>();
    #endregion

    #region Buffers
    //An jagged array of ASIO sample data from DSP_ASIO_Data as processed by NAudio
    public double[][] InputBuffer = [];
    public double[][] OutputBuffer = [];
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
    public event Action Driver_ResetRequest = delegate { };
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
        var ASIO_GetDriverNames = new ASIO_GetDriverNames();
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
        return this.InputBuffer == null || channelIndex < 0 || channelIndex >= this.InputBuffer.Length
            ? null
            : (this.InputBuffer[channelIndex]?.ToArray());
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double[]? GetOutputAudioData(int channelIndex)
    {
        return this.OutputBuffer == null || channelIndex < 0 || channelIndex >= this.OutputBuffer.Length
            ? null
            : (this.OutputBuffer?[channelIndex].ToArray());
    }
    #endregion

    #endregion

    #region Protected Functions

    #region ASIO Start
    protected void Start_ASIO(string asio_Device_Name, int sampleRate, int numberOf_Input_Channels, int numberOf_Output_Channels)
    {
        if (numberOf_Input_Channels < 1)
            throw new ArgumentOutOfRangeException(nameof(numberOf_Input_Channels), "numberOf_Input_Channels must be a postive number.");

        if (numberOf_Output_Channels < 1)
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
        this.CleanUp_StreamCaches();

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
        this.ASIO_THreadID = Environment.CurrentManagedThreadId;
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

    #region Group and Chain Streams and Buses as-needed

    protected DSP_Stream CloneAbstractBusStream(DSP_Stream original)
    {
        DSP_Stream clone = CommonFunctions.DeepClone<DSP_Stream>(original);
        clone.AbstractBusBuffer = new double[this.SamplesPerChannel];
        return clone;
    }

    // ----- Build Reverse Adjacency Map -----
    // We now want to traverse upstream. For each stream we use its OutputDestination
    // as the key so that for a given stream’s InputSource we can find any upstream stream whose
    // OutputDestination matches.
    protected Dictionary<IStreamItem, List<DSP_Stream>> BuildReverseAdjacencyMap(List<DSP_Stream> streams)
    {
        var reverseAdjacency = new Dictionary<IStreamItem, List<DSP_Stream>>();
        for (int i = 0; i < streams.Count; i++)
        {
            var s = streams[i];
            if (s == null)
                continue;

            var key = s.OutputDestination;
            if (!reverseAdjacency.TryGetValue(key, out var list))
            {
                list = new List<DSP_Stream>();
                reverseAdjacency[key] = list;
            }
            list.Add(s);
        }
        return reverseAdjacency;
    }

    // ----- Identify AbstractBus Master Streams -----
    /// <summary>
    /// Finds the single "master" stream for each AbstractBus index.
    /// A master is defined as a stream whose InputSource and OutputDestination are
    /// both of type AbstractBus and have the same index.
    /// </summary>
    protected Dictionary<int, DSP_Stream> GetAbstractBusMasters(ObservableCollection<DSP_Stream> allStreams)
    {
        var result = new Dictionary<int, DSP_Stream>();
        var duplicates = new HashSet<int>();

        for (int i = 0; i < allStreams.Count; i++)
        {
            var s = allStreams[i];
            if (s != null && s.InputSource.StreamType == StreamType.AbstractBus &&
                s.OutputDestination.StreamType == StreamType.AbstractBus &&
                s.InputSource.Index == s.OutputDestination.Index)
            {
                int abIndex = s.InputSource.Index;
                if (!result.ContainsKey(abIndex))
                {
                    result[abIndex] = s;
                }
                else
                {
                    // Duplicate master found; mark for exclusion.
                    duplicates.Add(abIndex);
                }
            }
        }

        // Remove duplicate masters.
        var duplicateList = duplicates.ToList();
        for (int i = 0; i < duplicateList.Count; i++)
        {
            result.Remove(duplicateList[i]);
        }

        return result;
    }

    // ----- Filter Out Invalid Streams -----
    protected List<DSP_Stream> GetValidStreams(ObservableCollection<DSP_Stream> allStreams, Dictionary<int, DSP_Stream> abstractBusMasters)
    {
        var valid = new List<DSP_Stream>();
        var busProduced = new HashSet<int>();

        for (int i = 0; i < allStreams.Count; i++)
        {
            var stream = allStreams[i];
            if (stream == null || stream.InputSource == null || stream.OutputDestination == null)
                continue;

            // Check AbstractBus usage (if not a master) that the master exists.
            bool isMaster = stream.InputSource.StreamType == StreamType.AbstractBus &&
                            stream.OutputDestination.StreamType == StreamType.AbstractBus &&
                            stream.InputSource.Index == stream.OutputDestination.Index;

            bool hasAbstractBusIn = stream.InputSource.StreamType == StreamType.AbstractBus;
            bool hasAbstractBusOut = stream.OutputDestination.StreamType == StreamType.AbstractBus;

            bool hasBusIn = stream.InputSource.StreamType == StreamType.Bus;
            bool hasBusOut = stream.OutputDestination.StreamType == StreamType.Bus;

            // If the stream references an AbstractBus on only one side, check for a master.
            if (!isMaster && hasAbstractBusIn ^ hasAbstractBusOut)
            {
                int abIndex = hasAbstractBusIn ? stream.InputSource.Index : stream.OutputDestination.Index;
                if (!abstractBusMasters.ContainsKey(abIndex))
                    continue; // Exclude this stream if no master is found.
            }

            // Enforce that a Bus can be produced only once.
            if (hasBusOut)
            {
                int busIndex = stream.OutputDestination.Index;
                if (busProduced.Contains(busIndex))
                    continue; // Already produced by another stream.
                busProduced.Add(busIndex);
            }
            // For AbstractBus masters and usages, multiple productions are allowed.

            valid.Add(stream);
        }
        return valid;
    }

    // ----- Build Raw Chains -----
    protected List<List<DSP_Stream>> BuildRawChains_Reversed(ObservableCollection<DSP_Stream> allStreams)
    {
        // 1. Identify AbstractBus masters.
        var abMasters = this.GetAbstractBusMasters(allStreams);

        // 2. Filter out misconfigured streams.
        var validStreams = this.GetValidStreams(allStreams, abMasters);

        // 3. Candidate endpoints: streams whose OutputDestination is a Channel.
        var endStreams = validStreams
                 .Where(s => s.OutputDestination.StreamType == StreamType.Channel)
                 .OrderBy(s => s.InputSource.StreamType == StreamType.Channel ? 0 :
                               s.InputSource.StreamType == StreamType.AbstractBus ? 1 :
                               s.InputSource.StreamType == StreamType.Bus ? 2 : 3)
                 .ToList();

        var rawChains = new List<List<DSP_Stream>>();

        // 4. Process each candidate endpoint.
        for (int i = 0; i < endStreams.Count; i++)
        {
            var end = endStreams[i];
            if (end == null)
                continue;

            var chain = new List<DSP_Stream>();
            DSP_Stream current = end;
            bool chainIsValid = true;
            bool done = false;

            // Build the chain in reverse: from endpoint back to start.
            while (!done)
            {
                chain.Add(current);

                switch (current.InputSource.StreamType)
                {
                    case StreamType.Channel:
                        // Reached a start stream.
                        done = true;
                        break;

                    case StreamType.Bus:
                        {
                            // For Bus, find the stream that produces it.
                            var ExistingFeeder = rawChains.Any(rc =>
                                rc.Any(c=> c.OutputDestination.Equals(current.InputSource)));
                            if (ExistingFeeder)
                            {
                                chainIsValid = true;
                                done = true;
                                break;
                            }

                            var linkStream = validStreams.FirstOrDefault(s =>
                                s.OutputDestination.Equals(current.InputSource));
                            if (linkStream == null)
                            {
                                chainIsValid = false;
                                done = true;
                            }
                            else
                            {
                                current = linkStream;
                            }
                        }
                        break;

                    case StreamType.AbstractBus:
                        {
                            // If at least one AbstractBus master exists, use its mapping.
                            if (abMasters.Count > 0)
                            {
                                //var masterIndex = abMasters.First().Value.InputSource.Index;
                                var masterAbstractBus = Program.DSP_Info.AbstractBuses[current.InputSource.Index];
                                
                                // Find a mapping where the mapping's OutputDestination matches the current InputSource.
                                var validMapping = masterAbstractBus.Mappings.FirstOrDefault(m =>
                                    Program.DSP_Info.Streams[m.OutputDestination.Index].OutputDestination.Index == 
                                    current.OutputDestination.Index);
                                if (validMapping == null)
                                {
                                    chainIsValid = false;
                                    done = true;
                                }
                                else
                                {
                                    // Look for the upstream stream whose OutputDestination equals the mapping's InputSource.
                                    var upstreamStream = Program.DSP_Info.Streams[validMapping.InputSource.Index];
                                    if (upstreamStream == null)
                                    {
                                        chainIsValid = false;
                                        done = true;
                                    }
                                    else
                                    {
                                        var AbstractMasterStream = abMasters.FirstOrDefault(
                                                        ab => ab.Value.InputSource.Index == current.InputSource.Index).Value;
                                        
                                        chain.Add(AbstractMasterStream);
                                        current = upstreamStream;
                                    }
                                }
                            }
                            else
                            {
                                // No AbstractBus master exists; if InputSource is AbstractBus, chain is invalid.
                                chainIsValid = false;
                                done = true;
                            }
                        }
                        break;

                    default:
                        chainIsValid = false;
                        done = true;
                        break;
                }
            }

            // Only add the chain if it is valid
            if (chainIsValid && chain.Count > 0)
            {
                // Reverse the chain so it runs from start to endpoint.
                chain.Reverse();
                rawChains.Add(chain);
            }
        }

        return rawChains;
    }

    protected string ComputeChainSignature(List<DSP_Stream> chain, int upToIndex)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < upToIndex; i++)
        {
            sb.Append(chain[i].GetHashCode().ToString());
            sb.Append("-");
        }
        return sb.ToString();
    }

    // ----- Post-Process a Raw Chain to Inject Cloned Masters with Caching -----
    // Now when injecting an AbstractBus master clone we first compute the upstream chain signature.
    // If that signature was seen before for this AbstractBus index, we reuse the clone.
    protected List<DSP_Stream>? PostProcessChain(List<DSP_Stream> rawChain, Dictionary<int, DSP_Stream> abMasters)
    {
        var finalChain = new List<DSP_Stream>();
        for (int i = 0; i < rawChain.Count; i++)
        {
            var stream = rawChain[i];

            // Determine if the stream is an AbstractBus master.
            bool isMaster = stream.InputSource.StreamType == StreamType.AbstractBus &&
                            stream.OutputDestination.StreamType == StreamType.AbstractBus &&
                            stream.InputSource.Index == stream.OutputDestination.Index;

            bool hasAbstractOut = stream.OutputDestination.StreamType == StreamType.AbstractBus;

            if (isMaster)
                continue;

            if (!isMaster && hasAbstractOut)
            {
                stream.AbstractBusBuffer = new double[this.SamplesPerChannel];
                int abIndex = stream.OutputDestination.Index;
                if (!abMasters.TryGetValue(abIndex, out var master))
                {
                    // If no master exists, mark the chain as invalid.
                    return null;
                }

                finalChain.Add(stream);

                // Compute a signature of the chain so far (upstream path).
                string signature = this.ComputeChainSignature(finalChain, finalChain.Count);

                // Attempt to reuse a previously cloned master if the chain is unchanged.
                if (this.AbstractBusCloneCache.TryGetValue((abIndex, signature), out var cachedClone))
                {
                    finalChain.Add(cachedClone);
                    this.UsedCloneKeys.Add((abIndex, signature));
                }
                else
                {
                    var clonedMaster = this.CloneAbstractBusStream(master);
                    this.AbstractBusCloneCache[(abIndex, signature)] = clonedMaster;
                    this.UsedCloneKeys.Add((abIndex, signature));
                    finalChain.Add(clonedMaster);
                }
            }
            else
            {
                finalChain.Add(stream);
            }
        }
        return finalChain;
    }

    // ----- Check for Chain Validity -----
    protected bool IsValidChain(List<DSP_Stream> chain)
    {
        bool hasAbstractBus = chain.Any(s =>
            s.InputSource?.StreamType == StreamType.AbstractBus ||
            s.OutputDestination?.StreamType == StreamType.AbstractBus);

        bool hasBuses = chain.Any(s =>
            s.InputSource?.StreamType == StreamType.Bus ||
            s.OutputDestination?.StreamType == StreamType.Bus);

        if (chain.Count == 0)
            return false;

        if (hasAbstractBus && chain.Count < 3)
            return false;

        if (!hasBuses && !hasAbstractBus && chain.Count > 1)
            return false;

        var last = chain[chain.Count - 1];
        if (last.OutputDestination == null || last.OutputDestination.StreamType != StreamType.Channel)
            return false;

        if (hasAbstractBus)
        {
            for (int h = 0; h < chain.Count; h++)
            {
                var chainItem = chain[h];
                if (chainItem.InputSource.StreamType == StreamType.AbstractBus && chainItem.OutputDestination.StreamType == StreamType.AbstractBus)
                    continue;
                bool isChainItemValid = false;

                if (chainItem.InputSource.StreamType == StreamType.AbstractBus)
                {
                    isChainItemValid = false;
                    var abstractBus = Program.DSP_Info.AbstractBuses[chainItem.InputSource.Index];
                    foreach (var mapping in abstractBus.Mappings)
                    {
                        var outputStream = Program.DSP_Info.Streams[mapping.OutputDestination.Index];
                        if (outputStream.OutputDestination.Equals(chainItem.OutputDestination)
                            && outputStream.InputSource.Index == chainItem.InputSource.Index)
                            isChainItemValid = true;
                    }
                }
                else
                    isChainItemValid = true;

                if (chainItem.OutputDestination.StreamType == StreamType.AbstractBus)
                {
                    isChainItemValid = false;
                    var abstractBus = Program.DSP_Info.AbstractBuses[chainItem.OutputDestination.Index];
                    foreach (var mapping in abstractBus.Mappings)
                    {
                        var inputStream = Program.DSP_Info.Streams[mapping.InputSource.Index];
                        if (inputStream.InputSource.Equals(chainItem.InputSource)
                            && inputStream.OutputDestination.Index == chainItem.OutputDestination.Index)
                            isChainItemValid = true;
                    }
                }
                else
                    isChainItemValid = true;

                if (!isChainItemValid)
                    return false;
            }
        }
        return true;
    }

    // ----- Helper to Compare Chains -----
    // This helper compares two lists of chains for equality so that we only update our
    // class-level cache if a chain has changed.
    protected bool AreChainsEqual(List<List<DSP_Stream>> chains1, List<List<DSP_Stream>> chains2)
    {
        if (chains1.Count != chains2.Count)
            return false;
        for (int i = 0; i < chains1.Count; i++)
        {
            var chain1 = chains1[i];
            var chain2 = chains2[i];
            if (chain1.Count != chain2.Count)
                return false;
            for (int j = 0; j < chain1.Count; j++)
            {
                if (!object.ReferenceEquals(chain1[j], chain2[j]))
                    return false;
            }
        }
        return true;
    }

    // ----- Build Final Stream Chains + Caching -----
    protected List<List<DSP_Stream>> BuildStreamChains(ObservableCollection<DSP_Stream> allStreams)
    {
        // Identify AbstractBus masters.
        var abMasters = this.GetAbstractBusMasters(allStreams);

        // Build raw chains using reverse chaining.
        var rawChains = this.BuildRawChains_Reversed(allStreams);

        var finalChains = new List<List<DSP_Stream>>();
        for (int i = 0; i < rawChains.Count; i++)
        {
            var chain = rawChains[i];
            if (!this.IsValidChain(chain))
                continue;

            var processed = this.PostProcessChain(chain, abMasters);
            if (processed == null || processed.Count == 0)
                continue;

            finalChains.Add(processed);
        }

        // Only update the persistent cache if changes are detected.
        if (!this.AreChainsEqual(this.ChainCache, finalChains))
        {
            this.ChainCache = finalChains;
        }

        // Remove unused cache entries.
        var keysToRemove = this.AbstractBusCloneCache.Keys
                              .Where(key => !this.UsedCloneKeys.Contains(key))
                              .ToList();
        for (int i = 0; i < keysToRemove.Count; i++)
        {
            this.AbstractBusCloneCache.Remove(keysToRemove[i]);
        }
        // Reset tracking for the next update.
        this.UsedCloneKeys.Clear();

        return this.ChainCache;
    }

    #endregion

    #region DSP Init / Header / Multi-Threading

    #region Single Threaded
    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    protected void DSP_SingleThreaded()
    {
        if (this.DSP_ASIO_Data == null)
            return;

        this.InputBufferConversion_ProcessingTime.Start();
        this.DSP_ASIO_Data.GetAsJaggedSamples(this.InputBuffer);
        this.InputBufferConversion_ProcessingTime.Stop();

        var dspStreams = Program.DSP_Info.Streams;
        if (dspStreams.Count > 0)
        {
            try
            {
                var chains = BuildStreamChains(dspStreams);
                // Process each chain sequentially.
                for (int i = 0; i < chains.Count; i++)
                {
                    DSP_Stream? PreviousStream = null;
                    for (int j = 0; j < chains[i].Count; j++)
                    {
                        DSP_Process_Channel(chains[i][j], PreviousStream);
                        PreviousStream = chains[i][j];
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex is not IndexOutOfRangeException && ex is not ArgumentOutOfRangeException)
                    throw;
            }
        }

        this.OutputBufferConversion_ProcessingTime.Start();
        this.DSP_ASIO_Data.SetAsJaggedSamples(this.OutputBuffer);
        this.OutputBufferConversion_ProcessingTime.Stop();
    }
    #endregion

    #region Multi-Threaded
    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    protected void DSP_MultiThreaded()
    {
        if (this.DSP_ASIO_Data == null)
            return;

        this.InputBufferConversion_ProcessingTime.Start();
        this.DSP_ASIO_Data.GetAsJaggedSamples(this.InputBuffer);
        this.InputBufferConversion_ProcessingTime.Stop();

        var dspStreams = Program.DSP_Info.Streams;
        if (dspStreams.Count > 0)
        {
            try
            {
                // The chains are now persisted in _chainCache and only updated if new paths are detected.
                var chains = BuildStreamChains(dspStreams);
                var tasks = new List<Task>(chains.Count);
                for (int i = 0; i < chains.Count; i++)
                {
                    int chainIndex = i; // Capture the loop variable to avoid closure issues
                    tasks.Add(Task.Run(() =>
                    {
                        DSP_Stream? PreviousStream = null;
                        for (int j = 0; j < chains[chainIndex].Count; j++)
                        {
                            DSP_Process_Channel(chains[chainIndex][j], PreviousStream);
                            PreviousStream = chains[chainIndex][j];
                        }
                    }));
                }
                Task.WaitAll(tasks.ToArray(), 500);
            }
            catch (Exception ex)
            {
                if (ex is not IndexOutOfRangeException && ex is not ArgumentOutOfRangeException)
                    throw;
            }
        }

        this.OutputBufferConversion_ProcessingTime.Start();
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
    protected void DSP_Process_Channel(DSP_Stream currentStream, DSP_Stream? previousStream)
    {
        //Function must be thread-safe

        //Make sure the Stream and Buffers and Channel Index are legit, otherwise return (i.e. output buffer is unchanged)
        if (currentStream == null ||
            this.OutputBuffer == null ||
            this.InputBuffer == null ||
            currentStream.OutputDestination == null ||
            currentStream.InputSource == null ||
            currentStream.OutputDestination.Index < 0 || currentStream.InputSource.Index < 0)
        {
            return;
        }

        bool IsNotByPassed = true;

        #region Setup I/O Buffers
        double[] Local_OutputBuffer;
        switch (currentStream.OutputDestination.StreamType)
        {
            case StreamType.Bus: //Write-once Read-many
                var Bus = Program.DSP_Info.Buses[currentStream.OutputDestination.Index];
                if (Bus != null)
                {
                    IsNotByPassed = !Bus.IsBypassed;
                    if (Bus.Buffer.Length != this.SamplesPerChannel)
                        Bus.Buffer = new double[this.SamplesPerChannel];
                }
                Local_OutputBuffer = Bus?.Buffer ?? new double[this.SamplesPerChannel];
                break;
            case StreamType.AbstractBus: //Write-once Read-many with fixed mappings
                var AbstractBus = Program.DSP_Info.AbstractBuses[currentStream.OutputDestination.Index];
                if (AbstractBus != null)
                {
                    IsNotByPassed = !AbstractBus.IsBypassed;
                    //todo: Fix this and then enable the Mapping ByPass checkbox
                    //int currentStreamIndex = Program.DSP_Info.Streams.IndexOf(currentStream);
                    //var mapping = AbstractBus.Mappings.FirstOrDefault(m => m.InputSource.Index == currentStreamIndex);
                    //IsNotByPassed = mapping != null ? !mapping.IsBypassed : !AbstractBus.IsBypassed;
                }
                //Saves the output into currentStream.AbstractBusBuffer for later so that it doesn't get lost,
                //for inputs it is used by previousstream logic below, as the AbstractBus Master Stream is a "virtual" run-time object
                if (currentStream.AbstractBusBuffer == null || currentStream.AbstractBusBuffer.Length < this.SamplesPerChannel)
                    currentStream.AbstractBusBuffer = new double[this.SamplesPerChannel];
                Local_OutputBuffer = currentStream.AbstractBusBuffer;
                break;
            case StreamType.Channel: //ASIO channel
            default:
                if (currentStream.OutputDestination.Index >= this.OutputBuffer.Length)
                    return;
                Local_OutputBuffer = this.OutputBuffer[currentStream.OutputDestination.Index];
                break;
        }

        double[] Local_InputBuffer;
        switch (currentStream.InputSource.StreamType)
        {
            case StreamType.Bus: //Write-once Read-many
                var Bus = Program.DSP_Info.Buses[currentStream.InputSource.Index];
                if (Bus != null)
                {
                    IsNotByPassed = !Bus.IsBypassed;
                    if (Bus.Buffer.Length != this.SamplesPerChannel)
                        Bus.Buffer = new double[this.SamplesPerChannel];
                }
                Local_InputBuffer = Bus?.Buffer ?? new double[this.SamplesPerChannel];
                break;
            case StreamType.AbstractBus: //Write-once Read-many with fixed mappings
                var AbstractBus = Program.DSP_Info.AbstractBuses[currentStream.InputSource.Index];
                if (AbstractBus != null)
                {
                    IsNotByPassed = !AbstractBus.IsBypassed;
                    //todo: Fix this and then enable the Mapping ByPass checkbox
                    //int currentStreamIndex = Program.DSP_Info.Streams.IndexOf(currentStream);
                    //var mapping = AbstractBus.Mappings.FirstOrDefault(m => m.OutputDestination.Index == currentStreamIndex);
                    //IsNotByPassed = mapping != null ? !mapping.IsBypassed : !AbstractBus.IsBypassed;
                }

                //If previousStream exists but has uninitialized buffer (shouldn't happen but guarded anyway)
                if (previousStream != null &&
                    (previousStream.AbstractBusBuffer == null || previousStream.AbstractBusBuffer.Length < this.SamplesPerChannel))
                {
                    previousStream.AbstractBusBuffer = new double[this.SamplesPerChannel];
                }
                //If previous stream doesn't exist then empty array, otherwise use it
                if (previousStream == null || previousStream.AbstractBusBuffer == null)
                {
                    Local_InputBuffer = new double[this.SamplesPerChannel];
                }
                else
                {
                    Local_InputBuffer = previousStream.AbstractBusBuffer;
                }
                break;
            case StreamType.Channel: //ASIO channel
            default:
                if (currentStream.InputSource.Index >= this.InputBuffer.Length)
                    return;
                Local_InputBuffer = this.InputBuffer[currentStream.InputSource.Index];
                break;
        }
        #endregion

        #region Init
        int ChannelFilterCount = currentStream.Filters.Count;
        double Local_InputVolumeGain = this.InputMasterVolume * currentStream.InputVolume;
        double Local_OutputVolumeGain = this.OutputMasterVolume * currentStream.OutputVolume;
        int Local_SamplesPerChannel = this.SamplesPerChannel;
        IFilter? CurrentFilter;
        #endregion

        //Apply the InputMasterVolume and StreamInputVolume
        for (var SampleIndex = 0; SampleIndex < Local_SamplesPerChannel; SampleIndex++)
            //Make a byval copy of the sample value as array elements are byref and that
            //would couple ASIO output to ASIO input array (a bad thing!)
            Local_OutputBuffer[SampleIndex] = (double)(Local_InputVolumeGain * Local_InputBuffer[SampleIndex]);

        try
        {
            if (IsNotByPassed)
                //Apply every DSP filter that exists (if any) in the stream to the samples
                for (int FilterIndex = 0; FilterIndex < ChannelFilterCount; FilterIndex++)
                {
                    CurrentFilter = currentStream.Filters[FilterIndex];
                    if (CurrentFilter is null || !CurrentFilter.FilterEnabled)
                        continue;

                    //Processes a whole block of input channel samples
                    Local_OutputBuffer = CurrentFilter.Transform(Local_OutputBuffer, currentStream);
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
            //Apply the stream Output Volume and master volume to the sample
            Local_OutputBuffer[SampleIndex] *= Local_OutputVolumeGain;
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

    protected void CleanUp_StreamCaches()
    {
        this.ChainCache.Clear();
        this.AbstractBusCloneCache.Clear();
        this.UsedCloneKeys.Clear();
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