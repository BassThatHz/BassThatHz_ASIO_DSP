namespace Test_Project_1;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NAudio.Wave;
using NAudio.Wave.Asio;
using System;
using System.Runtime.InteropServices;

[TestClass]
public class Test_ASIOSampleConvertor
{
    #region clamp helper tests

    [TestMethod]
    public void ClampToInt_InRange_ScalesLinearly()
    {
        Assert.AreEqual(0, AsioSampleConvertor.clampToInt(0.0));
        Assert.AreEqual(2147483647, AsioSampleConvertor.clampToInt(1.0));
        Assert.AreEqual((int)(-0.5 * 2147483647.0), AsioSampleConvertor.clampToInt(-0.5));
    }

    [TestMethod]
    public void ClampToInt_AboveOne_ClampsToMax()
    {
        Assert.AreEqual(2147483647, AsioSampleConvertor.clampToInt(2.5));
    }

    [TestMethod]
    public void ClampToInt_BelowNegativeOne_ClampsToMin()
    {
        Assert.AreEqual((int)(-1.0 * 2147483647.0), AsioSampleConvertor.clampToInt(-5.0));
    }

    [TestMethod]
    public void ClampToShort_InRange_ScalesLinearly()
    {
        Assert.AreEqual((short)0, AsioSampleConvertor.clampToShort(0.0));
        Assert.AreEqual((short)32767, AsioSampleConvertor.clampToShort(1.0));
        Assert.AreEqual((short)(-32767), AsioSampleConvertor.clampToShort(-1.0));
    }

    [TestMethod]
    public void ClampToShort_OutOfRange_Clamps()
    {
        Assert.AreEqual((short)32767, AsioSampleConvertor.clampToShort(10.0));
        Assert.AreEqual((short)(-32767), AsioSampleConvertor.clampToShort(-10.0));
    }

    [TestMethod]
    public void ClampTo24Bit_InRange_ScalesLinearly()
    {
        Assert.AreEqual(0, AsioSampleConvertor.clampTo24Bit(0.0));
        Assert.AreEqual(8388607, AsioSampleConvertor.clampTo24Bit(1.0));
        Assert.AreEqual(-8388607, AsioSampleConvertor.clampTo24Bit(-1.0));
    }

    [TestMethod]
    public void ClampTo24Bit_OutOfRange_Clamps()
    {
        Assert.AreEqual(8388607, AsioSampleConvertor.clampTo24Bit(3.0));
        Assert.AreEqual(-8388607, AsioSampleConvertor.clampTo24Bit(-3.0));
    }

    #endregion

    #region SelectSampleConvertor tests

    [TestMethod]
    public void SelectSampleConvertor_Int32LSB_16Bit_2Channels_SelectsOptimized2Channel()
    {
        var wf = new WaveFormat(44100, 16, 2);
        var convertor = AsioSampleConvertor.SelectSampleConvertor(wf, AsioSampleType.Int32LSB);
        Assert.AreEqual((AsioSampleConvertor.SampleConvertor)AsioSampleConvertor.ConvertorShortToInt2Channels, convertor);
    }

    [TestMethod]
    public void SelectSampleConvertor_Int32LSB_16Bit_MultiChannel_SelectsGeneric()
    {
        var wf = new WaveFormat(44100, 16, 4);
        var convertor = AsioSampleConvertor.SelectSampleConvertor(wf, AsioSampleType.Int32LSB);
        Assert.AreEqual((AsioSampleConvertor.SampleConvertor)AsioSampleConvertor.ConvertorShortToIntGeneric, convertor);
    }

    [TestMethod]
    public void SelectSampleConvertor_Int32LSB_32BitFloat_2Channels_SelectsFloatToInt2Channels()
    {
        var wf = WaveFormat.CreateIeeeFloatWaveFormat(44100, 2);
        var convertor = AsioSampleConvertor.SelectSampleConvertor(wf, AsioSampleType.Int32LSB);
        Assert.AreEqual((AsioSampleConvertor.SampleConvertor)AsioSampleConvertor.ConvertorFloatToInt2Channels, convertor);
    }

