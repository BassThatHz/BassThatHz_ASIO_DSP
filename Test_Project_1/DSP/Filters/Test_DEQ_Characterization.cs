#nullable enable

namespace Test_Project_1;

#region Usings
using BassThatHz_ASIO_DSP_Processor;
using Test_Project_1.TestHelpers;
#endregion

/// <summary>
/// CHARACTERIZATION / GUARD SUITE for DSP\Filters\DEQ.cs.
///
/// DEQ is the most stateful filter in the suite. Every block it:
///   * runs the input through a detection biquad (band-pass, high-pass or low-pass depending on
///     Biquad_Type) whose z-delays carry between blocks,
///   * derives an envelope, smooths Gain_Linear with the attack/release coefficients,
///   * rewrites the OUTPUT biquad's coefficients through UpdateGain*, and
///   * runs the block through that (also stateful) output biquad.
///
/// So block N depends on every earlier block twice over. All four DEQ_Types, both Threshold_Types
/// and all three Biquad_Types are exercised below, and GainApplied - the value the GUI displays -
/// is pinned after every single block.
///
/// SETUP ORDER MATTERS: ResetSampleRate installs the sample rate, ApplySettings then designs every
/// biquad at that rate. Behavior with and without ApplySettings is pinned separately.
/// </summary>
[TestClass]
public class Test_DEQ_Characterization
{
    #region Without ApplySettings

    /// <summary>
    /// A freshly constructed DEQ has never had any of its biquads designed, so the detection biquad
    /// has all-zero coefficients and reports an amplitude of zero. For the default BoostBelow type
    /// that resolves to GainApplied == 0, which skips the output biquad entirely and leaves the
    /// block untouched. Pinned so the "not yet configured" path cannot drift either.
    /// </summary>
    [TestMethod]
    public void Golden_Transform_WithoutApplySettingsIsAPassThrough()
    {
        var Local_Deq = new DEQ();
        var Local_Result = Local_Deq.Transform(DspCharacterization.Sine(8, 1, 0.5), new DSP_Stream());

        DspCharacterization.AssertExact(
            new double[]
            {
                0.0d, 0.3535533905932738d, 0.5d, 0.3535533905932738d,
                6.123233995736766E-17d, -0.35355339059327373d, -0.5d, -0.35355339059327384d
            },
            Local_Result, "Unconfigured DEQ output");
        DspCharacterization.AssertExact(0.0d, Local_Deq.GainApplied, "GainApplied");
    }

    #endregion

    #region Multi-Block Stateful Sequence - BoostBelow / PEQ / Peak

    private static double[][] BuildBoostBelowInputs()
    {
        return new[]
        {
            DspCharacterization.Sine(8, 1, 0.5),
            DspCharacterization.Sine(8, 1, 0.01),
            DspCharacterization.Constant(8, 1.0),
            DspCharacterization.Alternating(8, 1.0),
            DspCharacterization.Constant(8, 0.0),
            DspCharacterization.Noise(8, 14000UL),
        };
    }

    private static DEQ BuildBoostBelow()
    {
        var Local_Deq = new DEQ { TargetGain_dB = 6, TargetFrequency = 1000, TargetQ = 2, Threshold_dB = -20 };
        Local_Deq.ResetSampleRate(48000);
        Local_Deq.ApplySettings();
        return Local_Deq;
    }

