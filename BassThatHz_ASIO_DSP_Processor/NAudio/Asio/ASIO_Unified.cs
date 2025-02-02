#nullable disable

namespace NAudio.Wave
{
    using System;
    using System.Runtime.CompilerServices;
    using NAudio.Wave.Asio;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Text;
    using Microsoft.Win32;

    /// <summary>
    /// Original Contributor: Mark Heath 
    /// New Contributor to C# binding : Alexandre Mutel - email: alexandre_mutel at yahoo.fr
    /// Unified and Refactored by BassThatHz - 2023
    /// </summary>
    public class ASIO_Unified : IDisposable
    {
        #region Variables
        protected AsioAudioAvailableEventArgs On_Has_ASIO_Data_Args = null;

        //ASIO Driver Ext
        protected AsioCallbacks callbacks;
        protected AsioDriverCapability capability;
        protected AsioBufferInfo[] bufferInfos;
        protected bool isOutputReadySupported;
        protected IntPtr[] currentOutputBuffers;
        protected IntPtr[] currentInputBuffers;
        protected int numberOfOutputChannels;
        protected int numberOfInputChannels;
        protected int bufferSize;
        protected int outputChannelOffset;
        protected int inputChannelOffset;
        public Action Driver_ResetRequestCallback;
        public Action Driver_BufferSizeChangedCallback;
        public Action Driver_ResyncRequestCallback;
        public Action Driver_LatenciesChangedCallback;
        public Action Driver_OverloadCallback;
        public Action Driver_SampleRateChangedCallback;

        //ASIO Driver
        protected IntPtr pAsioComObject;
        protected IntPtr pinnedcallbacks;
        protected AsioDriverVTable asioDriverVTable;

        #endregion

        #region ASIO

        #region EventHandlers
        public event EventHandler<AsioAudioAvailableEventArgs> AudioAvailable;
        public event EventHandler DriverResetRequest;
        #endregion

        #region Constructors, Dispose
        public ASIO_Unified(string driverName)
        {
            //this.SyncContext = System.Threading.SynchronizationContext.Current;

            this.DriverName = driverName;
            this.GetAsioDriverByName(this.DriverName);
            this.AsioDriverExt();
            this.Driver_ResetRequestCallback = this.OnDriverResetRequest;
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public ASIO_Unified(int driverIndex)
        {
            // this.SyncContext = System.Threading.SynchronizationContext.Current;

            String[] names = GetDriverNames();
            if (names.Length == 0)
                throw new ArgumentException("There is no ASIO Driver installed on your system");

            if (driverIndex < 0 || driverIndex > names.Length)
                throw new ArgumentException(String.Format("Invalid device number. Must be in the range [0,{0}]", names.Length));

            this.DriverName = names[driverIndex];
            this.GetAsioDriverByName(this.DriverName);

            this.Driver_ResetRequestCallback = this.OnDriverResetRequest;
        }

        public void Dispose()
        {
            if (this.PlaybackState != PlaybackState.Stopped)
                this.Stop();

            try
            {
                AsioError result = this.asioDriverVTable.disposeBuffers(this.pAsioComObject);
                _ = result;
                Marshal.FreeHGlobal(this.pinnedcallbacks);
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex.ToString());
            }

            //ReleaseComAsioDriver
            _ = Marshal.Release(this.pAsioComObject);
        }
        #endregion

        #endregion

        #region Public Properties

        #region Read Only
        public string DriverName { get; protected set; }
        public bool IsInitalized { get; protected set; }
        public PlaybackState PlaybackState { get; protected set; }
        public int NumberOfOutputChannels { get; protected set; }
        public int NumberOfInputChannels { get; protected set; }
        public int SamplesPerBuffer { get; protected set; }
        #endregion

        public bool AutoStop { get; set; }
        public int OutputChannelOffset { get; set; }
        public int InputChannelOffset { get; set; }

        /// <summary>
        /// Gets the driver Capabilities
        /// </summary>
        public AsioDriverCapability GetDriverCapabilities
        {
            get
            {
                return this.capability;
            }
        }
        #endregion

        #region Public Members

        public void ShowControlPanel() => this.HandleException(this.asioDriverVTable.controlPanel(this.pAsioComObject), "controlPanel");

        public bool IsSampleRateSupported(int sampleRate) => this.CanSampleRate(sampleRate);
        public int DriverInputChannelCount => this.capability.NbInputChannels;

        public int DriverOutputChannelCount => this.capability.NbOutputChannels;

        public string AsioInputChannelName(int channel) =>
                    channel > this.DriverInputChannelCount ?
                    "" :
                    this.capability.InputChannelInfos[channel].name;

