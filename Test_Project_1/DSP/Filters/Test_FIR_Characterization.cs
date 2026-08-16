#nullable enable

namespace Test_Project_1;

#region Usings
using BassThatHz_ASIO_DSP_Processor;
using Test_Project_1.TestHelpers;
#endregion

/// <summary>
/// CHARACTERIZATION / GUARD SUITE for DSP\Filters\FIR.cs.
///
/// FIR is an overlap-SAVE fast convolver: every block slides the OverlapBuffer forward, appends the
/// new samples, forward-transforms the whole FFTSize window, multiplies by the cached taps
/// spectrum, inverse-transforms and takes the tail. The OverlapBuffer therefore carries the last
/// (FFTSize - blockLength) samples between calls, and the cached FFT instance is reused, so the
/// only meaningful test is a multi-block one.
///
/// FFTSize is set to 32 throughout so the golden vectors stay readable; the code path is identical
/// at the production default of 8192.
/// </summary>
[TestClass]
public class Test_FIR_Characterization
{
    #region Golden Constants

    private static readonly double[] Golden_Taps = { 0.5, 0.25, 0.125, 0.0625 };

    #endregion

    #region Without Taps

    /// <summary>
    /// Before SetTaps there is no cached taps spectrum, so Transform is a bit-exact pass-through
    /// returning the caller's instance.
    /// </summary>
    [TestMethod]
    public void Golden_Transform_WithoutTapsIsAnExactPassThrough()
    {
        var Local_Original = DspCharacterization.Noise(8, 18000UL);
        var Local_Input = DspCharacterization.Copy(Local_Original);
        var Local_Filter = new FIR();

        var Local_Result = Local_Filter.Transform(Local_Input, new DSP_Stream());

        Assert.IsTrue(ReferenceEquals(Local_Input, Local_Result), "Returns the input instance");
        DspCharacterization.AssertExact(
            new double[]
            {
                -0.8250068911458646d, 0.7159706499863778d, -0.4907851796537963d, -0.8988423260689968d,
                -0.6424535875165029d, -0.41890013387793834d, 0.8586087971888292d, -0.45179001164865085d
            },
            Local_Result, "No taps means no processing at all");
        Assert.IsNull(Local_Filter.Taps);
    }

    #endregion

    #region Multi-Block Stateful Sequences

    /// <summary>
    /// Six consecutive 8-sample blocks through a 4-tap low-pass at FFTSize 32.
    ///
    /// Because the overlap window is 24 samples, block 0's output already reflects the zero-filled
    /// history and blocks 1-5 progressively pull in real history - so an off-by-one in the
    /// overlap-save slide shows up immediately, and only in the LATER blocks.
    /// </summary>
    [TestMethod]
    public void Stateful_Transform_SixConsecutiveBlocks()
    {
        double[][] Local_Expected =
        {
            new[] { 0.08754815386900536d, 0.17262766062044932d, 0.14452837430303694d, -0.2124382945870343d, -0.2507224372817921d, -0.3776996884506417d, -0.28068277658566737d, -0.40445290246368343d },
            new[] { 0.23901168927979943d, -0.3286640690353901d, -0.05756926147663601d, -0.31422083695894204d, 0.08864571191647072d, -0.2313457570520974d, 0.26462944543541533d, 0.5787901552364958d },
            new[] { 0.04989486208469188d, -0.01174202813733477d, 0.11425126713681073d, -0.29100337933084974d, -0.4469861388852665d, -0.584991825653553d, -0.440058332543502d, -0.06296678706332448d },
            new[] { 0.11077803488958415d, 0.43018001276072015d, 0.4110717957502574d, -0.14278592170275758d, 0.017382010312996424d, -0.4386378942550085d, 0.10525388947758113d, -0.0797237293631447d },
            new[] { 0.4216612076944766d, -0.0028979463412244846d, 0.2078923243637042d, -0.24456846407466515d, -0.6432498404887408d, -0.7922839628564641d, -0.5994338885013364d, -0.721480671662965d },
            new[] { -0.5174556195006309d, -0.06097590544316897d, 0.25471285297715107d, -0.2213510064465728d, -0.1788816912904778d, 0.35406996854208d, 0.4458783335197467d, 0.5117623860372145d },
        };

        var Local_Stream = new DSP_Stream();
        var Local_Filter = new FIR { FFTSize = 32 };
        Local_Filter.SetTaps(DspCharacterization.Copy(Golden_Taps));

        for (int Local_Block = 0; Local_Block < Local_Expected.Length; Local_Block++)
        {
            var Local_Result = Local_Filter.Transform(DspCharacterization.Noise(8, (ulong)(18100 + Local_Block)), Local_Stream);
            DspCharacterization.AssertExact(Local_Expected[Local_Block], Local_Result, "Block " + Local_Block);
        }
    }

