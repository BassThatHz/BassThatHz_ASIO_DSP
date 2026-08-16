#nullable enable

namespace Test_Project_1;

#region Usings
using BassThatHz_ASIO_DSP_Processor;
using System;
using Test_Project_1.TestHelpers;
#endregion

/// <summary>
/// CHARACTERIZATION / GUARD SUITE for DSP\Filters\ClassicLimiter.cs.
///
/// ClassicLimiter carries Gain_Linear across blocks (smoothed by the attack/release coefficients)
/// and publishes CompressionApplied. It also builds a per-block suffix-maximum "look-ahead" array
/// out of an ArrayPool rental, which is exactly the kind of code an optimizer will want to rewrite -
/// so the golden sequence below drives eight consecutive blocks over signals that exercise the soft
/// knee, the above-threshold branch and the look-ahead branch, and pins every one.
/// </summary>
[TestClass]
public class Test_ClassicLimiter_Characterization
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
            DspCharacterization.Noise(8, 12000UL),
            DspCharacterization.Constant(8, -1.0),
            DspCharacterization.Sine(8, 1, 0.5),
        };
    }

    #endregion

    #region Multi-Block Stateful Sequence

    /// <summary>
    /// Eight consecutive blocks of audio, pinned bit-exactly.
    ///
    /// REGENERATED for the look-ahead unit fix (Math.Max of an amplitude difference -> Math.Min of the
    /// multiplier thresholdLinear / peak). Blocks 1, 2, 3, 5, 6 and 7 contain samples above the -8 dB
    /// top of the knee, so they take the above-threshold branch and its look-ahead correction; every
    /// one of their samples is now SMALLER in magnitude than it was before the fix, because the
    /// look-ahead target moved from "peak - threshold" (0.9 for a full-scale peak, i.e. barely any
    /// reduction, and an outright boost above 1.1) to "threshold / peak" (0.1 for a full-scale peak,
    /// the multiplier that actually lands the peak on the threshold).
    ///
    /// Blocks 0 (a 0.05 sine, entirely inside the soft knee) and 4 (silence) are BIT-IDENTICAL to the
    /// pre-fix golden, which is the evidence that the change touched only the look-ahead branch.
    /// </summary>
    [TestMethod]
    public void Stateful_Transform_EightBlocksOfAudio()
    {
        double[][] Local_Expected =
        {
            new[] { 0.0d, 0.03535533905932738d, 0.05d, 0.03535533905932738d, 6.123233995736766E-18d, -0.035355339059327376d, -0.05d, -0.03535533905932738d },
            new[] { 0.0d, 0.3535533905932738d, 0.49991583377200965d, 0.3534951031421018d, 6.123233995736766E-17d, -0.35349630490220063d, -0.49983511940061154d, -0.35343920619751873d },
            new[] { 0.9994877317011295d, 0.9992984654773773d, 0.9991092390781855d, 0.9989200524951743d, 0.9987309057199659d, 0.998541798744184d, 0.9983527315594541d, 0.9981637041574037d },
            new[] { 0.9979747165296619d, -0.9977857686678594d, 0.997596860563629d, -0.997407992208605d, 0.9972191635944235d, -0.9970303747127226d, 0.9968416255551418d, -0.9966529161133225d },
            new[] { 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d },
            new[] { -0.8282606675666211d, 0.21104643403464943d, -0.9202702288020503d, -0.6625318945791193d, -0.5038489832294d, -0.2377527228898321d, 0.9545212084560186d, 0.26836976141799634d },
            new[] { -0.9957669317791565d, -0.9955784484696407d, -0.9953900048199479d, -0.995201600821733d, -0.9950132364666529d, -0.9948249117463658d, -0.9946366266525322d, -0.9944483811768139d },
            new[] { 0.0d, 0.35163106545878425d, 0.4971978275267214d, 0.35161280830615393d, 6.123233995736766E-17d, -0.3516528188798615d, -0.497228585036616d, -0.3516341087362547d },
        };

        var Local_Stream = new DSP_Stream();
        var Local_Limiter = new ClassicLimiter();
        Local_Limiter.ResetSampleRate(48000);
        var Local_Inputs = BuildInputs();

        for (int Local_Block = 0; Local_Block < Local_Inputs.Length; Local_Block++)
        {
            var Local_Result = Local_Limiter.Transform(DspCharacterization.Copy(Local_Inputs[Local_Block]), Local_Stream);
            DspCharacterization.AssertExact(Local_Expected[Local_Block], Local_Result, "Block " + Local_Block);
        }
    }

    /// <summary>
    /// The same eight blocks, pinning CompressionApplied after each. Note block 4 (silence) leaves
    /// the value UNCHANGED rather than releasing it: the field is only written when the threshold is
    /// exceeded, so silence freezes the published figure.
    ///
    /// REGENERATED for the look-ahead unit fix. Block 0 (soft knee only) is unchanged at exactly 1.0;
    /// every later figure is now SMALLER than the pre-fix golden, i.e. more gain reduction, which is
    /// the whole point of the fix. Crucially every figure is &lt;= 1.0 - before the fix the look-ahead
    /// could drive this above unity, which is a reported "gain reduction" that is really a boost.
    /// </summary>
    [TestMethod]
    public void Stateful_Transform_EightBlocksOfCompressionApplied()
    {
        double[] Local_Expected =
        {
            1.0d,
            0.9996770377578235d,
            0.9981637041574037d,
            0.9966529161133225d,
            0.9966529161133225d,
            0.9959554547568421d,
            0.9944483811768139d,
            0.9945714511355739d,
        };

        var Local_Stream = new DSP_Stream();
        var Local_Limiter = new ClassicLimiter();
        Local_Limiter.ResetSampleRate(48000);
        var Local_Inputs = BuildInputs();

        for (int Local_Block = 0; Local_Block < Local_Inputs.Length; Local_Block++)
        {
            Local_Limiter.Transform(DspCharacterization.Copy(Local_Inputs[Local_Block]), Local_Stream);
            DspCharacterization.AssertExact(Local_Expected[Local_Block], Local_Limiter.CompressionApplied,
                "Block " + Local_Block + " CompressionApplied");
        }
    }

    /// <summary>
    /// Hard knee (UseSoftKnee false, KneeWidth_dB 0) across three blocks with a block-size change in
    /// the middle. This routes every sample through the above-threshold branch and its look-ahead
    /// suffix-maximum, which is the code path most likely to be restructured.
    ///
    /// REGENERATED for the look-ahead unit fix - this test is aimed squarely at the fixed branch, so
    /// it is expected to move. Sanity check on block 0: the look-ahead peak is 0.5, so the look-ahead
    /// target multiplier is now threshold / peak = 0.1 / 0.5 = 0.2 instead of the old
    /// peak - threshold = 0.4. A smaller target means the 99 ms attack ramp walks the gain DOWN twice
    /// as fast, so every sample here is strictly smaller in magnitude than the pre-fix golden while
    /// still tracking the same 0.5-amplitude sine.
    /// </summary>
    [TestMethod]
    public void Stateful_Transform_HardKneeWithBlockSizeChange()
    {
        double[][] Local_Expected =
        {
            new[] { 0.0d, 0.35349387608271493d, 0.49983168525390415d, 0.3533748846273012d, 6.123233995736766E-17d, -0.3533154076771769d, -0.4995793459216363d, -0.35320265424828207d },
            new[] { 0.0d, 0.8989390264365084d, 1.1021821192326179E-16d, -0.8987709172258848d },
            new[] { 0.0d, 0.35301114836406655d, 0.49914914881377775d, 0.3528923600341363d, 6.123233995736766E-17d, -0.3528329846146993d, -0.49889724023974114d, -0.3527204341830927d },
        };

        var Local_Stream = new DSP_Stream();
        var Local_Limiter = new ClassicLimiter { UseSoftKnee = false, KneeWidth_dB = 0 };
        Local_Limiter.ResetSampleRate(48000);

        DspCharacterization.AssertExact(Local_Expected[0],
            Local_Limiter.Transform(DspCharacterization.Sine(8, 1, 0.5), Local_Stream), "Block 0");
        DspCharacterization.AssertExact(Local_Expected[1],
            Local_Limiter.Transform(DspCharacterization.Sine(4, 1, 0.9), Local_Stream), "Block 1 (size drops to 4)");
        DspCharacterization.AssertExact(Local_Expected[2],
            Local_Limiter.Transform(DspCharacterization.Sine(8, 1, 0.5), Local_Stream), "Block 2 (size back to 8)");

        DspCharacterization.AssertExact(0.9976440434957123d, Local_Limiter.CompressionApplied, "Final CompressionApplied");
    }

    #endregion

    #region Without CalculateCoeffs

    /// <summary>
    /// With no ResetSampleRate the attack and release coefficients are 0, so the smoother snaps
    /// straight to the computed gain reduction instead of easing into it. That makes this the one
    /// golden that can be derived ENTIRELY BY HAND, which is why it is the anchor for the look-ahead
    /// unit fix.
    ///
    /// The block is a 0.5-amplitude sine, so the look-ahead peak is 0.5 and the threshold is -20 dB,
    /// i.e. thresholdLinear = 0.1. The look-ahead multiplier is now threshold / peak = 0.1 / 0.5 = 0.2
    /// (bit pattern 0.20000000000000004d), the smoother snaps straight onto it, and the peak sample
    /// therefore leaves at 0.5 * 0.2 = exactly thresholdLinear, 0.10000000000000002d. Landing the peak
    /// ON the threshold is the definition of limiting.
    ///
    /// REGENERATED: indices 2 and 6 moved from +/-0.19999999999999998d - which is 0.5 * 0.4, where 0.4
    /// was the old unit-mismatched "peak - threshold", leaving the peak at TWICE the threshold - to
    /// +/-0.10000000000000002d. The other six samples sit inside the soft knee or below it and are
    /// BIT-IDENTICAL to the pre-fix golden.
    /// </summary>
    [TestMethod]
    public void Golden_Transform_WithoutCalculateCoeffs()
    {
        var Local_Limiter = new ClassicLimiter();
        var Local_Result = Local_Limiter.Transform(DspCharacterization.Sine(8, 1, 0.5), new DSP_Stream());

        DspCharacterization.AssertExact(
            new double[]
            {
                0.0d, 0.35355339059327373d, 0.10000000000000002d, 0.35355339059327373d,
                6.123233995736766E-17d, -0.35355339059327373d, -0.10000000000000002d, -0.35355339059327384d
            },
            Local_Result, "Unconfigured ClassicLimiter output");
        DspCharacterization.AssertExact(1.0d, Local_Limiter.CompressionApplied, "CompressionApplied stays at its initial value");

        //The peak sample landed exactly on the threshold, hand-derived above.
        DspCharacterization.AssertExact(NAudio.Utils.Decibels.DecibelsToLinear(-20), Local_Result[2],
            "The limited peak equals thresholdLinear exactly");
    }

    #endregion

    #region Property Tests

    /// <summary>
    /// Silence in, exactly silence out, and the block is never touched (thresholdExceeded is false
    /// for every sample).
    /// </summary>
    [TestMethod]
    public void Property_Transform_SilenceStaysExactlySilent()
    {
        var Local_Stream = new DSP_Stream();
        var Local_Limiter = new ClassicLimiter();
        Local_Limiter.ResetSampleRate(48000);

        for (int Local_Block = 0; Local_Block < 6; Local_Block++)
        {
            var Local_Result = Local_Limiter.Transform(new double[8], Local_Stream);
            for (int i = 0; i < Local_Result.Length; i++)
                DspCharacterization.AssertExact(0.0d, Local_Result[i], "Block " + Local_Block + " sample " + i);
        }
    }

    /// <summary>
    /// A signal comfortably below the knee is passed through bit-exactly - the limiter must be
    /// transparent when it is not working.
    /// </summary>
    [TestMethod]
    public void Property_Transform_BelowTheKneeIsBitExactPassThrough()
    {
        var Local_Stream = new DSP_Stream();
        var Local_Limiter = new ClassicLimiter { Threshold_dB = -20, KneeWidth_dB = 24 };
        Local_Limiter.ResetSampleRate(48000);

        //-32 dB is below the knee start of -32 dB... use -40 dB to be unambiguously clear of it.
        double Local_Amplitude = 0.01;
        for (int Local_Block = 0; Local_Block < 4; Local_Block++)
        {
            var Local_Original = DspCharacterization.Sine(8, 1, Local_Amplitude);
            var Local_Result = Local_Limiter.Transform(DspCharacterization.Copy(Local_Original), Local_Stream);
            DspCharacterization.AssertExact(Local_Original, Local_Result, "Block " + Local_Block);
        }
    }

    /// <summary>
    /// FIXED BUG - this test used to be Bug_Transform_ClampConstantCollapsesToExactlyOne and asserted
    /// the DEFECT; it now asserts the corrected behaviour.
    ///
    /// The output clamp used to be written as
    ///
    ///     input[i] = Math.Min(1 - double.Epsilon, input[i] * this.Gain_Linear);
    ///
    /// The intent is plainly "the largest double strictly below 1", but double.Epsilon is the
    /// smallest positive SUBNORMAL (about 4.9e-324), which is far below the ULP of 1.0 (about
    /// 2.2e-16), so 1 - double.Epsilon rounds straight back to exactly 1.0. The ceiling was
    /// therefore full scale, not "just below full scale", and a limited sample could leave at
    /// exactly +1.0 (0 dBFS) - the first value an output converter can wrap on, which is precisely
    /// what the constant was written to avoid.
    ///
    /// It now reads
    ///
    ///     input[i] = Math.Clamp(input[i] * this.Gain_Linear, -OutputCeiling, OutputCeiling);
    ///
    /// with OutputCeiling = Math.BitDecrement(1.0), the largest double strictly below 1.0.
    ///
    /// The expected value below is derived from INTENDED BEHAVIOUR, not from what the code prints:
    /// a limiter must not emit at or beyond full scale, so a sample driven far over the ceiling has
    /// exactly one correct destination - the largest representable double strictly below 1.0.
    /// </summary>
    [TestMethod]
    public void Property_Transform_ClampCeilingIsGenuinelySubUnity()
    {
        //The original defect, kept as documentation: the old constant was not sub-unity at all.
        DspCharacterization.AssertExact(1.0d, 1.0d - double.Epsilon,
            "1.0 - double.Epsilon is exactly 1.0 - the intended 'just below unity' guard did not exist");

        //The replacement genuinely is sub-unity, and is the LARGEST such double.
        double Local_Ceiling = Math.BitDecrement(1.0);
        Assert.IsTrue(Local_Ceiling < 1.0d, "Math.BitDecrement(1.0) must be strictly below 1.0");
        DspCharacterization.AssertExact(1.0d, Math.BitIncrement(Local_Ceiling),
            "Math.BitDecrement(1.0) must be the LARGEST double below 1.0 - nothing sits between them");

        var Local_Stream = new DSP_Stream();
        var Local_Limiter = new ClassicLimiter();
        Local_Limiter.ResetSampleRate(48000);

        var Local_Result = Local_Limiter.Transform(DspCharacterization.Constant(16, 8.0), Local_Stream);

        DspCharacterization.AssertExact(Local_Ceiling, Local_Result[0],
            "The first hot sample must land on the ceiling, strictly inside full scale");
        for (int i = 0; i < Local_Result.Length; i++)
        {
            Assert.IsTrue(Local_Result[i] < 1.0d,
                "Sample " + i + " reached or exceeded full scale at " + Local_Result[i]);
            DspCharacterization.AssertExact(Local_Ceiling, Local_Result[i],
                "Sample " + i + " - 8.0 times any gain above 0.125 is over the ceiling, so the whole " +
                "block must be pinned to it");
        }
    }

    /// <summary>
    /// FIXED BUG - this test used to be Bug_Transform_ClampIsOneSidedSoNegativePeaksAreNotBounded and
    /// asserted the DEFECT; it now asserts the corrected behaviour.
    ///
    /// The clamp used to be ONE-SIDED: Math.Min bounds from above only, so an over-full-scale
    /// NEGATIVE sample was multiplied by the gain and then passed straight through with no lower
    /// bound at all. That is independent of the look-ahead unit fix (see
    /// <see cref="Property_Transform_GrosslyOverThresholdBlockIsNeverBoosted"/>), which stopped the
    /// limiter AMPLIFYING but could not bound the output: the 99 ms attack ramp has barely moved at
    /// the start of a block, so a -8.0 sample still left at roughly -7.9 - attenuated relative to its
    /// input, but nowhere near full scale, and handed straight to the converter.
    ///
    /// Math.Clamp against +/-OutputCeiling applies the ceiling symmetrically, so the negative side is
    /// now bounded exactly as tightly as the positive side.
    /// </summary>
    [TestMethod]
    public void Property_Transform_NegativePeaksAreBoundedByTheSymmetricClamp()
    {
        double Local_Ceiling = Math.BitDecrement(1.0);

        var Local_Stream = new DSP_Stream();
        var Local_Limiter = new ClassicLimiter();
        Local_Limiter.ResetSampleRate(48000);

        for (int Local_Block = 0; Local_Block < 4; Local_Block++)
            Local_Limiter.Transform(DspCharacterization.Constant(8, 8.0), Local_Stream);

        var Local_Result = Local_Limiter.Transform(DspCharacterization.Constant(8, -8.0), Local_Stream);

        DspCharacterization.AssertExact(-Local_Ceiling, Local_Result[0],
            "The negative side must be clamped to the mirror image of the positive ceiling");
        for (int i = 0; i < Local_Result.Length; i++)
            Assert.IsTrue(Local_Result[i] > -1.0d,
                "Sample " + i + " left the limiter below negative full scale at " + Local_Result[i]);
    }

    #endregion

    #region Look-Ahead Unit Fix (regression guards)

    /// <summary>
    /// REGRESSION GUARD for the look-ahead unit fix.
    ///
    /// The look-ahead correction used to read
    ///
    ///     gainReductionLinear = Math.Max(gainReductionLinear, peakValue - thresholdLinear);
    ///
    /// which mixed an amplitude DIFFERENCE into a gain MULTIPLIER. For a +/-8.0 block against the
    /// default -20 dB (0.1 linear) threshold that produced 7.9, and Math.Max selected it, so the
    /// "limiter" applied a 7.9x BOOST. It now reads Math.Min(gainReductionLinear,
    /// thresholdLinear / peakValue), the multiplier that actually lands the peak on the threshold.
    ///
    /// The invariant asserted here is the weakest one the fix must guarantee on its own: a limiter
    /// may attenuate but must NEVER make a sample louder than it arrived. (No +/-1.0 bound is
    /// asserted here - that is the OUTPUT CLAMP's job, a separate defect fixed separately, see
    /// <see cref="Property_Transform_NegativePeaksAreBoundedByTheSymmetricClamp"/> and the
    /// "Output Clamp Fix" region below.)
    ///
    /// This FAILS against the pre-fix code, where the negative blocks left at about -9.1.
    /// </summary>
    [TestMethod]
    public void Property_Transform_GrosslyOverThresholdBlockIsNeverBoosted()
    {
        var Local_Stream = new DSP_Stream();
        var Local_Limiter = new ClassicLimiter();
        Local_Limiter.ResetSampleRate(48000);

        for (int Local_Block = 0; Local_Block < 8; Local_Block++)
        {
            double Local_Amplitude = (Local_Block % 2 == 0) ? 8.0 : -8.0;
            var Local_Input = DspCharacterization.Constant(16, Local_Amplitude);
            var Local_Result = Local_Limiter.Transform(DspCharacterization.Copy(Local_Input), Local_Stream);

            for (int i = 0; i < Local_Result.Length; i++)
            {
                Assert.IsTrue(Math.Abs(Local_Result[i]) <= Math.Abs(Local_Input[i]),
                    "Block " + Local_Block + " sample " + i + " was BOOSTED: |" + Local_Result[i] +
                    "| exceeds the input magnitude |" + Local_Input[i] + "|");
            }
        }
    }

    /// <summary>
    /// REGRESSION GUARD for the look-ahead unit fix.
    ///
    /// CompressionApplied publishes Gain_Linear, which is a MULTIPLIER: 1.0 means "not working" and
    /// anything below 1.0 is gain reduction. A value above 1.0 is not gain reduction at all, it is a
    /// boost, and that is exactly what the old Math.Max produced on hot material. Every published
    /// figure across a long mixed sequence must now be &lt;= 1.0.
    ///
    /// This FAILS against the pre-fix code.
    /// </summary>
    [TestMethod]
    public void Property_Transform_CompressionAppliedNeverExceedsUnity()
    {
        var Local_Stream = new DSP_Stream();
        var Local_Limiter = new ClassicLimiter();
        Local_Limiter.ResetSampleRate(48000);

        double[][] Local_Blocks =
        {
            DspCharacterization.Constant(16, 8.0),
            DspCharacterization.Constant(16, -8.0),
            DspCharacterization.Sine(16, 1, 4.0),
            DspCharacterization.Alternating(16, 12.0),
            DspCharacterization.Noise(16, 999UL),
            DspCharacterization.Constant(16, 0.0),
            DspCharacterization.Sine(16, 2, 0.75),
            DspCharacterization.Constant(16, 1.0),
        };

        for (int Local_Block = 0; Local_Block < Local_Blocks.Length; Local_Block++)
        {
            Local_Limiter.Transform(Local_Blocks[Local_Block], Local_Stream);
            Assert.IsTrue(Local_Limiter.CompressionApplied <= 1.0d,
                "Block " + Local_Block + " reported a BOOST rather than gain reduction: " +
                Local_Limiter.CompressionApplied);
        }
    }

    /// <summary>
    /// REGRESSION GUARD against the fix over-correcting.
    ///
    /// The look-ahead branch must only ever engage for material above the threshold. Quiet audio that
    /// arrives AFTER a hot passage - the case where the smoother is still holding a small gain - must
    /// still be passed through bit-exactly, because the below-knee path never touches the sample at
    /// all. If a future change made the look-ahead reach below the threshold this would go silent.
    /// </summary>
    [TestMethod]
    public void Property_Transform_BelowThresholdAudioStillPassesThroughAfterHotMaterial()
    {
        var Local_Stream = new DSP_Stream();
        var Local_Limiter = new ClassicLimiter();
        Local_Limiter.ResetSampleRate(48000);

        //Drive the gain hard down first.
        for (int Local_Block = 0; Local_Block < 4; Local_Block++)
            Local_Limiter.Transform(DspCharacterization.Constant(64, 8.0), Local_Stream);

        Assert.IsTrue(Local_Limiter.CompressionApplied <= 1.0d, "The gain must be a reduction, not a boost");

        //-40 dB is comfortably below the -32 dB knee start, so nothing may be applied to it.
        for (int Local_Block = 0; Local_Block < 4; Local_Block++)
        {
            var Local_Original = DspCharacterization.Sine(8, 1, 0.01);
            var Local_Result = Local_Limiter.Transform(DspCharacterization.Copy(Local_Original), Local_Stream);
            DspCharacterization.AssertExact(Local_Original, Local_Result,
                "Quiet block " + Local_Block + " must be untouched");
        }
    }

    /// <summary>
    /// ANALYTIC ANCHOR for the look-ahead unit fix, with the smoother disabled.
    ///
    /// Skipping ResetSampleRate leaves AttackCoeff and ReleaseCoeff at 0, so the smoother snaps
    /// straight onto the computed target and the arithmetic is fully hand-checkable. For a +/-8.0
    /// block the two candidate multipliers are the static curve, 10^((-20 - 20*log10(8))/20), and the
    /// look-ahead, thresholdLinear / 8.0; both equal 0.0125 to within one ULP, so the block must leave
    /// at the -20 dB threshold, 0.1 linear - not at 7.9x its input level as it did before the fix.
    /// </summary>
    [TestMethod]
    public void Property_Transform_UnsmoothedHotBlockLandsOnTheThreshold()
    {
        double Local_ThresholdLinear = NAudio.Utils.Decibels.DecibelsToLinear(-20);

        foreach (double Local_Amplitude in new[] { 8.0, -8.0 })
        {
            var Local_Limiter = new ClassicLimiter(); //deliberately no ResetSampleRate
            var Local_Result = Local_Limiter.Transform(DspCharacterization.Constant(8, Local_Amplitude), new DSP_Stream());

            for (int i = 0; i < Local_Result.Length; i++)
            {
                Assert.IsTrue(Math.Abs(Local_Result[i]) <= Local_ThresholdLinear + 1e-15,
                    "Amplitude " + Local_Amplitude + " sample " + i + " should sit on the threshold, was " + Local_Result[i]);
                Assert.IsTrue(Math.Sign(Local_Result[i]) == Math.Sign(Local_Amplitude),
                    "Amplitude " + Local_Amplitude + " sample " + i + " changed polarity");
            }

            DspCharacterization.AssertExact(Math.CopySign(0.09999999999999995d, Local_Amplitude), Local_Result[0],
                "Amplitude " + Local_Amplitude + " first sample");
        }
    }

    #endregion

    #region Output Clamp Fix (regression guards)

    /// <summary>
    /// REGRESSION GUARD for the output clamp fix, negative half.
    ///
    /// A grossly over-threshold block of NEGATIVE samples must leave the limiter inside +/-1.0. This
    /// is the assertion that could not be made while the clamp was Math.Min: Math.Min bounds only
    /// from above, so the whole block sailed through at roughly -7.9.
    ///
    /// Justification for the exact value: with the default -20 dB threshold the smoothed gain starts
    /// at 1.0 and only walks DOWN towards 0.0125 over a 99 ms attack, so across a 600-sample block at
    /// 48 kHz it never falls below about 0.88. 8.0 times anything above 0.125 is over the ceiling, so
    /// EVERY sample of the block must be pinned to -Math.BitDecrement(1.0) exactly.
    ///
    /// This FAILS against the pre-fix code at every length.
    /// </summary>
    [TestMethod]
    public void Property_Transform_GrosslyOverThresholdNegativeBlockIsBoundedWithinFullScale()
    {
        double Local_Ceiling = Math.BitDecrement(1.0);

        foreach (int Local_Length in new[] { 8, 512, 600 })
        {
            var Local_Limiter = new ClassicLimiter();
            Local_Limiter.ResetSampleRate(48000);

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
    /// A symmetric +/-8.0 block (alternating at the Nyquist rate, so consecutive samples swap sign
    /// while the smoothed gain keeps falling) must be bounded on BOTH signs. Before the fix the
    /// positive samples were pinned at exactly 1.0 and the negative ones were not bounded at all,
    /// which is an asymmetric, DC-offsetting distortion as well as an out-of-range one.
    /// </summary>
    [TestMethod]
    public void Property_Transform_SymmetricHotBlockIsBoundedOnBothSigns()
    {
        double Local_Ceiling = Math.BitDecrement(1.0);

        foreach (int Local_Length in new[] { 8, 512, 600 })
        {
            var Local_Limiter = new ClassicLimiter();
            Local_Limiter.ResetSampleRate(48000);

            var Local_Input = DspCharacterization.Alternating(Local_Length, 8.0);
            var Local_Result = Local_Limiter.Transform(DspCharacterization.Copy(Local_Input), new DSP_Stream());

            bool Local_SawPositive = false;
            bool Local_SawNegative = false;
            for (int i = 0; i < Local_Result.Length; i++)
            {
                Assert.IsTrue(Math.Abs(Local_Result[i]) <= 1.0d,
                    "Length " + Local_Length + " sample " + i + " is outside +/-1.0 at " + Local_Result[i]);
                DspCharacterization.AssertExact(Math.CopySign(Local_Ceiling, Local_Input[i]), Local_Result[i],
                    "Length " + Local_Length + " sample " + i + " must be pinned to the ceiling of its own sign");

                if (Local_Result[i] > 0) Local_SawPositive = true;
                if (Local_Result[i] < 0) Local_SawNegative = true;
            }

            Assert.IsTrue(Local_SawPositive && Local_SawNegative,
                "Length " + Local_Length + " must actually exercise both polarities");
        }
    }

    /// <summary>
    /// REGRESSION GUARD proving the ceiling is genuinely SUB-unity, not merely "at most 1.0".
    ///
    /// This is the half of the defect that survives the one-sidedness argument entirely: even for
    /// purely positive material the old Math.Min(1 - double.Epsilon, ...) let a sample out at exactly
    /// +1.0, because 1 - double.Epsilon IS 1.0. A strict inequality is therefore the assertion, and
    /// it fails against the pre-fix code on the very first sample.
    /// </summary>
    [TestMethod]
    public void Property_Transform_ClampedOutputMagnitudeIsStrictlyBelowOne()
    {
        foreach (int Local_Length in new[] { 8, 512, 600 })
        {
            foreach (double Local_Amplitude in new[] { 8.0, -8.0, 1000.0, -1000.0 })
            {
                var Local_Limiter = new ClassicLimiter();
                Local_Limiter.ResetSampleRate(48000);

                var Local_Result = Local_Limiter.Transform(
                    DspCharacterization.Constant(Local_Length, Local_Amplitude), new DSP_Stream());

                for (int i = 0; i < Local_Result.Length; i++)
                    Assert.IsTrue(Math.Abs(Local_Result[i]) < 1.0d,
                        "Length " + Local_Length + " amplitude " + Local_Amplitude + " sample " + i +
                        " must be STRICTLY below full scale, was " + Local_Result[i]);
            }
        }
    }

    /// <summary>
    /// REGRESSION GUARD against the clamp perturbing normal audio.
    ///
    /// The clamp may only act on material that actually reaches the ceiling. Ordinary audio - here a
    /// -40 dB sine, comfortably below the -32 dB knee start - must still come out bit-for-bit
    /// identical to what went in, at every block length, both before and after a hot passage has
    /// driven the smoother down.
    /// </summary>
    [TestMethod]
    public void Property_Transform_BelowThresholdAudioIsUnperturbedByTheClamp()
    {
        foreach (int Local_Length in new[] { 8, 512, 600 })
        {
            var Local_Stream = new DSP_Stream();
            var Local_Limiter = new ClassicLimiter();
            Local_Limiter.ResetSampleRate(48000);

            var Local_Before = DspCharacterization.Sine(Local_Length, 1, 0.01);
            DspCharacterization.AssertExact(Local_Before,
                Local_Limiter.Transform(DspCharacterization.Copy(Local_Before), Local_Stream),
                "Length " + Local_Length + " quiet block before hot material");

            for (int Local_Block = 0; Local_Block < 4; Local_Block++)
                Local_Limiter.Transform(DspCharacterization.Constant(Local_Length, 8.0), Local_Stream);

            var Local_After = DspCharacterization.Sine(Local_Length, 1, 0.01);
            DspCharacterization.AssertExact(Local_After,
                Local_Limiter.Transform(DspCharacterization.Copy(Local_After), Local_Stream),
                "Length " + Local_Length + " quiet block after hot material");
        }
    }

    #endregion

    #region Aliasing / Mutation Contract

    /// <summary>
    /// Transform mutates the caller's array in place and returns that same instance; an empty block
    /// short-circuits.
    /// </summary>
    [TestMethod]
    public void Contract_Transform_MutatesInPlaceAndReturnsTheInputInstance()
    {
        var Local_Limiter = new ClassicLimiter();
        Local_Limiter.ResetSampleRate(48000);

        var Local_Input = DspCharacterization.Constant(8, 1.0);
        var Local_Result = Local_Limiter.Transform(Local_Input, new DSP_Stream());
        Assert.IsTrue(ReferenceEquals(Local_Input, Local_Result));
        Assert.AreNotEqual(1.0d, Local_Input[0], "The caller's array was written in place");

        var Local_Empty = new double[0];
        var Local_EmptyResult = Local_Limiter.Transform(Local_Empty, new DSP_Stream());
        Assert.IsTrue(ReferenceEquals(Local_Empty, Local_EmptyResult));
        Assert.AreEqual(0, Local_EmptyResult.Length);
    }

    #endregion
}