        public string AsioOutputChannelName(int channel) =>
                    channel > this.DriverOutputChannelCount ?
                    "" :
                    this.capability.OutputChannelInfos[channel].name;

        /// <summary>
        /// returns Tuple: int InputLatency, int OutputLatency
        /// </summary>
        public Tuple<int, int> PlaybackLatency
        {
            get
            {
                this.GetLatencies(out int InputLatency, out int OutputLatency);
                return new Tuple<int, int>(InputLatency, OutputLatency);
            }
        }

        #endregion

        #region Public Functions
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public void Init(int numberOfInputChannels, int numberOfOutputChannels, int desiredSampleRate, int outputChannelOffset, int inputChannelOffset)
        {
            if (this.IsInitalized)
                throw new InvalidOperationException("Already initialised this instance of Asio");
            this.IsInitalized = true;

            if (numberOfInputChannels <= 0)
                throw new ArgumentOutOfRangeException(nameof(numberOfInputChannels), "must be a postive number");

            if (numberOfOutputChannels <= 0)
                throw new ArgumentOutOfRangeException(nameof(numberOfOutputChannels), "must be a postive number");

            if (!this.IsSampleRateSupported(desiredSampleRate))
                throw new ArgumentException("SampleRate is not supported");

            if (capability.SampleRate != desiredSampleRate)
                this.SetSampleRate(desiredSampleRate);

            this.NumberOfInputChannels = numberOfInputChannels;
            this.NumberOfOutputChannels = numberOfOutputChannels;
            this.Set_IO_Channels(this.NumberOfOutputChannels, this.NumberOfInputChannels);

            // will throw an exception if channel offset is too high
            this.SetChannelOffset(outputChannelOffset, inputChannelOffset);

            // Used Prefered size of ASIO Buffer
            this.SamplesPerBuffer = this.CreateBuffers(false);
        }

        public void Start()
        {
            if (this.PlaybackState != PlaybackState.Playing)
            {
                this.On_Has_ASIO_Data_Args = null;
                this.PlaybackState = PlaybackState.Playing;
                this.HandleException(asioDriverVTable.start(pAsioComObject), "start");
            }
        }

        public AsioError Stop()
        {
            this.PlaybackState = PlaybackState.Stopped;
            return this.asioDriverVTable.stop(pAsioComObject);
        }

        #endregion

        #region Protected Functions

        protected void OnDriverResetRequest() => this.DriverResetRequest?.Invoke(this, EventArgs.Empty);

        #endregion

        #region AsioDriverExt
        /// <summary>
        /// Initializes a new instance of the <see cref="AsioDriverExt"/> class based on an already
        /// instantiated AsioDriver instance.
        /// </summary>
        /// <param name="driver">A AsioDriver already instantiated.</param>
        protected void AsioDriverExt()
        {
            if (!Init(IntPtr.Zero))
            {
                throw new AsioException(GetErrorMessage());
            }

            this.callbacks = new AsioCallbacks();
            this.callbacks.pasioMessage = this.AsioMessageCallBack;
            this.callbacks.pbufferSwitch = this.BufferSwitchCallBack;
            this.callbacks.pbufferSwitchTimeInfo = this.BufferSwitchTimeInfoCallBack;
            this.callbacks.psampleRateDidChange = this.SampleRateDidChangeCallBack;

            this.BuildCapabilities();
        }

        protected void Set_IO_Channels(int numberOfOutputChannels, int numberOfInputChannels)
        {
            if (numberOfOutputChannels < 0 || numberOfOutputChannels > this.capability.NbOutputChannels)
            {
                throw new ArgumentException(
                    $"Invalid number of channels {numberOfOutputChannels}, must be in the range [0,{this.capability.NbOutputChannels}]");
            }
            if (numberOfInputChannels < 0 || numberOfInputChannels > this.capability.NbInputChannels)
            {
                throw new ArgumentException("numberOfInputChannels",
                    $"Invalid number of input channels {numberOfInputChannels}, must be in the range [0,{this.capability.NbInputChannels}]");
            }

            this.numberOfOutputChannels = numberOfOutputChannels;
            this.numberOfInputChannels = numberOfInputChannels;
        }