    /// <summary>
    /// Six consecutive blocks of the default BoostBelow / PEQ / Peak configuration, over signals
    /// that cross the threshold in both directions and include silence and full-scale content.
    /// </summary>
    [TestMethod]
    public void Stateful_Transform_BoostBelow_SixBlocksOfAudio()
    {
        double[][] Local_Expected =
        {
            new[] { 0.0d, 0.3535533905932738d, 0.5d, 0.3535533905932738d, 6.123233995736766E-17d, -0.35355339059327373d, -0.5d, -0.35355339059327384d },
            new[] { 0.0d, 0.007071067811865476d, 0.01d, 0.007071067811865476d, 1.2246467991473532E-18d, -0.0070710678118654745d, -0.01d, -0.007071067811865477d },
            new[] { 0.7274757954760239d, 0.758464489041549d, 0.7872680960699676d, 0.8135035843972368d, 0.8368481684371089d, 0.8570419867205905d, 0.873889606180459d, 0.8872603810068769d },
            new[] { 0.8970864907768323d, -0.5514386088731864d, 0.844176208331244d, -0.606855213642795d, 0.787293248976399d, -0.6641870010408312d, 0.7304917152691672d, -0.7195322820793939d },
            new[] { -0.04875825573297099d, -0.0711432056525344d, -0.09132746859450491d, -0.10907286622345108d, -0.12419276363220286d, -0.13655286293644492d, -0.14607108575019573d, -0.15271658765331672d },
            new[] { -0.7594897055031844d, -0.8766143169014646d, -0.7760266448835559d, -0.28048646145556966d, 0.35441255093612145d, 0.09375998106309114d, 0.04269947546659747d, -0.09804439527805411d },
        };

        var Local_Stream = new DSP_Stream();
        var Local_Deq = BuildBoostBelow();
        var Local_Inputs = BuildBoostBelowInputs();

        for (int Local_Block = 0; Local_Block < Local_Inputs.Length; Local_Block++)
        {
            var Local_Result = Local_Deq.Transform(DspCharacterization.Copy(Local_Inputs[Local_Block]), Local_Stream);
            DspCharacterization.AssertExact(Local_Expected[Local_Block], Local_Result, "Block " + Local_Block);
        }
    }

    /// <summary>
    /// GainApplied after each of the same six blocks. Blocks 0 and 1 report exactly zero (the
    /// signal is above the threshold so the boost is suppressed), and it only opens up once the
    /// detection envelope moves.
    /// </summary>
    [TestMethod]
    public void Stateful_Transform_BoostBelow_SixBlocksOfGainApplied()
    {
        double[] Local_Expected =
        {
            0.0d,
            0.0d,
            0.08667191703143978d,
            0.08488918867265664d,
            0.09492751323172563d,
            0.08108985510803184d,
        };

        var Local_Stream = new DSP_Stream();
        var Local_Deq = BuildBoostBelow();
        var Local_Inputs = BuildBoostBelowInputs();

        for (int Local_Block = 0; Local_Block < Local_Inputs.Length; Local_Block++)
        {
            Local_Deq.Transform(DspCharacterization.Copy(Local_Inputs[Local_Block]), Local_Stream);
            DspCharacterization.AssertExact(Local_Expected[Local_Block], Local_Deq.GainApplied,
                "Block " + Local_Block + " GainApplied");
        }
    }

    /// <summary>
    /// ApplySettings called mid-sequence redesigns every biquad (which zeroes their z-delays) but
    /// leaves the smoothed Gain_Linear alone. The two blocks that follow - including a block-size
    /// change - are pinned, together with the sign normalization ApplySettings performs on
    /// TargetGain_dB.
    /// </summary>
    [TestMethod]
    public void Stateful_ApplySettings_MidSequenceRedesignsTheBiquads()
    {
        double[][] Local_Expected =
        {
            new[] { 0.0d, 0.36358588245804274d, 0.015487871302811818d, -0.34919009782228094d },
            new[] { -0.002440852608600594d, 0.2542462713555823d, 0.3713604869506264d, 0.27934180361674943d, 0.031040057066618443d, -0.2292217916327511d, -0.35016780691121047d, -0.2621623060802629d },
        };

        var Local_Stream = new DSP_Stream();
        var Local_Deq = BuildBoostBelow();
        foreach (var Local_Input in BuildBoostBelowInputs())
            Local_Deq.Transform(DspCharacterization.Copy(Local_Input), Local_Stream);

        Local_Deq.ApplySettings();

        DspCharacterization.AssertExact(Local_Expected[0],
            Local_Deq.Transform(DspCharacterization.Sine(4, 1, 0.5), Local_Stream), "Block 0 (size drops to 4)");
        DspCharacterization.AssertExact(Local_Expected[1],
            Local_Deq.Transform(DspCharacterization.Sine(8, 1, 0.5), Local_Stream), "Block 1 (size back to 8)");

        DspCharacterization.AssertExact(0.07776567449053533d, Local_Deq.GainApplied, "Final GainApplied");
        DspCharacterization.AssertExact(6.0d, Local_Deq.TargetGain_dB,
            "A Boost type forces TargetGain_dB positive");
    }

