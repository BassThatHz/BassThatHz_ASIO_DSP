#nullable enable

namespace Test_Project_1;

#region Usings
using BassThatHz_ASIO_DSP_Processor;
using System;
using Test_Project_1.TestHelpers;
#endregion

/// <summary>
/// CHARACTERIZATION / GUARD SUITE for DSP\Filters\SmartGain.cs.
///
/// SmartGain tracks a running PeakLevelLinear across blocks and derives the applied gain from it,
/// so its behavior only makes sense over a sequence. Six published properties are pinned after
/// every block: PeakLevelLinear, InputAbs, HeadroomLinear, ActualGainLinear, ActualGaindB and
/// MaxAllowedLinearGain.
///
/// WALL-CLOCK NOTE: SmartGain decays its peak once per block when DateTime.UtcNow has advanced past
/// Duration. Every test here sets Duration to one hour so that branch is deterministic regardless
/// of how long the suite takes - it is taken exactly once, on the first block, because the initial
/// StartPeakDuration is DateTime.MinValue.
/// </summary>
[TestClass]
public class Test_SmartGain_Characterization
{
    #region Golden Inputs

    private static double[][] BuildInputs()
    {
        return new[]
        {
            DspCharacterization.Sine(8, 1, 0.1),
            DspCharacterization.Sine(8, 1, 0.5),
            DspCharacterization.Constant(8, 1.0),
            DspCharacterization.Alternating(8, 1.0),
            DspCharacterization.Constant(8, 0.0),
            DspCharacterization.Noise(8, 15000UL),
            DspCharacterization.Constant(8, -1.0),
            DspCharacterization.Sine(8, 1, 0.25),
        };
    }

    #endregion

    #region Multi-Block Stateful Sequence

    /// <summary>
    /// Eight consecutive blocks at +6 dB requested gain, over signals that force the clip-avoidance
    /// path (blocks 2, 3, 6 are full scale) and the soft-clip path.
    /// </summary>
    [TestMethod]
    public void Stateful_Transform_EightBlocksOfAudio()
    {
        double[][] Local_Expected =
        {
            new[] { 0.0d, 0.1410863513160464d, 0.19952623149688797d, 0.1410863513160464d, 2.4434916074859767E-17d, -0.14108635131604638d, -0.19952623149688797d, -0.1410863513160464d },
            new[] { 0.0d, 0.705431756580232d, 0.9976311574844399d, 0.705431756580232d, 1.2217458037429883E-16d, -0.7054317565802319d, -0.9976311574844399d, -0.7054317565802322d },
            new[] { 0.707d, 0.707d, 0.707d, 0.707d, 0.707d, 0.707d, 0.707d, 0.707d },
            new[] { 0.707d, -0.707d, 0.707d, -0.707d, 0.707d, -0.707d, 0.707d, -0.707d },
            new[] { 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d },
            new[] { -0.8281028576621225d, 0.4638751284209126d, -0.7072132088364191d, -0.7819632038257305d, 0.4258306092761357d, -0.32879690045364596d, -0.09145588549854855d, 0.9088347956623875d },
            new[] { -0.707d, -0.707d, -0.707d, -0.707d, -0.707d, -0.707d, -0.707d, -0.707d },
            new[] { 0.0d, 0.1767766952966369d, 0.25d, 0.1767766952966369d, 3.061616997868383E-17d, -0.17677669529663687d, -0.25d, -0.17677669529663692d },
        };

        var Local_Stream = new DSP_Stream();
        var Local_Gain = new SmartGain { GaindB = 6.0, Duration = TimeSpan.FromHours(1) };
        var Local_Inputs = BuildInputs();

        for (int Local_Block = 0; Local_Block < Local_Inputs.Length; Local_Block++)
        {
            var Local_Result = Local_Gain.Transform(DspCharacterization.Copy(Local_Inputs[Local_Block]), Local_Stream);
            DspCharacterization.AssertExact(Local_Expected[Local_Block], Local_Result, "Block " + Local_Block);
        }
    }