    [TestMethod]
    public void SelectSampleConvertor_Int32LSB_32BitFloat_MultiChannel_SelectsFloatToIntGeneric()
    {
        var wf = WaveFormat.CreateIeeeFloatWaveFormat(44100, 4);
        var convertor = AsioSampleConvertor.SelectSampleConvertor(wf, AsioSampleType.Int32LSB);
        Assert.AreEqual((AsioSampleConvertor.SampleConvertor)AsioSampleConvertor.ConvertorFloatToIntGeneric, convertor);
    }

    [TestMethod]
    public void SelectSampleConvertor_Int32LSB_32BitPcm_2Channels_SelectsIntToInt2Channels()
    {
        var wf = new WaveFormat(44100, 32, 2);
        var convertor = AsioSampleConvertor.SelectSampleConvertor(wf, AsioSampleType.Int32LSB);
        Assert.AreEqual((AsioSampleConvertor.SampleConvertor)AsioSampleConvertor.ConvertorIntToInt2Channels, convertor);
    }

    [TestMethod]
    public void SelectSampleConvertor_Int32LSB_32BitPcm_MultiChannel_SelectsIntToIntGeneric()
    {
        var wf = new WaveFormat(44100, 32, 4);
        var convertor = AsioSampleConvertor.SelectSampleConvertor(wf, AsioSampleType.Int32LSB);
        Assert.AreEqual((AsioSampleConvertor.SampleConvertor)AsioSampleConvertor.ConvertorIntToIntGeneric, convertor);
    }

    [TestMethod]
    public void SelectSampleConvertor_Int16LSB_16Bit_2Channels_SelectsShortToShort2Channels()
    {
        var wf = new WaveFormat(44100, 16, 2);
        var convertor = AsioSampleConvertor.SelectSampleConvertor(wf, AsioSampleType.Int16LSB);
        Assert.AreEqual((AsioSampleConvertor.SampleConvertor)AsioSampleConvertor.ConvertorShortToShort2Channels, convertor);
    }

    [TestMethod]
    public void SelectSampleConvertor_Int16LSB_16Bit_MultiChannel_SelectsGeneric()
    {
        var wf = new WaveFormat(44100, 16, 4);
        var convertor = AsioSampleConvertor.SelectSampleConvertor(wf, AsioSampleType.Int16LSB);
        Assert.AreEqual((AsioSampleConvertor.SampleConvertor)AsioSampleConvertor.ConvertorShortToShortGeneric, convertor);
    }

    [TestMethod]
    public void SelectSampleConvertor_Int16LSB_32BitFloat_2Channels_SelectsFloatToShort2Channels()
    {
        var wf = WaveFormat.CreateIeeeFloatWaveFormat(44100, 2);
        var convertor = AsioSampleConvertor.SelectSampleConvertor(wf, AsioSampleType.Int16LSB);
        Assert.AreEqual((AsioSampleConvertor.SampleConvertor)AsioSampleConvertor.ConvertorFloatToShort2Channels, convertor);
    }

    [TestMethod]
    public void SelectSampleConvertor_Int16LSB_32BitFloat_MultiChannel_SelectsFloatToShortGeneric()
    {
        var wf = WaveFormat.CreateIeeeFloatWaveFormat(44100, 4);
        var convertor = AsioSampleConvertor.SelectSampleConvertor(wf, AsioSampleType.Int16LSB);
        Assert.AreEqual((AsioSampleConvertor.SampleConvertor)AsioSampleConvertor.ConvertorFloatToShortGeneric, convertor);
    }

    [TestMethod]
    public void SelectSampleConvertor_Int16LSB_32BitPcm_2Channels_SelectsIntToShort2Channels()
    {
        var wf = new WaveFormat(44100, 32, 2);
        var convertor = AsioSampleConvertor.SelectSampleConvertor(wf, AsioSampleType.Int16LSB);
        Assert.AreEqual((AsioSampleConvertor.SampleConvertor)AsioSampleConvertor.ConvertorIntToShort2Channels, convertor);
    }

