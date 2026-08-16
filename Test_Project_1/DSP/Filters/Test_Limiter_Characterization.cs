#nullable enable

namespace Test_Project_1;

#region Usings
using BassThatHz_ASIO_DSP_Processor;
using System;
using Test_Project_1.TestHelpers;
#endregion

/// <summary>
/// CHARACTERIZATION / GUARD SUITE for DSP\Filters\Limiter.cs.
///
/// Limiter is the most state-heavy filter in the chain: PeakValue survives across blocks and DECAYS
/// exponentially, Gain_Linear is smoothed by the attack/release coefficients, and IsBrickwall /
/// CompressionApplied are published to the GUI. A single-block test would miss all of it.
///
/// The sequence below drives EIGHT consecutive blocks that deliberately exercise the nonlinearity:
/// well below threshold, moderately above, full-scale DC, full-scale alternating, silence, back to
/// moderate, deterministic noise near full scale, and full-scale negative DC. Every block's audio
/// AND all three published state fields are pinned.
/// </summary>
[TestClass]
public class Test_Limiter_Characterization
{
    #region Golden Inputs

    private static double[][] BuildInputs()
    {
        return new[]
        {
            DspCharacterization.Sine(8, 1, 0.05),
            DspCharacterization.Sine(8, 1, 0.5),
            DspCharacterization.Constant(8, 1.0),
            DspCharacterization.Alternating(8, 1.0),
            DspCharacterization.Constant(8, 0.0),
            DspCharacterization.Sine(8, 1, 0.5),
            DspCharacterization.Noise(8, 11000UL),
            DspCharacterization.Constant(8, -1.0),
        };
    }

    #endregion

    #region Multi-Block Stateful Sequence

    /// <summary>
    /// Eight consecutive blocks. Each block's OUTPUT is pinned bit-exactly.
    /// </summary>
    [TestMethod]
    public void Stateful_Transform_EightBlocksOfAudio()
    {
        double[][] Local_Expected =
        {
            new[] { 0.0d, 0.03535533905932738d, 0.05d, 0.03535533905932738d, 6.123233995736766E-18d, -0.035355339059327376d, -0.05d, -0.03535533905932738d },
            new[] { 0.0d, 0.34581639975469114d, 0.48905824262412007d, 0.34581639975469114d, 5.989236114262583E-17d, -0.3458163997546911d, -0.48905824262412007d, -0.3458163997546912d },
            new[] { 0.9885530946569389d, 0.9885530946569389d, 0.9885530946569389d, 0.9885530946569389d, 0.9885530946569389d, 0.9885530946569389d, 0.9885530946569389d, 0.9885530946569389d },
            new[] { 0.9885530946569389d, -0.9885530946569389d, 0.9885530946569389d, -0.9885530946569389d, 0.9885530946569389d, -0.9885530946569389d, 0.9885530946569389d, -0.9885530946569389d },
            new[] { 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d },
            new[] { 0.0d, 0.3530200718249091d, 0.49924577336471054d, 0.3530200718249091d, 6.113997383389377E-17d, -0.353020071824909d, -0.49924577336471054d, -0.35302007182490913d },
            new[] { -0.8261876234079492d, 0.7886458445608802d, -0.9885530946569389d, 0.040247897293679176d, -0.14870276802801d, 0.4546816199254675d, -0.6865205411479235d, -0.6059002411002438d },
            new[] { -0.9885530946569389d, -0.9885530946569389d, -0.9885530946569389d, -0.9885530946569389d, -0.9885530946569389d, -0.9885530946569389d, -0.9885530946569389d, -0.9885530946569389d },
        };

        var Local_Stream = new DSP_Stream();
        var Local_Limiter = new Limiter();
        Local_Limiter.ResetSampleRate(48000);
        var Local_Inputs = BuildInputs();

        for (int Local_Block = 0; Local_Block < Local_Inputs.Length; Local_Block++)
        {
            var Local_Result = Local_Limiter.Transform(DspCharacterization.Copy(Local_Inputs[Local_Block]), Local_Stream);
            DspCharacterization.AssertExact(Local_Expected[Local_Block], Local_Result, "Block " + Local_Block);
        }
    }