        /// <summary>
        /// Allows adjustment of which is the first output channel we write to
        /// </summary>
        /// <param name="outputChannelOffset">Output Channel offset</param>
        /// <param name="inputChannelOffset">Input Channel offset</param>
        protected void SetChannelOffset(int outputChannelOffset, int inputChannelOffset)
        {

            if (outputChannelOffset + this.numberOfOutputChannels <= this.capability.NbOutputChannels)
            {
                this.outputChannelOffset = outputChannelOffset;
            }
            else
            {
                throw new ArgumentException("Invalid channel offset");
            }
            if (inputChannelOffset + this.numberOfInputChannels <= this.capability.NbInputChannels)
            {
                this.inputChannelOffset = inputChannelOffset;
            }
            else
            {
                throw new ArgumentException("Invalid channel offset");
            }

        }

        /// <summary>
        /// Releases this instance.
        /// </summary>

        /// <summary>
        /// Sets the sample rate.
        /// </summary>
        /// <param name="sampleRate">The sample rate.</param>
        protected void SetSampleRate(double sampleRate)
        {
            this.AsioDriver_SetSampleRate(sampleRate);
            // Update Capabilities
            this.BuildCapabilities();
        }

        /// <summary>
        /// Creates the buffers for playing.
        /// </summary>
        /// <param name="numberOfOutputChannels">The number of outputs channels.</param>
        /// <param name="numberOfInputChannels">The number of input channel.</param>
        /// <param name="useMaxBufferSize">if set to <c>true</c> [use max buffer size] else use Prefered size</param>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        protected int CreateBuffers(bool useMaxBufferSize)
        {
            // Ask for maximum of output channels even if we use only the nbOutputChannelsArg
            int nbTotalChannels = this.capability.NbInputChannels + this.capability.NbOutputChannels;
            this.bufferInfos = new AsioBufferInfo[nbTotalChannels];
            this.currentOutputBuffers = new IntPtr[this.numberOfOutputChannels];
            this.currentInputBuffers = new IntPtr[this.numberOfInputChannels];

            // and do the same for output channels
            // ONLY work on output channels (just put isInput = true for InputChannel)
            int totalIndex = 0;
            for (int index = 0; index < this.capability.NbInputChannels; index++, totalIndex++)
            {
                this.bufferInfos[totalIndex].isInput = true;
                this.bufferInfos[totalIndex].channelNum = index;
                this.bufferInfos[totalIndex].pBuffer0 = IntPtr.Zero;
                this.bufferInfos[totalIndex].pBuffer1 = IntPtr.Zero;
            }

            for (int index = 0; index < this.capability.NbOutputChannels; index++, totalIndex++)
            {
                this.bufferInfos[totalIndex].isInput = false;
                this.bufferInfos[totalIndex].channelNum = index;
                this.bufferInfos[totalIndex].pBuffer0 = IntPtr.Zero;
                this.bufferInfos[totalIndex].pBuffer1 = IntPtr.Zero;
            }

            if (useMaxBufferSize)
            {
                // use the drivers maximum buffer size
                this.bufferSize = this.capability.BufferMaxSize;
            }
            else
            {
                // use the drivers preferred buffer size
                this.bufferSize = this.capability.BufferPreferredSize;
            }

            unsafe
            {
                fixed (AsioBufferInfo* infos = &this.bufferInfos[0])
                {
                    IntPtr pOutputBufferInfos = new IntPtr(infos);

                    // Create the ASIO Buffers with the callbacks
                    this.CreateBuffers(pOutputBufferInfos, nbTotalChannels, this.bufferSize, ref this.callbacks);
                }
            }

            // Call outputReady
            this.isOutputReadySupported = this.asioDriverVTable.outputReady(this.pAsioComObject) == AsioError.ASE_OK;

            return this.bufferSize;
        }

        /// <summary>
        /// Builds the capabilities internally.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        protected void BuildCapabilities()
        {
            this.capability = new AsioDriverCapability();

            this.capability.DriverName = GetDriverName();

            // Get nb Input/Output channels
            this.GetChannels(out this.capability.NbInputChannels, out this.capability.NbOutputChannels);

            this.capability.InputChannelInfos = new AsioChannelInfo[this.capability.NbInputChannels];
            this.capability.OutputChannelInfos = new AsioChannelInfo[this.capability.NbOutputChannels];

            // Get ChannelInfo for Inputs
            for (int i = 0; i < this.capability.NbInputChannels; i++)
            {
                this.capability.InputChannelInfos[i] = this.GetChannelInfo(i, true);
            }

            // Get ChannelInfo for Output
            for (int i = 0; i < this.capability.NbOutputChannels; i++)
            {
                this.capability.OutputChannelInfos[i] = this.GetChannelInfo(i, false);
            }

            // Get the current SampleRate
            this.capability.SampleRate = this.GetSampleRate();

            var error = this.GetLatencies(out this.capability.InputLatency, out this.capability.OutputLatency);
            // focusrite scarlett 2i4 returns ASE_NotPresent here

            if (error != AsioError.ASE_OK && error != AsioError.ASE_NotPresent)
            {
                var ex = new AsioException("ASIOgetLatencies");
                ex.Error = error;
                throw ex;
            }

            // Get BufferSize
            this.GetBufferSize(out this.capability.BufferMinSize, out this.capability.BufferMaxSize,
                out this.capability.BufferPreferredSize, out this.capability.BufferGranularity);
        }