    [TestMethod]
    public void SelectSampleConvertor_Int16LSB_32BitPcm_MultiChannel_SelectsIntToShortGeneric()
    {
        var wf = new WaveFormat(44100, 32, 4);
        var convertor = AsioSampleConvertor.SelectSampleConvertor(wf, AsioSampleType.Int16LSB);
        Assert.AreEqual((AsioSampleConvertor.SampleConvertor)AsioSampleConvertor.ConvertorIntToShortGeneric, convertor);
    }

    [TestMethod]
    public void SelectSampleConvertor_Int24LSB_16Bit_ThrowsArgumentException()
    {
        var wf = new WaveFormat(44100, 16, 2);
        Assert.ThrowsExactly<ArgumentException>(() =>
            AsioSampleConvertor.SelectSampleConvertor(wf, AsioSampleType.Int24LSB));
    }

    [TestMethod]
    public void SelectSampleConvertor_Int24LSB_32BitFloat_SelectsConverterFloatTo24LSBGeneric()
    {
        var wf = WaveFormat.CreateIeeeFloatWaveFormat(44100, 2);
        var convertor = AsioSampleConvertor.SelectSampleConvertor(wf, AsioSampleType.Int24LSB);
        Assert.AreEqual((AsioSampleConvertor.SampleConvertor)AsioSampleConvertor.ConverterFloatTo24LSBGeneric, convertor);
    }

    [TestMethod]
    public void SelectSampleConvertor_Int24LSB_32BitPcm_ThrowsArgumentException()
    {
        var wf = new WaveFormat(44100, 32, 2);
        Assert.ThrowsExactly<ArgumentException>(() =>
            AsioSampleConvertor.SelectSampleConvertor(wf, AsioSampleType.Int24LSB));
    }

    [TestMethod]
    public void SelectSampleConvertor_Float32LSB_16Bit_ThrowsArgumentException()
    {
        var wf = new WaveFormat(44100, 16, 2);
        Assert.ThrowsExactly<ArgumentException>(() =>
            AsioSampleConvertor.SelectSampleConvertor(wf, AsioSampleType.Float32LSB));
    }

    [TestMethod]
    public void SelectSampleConvertor_Float32LSB_32BitFloat_SelectsFloatToFloatGeneric()
    {
        var wf = WaveFormat.CreateIeeeFloatWaveFormat(44100, 2);
        var convertor = AsioSampleConvertor.SelectSampleConvertor(wf, AsioSampleType.Float32LSB);
        Assert.AreEqual((AsioSampleConvertor.SampleConvertor)AsioSampleConvertor.ConverterFloatToFloatGeneric, convertor);
    }

    [TestMethod]
    public void SelectSampleConvertor_Float32LSB_32BitPcm_SelectsIntToFloatGeneric()
    {
        var wf = new WaveFormat(44100, 32, 2);
        var convertor = AsioSampleConvertor.SelectSampleConvertor(wf, AsioSampleType.Float32LSB);
        Assert.AreEqual((AsioSampleConvertor.SampleConvertor)AsioSampleConvertor.ConvertorIntToFloatGeneric, convertor);
    }

    [TestMethod]
    public void SelectSampleConvertor_UnsupportedAsioType_ThrowsArgumentException()
    {
        var wf = new WaveFormat(44100, 16, 2);
        Assert.ThrowsExactly<ArgumentException>(() =>
            AsioSampleConvertor.SelectSampleConvertor(wf, (AsioSampleType)9999));
    }

    #endregion

    #region Data conversion round-trip tests

