#nullable enable

namespace Test_Project_1;

#region Usings
using BassThatHz_ASIO_DSP_Processor;
using NAudio.Dsp;
using System;
using Test_Project_1.TestHelpers;
#endregion

/// <summary>
/// CHARACTERIZATION / GUARD SUITE for DSP\Filters\BiQuadFilter.cs.
///
/// BiQuadFilter is the workhorse of the whole DSP chain (GPEQ, Basic_HPF_LPF and DEQ all delegate
/// to it), and it is stateful: x1/x2/y1/y2 carry between blocks. Two things are pinned:
///
/// 1. THE COEFFICIENTS produced by every designer method - a cheap, extremely sensitive drift
///    detector for the cookbook formulas.
/// 2. THE OUTPUT over eight consecutive blocks, including a block-size change and a mid-sequence
///    ResetSampleRate, which proves the z-delays are carried and reset exactly as they are today.
/// </summary>
[TestClass]
public class Test_BiQuadFilter_Characterization
{
    #region Helpers
    private static void AssertCoefficients(BiQuadFilter filter, double a0, double a1, double a2, double a3, double a4, string name)
    {
        DspCharacterization.AssertExact(a0, filter.a0, name + ".a0");
        DspCharacterization.AssertExact(a1, filter.a1, name + ".a1");
        DspCharacterization.AssertExact(a2, filter.a2, name + ".a2");
        DspCharacterization.AssertExact(a3, filter.a3, name + ".a3");
        DspCharacterization.AssertExact(a4, filter.a4, name + ".a4");
    }
    #endregion

    #region Golden Vectors - Designer Coefficients

    [TestMethod]
    public void Golden_Coefficients_AllDesignerMethodsAt48kHz()
    {
        var Local_Filter = new BiQuadFilter();

        Local_Filter.LowPassFilter(48000, 1000, 0.7071067811865476);
        AssertCoefficients(Local_Filter, 0.003916126660547383d, 0.007832253321094766d, 0.003916126660547383d, -1.815341082704568d, 0.8310055893467576d, "LowPassFilter");

        Local_Filter.LowPassFilter1st(48000, 1000, 0.5);
        AssertCoefficients(Local_Filter, 0.061511768503621556d, 0.061511768503621556d, 0.0d, -0.8769764629927569d, 0.0d, "LowPassFilter1st");

        Local_Filter.HighPassFilter(48000, 80, 0.7071067811865476);
        AssertCoefficients(Local_Filter, 0.9926225427561189d, -1.9852450855122379d, 0.9926225427561189d, -1.9851906578962613d, 0.9852995131282146d, "HighPassFilter");

        Local_Filter.HighPassFilter1st(48000, 80, 0.5);
        AssertCoefficients(Local_Filter, 0.994791237659377d, -0.994791237659377d, 0.0d, -0.9895824753187541d, 0.0d, "HighPassFilter1st");

        Local_Filter.PhaseInvertFilter(48000, 80, 0.5);
        AssertCoefficients(Local_Filter, -1.0d, 0.0d, 0.0d, 0.0d, 0.0d, "PhaseInvertFilter");

        Local_Filter.PeakingEQ(48000, 1000, 2.0, 6.0);
        AssertCoefficients(Local_Filter, 1.0224727682198582d, -1.938116580557223d, 0.9323677439107332d, -1.938116580557223d, 0.9548405121305915d, "PeakingEQ");

        Local_Filter.NotchFilter(48000, 1000, 4.0);
        AssertCoefficients(Local_Filter, 0.9839461568496084d, -1.951056722154107d, 0.9839461568496084d, -1.951056722154107d, 0.9678923136992169d, "NotchFilter");

        Local_Filter.AllPassFilter(48000, 1000, 1.0);
        AssertCoefficients(Local_Filter, 0.8774704646235392d, -1.8614084445321082d, 1.0d, -1.8614084445321082d, 0.8774704646235392d, "AllPassFilter");

        Local_Filter.BandPassFilterConstantPeakGain(48000, 1000, 1.0);
        AssertCoefficients(Local_Filter, 0.0612647676882304d, 0.0d, -0.0612647676882304d, -1.8614084445321082d, 0.8774704646235392d, "BandPassFilterConstantPeakGain");

        Local_Filter.LowShelf(48000, 200, 1.0, 6.0);
        AssertCoefficients(Local_Filter, 1.0064455778511419d, -1.9686123523200318d, 0.9631200582728409d, -1.9688501073857254d, 0.9693278810582894d, "LowShelf");

        Local_Filter.HighShelf(48000, 5000, 1.0, -6.0);
        AssertCoefficients(Local_Filter, 0.5847991561778957d, -0.5649439223247527d, 0.19980466359750232d, -1.236520927306563d, 0.456180824757208d, "HighShelf");
    }