        /// <summary>
        /// Callback called by the AsioDriver on fill buffer demand. Redirect call to external callback.
        /// </summary>
        /// <param name="doubleBufferIndex">Index of the double buffer.</param>
        /// <param name="directProcess">if set to <c>true</c> [direct process].</param>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        protected void BufferSwitchCallBack(int doubleBufferIndex, bool directProcess)
        {
            for (int i = 0; i < this.numberOfInputChannels; i++)
                this.currentInputBuffers[i] = this.bufferInfos[i].Buffer(doubleBufferIndex);

            var IndexOffset = this.capability.NbInputChannels;
            for (int i = 0; i < this.numberOfOutputChannels; i++)
                this.currentOutputBuffers[i] = this.bufferInfos[i + IndexOffset].Buffer(doubleBufferIndex);

            if (this.On_Has_ASIO_Data_Args == null)
            {
                this.On_Has_ASIO_Data_Args =
                        new AsioAudioAvailableEventArgs
                        (
                            this.currentInputBuffers,
                            this.currentOutputBuffers,
                            this.SamplesPerBuffer,
                            this.capability.InputChannelInfos[0].type
                        );
            }
            else
            {
                this.On_Has_ASIO_Data_Args.Init(this.currentInputBuffers,
                                            this.currentOutputBuffers,
                                            this.SamplesPerBuffer,
                                            this.capability.InputChannelInfos[0].type);
            }

            this.AudioAvailable?.Invoke(this, this.On_Has_ASIO_Data_Args);

            if (this.isOutputReadySupported)
                _ = this.asioDriverVTable.outputReady(this.pAsioComObject);
        }

        /// <summary>
        /// Callback called by the AsioDriver on event "Samples rate changed".
        /// </summary>
        /// <param name="sRate">The sample rate.</param>
        protected void SampleRateDidChangeCallBack(double sRate)
        {
            // Check when this is called?
            this.capability.SampleRate = sRate;
            this.Driver_SampleRateChangedCallback?.Invoke();
        }

        /// <summary>
        /// Asio message call back.
        /// </summary>
        /// <param name="selector">The selector.</param>
        /// <param name="value">The value.</param>
        /// <param name="message">The message.</param>
        /// <param name="opt">The opt.</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        protected int AsioMessageCallBack(AsioMessageSelector selector, int value, IntPtr message, IntPtr opt)
        {
            // Check when this is called?
            switch (selector)
            {
                case AsioMessageSelector.kAsioSelectorSupported:
                    AsioMessageSelector subValue = (AsioMessageSelector)Enum.ToObject(typeof(AsioMessageSelector), value);
                    switch (subValue)
                    {
                        case AsioMessageSelector.kAsioEngineVersion:
                            return 1;
                        case AsioMessageSelector.kAsioResetRequest:
                            this.Driver_ResetRequestCallback?.Invoke();
                            return 0;
                        case AsioMessageSelector.kAsioBufferSizeChange:
                            this.Driver_BufferSizeChangedCallback?.Invoke();
                            return 0;
                        case AsioMessageSelector.kAsioResyncRequest:
                            this.Driver_ResyncRequestCallback?.Invoke();
                            return 0;
                        case AsioMessageSelector.kAsioLatenciesChanged:
                            this.Driver_LatenciesChangedCallback?.Invoke();
                            return 0;
                        case AsioMessageSelector.kAsioSupportsTimeInfo:
                            //                            return 1; DON'T SUPPORT FOR NOW. NEED MORE TESTING.
                            return 0;
                        case AsioMessageSelector.kAsioSupportsTimeCode:
                            //                            return 1; DON'T SUPPORT FOR NOW. NEED MORE TESTING.
                            return 0;
                        case AsioMessageSelector.kAsioOverload:
                            this.Driver_OverloadCallback?.Invoke();
                            return 0;
                    }
                    break;
                case AsioMessageSelector.kAsioEngineVersion:
                    return 2;
                case AsioMessageSelector.kAsioResetRequest:
                    this.Driver_ResetRequestCallback?.Invoke();
                    return 1;
                case AsioMessageSelector.kAsioBufferSizeChange:
                    this.Driver_BufferSizeChangedCallback?.Invoke();
                    return 0;
                case AsioMessageSelector.kAsioResyncRequest:
                    this.Driver_ResyncRequestCallback?.Invoke();
                    return 0;
                case AsioMessageSelector.kAsioLatenciesChanged:
                    this.Driver_LatenciesChangedCallback?.Invoke();
                    return 0;
                case AsioMessageSelector.kAsioSupportsTimeInfo:
                    return 0;
                case AsioMessageSelector.kAsioSupportsTimeCode:
                    return 0;
                case AsioMessageSelector.kAsioOverload:
                    this.Driver_OverloadCallback?.Invoke();
                    return 0;
            }
            return 0;
        }

