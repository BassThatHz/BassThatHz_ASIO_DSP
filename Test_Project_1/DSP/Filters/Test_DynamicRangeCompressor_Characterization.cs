#nullable enable

namespace Test_Project_1;

#region Usings
using BassThatHz_ASIO_DSP_Processor;
using System;
using Test_Project_1.TestHelpers;
#endregion

/// <summary>
/// CHARACTERIZATION / GUARD SUITE for DSP\Filters\DynamicRangeCompressor.cs.
///
/// Two things make this filter interesting to a would-be optimizer, and both are pinned here:
///
/// 1. STATE. Gain_Linear carries between blocks and CompressionApplied is published to the GUI, so
///    the eight-block sequences below pin the audio and the state after EVERY block.
///
/// 2. TWO IMPLEMENTATIONS OF THE SAME MATH. Blocks of 512 samples or fewer use a stackalloc
///    scratch buffer; anything longer rents from ArrayPool and runs a duplicated copy of the loop.
///    A divergence between the two would be inaudible in testing and catastrophic in production, so
///    both paths - and the 512-sample boundary itself - are pinned.
/// </summary>
[TestClass]
public class Test_DynamicRangeCompressor_Characterization
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
            DspCharacterization.Noise(8, 13000UL),
            DspCharacterization.Constant(8, -1.0),
            DspCharacterization.Sine(8, 1, 0.5),
        };
    }

    #endregion

    #region Multi-Block Stateful Sequence (small / stackalloc path)

    /// <summary>
    /// Eight consecutive blocks of audio through the stackalloc path, pinned bit-exactly.
    ///
    /// REGENERATED for the look-ahead unit fix (Math.Max of an amplitude difference -> Math.Min of the
    /// multiplier thresholdLinear / peakSuffix). Blocks 1, 2, 3, 5, 6 and 7 contain samples above the
    /// -8 dB top of the knee and therefore take the above-threshold branch and its look-ahead
    /// correction; every one of their samples is now SMALLER in magnitude than before the fix.
    ///
    /// Blocks 0 (a 0.05 sine, entirely inside the soft knee) and 4 (silence) are BIT-IDENTICAL to the
    /// pre-fix golden - evidence that nothing outside the look-ahead branch moved.
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
            new[] { -0.8272327764144809d, -0.3695715296211574d, -0.8483917642507564d, 0.6268757305029558d, -0.858512275956793d, -0.9314567666052233d, 0.6058267764172238d, -0.8468526467662896d },
            new[] { -0.9952320709614266d, -0.9950437001949547d, -0.9948553690646249d, -0.9946670775620973d, -0.9944788256790335d, -0.9942906134070969d, -0.9941024407379528d, -0.9939143076632682d },
            new[] { 0.0d, 0.35144613508595923d, 0.4969363515155808d, 0.35143172890416674d, 6.123233995736766E-17d, -0.35147547294014914d, -0.49697783277672697d, -0.3514604558299343d },
        };

        var Local_Stream = new DSP_Stream();
        var Local_Compressor = new DynamicRangeCompressor();
        Local_Compressor.ResetSampleRate(48000);
        var Local_Inputs = BuildInputs();

        for (int Local_Block = 0; Local_Block < Local_Inputs.Length; Local_Block++)
        {
            var Local_Result = Local_Compressor.Transform(DspCharacterization.Copy(Local_Inputs[Local_Block]), Local_Stream);
            DspCharacterization.AssertExact(Local_Expected[Local_Block], Local_Result, "Block " + Local_Block);
        }
    }

    /// <summary>
    /// CompressionApplied after each of the same eight blocks. As with ClassicLimiter, silence
    /// (block 4) freezes the published figure rather than releasing it, because Gain_Linear is only
    /// updated when the threshold is exceeded.
    ///
    /// REGENERATED for the look-ahead unit fix. Block 0 (soft knee only) is unchanged at exactly 1.0;
    /// every later figure is SMALLER than the pre-fix golden, i.e. more gain reduction, and every one
    /// is &lt;= 1.0 - a multiplier above 1.0 would be a boost, not compression.
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
            0.9954204813723826d,
            0.9939143076632682d,
            0.9940802865450462d,
        };

        var Local_Stream = new DSP_Stream();
        var Local_Compressor = new DynamicRangeCompressor();
        Local_Compressor.ResetSampleRate(48000);
        var Local_Inputs = BuildInputs();

        for (int Local_Block = 0; Local_Block < Local_Inputs.Length; Local_Block++)
        {
            Local_Compressor.Transform(DspCharacterization.Copy(Local_Inputs[Local_Block]), Local_Stream);
            DspCharacterization.AssertExact(Local_Expected[Local_Block], Local_Compressor.CompressionApplied,
                "Block " + Local_Block + " CompressionApplied");
        }
    }

    #endregion

    #region Large-Block (ArrayPool) Path

    /// <summary>
    /// A 600-sample block exceeds the 512-sample stackalloc limit and therefore runs the DUPLICATED
    /// ArrayPool loop. Pinning all 600 samples would be unreadable, so this pins the first eight,
    /// the last eight, the total energy of the block, and the resulting CompressionApplied - a
    /// combination that no realistic arithmetic change could survive.
    ///
    /// TOLERANCE NOTE: the energy figure is an accumulated sum of 600 squares and is still asserted
    /// BIT-EXACTLY, because the summation order is part of what must not change.
    ///
    /// REGENERATED for the look-ahead unit fix. This is full-scale noise, so essentially every sample
    /// takes the above-threshold branch and the whole block moves. The block energy is the clearest
    /// evidence that the new goldens are correct in direction as well as in value: it fell from
    /// 193.9700340180094 to 190.34158675509653. A compressor may only remove energy from a block that
    /// is over its threshold - before the fix the look-ahead was ADDING energy back.
    /// </summary>
    [TestMethod]
    public void Stateful_Transform_LargeBlockUsesTheArrayPoolPath()
    {
        var Local_Compressor = new DynamicRangeCompressor();
        Local_Compressor.ResetSampleRate(48000);

        var Local_Result = Local_Compressor.Transform(DspCharacterization.Noise(600, 13500UL), new DSP_Stream());
        Assert.AreEqual(600, Local_Result.Length);

        var Local_First = new double[8];
        var Local_Last = new double[8];
        Array.Copy(Local_Result, 0, Local_First, 0, 8);
        Array.Copy(Local_Result, 592, Local_Last, 0, 8);

        DspCharacterization.AssertExact(
            new double[]
            {
                0.17034915907974857d, -0.6620472497298409d, 0.18453854821956453d, 0.2764261428644103d,
                0.9596166201678392d, 0.7158534651037595d, -0.5660636779963125d, 0.5885942066008295d
            },
            Local_First, "First eight samples of the large-block path");

        DspCharacterization.AssertExact(
            new double[]
            {
                -0.8024166096501297d, 0.928208926989056d, 0.8312707499184165d, 0.8022360355221202d,
                0.8437016315186933d, -0.9469487802671164d, 0.3546636809547002d, -0.8820071839700273d
            },
            Local_Last, "Last eight samples of the large-block path");

        double Local_Energy = 0;
        for (int i = 0; i < Local_Result.Length; i++)
            Local_Energy += Local_Result[i] * Local_Result[i];
        DspCharacterization.AssertExact(190.34158675509653d, Local_Energy, "Total energy of the large block");

        DspCharacterization.AssertExact(0.9871416027558172d, Local_Compressor.CompressionApplied, "CompressionApplied");
    }

    /// <summary>
    /// Exactly 512 samples is the LAST size that takes the stackalloc path (the test is
    /// len &gt; 512). Pinned so a change to the constant is caught immediately.
    ///
    /// REGENERATED for the look-ahead unit fix - full-scale noise, so the above-threshold branch runs
    /// for essentially every sample. Each of the eight pinned samples is smaller in magnitude than its
    /// pre-fix value and CompressionApplied dropped from 0.997985400649859 to 0.9847410988333473,
    /// i.e. more gain reduction, still below unity.
    /// </summary>
    [TestMethod]
    public void Stateful_Transform_ExactlyFiveHundredAndTwelveTakesTheStackallocPath()
    {
        var Local_Compressor = new DynamicRangeCompressor();
        Local_Compressor.ResetSampleRate(48000);

        var Local_Result = Local_Compressor.Transform(DspCharacterization.Noise(512, 13600UL), new DSP_Stream());
        var Local_Last = new double[8];
        Array.Copy(Local_Result, 504, Local_Last, 0, 8);

        DspCharacterization.AssertExact(
            new double[]
            {
                0.9823018813424466d, -0.482865979939392d, 0.8076231069888563d, -0.7775673415935913d,
                0.10955874616764968d, -0.3859289384306454d, -0.47100026210247337d, -0.6162560116335914d
            },
            Local_Last, "Last eight samples at exactly 512");
        DspCharacterization.AssertExact(0.9847410988333473d, Local_Compressor.CompressionApplied, "CompressionApplied");
    }

    /// <summary>
    /// The stackalloc path and the ArrayPool path implement the same maths on two separate copies of
    /// the loop. Feeding one 513-sample block through the pool path and the same samples through a
    /// fresh compressor as 513 = 512 + 1 would NOT be comparable (the look-ahead suffix maximum is
    /// per-block), so instead this compares the pool path against a hand-written reference of the
    /// documented algorithm.
    /// </summary>
    [TestMethod]
    public void Property_Transform_BothImplementationPathsAgreeWithTheReferenceAlgorithm()
    {
        foreach (int Local_Length in new[] { 8, 512, 513, 600 })
        {
            var Local_Input = DspCharacterization.Noise(Local_Length, (ulong)(40000 + Local_Length));

            var Local_Compressor = new DynamicRangeCompressor();
            Local_Compressor.ResetSampleRate(48000);
            var Local_Actual = Local_Compressor.Transform(DspCharacterization.Copy(Local_Input), new DSP_Stream());

            var Local_Reference = ReferenceCompress(DspCharacterization.Copy(Local_Input), 48000);

            DspCharacterization.AssertExact(Local_Reference, Local_Actual,
                "Length " + Local_Length + " must match the reference algorithm bit for bit");
        }
    }

    /// <summary>
    /// A deliberately naive transcription of DynamicRangeCompressor's documented algorithm at its
    /// default settings. Used to prove the stackalloc and ArrayPool paths are the same maths.
    /// </summary>
    private static double[] ReferenceCompress(double[] input, double sampleRate)
    {
        const double Local_ThresholdDb = -20;
        const double Local_Ratio = 24;
        const double Local_AttackMs = 99;
        const double Local_ReleaseMs = 1;
        const double Local_KneeDb = 24;

        double Local_Attack = Math.Exp(-1.0 / (0.001 * Local_AttackMs * sampleRate));
        double Local_Release = Math.Exp(-1.0 / (0.001 * Local_ReleaseMs * sampleRate));
        double Local_InverseRatioFactor = 10D / Local_Ratio;
        double Local_ThresholdLinear = NAudio.Utils.Decibels.DecibelsToLinear(Local_ThresholdDb);

        int Local_Length = input.Length;
        var Local_SuffixMax = new double[Local_Length];
        for (int i = 0; i < Local_Length; i++)
            Local_SuffixMax[i] = Math.Abs(input[i]);
        double Local_RunningMax = Local_SuffixMax[Local_Length - 1];
        for (int i = Local_Length - 1; i >= 0; i--)
        {
            if (Local_SuffixMax[i] > Local_RunningMax) Local_RunningMax = Local_SuffixMax[i];
            Local_SuffixMax[i] = Local_RunningMax;
        }

        double Local_Gain = 1.0;
        for (int i = 0; i < Local_Length; i++)
        {
            double Local_PeakSuffix = Local_SuffixMax[i];
            double Local_InstAbs = Math.Abs(input[i]);
            double Local_InputDb = NAudio.Utils.Decibels.LinearToDecibels(Local_InstAbs + 1e-99);

            double Local_Reduction = 0;
            bool Local_Exceeded = false;

            if (Local_InputDb > Local_ThresholdDb - Local_KneeDb * 0.5 && Local_InputDb < Local_ThresholdDb + Local_KneeDb * 0.5)
            {
                Local_Exceeded = true;
                double Local_KneeStart = Local_ThresholdDb - Local_KneeDb * 0.5;
                double Local_Ratio2 = (Local_InputDb - Local_KneeStart) / Local_KneeDb;
                double Local_Adjusted = Local_KneeStart + Local_Ratio2 * Local_KneeDb;
                Local_Reduction = NAudio.Utils.Decibels.DecibelsToLinear(Local_Adjusted - Local_InputDb);
            }
            else if (Local_InputDb > Local_ThresholdDb)
            {
                Local_Exceeded = true;
                double Local_DesiredDb = Local_ThresholdDb + (Local_InputDb - Local_ThresholdDb) * Local_InverseRatioFactor;
                Local_Reduction = NAudio.Utils.Decibels.DecibelsToLinear(Local_DesiredDb - Local_InputDb);
                //Look-ahead: the multiplier that brings the look-ahead peak down to the threshold is
                //ThresholdLinear / PeakSuffix, and more reduction is the SMALLER multiplier.
                if (Local_PeakSuffix > Local_ThresholdLinear)
                    Local_Reduction = Math.Min(Local_Reduction, Local_ThresholdLinear / Local_PeakSuffix);
            }

            if (Local_Exceeded)
            {
                if (Local_Reduction < Local_Gain)
                    Local_Gain = Local_Attack * (Local_Gain - Local_Reduction) + Local_Reduction;
                else
                    Local_Gain = Local_Release * (Local_Gain - Local_Reduction) + Local_Reduction;

                //Output ceiling: the largest double strictly below 1.0, applied symmetrically. The
                //production code originally had no clamp here at all, and its sibling ClassicLimiter
                //used Math.Min(1 - double.Epsilon, ...) - which is Math.Min(1.0, ...), i.e. neither
                //sub-unity nor two-sided.
                input[i] = Math.Clamp(input[i] * Local_Gain, -Math.BitDecrement(1.0), Math.BitDecrement(1.0));
            }
        }

        return input;
    }

    #endregion

    #region Without CalculateCoeffs

    /// <summary>
    /// Without ResetSampleRate the coefficients are 0, so the smoother snaps straight to the
    /// computed gain. That makes this the one golden that is fully hand-derivable, and therefore the
    /// anchor for the look-ahead unit fix.
    ///
    /// The block is a 0.5-amplitude sine, so the look-ahead peak is 0.5 and the threshold is -20 dB
    /// (thresholdLinear = 0.1). The static 24:1 curve would ask for a multiplier of about 0.39, but
    /// the look-ahead multiplier is threshold / peak = 0.1 / 0.5 = 0.2 and Math.Min takes the smaller
    /// of the two, so the peak sample leaves at 0.5 * 0.2 = exactly thresholdLinear,
    /// 0.10000000000000002d.
    ///
    /// REGENERATED: indices 2 and 6 moved from +/-0.19999999999999998d - which is 0.5 * 0.4, where 0.4
    /// was the old unit-mismatched "peak - threshold", leaving the peak at TWICE the threshold - to
    /// +/-0.10000000000000002d. The other six samples are inside or below the knee and are
    /// BIT-IDENTICAL to the pre-fix golden.
    /// </summary>
    [TestMethod]
    public void Golden_Transform_WithoutCalculateCoeffs()
    {
        var Local_Compressor = new DynamicRangeCompressor();
        var Local_Result = Local_Compressor.Transform(DspCharacterization.Sine(8, 1, 0.5), new DSP_Stream());

        DspCharacterization.AssertExact(
            new double[]
            {
                0.0d, 0.35355339059327373d, 0.10000000000000002d, 0.35355339059327373d,
                6.123233995736766E-17d, -0.35355339059327373d, -0.10000000000000002d, -0.35355339059327384d
            },
            Local_Result, "Unconfigured DynamicRangeCompressor output");

        //The peak sample landed exactly on the threshold, hand-derived above.
        DspCharacterization.AssertExact(NAudio.Utils.Decibels.DecibelsToLinear(-20), Local_Result[2],
            "The compressed peak equals thresholdLinear exactly");
    }

    #endregion

    #region Property Tests

    /// <summary>
    /// Silence in, exactly silence out.
    /// </summary>
    [TestMethod]
    public void Property_Transform_SilenceStaysExactlySilent()
    {
        var Local_Stream = new DSP_Stream();
        var Local_Compressor = new DynamicRangeCompressor();
        Local_Compressor.ResetSampleRate(48000);

        foreach (int Local_Length in new[] { 8, 512, 600 })
        {
            var Local_Result = Local_Compressor.Transform(new double[Local_Length], Local_Stream);
            for (int i = 0; i < Local_Result.Length; i++)
                DspCharacterization.AssertExact(0.0d, Local_Result[i], "Length " + Local_Length + " sample " + i);
        }
    }

    /// <summary>
    /// FIXED BUG - this test used to be Bug_Transform_LookAheadMixesLinearPeakIntoTheGainAndThereIsNoOutputClamp
    /// and asserted the DEFECT; it now asserts the corrected behaviour.
    ///
    /// The above-threshold branch used to mix a LINEAR PEAK DIFFERENCE into what is otherwise a gain
    /// MULTIPLIER:
    ///
    ///     gainReductionLinear = Math.Max(gainReductionLinear, peakSuffix - thresholdLinear);
    ///
    /// peakSuffix - thresholdLinear is an amplitude, not a ratio, so for any peak above about 1.1 it
    /// exceeded 1.0 and the "gain reduction" became a gain BOOST - a +8.0 block against the default
    /// -20 dB threshold produced 7.9 and left the compressor above +9.0, LOUDER than it arrived.
    ///
    /// The correct multiplier is the one that brings the look-ahead peak down to the threshold,
    /// thresholdLinear / peakSuffix, and "more reduction" is the SMALLER multiplier:
    ///
    ///     gainReductionLinear = Math.Min(gainReductionLinear, thresholdLinear / peakSuffix);
    ///
    /// NOTE ON SCOPE: this test covers the LOOK-AHEAD fix only. Bounding the output inside +/-1 is
    /// the OUTPUT CLAMP's job, which was a separate defect - DynamicRangeCompressor applied the gain
    /// with no output clamp at all (input[i] *= gainLinearLocal) - fixed separately; see the "Output
    /// Clamp Fix" region below. Because the clamp now pins a +8.0 block to the ceiling, the attack
    /// ramp is no longer observable in the OUTPUT of a grossly over-threshold block, so the
    /// "gain keeps falling" half of this test uses a 0.5 block, which is above the threshold (-6 dB
    /// against a -8 dB knee top) yet never reaches the ceiling.
    /// </summary>
    [TestMethod]
    public void Property_Transform_LookAheadAttenuatesInsteadOfBoosting()
    {
        var Local_Compressor = new DynamicRangeCompressor();
        Local_Compressor.ResetSampleRate(48000);

        var Local_Result = Local_Compressor.Transform(DspCharacterization.Constant(8, 8.0), new DSP_Stream());

        for (int i = 0; i < Local_Result.Length; i++)
            Assert.IsTrue(Local_Result[i] <= 8.0d,
                "Sample " + i + " was AMPLIFIED rather than compressed; it left at " + Local_Result[i]);

        Assert.IsTrue(Local_Compressor.CompressionApplied <= 1.0d,
            "CompressionApplied must be a reduction multiplier, not a boost: " + Local_Compressor.CompressionApplied);

        //The gain must also be strictly decreasing across a sustained over-threshold block. Measured
        //on 0.5, which engages the above-threshold branch but stays well clear of the output ceiling,
        //so the ramp is visible in the samples themselves.
        var Local_Ramped = new DynamicRangeCompressor();
        Local_Ramped.ResetSampleRate(48000);
        var Local_RampResult = Local_Ramped.Transform(DspCharacterization.Constant(8, 0.5), new DSP_Stream());

        for (int i = 0; i < Local_RampResult.Length; i++)
            Assert.IsTrue(Math.Abs(Local_RampResult[i]) < 1.0d,
                "The 0.5 probe block must stay clear of the ceiling; sample " + i + " was " + Local_RampResult[i]);

        for (int i = 1; i < Local_RampResult.Length; i++)
            Assert.IsTrue(Local_RampResult[i] < Local_RampResult[i - 1],
                "The attack ramp must keep reducing a sustained over-threshold block; sample " + i +
                " (" + Local_RampResult[i] + ") did not fall below sample " + (i - 1) + " (" + Local_RampResult[i - 1] + ")");
    }

    /// <summary>
    /// A signal well below the knee is passed through bit-exactly.
    /// </summary>
    [TestMethod]
    public void Property_Transform_BelowTheKneeIsBitExactPassThrough()
    {
        var Local_Stream = new DSP_Stream();
        var Local_Compressor = new DynamicRangeCompressor();
        Local_Compressor.ResetSampleRate(48000);

        for (int Local_Block = 0; Local_Block < 4; Local_Block++)
        {
            var Local_Original = DspCharacterization.Sine(8, 1, 0.01);
            var Local_Result = Local_Compressor.Transform(DspCharacterization.Copy(Local_Original), Local_Stream);
            DspCharacterization.AssertExact(Local_Original, Local_Result, "Block " + Local_Block);
        }
    }

    #endregion

    #region Look-Ahead Unit Fix (regression guards)

    /// <summary>
    /// REGRESSION GUARD for the look-ahead unit fix: a compressor may attenuate but must NEVER make a
    /// sample louder than it arrived, on either polarity, on either implementation path.
    ///
    /// No +/-1.0 bound is asserted here - that is the OUTPUT CLAMP's job, a separate defect fixed
    /// separately (see the "Output Clamp Fix" region below); this pins only what the unit fix itself
    /// guarantees.
    ///
    /// This FAILS against the pre-fix code, where a +/-8.0 block left at about +/-9.1.
    /// </summary>
    [TestMethod]
    public void Property_Transform_GrosslyOverThresholdBlockIsNeverBoosted()
    {
        foreach (int Local_Length in new[] { 8, 512, 600 })
        {
            var Local_Stream = new DSP_Stream();
            var Local_Compressor = new DynamicRangeCompressor();
            Local_Compressor.ResetSampleRate(48000);

            for (int Local_Block = 0; Local_Block < 8; Local_Block++)
            {
                double Local_Amplitude = (Local_Block % 2 == 0) ? 8.0 : -8.0;
                var Local_Input = DspCharacterization.Constant(Local_Length, Local_Amplitude);
                var Local_Result = Local_Compressor.Transform(DspCharacterization.Copy(Local_Input), Local_Stream);

                for (int i = 0; i < Local_Result.Length; i++)
                {
                    Assert.IsTrue(Math.Abs(Local_Result[i]) <= Math.Abs(Local_Input[i]),
                        "Length " + Local_Length + " block " + Local_Block + " sample " + i +
                        " was BOOSTED: |" + Local_Result[i] + "| exceeds |" + Local_Input[i] + "|");
                }
            }
        }
    }

    /// <summary>
    /// REGRESSION GUARD for the look-ahead unit fix.
    ///
    /// CompressionApplied publishes Gain_Linear, a MULTIPLIER: 1.0 is "not working" and below 1.0 is
    /// gain reduction. Above 1.0 is a boost, which is what the old Math.Max produced on hot material.
    /// Every figure across a long mixed sequence must be &lt;= 1.0.
    ///
    /// This FAILS against the pre-fix code.
    /// </summary>
    [TestMethod]
    public void Property_Transform_CompressionAppliedNeverExceedsUnity()
    {
        var Local_Stream = new DSP_Stream();
        var Local_Compressor = new DynamicRangeCompressor();
        Local_Compressor.ResetSampleRate(48000);

        double[][] Local_Blocks =
        {
            DspCharacterization.Constant(16, 8.0),
            DspCharacterization.Constant(16, -8.0),
            DspCharacterization.Sine(16, 1, 4.0),
            DspCharacterization.Alternating(600, 12.0),
            DspCharacterization.Noise(16, 999UL),
            DspCharacterization.Constant(16, 0.0),
            DspCharacterization.Sine(16, 2, 0.75),
            DspCharacterization.Constant(513, 1.0),
        };

        for (int Local_Block = 0; Local_Block < Local_Blocks.Length; Local_Block++)
        {
            Local_Compressor.Transform(Local_Blocks[Local_Block], Local_Stream);
            Assert.IsTrue(Local_Compressor.CompressionApplied <= 1.0d,
                "Block " + Local_Block + " reported a BOOST rather than gain reduction: " +
                Local_Compressor.CompressionApplied);
        }
    }

    /// <summary>
    /// REGRESSION GUARD against the fix over-correcting: quiet audio arriving after a hot passage,
    /// while the smoother is still holding a small gain, must still be passed through bit-exactly,
    /// because the below-knee path never touches the sample.
    /// </summary>
    [TestMethod]
    public void Property_Transform_BelowThresholdAudioStillPassesThroughAfterHotMaterial()
    {
        var Local_Stream = new DSP_Stream();
        var Local_Compressor = new DynamicRangeCompressor();
        Local_Compressor.ResetSampleRate(48000);

        for (int Local_Block = 0; Local_Block < 4; Local_Block++)
            Local_Compressor.Transform(DspCharacterization.Constant(64, 8.0), Local_Stream);

        Assert.IsTrue(Local_Compressor.CompressionApplied <= 1.0d, "The gain must be a reduction, not a boost");

        //-40 dB is comfortably below the -32 dB knee start, so nothing may be applied to it.
        for (int Local_Block = 0; Local_Block < 4; Local_Block++)
        {
            var Local_Original = DspCharacterization.Sine(8, 1, 0.01);
            var Local_Result = Local_Compressor.Transform(DspCharacterization.Copy(Local_Original), Local_Stream);
            DspCharacterization.AssertExact(Local_Original, Local_Result,
                "Quiet block " + Local_Block + " must be untouched");
        }
    }

    /// <summary>
    /// ANALYTIC ANCHOR for the look-ahead unit fix, with the smoother disabled.
    ///
    /// Skipping ResetSampleRate leaves AttackCoeff and ReleaseCoeff at 0, so the gain snaps straight
    /// onto the computed target and the arithmetic is hand-checkable. For a +/-8.0 block the static
    /// 24:1 curve asks for about 0.0776 while the look-ahead asks for thresholdLinear / 8.0 = 0.0125;
    /// Math.Min takes the look-ahead, so the block leaves at exactly the -20 dB threshold, 0.1 linear
    /// - not at 7.9x its input level as it did before the fix.
    /// </summary>
    [TestMethod]
    public void Property_Transform_UnsmoothedHotBlockLandsOnTheThreshold()
    {
        double Local_ThresholdLinear = NAudio.Utils.Decibels.DecibelsToLinear(-20);

        foreach (double Local_Amplitude in new[] { 8.0, -8.0 })
        {
            var Local_Compressor = new DynamicRangeCompressor(); //deliberately no ResetSampleRate
            var Local_Result = Local_Compressor.Transform(DspCharacterization.Constant(8, Local_Amplitude), new DSP_Stream());

            for (int i = 0; i < Local_Result.Length; i++)
            {
                DspCharacterization.AssertExact(Math.CopySign(Local_ThresholdLinear, Local_Amplitude), Local_Result[i],
                    "Amplitude " + Local_Amplitude + " sample " + i + " must land exactly on the threshold");
            }

            DspCharacterization.AssertExact(Local_ThresholdLinear / 8.0, Local_Compressor.CompressionApplied,
                "Amplitude " + Local_Amplitude + " CompressionApplied is thresholdLinear / peak");
        }
    }

    #endregion

    #region Output Clamp Fix (regression guards)

    /// <summary>
    /// REGRESSION GUARD for the output clamp fix, negative half, on BOTH implementation paths.
    ///
    /// DynamicRangeCompressor applied the smoothed gain with a bare <c>input[i] *= gainLinearLocal;</c>
    /// on both the stackalloc and the ArrayPool loop - no output bound of any kind. That is the
    /// extreme form of the two-part clamp defect its sibling ClassicLimiter carried: (a) the ceiling
    /// ClassicLimiter used, 1 - double.Epsilon, is exactly 1.0 because double.Epsilon is the smallest
    /// positive SUBNORMAL (~4.9e-324), far below the ULP of 1.0 (~2.2e-16); and (b) that clamp was
    /// one-sided anyway. Both paths now use Math.Clamp against +/-Math.BitDecrement(1.0).
    ///
    /// Justification for the exact value: with the default -20 dB threshold the smoothed gain starts
    /// at 1.0 and only walks DOWN towards 0.0125 over a 99 ms attack, so across a 600-sample block at
    /// 48 kHz it never falls below about 0.88. 8.0 times anything above 0.125 is over the ceiling, so
    /// EVERY sample of the block must be pinned to -Math.BitDecrement(1.0) exactly.
    ///
    /// The 8 / 512 / 600 sweep covers the stackalloc path, its 512-sample boundary, and the
    /// duplicated ArrayPool path. This FAILS against the pre-fix code at every length.
    /// </summary>
    [TestMethod]
    public void Property_Transform_GrosslyOverThresholdNegativeBlockIsBoundedWithinFullScale()
    {
        double Local_Ceiling = Math.BitDecrement(1.0);

        foreach (int Local_Length in new[] { 8, 512, 600 })
        {
            var Local_Compressor = new DynamicRangeCompressor();
            Local_Compressor.ResetSampleRate(48000);

            var Local_Result = Local_Compressor.Transform(
                DspCharacterization.Constant(Local_Length, -8.0), new DSP_Stream());

            for (int i = 0; i < Local_Result.Length; i++)
            {
                Assert.IsTrue(Local_Result[i] >= -1.0d && Local_Result[i] <= 1.0d,
                    "Length " + Local_Length + " sample " + i + " left the compressor outside +/-1.0 at " +
                    Local_Result[i]);
                DspCharacterization.AssertExact(-Local_Ceiling, Local_Result[i],
                    "Length " + Local_Length + " sample " + i + " must be pinned to the negative ceiling");
            }
        }
    }

    /// <summary>
    /// REGRESSION GUARD for the output clamp fix, both halves at once, on BOTH implementation paths.
    ///
    /// A symmetric +/-8.0 block (alternating at the Nyquist rate, so consecutive samples swap sign
    /// while the smoothed gain keeps falling) must be bounded on BOTH signs. Before the fix neither
    /// sign was bounded at all here.
    /// </summary>
    [TestMethod]
    public void Property_Transform_SymmetricHotBlockIsBoundedOnBothSigns()
    {
        double Local_Ceiling = Math.BitDecrement(1.0);

        foreach (int Local_Length in new[] { 8, 512, 600 })
        {
            var Local_Compressor = new DynamicRangeCompressor();
            Local_Compressor.ResetSampleRate(48000);

            var Local_Input = DspCharacterization.Alternating(Local_Length, 8.0);
            var Local_Result = Local_Compressor.Transform(DspCharacterization.Copy(Local_Input), new DSP_Stream());

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
    /// Even if the missing clamp had been added as the sibling's Math.Min(1 - double.Epsilon, ...),
    /// a sample could still leave at exactly +1.0 (0 dBFS) - the first value an output converter can
    /// wrap on - because 1 - double.Epsilon IS 1.0. A STRICT inequality is therefore the assertion.
    /// </summary>
    [TestMethod]
    public void Property_Transform_ClampedOutputMagnitudeIsStrictlyBelowOne()
    {
        foreach (int Local_Length in new[] { 8, 512, 600 })
        {
            foreach (double Local_Amplitude in new[] { 8.0, -8.0, 1000.0, -1000.0 })
            {
                var Local_Compressor = new DynamicRangeCompressor();
                Local_Compressor.ResetSampleRate(48000);

                var Local_Result = Local_Compressor.Transform(
                    DspCharacterization.Constant(Local_Length, Local_Amplitude), new DSP_Stream());

                for (int i = 0; i < Local_Result.Length; i++)
                    Assert.IsTrue(Math.Abs(Local_Result[i]) < 1.0d,
                        "Length " + Local_Length + " amplitude " + Local_Amplitude + " sample " + i +
                        " must be STRICTLY below full scale, was " + Local_Result[i]);
            }
        }
    }

    /// <summary>
    /// REGRESSION GUARD against the clamp perturbing normal audio, on BOTH implementation paths.
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
            var Local_Compressor = new DynamicRangeCompressor();
            Local_Compressor.ResetSampleRate(48000);

            var Local_Before = DspCharacterization.Sine(Local_Length, 1, 0.01);
            DspCharacterization.AssertExact(Local_Before,
                Local_Compressor.Transform(DspCharacterization.Copy(Local_Before), Local_Stream),
                "Length " + Local_Length + " quiet block before hot material");

            for (int Local_Block = 0; Local_Block < 4; Local_Block++)
                Local_Compressor.Transform(DspCharacterization.Constant(Local_Length, 8.0), Local_Stream);

            var Local_After = DspCharacterization.Sine(Local_Length, 1, 0.01);
            DspCharacterization.AssertExact(Local_After,
                Local_Compressor.Transform(DspCharacterization.Copy(Local_After), Local_Stream),
                "Length " + Local_Length + " quiet block after hot material");
        }
    }

    #endregion

    #region Aliasing / Mutation Contract

    /// <summary>
    /// Transform mutates the caller's array in place and returns that same instance, for both the
    /// stackalloc and the ArrayPool path; an empty block short-circuits.
    /// </summary>
    [TestMethod]
    public void Contract_Transform_MutatesInPlaceAndReturnsTheInputInstance()
    {
        var Local_Compressor = new DynamicRangeCompressor();
        Local_Compressor.ResetSampleRate(48000);

        foreach (int Local_Length in new[] { 8, 600 })
        {
            var Local_Input = DspCharacterization.Constant(Local_Length, 1.0);
            var Local_Result = Local_Compressor.Transform(Local_Input, new DSP_Stream());
            Assert.IsTrue(ReferenceEquals(Local_Input, Local_Result), "Length " + Local_Length);
            Assert.AreNotEqual(1.0d, Local_Input[0], "Length " + Local_Length + " was written in place");
        }

        var Local_Empty = new double[0];
        Assert.IsTrue(ReferenceEquals(Local_Empty, Local_Compressor.Transform(Local_Empty, new DSP_Stream())));
    }

    #endregion
}
