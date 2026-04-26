using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.IO;

// ReSharper disable once CheckNamespace
namespace NAudio.Wave
{
    /// <summary>
    /// GSM 610
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    public sealed class Gsm610WaveFormat : WaveFormat
    {
        // samplesPerBlock is a constant for GSM 6.10 (320 samples per block).
        // Use a const to avoid storing the same value per-instance and
        // reduce memory footprint.
        private const short samplesPerBlock = 320;

        /// <summary>
        /// Creates a GSM 610 WaveFormat
        /// For now hardcoded to 13kbps
        /// </summary>
        public Gsm610WaveFormat()
        {
            waveFormatTag = WaveFormatEncoding.Gsm610;
            channels = 1;
            averageBytesPerSecond = 1625;
            bitsPerSample = 0; // must be zero
            blockAlign = 65;
            sampleRate = 8000;

            extraSize = 2;
            // samplesPerBlock is a compile-time constant; nothing to assign.
        }

        /// <summary>
        /// Samples per block
        /// </summary>
        public short SamplesPerBlock
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return samplesPerBlock; }
        }

        /// <summary>
        /// Writes this structure to a BinaryWriter
        /// </summary>
        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            writer.Write(samplesPerBlock);
        }
    }
}