    #endregion

    #region Other DEQ Types

    /// <summary>
    /// CutAbove / PEQ / Peak over four consecutive blocks, with GainApplied pinned after each.
    /// ApplySettings forces TargetGain_dB negative for a Cut type.
    /// </summary>
    [TestMethod]
    public void Stateful_Transform_CutAbove_FourBlocks()
    {
        double[][] Local_ExpectedAudio =
        {
            new[] { 0.0d, 0.48487292833238704d, 0.6650594678053706d, 0.4373450056824632d, -0.06244845568565971d, -0.5390729918898238d, -0.7108515099651749d, -0.4747209890596646d },
            new[] { 0.03377791787418749d, 0.5188419286746068d, 0.6982729934791677d, 0.46876506286440034d, -0.03359744976596868d, -0.5130687854811161d, -0.6874944551700108d, -0.4535985256239435d },
            new[] { 0.052306165654509185d, 0.5346853041895434d, 0.711597087176615d, 0.47985621985266513d, -0.02451280351435625d, -0.5059462024958228d, -0.6824656559303142d, -0.45084351709162274d },
            new[] { 0.05289453015481904d, 0.5332846349610683d, 0.7084485114363636d, 0.47522968990084496d, -0.030363201981196686d, -0.512813224434445d, -0.6901903268345462d, -0.4592883289503169d },
        };
        double[] Local_ExpectedGain =
        {
            -0.12701277205367204d, -0.101447883081607d, -0.11429892035720651d, -0.11723312888199838d,
        };

        var Local_Stream = new DSP_Stream();
        var Local_Deq = new DEQ
        {
            DEQ_Type = DEQ.DEQType.CutAbove,
            TargetGain_dB = -6,
            TargetFrequency = 1000,
            TargetQ = 2,
            Threshold_dB = -20
        };
        Local_Deq.ResetSampleRate(48000);
        Local_Deq.ApplySettings();

        for (int Local_Block = 0; Local_Block < Local_ExpectedAudio.Length; Local_Block++)
        {
            var Local_Result = Local_Deq.Transform(DspCharacterization.Sine(8, 1, 0.5), Local_Stream);
            DspCharacterization.AssertExact(Local_ExpectedAudio[Local_Block], Local_Result, "Block " + Local_Block);
            DspCharacterization.AssertExact(Local_ExpectedGain[Local_Block], Local_Deq.GainApplied,
                "Block " + Local_Block + " GainApplied");
        }

        DspCharacterization.AssertExact(-6.0d, Local_Deq.TargetGain_dB, "A Cut type forces TargetGain_dB negative");
    }

    /// <summary>
    /// RMS detection with a High_Shelf output biquad and BoostAbove, over four consecutive blocks.
    /// This is the only configuration that exercises the RMS branch of the detector AND
    /// UpdateGain_HighShelf, which rebuilds the coefficients from scratch every block.
    /// </summary>
    [TestMethod]
    public void Stateful_Transform_RmsHighShelfBoostAbove_FourBlocks()
    {
        double[][] Local_ExpectedAudio =
        {
            new[] { 0.0d, 0.35687639515302466d, 0.5040428125907414d, 0.3553404301764823d, -0.00205376876269886d, -0.3587042540793709d, -0.5056090166837169d, -0.3566354607351176d },
            new[] { 0.003213633844474484d, 0.3620550543363294d, 0.5088831988292918d, 0.3571316070347745d, -0.004661231663114773d, -0.3647582993576891d, -0.5122947761046031d, -0.36083306659690617d },
            new[] { 0.0031820535127270455d, 0.36546230976181404d, 0.5130560437098074d, 0.35903547087526133d, -0.006636798826760437d, -0.3698564347295601d, -0.5178403809962769d, -0.36380904181977414d },
            new[] { 0.00428071497829724d, 0.36984618465910435d, 0.5180840534476517d, 0.3617441849908535d, -0.007812742715646859d, -0.3741804165532735d, -0.5227159033028628d, -0.3663090204508343d },
        };
        double[] Local_ExpectedGain =
        {
            0.08946490398557796d, 0.17861220963166627d, 0.26751761799832063d, 0.35263055265377347d,
        };

        var Local_Stream = new DSP_Stream();
        var Local_Deq = new DEQ
        {
            Threshold_Type = DEQ.ThresholdType.RMS,
            Biquad_Type = DEQ.BiquadType.High_Shelf,
            DEQ_Type = DEQ.DEQType.BoostAbove,
            TargetGain_dB = 6,
            TargetFrequency = 2000,
            TargetSlope = 1,
            TargetQ = 1,
            Threshold_dB = -20
        };
        Local_Deq.ResetSampleRate(48000);
        Local_Deq.ApplySettings();

        for (int Local_Block = 0; Local_Block < Local_ExpectedAudio.Length; Local_Block++)
        {
            var Local_Result = Local_Deq.Transform(DspCharacterization.Sine(8, 1, 0.5), Local_Stream);
            DspCharacterization.AssertExact(Local_ExpectedAudio[Local_Block], Local_Result, "Block " + Local_Block);
            DspCharacterization.AssertExact(Local_ExpectedGain[Local_Block], Local_Deq.GainApplied,
                "Block " + Local_Block + " GainApplied");
        }
    }