    /// <summary>
    /// Four consecutive blocks with a UNIT IMPULSE as the taps. Mathematically this is the identity
    /// convolution, so a monotonically increasing ramp must come back out as itself.
    ///
    /// TOLERANCE NOTE: this test is EXACT like every other golden vector. The values are NOT the
    /// clean integers of the input - they are the integers as reconstructed through a forward and
    /// inverse FFT, e.g. 1.9999999999999991 rather than 2. That residue is the whole point: it is a
    /// fingerprint of the exact arithmetic in the transform pair, and any change to it will be
    /// caught here.
    /// </summary>
    [TestMethod]
    public void Stateful_Transform_UnitImpulseTapsReproduceTheRampToTheLastBit()
    {
        double[][] Local_Expected =
        {
            new[] { 1.0d, 1.9999999999999991d, 3.0d, 3.9999999999999982d, 5.0d, 5.999999999999998d, 7.0d, 7.999999999999997d },
            new[] { 9.0d, 9.999999999999996d, 11.0d, 11.999999999999996d, 12.999999999999998d, 13.999999999999996d, 15.0d, 15.999999999999995d },
            new[] { 17.0d, 17.999999999999996d, 19.0d, 19.999999999999993d, 21.0d, 21.999999999999996d, 23.0d, 23.999999999999996d },
            new[] { 25.0d, 25.999999999999993d, 26.999999999999996d, 27.999999999999996d, 29.0d, 29.999999999999996d, 31.0d, 31.999999999999996d },
        };

        var Local_Stream = new DSP_Stream();
        var Local_Filter = new FIR { FFTSize = 32 };
        Local_Filter.SetTaps(DspCharacterization.Impulse(4, 0, 1.0));

        for (int Local_Block = 0; Local_Block < Local_Expected.Length; Local_Block++)
        {
            var Local_Result = Local_Filter.Transform(DspCharacterization.Ramp(8, 1 + Local_Block * 8, 1), Local_Stream);
            DspCharacterization.AssertExact(Local_Expected[Local_Block], Local_Result, "Block " + Local_Block);
        }
    }

    /// <summary>
    /// A block-size change mid-sequence. Overlap-save derives its slide length from the CURRENT
    /// block length, so shrinking and then re-growing the block is the case most likely to be
    /// broken by an optimization that caches the overlap size.
    /// </summary>
    [TestMethod]
    public void Stateful_Transform_BlockSizeChangeMidSequence()
    {
        double[][] Local_Expected =
        {
            new[] { 0.0876513527528806d, -0.28558422255209737d, -0.4773632996438157d, -0.1939467689685695d, 0.1229074023707768d, -0.23191340874176186d, -0.21823837236911198d, -0.5058413624277668d },
            new[] { 0.16564625025828422d, 0.18074411057566825d, -0.19346353878728467d, -0.04572931134047732d },
            new[] { -0.2722530127252255d, 0.34498728590405503d, -0.057784457924997104d, -0.022511853712384938d, 0.05164370076730246d, -0.43920554594467304d, -0.3776139283269465d, -0.16435524702740775d },
        };

        var Local_Stream = new DSP_Stream();
        var Local_Filter = new FIR { FFTSize = 32 };
        Local_Filter.SetTaps(DspCharacterization.Copy(Golden_Taps));

        DspCharacterization.AssertExact(Local_Expected[0],
            Local_Filter.Transform(DspCharacterization.Noise(8, 18300UL), Local_Stream), "Block 0 (8 samples)");
        DspCharacterization.AssertExact(Local_Expected[1],
            Local_Filter.Transform(DspCharacterization.Noise(4, 18301UL), Local_Stream), "Block 1 (4 samples)");
        DspCharacterization.AssertExact(Local_Expected[2],
            Local_Filter.Transform(DspCharacterization.Noise(8, 18302UL), Local_Stream), "Block 2 (back to 8 samples)");
    }