    [TestMethod]
    public void Golden_Coefficients_UpdateGainVariants()
    {
        var Local_Peq = new BiQuadFilter();
        Local_Peq.PeakingEQ(48000, 1000, 2.0, 6.0);
        Local_Peq.UpdateGain(-3.0);
        AssertCoefficients(Local_Peq, 0.609048320817882d, -1.1544626767838313d, 0.5553761688951748d, -1.938116580557223d, 0.9548405121305915d, "UpdateGain");
        DspCharacterization.AssertExact(-3.0d, Local_Peq.Gain, "UpdateGain records the new gain");

        var Local_LowShelf = new BiQuadFilter();
        Local_LowShelf.LowShelf(48000, 200, 1.0, 6.0);
        Local_LowShelf.UpdateGain_LowShelf(-4.0);
        AssertCoefficients(Local_LowShelf, 0.99573762595106d, -1.958620726704083d, 0.9634163943098388d, -1.9584647668186006d, 0.959309980146381d, "UpdateGain_LowShelf");

        var Local_HighShelf = new BiQuadFilter();
        Local_HighShelf.HighShelf(48000, 5000, 1.0, -6.0);
        Local_HighShelf.UpdateGain_HighShelf(4.0);
        AssertCoefficients(Local_HighShelf, 1.4302186492615798d, -1.7103123763867043d, 0.6247270542103689d, -1.015541956132481d, 0.3601752832177258d, "UpdateGain_HighShelf");
    }

    [TestMethod]
    public void Golden_Coefficients_ChangeSampleRateRedesignsInPlace()
    {
        var Local_Filter = new BiQuadFilter();
        Local_Filter.LowPassFilter(48000, 1000, 0.7071067811865476);
        Local_Filter.ChangeSampleRate(96000);
        AssertCoefficients(Local_Filter, 0.0010232176384709138d, 0.0020464352769418276d, 0.0010232176384709138d, -1.9075016260460762d, 0.91159449659996d, "ChangeSampleRate(96000)");
        DspCharacterization.AssertExact(96000.0d, Local_Filter.SampleRate, "SampleRate is updated");
    }

    /// <summary>
    /// ChangeSampleRate on a filter whose type was never set through a designer method still
    /// dispatches on the default enum value (PEQ) rather than throwing.
    /// </summary>
    [TestMethod]
    public void Contract_ChangeSampleRate_UnknownTypeThrows()
    {
        var Local_Filter = new BiQuadFilter();
        Local_Filter.BiQuadFilterType = (BiQuadFilter.BiQuadFilterTypes)9999;
        Assert.ThrowsExactly<NotSupportedException>(() => Local_Filter.ChangeSampleRate(48000));
    }

    #endregion

    #region Multi-Block Stateful Sequences

