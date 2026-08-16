#nullable enable

namespace Test_Project_1;

#region Usings
using BassThatHz_ASIO_DSP_Processor;
using System;
using Test_Project_1.TestHelpers;
#endregion

/// <summary>
/// CHARACTERIZATION / GUARD SUITE for DSP\Filters\Delay.cs.
///
/// Delay is a circular-buffer filter whose entire behavior IS its state: DelayBuffer, ReadIndex and
/// WriteIndex all carry across calls. All of those are protected, so the sequence of outputs over
/// many consecutive blocks is the observable that pins them - a single-block test would say almost
/// nothing.
///
/// The sequences below use a monotonically increasing ramp so a wrong index shows up as an obviously
/// wrong integer rather than as noise.
/// </summary>
[TestClass]
public class Test_Delay_Characterization
{
    #region Multi-Block Stateful Sequences

    /// <summary>
    /// Six consecutive blocks at 48 kHz with 0.125 ms of delay (exactly 6 samples) and a nominal
    /// 8-sample block, INCLUDING a mid-sequence block-size change to 4 and back to 8.
    ///
    /// The ramp starts at 1, so the first six outputs are the zero-filled buffer, then the input
    /// reappears delayed by exactly six samples and stays aligned across the size change.
    /// </summary>
    [TestMethod]
    public void Stateful_Transform_SixBlocksIncludingABlockSizeChange()
    {
        double[][] Local_Expected =
        {
            new[] { 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 1.0d, 2.0d },
            new[] { 3.0d, 4.0d, 5.0d, 6.0d, 7.0d, 8.0d, 9.0d, 10.0d },
            new[] { 11.0d, 12.0d, 13.0d, 14.0d, 15.0d, 16.0d, 17.0d, 18.0d },
            new[] { 19.0d, 20.0d, 21.0d, 22.0d, 23.0d, 24.0d, 25.0d, 26.0d },
            new[] { 27.0d, 28.0d, 29.0d, 30.0d },
            new[] { 31.0d, 32.0d, 33.0d, 34.0d, 35.0d, 36.0d, 37.0d, 38.0d },
        };

        var Local_Stream = new DSP_Stream();
        var Local_Delay = new Delay();
        Local_Delay.Initialize(0.125m, 8, 48000);

        for (int Local_Block = 0; Local_Block < 4; Local_Block++)
        {
            var Local_Result = Local_Delay.Transform(DspCharacterization.Ramp(8, Local_Block * 8 + 1, 1), Local_Stream);
            DspCharacterization.AssertExact(Local_Expected[Local_Block], Local_Result, "Block " + Local_Block);
        }

        DspCharacterization.AssertExact(Local_Expected[4],
            Local_Delay.Transform(DspCharacterization.Ramp(4, 33, 1), Local_Stream), "Block 4 (size drops to 4)");
        DspCharacterization.AssertExact(Local_Expected[5],
            Local_Delay.Transform(DspCharacterization.Ramp(8, 37, 1), Local_Stream), "Block 5 (size back to 8)");
    }

    /// <summary>
    /// ResetSampleRate mid-sequence recomputes the delay in samples AND clears the circular buffer,
    /// so the delay line restarts from silence. At 96 kHz the same 0.125 ms becomes 12 samples.
    /// </summary>
    [TestMethod]
    public void Stateful_ResetSampleRate_MidSequenceClearsTheDelayLine()
    {
        double[][] Local_Expected =
        {
            new[] { 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d },
            new[] { 0.0d, 0.0d, 0.0d, 0.0d, 100.0d, 101.0d, 102.0d, 103.0d },
            new[] { 104.0d, 105.0d, 106.0d, 107.0d, 108.0d, 109.0d, 110.0d, 111.0d },
        };

        var Local_Stream = new DSP_Stream();
        var Local_Delay = new Delay();
        Local_Delay.Initialize(0.125m, 8, 48000);

        //Prime the delay line with four blocks, then change the sample rate.
        for (int Local_Block = 0; Local_Block < 4; Local_Block++)
            Local_Delay.Transform(DspCharacterization.Ramp(8, Local_Block * 8 + 1, 1), Local_Stream);
        Local_Delay.Transform(DspCharacterization.Ramp(4, 33, 1), Local_Stream);
        Local_Delay.Transform(DspCharacterization.Ramp(8, 37, 1), Local_Stream);

        Local_Delay.ResetSampleRate(96000);

        for (int Local_Block = 0; Local_Block < Local_Expected.Length; Local_Block++)
        {
            var Local_Result = Local_Delay.Transform(DspCharacterization.Ramp(8, 100 + Local_Block * 8, 1), Local_Stream);
            DspCharacterization.AssertExact(Local_Expected[Local_Block], Local_Result, "Post-reset block " + Local_Block);
        }
    }

