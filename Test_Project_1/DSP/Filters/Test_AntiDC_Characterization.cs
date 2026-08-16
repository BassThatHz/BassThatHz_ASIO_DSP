#nullable enable

namespace Test_Project_1;

#region Usings
using BassThatHz_ASIO_DSP_Processor;
using System;
using Test_Project_1.TestHelpers;
#endregion

/// <summary>
/// CHARACTERIZATION / GUARD SUITE for DSP\Filters\AntiDC.cs.
///
/// AntiDC is a protection filter: it counts CONSECUTIVE IDENTICAL samples (a stuck DAC / DC fault)
/// and counts clip events inside a rolling window, and once either limit is hit it latches
/// IsOutputMuted and zeroes the output forever. All of that state - ConsecutiveDCEventsDetected,
/// ClipEventsPerDurationDetected, PreviousInputValue, WasPreviousInputIdentical, IsOutputMuted -
/// carries between blocks, so the sequences below run several consecutive blocks and pin the latch.
///
/// WALL-CLOCK NOTE: the clip detector compares DateTime.UtcNow against DetectionDuration. That is
/// handled two ways here, both fully deterministic:
///   * the DC tests raise Clip_Threshold above every sample they use, so the clip branch is never
///     entered at all; and
///   * the clip tests set DetectionDuration to one day, so the window is unconditionally open after
///     the first clipped sample regardless of how fast the machine is.
/// </summary>
[TestClass]
public class Test_AntiDC_Characterization
{
    #region Multi-Block Stateful Sequence - DC latch

    private static double[][] BuildDcInputs()
    {
        return new[]
        {
            DspCharacterization.Sine(8, 1, 0.5),
            DspCharacterization.Constant(8, 0.5),
            DspCharacterization.Constant(8, 0.5),
            DspCharacterization.Sine(8, 1, 0.5),
            DspCharacterization.Constant(8, 0.0),
            DspCharacterization.Noise(8, 17000UL),
        };
    }

    /// <summary>
    /// Six consecutive blocks with MaxConsecutiveDCSamples set to 10.
    ///
    /// Block 0 is a sine and passes through. Blocks 1 and 2 are a constant 0.5 - the DC fault -
    /// so the consecutive counter climbs across the BLOCK BOUNDARY and trips two samples into
    /// block 2, from which point every sample is zeroed. Blocks 3-5 prove the mute is LATCHED:
    /// perfectly healthy audio afterwards still comes out silent.
    /// </summary>
    [TestMethod]
    public void Stateful_Transform_DcFaultLatchesTheMuteAcrossBlockBoundaries()
    {
        double[][] Local_Expected =
        {
            new[] { 0.0d, 0.3535533905932738d, 0.5d, 0.3535533905932738d, 6.123233995736766E-17d, -0.35355339059327373d, -0.5d, -0.35355339059327384d },
            new[] { 0.5d, 0.5d, 0.5d, 0.5d, 0.5d, 0.5d, 0.5d, 0.5d },
            new[] { 0.5d, 0.5d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d },
            new[] { 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d },
            new[] { 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d },
            new[] { 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d },
        };

        var Local_Stream = new DSP_Stream();
        var Local_AntiDC = new AntiDC { Clip_Threshold = 2.0, MaxConsecutiveDCSamples = 10 };
        var Local_Inputs = BuildDcInputs();

        for (int Local_Block = 0; Local_Block < Local_Inputs.Length; Local_Block++)
        {
            var Local_Result = Local_AntiDC.Transform(DspCharacterization.Copy(Local_Inputs[Local_Block]), Local_Stream);
            DspCharacterization.AssertExact(Local_Expected[Local_Block], Local_Result, "Block " + Local_Block);
        }
    }

    /// <summary>
    /// ResetDetection clears the latch and both counters, so healthy audio flows again.
    /// </summary>
    [TestMethod]
    public void Stateful_ResetDetection_ClearsTheLatch()
    {
        var Local_Stream = new DSP_Stream();
        var Local_AntiDC = new AntiDC { Clip_Threshold = 2.0, MaxConsecutiveDCSamples = 10 };

        for (int Local_Block = 0; Local_Block < 3; Local_Block++)
            Local_AntiDC.Transform(DspCharacterization.Constant(8, 0.5), Local_Stream);

        //Confirm it really is muted before resetting.
        DspCharacterization.AssertExact(new double[8],
            Local_AntiDC.Transform(DspCharacterization.Sine(8, 1, 0.5), Local_Stream), "Still muted before the reset");

        Local_AntiDC.ResetDetection();

        DspCharacterization.AssertExact(
            new double[]
            {
                0.0d, 0.3535533905932738d, 0.5d, 0.3535533905932738d,
                6.123233995736766E-17d, -0.35355339059327373d, -0.5d, -0.35355339059327384d
            },
            Local_AntiDC.Transform(DspCharacterization.Sine(8, 1, 0.5), Local_Stream),
            "ResetDetection must let audio through again");
    }