    /// <summary>
    /// Eight consecutive blocks through a 1 kHz low-pass at 48 kHz. Blocks 0-5 are noise, block 6
    /// is a SHORTER block (a block-size change), and block 7 is full-scale alternating +/-1, the
    /// worst case for a feedback path.
    ///
    /// Every block is pinned, not just the first: the biquad's z-delays make block N depend on all
    /// of 0..N-1, so this is the sequence that actually proves the state handling is unchanged.
    /// </summary>
    [TestMethod]
    public void Stateful_Transform_EightBlocksIncludingBlockSizeChangeAndFullScale()
    {
        double[][] Local_Expected =
        {
            new[] { -0.003275286870604726d, -0.01592312855253768d, -0.033510440643824684d, -0.04204254241125373d, -0.041752827564303485d, -0.04611725084193517d, -0.05500326332346015d, -0.05944239198546713d },
            new[] { -0.061013548765639546d, -0.06325040156102874d, -0.06201900909826923d, -0.05074241691058045d, -0.030580211922576497d, -0.012770918069218547d, -0.0055859383646243085d, -0.00841909172513297d },
            new[] { -0.015417581676706888d, -0.016557016785100888d, -0.005719336710265794d, 0.016381900210381276d, 0.04776305166758331d, 0.0795027244320142d, 0.10281762227354715d, 0.12421218169171296d },
            new[] { 0.14497130685762966d, 0.1550434858238359d, 0.15843903890036087d, 0.1676757860290913d, 0.18143935266950223d, 0.19444706761581262d, 0.20639417386977355d, 0.21357680166432064d },
            new[] { 0.21516322673499358d, 0.2145250631810347d, 0.20968201509109322d, 0.19932890864552238d, 0.19176018910544904d, 0.19271356445279467d, 0.2006697180819218d, 0.20931500833985725d },
            new[] { 0.21405327350640202d, 0.21454124045446538d, 0.20439500927114498d, 0.18560916567686214d, 0.16669052962129458d, 0.148774838372429d, 0.13207358050467266d, 0.11816019094721918d },
            new[] { 0.1108178116846134d, 0.11065234872369561d, 0.11199269713344974d, 0.10549720208642226d },
            new[] { 0.09338654619306336d, 0.08261346551510976d, 0.072366876077951d, 0.06271831157293305d, 0.053717849131869264d, 0.045396950932109395d, 0.03777111718031238d, 0.030842340793185985d },
        };

        var Local_Stream = new DSP_Stream();
        var Local_Filter = new BiQuadFilter();
        Local_Filter.LowPassFilter(48000, 1000, 0.7071067811865476);

        for (int Local_Block = 0; Local_Block < 6; Local_Block++)
        {
            var Local_Result = Local_Filter.Transform(DspCharacterization.Noise(8, (ulong)(7000 + Local_Block)), Local_Stream);
            DspCharacterization.AssertExact(Local_Expected[Local_Block], Local_Result, "Block " + Local_Block);
        }

        DspCharacterization.AssertExact(Local_Expected[6],
            Local_Filter.Transform(DspCharacterization.Noise(4, 7100UL), Local_Stream), "Block 6 (size drops to 4)");
        DspCharacterization.AssertExact(Local_Expected[7],
            Local_Filter.Transform(DspCharacterization.Alternating(8, 1.0), Local_Stream), "Block 7 (full-scale alternating)");
    }

    /// <summary>
    /// ResetSampleRate mid-sequence redesigns the filter AND calls Init(), zeroing the z-delays.
    /// The three blocks that follow are pinned, so an optimization that skipped the state reset (or
    /// kept it when it should not) is caught.
    /// </summary>
    [TestMethod]
    public void Stateful_ResetSampleRate_MidSequenceClearsTheZDelays()
    {
        double[][] Local_Expected =
        {
            new[] { -0.0008555658540516835d, -0.0031298590160976523d, -0.005690865596188716d, -0.008421183697088077d, -0.011922116319510574d, -0.016462330694682007d, -0.021707736334812718d, -0.026371223669706814d },
            new[] { -0.03073434433182337d, -0.03624560765960855d, -0.04325195988725222d, -0.05095440054053217d, -0.05795808794976256d, -0.06193206265628764d, -0.06286128268951499d, -0.06424287225348092d },
            new[] { -0.06701716950625813d, -0.06927978981467547d, -0.07072670169044713d, -0.07227580389752372d, -0.07477358955887037d, -0.07713957880671056d, -0.07701969338985179d, -0.07411525451572926d },
        };

        var Local_Stream = new DSP_Stream();
        var Local_Filter = new BiQuadFilter();
        Local_Filter.LowPassFilter(48000, 1000, 0.7071067811865476);

        for (int Local_Block = 0; Local_Block < 6; Local_Block++)
            Local_Filter.Transform(DspCharacterization.Noise(8, (ulong)(7000 + Local_Block)), Local_Stream);
        Local_Filter.Transform(DspCharacterization.Noise(4, 7100UL), Local_Stream);
        Local_Filter.Transform(DspCharacterization.Alternating(8, 1.0), Local_Stream);

        Local_Filter.ResetSampleRate(96000);

        for (int Local_Block = 0; Local_Block < Local_Expected.Length; Local_Block++)
        {
            var Local_Result = Local_Filter.Transform(DspCharacterization.Noise(8, (ulong)(7200 + Local_Block)), Local_Stream);
            DspCharacterization.AssertExact(Local_Expected[Local_Block], Local_Result, "Post-reset block " + Local_Block);
        }
    }

