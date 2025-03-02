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
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.Formatters.Binary;
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
    // ----- 1. CloneStream Stub -----
    protected DSP_Stream CloneAbstractBusStream(DSP_Stream original)
    {
        DSP_Stream clone = new();

        // Clone auxiliary buffer if needed
        if (original.AuxBuffer != null)
        {
            clone.AuxBuffer = [.. original.AuxBuffer.Select(buffer => (double[])buffer.Clone())];
        }

        // Clone the stream items (assuming they provide a clone method)
        clone.InputSource = original.InputSource.DeepClone();
        clone.OutputDestination = original.OutputDestination.DeepClone();

        // Copy volumes
        clone.InputVolume = original.InputVolume;
        clone.OutputVolume = original.OutputVolume;

        // Deep clone each filter
        clone.Filters = [];
        foreach (IFilter filter in original.Filters)
        {
            clone.Filters.Add(filter.DeepClone());
        }

        return clone;
    }

    // ----- 2. DFS to Build Raw Chains (Without Injection) -----
    protected void DFS_Chains(DSP_Stream current, Dictionary<IStreamItem, List<DSP_Stream>> adjacency,
        List<DSP_Stream> path, List<List<DSP_Stream>> rawChains, HashSet<DSP_Stream> visited)
    {
        // Loop detection: if 'current' is already in this path, abort this branch.
        if (!visited.Add(current))
            return;

        // Add current stream to the ongoing chain path.
        path.Add(current);

        // If current stream outputs to a Channel, we have a complete chain.
        if (current.OutputDestination.StreamType == StreamType.Channel)
        {
            rawChains.Add([.. path]);
            // Backtrack:
            path.RemoveAt(path.Count - 1);
            visited.Remove(current);
            return;
        }

        // Follow all streams that use current.OutputDestination as their input.
        var outKey = current.OutputDestination;
        if (adjacency.TryGetValue(outKey, out var nextStreams))
        {
            foreach (var nxt in nextStreams)
            {
                DFS_Chains(nxt, adjacency, path, rawChains, visited);
            }
        }

        // Backtrack: remove current stream from the path and visited set.
        path.RemoveAt(path.Count - 1);
        visited.Remove(current);
    }

    // ----- 3. Build the Adjacency Map -----
    // Key: StreamItem (the input source) → Value: list of streams that consume it.
    protected Dictionary<IStreamItem, List<DSP_Stream>> BuildAdjacencyMap(List<DSP_Stream> streams)
    {
        var adjacency = new Dictionary<IStreamItem, List<DSP_Stream>>();
        foreach (var s in streams)
        {
            if (s == null)
                continue;
            var key = s.InputSource;
            if (!adjacency.TryGetValue(key, out var list))
            {
                list = [];
                adjacency[key] = list;
            }
            list.Add(s);
        }
        return adjacency;
    }

    // ----- 4. Identify AbstractBus Master Streams -----
    /// <summary>
    /// Finds the single "master" stream for each AbstractBus index.
    /// A master is defined as a stream whose InputSource and OutputDestination are
    /// both of type AbstractBus and have the same index.
    /// </summary>
    protected Dictionary<int, DSP_Stream> GetAbstractBusMasters(ObservableCollection<DSP_Stream> allStreams)
    {
        var result = new Dictionary<int, DSP_Stream>();
        var duplicates = new HashSet<int>();

        foreach (var s in allStreams)
        {
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
        foreach (var dup in duplicates)
        {
            result.Remove(dup);
        }

        return result;
    }

    // ----- 5. Filter Out Invalid Streams -----
    /// <summary>
    /// Filters out streams that are misconfigured.
    /// For Bus streams, enforce that each Bus is produced only once.
    /// For AbstractBus usages, if a stream references an AbstractBus (as input or output)
    /// but is not itself the master, it will later have a master injected.
    /// Exclude the stream immediately if no master exists for its AbstractBus index.
    /// </summary>
    protected List<DSP_Stream> GetValidStreams(ObservableCollection<DSP_Stream> allStreams, Dictionary<int, DSP_Stream> abstractBusMasters)
    {
        var valid = new List<DSP_Stream>();
        var busProduced = new HashSet<int>();

        foreach (var s in allStreams)
        {
            if (s == null || s.InputSource == null || s.OutputDestination == null)
                continue;

            // Check AbstractBus usage (if not a master) that the master exists.
            bool isMaster = s.InputSource.StreamType == StreamType.AbstractBus &&
                             s.OutputDestination.StreamType == StreamType.AbstractBus &&
                             s.InputSource.Index == s.OutputDestination.Index;
            bool hasAbstractIn = s.InputSource.StreamType == StreamType.AbstractBus;
            bool hasAbstractOut = s.OutputDestination.StreamType == StreamType.AbstractBus;

            // If the stream references an AbstractBus on only one side, check for a master.
            if (!isMaster && hasAbstractIn ^ hasAbstractOut)
            {
                int abIndex = hasAbstractIn ? s.InputSource.Index : s.OutputDestination.Index;
                if (!abstractBusMasters.ContainsKey(abIndex))
                    continue; // Exclude this stream if no master is found.
            }

            // Enforce that a Bus can be produced only once.
            if (s.OutputDestination.StreamType == StreamType.Bus)
            {
                int busIndex = s.OutputDestination.Index;
                if (busProduced.Contains(busIndex))
                    continue; // Already produced by another stream.
                busProduced.Add(busIndex);
            }
            // For AbstractBus masters and usages, multiple productions are allowed.

            valid.Add(s);
        }
        return valid;
    }

    // ----- 6. Build Raw Chains Using DFS -----
    protected List<List<DSP_Stream>> BuildRawChains(ObservableCollection<DSP_Stream> allStreams)
    {
        // Identify AbstractBus masters first.
        var abMasters = GetAbstractBusMasters(allStreams);

        // Filter out misconfigured streams.
        var validStreams = GetValidStreams(allStreams, abMasters);

        // Build the adjacency map.
        var adjacency = BuildAdjacencyMap(validStreams);

        // Find all candidate start streams (those with Channel input).
        var startStreams = validStreams
            .Where(s => s.InputSource.StreamType == StreamType.Channel)
            .ToList();

        var rawChains = new List<List<DSP_Stream>>();
        foreach (var start in startStreams)
        {
            if (start == null)
                continue;
            var path = new List<DSP_Stream>();
            var visited = new HashSet<DSP_Stream>();
            DFS_Chains(start, adjacency, path, rawChains, visited);
        }
        return rawChains;
    }

    // ----- 7. Post-Process a Raw Chain to Inject Cloned Masters -----
    /// <summary>
    /// Walks through a raw chain and, for each stream that uses an AbstractBus on only one side,
    /// injects a cloned master (using CloneStream) immediately before (if the AbstractBus is on input)
    /// or after (if on output) the stream. If a master for a given AbstractBus index cannot be found,
    /// the chain is considered invalid and null is returned.
    /// </summary>
    protected List<DSP_Stream>? PostProcessChain(List<DSP_Stream> rawChain, Dictionary<int, DSP_Stream> abMasters)
    {
        var finalChain = new List<DSP_Stream>();

        foreach (var stream in rawChain)
        {
            // Determine if the stream is the master.
            bool isMaster = stream.InputSource.StreamType == StreamType.AbstractBus &&
                            stream.OutputDestination.StreamType == StreamType.AbstractBus &&
                            stream.InputSource.Index == stream.OutputDestination.Index;

            bool hasAbstractIn = stream.InputSource.StreamType == StreamType.AbstractBus;
            bool hasAbstractOut = stream.OutputDestination.StreamType == StreamType.AbstractBus;

            // If the stream is not the master and uses an AbstractBus on only one side,
            // then inject a clone of the master.
            if (!isMaster && hasAbstractIn ^ hasAbstractOut)
            {
                int abIndex = hasAbstractIn ? stream.InputSource.Index : stream.OutputDestination.Index;
                if (!abMasters.TryGetValue(abIndex, out var master))
                {
                    // Master not found: chain is invalid.
                    return null;
                }
                // Clone the master.
                var clonedMaster = CloneAbstractBusStream(master);

                // If AbstractBus is used as input only, insert the clone before this stream.
                if (hasAbstractIn)
                {
                    finalChain.Add(clonedMaster);
                    finalChain.Add(stream);
                }
                // If used as output only, insert the clone after this stream.
                else if (hasAbstractOut)
                {
                    finalChain.Add(stream);
                    finalChain.Add(clonedMaster);
                }
            }
            else
            {
                // No AbstractBus injection needed.
                finalChain.Add(stream);
            }
        }
        return finalChain;
    }

    // ----- 8. Build Final Stream Chains (Raw DFS + Post-Processing) -----
    protected List<List<DSP_Stream>> BuildStreamChains(ObservableCollection<DSP_Stream> allStreams)
    {
        // Identify AbstractBus masters.
        var abMasters = GetAbstractBusMasters(allStreams);

        // Build raw chains via DFS.
        var rawChains = BuildRawChains(allStreams);

        // Process each raw chain to inject AbstractBus clones where needed.
        var finalChains = new List<List<DSP_Stream>>();
        foreach (var chain in rawChains)
        {
            var processed = PostProcessChain(chain, abMasters);
            if (processed != null)
                finalChains.Add(processed);
        }
        return finalChains;
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
                    for (int j = 0; j < chains[i].Count; j++)
                    {
                        DSP_Process_Channel(chains[i][j]);
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
                var chains = BuildStreamChains(dspStreams);
                var tasks = new List<Task>(chains.Count);
                // Process each chain in parallel.
                for (int i = 0; i < chains.Count; i++)
                {
                    int chainIndex = i; // Capture the loop variable to avoid closure issues
                    tasks.Add(Task.Run(() =>
                    {
                        for (int j = 0; j < chains[chainIndex].Count; j++)
                        {
                            DSP_Process_Channel(chains[chainIndex][j]);
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
    protected void DSP_Process_Channel(DSP_Stream CurrentStream)
    {
        //Function must be thread-safe

        //Make sure the Stream and Buffers and Channel Index are legit, otherwise return (i.e. output buffer is muted zeroes)
        if (CurrentStream == null ||
            this.OutputBuffer == null || 
            this.InputBuffer == null ||
            CurrentStream.OutputDestination == null ||
            CurrentStream.InputSource == null ||
            CurrentStream.OutputDestination.Index < 0 || CurrentStream.InputSource.Index < 0 ||
            CurrentStream.OutputDestination.Index >= this.OutputBuffer.Length ||
            CurrentStream.InputSource.Index >= this.InputBuffer.Length)
        {
            return;
        }

        bool IsNotByPassed = true;

        double[] Local_OutputBuffer;  // Default to Channel
        switch (CurrentStream.OutputDestination.StreamType)
        {
            case StreamType.Bus:
                var Bus = Program.DSP_Info.Buses[CurrentStream.OutputDestination.Index];
                if (Bus.Buffer.Length != this.SamplesPerChannel)
                    Bus.Buffer = new double[this.SamplesPerChannel];
                Local_OutputBuffer = Bus.Buffer;
                break;
            //case StreamType.AbstractBus:
            //    var AbstractBus = Program.DSP_Info.AbstractBuses[CurrentStream.OutputDestination.Index];
            //    if (AbstractBus != null && AbstractBus.IsBypassed)
            //        IsNotByPassed = false;
            //    //Local_OutputBuffer = AbstractBus.Buffer;
            //    break;
            case StreamType.Channel:
            default:
                    Local_OutputBuffer = this.OutputBuffer[CurrentStream.OutputDestination.Index];
                break;
        }

        double[] Local_InputBuffer; // Default to Channel
        switch (CurrentStream.InputSource.StreamType)
        {
            case StreamType.Bus:
                var Bus = Program.DSP_Info.Buses[CurrentStream.InputSource.Index];
                if (Bus.Buffer.Length != this.SamplesPerChannel)
                    Bus.Buffer = new double[this.SamplesPerChannel];
                Local_InputBuffer = Bus.Buffer;
                break;
            //case StreamType.AbstractBus:
            //    var AbstractBus = Program.DSP_Info.AbstractBuses[CurrentStream.InputSource.Index];
            //    if (AbstractBus != null && AbstractBus.IsBypassed)
            //        IsNotByPassed = false;
            //    //Local_InputBuffer = AbstractBus.Buffer;
            //    break;
            case StreamType.Channel:
            default:
                Local_InputBuffer = this.InputBuffer[CurrentStream.InputSource.Index];
                break;
        }

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
            if (IsNotByPassed)
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