    /// <summary>
    /// CutBelow with a Low_Shelf output biquad on a very quiet signal, over four consecutive
    /// blocks. Exercises the low-pass detection branch and UpdateGain_LowShelf.
    /// </summary>
    [TestMethod]
    public void Stateful_Transform_CutBelowLowShelf_FourBlocks()
    {
        double[][] Local_ExpectedAudio =
        {
            new[] { 0.0d, 0.007070391318993441d, 0.009997690824637787d, 0.007067127637609572d, -4.6117423824210135E-06d, -0.007074995138704273d, -0.010002283896582573d, -0.0070717072469076376d },
            new[] { -8.397824306678736E-07d, 0.007067392843227958d, 0.009991643702622226d, 0.007058088762372428d, -1.5638935406058277E-05d, -0.00708661610548632d, -0.010013498180939868d, -0.007082463479711659d },
            new[] { -1.2025887374764524E-05d, 0.0070544999772816d, 0.009976155114488246d, 0.007040052427737572d, -3.5238576476255556E-05d, -0.007106407821559752d, -0.010032500827090311d, -0.0071006349908069154d },
            new[] { -3.025332687542001E-05d, 0.007034945001395476d, 0.009954385041311848d, 0.007016106764875368d, -6.039389013353519E-05d, -0.007131424332155019d, -0.010056416135952932d, -0.0071234157538399056d },
        };
        double[] Local_ExpectedGain =
        {
            -0.08979195925084324d, -0.17864161037515508d, -0.26654930660138226d, -0.3535156052193196d,
        };

        var Local_Stream = new DSP_Stream();
        var Local_Deq = new DEQ
        {
            DEQ_Type = DEQ.DEQType.CutBelow,
            Biquad_Type = DEQ.BiquadType.Low_Shelf,
            TargetGain_dB = -6,
            TargetFrequency = 200,
            TargetSlope = 1,
            TargetQ = 1,
            Threshold_dB = -20
        };
        Local_Deq.ResetSampleRate(48000);
        Local_Deq.ApplySettings();

        for (int Local_Block = 0; Local_Block < Local_ExpectedAudio.Length; Local_Block++)
        {
            var Local_Result = Local_Deq.Transform(DspCharacterization.Sine(8, 1, 0.01), Local_Stream);
            DspCharacterization.AssertExact(Local_ExpectedAudio[Local_Block], Local_Result, "Block " + Local_Block);
            DspCharacterization.AssertExact(Local_ExpectedGain[Local_Block], Local_Deq.GainApplied,
                "Block " + Local_Block + " GainApplied");
        }
    }

    #endregion

    #region Property Tests