        /// <summary>
        /// Buffers switch time info call back.
        /// </summary>
        /// <param name="asioTimeParam">The asio time param.</param>
        /// <param name="doubleBufferIndex">Index of the double buffer.</param>
        /// <param name="directProcess">if set to <c>true</c> [direct process].</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected IntPtr BufferSwitchTimeInfoCallBack(IntPtr asioTimeParam, int doubleBufferIndex, bool directProcess)
        {
            // Check when this is called?
            return IntPtr.Zero;
        }
        #endregion

        #region AsioDriver

        /// <summary>
        /// Gets the ASIO driver names installed.
        /// </summary>
        /// <returns>a list of driver names. Use this name to GetAsioDriverByName</returns>
        public static string[] GetDriverNames()
        {
            var names = Array.Empty<string>();
            var regKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\ASIO");

            if (regKey != null)
            {
                names = regKey.GetSubKeyNames();
                regKey.Close();
            }

            return names;
        }

        /// <summary>
        /// Instantiate a AsioDriver given its name.
        /// </summary>
        /// <param name="name">The name of the driver</param>
        /// <returns>an AsioDriver instance</returns>
        protected void GetAsioDriverByName(String name)
        {
            string guid = string.Empty;
            var regKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\ASIO\\" + name);
            if (regKey == null)
            {
                throw new ArgumentException($"Driver Name {name} doesn't exist");
            }
            guid = regKey?.GetValue("CLSID")?.ToString();

            this.InitFromGuid(new Guid(guid));
        }

        /// <summary>
        /// Inits the AsioDriver..
        /// </summary>
        /// <param name="sysHandle">The sys handle.</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        protected bool Init(IntPtr sysHandle)
        {
            int ret = this.asioDriverVTable.init(pAsioComObject, sysHandle);
            return ret == 1;
        }

        /// <summary>
        /// Gets the name of the driver.
        /// </summary>
        /// <returns></returns>
        protected string GetDriverName()
        {
            var name = new StringBuilder(256);
            this.asioDriverVTable.getDriverName(pAsioComObject, name);
            return name.ToString();
        }

        /// <summary>
        /// Gets the driver version.
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public int AsioDriver_GetDriverVersion()
        {
            return this.asioDriverVTable.getDriverVersion(pAsioComObject);
        }

        /// <summary>
        /// Gets the error message.
        /// </summary>
        /// <returns></returns>
        protected string GetErrorMessage()
        {
            var errorMessage = new StringBuilder(256);
            this.asioDriverVTable.getErrorMessage(pAsioComObject, errorMessage);
            return errorMessage.ToString();
        }

        /// <summary>
        /// Starts this instance.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        protected void AsioDriver_Start()
        {
            this.HandleException(this.asioDriverVTable.start(this.pAsioComObject), "start");
        }

        /// <summary>
        /// Stops this instance.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        protected AsioError AsioDriver_Stop()
        {
            return this.asioDriverVTable.stop(this.pAsioComObject);
        }

        /// <summary>
        /// Gets the number of channels.
        /// </summary>
        /// <param name="numInputChannels">The num input channels.</param>
        /// <param name="numOutputChannels">The num output channels.</param>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        protected void GetChannels(out int numInputChannels, out int numOutputChannels)
        {
            this.HandleException(this.asioDriverVTable.getChannels(this.pAsioComObject,
                out numInputChannels, out numOutputChannels), "getChannels");
        }

        /// <summary>
        /// Gets the latencies (n.b. does not throw an exception)
        /// </summary>
        /// <param name="inputLatency">The input latency.</param>
        /// <param name="outputLatency">The output latency.</param>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        protected AsioError GetLatencies(out int inputLatency, out int outputLatency)
        {
            return this.asioDriverVTable.getLatencies(this.pAsioComObject, out inputLatency, out outputLatency);
        }