    /// <summary>
    /// SetTaps mid-sequence rebuilds the taps spectrum and RESETS the overlap buffer to zeros, so
    /// the next block behaves as if the filter had just started. With unit-impulse taps the output
    /// is therefore the input again - modulo the FFT round-trip residue, which is pinned exactly.
    /// </summary>
    [TestMethod]
    public void Stateful_SetTaps_MidSequenceResetsTheOverlapBuffer()
    {
        var Local_Stream = new DSP_Stream();
        var Local_Filter = new FIR { FFTSize = 32 };
        Local_Filter.SetTaps(DspCharacterization.Copy(Golden_Taps));

        Local_Filter.Transform(DspCharacterization.Noise(8, 18300UL), Local_Stream);
        Local_Filter.Transform(DspCharacterization.Noise(4, 18301UL), Local_Stream);
        Local_Filter.Transform(DspCharacterization.Noise(8, 18302UL), Local_Stream);

        Local_Filter.SetTaps(new double[] { 1.0, 0.0, 0.0, 0.0 });

        DspCharacterization.AssertExact(
            new double[]
            {
                -0.8245940956103633d, 0.8829167195284399d, -0.06192810909611327d, 0.4189071242985676d,
                0.815108519577812d, -0.6975805650011774d, 0.7186174215471464d, -0.8998733192901223d
            },
            Local_Filter.Transform(DspCharacterization.Noise(8, 18400UL), Local_Stream),
            "The first block after SetTaps reproduces the input through a clean overlap buffer");

        //For reference, the untouched input the block above reconstructs.
        DspCharacterization.AssertExact(
            new double[]
            {
                -0.8245940956103635d, 0.88291671952844d, -0.06192810909611324d, 0.4189071242985676d,
                0.815108519577812d, -0.6975805650011775d, 0.7186174215471464d, -0.8998733192901225d
            },
            DspCharacterization.Noise(8, 18400UL), "The deterministic source itself must not drift");
    }

    /// <summary>
    /// SetTaps replaces the public Taps array by reference and allocates the overlap buffer at
    /// FFTSize.
    /// </summary>
    [TestMethod]
    public void Contract_SetTaps_StoresTheTapsByReference()
    {
        var Local_Filter = new FIR { FFTSize = 32 };
        var Local_Taps = DspCharacterization.Copy(Golden_Taps);
        Local_Filter.SetTaps(Local_Taps);

        Assert.IsTrue(ReferenceEquals(Local_Taps, Local_Filter.Taps), "Taps is stored by reference, not copied");
        DspCharacterization.AssertExact(Golden_Taps, Local_Filter.Taps!, "Taps contents");
    }

    #endregion

    #region Property Tests

    /// <summary>
    /// Silence in, silence out - to within the transform's own noise floor.
    ///
    /// TOLERANCE NOTE: this is one of the few places a tolerance is unavoidable. The forward and
    /// inverse transforms of an all-zero window do return exact zeros here, but asserting exactness
    /// on a value that is mathematically zero and numerically a sum of signed products would make
    /// the guard brittle for no benefit; 1e-15 is far below any audible or even measurable level.
    /// </summary>
    [TestMethod]
    public void Property_Transform_SilenceStaysSilent()
    {
        var Local_Stream = new DSP_Stream();
        var Local_Filter = new FIR { FFTSize = 32 };
        Local_Filter.SetTaps(DspCharacterization.Copy(Golden_Taps));

        for (int Local_Block = 0; Local_Block < 6; Local_Block++)
        {
            var Local_Result = Local_Filter.Transform(new double[8], Local_Stream);
            for (int i = 0; i < Local_Result.Length; i++)
                Assert.AreEqual(0.0d, Local_Result[i], 1e-15, "Block " + Local_Block + " sample " + i);
        }
    }