    /// <summary>
    /// Setting DelayInMS mid-sequence likewise resizes and clears. The observable is the number of
    /// leading zeros before the input reappears.
    /// </summary>
    [TestMethod]
    public void Stateful_DelayInMS_MidSequenceResizesAndClears()
    {
        var Local_Stream = new DSP_Stream();
        var Local_Delay = new Delay();
        Local_Delay.Initialize(0.125m, 8, 48000);

        for (int Local_Block = 0; Local_Block < 4; Local_Block++)
            Local_Delay.Transform(DspCharacterization.Ramp(8, Local_Block * 8 + 1, 1), Local_Stream);

        //0.0625 ms at 48 kHz is exactly 3 samples.
        Local_Delay.DelayInMS = 0.0625m;

        var Local_First = Local_Delay.Transform(DspCharacterization.Ramp(8, 1, 1), Local_Stream);
        DspCharacterization.AssertExact(
            new double[] { 0.0d, 0.0d, 0.0d, 1.0d, 2.0d, 3.0d, 4.0d, 5.0d },
            Local_First, "Three leading zeros after shortening the delay");

        var Local_Second = Local_Delay.Transform(DspCharacterization.Ramp(8, 9, 1), Local_Stream);
        DspCharacterization.AssertExact(
            new double[] { 6.0d, 7.0d, 8.0d, 9.0d, 10.0d, 11.0d, 12.0d, 13.0d },
            Local_Second, "And the ramp continues uninterrupted");
    }

    #endregion

    #region Property Tests

    /// <summary>
    /// The delay in samples is truncate(sampleRate * delayInMS / 1000). Verified through the
    /// observable: exactly that many leading zeros before the impulse comes out.
    /// </summary>
    [TestMethod]
    public void Property_Transform_DelaysByTheExpectedNumberOfSamples()
    {
        (int SampleRate, decimal DelayMs, int ExpectedSamples)[] Local_Cases =
        {
            (48000, 0m, 0), (48000, 0.125m, 6), (48000, 0.25m, 12), (96000, 0.125m, 12), (44100, 0.5m, 22)
        };

        foreach (var Local_Case in Local_Cases)
        {
            var Local_Delay = new Delay();
            Local_Delay.Initialize(Local_Case.DelayMs, 32, Local_Case.SampleRate);
            var Local_Stream = new DSP_Stream();

            //Feed an impulse and count how many samples pass before it emerges.
            var Local_Output = Local_Delay.Transform(DspCharacterization.Impulse(32, 0, 1.0), Local_Stream);

            int Local_FoundAt = -1;
            for (int i = 0; i < Local_Output.Length; i++)
            {
                if (Local_Output[i] != 0.0)
                {
                    Local_FoundAt = i;
                    break;
                }
            }

            Assert.AreEqual(Local_Case.ExpectedSamples, Local_FoundAt,
                "Delay of " + Local_Case.DelayMs + " ms at " + Local_Case.SampleRate + " Hz");
            DspCharacterization.AssertExact(1.0d, Local_Output[Local_FoundAt], "The impulse must come through unattenuated");
        }
    }

    /// <summary>
    /// A zero-delay Delay is a bit-exact pass-through once the pipeline is full.
    /// </summary>
    [TestMethod]
    public void Property_Transform_ZeroDelayIsAnExactPassThrough()
    {
        var Local_Delay = new Delay();
        Local_Delay.Initialize(0m, 8, 48000);
        var Local_Stream = new DSP_Stream();

        for (int Local_Block = 0; Local_Block < 6; Local_Block++)
        {
            var Local_Original = DspCharacterization.Noise(8, (ulong)(36000 + Local_Block));
            var Local_Result = Local_Delay.Transform(DspCharacterization.Copy(Local_Original), Local_Stream);
            DspCharacterization.AssertExact(Local_Original, Local_Result, "Block " + Local_Block);
        }
    }

    #endregion

    #region Guard Paths / Aliasing

    /// <summary>
    /// A Delay that was never initialized has a null DelayBuffer and is a complete pass-through.
    /// </summary>
    [TestMethod]
    public void Contract_Transform_UninitializedIsAPassThrough()
    {
        var Local_Input = DspCharacterization.Ramp(8, 1, 1);
        var Local_Result = new Delay().Transform(Local_Input, new DSP_Stream());
        Assert.IsTrue(ReferenceEquals(Local_Input, Local_Result));
        DspCharacterization.AssertExact(
            new double[] { 1.0d, 2.0d, 3.0d, 4.0d, 5.0d, 6.0d, 7.0d, 8.0d }, Local_Result,
            "An uninitialized Delay must not alter the block");
    }

    /// <summary>
    /// Transform mutates the caller's array in place and returns that same instance.
    /// </summary>
    [TestMethod]
    public void Contract_Transform_MutatesInPlaceAndReturnsTheInputInstance()
    {
        var Local_Delay = new Delay();
        Local_Delay.Initialize(0.125m, 8, 48000);

        var Local_Input = DspCharacterization.Ramp(8, 1, 1);
        var Local_Result = Local_Delay.Transform(Local_Input, new DSP_Stream());

        Assert.IsTrue(ReferenceEquals(Local_Input, Local_Result));
        DspCharacterization.AssertExact(0.0d, Local_Input[0], "The caller's array was overwritten in place");
    }

    /// <summary>
    /// Negative arguments are rejected.
    /// </summary>
    [TestMethod]
    public void Contract_NegativeArgumentsAreRejected()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new Delay().DelayInMS = -1m);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new Delay().ResetSampleRate(-1));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new Delay().ResetBufferSize(-1));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new Delay().Initialize(-1m, 8, 48000));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new Delay().Initialize(1m, -8, 48000));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new Delay().Initialize(1m, 8, -48000));
    }

    #endregion
}
