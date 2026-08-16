#nullable enable

namespace Test_Project_1;

#region Usings
using BassThatHz_ASIO_DSP_Processor;
using NAudio.Dsp;
using Test_Project_1.TestHelpers;
#endregion

/// <summary>
/// CHARACTERIZATION / GUARD SUITE for DSP\Filters\GPEQ.cs.
///
/// GPEQ is a dispatcher: it walks its Filters list and chains each enabled child filter. It has no
/// state of its own, but its children do, so the multi-block sequences below pin the CHAIN's
/// carried state as well as the dispatch order and the FilterEnabled skipping.
/// </summary>
[TestClass]
public class Test_GPEQ_Characterization
{
    #region Helpers

    /// <summary>
    /// Builds the fixed three-band GPEQ used by the golden vectors: an enabled 1 kHz peaking EQ,
    /// an enabled 80 Hz high-pass, and a DISABLED 5 kHz low-pass that must be skipped.
    /// </summary>
    private static GPEQ BuildGoldenChain()
    {
        var Local_GPEQ = new GPEQ();

        var Local_Peq = new BiQuadFilter();
        Local_Peq.PeakingEQ(48000, 1000, 2.0, 6.0);
        Local_Peq.FilterEnabled = true;

        var Local_Hpf = new BiQuadFilter();
        Local_Hpf.HighPassFilter(48000, 80, 0.7071067811865476);
        Local_Hpf.FilterEnabled = true;

        var Local_DisabledLpf = new BiQuadFilter();
        Local_DisabledLpf.LowPassFilter(48000, 5000, 0.7071067811865476);
        Local_DisabledLpf.FilterEnabled = false;

        Local_GPEQ.Filters.Add(Local_Peq);
        Local_GPEQ.Filters.Add(Local_Hpf);
        Local_GPEQ.Filters.Add(Local_DisabledLpf);
        return Local_GPEQ;
    }

    #endregion

    #region Multi-Block Stateful Sequences

    /// <summary>
    /// Seven consecutive blocks through the three-band chain, with a block-size change on the last.
    /// </summary>
    [TestMethod]
    public void Stateful_Transform_SevenBlocksIncludingABlockSizeChange()
    {
        double[][] Local_Expected =
        {
            new[] { -0.846750410698404d, -0.0644474965176649d, 0.8510863461715139d, -0.5502870594615717d, 0.5622406255080525d, -0.14088889101250557d, 0.014981377759132553d, -0.3677306781937013d },
            new[] { -0.1483146826092265d, 0.7827651749350137d, 0.9806430646666617d, -0.5465991326792417d, -0.5964807945088583d, -0.2665950689139761d, 0.9706662112028371d, -0.9398066507351139d },
            new[] { 0.5492544171720499d, -0.40496969283880135d, -0.9852409953344093d, -0.6616346906954194d, 0.16757544569677818d, -0.42927386084379904d, -0.1321991662600193d, 0.4415811959642798d },
            new[] { -0.7869155105490004d, 0.38688299200891385d, -0.898214508643233d, -0.686885632750847d, -1.0059985500148065d, -0.5555142979968901d, 0.8372468389095078d, -0.10270406765033346d },
            new[] { -0.04801963918675933d, -0.7466915595184823d, -0.7681360343288123d, -0.6685231804423333d, -0.10600346474824485d, -0.5819897130711662d, -0.13132983209319843d, -0.6208512580367355d },
            new[] { 0.7137301929915234d, 0.16921394533786116d, -0.5651628215214008d, -0.587405806063138d, 0.8458246178965572d, -0.568116960625084d, 0.9584716881538413d, 0.963827310049016d },
            new[] { 0.42889731736330217d, -0.23450755525907718d, -0.2809451975526237d, 0.02377762010943668d },
        };

        var Local_Stream = new DSP_Stream();
        var Local_GPEQ = BuildGoldenChain();

        for (int Local_Block = 0; Local_Block < 6; Local_Block++)
        {
            var Local_Result = Local_GPEQ.Transform(DspCharacterization.Noise(8, (ulong)(9000 + Local_Block)), Local_Stream);
            DspCharacterization.AssertExact(Local_Expected[Local_Block], Local_Result, "Block " + Local_Block);
        }

        DspCharacterization.AssertExact(Local_Expected[6],
            Local_GPEQ.Transform(DspCharacterization.Noise(4, 9100UL), Local_Stream), "Block 6 (size drops to 4)");
    }