    /// <summary>
    /// Splitting a block in two and running it through a fresh filter gives bit-identical results
    /// to running it whole - i.e. the state hand-off across the block boundary is exact.
    /// </summary>
    [TestMethod]
    public void Property_Transform_IsBlockSizeIndependent()
    {
        var Local_Stream = new DSP_Stream();
        var Local_Whole = DspCharacterization.Noise(64, 37000UL);

        var Local_A = new BiQuadFilter();
        Local_A.HighPassFilter(48000, 120, 0.7071067811865476);
        var Local_WholeResult = Local_A.Transform(DspCharacterization.Copy(Local_Whole), Local_Stream);

        var Local_B = new BiQuadFilter();
        Local_B.HighPassFilter(48000, 120, 0.7071067811865476);
        var Local_Split = new double[64];
        int Local_Offset = 0;
        foreach (int Local_ChunkSize in new[] { 8, 16, 4, 32, 4 })
        {
            var Local_Chunk = new double[Local_ChunkSize];
            Array.Copy(Local_Whole, Local_Offset, Local_Chunk, 0, Local_ChunkSize);
            var Local_ChunkResult = Local_B.Transform(Local_Chunk, Local_Stream);
            Array.Copy(Local_ChunkResult, 0, Local_Split, Local_Offset, Local_ChunkSize);
            Local_Offset += Local_ChunkSize;
        }

        DspCharacterization.AssertExact(Local_WholeResult, Local_Split,
            "Chunked processing must be bit-identical to whole-block processing");
    }

    #endregion

    #region Zero-Denominator Guard

    /// <summary>
    /// FIXED DEFECT - this test asserts the CORRECT behavior (it previously PINNED the defect as
    /// Bug_SetCoefficients_ZeroDenominatorGuardIsIneffective).
    ///
    /// SetCoefficients used to guard against a zero denominator by storing EPS (1e-12) into the
    /// FIELD aa0, while every division that followed used the PARAMETER aa0 - which the parameter
    /// name shadows - and that parameter was still zero:
    ///
    ///     this.aa0 = aa0 == 0 ? EPS : aa0;   // field was guarded
    ///     ...
    ///     this.a0 = b0 / aa0;                // parameter was NOT
    ///
    /// so all five precomputed coefficients became +/-Infinity and the filter emitted non-finite
    /// audio for the rest of its life. The guard was dead code.
    ///
    /// The guarded denominator is now computed once into a local and every division uses it, so a
    /// zero denominator degrades to the EPS-normalized coefficients instead of poisoning the stream.
    /// The expected values below are the analytic b/EPS and aa/EPS quotients, each exactly
    /// representable as a double: 3/1e-12 = 3e12, 4/1e-12 = 4e12, 5/1e-12 = 5e12, 1/1e-12 = 1e12,
    /// 2/1e-12 = 2e12.
    /// </summary>
    [TestMethod]
    public void Contract_SetCoefficients_ZeroDenominatorGuardIsEffective()
    {
        var Local_Filter = new BiQuadFilter();
        Local_Filter.SetCoefficients(0, 1, 2, 3, 4, 5);

        DspCharacterization.AssertExact(1E-12d, Local_Filter.aa0, "The FIELD receives the EPS guard");

        //Every precomputed coefficient must be FINITE - no Infinity, no NaN.
        Assert.IsTrue(double.IsFinite(Local_Filter.a0), "a0 must be finite");
        Assert.IsTrue(double.IsFinite(Local_Filter.a1), "a1 must be finite");
        Assert.IsTrue(double.IsFinite(Local_Filter.a2), "a2 must be finite");
        Assert.IsTrue(double.IsFinite(Local_Filter.a3), "a3 must be finite");
        Assert.IsTrue(double.IsFinite(Local_Filter.a4), "a4 must be finite");

        //And they are exactly the EPS-normalized quotients.
        AssertCoefficients(Local_Filter, 3E12d, 4E12d, 5E12d, 1E12d, 2E12d, "SetCoefficients(aa0 == 0)");
    }