    #endregion

    #region Multi-Block Stateful Sequence - Clip latch

    /// <summary>
    /// Three consecutive blocks with Clip_Threshold 0.9 and MaxClipEventsPerDuration 3, and a
    /// one-day detection window so the timing comparison is deterministic.
    ///
    /// Block 0 is below the clip threshold and passes through. Block 1 is full-scale alternating,
    /// so the third clipped sample trips the latch and the rest of the block is zeroed. Block 2
    /// proves the latch holds.
    /// </summary>
    [TestMethod]
    public void Stateful_Transform_ClipEventsLatchTheMute()
    {
        double[][] Local_Expected =
        {
            new[] { 0.0d, 0.3535533905932738d, 0.5d, 0.3535533905932738d, 6.123233995736766E-17d, -0.35355339059327373d, -0.5d, -0.35355339059327384d },
            new[] { 1.0d, -1.0d, 1.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d },
            new[] { 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d },
        };

        var Local_Stream = new DSP_Stream();
        var Local_AntiDC = new AntiDC
        {
            Clip_Threshold = 0.9,
            MaxClipEventsPerDuration = 3,
            DetectionDuration = TimeSpan.FromDays(1),
            MaxConsecutiveDCSamples = 1000
        };

        DspCharacterization.AssertExact(Local_Expected[0],
            Local_AntiDC.Transform(DspCharacterization.Sine(8, 1, 0.5), Local_Stream), "Block 0");
        DspCharacterization.AssertExact(Local_Expected[1],
            Local_AntiDC.Transform(DspCharacterization.Alternating(8, 1.0), Local_Stream), "Block 1");
        DspCharacterization.AssertExact(Local_Expected[2],
            Local_AntiDC.Transform(DspCharacterization.Sine(8, 1, 0.5), Local_Stream), "Block 2");
    }

    /// <summary>
    /// With MaxClipEventsPerDuration 1, a full-scale block trips on the SECOND clipped sample
    /// (the first one only arms the window), so exactly one sample survives.
    /// </summary>
    [TestMethod]
    public void Stateful_Transform_SingleClipEventTripsOnTheSecondSample()
    {
        var Local_Stream = new DSP_Stream();
        var Local_AntiDC = new AntiDC
        {
            Clip_Threshold = 0.9,
            MaxClipEventsPerDuration = 1,
            DetectionDuration = TimeSpan.FromDays(1),
            MaxConsecutiveDCSamples = 1000
        };

        DspCharacterization.AssertExact(
            new double[] { 1.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d },
            Local_AntiDC.Transform(DspCharacterization.Constant(8, 1.0), Local_Stream), "First block");

        DspCharacterization.AssertExact(new double[8],
            Local_AntiDC.Transform(DspCharacterization.Sine(8, 1, 0.5), Local_Stream), "Second block stays muted");
    }

    #endregion

    #region Property Tests

    /// <summary>
    /// Healthy audio that never repeats a value and never clips passes through bit-exactly, block
    /// after block.
    /// </summary>
    [TestMethod]
    public void Property_Transform_HealthyAudioPassesThroughUntouched()
    {
        var Local_Stream = new DSP_Stream();
        var Local_AntiDC = new AntiDC
        {
            Clip_Threshold = 2.0,
            MaxConsecutiveDCSamples = 4,
            DetectionDuration = TimeSpan.FromDays(1)
        };

        for (int Local_Block = 0; Local_Block < 8; Local_Block++)
        {
            var Local_Original = DspCharacterization.Noise(32, (ulong)(42000 + Local_Block));
            var Local_Result = Local_AntiDC.Transform(DspCharacterization.Copy(Local_Original), Local_Stream);
            DspCharacterization.AssertExact(Local_Original, Local_Result, "Block " + Local_Block);
        }
    }