    /// <summary>
    /// ResetSampleRate is forwarded to every child - including the DISABLED one - and redesigning
    /// each child also zeroes its z-delays. The three blocks after the reset are pinned.
    /// </summary>
    [TestMethod]
    public void Stateful_ResetSampleRate_IsForwardedToChildrenAndResetsTheirState()
    {
        double[][] Local_Expected =
        {
            new[] { -0.8404701290658692d, -0.9766119947539746d, 0.04886140479363532d, 0.08803961471501098d, -0.7345237174453693d, -0.3194273381534616d, -0.09342447228916795d, -0.6282139900222357d },
            new[] { -0.18099886228390671d, -0.17709931531407197d, 0.12859450429951533d, 0.047757610607434275d, 0.09394207100214066d, -0.42784544307117817d, 0.8766183706681491d, 0.8305642519845045d },
            new[] { 0.5654131760492336d, 0.7056253554775043d, 0.2874042717611861d, 0.08203775151000248d, 0.9921046048929603d, -0.47175391884377504d, -0.10961043776814694d, 0.29748185518659087d },
        };

        var Local_Stream = new DSP_Stream();
        var Local_GPEQ = BuildGoldenChain();

        for (int Local_Block = 0; Local_Block < 6; Local_Block++)
            Local_GPEQ.Transform(DspCharacterization.Noise(8, (ulong)(9000 + Local_Block)), Local_Stream);
        Local_GPEQ.Transform(DspCharacterization.Noise(4, 9100UL), Local_Stream);

        Local_GPEQ.ResetSampleRate(96000);

        //Every child - enabled or not - has been redesigned at the new rate.
        foreach (var Local_Child in Local_GPEQ.Filters)
            DspCharacterization.AssertExact(96000.0d, ((BiQuadFilter)Local_Child).SampleRate, "Child sample rate");

        for (int Local_Block = 0; Local_Block < Local_Expected.Length; Local_Block++)
        {
            var Local_Result = Local_GPEQ.Transform(DspCharacterization.Noise(8, (ulong)(9200 + Local_Block)), Local_Stream);
            DspCharacterization.AssertExact(Local_Expected[Local_Block], Local_Result, "Post-reset block " + Local_Block);
        }
    }

    #endregion

    #region Property Tests

    /// <summary>
    /// GPEQ's output must be bit-identical to applying the enabled children by hand in list order -
    /// this pins both the composition and the skipping of disabled entries.
    /// </summary>
    [TestMethod]
    public void Property_Transform_EqualsManualChainingOfEnabledChildrenInListOrder()
    {
        var Local_Stream = new DSP_Stream();

        var Local_GPEQ = BuildGoldenChain();
        var Local_ChainResult = Local_GPEQ.Transform(DspCharacterization.Noise(16, 39000UL), Local_Stream);

        var Local_Peq = new BiQuadFilter();
        Local_Peq.PeakingEQ(48000, 1000, 2.0, 6.0);
        var Local_Hpf = new BiQuadFilter();
        Local_Hpf.HighPassFilter(48000, 80, 0.7071067811865476);

        var Local_Manual = DspCharacterization.Noise(16, 39000UL);
        Local_Manual = Local_Peq.Transform(Local_Manual, Local_Stream);
        Local_Manual = Local_Hpf.Transform(Local_Manual, Local_Stream);

        DspCharacterization.AssertExact(Local_Manual, Local_ChainResult,
            "GPEQ must be exactly the composition of its enabled children, in order");
    }