    /// <summary>
    /// The zero-denominator guard must also keep the AUDIO finite: with the defect the poisoned
    /// +/-Infinity coefficients produced Infinity/NaN output, and because Infinity/NaN is written
    /// straight back into the x1/x2/y1/y2 z-delays the filter stayed poisoned for every later block.
    ///
    /// A purely feed-forward degenerate design (aa1 = aa2 = 0) is used so that the EPS-normalized
    /// filter has no feedback path and is therefore bounded for a bounded input - which isolates
    /// exactly what this fix is responsible for: finiteness, not stability.
    /// </summary>
    [TestMethod]
    public void Contract_SetCoefficients_ZeroDenominator_TransformStaysFiniteAcrossBlocks()
    {
        var Local_Stream = new DSP_Stream();
        var Local_Filter = new BiQuadFilter();
        Local_Filter.SetCoefficients(0, 0, 0, 0.5, 0.25, 0.125);

        AssertCoefficients(Local_Filter, 5E11d, 2.5E11d, 1.25E11d, 0.0d, 0.0d, "SetCoefficients(aa0 == 0, feed-forward)");

        //Six blocks through the array overload (mutates in place).
        for (int Local_Block = 0; Local_Block < 6; Local_Block++)
        {
            var Local_Result = Local_Filter.Transform(DspCharacterization.Noise(8, (ulong)(7400 + Local_Block)), Local_Stream);
            for (int i = 0; i < Local_Result.Length; i++)
                Assert.IsTrue(double.IsFinite(Local_Result[i]),
                    "Transform block " + Local_Block + " sample " + i + " must stay finite, was " + DspCharacterization.ToLiteral(Local_Result[i]));
        }

        //And six more through the span overload, proving the carried state was never poisoned.
        for (int Local_Block = 0; Local_Block < 6; Local_Block++)
        {
            var Local_Buffer = DspCharacterization.Noise(8, (ulong)(7500 + Local_Block));
            Local_Filter.TransformInPlace(Local_Buffer, Local_Stream);
            for (int i = 0; i < Local_Buffer.Length; i++)
                Assert.IsTrue(double.IsFinite(Local_Buffer[i]),
                    "TransformInPlace block " + Local_Block + " sample " + i + " must stay finite, was " + DspCharacterization.ToLiteral(Local_Buffer[i]));
        }
    }

    /// <summary>
    /// REGRESSION GUARD FOR THE FIX ITSELF: the guard must not perturb the normal path. For a
    /// non-zero denominator the guarded local is bit-identical to the parameter, so every quotient
    /// must be bit-identical to the pre-fix values pinned below (captured from the code as it was
    /// before the fix; this test therefore passes against both the old and the new code, while the
    /// two tests above fail against the old code).
    /// </summary>
    [TestMethod]
    public void Contract_SetCoefficients_NonZeroDenominatorIsBitIdenticalToPreFixBehavior()
    {
        var Local_Filter = new BiQuadFilter();
        Local_Filter.SetCoefficients(
            1.0391534619132452d, -1.9711289410505657d, 0.9614398094219848d,
            0.9801457678338075d, -1.960291535667615d, 0.9801457678338075d);

        DspCharacterization.AssertExact(1.0391534619132452d, Local_Filter.aa0, "aa0 is stored unguarded when non-zero");
        AssertCoefficients(Local_Filter,
            0.94321561132001108d, -1.8864312226400222d, 0.94321561132001108d,
            -1.8968602937831789d, 0.92521446028945786d, "SetCoefficients(non-zero aa0)");

        //An exactly-representable denominator too, where any change in the divide would be obvious.
        var Local_Exact = new BiQuadFilter();
        Local_Exact.SetCoefficients(2, 3, 4, 5, 6, 7);
        DspCharacterization.AssertExact(2.0d, Local_Exact.aa0, "aa0 == 2 is stored unguarded");
        AssertCoefficients(Local_Exact, 2.5d, 3.0d, 3.5d, 1.5d, 2.0d, "SetCoefficients(2, 3, 4, 5, 6, 7)");

        //A negative denominator must not be mistaken for the zero case.
        var Local_Negative = new BiQuadFilter();
        Local_Negative.SetCoefficients(-4, 2, 1, 8, 4, 2);
        DspCharacterization.AssertExact(-4.0d, Local_Negative.aa0, "aa0 == -4 is stored unguarded");
        AssertCoefficients(Local_Negative, -2.0d, -1.0d, -0.5d, -0.5d, -0.25d, "SetCoefficients(-4, 2, 1, 8, 4, 2)");
    }

