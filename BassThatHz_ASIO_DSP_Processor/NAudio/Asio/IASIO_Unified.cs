namespace NAudio.Wave;

#region Usings
using NAudio.Wave.Asio;
using System;
#endregion

public interface IASIO_Unified : IDisposable
{
    // Callbacks (exposed as properties)
    Action Driver_ResetRequestCallback { get; set; }
    Action Driver_BufferSizeChangedCallback { get; set; }
    Action Driver_ResyncRequestCallback { get; set; }
    Action Driver_LatenciesChangedCallback { get; set; }
    Action Driver_OverloadCallback { get; set; }
    Action Driver_SampleRateChangedCallback { get; set; }

    // Events
    event EventHandler<AsioAudioAvailableEventArgs> AudioAvailable;
    event EventHandler DriverResetRequest;

    // Read-only Properties
    string DriverName { get; }
    bool IsInitalized { get; }
    PlaybackState PlaybackState { get; }
    int NumberOfOutputChannels { get; }
    int NumberOfInputChannels { get; }
    int SamplesPerBuffer { get; }

    // Read-Write Properties
    bool AutoStop { get; set; }
    int OutputChannelOffset { get; set; }
    int InputChannelOffset { get; set; }

    // Additional Properties
    AsioDriverCapability GetDriverCapabilities { get; }
    int DriverInputChannelCount { get; }
    int DriverOutputChannelCount { get; }
    Tuple<int, int> PlaybackLatency { get; }

    // Public Methods
    string AsioInputChannelName(int channel);
    string AsioOutputChannelName(int channel);

    void ShowControlPanel();
    bool IsSampleRateSupported(int sampleRate);

    /// <summary>
    /// Initializes the ASIO driver with the specified number of channels, sample rate, and channel offsets.
    /// </summary>
    void Init(int numberOfInputChannels, int numberOfOutputChannels, int desiredSampleRate, int outputChannelOffset, int inputChannelOffset);

    void Start();
    AsioError Stop();

    int AsioDriver_GetDriverVersion();
    double GetSampleRate();
    void GetClockSources(out long clocks, int numSources);
    void GetSamplePosition(out long samplePos, ref Asio64Bit timeStamp);
}