    [TestMethod]
    public unsafe void ConvertorShortToInt2Channels_PlacesShortInUpper16BitsOfInt32()
    {
        short[] input = { 1000, -2000 };
        IntPtr inputPtr = Marshal.AllocHGlobal(input.Length * sizeof(short));
        IntPtr leftPtr = Marshal.AllocHGlobal(sizeof(int));
        IntPtr rightPtr = Marshal.AllocHGlobal(sizeof(int));
        try
        {
            Marshal.Copy(input, 0, inputPtr, input.Length);
            *(int*)leftPtr = 0;
            *(int*)rightPtr = 0;

            AsioSampleConvertor.ConvertorShortToInt2Channels(inputPtr, new[] { leftPtr, rightPtr }, 2, 1);

            int leftResult = *(int*)leftPtr;
            int rightResult = *(int*)rightPtr;

            // Value is placed in the upper 16 bits of the 32-bit int (i.e. shifted left by 16)
            Assert.AreEqual(1000 << 16, leftResult);
            Assert.AreEqual((short)(-2000) << 16, rightResult);
        }
        finally
        {
            Marshal.FreeHGlobal(inputPtr);
            Marshal.FreeHGlobal(leftPtr);
            Marshal.FreeHGlobal(rightPtr);
        }
    }

    [TestMethod]
    public unsafe void ConvertorShortToIntGeneric_MultiChannel_MatchesOptimized2ChannelBehavior()
    {
        short[] input = { 500, -700, 300, -900 }; // 2 samples x 2 channels interleaved
        IntPtr inputPtr = Marshal.AllocHGlobal(input.Length * sizeof(short));
        IntPtr leftPtr = Marshal.AllocHGlobal(2 * sizeof(int));
        IntPtr rightPtr = Marshal.AllocHGlobal(2 * sizeof(int));
        try
        {
            Marshal.Copy(input, 0, inputPtr, input.Length);
            // Zero the output buffers first: the converter only writes the upper 16 bits of each int32
            // slot (leaving the lower 16 bits untouched), so uninitialized memory would corrupt the result.
            ((int*)leftPtr)[0] = 0; ((int*)leftPtr)[1] = 0;
            ((int*)rightPtr)[0] = 0; ((int*)rightPtr)[1] = 0;

            AsioSampleConvertor.ConvertorShortToIntGeneric(inputPtr, new[] { leftPtr, rightPtr }, 2, 2);

            int* left = (int*)leftPtr;
            int* right = (int*)rightPtr;
            Assert.AreEqual(500 << 16, left[0]);
            Assert.AreEqual((short)(-700) << 16, right[0]);
            Assert.AreEqual(300 << 16, left[1]);
            Assert.AreEqual((short)(-900) << 16, right[1]);
        }
        finally
        {
            Marshal.FreeHGlobal(inputPtr);
            Marshal.FreeHGlobal(leftPtr);
            Marshal.FreeHGlobal(rightPtr);
        }
    }

    [TestMethod]
    public unsafe void ConvertorFloatToInt2Channels_MaxValue_ProducesIntMax()
    {
        float[] input = { 1.0f, -1.0f };
        IntPtr inputPtr = Marshal.AllocHGlobal(input.Length * sizeof(float));
        IntPtr leftPtr = Marshal.AllocHGlobal(sizeof(int));
        IntPtr rightPtr = Marshal.AllocHGlobal(sizeof(int));
        try
        {
            Marshal.Copy(input, 0, inputPtr, input.Length);

            AsioSampleConvertor.ConvertorFloatToInt2Channels(inputPtr, new[] { leftPtr, rightPtr }, 2, 1);

            Assert.AreEqual(2147483647, *(int*)leftPtr);
            Assert.AreEqual((int)(-1.0 * 2147483647.0), *(int*)rightPtr);
        }
        finally
        {
            Marshal.FreeHGlobal(inputPtr);
            Marshal.FreeHGlobal(leftPtr);
            Marshal.FreeHGlobal(rightPtr);
        }
    }