    #endregion

    #region Aliasing / Mutation Contract

    /// <summary>
    /// A default-constructed BiQuadFilter has all-zero coefficients, so it OUTPUTS SILENCE - it is
    /// not a pass-through. Basic_HPF_LPF relies on its FilterEnabled flag rather than on this.
    /// </summary>
    [TestMethod]
    public void Contract_DefaultConstructed_OutputsSilenceNotPassThrough()
    {
        //Pinned bit-exactly, INCLUDING the sign of zero: with all-zero coefficients the products
        //0 * sample carry the sample's sign, so a negative last sample yields -0.0. An optimization
        //that short-circuited the multiply, or reordered the accumulation, would change that bit.
        DspCharacterization.AssertExact(
            new double[] { 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, -0.0d },
            new BiQuadFilter().Transform(DspCharacterization.Noise(8, 7300UL), new DSP_Stream()),
            "A default-constructed biquad outputs silence, not a pass-through");
    }

    /// <summary>
    /// Transform(double[]) mutates in place and returns the caller's instance; the
    /// ReadOnlySpan/Span overload writes to the separate output buffer and leaves the input alone.
    /// </summary>
    [TestMethod]
    public void Contract_Transform_AliasingBehavior()
    {
        var Local_Stream = new DSP_Stream();

        var Local_InPlaceFilter = new BiQuadFilter();
        Local_InPlaceFilter.LowPassFilter(48000, 1000, 0.7071067811865476);
        var Local_Input = DspCharacterization.Noise(8, 37100UL);
        var Local_Result = Local_InPlaceFilter.Transform(Local_Input, Local_Stream);
        Assert.IsTrue(ReferenceEquals(Local_Input, Local_Result), "Transform(double[]) returns the input instance");

        var Local_SpanFilter = new BiQuadFilter();
        Local_SpanFilter.LowPassFilter(48000, 1000, 0.7071067811865476);
        var Local_SpanInput = DspCharacterization.Noise(8, 37100UL);
        var Local_SpanBefore = DspCharacterization.Copy(Local_SpanInput);
        var Local_SpanOutput = new double[8];
        Local_SpanFilter.Transform(Local_SpanInput, Local_SpanOutput, Local_Stream);

        DspCharacterization.AssertExact(Local_SpanBefore, Local_SpanInput, "The span overload must not touch its input");
        DspCharacterization.AssertExact(Local_Result, Local_SpanOutput, "Both overloads must produce identical audio");
    }

    /// <summary>
    /// The span overload rejects mismatched lengths.
    /// </summary>
    [TestMethod]
    public void Contract_Transform_SpanOverloadRejectsLengthMismatch()
    {
        var Local_Filter = new BiQuadFilter();
        Local_Filter.LowPassFilter(48000, 1000, 0.7071067811865476);
        var Local_Stream = new DSP_Stream();

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            var Local_Input = new double[8];
            var Local_Output = new double[4];
            Local_Filter.Transform(Local_Input, Local_Output, Local_Stream);
        });
    }

    #endregion
}