    /// <summary>
    /// Order matters: swapping two children changes the result. This guards against an
    /// "optimization" that reordered or parallelized the chain.
    /// </summary>
    [TestMethod]
    public void Property_Transform_ChildOrderIsSignificant()
    {
        var Local_Stream = new DSP_Stream();

        var Local_Forward = new GPEQ();
        var Local_A1 = new BiQuadFilter(); Local_A1.PeakingEQ(48000, 1000, 2.0, 12.0); Local_A1.FilterEnabled = true;
        var Local_B1 = new BiQuadFilter(); Local_B1.LowPassFilter(48000, 300, 0.5); Local_B1.FilterEnabled = true;
        Local_Forward.Filters.Add(Local_A1);
        Local_Forward.Filters.Add(Local_B1);

        var Local_Reverse = new GPEQ();
        var Local_A2 = new BiQuadFilter(); Local_A2.PeakingEQ(48000, 1000, 2.0, 12.0); Local_A2.FilterEnabled = true;
        var Local_B2 = new BiQuadFilter(); Local_B2.LowPassFilter(48000, 300, 0.5); Local_B2.FilterEnabled = true;
        Local_Reverse.Filters.Add(Local_B2);
        Local_Reverse.Filters.Add(Local_A2);

        var Local_ForwardResult = Local_Forward.Transform(DspCharacterization.Noise(16, 39100UL), Local_Stream);
        var Local_ReverseResult = Local_Reverse.Transform(DspCharacterization.Noise(16, 39100UL), Local_Stream);

        bool Local_AnyDifference = false;
        for (int i = 0; i < Local_ForwardResult.Length; i++)
        {
            if (Local_ForwardResult[i] != Local_ReverseResult[i])
            {
                Local_AnyDifference = true;
                break;
            }
        }
        Assert.IsTrue(Local_AnyDifference, "Reordering the chain must change the result - order is load bearing");
    }

    #endregion

    #region Guard Paths / Aliasing

    /// <summary>
    /// An empty GPEQ, and one whose children are all disabled or null, is a bit-exact pass-through
    /// returning the caller's instance.
    /// </summary>
    [TestMethod]
    public void Contract_Transform_EmptyOrFullyDisabledChainIsAPassThrough()
    {
        var Local_Stream = new DSP_Stream();
        var Local_Original = DspCharacterization.Noise(8, 9300UL);

        var Local_Empty = new GPEQ();
        var Local_EmptyInput = DspCharacterization.Copy(Local_Original);
        var Local_EmptyResult = Local_Empty.Transform(Local_EmptyInput, Local_Stream);
        Assert.IsTrue(ReferenceEquals(Local_EmptyInput, Local_EmptyResult), "Empty chain returns the input instance");
        DspCharacterization.AssertExact(
            new double[]
            {
                0.16601480595698748d, 0.5848936374465286d, 0.6815735357165975d, 0.4401071284364757d,
                -0.3444294168178512d, -0.35760075694748994d, 0.9034212173954337d, -0.7059780704466398d
            },
            Local_EmptyResult, "Empty chain must not alter the block");

        var Local_Disabled = new GPEQ();
        var Local_Child = new BiQuadFilter();
        Local_Child.LowPassFilter(48000, 500, 0.7071067811865476);
        Local_Child.FilterEnabled = false;
        Local_Disabled.Filters.Add(Local_Child);
        Local_Disabled.Filters.Add(null!);

        var Local_DisabledInput = DspCharacterization.Copy(Local_Original);
        DspCharacterization.AssertExact(Local_Original, Local_Disabled.Transform(Local_DisabledInput, Local_Stream),
            "Disabled and null children must be skipped without touching the block");
    }

    /// <summary>
    /// ResetSampleRate tolerates null children.
    /// </summary>
    [TestMethod]
    public void Contract_ResetSampleRate_ToleratesNullChildren()
    {
        var Local_GPEQ = new GPEQ();
        Local_GPEQ.Filters.Add(null!);
        Local_GPEQ.ResetSampleRate(48000);
        Assert.AreEqual(1, Local_GPEQ.Filters.Count);
    }

    #endregion
}