    [TestMethod]
    public unsafe void ConvertorFloatToInt2Channels_ClipsOutOfRangeValues()
    {
        float[] input = { 5.0f, -5.0f };
        IntPtr inputPtr = Marshal.AllocHGlobal(input.Length * sizeof(float));
        IntPtr leftPtr = Marshal.AllocHGlobal(sizeof(int));
        IntPtr rightPtr = Marshal.AllocHGlobal(sizeof(int));
        try
        {
            Marshal.Copy(input, 0, inputPtr, input.Length);

            AsioSampleConvertor.ConvertorFloatToInt2Channels(inputPtr, new[] { leftPtr, rightPtr }, 2, 1);

            Assert.AreEqual(2147483647, *(int*)leftPtr);
            Assert.AreEqual((int)(-1.0 * 2147483647.0), *(int*)rightPtr);
        }
        finally
        {
            Marshal.FreeHGlobal(inputPtr);
            Marshal.FreeHGlobal(leftPtr);
            Marshal.FreeHGlobal(rightPtr);
        }
    }

    [TestMethod]
    public unsafe void ConvertorFloatToIntGeneric_MultiChannel_ClipsAndConvertsCorrectly()
    {
        float[] input = { 0.0f, 2.0f, -2.0f, 0.5f };
        IntPtr inputPtr = Marshal.AllocHGlobal(input.Length * sizeof(float));
        IntPtr[] channels = { Marshal.AllocHGlobal(2 * sizeof(int)), Marshal.AllocHGlobal(2 * sizeof(int)) };
        try
        {
            Marshal.Copy(input, 0, inputPtr, input.Length);

            AsioSampleConvertor.ConvertorFloatToIntGeneric(inputPtr, channels, 2, 2);

            int* ch0 = (int*)channels[0];
            int* ch1 = (int*)channels[1];
            Assert.AreEqual(0, ch0[0]);
            Assert.AreEqual(2147483647, ch1[0]); // clipped from 2.0
            Assert.AreEqual((int)(-1.0 * 2147483647.0), ch0[1]); // clipped from -2.0
            Assert.AreEqual(AsioSampleConvertor.clampToInt(0.5), ch1[1]);
        }
        finally
        {
            Marshal.FreeHGlobal(inputPtr);
            Marshal.FreeHGlobal(channels[0]);
            Marshal.FreeHGlobal(channels[1]);
        }
    }

    [TestMethod]
    public unsafe void ConvertorIntToInt2Channels_CopiesValuesDirectly()
    {
        int[] input = { int.MaxValue, int.MinValue };
        IntPtr inputPtr = Marshal.AllocHGlobal(input.Length * sizeof(int));
        IntPtr leftPtr = Marshal.AllocHGlobal(sizeof(int));
        IntPtr rightPtr = Marshal.AllocHGlobal(sizeof(int));
        try
        {
            Marshal.Copy(input, 0, inputPtr, input.Length);

            AsioSampleConvertor.ConvertorIntToInt2Channels(inputPtr, new[] { leftPtr, rightPtr }, 2, 1);

            Assert.AreEqual(int.MaxValue, *(int*)leftPtr);
            Assert.AreEqual(int.MinValue, *(int*)rightPtr);
        }
        finally
        {
            Marshal.FreeHGlobal(inputPtr);
            Marshal.FreeHGlobal(leftPtr);
            Marshal.FreeHGlobal(rightPtr);
        }
    }

    [TestMethod]
    public unsafe void ConvertorIntToIntGeneric_MultiChannel_CopiesValuesDirectly()
    {
        int[] input = { 1, 2, 3, 4 };
        IntPtr inputPtr = Marshal.AllocHGlobal(input.Length * sizeof(int));
        IntPtr[] channels = { Marshal.AllocHGlobal(2 * sizeof(int)), Marshal.AllocHGlobal(2 * sizeof(int)) };
        try
        {
            Marshal.Copy(input, 0, inputPtr, input.Length);

            AsioSampleConvertor.ConvertorIntToIntGeneric(inputPtr, channels, 2, 2);

            int* ch0 = (int*)channels[0];
            int* ch1 = (int*)channels[1];
            Assert.AreEqual(1, ch0[0]);
            Assert.AreEqual(2, ch1[0]);
            Assert.AreEqual(3, ch0[1]);
            Assert.AreEqual(4, ch1[1]);
        }
        finally
        {
            Marshal.FreeHGlobal(inputPtr);
            Marshal.FreeHGlobal(channels[0]);
            Marshal.FreeHGlobal(channels[1]);
        }
    }