    /// <summary>
    /// All six published state properties after each of the same eight blocks.
    ///
    /// The values worth noticing: PeakLevelLinear ratchets up to 1.0 and never comes back down
    /// (with Duration set long, the decay branch is not re-entered), so once a full-scale block has
    /// been seen the filter is permanently pinned to unity gain; and HeadroomLinear becomes
    /// +Infinity on the silent block because it is 1/|result|.
    /// </summary>
    [TestMethod]
    public void Stateful_Transform_EightBlocksOfPublishedState()
    {
        (double Peak, double InputAbs, double Headroom, double ActualGainLinear, double ActualGaindB, double MaxAllowed)[] Local_Expected =
        {
            (0.1d, 0.1410863513160464d, 7.087857830839413d, 1.9952623149688797d, 6.0d, 10.0d),
            (0.5d, 0.7054317565802322d, 1.4175715661678823d, 1.9952623149688797d, 6.0d, 2.0d),
            (1.0d, 0.707d, 1.0d, 1.0d, 0.0d, 1.0d),
            (1.0d, 0.707d, 1.0d, 1.0d, 0.0d, 1.0d),
            (1.0d, 0.0d, double.PositiveInfinity, 1.0d, 0.0d, 1.0d),
            (1.0d, 0.9088347956623875d, 1.1003099845788458d, 1.0d, 0.0d, 1.0d),
            (1.0d, 0.707d, 1.0d, 1.0d, 0.0d, 1.0d),
            (1.0d, 0.17677669529663692d, 5.656854249492379d, 1.0d, 0.0d, 1.0d),
        };

        var Local_Stream = new DSP_Stream();
        var Local_Gain = new SmartGain { GaindB = 6.0, Duration = TimeSpan.FromHours(1) };
        var Local_Inputs = BuildInputs();

        for (int Local_Block = 0; Local_Block < Local_Inputs.Length; Local_Block++)
        {
            Local_Gain.Transform(DspCharacterization.Copy(Local_Inputs[Local_Block]), Local_Stream);

            DspCharacterization.AssertExact(Local_Expected[Local_Block].Peak, Local_Gain.PeakLevelLinear, "Block " + Local_Block + " PeakLevelLinear");
            DspCharacterization.AssertExact(Local_Expected[Local_Block].InputAbs, Local_Gain.InputAbs, "Block " + Local_Block + " InputAbs");
            DspCharacterization.AssertExact(Local_Expected[Local_Block].Headroom, Local_Gain.HeadroomLinear, "Block " + Local_Block + " HeadroomLinear");
            DspCharacterization.AssertExact(Local_Expected[Local_Block].ActualGainLinear, Local_Gain.ActualGainLinear, "Block " + Local_Block + " ActualGainLinear");
            DspCharacterization.AssertExact(Local_Expected[Local_Block].ActualGaindB, Local_Gain.ActualGaindB, "Block " + Local_Block + " ActualGaindB");
            DspCharacterization.AssertExact(Local_Expected[Local_Block].MaxAllowed, Local_Gain.MaxAllowedLinearGain, "Block " + Local_Block + " MaxAllowedLinearGain");
        }
    }

    /// <summary>
    /// PeakHold true at 0 dB across three blocks with a block-size change in the middle. In this
    /// mode the decay branch is never entered at all, so the peak is a pure running maximum.
    /// </summary>
    [TestMethod]
    public void Stateful_Transform_PeakHoldWithBlockSizeChange()
    {
        double[][] Local_Expected =
        {
            new[] { 0.0d, 0.3535533905932738d, 0.5d, 0.3535533905932738d, 6.123233995736766E-17d, -0.35355339059327373d, -0.5d, -0.35355339059327384d },
            new[] { 0.0d, 0.25d, 3.061616997868383E-17d, -0.25d },
            new[] { 0.0d, 0.3535533905932738d, 0.5d, 0.3535533905932738d, 6.123233995736766E-17d, -0.35355339059327373d, -0.5d, -0.35355339059327384d },
        };

        var Local_Stream = new DSP_Stream();
        var Local_Gain = new SmartGain { GaindB = 0.0, PeakHold = true, Duration = TimeSpan.FromHours(1) };

        DspCharacterization.AssertExact(Local_Expected[0],
            Local_Gain.Transform(DspCharacterization.Sine(8, 1, 0.5), Local_Stream), "Block 0");
        DspCharacterization.AssertExact(Local_Expected[1],
            Local_Gain.Transform(DspCharacterization.Sine(4, 1, 0.25), Local_Stream), "Block 1 (size drops to 4)");
        DspCharacterization.AssertExact(Local_Expected[2],
            Local_Gain.Transform(DspCharacterization.Sine(8, 1, 0.5), Local_Stream), "Block 2 (size back to 8)");

        DspCharacterization.AssertExact(0.5d, Local_Gain.PeakLevelLinear, "PeakLevelLinear");
        DspCharacterization.AssertExact(1.0d, Local_Gain.ActualGainLinear, "ActualGainLinear");
        DspCharacterization.AssertExact(0.0d, Local_Gain.ActualGaindB, "ActualGaindB");
        DspCharacterization.AssertExact(2.0d, Local_Gain.MaxAllowedLinearGain, "MaxAllowedLinearGain");
        DspCharacterization.AssertExact(2.8284271247461894d, Local_Gain.HeadroomLinear, "HeadroomLinear");
        DspCharacterization.AssertExact(0.35355339059327384d, Local_Gain.InputAbs, "InputAbs");
    }

    #endregion

    #region Edge Cases