        /// <summary>
        /// Gets the size of the buffer.
        /// </summary>
        /// <param name="minSize">Size of the min.</param>
        /// <param name="maxSize">Size of the max.</param>
        /// <param name="preferredSize">Size of the preferred.</param>
        /// <param name="granularity">The granularity.</param>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        protected void GetBufferSize(out int minSize, out int maxSize, out int preferredSize, out int granularity)
        {
            this.HandleException(this.asioDriverVTable.getBufferSize(pAsioComObject,
                out minSize, out maxSize, out preferredSize, out granularity), "getBufferSize");
        }

        /// <summary>
        /// Determines whether this instance can use the specified sample rate.
        /// </summary>
        /// <param name="sampleRate">The sample rate.</param>
        /// <returns>
        /// 	<c>true</c> if this instance [can sample rate] the specified sample rate; otherwise, <c>false</c>.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        protected bool CanSampleRate(double sampleRate)
        {
            var error = this.asioDriverVTable.canSampleRate(this.pAsioComObject, sampleRate);
            if (error == AsioError.ASE_NoClock)
            {
                return false;
            }
            if (error == AsioError.ASE_OK)
            {
                return true;
            }
            this.HandleException(error, "canSampleRate");
            return false;
        }

        /// <summary>
        /// Gets the sample rate.
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public double GetSampleRate()
        {
            this.HandleException(this.asioDriverVTable.getSampleRate(pAsioComObject, out double sampleRate), "getSampleRate");
            return sampleRate;
        }

        /// <summary>
        /// Sets the sample rate.
        /// </summary>
        /// <param name="sampleRate">The sample rate.</param>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        protected void AsioDriver_SetSampleRate(double sampleRate)
        {
            this.HandleException(this.asioDriverVTable.setSampleRate(this.pAsioComObject, sampleRate), "setSampleRate");
        }

        /// <summary>
        /// Gets the clock sources.
        /// </summary>
        /// <param name="clocks">The clocks.</param>
        /// <param name="numSources">The num sources.</param>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public void GetClockSources(out long clocks, int numSources)
        {
            this.HandleException(this.asioDriverVTable.getClockSources(this.pAsioComObject, out clocks, numSources), "getClockSources");
        }

        /// <summary>
        /// Sets the clock source.
        /// </summary>
        /// <param name="reference">The reference.</param>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        protected void SetClockSource(int reference)
        {
            this.HandleException(this.asioDriverVTable.setClockSource(this.pAsioComObject, reference), "setClockSources");
        }

        /// <summary>
        /// Gets the sample position.
        /// </summary>
        /// <param name="samplePos">The sample pos.</param>
        /// <param name="timeStamp">The time stamp.</param>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public void GetSamplePosition(out long samplePos, ref Asio64Bit timeStamp)
        {
            this.HandleException(this.asioDriverVTable.getSamplePosition(this.pAsioComObject,
                out samplePos, ref timeStamp), "getSamplePosition");
        }

        /// <summary>
        /// Gets the channel info.
        /// </summary>
        /// <param name="channelNumber">The channel number.</param>
        /// <param name="trueForInputInfo">if set to <c>true</c> [true for input info].</param>
        /// <returns>Channel Info</returns>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        protected AsioChannelInfo GetChannelInfo(int channelNumber, bool trueForInputInfo)
        {
            var info = new AsioChannelInfo { channel = channelNumber, isInput = trueForInputInfo };
            this.HandleException(this.asioDriverVTable.getChannelInfo(this.pAsioComObject, ref info), "getChannelInfo");
            return info;
        }

        /// <summary>
        /// Creates the buffers.
        /// </summary>
        /// <param name="bufferInfos">The buffer infos.</param>
        /// <param name="numChannels">The num channels.</param>
        /// <param name="bufferSize">Size of the buffer.</param>
        /// <param name="callbacks">The callbacks.</param>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        protected void CreateBuffers(IntPtr bufferInfos, int numChannels, int bufferSize, ref AsioCallbacks callbacks)
        {
            // next two lines suggested by droidi on codeplex issue tracker
            this.pinnedcallbacks = Marshal.AllocHGlobal(Marshal.SizeOf(callbacks));
            Marshal.StructureToPtr(callbacks, this.pinnedcallbacks, false);
            this.HandleException(this.asioDriverVTable.createBuffers(this.pAsioComObject,
                bufferInfos, numChannels, bufferSize, this.pinnedcallbacks), "createBuffers");
        }

