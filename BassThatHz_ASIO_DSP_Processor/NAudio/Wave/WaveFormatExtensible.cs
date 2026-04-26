using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using NAudio.Dmo;

namespace NAudio.Wave
{
    /// <summary>
    /// WaveFormatExtensible
    /// http://www.microsoft.com/whdc/device/audio/multichaud.mspx
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 2)]	
    public class WaveFormatExtensible : WaveFormat
    {
        // fields kept sequential for interop layout
        private readonly short wValidBitsPerSample; // bits of precision, or is wSamplesPerBlock if wBitsPerSample==0
        private readonly int dwChannelMask; // which channels are present in stream
        private readonly Guid subFormat;

        /// <summary>
        /// Parameterless constructor for marshalling
        /// </summary>
        WaveFormatExtensible()
        {
        }

        /// <summary>
        /// Creates a new WaveFormatExtensible for PCM or IEEE
        /// </summary>
        public WaveFormatExtensible(int rate, int bits, int channels)
            : base(rate, bits, channels)
        {
            waveFormatTag = WaveFormatEncoding.Extensible;
            extraSize = 22;
            wValidBitsPerSample = (short)bits;
            // set low 'channels' bits. Avoid loop for performance. If channels >= 32, set all bits.
            dwChannelMask = (channels >= 32) ? -1 : ((1 << channels) - 1);
            // KSDATAFORMAT_SUBTYPE_IEEE_FLOAT for 32-bit samples, otherwise PCM
            subFormat = (bits == 32) ? AudioMediaSubtypes.MEDIASUBTYPE_IEEE_FLOAT : AudioMediaSubtypes.MEDIASUBTYPE_PCM;

        }

        /// <summary>
        /// WaveFormatExtensible for PCM or floating point can be awkward to work with
        /// This creates a regular WaveFormat structure representing the same audio format
        /// Returns the WaveFormat unchanged for non PCM or IEEE float
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public WaveFormat ToStandardWaveFormat()
        {
            if (subFormat == AudioMediaSubtypes.MEDIASUBTYPE_IEEE_FLOAT && bitsPerSample == 32)
                return CreateIeeeFloatWaveFormat(sampleRate, channels);
            if (subFormat == AudioMediaSubtypes.MEDIASUBTYPE_PCM)
                return new WaveFormat(sampleRate, bitsPerSample, channels);
            return this;
            //throw new InvalidOperationException("Not a recognised PCM or IEEE float format");
        }

        /// <summary>
        /// SubFormat (may be one of AudioMediaSubtypes)
        /// </summary>
        public Guid SubFormat { get { return subFormat; } }

        /// <summary>
        /// Serialize
        /// </summary>
        /// <param name="writer"></param>
        public override void Serialize(System.IO.BinaryWriter writer)
        {
            base.Serialize(writer);
            writer.Write(wValidBitsPerSample);
            writer.Write(dwChannelMask);
            // avoid allocating a managed byte[] for the Guid by writing directly from a stack span
            Span<byte> guidBytes = stackalloc byte[16];
            subFormat.TryWriteBytes(guidBytes);
            writer.Write(guidBytes);
        }

        /// <summary>
        /// String representation
        /// </summary>
        public override string ToString()
        {
            return $"{base.ToString()} wBitsPerSample:{wValidBitsPerSample} dwChannelMask:{dwChannelMask} subFormat:{subFormat} extraSize:{extraSize}";
        }
    }
}
