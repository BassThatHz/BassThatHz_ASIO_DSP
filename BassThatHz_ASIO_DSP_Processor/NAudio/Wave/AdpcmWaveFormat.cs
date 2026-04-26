using System;
using System.Runtime.InteropServices;

// ReSharper disable once CheckNamespace
namespace NAudio.Wave
{
    /// <summary>
    /// Microsoft ADPCM
    /// See http://icculus.org/SDL_sound/downloads/external_documentation/wavecomp.htm
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack=2)]
    public class AdpcmWaveFormat : WaveFormat
    {
        readonly short samplesPerBlock;
        readonly short numCoeff;
        // 7 pairs of coefficients
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 14)]
        readonly short[] coefficients;

        /// <summary>
        /// Empty constructor needed for marshalling from a pointer
        /// </summary>
        AdpcmWaveFormat() : this(8000,1)
        {
        }

        /// <summary>
        /// Samples per block
        /// </summary>
        public int SamplesPerBlock => samplesPerBlock;

        /// <summary>
        /// Number of coefficients
        /// </summary>
        public int NumCoefficients => numCoeff;

        /// <summary>
        /// Coefficients
        /// </summary>
        public short[] Coefficients => coefficients;

        /// <summary>
        /// Microsoft ADPCM  
        /// </summary>
        /// <param name="sampleRate">Sample Rate</param>
        /// <param name="channels">Channels</param>
        public AdpcmWaveFormat(int sampleRate, int channels) : base(sampleRate,0,channels)
        {
            waveFormatTag = WaveFormatEncoding.Adpcm;

            // TODO: validate sampleRate, bitsPerSample
            extraSize = 32;

            // determine block align based on requested sample rate
            switch (sampleRate)
            {
                case 8000:
                case 11025:
                    blockAlign = 256;
                    break;
                case 22050:
                    blockAlign = 512;
                    break;
                case 44100:
                default:
                    blockAlign = 1024;
                    break;
            }

            bitsPerSample = 4;

            samplesPerBlock = (short)((blockAlign - 7 * channels) * 8 / (bitsPerSample * channels) + 2);

            // compute average bytes per second using the original sampleRate parameter
            averageBytesPerSecond = sampleRate * blockAlign / samplesPerBlock;

            // number of coefficient pairs
            numCoeff = 7;

            // initialize coefficients array once per instance (readonly field)
            coefficients = new short[14]
            {
                256, 0, 512, -256, 0, 0, 192, 64, 240, 0, 460, -208, 392, -232
            };
        }

        /// <summary>
        /// Serializes this wave format
        /// </summary>
        /// <param name="writer">Binary writer</param>
        public override void Serialize(System.IO.BinaryWriter writer)
        {
            base.Serialize(writer);
            writer.Write(samplesPerBlock);
            writer.Write(numCoeff);
            // use indexed loop for arrays (slightly faster than foreach)
            for (int i = 0; i < coefficients.Length; i++)
            {
                writer.Write(coefficients[i]);
            }
        }

        /// <summary>
        /// String Description of this WaveFormat
        /// </summary>
        public override string ToString()
        {
            return $"Microsoft ADPCM {SampleRate} Hz {channels} channels {bitsPerSample} bits per sample {samplesPerBlock} samples per block";
        }
    }
}