    /// <summary>
    /// Silence leaves MaxAllowedLinearGain and HeadroomLinear at +Infinity (both are reciprocals of
    /// zero). Pinned because a "defensive" guard added during optimization would change the numbers
    /// the GUI shows.
    /// </summary>
    [TestMethod]
    public void Golden_Transform_SilenceProducesInfiniteHeadroomAndMaxGain()
    {
        var Local_Gain = new SmartGain { GaindB = 0.0, PeakHold = true, Duration = TimeSpan.FromHours(1) };
        var Local_Result = Local_Gain.Transform(new double[8], new DSP_Stream());

        DspCharacterization.AssertExact(new double[8], Local_Result, "Silence in, silence out");
        Assert.IsTrue(double.IsPositiveInfinity(Local_Gain.MaxAllowedLinearGain), "MaxAllowedLinearGain");
        Assert.IsTrue(double.IsPositiveInfinity(Local_Gain.HeadroomLinear), "HeadroomLinear");
        DspCharacterization.AssertExact(1.0d, Local_Gain.ActualGainLinear, "ActualGainLinear");
    }

    /// <summary>
    /// The soft-clip guard replaces any result whose magnitude reaches 0.999 with
    /// sign(result) * 0.707 - a very audible 3 dB drop rather than a gentle limit. Pinned as-is.
    /// </summary>
    [TestMethod]
    public void Golden_Transform_SoftClipSubstitutesPlusOrMinus0Point707()
    {
        var Local_Gain = new SmartGain { GaindB = 0.0, PeakHold = true, Duration = TimeSpan.FromHours(1) };
        var Local_Input = new double[] { 1.0, -1.0, 0.9989, -0.9989, 0.99899999, 0.5, -0.5, 0.0 };
        var Local_Result = Local_Gain.Transform(Local_Input, new DSP_Stream());

        DspCharacterization.AssertExact(0.707d, Local_Result[0], "+1.0 becomes +0.707");
        DspCharacterization.AssertExact(-0.707d, Local_Result[1], "-1.0 becomes -0.707");
        DspCharacterization.AssertExact(0.9989d, Local_Result[2], "Just under the 0.999 threshold is untouched");
        DspCharacterization.AssertExact(-0.9989d, Local_Result[3], "Same on the negative side");
        DspCharacterization.AssertExact(0.99899999d, Local_Result[4], "The threshold is >= 0.999, so 0.99899999 survives");
        DspCharacterization.AssertExact(0.5d, Local_Result[5], "Ordinary samples are untouched at 0 dB");
        DspCharacterization.AssertExact(-0.5d, Local_Result[6], "Ordinary negative samples are untouched too");
        DspCharacterization.AssertExact(0.0d, Local_Result[7], "Zero stays zero");
    }

    /// <summary>
    /// The GaindB setter recomputes the linear request; setting it is the only supported way to
    /// change the gain.
    /// </summary>
    [TestMethod]
    public void Property_GaindB_SetterRecomputesTheLinearRequest()
    {
        var Local_Gain = new SmartGain { PeakHold = true, Duration = TimeSpan.FromHours(1) };

        Local_Gain.GaindB = 0;
        var Local_Unity = Local_Gain.Transform(DspCharacterization.Constant(8, 0.25), new DSP_Stream());
        DspCharacterization.AssertExact(0.25d, Local_Unity[0], "0 dB must be exactly unity");

        var Local_Doubled = new SmartGain { PeakHold = true, Duration = TimeSpan.FromHours(1) };
        Local_Doubled.GaindB = 6.0;
        var Local_Result = Local_Doubled.Transform(DspCharacterization.Constant(8, 0.25), new DSP_Stream());
        DspCharacterization.AssertExact(0.49881557874221993d, Local_Result[0], "+6 dB on a 0.25 block");
        DspCharacterization.AssertExact(6.0d, Local_Doubled.ActualGaindB, "ActualGaindB tracks the request when it is achievable");
    }

    #endregion

    #region Aliasing / Mutation Contract

    /// <summary>
    /// Transform mutates the caller's array in place and returns that same instance; an empty block
    /// short-circuits before InputAbs is written (which would otherwise index [-1]).
    /// </summary>
    [TestMethod]
    public void Contract_Transform_MutatesInPlaceAndReturnsTheInputInstance()
    {
        var Local_Gain = new SmartGain { GaindB = 6.0, Duration = TimeSpan.FromHours(1) };

        var Local_Input = DspCharacterization.Constant(8, 0.25);
        var Local_Result = Local_Gain.Transform(Local_Input, new DSP_Stream());
        Assert.IsTrue(ReferenceEquals(Local_Input, Local_Result));
        Assert.AreNotEqual(0.25d, Local_Input[0], "The caller's array was written in place");

        var Local_Empty = new double[0];
        var Local_EmptyResult = Local_Gain.Transform(Local_Empty, new DSP_Stream());
        Assert.IsTrue(ReferenceEquals(Local_Empty, Local_EmptyResult));
        Assert.AreEqual(0, Local_EmptyResult.Length);
    }

    #endregion
}