    [TestMethod]
    public unsafe void ConvertorIntToShort2Channels_ShiftsRightBy16Bits()
    {
        int[] input = { 1000 << 16, -2000 << 16 };
        IntPtr inputPtr = Marshal.AllocHGlobal(input.Length * sizeof(int));
        IntPtr leftPtr = Marshal.AllocHGlobal(sizeof(short));
        IntPtr rightPtr = Marshal.AllocHGlobal(sizeof(short));
        try
        {
            Marshal.Copy(input, 0, inputPtr, input.Length);

            AsioSampleConvertor.ConvertorIntToShort2Channels(inputPtr, new[] { leftPtr, rightPtr }, 2, 1);

            Assert.AreEqual((short)1000, *(short*)leftPtr);
            Assert.AreEqual((short)(-2000), *(short*)rightPtr);
        }
        finally
        {
            Marshal.FreeHGlobal(inputPtr);
            Marshal.FreeHGlobal(leftPtr);
            Marshal.FreeHGlobal(rightPtr);
        }
    }

    [TestMethod]
    public unsafe void ConvertorIntToShortGeneric_MultiChannel_ShiftsRightBy16Bits()
    {
        // SUSPECTED REAL DEFECT: NAudio\Asio\ASIOSampleConvertor.cs ConvertorIntToShortGeneric (around line 269-288) â€”
        // the output buffers are declared/advanced as `int*[] samples` (4-byte stride) even though each written
        // value is cast to `short` and the method name/contract implies a 16-bit (2-byte) SHORT output buffer,
        // consistent with the optimized ConvertorIntToShort2Channels sibling which correctly uses `short*`.
        // This test documents the CORRECT expected behavior (2-byte packed short buffer) and is expected to FAIL
        // against the current implementation, which instead writes 4-byte-strided int-sized slots.
        int[] input = { 100 << 16, 200 << 16, 300 << 16, 400 << 16 };
        IntPtr inputPtr = Marshal.AllocHGlobal(input.Length * sizeof(int));
        // NOTE: allocate generously (sized for the buggy 4-byte-stride write path) to avoid heap corruption
        // regardless of whether the implementation is fixed to the correct 2-byte stride in the future.
        IntPtr[] channels = { Marshal.AllocHGlobal(2 * sizeof(int)), Marshal.AllocHGlobal(2 * sizeof(int)) };
        try
        {
            Marshal.Copy(input, 0, inputPtr, input.Length);

            AsioSampleConvertor.ConvertorIntToShortGeneric(inputPtr, channels, 2, 2);

            short* ch0 = (short*)channels[0];
            short* ch1 = (short*)channels[1];
            Assert.AreEqual((short)100, ch0[0]);
            Assert.AreEqual((short)200, ch1[0]);
            Assert.AreEqual((short)300, ch0[1]);
            Assert.AreEqual((short)400, ch1[1]);
        }
        finally
        {
            Marshal.FreeHGlobal(inputPtr);
            Marshal.FreeHGlobal(channels[0]);
            Marshal.FreeHGlobal(channels[1]);
        }
    }