    /// <summary>
    /// The same eight blocks, but pinning the PUBLISHED STATE after each one:
    /// CompressionApplied, PeakValue and IsBrickwall.
    ///
    /// PeakValue is the interesting one - it is a running maximum that decays by exp(-1/decayFactor)
    /// once per over-limit block, which is why blocks 4 and 5 show it creeping down from 1.0 rather
    /// than snapping back.
    /// </summary>
    [TestMethod]
    public void Stateful_Transform_EightBlocksOfPublishedState()
    {
        (double CompressionApplied, double PeakValue, bool IsBrickwall)[] Local_Expected =
        {
            (1.0d, 0.05d, false),
            (0.9781164852482401d, 0.5d, false),
            (0.9885530946569389d, 0.9999966666722222d, true),
            (0.9885530946569389d, 0.9999966666722222d, true),
            (0.9885563898394131d, 0.9900465335885559d, false),
            (0.9984915467294211d, 0.9900432334389442d, false),
            (0.9927385654139922d, 0.9957805951390668d, true),
            (0.9885530946569389d, 0.9999966666722222d, true),
        };

        var Local_Stream = new DSP_Stream();
        var Local_Limiter = new Limiter();
        Local_Limiter.ResetSampleRate(48000);
        var Local_Inputs = BuildInputs();

        for (int Local_Block = 0; Local_Block < Local_Inputs.Length; Local_Block++)
        {
            Local_Limiter.Transform(DspCharacterization.Copy(Local_Inputs[Local_Block]), Local_Stream);
            DspCharacterization.AssertExact(Local_Expected[Local_Block].CompressionApplied, Local_Limiter.CompressionApplied,
                "Block " + Local_Block + " CompressionApplied");
            DspCharacterization.AssertExact(Local_Expected[Local_Block].PeakValue, Local_Limiter.PeakValue,
                "Block " + Local_Block + " PeakValue");
            Assert.AreEqual(Local_Expected[Local_Block].IsBrickwall, Local_Limiter.IsBrickwall,
                "Block " + Local_Block + " IsBrickwall");
        }
    }

    /// <summary>
    /// ApplySettings zeroes PeakValue mid-sequence but leaves Gain_Linear alone, so the three
    /// following blocks continue to release from the gain the limiter had already reached.
    /// </summary>
    [TestMethod]
    public void Stateful_ApplySettings_MidSequenceZeroesPeakValueButNotTheSmoothedGain()
    {
        double[][] Local_ExpectedAudio =
        {
            new[] { 0.0d, 0.3419344715884932d, 0.4835683671633248d, 0.3419344715884932d, 5.922004530154777E-17d, -0.34193447158849316d, -0.4835683671633248d, -0.34193447158849327d },
            new[] { 0.0d, 0.33467165514230063d, 0.4732971936440929d, 0.33467165514230063d, 5.796218932416634E-17d, -0.3346716551423006d, -0.4732971936440929d, -0.3346716551423007d },
            new[] { 0.0d, 0.32770523817819386d, 0.4634451922923071d, 0.32770523817819386d, 5.675566713210035E-17d, -0.3277052381781938d, -0.4634451922923071d, -0.3277052381781939d },
        };
        (double CompressionApplied, double PeakValue, bool IsBrickwall)[] Local_ExpectedState =
        {
            (0.9671367343266496d, 0.5d, false),
            (0.9465943872881858d, 0.5d, false),
            (0.9268903845846141d, 0.5d, false),
        };

        var Local_Stream = new DSP_Stream();
        var Local_Limiter = new Limiter();
        Local_Limiter.ResetSampleRate(48000);
        foreach (var Local_Input in BuildInputs())
            Local_Limiter.Transform(DspCharacterization.Copy(Local_Input), Local_Stream);

        Local_Limiter.ApplySettings();
        DspCharacterization.AssertExact(0.0d, Local_Limiter.PeakValue, "ApplySettings zeroes PeakValue");

        for (int Local_Block = 0; Local_Block < Local_ExpectedAudio.Length; Local_Block++)
        {
            var Local_Result = Local_Limiter.Transform(DspCharacterization.Sine(8, 1, 0.5), Local_Stream);
            DspCharacterization.AssertExact(Local_ExpectedAudio[Local_Block], Local_Result, "Post-ApplySettings block " + Local_Block);
            DspCharacterization.AssertExact(Local_ExpectedState[Local_Block].CompressionApplied, Local_Limiter.CompressionApplied,
                "Post-ApplySettings block " + Local_Block + " CompressionApplied");
            DspCharacterization.AssertExact(Local_ExpectedState[Local_Block].PeakValue, Local_Limiter.PeakValue,
                "Post-ApplySettings block " + Local_Block + " PeakValue");
            Assert.AreEqual(Local_ExpectedState[Local_Block].IsBrickwall, Local_Limiter.IsBrickwall,
                "Post-ApplySettings block " + Local_Block + " IsBrickwall");
        }
    }

