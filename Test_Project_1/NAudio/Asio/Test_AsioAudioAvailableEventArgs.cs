namespace Test_Project_1;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NAudio.Wave;
using NAudio.Wave.Asio;
using System;
using System.Runtime.InteropServices;

[TestClass]
public class Test_AsioAudioAvailableEventArgs
{
    private static IntPtr[] AllocBuffers(int channels, int samplesPerBuffer, int bytesPerSample)
    {
        var buffers = new IntPtr[channels];
        for (int ch = 0; ch < channels; ch++)
            buffers[ch] = Marshal.AllocHGlobal(samplesPerBuffer * bytesPerSample);
        return buffers;
    }

    private static void FreeBuffers(IntPtr[] buffers)
    {
        foreach (var b in buffers)
            if (b != IntPtr.Zero)
                Marshal.FreeHGlobal(b);
    }

    #region Constructor / Init

    [TestMethod]
    public void Constructor_SetsAllProperties()
    {
        var input = new IntPtr[] { new IntPtr(1) };
        var output = new IntPtr[] { new IntPtr(2) };
        var args = new AsioAudioAvailableEventArgs(input, output, 128, AsioSampleType.Int32LSB);

        Assert.AreSame(input, args.InputBuffers);
        Assert.AreSame(output, args.OutputBuffers);
        Assert.AreEqual(128, args.SamplesPerBuffer);
        Assert.AreEqual(AsioSampleType.Int32LSB, args.AsioSampleType);
    }

    [TestMethod]
    public void Init_ReplacesAllPropertiesOnExistingInstance()
    {
        var args = new AsioAudioAvailableEventArgs(new IntPtr[0], new IntPtr[0], 64, AsioSampleType.Int16LSB);

        var newInput = new IntPtr[] { new IntPtr(3) };
        var newOutput = new IntPtr[] { new IntPtr(4) };
        args.Init(newInput, newOutput, 256, AsioSampleType.Float32LSB);

        Assert.AreSame(newInput, args.InputBuffers);
        Assert.AreSame(newOutput, args.OutputBuffers);
        Assert.AreEqual(256, args.SamplesPerBuffer);
        Assert.AreEqual(AsioSampleType.Float32LSB, args.AsioSampleType);
    }

    #endregion

    #region GetAsJaggedSamples / SetAsJaggedSamples round-trip

    [TestMethod]
    public void JaggedSamples_Int32LSB_RoundTripsWithinPrecision()
    {
        const int channels = 2, samples = 8;
        var buffers = AllocBuffers(channels, samples, sizeof(int));
        try
        {
            var args = new AsioAudioAvailableEventArgs(buffers, buffers, samples, AsioSampleType.Int32LSB);
            var source = new double[channels][];
            for (int ch = 0; ch < channels; ch++)
            {
                source[ch] = new double[samples];
                for (int n = 0; n < samples; n++)
                    source[ch][n] = (ch == 0 ? 1 : -1) * (n / (double)samples);
            }

            args.SetAsJaggedSamples(source);

            var result = new double[channels][];
            for (int ch = 0; ch < channels; ch++) result[ch] = new double[samples];
            args.GetAsJaggedSamples(result);

            for (int ch = 0; ch < channels; ch++)
                for (int n = 0; n < samples; n++)
                    Assert.AreEqual(source[ch][n], result[ch][n], 1e-8, $"ch={ch} n={n}");
        }
        finally
        {
            FreeBuffers(buffers);
        }
    }