    [TestMethod]
    public unsafe void ConvertorIntToFloatGeneric_ConvertsFullScaleIntToFloat()
    {
        int[] input = { int.MaxValue, int.MinValue, 0, 0 };
        IntPtr inputPtr = Marshal.AllocHGlobal(input.Length * sizeof(int));
        IntPtr[] channels = { Marshal.AllocHGlobal(2 * sizeof(float)), Marshal.AllocHGlobal(2 * sizeof(float)) };
        try
        {
            Marshal.Copy(input, 0, inputPtr, input.Length);

            AsioSampleConvertor.ConvertorIntToFloatGeneric(inputPtr, channels, 2, 2);

            float* ch0 = (float*)channels[0];
            float* ch1 = (float*)channels[1];
            Assert.AreEqual((float)(int.MaxValue / (1 << 31)), ch0[0], 0.0001f);
            Assert.AreEqual((float)(int.MinValue / (1 << 31)), ch1[0], 0.0001f);
        }
        finally
        {
            Marshal.FreeHGlobal(inputPtr);
            Marshal.FreeHGlobal(channels[0]);
            Marshal.FreeHGlobal(channels[1]);
        }
    }

    [TestMethod]
    public unsafe void ConvertorShortToShort2Channels_CopiesValuesDirectly()
    {
        short[] input = { 12345, -12345 };
        IntPtr inputPtr = Marshal.AllocHGlobal(input.Length * sizeof(short));
        IntPtr leftPtr = Marshal.AllocHGlobal(sizeof(short));
        IntPtr rightPtr = Marshal.AllocHGlobal(sizeof(short));
        try
        {
            Marshal.Copy(input, 0, inputPtr, input.Length);

            AsioSampleConvertor.ConvertorShortToShort2Channels(inputPtr, new[] { leftPtr, rightPtr }, 2, 1);

            Assert.AreEqual((short)12345, *(short*)leftPtr);
            Assert.AreEqual((short)(-12345), *(short*)rightPtr);
        }
        finally
        {
            Marshal.FreeHGlobal(inputPtr);
            Marshal.FreeHGlobal(leftPtr);
            Marshal.FreeHGlobal(rightPtr);
        }
    }

    [TestMethod]
    public unsafe void ConvertorShortToShortGeneric_MultiChannel_CopiesValuesDirectly()
    {
        short[] input = { 1, 2, 3, 4 };
        IntPtr inputPtr = Marshal.AllocHGlobal(input.Length * sizeof(short));
        IntPtr[] channels = { Marshal.AllocHGlobal(2 * sizeof(short)), Marshal.AllocHGlobal(2 * sizeof(short)) };
        try
        {
            Marshal.Copy(input, 0, inputPtr, input.Length);

            AsioSampleConvertor.ConvertorShortToShortGeneric(inputPtr, channels, 2, 2);

            short* ch0 = (short*)channels[0];
            short* ch1 = (short*)channels[1];
            Assert.AreEqual((short)1, ch0[0]);
            Assert.AreEqual((short)2, ch1[0]);
            Assert.AreEqual((short)3, ch0[1]);
            Assert.AreEqual((short)4, ch1[1]);
        }
        finally
        {
            Marshal.FreeHGlobal(inputPtr);
            Marshal.FreeHGlobal(channels[0]);
            Marshal.FreeHGlobal(channels[1]);
        }
    }

    [TestMethod]
    public unsafe void ConvertorFloatToShort2Channels_ClipsAndConvertsCorrectly()
    {
        float[] input = { 2.0f, -2.0f };
        IntPtr inputPtr = Marshal.AllocHGlobal(input.Length * sizeof(float));
        IntPtr leftPtr = Marshal.AllocHGlobal(sizeof(short));
        IntPtr rightPtr = Marshal.AllocHGlobal(sizeof(short));
        try
        {
            Marshal.Copy(input, 0, inputPtr, input.Length);

            AsioSampleConvertor.ConvertorFloatToShort2Channels(inputPtr, new[] { leftPtr, rightPtr }, 2, 1);

            Assert.AreEqual((short)32767, *(short*)leftPtr);
            Assert.AreEqual((short)(-32767), *(short*)rightPtr);
        }
        finally
        {
            Marshal.FreeHGlobal(inputPtr);
            Marshal.FreeHGlobal(leftPtr);
            Marshal.FreeHGlobal(rightPtr);
        }
    }