    /// <summary>
    /// Assigning FilterEnabled - which the GUI does routinely - also zeroes PeakValue as a side
    /// effect, even when the value assigned is the one already there.
    /// </summary>
    [TestMethod]
    public void Stateful_FilterEnabledSetter_ZeroesPeakValue()
    {
        var Local_Limiter = new Limiter();
        Local_Limiter.ResetSampleRate(48000);
        Local_Limiter.Transform(DspCharacterization.Constant(8, 1.0), new DSP_Stream());
        Assert.IsTrue(Local_Limiter.PeakValue > 0.9d);

        Local_Limiter.FilterEnabled = false;
        DspCharacterization.AssertExact(0.0d, Local_Limiter.PeakValue, "Setting FilterEnabled must zero PeakValue");
    }

    #endregion

    #region Without CalculateCoeffs

    /// <summary>
    /// Without ResetSampleRate/CalculateCoeffs the attack and release coefficients are their
    /// initial 1.0, which makes the smoother a no-op: the block passes through unchanged and
    /// CompressionApplied stays at 1.0. Pinned so the "unconfigured" path is not accidentally
    /// changed either.
    /// </summary>
    [TestMethod]
    public void Golden_Transform_WithoutCalculateCoeffsIsAPassThrough()
    {
        var Local_Limiter = new Limiter();
        var Local_Result = Local_Limiter.Transform(DspCharacterization.Sine(8, 1, 0.5), new DSP_Stream());

        DspCharacterization.AssertExact(
            new double[]
            {
                0.0d, 0.3535533905932738d, 0.5d, 0.3535533905932738d,
                6.123233995736766E-17d, -0.35355339059327373d, -0.5d, -0.35355339059327384d
            },
            Local_Result, "Unconfigured limiter output");

        DspCharacterization.AssertExact(1.0d, Local_Limiter.CompressionApplied, "CompressionApplied");
        DspCharacterization.AssertExact(0.5d, Local_Limiter.PeakValue, "PeakValue");
        Assert.IsFalse(Local_Limiter.IsBrickwall, "IsBrickwall");
    }

    #endregion

    #region Property Tests