    [TestMethod]
    public void SetAsJaggedSamples_Int32LSB_FullScalePositive_DoesNotOverflowToNegative()
    {
        // Regression test: (float)Int32.MaxValue rounds up to 2147483648f (2^31), so a naive
        // (int)(1.0 * 2147483648f) cast overflows and silently wraps to a large negative value
        // instead of clamping to int.MaxValue. Full-scale (+1.0) input must produce a
        // full-scale-positive sample, never a full-scale-negative one.
        const int channels = 1, samples = 4;
        var buffers = AllocBuffers(channels, samples, sizeof(int));
        try
        {
            var args = new AsioAudioAvailableEventArgs(buffers, buffers, samples, AsioSampleType.Int32LSB);
            var source = new double[][] { new double[] { 1.0, 1.0, 1.0, 1.0 } };
            args.SetAsJaggedSamples(source);

            unsafe
            {
                var ptr = (int*)buffers[0];
                for (int n = 0; n < samples; n++)
                    Assert.IsTrue(ptr[n] > 0, $"Sample {n} should be positive (full-scale), was {ptr[n]}");
            }
        }
        finally
        {
            FreeBuffers(buffers);
        }
    }

    [TestMethod]
    public void JaggedSamples_Int16LSB_RoundTripsWithinPrecision()
    {
        const int channels = 2, samples = 8;
        var buffers = AllocBuffers(channels, samples, sizeof(short));
        try
        {
            var args = new AsioAudioAvailableEventArgs(buffers, buffers, samples, AsioSampleType.Int16LSB);
            var source = new double[channels][];
            for (int ch = 0; ch < channels; ch++)
            {
                source[ch] = new double[samples];
                for (int n = 0; n < samples; n++)
                    source[ch][n] = (ch == 0 ? 0.5 : -0.5) * (n / (double)samples);
            }

            args.SetAsJaggedSamples(source);

            var result = new double[channels][];
            for (int ch = 0; ch < channels; ch++) result[ch] = new double[samples];
            args.GetAsJaggedSamples(result);

            for (int ch = 0; ch < channels; ch++)
                for (int n = 0; n < samples; n++)
                    Assert.AreEqual(source[ch][n], result[ch][n], 1e-4, $"ch={ch} n={n}");
        }
        finally
        {
            FreeBuffers(buffers);
        }
    }

    [TestMethod]
    public void JaggedSamples_Int24LSB_RoundTripsWithinPrecision()
    {
        const int channels = 1, samples = 4;
        var buffers = AllocBuffers(channels, samples, 3);
        try
        {
            var args = new AsioAudioAvailableEventArgs(buffers, buffers, samples, AsioSampleType.Int24LSB);
            var source = new double[][] { new double[] { 0.75, -0.75, 0.1, -0.1 } };
            args.SetAsJaggedSamples(source);

            var result = new double[][] { new double[samples] };
            args.GetAsJaggedSamples(result);

            for (int n = 0; n < samples; n++)
                Assert.AreEqual(source[0][n], result[0][n], 1e-4, $"n={n}");
        }
        finally
        {
            FreeBuffers(buffers);
        }
    }

    [TestMethod]
    public void JaggedSamples_Float32LSB_RoundTripsExactly()
    {
        const int channels = 1, samples = 4;
        var buffers = AllocBuffers(channels, samples, sizeof(float));
        try
        {
            var args = new AsioAudioAvailableEventArgs(buffers, buffers, samples, AsioSampleType.Float32LSB);
            var source = new double[][] { new double[] { 0.123, -0.456, 0.789, -1.0 } };
            args.SetAsJaggedSamples(source);

            var result = new double[][] { new double[samples] };
            args.GetAsJaggedSamples(result);

            for (int n = 0; n < samples; n++)
                Assert.AreEqual(source[0][n], result[0][n], 1e-6, $"n={n}");
        }
        finally
        {
            FreeBuffers(buffers);
        }
    }

    [TestMethod]
    public void JaggedSamples_Float64LSB_RoundTripsExactly()
    {
        const int channels = 1, samples = 4;
        var buffers = AllocBuffers(channels, samples, sizeof(double));
        try
        {
            var args = new AsioAudioAvailableEventArgs(buffers, buffers, samples, AsioSampleType.Float64LSB);
            var source = new double[][] { new double[] { 0.1, -0.2, 0.3, -0.4 } };
            args.SetAsJaggedSamples(source);

            var result = new double[][] { new double[samples] };
            args.GetAsJaggedSamples(result);

            for (int n = 0; n < samples; n++)
                Assert.AreEqual(source[0][n], result[0][n], 1e-12, $"n={n}");
        }
        finally
        {
            FreeBuffers(buffers);
        }
    }