    /// <summary>
    /// ApplySettings normalizes the sign of TargetGain_dB according to the DEQ type, whichever sign
    /// the user typed.
    /// </summary>
    [TestMethod]
    public void Property_ApplySettings_NormalizesTheSignOfTargetGain()
    {
        (DEQ.DEQType Type, double Input, double Expected)[] Local_Cases =
        {
            (DEQ.DEQType.CutAbove, 6, -6), (DEQ.DEQType.CutAbove, -6, -6),
            (DEQ.DEQType.CutBelow, 6, -6), (DEQ.DEQType.CutBelow, -6, -6),
            (DEQ.DEQType.BoostAbove, 6, 6), (DEQ.DEQType.BoostAbove, -6, 6),
            (DEQ.DEQType.BoostBelow, 6, 6), (DEQ.DEQType.BoostBelow, -6, 6),
        };

        foreach (var Local_Case in Local_Cases)
        {
            var Local_Deq = new DEQ { DEQ_Type = Local_Case.Type, TargetGain_dB = Local_Case.Input, TargetFrequency = 1000, TargetQ = 1, TargetSlope = 1 };
            Local_Deq.ResetSampleRate(48000);
            Local_Deq.ApplySettings();
            DspCharacterization.AssertExact(Local_Case.Expected, Local_Deq.TargetGain_dB,
                Local_Case.Type + " with input " + Local_Case.Input);
        }
    }

    /// <summary>
    /// An unrecognized DEQ_Type or Biquad_Type is rejected loudly rather than silently ignored.
    /// </summary>
    [TestMethod]
    public void Contract_ApplySettings_RejectsUnknownEnumValues()
    {
        var Local_BadDeqType = new DEQ { DEQ_Type = (DEQ.DEQType)9999 };
        Assert.ThrowsExactly<System.NotSupportedException>(() => Local_BadDeqType.ApplySettings());

        var Local_BadBiquadType = new DEQ { Biquad_Type = (DEQ.BiquadType)9999, TargetFrequency = 1000, TargetQ = 1 };
        Local_BadBiquadType.ResetSampleRate(48000);
        Assert.ThrowsExactly<System.NotSupportedException>(() => Local_BadBiquadType.ApplySettings());
    }

    /// <summary>
    /// Silence produces no non-finite output, whatever the configuration.
    /// </summary>
    [TestMethod]
    public void Property_Transform_SilenceProducesFiniteOutput()
    {
        foreach (DEQ.DEQType Local_Type in System.Enum.GetValues<DEQ.DEQType>())
        {
            foreach (DEQ.BiquadType Local_BiquadType in System.Enum.GetValues<DEQ.BiquadType>())
            {
                var Local_Deq = new DEQ
                {
                    DEQ_Type = Local_Type,
                    Biquad_Type = Local_BiquadType,
                    TargetGain_dB = 6,
                    TargetFrequency = 1000,
                    TargetQ = 1,
                    TargetSlope = 1
                };
                Local_Deq.ResetSampleRate(48000);
                Local_Deq.ApplySettings();

                var Local_Stream = new DSP_Stream();
                for (int Local_Block = 0; Local_Block < 4; Local_Block++)
                {
                    var Local_Result = Local_Deq.Transform(new double[8], Local_Stream);
                    for (int i = 0; i < Local_Result.Length; i++)
                    {
                        Assert.IsFalse(double.IsNaN(Local_Result[i]) || double.IsInfinity(Local_Result[i]),
                            Local_Type + "/" + Local_BiquadType + " block " + Local_Block + " sample " + i);
                    }
                }
            }
        }
    }

    #endregion

    #region Aliasing / Mutation Contract

    /// <summary>
    /// Transform mutates the caller's array in place and returns that same instance; an empty block
    /// short-circuits before the detector runs.
    /// </summary>
    [TestMethod]
    public void Contract_Transform_MutatesInPlaceAndReturnsTheInputInstance()
    {
        var Local_Deq = BuildBoostBelow();
        var Local_Stream = new DSP_Stream();

        //Prime it so the output biquad is engaged, then check the aliasing on a block it modifies.
        Local_Deq.Transform(DspCharacterization.Constant(8, 1.0), Local_Stream);

        var Local_Input = DspCharacterization.Constant(8, 1.0);
        var Local_Before = DspCharacterization.Copy(Local_Input);
        var Local_Result = Local_Deq.Transform(Local_Input, Local_Stream);

        Assert.IsTrue(ReferenceEquals(Local_Input, Local_Result), "Transform returns the input instance");
        Assert.AreNotEqual(Local_Before[0], Local_Input[0], "The caller's array was written in place");

        var Local_Empty = new double[0];
        Assert.IsTrue(ReferenceEquals(Local_Empty, Local_Deq.Transform(Local_Empty, Local_Stream)));
    }

    #endregion
}