    [TestMethod]
    public unsafe void ConvertorFloatToShortGeneric_MultiChannel_ClipsAndConvertsCorrectly()
    {
        float[] input = { 0.0f, 1.0f, -1.0f, 0.25f };
        IntPtr inputPtr = Marshal.AllocHGlobal(input.Length * sizeof(float));
        IntPtr[] channels = { Marshal.AllocHGlobal(2 * sizeof(short)), Marshal.AllocHGlobal(2 * sizeof(short)) };
        try
        {
            Marshal.Copy(input, 0, inputPtr, input.Length);

            AsioSampleConvertor.ConvertorFloatToShortGeneric(inputPtr, channels, 2, 2);

            short* ch0 = (short*)channels[0];
            short* ch1 = (short*)channels[1];
            Assert.AreEqual((short)0, ch0[0]);
            Assert.AreEqual((short)32767, ch1[0]);
            Assert.AreEqual((short)(-32767), ch0[1]);
            Assert.AreEqual(AsioSampleConvertor.clampToShort(0.25), ch1[1]);
        }
        finally
        {
            Marshal.FreeHGlobal(inputPtr);
            Marshal.FreeHGlobal(channels[0]);
            Marshal.FreeHGlobal(channels[1]);
        }
    }

    [TestMethod]
    public unsafe void ConverterFloatTo24LSBGeneric_WritesThreeBytesLittleEndianPerSample()
    {
        float[] input = { 1.0f, -1.0f };
        IntPtr inputPtr = Marshal.AllocHGlobal(input.Length * sizeof(float));
        IntPtr[] channels = { Marshal.AllocHGlobal(3), Marshal.AllocHGlobal(3) };
        try
        {
            Marshal.Copy(input, 0, inputPtr, input.Length);

            AsioSampleConvertor.ConverterFloatTo24LSBGeneric(inputPtr, channels, 2, 1);

            byte* ch0 = (byte*)channels[0];
            byte* ch1 = (byte*)channels[1];
            int expectedMax = AsioSampleConvertor.clampTo24Bit(1.0);
            int expectedMin = AsioSampleConvertor.clampTo24Bit(-1.0);

            Assert.AreEqual((byte)expectedMax, ch0[0]);
            Assert.AreEqual((byte)(expectedMax >> 8), ch0[1]);
            Assert.AreEqual((byte)(expectedMax >> 16), ch0[2]);

            Assert.AreEqual((byte)expectedMin, ch1[0]);
            Assert.AreEqual((byte)(expectedMin >> 8), ch1[1]);
            Assert.AreEqual((byte)(expectedMin >> 16), ch1[2]);
        }
        finally
        {
            Marshal.FreeHGlobal(inputPtr);
            Marshal.FreeHGlobal(channels[0]);
            Marshal.FreeHGlobal(channels[1]);
        }
    }

    [TestMethod]
    public unsafe void ConverterFloatToFloatGeneric_CopiesValuesDirectly()
    {
        float[] input = { 0.1f, 0.2f, 0.3f, 0.4f };
        IntPtr inputPtr = Marshal.AllocHGlobal(input.Length * sizeof(float));
        IntPtr[] channels = { Marshal.AllocHGlobal(2 * sizeof(float)), Marshal.AllocHGlobal(2 * sizeof(float)) };
        try
        {
            Marshal.Copy(input, 0, inputPtr, input.Length);

            AsioSampleConvertor.ConverterFloatToFloatGeneric(inputPtr, channels, 2, 2);

            float* ch0 = (float*)channels[0];
            float* ch1 = (float*)channels[1];
            Assert.AreEqual(0.1f, ch0[0], 0.00001f);
            Assert.AreEqual(0.2f, ch1[0], 0.00001f);
            Assert.AreEqual(0.3f, ch0[1], 0.00001f);
            Assert.AreEqual(0.4f, ch1[1], 0.00001f);
        }
        finally
        {
            Marshal.FreeHGlobal(inputPtr);
            Marshal.FreeHGlobal(channels[0]);
            Marshal.FreeHGlobal(channels[1]);
        }
    }

    #endregion
}
