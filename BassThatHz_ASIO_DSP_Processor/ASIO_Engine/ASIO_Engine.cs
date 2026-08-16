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
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
#endregion

/// <summary>
///  BassThatHz ASIO DSP Processor Engine
///  Copyright (c) 2026 BassThatHz
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
    // Double-buffer partner for ChainCache. Every call rebuilds into whichever of the two lists
    // is currently INACTIVE and then swaps the two references only if the result actually
    // changed. That keeps the "cache is only updated when a change is detected" semantics of the
    // original code while removing the per-callback List allocations it used to do.
    protected List<List<DSP_Stream>> ChainCache_Scratch = new();
    // This cache will prevent AbstractBus master re-cloning if the upstream chain path hasn’t changed.
    protected Dictionary<AbstractBusCloneKey, DSP_Stream> AbstractBusCloneCache = new();
    //Uniquely identifies active AbstractBus clones
    protected HashSet<AbstractBusCloneKey> UsedCloneKeys = new();
    #endregion

    #region Chain Building Scratch Space
    //
    // All of the collections in this region are owned exclusively by the chain-building code,
    // which only ever runs on the single DSP thread (DSP_ManualBackgroundThread) or, when
    // IsMT_BackgroundThreadEnabled is false, on the ASIO callback thread - and always BEFORE any
    // per-chain Tasks are spawned. They are therefore single-threaded by construction and are
    // safe to hoist into fields. They exist purely so that the hot path can .Clear() and refill
    // them instead of allocating a fresh Dictionary/HashSet/List on every buffer switch.
    //
    protected readonly Dictionary<int, DSP_Stream> AbMasters_Scratch = new();
    protected readonly HashSet<int> AbMasterDuplicates_Scratch = new();
    //BuildRawChains_Reversed needs its own masters dictionary because BuildStreamChains holds a
    //live reference to AbMasters_Scratch across the BuildRawChains_Reversed call.
    protected readonly Dictionary<int, DSP_Stream> AbMasters_Raw_Scratch = new();
    protected readonly HashSet<int> AbMasterDuplicates_Raw_Scratch = new();
    protected readonly List<DSP_Stream> ValidStreams_Scratch = new();
    protected readonly HashSet<int> BusProduced_Scratch = new();
    protected readonly List<DSP_Stream> EndStreams_Scratch = new();
    protected readonly List<List<DSP_Stream>> RawChains_Scratch = new();
    protected readonly List<AbstractBusCloneKey> CloneKeysToRemove_Scratch = new();
    //Pool of reusable per-chain lists used while building the RAW chains. Raw chains never
    //escape a single BuildStreamChains() call, so recycling their backing lists is safe.
    protected readonly Stack<List<DSP_Stream>> RawChainListPool = new();
    #endregion

    #region Buffers
    //An jagged array of ASIO sample data from DSP_ASIO_Data as processed by NAudio
    public double[][] InputBuffer = [];
    public double[][] OutputBuffer = [];

    // A single, shared, READ-ONLY all-zeros buffer used for the "missing input" fallbacks in
    // DSP_Process_Channel. It is only ever used as an INPUT (the sample loop reads from it and
    // writes to Local_OutputBuffer), never as an output destination, so sharing it across the
    // concurrently running chains cannot race: every chain only reads zeros from it.
    // DO NOT ever assign this to Local_OutputBuffer or hand it to a filter.
    protected double[] SharedZeroInputBuffer = [];
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
    //Holds an array of Tasks, one per stream of DSP processing that is running in parallel.
    //Re-allocated only when the number of chains changes (Task.WaitAll requires an exactly
    //sized, null-free array), not on every buffer switch.
    protected Task[]? StreamTaskList = null;
    //One pre-built worker per chain. Holding the chain index in a long-lived object instead of
    //capturing it in a lambda removes the closure (display class) + delegate allocations that
    //used to happen once per chain per buffer switch.
    protected ChainWorker[] ChainWorkers = [];
    #endregion

    #region Data Available Notification
    //Pre-allocated fire-and-forget notification work item. The original code did
    //Task.Run(() => {...}) on EVERY buffer switch, which allocated a display class, a delegate
    //and a Task each time. This instance is created once in the constructor and re-queued.
    protected readonly DataAvailableNotifier DataAvailableWorkItem;
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

    #region Nested Types

    /// <summary>
    /// Non-allocating cache key replacing the old (int abIndex, string chainSignature) tuple.
    /// The old key built a StringBuilder plus a string for EVERY AbstractBus node of EVERY chain
    /// on EVERY buffer switch. This struct carries the same information as a 64 bit rolling hash
    /// of the very same per-node hash codes, and implements IEquatable so that Dictionary and
    /// HashSet lookups use EqualityComparer&lt;T&gt;.Default without boxing.
    /// </summary>
    protected readonly struct AbstractBusCloneKey : IEquatable<AbstractBusCloneKey>
    {
        /// <summary>The AbstractBus index this clone belongs to.</summary>
        public readonly int AbIndex;

        /// <summary>Rolling hash of the upstream chain path that feeds this AbstractBus.</summary>
        public readonly long Signature;

        /// <summary>Creates a new key.</summary>
        public AbstractBusCloneKey(int abIndex, long signature)
        {
            this.AbIndex = abIndex;
            this.Signature = signature;
        }

        /// <summary>Value equality over both members.</summary>
        public bool Equals(AbstractBusCloneKey other) =>
            this.AbIndex == other.AbIndex && this.Signature == other.Signature;

        /// <summary>Value equality over both members.</summary>
        public override bool Equals(object? obj) => obj is AbstractBusCloneKey other && this.Equals(other);

        /// <summary>Compact integer hash, no allocations.</summary>
        public override int GetHashCode()
        {
            unchecked
            {
                return (this.AbIndex * 397) ^ (int)this.Signature ^ (int)(this.Signature >> 32);
            }
        }
    }

    /// <summary>
    /// A pre-allocated, reusable per-chain DSP worker.
    /// The chain index is fixed at construction time and the chains list is only re-assigned when
    /// it actually changes, so in steady state running the DSP performs no writes to this object
    /// at all - which also means no per-callback closure/delegate allocation.
    /// </summary>
    protected sealed class ChainWorker
    {
        /// <summary>Cached delegate handed to Task.Run so no delegate is allocated per callback.</summary>
        public readonly Action Run;

        private readonly int ChainIndex;
        private ASIO_Engine? Engine;
        private List<List<DSP_Stream>>? Chains;

        /// <summary>Creates a worker permanently bound to one chain index.</summary>
        /// <param name="chainIndex">The index into the chains list this worker processes.</param>
        public ChainWorker(int chainIndex)
        {
            this.ChainIndex = chainIndex;
            this.Run = this.Execute;
        }

        /// <summary>
        /// Points the worker at the current engine + chain set. Writes are skipped when nothing
        /// changed so that the steady state performs no cross-thread writes whatsoever.
        /// </summary>
        public void Prepare(ASIO_Engine engine, List<List<DSP_Stream>> chains)
        {
            if (!ReferenceEquals(this.Engine, engine))
                this.Engine = engine;
            if (!ReferenceEquals(this.Chains, chains))
                this.Chains = chains;
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void Execute()
        {
            var Local_Engine = this.Engine;
            var Local_Chains = this.Chains;
            if (Local_Engine == null || Local_Chains == null)
                return;

            //Mirrors the original lambda exactly, including the re-indexing of chains[chainIndex].
            var Local_ChainIndex = this.ChainIndex;
            if (Local_ChainIndex >= Local_Chains.Count)
                return;

            var Local_Chain = Local_Chains[Local_ChainIndex];
            DSP_Stream? PreviousStream = null;
            for (int j = 0; j < Local_Chain.Count; j++)
            {
                Local_Engine.DSP_Process_Channel(Local_Chain[j], PreviousStream);
                PreviousStream = Local_Chain[j];
            }
        }
    }

    /// <summary>
    /// Zero-allocation replacement for the per-callback Task.Run(() =&gt; { ... }) that notified
    /// listeners of data availability. A single instance is created in the constructor and
    /// re-queued to the thread pool on every buffer switch, which preserves the
    /// "fire and forget on a thread pool thread" semantics with no allocation at all.
    /// </summary>
    protected sealed class DataAvailableNotifier : IThreadPoolWorkItem
    {
        private readonly ASIO_Engine Engine;

        /// <summary>Creates the notifier for a given engine.</summary>
        public DataAvailableNotifier(ASIO_Engine engine)
        {
            this.Engine = engine;
        }

        /// <summary>Raises the input/output data available events on a thread pool thread.</summary>
        public void Execute()
        {
            //Task.Run() used to capture any exception thrown here into an (unobserved) Task,
            //i.e. it was silently swallowed. A raw thread pool work item would instead tear the
            //process down, so swallow explicitly to keep the previous behaviour.
            try
            {
                this.Engine.InputDataAvailable?.Invoke();
                this.Engine.OutputDataAvailable?.Invoke();
            }
            catch (Exception ex)
            {
                _ = ex;
            }
        }
    }
    #endregion

    #region Constructor / Dispose
    public ASIO_Engine()
    {
        //Pre-allocate the data-available notifier so the audio callback never allocates one
        this.DataAvailableWorkItem = new DataAvailableNotifier(this);

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
            //DEFECT FIX: the DSP thread shutdown used to be skipped whenever ASIO.Dispose() threw,
            //which left DSP_AllowedToRun true (thread kept spinning). Guard it separately.
            try
            {
                //ASIO uses unmanaged Windows OLE com sub-system, we have to dispose it
                this.ASIO?.Dispose();
            }
            finally
            {
                this.ASIO = null;
            }
        }
        catch (Exception ex)
        {
            //Never let a driver teardown failure abort the rest of the shutdown, and never show a
            //modal dialog from here - Dispose can run on the finalizer thread.
            Debug.ReportSwallowed(ex);
        }
        finally
        {
            try
            {
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
                        _ = ex; //Interrupting an already-exited thread is expected and harmless.
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.ReportSwallowed(ex);
            }

            //DEFECT FIX: this used to sit inside the try, so any failure above left the finalizer
            //armed - ~ASIO_Engine would then re-enter Dispose() on the finalizer thread and
            //(previously) raise a modal MessageBox from it, hanging the finalizer queue.
            GC.SuppressFinalize(this);
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
    /// The active driver's reported hardware latencies, in samples, or null when no driver is open.
    /// A named ValueTuple (InputLatency, OutputLatency), so reading it does not heap-allocate.
    /// </summary>
    public (int InputLatency, int OutputLatency)? PlaybackLatency => this.ASIO?.PlaybackLatency;

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

    /// <summary>
    /// Gets a defensive COPY of one input channel's audio data.
    /// The copy is deliberate: the caller is the GUI thread (meters / RTA) and the underlying
    /// buffer is mutated by the audio thread, so handing out the live array would produce
    /// torn reads. Use <see cref="TryCopyInputAudioData"/> to avoid the per-call allocation.
    /// </summary>
    /// <param name="channelIndex">The input channel index.</param>
    /// <returns>A newly allocated copy, or null when the index is out of range.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double[]? GetInputAudioData(int channelIndex)
    {
        var Local_InputBuffer = this.InputBuffer;
        if (Local_InputBuffer == null || channelIndex < 0 || channelIndex >= Local_InputBuffer.Length)
            return null;

        var Local_Channel = Local_InputBuffer[channelIndex];
        if (Local_Channel == null)
            return null;

        var Local_Copy = new double[Local_Channel.Length];
        Local_Channel.AsSpan().CopyTo(Local_Copy);
        return Local_Copy;
    }

    /// <summary>
    /// Gets a defensive COPY of one output channel's audio data.
    /// The copy is deliberate: the caller is the GUI thread (meters / RTA) and the underlying
    /// buffer is mutated by the audio thread, so handing out the live array would produce
    /// torn reads. Use <see cref="TryCopyOutputAudioData"/> to avoid the per-call allocation.
    /// </summary>
    /// <param name="channelIndex">The output channel index.</param>
    /// <returns>A newly allocated copy, or null when the index is out of range.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double[]? GetOutputAudioData(int channelIndex)
    {
        var Local_OutputBuffer = this.OutputBuffer;
        if (Local_OutputBuffer == null || channelIndex < 0 || channelIndex >= Local_OutputBuffer.Length)
            return null;

        var Local_Channel = Local_OutputBuffer[channelIndex];
        if (Local_Channel == null)
            return null;

        var Local_Copy = new double[Local_Channel.Length];
        Local_Channel.AsSpan().CopyTo(Local_Copy);
        return Local_Copy;
    }

    /// <summary>
    /// Non-allocating variant of <see cref="GetInputAudioData"/>: copies one input channel's
    /// audio data into a caller supplied destination. The defensive-copy semantics are
    /// identical, only the buffer ownership changes.
    /// </summary>
    /// <param name="channelIndex">The input channel index.</param>
    /// <param name="destination">The caller-owned destination span.</param>
    /// <param name="samplesCopied">Receives the number of samples written.</param>
    /// <returns>True when the channel existed and the destination was large enough.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryCopyInputAudioData(int channelIndex, Span<double> destination, out int samplesCopied)
    {
        samplesCopied = 0;
        var Local_InputBuffer = this.InputBuffer;
        if (Local_InputBuffer == null || channelIndex < 0 || channelIndex >= Local_InputBuffer.Length)
            return false;

        var Local_Channel = Local_InputBuffer[channelIndex];
        if (Local_Channel == null || destination.Length < Local_Channel.Length)
            return false;

        Local_Channel.AsSpan().CopyTo(destination);
        samplesCopied = Local_Channel.Length;
        return true;
    }

    /// <summary>
    /// Non-allocating variant of <see cref="GetOutputAudioData"/>: copies one output channel's
    /// audio data into a caller supplied destination. The defensive-copy semantics are
    /// identical, only the buffer ownership changes.
    /// </summary>
    /// <param name="channelIndex">The output channel index.</param>
    /// <param name="destination">The caller-owned destination span.</param>
    /// <param name="samplesCopied">Receives the number of samples written.</param>
    /// <returns>True when the channel existed and the destination was large enough.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryCopyOutputAudioData(int channelIndex, Span<double> destination, out int samplesCopied)
    {
        samplesCopied = 0;
        var Local_OutputBuffer = this.OutputBuffer;
        if (Local_OutputBuffer == null || channelIndex < 0 || channelIndex >= Local_OutputBuffer.Length)
            return false;

        var Local_Channel = Local_OutputBuffer[channelIndex];
        if (Local_Channel == null || destination.Length < Local_Channel.Length)
            return false;

        Local_Channel.AsSpan().CopyTo(destination);
        samplesCopied = Local_Channel.Length;
        return true;
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

            //Read-only all-zeros fallback used by DSP_Process_Channel, see SharedZeroInputBuffer
            this.SharedZeroInputBuffer = new double[this.SamplesPerChannel];
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

        //Allows any event listeners to get to be notified of Data Availability.
        //Uses a pre-allocated work item instead of Task.Run(lambda) so that this - which runs on
        //EVERY buffer switch - does not allocate a closure, a delegate and a Task each time.
        ThreadPool.UnsafeQueueUserWorkItem(this.DataAvailableWorkItem, preferLocal: false);

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

    // ----- Identify AbstractBus Master Streams -----
    /// <summary>
    /// Finds the single "master" stream for each AbstractBus index.
    /// A master is defined as a stream whose InputSource and OutputDestination are
    /// both of type AbstractBus and have the same index.
    /// </summary>
    protected Dictionary<int, DSP_Stream> GetAbstractBusMasters(ObservableCollection<DSP_Stream> allStreams)
    {
        //Uses the pre-allocated scratch dictionary owned by BuildStreamChains.
        this.FillAbstractBusMasters(allStreams, this.AbMasters_Scratch, this.AbMasterDuplicates_Scratch);
        return this.AbMasters_Scratch;
    }

    /// <summary>
    /// Core of <see cref="GetAbstractBusMasters"/>, written against caller supplied (reusable)
    /// collections so that the hot path allocates nothing. Behaviour is identical to the
    /// original: the last write wins per index, and any index seen more than once is removed.
    /// </summary>
    /// <param name="allStreams">The configured streams.</param>
    /// <param name="result">Destination dictionary, cleared by this method.</param>
    /// <param name="duplicates">Scratch set for duplicate detection, cleared by this method.</param>
    protected void FillAbstractBusMasters(ObservableCollection<DSP_Stream> allStreams,
                                          Dictionary<int, DSP_Stream> result,
                                          HashSet<int> duplicates)
    {
        result.Clear();
        duplicates.Clear();

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
                    _ = duplicates.Add(abIndex);
                }
            }
        }

        // Remove duplicate masters. Enumerating the HashSet directly uses its struct enumerator,
        // so this replaces the old duplicates.ToList() allocation with nothing at all.
        if (duplicates.Count > 0)
        {
            foreach (var duplicateIndex in duplicates)
                _ = result.Remove(duplicateIndex);
        }
    }

    // ----- Filter Out Invalid Streams -----
    protected List<DSP_Stream> GetValidStreams(ObservableCollection<DSP_Stream> allStreams, Dictionary<int, DSP_Stream> abstractBusMasters)
    {
        //Reuses the pre-allocated scratch list/set instead of allocating a new List + HashSet
        //on every single audio callback. Single-threaded by construction, see the
        //"Chain Building Scratch Space" region.
        var valid = this.ValidStreams_Scratch;
        var busProduced = this.BusProduced_Scratch;
        valid.Clear();
        busProduced.Clear();

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
        //    NOTE: uses a SEPARATE scratch dictionary from GetAbstractBusMasters() because
        //    BuildStreamChains holds a live reference to that one across this call.
        this.FillAbstractBusMasters(allStreams, this.AbMasters_Raw_Scratch, this.AbMasterDuplicates_Raw_Scratch);
        var abMasters = this.AbMasters_Raw_Scratch;

        // 2. Filter out misconfigured streams.
        var validStreams = this.GetValidStreams(allStreams, abMasters);

        // 3. Candidate endpoints: streams whose OutputDestination is a Channel.
        //    The original used .Where(...).OrderBy(rank).ToList(). OrderBy is a STABLE sort and
        //    the rank is always one of 0..3, so four in-order passes produce a bit-identical
        //    ordering with zero allocations (no iterators, no lambdas, no sort buffers).
        var endStreams = this.EndStreams_Scratch;
        endStreams.Clear();
        for (int rank = 0; rank <= 3; rank++)
        {
            for (int i = 0; i < validStreams.Count; i++)
            {
                var s = validStreams[i];
                if (s.OutputDestination.StreamType != StreamType.Channel)
                    continue;

                var inputType = s.InputSource.StreamType;
                int streamRank = inputType == StreamType.Channel ? 0 :
                                 inputType == StreamType.AbstractBus ? 1 :
                                 inputType == StreamType.Bus ? 2 : 3;
                if (streamRank == rank)
                    endStreams.Add(s);
            }
        }

        // Recycle last call's raw chain lists before rebuilding.
        var rawChains = this.RawChains_Scratch;
        for (int i = 0; i < rawChains.Count; i++)
            this.ReturnRawChainList(rawChains[i]);
        rawChains.Clear();

        // 4. Process each candidate endpoint.
        for (int i = 0; i < endStreams.Count; i++)
        {
            var end = endStreams[i];
            if (end == null)
                continue;

            var chain = this.RentRawChainList();
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
                            // Plain nested loops instead of rawChains.Any(rc => rc.Any(...)):
                            // the LINQ version boxed two List enumerators and allocated a
                            // capturing closure per chain node, every single callback.
                            var LookupKey = current.InputSource;
                            bool ExistingFeeder = false;
                            for (int rc = 0; rc < rawChains.Count && !ExistingFeeder; rc++)
                            {
                                var RawChain = rawChains[rc];
                                for (int c = 0; c < RawChain.Count; c++)
                                {
                                    if (RawChain[c].OutputDestination.Equals(LookupKey))
                                    {
                                        ExistingFeeder = true;
                                        break;
                                    }
                                }
                            }
                            if (ExistingFeeder)
                            {
                                chainIsValid = true;
                                done = true;
                                break;
                            }

                            DSP_Stream? linkStream = null;
                            for (int s = 0; s < validStreams.Count; s++)
                            {
                                if (validStreams[s].OutputDestination.Equals(LookupKey))
                                {
                                    linkStream = validStreams[s];
                                    break;
                                }
                            }
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
                                // Loop instead of FirstOrDefault(lambda): the lambda captured
                                // 'current' and so allocated a display class per chain node.
                                var Local_Mappings = masterAbstractBus.Mappings;
                                var Local_CurrentOutputIndex = current.OutputDestination.Index;
                                IAbstractBusMappings? validMapping = null;
                                for (int m = 0; m < Local_Mappings.Count; m++)
                                {
                                    var Local_Mapping = Local_Mappings[m];
                                    if (Program.DSP_Info.Streams[Local_Mapping.OutputDestination.Index].OutputDestination.Index
                                        == Local_CurrentOutputIndex)
                                    {
                                        validMapping = Local_Mapping;
                                        break;
                                    }
                                }
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
                                        // Loop instead of abMasters.FirstOrDefault(lambda):
                                        // that boxed the Dictionary enumerator and allocated a
                                        // closure. Dictionary foreach uses a struct enumerator
                                        // and preserves the exact same iteration order.
                                        var Local_CurrentInputIndex = current.InputSource.Index;
                                        DSP_Stream? AbstractMasterStream = null;
                                        foreach (var Local_Master in abMasters)
                                        {
                                            if (Local_Master.Value.InputSource.Index == Local_CurrentInputIndex)
                                            {
                                                AbstractMasterStream = Local_Master.Value;
                                                break;
                                            }
                                        }

                                        chain.Add(AbstractMasterStream!);
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
                // Reverse the chain so it runs from start to endpoint. (In-place, no allocation.)
                chain.Reverse();
                rawChains.Add(chain);
            }
            else
            {
                //Recycle the rejected chain list instead of dropping it on the floor.
                this.ReturnRawChainList(chain);
            }
        }

        return rawChains;
    }

    /// <summary>
    /// Takes a reusable per-chain list out of the raw chain pool, growing the pool only when
    /// the topology genuinely needs more chains than were ever needed before.
    /// </summary>
    /// <returns>An empty list ready to be filled.</returns>
    protected List<DSP_Stream> RentRawChainList()
    {
        if (this.RawChainListPool.Count > 0)
        {
            var Local_Pooled = this.RawChainListPool.Pop();
            Local_Pooled.Clear();
            return Local_Pooled;
        }
        return new List<DSP_Stream>(8);
    }

    /// <summary>
    /// Returns a raw chain list to the pool. Raw chains never escape a single
    /// BuildStreamChains() call (PostProcessChain copies their contents out), so recycling
    /// them cannot hand a live list back to a running DSP task.
    /// </summary>
    /// <param name="list">The list to recycle.</param>
    protected void ReturnRawChainList(List<DSP_Stream> list)
    {
        list.Clear();
        this.RawChainListPool.Push(list);
    }

    /// <summary>
    /// Computes a cheap, allocation-free signature of the upstream chain path.
    /// Replaces the original StringBuilder-per-node + string-per-node implementation with a
    /// 64 bit FNV-1a rolling hash over the exact same per-node hash codes. The node count is
    /// folded in as well so a prefix can never collide with a longer path that starts with it.
    /// </summary>
    /// <param name="chain">The chain to sign.</param>
    /// <param name="upToIndex">Number of leading nodes to include.</param>
    /// <returns>The signature value.</returns>
    protected long ComputeChainSignature(List<DSP_Stream> chain, int upToIndex)
    {
        unchecked
        {
            const ulong FNV_OffsetBasis = 14695981039346656037UL;
            const ulong FNV_Prime = 1099511628211UL;

            ulong Local_Hash = FNV_OffsetBasis;
            for (int i = 0; i < upToIndex; i++)
            {
                uint Local_NodeHash = (uint)chain[i].GetHashCode();
                //Mix the node hash in one byte at a time, which is what gives FNV-1a its
                //avalanche behaviour (folding a whole int in one step diffuses poorly).
                for (int b = 0; b < 4; b++)
                {
                    Local_Hash ^= (byte)(Local_NodeHash >> (b * 8));
                    Local_Hash *= FNV_Prime;
                }
            }
            Local_Hash ^= (uint)upToIndex;
            Local_Hash *= FNV_Prime;
            return (long)Local_Hash;
        }
    }

    // ----- Post-Process a Raw Chain to Inject Cloned Masters with Caching -----
    // Now when injecting an AbstractBus master clone we first compute the upstream chain signature.
    // If that signature was seen before for this AbstractBus index, we reuse the clone.
    protected List<DSP_Stream>? PostProcessChain(List<DSP_Stream> rawChain, Dictionary<int, DSP_Stream> abMasters)
    {
        //Allocating convenience overload, kept for API/test compatibility. The hot path uses the
        //destination-supplying overload below so that nothing is allocated per callback.
        var finalChain = new List<DSP_Stream>(rawChain.Count + 1);
        return this.PostProcessChain(rawChain, abMasters, finalChain) ? finalChain : null;
    }

    /// <summary>
    /// Post-processes a raw chain into a caller supplied destination list, injecting cached
    /// AbstractBus master clones. Behaviourally identical to the allocating overload.
    /// </summary>
    /// <param name="rawChain">The raw (already reversed) chain.</param>
    /// <param name="abMasters">The AbstractBus master lookup.</param>
    /// <param name="finalChain">Destination list; cleared by this method.</param>
    /// <returns>False when the chain is invalid (no master for a referenced AbstractBus).</returns>
    protected bool PostProcessChain(List<DSP_Stream> rawChain,
                                    Dictionary<int, DSP_Stream> abMasters,
                                    List<DSP_Stream> finalChain)
    {
        finalChain.Clear();

        int Local_SamplesPerChannel = this.SamplesPerChannel;
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
                // The original unconditionally did `new double[SamplesPerChannel]` here, i.e. it
                // allocated one buffer per AbstractBus node per chain on EVERY buffer switch.
                // The buffer is fully overwritten by DSP_Process_Channel's input-gain loop before
                // it is ever read, so re-using an existing correctly sized buffer is bit-identical
                // - and it matches the length guard DSP_Process_Channel already applies itself.
                if (stream.AbstractBusBuffer.Length != Local_SamplesPerChannel)
                    stream.AbstractBusBuffer = new double[Local_SamplesPerChannel];

                int abIndex = stream.OutputDestination.Index;
                if (!abMasters.TryGetValue(abIndex, out var master))
                {
                    // If no master exists, mark the chain as invalid.
                    return false;
                }

                finalChain.Add(stream);

                // Compute a signature of the chain so far (upstream path).
                long signature = this.ComputeChainSignature(finalChain, finalChain.Count);
                var cloneKey = new AbstractBusCloneKey(abIndex, signature);

                // Attempt to reuse a previously cloned master if the chain is unchanged.
                if (this.AbstractBusCloneCache.TryGetValue(cloneKey, out var cachedClone))
                {
                    finalChain.Add(cachedClone);
                    _ = this.UsedCloneKeys.Add(cloneKey);
                }
                else
                {
                    var clonedMaster = this.CloneAbstractBusStream(master);
                    this.AbstractBusCloneCache[cloneKey] = clonedMaster;
                    _ = this.UsedCloneKeys.Add(cloneKey);
                    finalChain.Add(clonedMaster);
                }
            }
            else
            {
                finalChain.Add(stream);
            }
        }
        return true;
    }

    // ----- Check for Chain Validity -----
    protected bool IsValidChain(List<DSP_Stream> chain)
    {
        //Plain loops instead of chain.Any(lambda): List<T>.Any() boxes the list's struct
        //enumerator on every call, twice per chain per callback.
        bool hasAbstractBus = false;
        bool hasBuses = false;
        for (int i = 0; i < chain.Count; i++)
        {
            var s = chain[i];
            if (s.InputSource?.StreamType == StreamType.AbstractBus ||
                s.OutputDestination?.StreamType == StreamType.AbstractBus)
                hasAbstractBus = true;

            if (s.InputSource?.StreamType == StreamType.Bus ||
                s.OutputDestination?.StreamType == StreamType.Bus)
                hasBuses = true;

            if (hasAbstractBus && hasBuses)
                break;
        }

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

        // Build into the currently INACTIVE half of the double buffer, re-using its per-chain
        // lists. Nothing that is currently published in ChainCache is touched here.
        var finalChains = this.ChainCache_Scratch;
        int Local_Written = 0;
        for (int i = 0; i < rawChains.Count; i++)
        {
            var chain = rawChains[i];
            if (!this.IsValidChain(chain))
                continue;

            List<DSP_Stream> Local_Target;
            if (Local_Written < finalChains.Count)
                Local_Target = finalChains[Local_Written];
            else
            {
                Local_Target = new List<DSP_Stream>(8);
                finalChains.Add(Local_Target);
            }

            if (!this.PostProcessChain(chain, abMasters, Local_Target) || Local_Target.Count == 0)
                continue;

            Local_Written++;
        }
        //Drop any surplus chains left over from a previous (larger) topology. RemoveRange does
        //not allocate, and this only happens when the configuration actually shrinks.
        if (finalChains.Count > Local_Written)
            finalChains.RemoveRange(Local_Written, finalChains.Count - Local_Written);

        // Only update the persistent cache if changes are detected. Swapping the two halves of
        // the double buffer keeps the previous cache list alive as next call's scratch, so no
        // List is allocated here in steady state.
        if (!this.AreChainsEqual(this.ChainCache, finalChains))
        {
            this.ChainCache_Scratch = this.ChainCache;
            this.ChainCache = finalChains;
        }

        // Remove unused cache entries. Enumerating the Dictionary itself (rather than its .Keys
        // collection, which allocates a KeyCollection wrapper) uses a struct enumerator, so
        // collecting into the reusable scratch list replaces the old .Where().ToList() pair
        // with zero allocations.
        var keysToRemove = this.CloneKeysToRemove_Scratch;
        keysToRemove.Clear();
        foreach (var entry in this.AbstractBusCloneCache)
        {
            if (!this.UsedCloneKeys.Contains(entry.Key))
                keysToRemove.Add(entry.Key);
        }
        for (int i = 0; i < keysToRemove.Count; i++)
        {
            _ = this.AbstractBusCloneCache.Remove(keysToRemove[i]);
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
                int Local_ChainCount = chains.Count;

                // Pre-allocated Task[] + pre-built per-chain workers. The original allocated a
                // List<Task>, a Task[] (via ToArray) and one closure + one delegate per chain,
                // on every single buffer switch.
                var Local_Tasks = this.EnsureStreamTaskList(Local_ChainCount);
                var Local_Workers = this.EnsureChainWorkers(Local_ChainCount);
                for (int i = 0; i < Local_ChainCount; i++)
                {
                    var Local_Worker = Local_Workers[i];
                    Local_Worker.Prepare(this, chains);
                    Local_Tasks[i] = Task.Run(Local_Worker.Run);
                }
                Task.WaitAll(Local_Tasks, 500);
            }
            catch (Exception ex)
            {
                //DEFECT FIX: Task.WaitAll wraps worker faults in an AggregateException, so the
                //original 'ex is not IndexOutOfRangeException' filter never matched and the
                //deliberately-tolerated "user edited the stream list while the DSP was running"
                //race was rethrown here instead of being swallowed the way the single-threaded
                //path does. Unwrap the aggregate before deciding.
                if (!IsToleratedTransientDspFault(ex))
                    throw;
            }
        }

        this.OutputBufferConversion_ProcessingTime.Start();
        this.DSP_ASIO_Data.SetAsJaggedSamples(this.OutputBuffer);
        this.OutputBufferConversion_ProcessingTime.Stop();
    }

    /// <summary>
    /// True when the fault is the known, deliberately-tolerated indexing race caused by the user
    /// adding/removing a stream while the DSP is running. Unwraps AggregateException (thrown by
    /// Task.WaitAll on the multi-threaded path) so both DSP paths tolerate the same set.
    /// </summary>
    /// <param name="ex">The caught exception.</param>
    /// <returns>True to swallow, false to rethrow.</returns>
    protected static bool IsToleratedTransientDspFault(Exception ex)
    {
        if (ex is AggregateException Local_Aggregate)
        {
            var Local_Inner = Local_Aggregate.Flatten().InnerExceptions;
            if (Local_Inner.Count == 0)
                return false;

            for (int i = 0; i < Local_Inner.Count; i++)
            {
                if (!IsToleratedTransientDspFault(Local_Inner[i]))
                    return false;
            }
            return true;
        }

        return ex is IndexOutOfRangeException or ArgumentOutOfRangeException;
    }

    /// <summary>
    /// Returns an exactly sized Task array for Task.WaitAll, re-allocating only when the number
    /// of DSP chains changes (Task.WaitAll rejects arrays containing nulls, so the array cannot
    /// simply be over-sized).
    /// </summary>
    /// <param name="count">The required number of tasks.</param>
    /// <returns>A reusable Task array of exactly <paramref name="count"/> entries.</returns>
    protected Task[] EnsureStreamTaskList(int count)
    {
        var Local_TaskList = this.StreamTaskList;
        if (Local_TaskList == null || Local_TaskList.Length != count)
        {
            Local_TaskList = count == 0 ? [] : new Task[count];
            this.StreamTaskList = Local_TaskList;
        }
        return Local_TaskList;
    }

    /// <summary>
    /// Returns the pre-built per-chain workers, growing (never shrinking) the array only when a
    /// topology needs more chains than have ever been needed before. Worker i is permanently
    /// bound to chain index i, so the steady state performs no writes to any worker.
    /// </summary>
    /// <param name="count">The required number of workers.</param>
    /// <returns>An array with at least <paramref name="count"/> workers.</returns>
    protected ChainWorker[] EnsureChainWorkers(int count)
    {
        var Local_Workers = this.ChainWorkers;
        if (Local_Workers.Length < count)
        {
            var Local_Grown = new ChainWorker[count];
            Array.Copy(Local_Workers, Local_Grown, Local_Workers.Length);
            for (int i = Local_Workers.Length; i < count; i++)
                Local_Grown[i] = new ChainWorker(i);

            Local_Workers = Local_Grown;
            this.ChainWorkers = Local_Workers;
        }
        return Local_Workers;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    protected void DSP_ManualBackgroundThread()
    {
        while (true) //Keep-alive
        {
            _ = this.DSP_RunOnce_ARE.WaitOne(); //Pause the thread until signaled
            if (!this.DSP_AllowedToRun) //Check if we should run
                break; //Breaks out of keep-alive loop which ends the long-running background thread cleanly

            //DEFECT FIX: the try/catch used to wrap the whole while-loop, so a single transient
            //exception permanently killed this long-running thread. The thread is only started
            //once (in the constructor) and is never restarted, and the real-time ASIO callback
            //waits on DSP_PassCompleted_ARE with NO timeout - so a dead thread hung the driver
            //callback forever. The catch now guards one pass only, and the completion signal is
            //raised from a finally so the ASIO callback is always released.
            try
            {
                if (this.IsMultiThreadingEnabled)
                    this.DSP_MultiThreaded(); //MT on the background thread
                else
                    this.DSP_SingleThreaded(); //ST on the background thread
            }
            catch (Exception ex)
            {
                //DEFECT FIX: this used to call this.Error(ex) -> Debug.Error(ex), which shows up to
                //three MODAL dialogs and can rethrow. Running that here re-introduced the very
                //deadlock the finally below exists to prevent: a finally only runs once the catch
                //body has completed, so a modal dialog on this real-time thread blocked the signal
                //and left the ASIO driver callback waiting on DSP_PassCompleted_ARE forever (it
                //waits with NO timeout). A real-time audio thread must never block on UI.
                //ReportSwallowed records the error and raises SwallowedErrorReported without ever
                //showing UI or rethrowing, so the fault stays observable (tests, a debugger
                //breakpoint, or a GUI subscriber that surfaces it on the UI thread later) while the
                //audio callback is always released promptly.
                Debug.ReportSwallowed(ex);
            }
            finally
            {
                _ = this.DSP_RunOnce_ARE.Reset(); //Tell the thread it is ready to pause
                _ = this.DSP_PassCompleted_ARE.Set(); //Signal that we are done
            }
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

        //
        // Fallback-buffer strategy (all three fallbacks below used to be a fresh
        // `new double[SamplesPerChannel]` on the hot path):
        //
        //  * INPUT fallbacks use this.SharedZeroInputBuffer - a single, process-wide,
        //    all-zeros array. It is only ever READ from (the input-gain loop reads it and writes
        //    into Local_OutputBuffer), never written to and never handed to a filter, so sharing
        //    it between concurrently running chains is provably race-free: every reader only
        //    ever sees zeros, which is exactly what a freshly allocated array would have given.
        //
        //  * OUTPUT fallbacks use a PER-STREAM buffer (DSP_Stream.GetFallbackOutputBuffer).
        //    A shared writable scratch buffer would be a genuine data race between chains, so it
        //    is deliberately not used here. The per-stream buffer's contents are discarded (the
        //    configured Bus does not exist, so nothing downstream can read it); the only way two
        //    threads can touch the same one is when the very same DSP_Stream instance appears in
        //    two chains, and even then both write the same length of throw-away data.
        //
        int Local_FallbackLength = this.SamplesPerChannel;
        var Local_ZeroInputBuffer = this.SharedZeroInputBuffer;
        if (Local_ZeroInputBuffer.Length < Local_FallbackLength)
        {
            //Benign race: concurrent chains may each publish their own all-zeros array here,
            //they are value-identical and each caller uses the local it just validated.
            Local_ZeroInputBuffer = new double[Local_FallbackLength];
            this.SharedZeroInputBuffer = Local_ZeroInputBuffer;
        }

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
                Local_OutputBuffer = Bus?.Buffer ?? currentStream.GetFallbackOutputBuffer(Local_FallbackLength);
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
                Local_InputBuffer = Bus?.Buffer ?? Local_ZeroInputBuffer;
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
                //If previous stream doesn't exist then a read-only all-zeros array, otherwise use it
                if (previousStream == null || previousStream.AbstractBusBuffer == null)
                {
                    Local_InputBuffer = Local_ZeroInputBuffer;
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
    protected virtual void Show_ASIO_ControlPanel()
    {
        if (string.IsNullOrEmpty(this.DeviceName))
            throw new InvalidOperationException("DeviceName isn't set");

        using var asio = new ASIO_Unified(this.DeviceName);
        asio.ShowControlPanel();
    }

    protected virtual void Show_ASIO_ControlPanel(string deviceName)
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
        this.ChainCache_Scratch.Clear();
        this.AbstractBusCloneCache.Clear();
        this.UsedCloneKeys.Clear();

        //Chain building scratch space
        this.AbMasters_Scratch.Clear();
        this.AbMasterDuplicates_Scratch.Clear();
        this.AbMasters_Raw_Scratch.Clear();
        this.AbMasterDuplicates_Raw_Scratch.Clear();
        this.ValidStreams_Scratch.Clear();
        this.BusProduced_Scratch.Clear();
        this.EndStreams_Scratch.Clear();
        this.RawChains_Scratch.Clear();
        this.CloneKeysToRemove_Scratch.Clear();
        this.RawChainListPool.Clear();
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