    /// <summary>
    /// Convolution is linear: filtering a+b gives the same result as filtering a and b separately
    /// and adding, provided each filter instance sees the same history.
    ///
    /// TOLERANCE NOTE: 1e-12 - linearity holds mathematically but each of the three chains
    /// accumulates its own rounding through an independent FFT/IFFT pair, so exactness is genuinely
    /// unattainable here.
    /// </summary>
    [TestMethod]
    public void Property_Transform_IsLinear()
    {
        var Local_Stream = new DSP_Stream();

        var Local_FilterA = new FIR { FFTSize = 32 };
        Local_FilterA.SetTaps(DspCharacterization.Copy(Golden_Taps));
        var Local_FilterB = new FIR { FFTSize = 32 };
        Local_FilterB.SetTaps(DspCharacterization.Copy(Golden_Taps));
        var Local_FilterSum = new FIR { FFTSize = 32 };
        Local_FilterSum.SetTaps(DspCharacterization.Copy(Golden_Taps));

        for (int Local_Block = 0; Local_Block < 5; Local_Block++)
        {
            var Local_A = DspCharacterization.Noise(8, (ulong)(43000 + Local_Block));
            var Local_B = DspCharacterization.Sine(8, 2, 0.3);
            var Local_Sum = new double[8];
            for (int i = 0; i < 8; i++)
                Local_Sum[i] = Local_A[i] + Local_B[i];

            var Local_ResultA = Local_FilterA.Transform(DspCharacterization.Copy(Local_A), Local_Stream);
            var Local_ResultB = Local_FilterB.Transform(DspCharacterization.Copy(Local_B), Local_Stream);
            var Local_ResultSum = Local_FilterSum.Transform(Local_Sum, Local_Stream);

            for (int i = 0; i < 8; i++)
            {
                Assert.AreEqual(Local_ResultA[i] + Local_ResultB[i], Local_ResultSum[i], 1e-12,
                    "Block " + Local_Block + " sample " + i);
            }
        }
    }

    #endregion

    #region Aliasing / Mutation Contract

    /// <summary>
    /// Transform writes the result into the caller's array and returns that same instance.
    /// </summary>
    [TestMethod]
    public void Contract_Transform_MutatesInPlaceAndReturnsTheInputInstance()
    {
        var Local_Filter = new FIR { FFTSize = 32 };
        Local_Filter.SetTaps(DspCharacterization.Copy(Golden_Taps));

        var Local_Input = DspCharacterization.Noise(8, 18100UL);
        var Local_Before = DspCharacterization.Copy(Local_Input);
        var Local_Result = Local_Filter.Transform(Local_Input, new DSP_Stream());

        Assert.IsTrue(ReferenceEquals(Local_Input, Local_Result), "Transform returns the input instance");
        Assert.AreNotEqual(Local_Before[0], Local_Input[0], "The caller's array was written in place");
    }

    /// <summary>
    /// A block LONGER than FFTSize makes the internal Array.Copy throw; FIR swallows the exception
    /// and returns the input unchanged rather than letting it abort the filter chain. Pinned
    /// because that swallow is load bearing for the real-time path.
    /// </summary>
    [TestMethod]
    public void Contract_Transform_OversizedBlockIsSwallowedAndPassesThrough()
    {
        var Local_Filter = new FIR { FFTSize = 32 };
        Local_Filter.SetTaps(DspCharacterization.Copy(Golden_Taps));

        var Local_Original = DspCharacterization.Noise(64, 43100UL);
        var Local_Input = DspCharacterization.Copy(Local_Original);
        var Local_Result = Local_Filter.Transform(Local_Input, new DSP_Stream());

        Assert.IsTrue(ReferenceEquals(Local_Input, Local_Result));
        DspCharacterization.AssertExact(Local_Original, Local_Result,
            "An oversized block must be returned untouched, not partially processed");
    }

    #endregion
}
