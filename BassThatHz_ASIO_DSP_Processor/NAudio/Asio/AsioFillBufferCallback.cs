#nullable disable

namespace NAudio.Wave.Asio
{
    using System;

    /// <summary>
    /// Callback used by legacy AsioDriverExt style API to get wave data
    /// </summary>
    public delegate void AsioFillBufferCallback(IntPtr[] inputChannels, IntPtr[] outputChannels);
}