    /// <summary>
    /// Silence is BELOW DC_Threshold, so it is never treated as a stuck-DAC fault however long it
    /// lasts. This matters: a paused source must not mute the output permanently.
    /// </summary>
    [TestMethod]
    public void Property_Transform_SilenceIsNotTreatedAsADcFault()
    {
        var Local_Stream = new DSP_Stream();
        var Local_AntiDC = new AntiDC
        {
            Clip_Threshold = 2.0,
            MaxConsecutiveDCSamples = 2,
            DetectionDuration = TimeSpan.FromDays(1)
        };

        for (int Local_Block = 0; Local_Block < 8; Local_Block++)
            DspCharacterization.AssertExact(new double[16], Local_AntiDC.Transform(new double[16], Local_Stream),
                "Silent block " + Local_Block);

        //And audio still flows afterwards.
        var Local_Original = DspCharacterization.Noise(16, 42100UL);
        DspCharacterization.AssertExact(Local_Original,
            Local_AntiDC.Transform(DspCharacterization.Copy(Local_Original), Local_Stream),
            "Audio must still flow after prolonged silence");
    }

    /// <summary>
    /// The MaxConsecutiveDCSamples and MaxClipEventsPerDuration setters clamp to a minimum of 1.
    /// </summary>
    [TestMethod]
    public void Property_Setters_ClampToAMinimumOfOne()
    {
        var Local_AntiDC = new AntiDC();

        Local_AntiDC.MaxConsecutiveDCSamples = 0;
        Assert.AreEqual(1, Local_AntiDC.MaxConsecutiveDCSamples);
        Local_AntiDC.MaxConsecutiveDCSamples = -100;
        Assert.AreEqual(1, Local_AntiDC.MaxConsecutiveDCSamples);
        Local_AntiDC.MaxConsecutiveDCSamples = 42;
        Assert.AreEqual(42, Local_AntiDC.MaxConsecutiveDCSamples);

        Local_AntiDC.MaxClipEventsPerDuration = 0;
        Assert.AreEqual(1, Local_AntiDC.MaxClipEventsPerDuration);
        Local_AntiDC.MaxClipEventsPerDuration = -5;
        Assert.AreEqual(1, Local_AntiDC.MaxClipEventsPerDuration);
        Local_AntiDC.MaxClipEventsPerDuration = 7;
        Assert.AreEqual(7, Local_AntiDC.MaxClipEventsPerDuration);
    }

    /// <summary>
    /// Defaults, pinned so a change to the protection thresholds is deliberate.
    /// </summary>
    [TestMethod]
    public void Property_Defaults()
    {
        var Local_AntiDC = new AntiDC();
        Assert.AreEqual(42, Local_AntiDC.MaxConsecutiveDCSamples);
        Assert.AreEqual(1, Local_AntiDC.MaxClipEventsPerDuration);
        DspCharacterization.AssertExact(0.9999d, Local_AntiDC.Clip_Threshold, "Clip_Threshold");
        DspCharacterization.AssertExact(1E-05d, Local_AntiDC.DC_Threshold, "DC_Threshold");
        Assert.AreEqual(TimeSpan.FromMilliseconds(1), Local_AntiDC.DetectionDuration);
    }

    #endregion

    #region Aliasing / Mutation Contract

    /// <summary>
    /// Transform mutates the caller's array in place and returns that same instance, both on the
    /// pass-through path and on the muted path.
    /// </summary>
    [TestMethod]
    public void Contract_Transform_MutatesInPlaceAndReturnsTheInputInstance()
    {
        var Local_Stream = new DSP_Stream();
        var Local_AntiDC = new AntiDC { Clip_Threshold = 2.0, MaxConsecutiveDCSamples = 4 };

        var Local_PassThroughInput = DspCharacterization.Noise(8, 42200UL);
        Assert.IsTrue(ReferenceEquals(Local_PassThroughInput, Local_AntiDC.Transform(Local_PassThroughInput, Local_Stream)));

        //Trip the latch, then confirm the muted path writes into the caller's array.
        Local_AntiDC.Transform(DspCharacterization.Constant(8, 0.5), Local_Stream);
        Local_AntiDC.Transform(DspCharacterization.Constant(8, 0.5), Local_Stream);

        var Local_MutedInput = DspCharacterization.Constant(8, 0.5);
        var Local_MutedResult = Local_AntiDC.Transform(Local_MutedInput, Local_Stream);
        Assert.IsTrue(ReferenceEquals(Local_MutedInput, Local_MutedResult));
        DspCharacterization.AssertExact(0.0d, Local_MutedInput[0], "The muted path zeroes the caller's array in place");

        var Local_Empty = new double[0];
        Assert.IsTrue(ReferenceEquals(Local_Empty, Local_AntiDC.Transform(Local_Empty, Local_Stream)));
    }

    #endregion
}