    /// <summary>
    /// Whatever the input, the limiter never lets a sample reach or exceed 1.0 on the positive side
    /// once it is engaged. The clamp is now symmetric, so the same holds on the negative side - see
    /// <see cref="Property_Transform_ClampCeilingIsGenuinelySubUnityAndSymmetric"/>.
    /// </summary>
    [TestMethod]
    public void Property_Transform_PositivePeaksNeverReachUnity()
    {
        var Local_Stream = new DSP_Stream();
        var Local_Limiter = new Limiter();
        Local_Limiter.ResetSampleRate(48000);

        for (int Local_Block = 0; Local_Block < 12; Local_Block++)
        {
            var Local_Result = Local_Limiter.Transform(DspCharacterization.Constant(16, 4.0), Local_Stream);
            for (int i = 0; i < Local_Result.Length; i++)
            {
                Assert.IsTrue(Local_Result[i] < 1.0d,
                    "Block " + Local_Block + " sample " + i + " reached " + Local_Result[i]);
            }
        }
    }

    /// <summary>
    /// FIXED BUG - this test used to be Bug_Transform_ClampConstantCollapsesToExactlyOneAndIsOneSided
    /// and asserted the DEFECT; it now asserts the corrected behaviour.
    ///
    /// Both output clamps in Limiter.Transform used to be written as
    ///
    ///     double limit = 1.0 - double.Epsilon;
    ///     input[i] = v &lt; limit ? v : limit;
    ///
    /// The intent is plainly "the largest double strictly below 1", but double.Epsilon is the
    /// smallest positive SUBNORMAL (about 4.9e-324), which is far below the ULP of 1.0 (about
    /// 2.2e-16), so 1.0 - double.Epsilon rounds straight back to exactly 1.0. The guard constant
    /// was therefore full scale, not "just below full scale", and the clamp was also ONE-SIDED -
    /// `v &lt; limit ? v : limit` is Math.Min, which bounds the value from above only.
    ///
    /// The same defect has already been FIXED in the two sibling filters (Math.Clamp against
    /// +/-Math.BitDecrement(1.0)) - see
    /// Test_ClassicLimiter_Characterization.Property_Transform_NegativePeaksAreBoundedByTheSymmetricClamp
    /// and Property_Transform_ClampCeilingIsGenuinelySubUnity. Limiter now matches them.
    ///
    /// The first assertion below keeps the LANGUAGE FACT on the record - 1.0 - double.Epsilon really
    /// is exactly 1.0 - so nobody reintroduces the constant. The pinned audio value is the DEFAULT
    /// configuration, where MaxValue is -0.1 dBFS and the gain is derived from the same running peak
    /// the samples are measured against; the ceiling is therefore never reached and the value is
    /// bit-identical to the pre-fix golden. The configuration in which the ceiling IS reached is
    /// covered by the regression guards further down.
    /// </summary>
    [TestMethod]
    public void Property_Transform_ClampCeilingIsGenuinelySubUnityAndSymmetric()
    {
        DspCharacterization.AssertExact(1.0d, 1.0d - double.Epsilon,
            "1.0 - double.Epsilon is exactly 1.0 - the intended 'just below unity' guard did not exist");
        Assert.IsTrue(Math.BitDecrement(1.0d) < 1.0d,
            "Math.BitDecrement(1.0) is the constant that was actually meant");

        var Local_Stream = new DSP_Stream();
        var Local_Limiter = new Limiter();
        Local_Limiter.ResetSampleRate(48000);

        //Drive the limiter hard so it is fully engaged, then hand it a very negative block.
        for (int Local_Block = 0; Local_Block < 4; Local_Block++)
            Local_Limiter.Transform(DspCharacterization.Constant(8, 4.0), Local_Stream);

        var Local_Result = Local_Limiter.Transform(DspCharacterization.Constant(8, -4.0), Local_Stream);

        DspCharacterization.AssertExact(-0.988553094656939d, Local_Result[0],
            "At the default -0.1 dBFS MaxValue the ceiling is never reached, so the corrected clamp " +
            "returns this sample bit-for-bit unchanged");
        Assert.IsTrue(Local_Result[0] > -1.0d, "The negative side is bounded");
        Assert.IsTrue(Local_Result[0] < 1.0d, "The positive side is bounded");
    }

