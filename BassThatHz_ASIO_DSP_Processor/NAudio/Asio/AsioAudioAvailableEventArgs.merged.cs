namespace NAudio.Wave
{
    using System;
    using System.Buffers.Binary;
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;
    using NAudio.Wave.Asio;

    // Merged copy of AsioAudioAvailableEventArgs to coexist in ASIO_Unified compilation unit.
    public class AsioAudioAvailableEventArgs : EventArgs
    {
        protected const float Int32LSB_MaxValue = (float)Int32.MaxValue;
        protected const float Int32MaxValueReciprocal = 1f / (float)Int32.MaxValue;
        protected const float Int16MaxValue = (float)Int16.MaxValue;
        protected const float Int16MaxValueReciprocal = 1f / (float)Int16.MaxValue;
        protected const float Int24LSBMaxValue = 8388608.0f;
        protected const float Int24LSBMaxValueReciprocal = 1f / 8388608.0f;
        protected const int Int24MaxValue = (1 << 23) - 1; // Max value for a 24-bit signed integer

        public AsioAudioAvailableEventArgs(IntPtr[] inputBuffers, IntPtr[] outputBuffers, int samplesPerBuffer, AsioSampleType asioSampleType)
        {
            InputBuffers = inputBuffers;
            OutputBuffers = outputBuffers;
            SamplesPerBuffer = samplesPerBuffer;
            AsioSampleType = asioSampleType;
        }

        public void Init(IntPtr[] inputBuffers, IntPtr[] outputBuffers, int samplesPerBuffer, AsioSampleType asioSampleType)
        {
            InputBuffers = inputBuffers;
            OutputBuffers = outputBuffers;
            SamplesPerBuffer = samplesPerBuffer;
            AsioSampleType = asioSampleType;
        }

        public IntPtr[] InputBuffers { get; protected set; }
        public IntPtr[] OutputBuffers { get; protected set; }
        public int SamplesPerBuffer { get; protected set; }

        public int GetAsInterleavedSamples(float[] inputSamples)
        {
            int InputChannels = InputBuffers.Length;
            if (inputSamples.Length < SamplesPerBuffer * InputChannels) throw new ArgumentException("input buffer not big enough");
            int index = 0;
            unsafe
            {
                if (AsioSampleType == AsioSampleType.Int32LSB)
                {
                    for (int n = 0; n < SamplesPerBuffer; n++)
                    {
                        for (int ch = 0; ch < InputChannels; ch++)
                        {
                            inputSamples[index++] = *((int*)InputBuffers[ch] + n) * Int32MaxValueReciprocal;
                        }
                    }
                }
                else if (AsioSampleType == AsioSampleType.Int16LSB)
                {
                    for (int n = 0; n < SamplesPerBuffer; n++)
                    {
                        for (int ch = 0; ch < InputChannels; ch++)
                        {
                            inputSamples[index++] = *((short*)InputBuffers[ch] + n) * Int16MaxValueReciprocal;
                        }
                    }
                }
                else if (AsioSampleType == AsioSampleType.Int24LSB)
                {
                    for (int n = 0; n < SamplesPerBuffer; n++)
                    {
                        for (int ch = 0; ch < InputChannels; ch++)
                        {
                            byte* InputpSample = (byte*)InputBuffers[ch] + n * 3;
                            int InputSample = InputpSample[0] | (InputpSample[1] << 8) | ((sbyte)InputpSample[2] << 16);
                            inputSamples[index++] = InputSample * Int24LSBMaxValueReciprocal;
                        }
                    }
                }
                else if (AsioSampleType == AsioSampleType.Float32LSB)
                {
                    for (int n = 0; n < SamplesPerBuffer; n++)
                    {
                        for (int ch = 0; ch < InputChannels; ch++)
                        {
                            inputSamples[index++] = *((float*)InputBuffers[ch] + n);
                        }
                    }
                }
                else
                {
                    throw new NotImplementedException(String.Format("ASIO Sample Type {0} not supported", AsioSampleType));
                }
            }
            return SamplesPerBuffer * InputChannels;
        }

        public void SetAsInterleavedSamples(float[] outputSamples)
        {
            int OutputChannels = OutputBuffers.Length;
            if (outputSamples.Length < SamplesPerBuffer * OutputChannels) throw new ArgumentException("output buffer not big enough");
            int index = 0;
            unsafe
            {
                if (AsioSampleType == AsioSampleType.Int32LSB)
                {
                    for (int n = 0; n < SamplesPerBuffer; n++)
                    {
                        for (int ch = 0; ch < OutputChannels; ch++)
                        {
                            *((int*)OutputBuffers[ch] + n) = (int)(outputSamples[index++] * Int32LSB_MaxValue);
                        }
                    }
                }
                else if (AsioSampleType == AsioSampleType.Int16LSB)
                {
                    for (int n = 0; n < SamplesPerBuffer; n++)
                    {
                        for (int ch = 0; ch < OutputChannels; ch++)
                        {
                            *((short*)OutputBuffers[ch] + n) = (short)(outputSamples[index++] * Int16MaxValue);
                        }
                    }
                }
                else if (AsioSampleType == AsioSampleType.Int24LSB)
                {
                    for (int n = 0; n < SamplesPerBuffer; n++)
                    {
                        for (int ch = 0; ch < OutputChannels; ch++)
                        {
                            var SampleValue = outputSamples[index++];
                            int sampleInt = (int)(SampleValue * Int24MaxValue);
                            sampleInt = sampleInt < -Int24MaxValue ? -Int24MaxValue : sampleInt > Int24MaxValue ? Int24MaxValue : sampleInt;
                            byte* OutputpSample = (byte*)OutputBuffers[ch] + n * 3;
                            OutputpSample[0] = (byte)(sampleInt & 0xFF);
                            OutputpSample[1] = (byte)((sampleInt >> 8) & 0xFF);
                            OutputpSample[2] = (byte)((sampleInt >> 16) & 0xFF);
                        }
                    }
                }
                else if (AsioSampleType == AsioSampleType.Float32LSB)
                {
                    for (int n = 0; n < SamplesPerBuffer; n++)
                    {
                        for (int ch = 0; ch < OutputChannels; ch++)
                        {
                            *((float*)OutputBuffers[ch] + n) = outputSamples[index++];
                        }
                    }
                }
                else
                {
                    throw new NotImplementedException(String.Format("ASIO Sample Type {0} not supported", AsioSampleType));
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public void GetAsJaggedSamples(double[][] inputSamples)
        {
            int InputChannels = InputBuffers.Length;

            if (inputSamples == null)
                throw new ArgumentNullException(nameof(inputSamples));
            if (inputSamples.Length < InputChannels)
                throw new InvalidOperationException("inputSamples cannot be smaller than [channelcount][]");
            if (inputSamples[0] == null)
                throw new InvalidOperationException("inputSamples[x] cannot be null. Channels must be initalized");
            if (inputSamples[0].Length < SamplesPerBuffer)
                throw new InvalidOperationException("inputSamples[x] cannot be smaller than SamplesPerBuffer. Channels must be initalized");

            var LocalSamplesPerBuffer = SamplesPerBuffer;
            if (AsioSampleType == AsioSampleType.Int32LSB)
            {
                _ = Parallel.For(0, InputChannels, (ch) =>
                {
                    unsafe
                    {
                        for (int n = 0; n < LocalSamplesPerBuffer; n++)
                            inputSamples[ch][n] = *((int*)InputBuffers[ch] + n) * Int32MaxValueReciprocal;
                    }
                });
            }
            else if (AsioSampleType == AsioSampleType.Float64LSB)
            {
                _ = Parallel.For(0, InputChannels, (ch) =>
                {
                    unsafe
                    {
                        var SamplePointer = (double*)InputBuffers[ch];
                        for (int n = 0; n < LocalSamplesPerBuffer; n++)
                            inputSamples[ch][n] = *(SamplePointer + n);
                    }
                });
            }
            else if (AsioSampleType == AsioSampleType.Float64MSB)
            {
                _ = Parallel.For(0, InputChannels, (ch) =>
                {
                    unsafe
                    {
                        var SamplePointer = (double*)InputBuffers[ch];
                        var OutBuffer = inputSamples[ch];
                        for (int n = 0; n < LocalSamplesPerBuffer; n++)
                        {
                            ulong bits = *((ulong*)SamplePointer + n);
                            bits = BinaryPrimitives.ReverseEndianness(bits);
                            OutBuffer[n] = BitConverter.Int64BitsToDouble((long)bits);
                        }
                    }
                });
            }
            else if (AsioSampleType == AsioSampleType.Int16LSB)
            {
                _ = Parallel.For(0, InputChannels, (ch) =>
                {
                    unsafe
                    {
                        var SamplePointer = (short*)InputBuffers[ch];
                        for (int n = 0; n < LocalSamplesPerBuffer; n++)
                            inputSamples[ch][n] = *(SamplePointer + n) * Int16MaxValueReciprocal;
                    }
                });
            }
            else if (AsioSampleType == AsioSampleType.Int24LSB)
            {
                _ = Parallel.For(0, InputChannels, (ch) =>
                {
                    unsafe
                    {
                        var SamplePointer = (byte*)InputBuffers[ch];
                        for (int n = 0; n < LocalSamplesPerBuffer; n++)
                        {
                            byte* InputpSample = SamplePointer + n * 3;
                            int InputSample = InputpSample[0] | (InputpSample[1] << 8) | ((sbyte)InputpSample[2] << 16);
                            inputSamples[ch][n] = InputSample * Int24LSBMaxValueReciprocal;
                        }
                    }
                });
            }
            else if (AsioSampleType == AsioSampleType.Float32LSB)
            {
                _ = Parallel.For(0, InputChannels, (ch) =>
                {
                    unsafe
                    {
                        var SamplePointer = (float*)InputBuffers[ch];
                        for (int n = 0; n < SamplesPerBuffer; n++)
                            inputSamples[ch][n] = *(SamplePointer + n);
                    }
                });
            }
            else
            {
                throw new NotImplementedException(String.Format("ASIO Sample Type {0} not supported", AsioSampleType));
            }

        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public void SetAsJaggedSamples(double[][] outputSamples)
        {
            int OutputChannels = OutputBuffers.Length;
            if (outputSamples == null)
                throw new ArgumentNullException(nameof(outputSamples));
            if (outputSamples.Length < OutputChannels)
                throw new InvalidOperationException("outputSamples cannot be smaller than [channelcount][]");
            if (outputSamples[0] == null)
                throw new InvalidOperationException("outputSamples[x] cannot be null. Channels must be initalized");
            if (outputSamples[0].Length < SamplesPerBuffer)
                throw new InvalidOperationException("outputSamples[x] cannot be smaller than SamplesPerBuffer. Channels must be initalized");

            var LocalSamplesPerBuffer = SamplesPerBuffer;
            if (AsioSampleType == AsioSampleType.Int32LSB)
            {
                _ = Parallel.For(0, OutputChannels, (ch) =>
                {
                    unsafe
                    {
                        for (int n = 0; n < LocalSamplesPerBuffer; n++)
                        {
                            *((int*)OutputBuffers[ch] + n) = (int)(outputSamples[ch][n] * Int32LSB_MaxValue);
                        }
                    }
                });
            }
            else if (AsioSampleType == AsioSampleType.Float64LSB)
            {
                _ = Parallel.For(0, OutputChannels, (ch) =>
                {
                    unsafe
                    {
                        var SamplePointer = (double*)OutputBuffers[ch];
                        var OutSamples = outputSamples[ch];
                        for (int n = 0; n < LocalSamplesPerBuffer; n++)
                        {
                            *(SamplePointer + n) = (double)OutSamples[n];
                        }
                    }
                });
            }
            else if (AsioSampleType == AsioSampleType.Float64MSB)
            {
                _ = Parallel.For(0, OutputChannels, (ch) =>
                {
                    unsafe
                    {
                        var SamplePointer = (double*)OutputBuffers[ch];
                        var OutSamples = outputSamples[ch];
                        for (int n = 0; n < LocalSamplesPerBuffer; n++)
                        {
                            long bits = BitConverter.DoubleToInt64Bits(OutSamples[n]);
                            ulong ubits = (ulong)bits;
                            ubits = BinaryPrimitives.ReverseEndianness(ubits);
                            *(SamplePointer + n) = BitConverter.Int64BitsToDouble((long)ubits);
                        }
                    }
                });
            }
            else if (AsioSampleType == AsioSampleType.Int16LSB)
            {
                float MaxValue = (float)Int16.MaxValue;
                _ = Parallel.For(0, OutputChannels, (ch) =>
                {
                    unsafe
                    {
                        var SamplePointer = (short*)OutputBuffers[ch];
                        var OutSamples = outputSamples[ch];
                        for (int n = 0; n < LocalSamplesPerBuffer; n++)
                        {
                            *(SamplePointer + n) = (short)(OutSamples[n] * MaxValue);
                        }
                    }
                });
            }
            else if (AsioSampleType == AsioSampleType.Int24LSB)
            {
                _ = Parallel.For(0, OutputChannels, (ch) =>
                {
                    unsafe
                    {
                        var SamplePointer = (byte*)OutputBuffers[ch];
                        var OutSamples = outputSamples[ch];
                        for (int n = 0; n < LocalSamplesPerBuffer; n++)
                        {
                            int sampleValue = (int)(OutSamples[n] * Int24MaxValue);
                            sampleValue = sampleValue < -Int24MaxValue ? -Int24MaxValue : sampleValue > Int24MaxValue ? Int24MaxValue : sampleValue;
                            SamplePointer[n * 3] = (byte)(sampleValue & 0xFF);
                            SamplePointer[n * 3 + 1] = (byte)((sampleValue >> 8) & 0xFF);
                            SamplePointer[n * 3 + 2] = (byte)((sampleValue >> 16) & 0xFF);
                        }
                    }
                });
            }
            else if (AsioSampleType == AsioSampleType.Float32LSB)
            {
                _ = Parallel.For(0, OutputChannels, (ch) =>
                {
                    unsafe
                    {
                        var SamplePointer = (float*)OutputBuffers[ch];
                        var OutSamples = outputSamples[ch];
                        for (int n = 0; n < LocalSamplesPerBuffer; n++)
                        {
                            *(SamplePointer + n) = (float)OutSamples[n];
                        }
                    }
                });
            }
            else
            {
                throw new NotImplementedException(String.Format("ASIO Sample Type {0} not supported", AsioSampleType));
            }

        }

        public AsioSampleType AsioSampleType { get; protected set; }
    }

}