    [TestMethod]
    public void GetAsJaggedSamples_Float64MSB_ReversesEndiannessCorrectly()
    {
        const int channels = 1, samples = 1;
        var buffers = AllocBuffers(channels, samples, sizeof(double));
        try
        {
            double expected = 0.42;
            unsafe
            {
                long bits = BitConverter.DoubleToInt64Bits(expected);
                ulong reversed = System.Buffers.Binary.BinaryPrimitives.ReverseEndianness((ulong)bits);
                *(ulong*)buffers[0] = reversed;
            }

            var args = new AsioAudioAvailableEventArgs(buffers, buffers, samples, AsioSampleType.Float64MSB);
            var result = new double[][] { new double[samples] };
            args.GetAsJaggedSamples(result);

            Assert.AreEqual(expected, result[0][0], 1e-12);
        }
        finally
        {
            FreeBuffers(buffers);
        }
    }

    [TestMethod]
    public void GetAsJaggedSamples_NullArray_ThrowsArgumentNullException()
    {
        var args = new AsioAudioAvailableEventArgs(new IntPtr[1], new IntPtr[1], 4, AsioSampleType.Int32LSB);
        Assert.ThrowsExactly<ArgumentNullException>(() => args.GetAsJaggedSamples(null));
    }

    [TestMethod]
    public void GetAsJaggedSamples_TooFewChannels_ThrowsInvalidOperationException()
    {
        var args = new AsioAudioAvailableEventArgs(new IntPtr[2], new IntPtr[2], 4, AsioSampleType.Int32LSB);
        Assert.ThrowsExactly<InvalidOperationException>(() => args.GetAsJaggedSamples(new double[1][]));
    }

    [TestMethod]
    public void GetAsJaggedSamples_NullChannelBuffer_ThrowsInvalidOperationException()
    {
        var args = new AsioAudioAvailableEventArgs(new IntPtr[1], new IntPtr[1], 4, AsioSampleType.Int32LSB);
        Assert.ThrowsExactly<InvalidOperationException>(() => args.GetAsJaggedSamples(new double[1][] { null }));
    }

    [TestMethod]
    public void GetAsJaggedSamples_ChannelBufferTooSmall_ThrowsInvalidOperationException()
    {
        var args = new AsioAudioAvailableEventArgs(new IntPtr[1], new IntPtr[1], 4, AsioSampleType.Int32LSB);
        Assert.ThrowsExactly<InvalidOperationException>(() => args.GetAsJaggedSamples(new double[1][] { new double[2] }));
    }

    [TestMethod]
    public void GetAsJaggedSamples_UnsupportedSampleType_ThrowsNotImplementedException()
    {
        var args = new AsioAudioAvailableEventArgs(new IntPtr[1], new IntPtr[1], 4, (AsioSampleType)9999);
        Assert.ThrowsExactly<NotImplementedException>(() => args.GetAsJaggedSamples(new double[1][] { new double[4] }));
    }

    [TestMethod]
    public void SetAsJaggedSamples_UnsupportedSampleType_ThrowsNotImplementedException()
    {
        var args = new AsioAudioAvailableEventArgs(new IntPtr[1], new IntPtr[1], 4, (AsioSampleType)9999);
        Assert.ThrowsExactly<NotImplementedException>(() => args.SetAsJaggedSamples(new double[1][] { new double[4] }));
    }

    #endregion

    #region GetAsInterleavedSamples / SetAsInterleavedSamples round-trip