    /// <summary>
    /// Silence in, silence out - and it must be exactly +0.0, not a denormal.
    /// </summary>
    [TestMethod]
    public void Property_Transform_SilenceStaysExactlySilent()
    {
        var Local_Stream = new DSP_Stream();
        var Local_Limiter = new Limiter();
        Local_Limiter.ResetSampleRate(48000);

        for (int Local_Block = 0; Local_Block < 6; Local_Block++)
        {
            var Local_Result = Local_Limiter.Transform(new double[8], Local_Stream);
            for (int i = 0; i < Local_Result.Length; i++)
                DspCharacterization.AssertExact(0.0d, Local_Result[i], "Block " + Local_Block + " sample " + i);
        }
    }

    #endregion

    #region Output Clamp Fix (regression guards)

    /// <summary>
    /// Builds a limiter whose ceiling is 0 dBFS.
    /// </summary>
    /// <remarks>
    /// MaxValue = 1.0 is the top of the Limit slider in LimiterControl (BTH_VolumeSliderControl
    /// clamps to MaxDb = 0, and DecibelsToLinear(0) is exactly 1.0), so this is an ordinary,
    /// UI-reachable "limit at full scale" setting - not a contrived one.
    ///
    /// It matters because it is the configuration in which the output clamp actually ENGAGES. At the
    /// factory default MaxValue of -0.1 dBFS the brickwall gain (MaxValue / peak) already lands every
    /// sample at 0.98855, comfortably inside the ceiling, so the clamp is never exercised - which is
    /// exactly why the broken constant survived unnoticed. With the ceiling at 1.0 the gain lands the
    /// sample AT full scale, and floating point rounding of the dB round trip
    /// (Exp((Log(1) - Log(8)) * ln10/20) is 0.12500000000000003, not 0.125) pushes it a single ULP
    /// PAST it, to +/-1.0000000000000002.
    /// </remarks>
    private static Limiter BuildFullScaleCeilingLimiter()
    {
        var Local_Limiter = new Limiter();
        Local_Limiter.MaxValue = 1.0d;
        Local_Limiter.ResetSampleRate(48000);
        return Local_Limiter;
    }

    /// <summary>
    /// REGRESSION GUARD for the output clamp fix, negative half.
    ///
    /// A grossly over-threshold block of NEGATIVE samples must leave the limiter inside +/-1.0. This
    /// is the assertion that could not be made while the clamp was `v &lt; limit ? v : limit`, i.e.
    /// Math.Min: Math.Min bounds only from above, so -1.0000000000000002 sailed straight through and
    /// the limiter emitted audio OUTSIDE full scale.
    ///
    /// Justification for the exact expected value, from intended behaviour rather than from observed
    /// output: a limiter must not emit at or beyond full scale, so a sample driven past the ceiling
    /// has exactly one correct destination - the largest representable double strictly below 1.0,
    /// with the sign of the input. Every sample of the block is over the ceiling (see
    /// <see cref="BuildFullScaleCeilingLimiter"/>), so the whole block must pin to it.
    ///
    /// FAILS against the pre-fix code at every length.
    /// </summary>
    [TestMethod]
    public void Property_Transform_GrosslyOverThresholdNegativeBlockIsBoundedWithinFullScale()
    {
        double Local_Ceiling = Math.BitDecrement(1.0d);

        foreach (int Local_Length in new[] { 1, 8, 64, 256 })
        {
            var Local_Limiter = BuildFullScaleCeilingLimiter();
            var Local_Result = Local_Limiter.Transform(
                DspCharacterization.Constant(Local_Length, -8.0), new DSP_Stream());

            for (int i = 0; i < Local_Result.Length; i++)
            {
                Assert.IsTrue(Local_Result[i] >= -1.0d && Local_Result[i] <= 1.0d,
                    "Length " + Local_Length + " sample " + i + " left the limiter outside +/-1.0 at " +
                    Local_Result[i]);
                DspCharacterization.AssertExact(-Local_Ceiling, Local_Result[i],
                    "Length " + Local_Length + " sample " + i + " must be pinned to the negative ceiling");
            }
        }
    }

