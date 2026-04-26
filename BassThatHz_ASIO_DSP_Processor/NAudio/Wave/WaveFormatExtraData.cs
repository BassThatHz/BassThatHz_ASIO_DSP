using System;
using System.Runtime.InteropServices;
using System.IO;
using System.Buffers;

// ReSharper disable once CheckNamespace
namespace NAudio.Wave
{
    /// <summary>
    /// This class used for marshalling from unmanaged code
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 2)]
    public class WaveFormatExtraData : WaveFormat
    {
        // try with 100 bytes for now, increase if necessary
        // Keep the MarshalAs attribute for interop layout, but avoid allocating
        // the backing array until it's needed to reduce memory pressure for
        // instances that don't actually carry extra data.
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 100)]
        protected byte[] extraData;

        /// <summary>
        /// Allows the extra data to be read. Returns an empty array when there
        /// is no extra data to avoid null checks in callers.
        /// </summary>
        public byte[] ExtraData => extraData ?? Array.Empty<byte>();

        /// <summary>
        /// parameterless constructor for marshalling
        /// </summary>
        public WaveFormatExtraData()
        {
        }

        /// <summary>
        /// Reads this structure from a BinaryReader
        /// </summary>
        public WaveFormatExtraData(BinaryReader reader)
            : base(reader)
        {
            ReadExtraData(reader);
        }

        public void ReadExtraData(BinaryReader reader)
        {
            if (this.extraSize > 0)
            {
                // Allocate exactly the amount of extra data required instead
                // of holding the full marshal-size buffer. This minimizes GC
                // pressure and memory usage when many instances are created.
                if (extraData == null || extraData.Length < extraSize)
                {
                    extraData = new byte[extraSize];
                }

                int read = 0;
                while (read < extraSize)
                {
                    int n = reader.Read(extraData, read, extraSize - read);
                    if (n == 0)
                    {
                        throw new EndOfStreamException("Unexpected end of stream while reading WaveFormat extra data");
                    }
                    read += n;
                }
            }
        }

        /// <summary>
        /// Writes this structure to a BinaryWriter
        /// </summary>
        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            if (extraSize > 0)
            {
                // If extraData hasn't been allocated, write zero bytes to match
                // the expected size. If it exists, only write the requested
                // number of bytes (which may be smaller than the marshal size).
                if (extraData == null)
                {
                    // Write zeros without allocating a large temporary array
                    const int bufferSize = 1024;
                    byte[] zeros = ArrayPool<byte>.Shared.Rent(bufferSize);
                    try
                    {
                        Array.Clear(zeros, 0, bufferSize);
                        int remaining = extraSize;
                        while (remaining > 0)
                        {
                            int toWrite = Math.Min(remaining, bufferSize);
                            writer.Write(zeros, 0, toWrite);
                            remaining -= toWrite;
                        }
                    }
                    finally
                    {
                        ArrayPool<byte>.Shared.Return(zeros);
                    }
                }
                else
                {
                    writer.Write(extraData, 0, Math.Min(extraData.Length, extraSize));
                }
            }
        }
    }
}