    [TestMethod]
    public void InterleavedSamples_Int32LSB_RoundTripsWithinPrecision()
    {
        const int channels = 2, samples = 4;
        var buffers = AllocBuffers(channels, samples, sizeof(int));
        try
        {
            var args = new AsioAudioAvailableEventArgs(buffers, buffers, samples, AsioSampleType.Int32LSB);
            var source = new float[] { 0.5f, -0.5f, 0.25f, -0.25f, 0f, 1f, -1f, 0.1f };
            args.SetAsInterleavedSamples(source);

            var result = new float[channels * samples];
            int written = args.GetAsInterleavedSamples(result);

            Assert.AreEqual(channels * samples, written);
            for (int i = 0; i < source.Length; i++)
                Assert.AreEqual(source[i], result[i], 1e-4f, $"i={i}");
        }
        finally
        {
            FreeBuffers(buffers);
        }
    }

    [TestMethod]
    public void SetAsInterleavedSamples_Int32LSB_FullScalePositive_DoesNotOverflowToNegative()
    {
        const int channels = 1, samples = 2;
        var buffers = AllocBuffers(channels, samples, sizeof(int));
        try
        {
            var args = new AsioAudioAvailableEventArgs(buffers, buffers, samples, AsioSampleType.Int32LSB);
            args.SetAsInterleavedSamples(new float[] { 1.0f, 1.0f });

            unsafe
            {
                var ptr = (int*)buffers[0];
                Assert.IsTrue(ptr[0] > 0, $"Sample 0 should be positive (full-scale), was {ptr[0]}");
                Assert.IsTrue(ptr[1] > 0, $"Sample 1 should be positive (full-scale), was {ptr[1]}");
            }
        }
        finally
        {
            FreeBuffers(buffers);
        }
    }

    [TestMethod]
    public void InterleavedSamples_Float32LSB_RoundTripsExactly()
    {
        const int channels = 1, samples = 3;
        var buffers = AllocBuffers(channels, samples, sizeof(float));
        try
        {
            var args = new AsioAudioAvailableEventArgs(buffers, buffers, samples, AsioSampleType.Float32LSB);
            var source = new float[] { 0.1f, -0.2f, 0.3f };
            args.SetAsInterleavedSamples(source);

            var result = new float[samples];
            args.GetAsInterleavedSamples(result);

            for (int i = 0; i < samples; i++)
                Assert.AreEqual(source[i], result[i], 1e-6f);
        }
        finally
        {
            FreeBuffers(buffers);
        }
    }

    [TestMethod]
    public void GetAsInterleavedSamples_BufferTooSmall_ThrowsArgumentException()
    {
        var args = new AsioAudioAvailableEventArgs(new IntPtr[2], new IntPtr[2], 4, AsioSampleType.Float32LSB);
        Assert.ThrowsExactly<ArgumentException>(() => args.GetAsInterleavedSamples(new float[1]));
    }

    [TestMethod]
    public void SetAsInterleavedSamples_BufferTooSmall_ThrowsArgumentException()
    {
        var args = new AsioAudioAvailableEventArgs(new IntPtr[2], new IntPtr[2], 4, AsioSampleType.Float32LSB);
        Assert.ThrowsExactly<ArgumentException>(() => args.SetAsInterleavedSamples(new float[1]));
    }

    [TestMethod]
    public void GetAsInterleavedSamples_UnsupportedSampleType_ThrowsNotImplementedException()
    {
        var args = new AsioAudioAvailableEventArgs(new IntPtr[1], new IntPtr[1], 4, (AsioSampleType)9999);
        Assert.ThrowsExactly<NotImplementedException>(() => args.GetAsInterleavedSamples(new float[4]));
    }

    [TestMethod]
    public void SetAsInterleavedSamples_UnsupportedSampleType_ThrowsNotImplementedException()
    {
        var args = new AsioAudioAvailableEventArgs(new IntPtr[1], new IntPtr[1], 4, (AsioSampleType)9999);
        Assert.ThrowsExactly<NotImplementedException>(() => args.SetAsInterleavedSamples(new float[4]));
    }

    #endregion
}