    /// <summary>
    /// REGRESSION GUARD for the output clamp fix, both halves at once.
    ///
    /// A symmetric +/-8.0 block (alternating at the Nyquist rate) must be bounded on BOTH signs.
    /// Before the fix the positive samples were pinned at exactly 1.0 and the negative ones were not
    /// bounded at all, which is an asymmetric, DC-offsetting distortion as well as an out-of-range
    /// one. The corrected clamp is symmetric, so the block must come out as a clean +/-ceiling
    /// square wave with no DC offset at all.
    /// </summary>
    [TestMethod]
    public void Property_Transform_SymmetricHotBlockIsBoundedOnBothSigns()
    {
        double Local_Ceiling = Math.BitDecrement(1.0d);

        var Local_Limiter = BuildFullScaleCeilingLimiter();
        var Local_Result = Local_Limiter.Transform(
            DspCharacterization.Alternating(64, 8.0), new DSP_Stream());

        double Local_Sum = 0.0d;
        for (int i = 0; i < Local_Result.Length; i++)
        {
            Assert.IsTrue(Math.Abs(Local_Result[i]) <= 1.0d,
                "Sample " + i + " left the limiter outside +/-1.0 at " + Local_Result[i]);
            DspCharacterization.AssertExact(
                i % 2 == 0 ? Local_Ceiling : -Local_Ceiling, Local_Result[i],
                "Sample " + i + " must be pinned to the ceiling of its own sign");
            Local_Sum += Local_Result[i];
        }

        DspCharacterization.AssertExact(0.0d, Local_Sum,
            "A symmetric input clamped symmetrically must sum to exactly zero - no DC offset");
    }

    /// <summary>
    /// REGRESSION GUARD proving the ceiling is genuinely SUB-UNITY, which is the half of the defect
    /// that the symmetry fix alone would not have caught.
    ///
    /// With the old constant the positive side was bounded at exactly 1.0, so a hot block left the
    /// limiter AT 0 dBFS - the first value an output converter can wrap on, and precisely what the
    /// constant was written to avoid. The expected value is derived from intent, not from output:
    /// "strictly below full scale" has exactly one best representation, Math.BitDecrement(1.0).
    /// </summary>
    [TestMethod]
    public void Property_Transform_OutputMagnitudeIsStrictlyBelowFullScale()
    {
        double Local_Ceiling = Math.BitDecrement(1.0d);

        foreach (double Local_Amplitude in new[] { 8.0d, -8.0d, 4.0d, -4.0d, 2.0d, -2.0d })
        {
            var Local_Limiter = BuildFullScaleCeilingLimiter();
            var Local_Result = Local_Limiter.Transform(
                DspCharacterization.Constant(16, Local_Amplitude), new DSP_Stream());

            for (int i = 0; i < Local_Result.Length; i++)
            {
                Assert.IsTrue(Math.Abs(Local_Result[i]) < 1.0d,
                    "Amplitude " + Local_Amplitude + " sample " + i +
                    " reached or exceeded full scale at " + Local_Result[i]);
                DspCharacterization.AssertExact(Math.CopySign(Local_Ceiling, Local_Amplitude), Local_Result[i],
                    "Amplitude " + Local_Amplitude + " sample " + i + " must sit on the sub-unity ceiling");
            }
        }
    }