        /// <summary>
        /// Futures the specified selector.
        /// </summary>
        /// <param name="selector">The selector.</param>
        /// <param name="opt">The opt.</param>
        protected void Future(int selector, IntPtr opt)
        {
            this.HandleException(this.asioDriverVTable.future(this.pAsioComObject, selector, opt), "future");
        }

        /// <summary>
        /// Handles the exception. Throws an exception based on the error.
        /// </summary>
        /// <param name="error">The error to check.</param>
        /// <param name="methodName">Method name</param>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        protected void HandleException(AsioError error, string methodName)
        {
            if (error != AsioError.ASE_OK && error != AsioError.ASE_SUCCESS)
            {
                var asioException = new AsioException(
                    $"Error code [{AsioException.getErrorName(error)}] while calling ASIO method <{methodName}>, {this.GetErrorMessage()}")
                {
                    Error = error
                };
                throw asioException;
            }
        }

        /// <summary>
        /// Inits the vTable method from GUID. This is a tricky part of this class.
        /// </summary>
        /// <param name="asioGuid">The ASIO GUID.</param>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        protected void InitFromGuid(Guid asioGuid)
        {
            const uint CLSCTX_INPROC_SERVER = 1;
            // Start to query the virtual table a index 3 (init method of AsioDriver)
            const int INDEX_VTABLE_FIRST_METHOD = 3;

            // Pointer to the ASIO object
            // USE CoCreateInstance instead of builtin COM-Class instantiation,
            // because the AsioDriver expect to have the ASIOGuid used for both COM Object and COM interface
            // The CoCreateInstance is working only in STAThread mode.
            int hresult = CoCreateInstance(ref asioGuid, IntPtr.Zero, CLSCTX_INPROC_SERVER, ref asioGuid, out this.pAsioComObject);
            if (hresult != 0)
            {
                throw new COMException("Unable to instantiate ASIO. Check if STAThread is set", hresult);
            }

            // The first pointer at the adress of the ASIO Com Object is a pointer to the
            // C++ Virtual table of the object.
            // Gets a pointer to VTable.
            IntPtr pVtable = Marshal.ReadIntPtr(this.pAsioComObject);

            // Instantiate our Virtual table mapping
            this.asioDriverVTable = new AsioDriverVTable();

            // This loop is going to retrieve the pointer from the C++ VirtualTable
            // and attach an internal delegate in order to call the method on the COM Object.
            FieldInfo[] fieldInfos = typeof(AsioDriverVTable).GetFields();
            for (int i = 0; i < fieldInfos.Length; i++)
            {
                FieldInfo fieldInfo = fieldInfos[i];
                // Read the method pointer from the VTable
                IntPtr pPointerToMethodInVTable = Marshal.ReadIntPtr(pVtable, (i + INDEX_VTABLE_FIRST_METHOD) * IntPtr.Size);
                // Instantiate a delegate
                object methodDelegate = Marshal.GetDelegateForFunctionPointer(pPointerToMethodInVTable, fieldInfo.FieldType);
                // Store the delegate in our C# VTable
                fieldInfo.SetValue(this.asioDriverVTable, methodDelegate);
            }
        }