    /// <summary>
    /// REGRESSION GUARD for the multi-block case. Limiter carries PeakValue (which DECAYS by
    /// exp(-1/decayFactor) once per over-limit block) and IsBrickwall across calls, so a bound that
    /// only held for the first block would be worthless. Twelve consecutive hot blocks of
    /// alternating sign must each be bounded.
    /// </summary>
    [TestMethod]
    public void Property_Transform_BoundHoldsAcrossConsecutiveBlocks()
    {
        double Local_Ceiling = Math.BitDecrement(1.0d);

        var Local_Stream = new DSP_Stream();
        var Local_Limiter = BuildFullScaleCeilingLimiter();

        for (int Local_Block = 0; Local_Block < 12; Local_Block++)
        {
            double Local_Amplitude = Local_Block % 2 == 0 ? 8.0d : -8.0d;
            var Local_Result = Local_Limiter.Transform(
                DspCharacterization.Constant(32, Local_Amplitude), Local_Stream);

            for (int i = 0; i < Local_Result.Length; i++)
            {
                Assert.IsTrue(Math.Abs(Local_Result[i]) < 1.0d,
                    "Block " + Local_Block + " sample " + i + " left the limiter at " + Local_Result[i]);
                DspCharacterization.AssertExact(Math.CopySign(Local_Ceiling, Local_Amplitude), Local_Result[i],
                    "Block " + Local_Block + " sample " + i + " must stay pinned to the ceiling");
            }

            Assert.IsTrue(Local_Limiter.IsBrickwall, "Block " + Local_Block + " must still be brickwalling");
        }
    }

    /// <summary>
    /// NO-REGRESSION GUARD: the clamp must be invisible to audio that never approaches it.
    ///
    /// Math.Clamp returns anything inside the ceiling completely untouched, so a below-threshold
    /// block must still pass through BIT-EXACTLY - not merely "close". This is the assertion that
    /// makes the golden vectors above trustworthy: the fix moved no pinned value because no pinned
    /// vector ever reached the ceiling.
    /// </summary>
    [TestMethod]
    public void Property_Transform_BelowThresholdAudioPassesThroughBitExactly()
    {
        foreach (double Local_MaxValue in new[] { 0.98855309465693886d, 1.0d })
        {
            var Local_Stream = new DSP_Stream();
            var Local_Limiter = new Limiter();
            Local_Limiter.MaxValue = Local_MaxValue;
            Local_Limiter.ResetSampleRate(48000);

            for (int Local_Block = 0; Local_Block < 4; Local_Block++)
            {
                var Local_Input = DspCharacterization.Sine(8, 1, 0.05);
                var Local_Result = Local_Limiter.Transform(
                    DspCharacterization.Copy(Local_Input), Local_Stream);

                DspCharacterization.AssertExact(Local_Input, Local_Result,
                    "MaxValue " + Local_MaxValue + " block " + Local_Block +
                    " - below-threshold audio must be untouched");
            }

            DspCharacterization.AssertExact(1.0d, Local_Limiter.CompressionApplied,
                "MaxValue " + Local_MaxValue + " - no compression was applied");
        }
    }

    #endregion

    #region Aliasing / Mutation Contract

    /// <summary>
    /// Transform mutates the caller's array in place and returns that same instance; an empty block
    /// short-circuits without touching any state.
    /// </summary>
    [TestMethod]
    public void Contract_Transform_MutatesInPlaceAndReturnsTheInputInstance()
    {
        var Local_Limiter = new Limiter();
        Local_Limiter.ResetSampleRate(48000);

        var Local_Input = DspCharacterization.Constant(8, 1.0);
        var Local_Result = Local_Limiter.Transform(Local_Input, new DSP_Stream());
        Assert.IsTrue(ReferenceEquals(Local_Input, Local_Result));
        Assert.AreNotEqual(1.0d, Local_Input[0], "The caller's array was written in place");

        var Local_Empty = new double[0];
        var Local_EmptyResult = Local_Limiter.Transform(Local_Empty, new DSP_Stream());
        Assert.IsTrue(ReferenceEquals(Local_Empty, Local_EmptyResult));
    }

    #endregion
}