        /// <summary>
        /// Internal VTable structure to store all the delegates to the C++ COM method.
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 2)]
        public class AsioDriverVTable
        {
            //3  virtual ASIOBool init(void *sysHandle) = 0;
            [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
            public delegate int ASIOInit(IntPtr _pUnknown, IntPtr sysHandle);
            public ASIOInit init = null;
            //4  virtual void getDriverName(char *name) = 0;
            [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
            public delegate void ASIOgetDriverName(IntPtr _pUnknown, StringBuilder name);
            public ASIOgetDriverName getDriverName = null;
            //5  virtual long getDriverVersion() = 0;
            [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
            public delegate int ASIOgetDriverVersion(IntPtr _pUnknown);
            public ASIOgetDriverVersion getDriverVersion = null;
            //6  virtual void getErrorMessage(char *string) = 0;	
            [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
            public delegate void ASIOgetErrorMessage(IntPtr _pUnknown, StringBuilder errorMessage);
            public ASIOgetErrorMessage getErrorMessage = null;
            //7  virtual ASIOError start() = 0;
            [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
            public delegate AsioError ASIOstart(IntPtr _pUnknown);
            public ASIOstart start = null;
            //8  virtual ASIOError stop() = 0;
            [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
            public delegate AsioError ASIOstop(IntPtr _pUnknown);
            public ASIOstop stop = null;
            //9  virtual ASIOError getChannels(long *numInputChannels, long *numOutputChannels) = 0;
            [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
            public delegate AsioError ASIOgetChannels(IntPtr _pUnknown, out int numInputChannels, out int numOutputChannels);
            public ASIOgetChannels getChannels = null;
            //10  virtual ASIOError getLatencies(long *inputLatency, long *outputLatency) = 0;
            [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
            public delegate AsioError ASIOgetLatencies(IntPtr _pUnknown, out int inputLatency, out int outputLatency);
            public ASIOgetLatencies getLatencies = null;
            //11 virtual ASIOError getBufferSize(long *minSize, long *maxSize, long *preferredSize, long *granularity) = 0;
            [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
            public delegate AsioError ASIOgetBufferSize(IntPtr _pUnknown, out int minSize, out int maxSize, out int preferredSize, out int granularity);
            public ASIOgetBufferSize getBufferSize = null;
            //12 virtual ASIOError canSampleRate(ASIOSampleRate sampleRate) = 0;
            [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
            public delegate AsioError ASIOcanSampleRate(IntPtr _pUnknown, double sampleRate);
            public ASIOcanSampleRate canSampleRate = null;
            //13 virtual ASIOError getSampleRate(ASIOSampleRate *sampleRate) = 0;
            [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
            public delegate AsioError ASIOgetSampleRate(IntPtr _pUnknown, out double sampleRate);
            public ASIOgetSampleRate getSampleRate = null;
            //14 virtual ASIOError setSampleRate(ASIOSampleRate sampleRate) = 0;
            [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
            public delegate AsioError ASIOsetSampleRate(IntPtr _pUnknown, double sampleRate);
            public ASIOsetSampleRate setSampleRate = null;
            //15 virtual ASIOError getClockSources(ASIOClockSource *clocks, long *numSources) = 0;
            [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
            public delegate AsioError ASIOgetClockSources(IntPtr _pUnknown, out long clocks, int numSources);
            public ASIOgetClockSources getClockSources = null;
            //16 virtual ASIOError setClockSource(long reference) = 0;
            [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
            public delegate AsioError ASIOsetClockSource(IntPtr _pUnknown, int reference);
            public ASIOsetClockSource setClockSource = null;
            //17 virtual ASIOError getSamplePosition(ASIOSamples *sPos, ASIOTimeStamp *tStamp) = 0;
            [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
            public delegate AsioError ASIOgetSamplePosition(IntPtr _pUnknown, out long samplePos, ref Asio64Bit timeStamp);
            public ASIOgetSamplePosition getSamplePosition = null;
            //18 virtual ASIOError getChannelInfo(ASIOChannelInfo *info) = 0;
            [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
            public delegate AsioError ASIOgetChannelInfo(IntPtr _pUnknown, ref AsioChannelInfo info);
            public ASIOgetChannelInfo getChannelInfo = null;
            //19 virtual ASIOError createBuffers(ASIOBufferInfo *bufferInfos, long numChannels, long bufferSize, ASIOCallbacks *callbacks) = 0;
            [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
            //            public delegate ASIOError ASIOcreateBuffers(IntPtr _pUnknown, ref ASIOBufferInfo[] bufferInfos, int numChannels, int bufferSize, ref ASIOCallbacks callbacks);
            public delegate AsioError ASIOcreateBuffers(IntPtr _pUnknown, IntPtr bufferInfos, int numChannels, int bufferSize, IntPtr callbacks);
            public ASIOcreateBuffers createBuffers = null;
            //20 virtual ASIOError disposeBuffers() = 0;
            [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
            public delegate AsioError ASIOdisposeBuffers(IntPtr _pUnknown);
            public ASIOdisposeBuffers disposeBuffers = null;
            //21 virtual ASIOError controlPanel() = 0;
            [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
            public delegate AsioError ASIOcontrolPanel(IntPtr _pUnknown);
            public ASIOcontrolPanel controlPanel = null;
            //22 virtual ASIOError future(long selector,void *opt) = 0;
            [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
            public delegate AsioError ASIOfuture(IntPtr _pUnknown, int selector, IntPtr opt);
            public ASIOfuture future = null;
            //23 virtual ASIOError outputReady() = 0;
            [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
            public delegate AsioError ASIOoutputReady(IntPtr _pUnknown);
            public ASIOoutputReady outputReady = null;
        }

        [DllImport("ole32.Dll")]
#pragma warning disable SYSLIB1054 // Use 'LibraryImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time
        protected static extern int CoCreateInstance(ref Guid clsid,
#pragma warning restore SYSLIB1054 // Use 'LibraryImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time
           IntPtr inner,
           uint context,
           ref Guid uuid,
           out IntPtr rReturnedComObject);

        #endregion

    }
}