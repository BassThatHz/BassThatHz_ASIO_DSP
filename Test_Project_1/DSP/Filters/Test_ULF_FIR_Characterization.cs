#nullable enable

namespace Test_Project_1;

#region Usings
using BassThatHz_ASIO_DSP_Processor;
using Test_Project_1.TestHelpers;
#endregion

/// <summary>
/// CHARACTERIZATION / GUARD SUITE for DSP\Filters\ULF_FIR.cs.
///
/// ULF_FIR is the overlap-save fast convolver again, but with an extra layer: it resamples the taps
/// spectrum by index mapping (mIndex = round(n * inputRate / tapsRate)), applies a Gaussian roll-off
/// above 0.9 * the taps' Nyquist, and rebuilds the negative half of the spectrum by conjugate
/// symmetry. Its OverlapBuffer carries between blocks exactly as FIR's does.
///
/// It reads the PROCESS-WIDE Program.DSP_Info.InSampleRate on every block, so every test saves and
/// restores that singleton. FFTSize is 32 throughout to keep the golden vectors readable.
/// </summary>
[TestClass]
public class Test_ULF_FIR_Characterization
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
        int Local_SavedRate = Program.DSP_Info.InSampleRate;
        try
        {
            Program.DSP_Info.InSampleRate = 48000;

            var Local_Input = DspCharacterization.Noise(8, 19000UL);
            var Local_Filter = new ULF_FIR();
            var Local_Result = Local_Filter.Transform(Local_Input, new DSP_Stream());

            Assert.IsTrue(ReferenceEquals(Local_Input, Local_Result), "Returns the input instance");
            DspCharacterization.AssertExact(
                new double[]
                {
                    -0.8239749023071119d, 0.13333582384153297d, -0.4186425032595886d, 0.3955312998499143d,
                    -0.9985483197807157d, 0.8843987883139641d, 0.5086303580846219d, 0.4280017192476697d
                },
                Local_Result, "No taps means no processing at all");
            Assert.IsNull(Local_Filter.Taps);
        }
        finally
        {
            Program.DSP_Info.InSampleRate = Local_SavedRate;
        }
    }

    #endregion

    #region Multi-Block Stateful Sequences

    /// <summary>
    /// Six consecutive blocks at the DEFAULT TapsSampleRate of 960 Hz against a 48 kHz stream.
    ///
    /// This exposes a behavior worth pinning explicitly: the index mapping is
    /// mIndex = round(n * 48000/960) = 50n, and with FFTSize 32 only n = 0 maps inside the 32-bin
    /// taps spectrum. Every other bin is forced to Complex.Zero, so the filter passes DC only and
    /// every output block is a CONSTANT. That is the documented ultra-low-frequency behavior taken
    /// to its extreme by the small FFT size used here; the same code runs at FFTSize 8192 in
    /// production.
    /// </summary>
    [TestMethod]
    public void Stateful_Transform_DefaultTapsRate_SixConsecutiveBlocks()
    {
        double[] Local_ExpectedConstants =
        {
            0.0215310163670792d,
            0.1200033002423633d,
            0.06104185162585226d,
            0.1376154205175461d,
            0.2110054905503657d,
            0.1086143105831852d,
        };

        int Local_SavedRate = Program.DSP_Info.InSampleRate;
        try
        {
            Program.DSP_Info.InSampleRate = 48000;
            var Local_Stream = new DSP_Stream();
            var Local_Filter = new ULF_FIR { FFTSize = 32, TapsSampleRate = 960 };
            Local_Filter.SetTaps(DspCharacterization.Copy(Golden_Taps));

            for (int Local_Block = 0; Local_Block < Local_ExpectedConstants.Length; Local_Block++)
            {
                var Local_Result = Local_Filter.Transform(DspCharacterization.Noise(8, (ulong)(19100 + Local_Block)), Local_Stream);
                DspCharacterization.AssertExact(
                    DspCharacterization.Constant(8, Local_ExpectedConstants[Local_Block]),
                    Local_Result, "Block " + Local_Block);
            }
        }
        finally
        {
            Program.DSP_Info.InSampleRate = Local_SavedRate;
        }
    }

    /// <summary>
    /// Four consecutive blocks with TapsSampleRate matching the stream rate, so the index mapping is
    /// one-to-one and the whole spectrum participates. This is the configuration that actually
    /// exercises the Gaussian roll-off and the conjugate mirroring.
    /// </summary>
    [TestMethod]
    public void Stateful_Transform_MatchedTapsRate_FourConsecutiveBlocks()
    {
        double[][] Local_Expected =
        {
            new[] { -0.42025963881415096d, -0.5890781856727939d, 0.09068722278182759d, -0.4149263489558638d, -0.32883759405563295d, 0.24438276625847866d, 0.3054046179048816d, 0.29480199333941504d },
            new[] { 0.06683594134815311d, 0.053842480520623806d, 0.42730781502943793d, -0.2580016914961941d, 0.12738773529078473d, 0.39833269999577836d, -0.15637092125671276d, -0.21531732115419167d },
            new[] { 0.1361231996263181d, 0.4870323066884491d, 0.7328849977755811d, 0.8817393016437559d, 0.09965829781652114d, 0.28763007027945775d, 0.2699127142408569d, -0.36176293548153643d },
            new[] { -0.5223302335560116d, -0.47606718715116386d, -0.6911998000256014d, 0.12703832996167286d, 0.21556445133471894d, 0.2839775541051305d, -0.16139244780219078d, 0.09975120987056064d },
        };

        int Local_SavedRate = Program.DSP_Info.InSampleRate;
        try
        {
            Program.DSP_Info.InSampleRate = 48000;
            var Local_Stream = new DSP_Stream();
            var Local_Filter = new ULF_FIR { FFTSize = 32, TapsSampleRate = 48000 };
            Local_Filter.SetTaps(DspCharacterization.Copy(Golden_Taps));

            for (int Local_Block = 0; Local_Block < Local_Expected.Length; Local_Block++)
            {
                var Local_Result = Local_Filter.Transform(DspCharacterization.Noise(8, (ulong)(19200 + Local_Block)), Local_Stream);
                DspCharacterization.AssertExact(Local_Expected[Local_Block], Local_Result, "Block " + Local_Block);
            }
        }
        finally
        {
            Program.DSP_Info.InSampleRate = Local_SavedRate;
        }
    }

    /// <summary>
    /// A block-size change mid-sequence, which changes the overlap-save slide length.
    /// </summary>
    [TestMethod]
    public void Stateful_Transform_BlockSizeChangeMidSequence()
    {
        double[][] Local_Expected =
        {
            new[] { 0.10285023811342811d, 0.40820883279073744d, -0.07135698521835357d, -0.36711130981047635d, -0.11832946829999845d, 0.2315777026698223d, -0.14090844111381123d, -0.027413235493872243d },
            new[] { 0.4253790418127694d, -0.023083969887023076d, -0.24769597719255998d, -0.4529657905139594d },
            new[] { -0.4539329932925552d, -0.051879961044400694d, -0.1887955527443641d, -0.4529513663896124d, -0.3070623787481337d, 0.016443699364620395d, -0.2920877548312287d, 0.3054925788130206d },
        };

        int Local_SavedRate = Program.DSP_Info.InSampleRate;
        try
        {
            Program.DSP_Info.InSampleRate = 48000;
            var Local_Stream = new DSP_Stream();
            var Local_Filter = new ULF_FIR { FFTSize = 32, TapsSampleRate = 48000 };
            Local_Filter.SetTaps(DspCharacterization.Copy(Golden_Taps));

            DspCharacterization.AssertExact(Local_Expected[0],
                Local_Filter.Transform(DspCharacterization.Noise(8, 19300UL), Local_Stream), "Block 0 (8 samples)");
            DspCharacterization.AssertExact(Local_Expected[1],
                Local_Filter.Transform(DspCharacterization.Noise(4, 19301UL), Local_Stream), "Block 1 (4 samples)");
            DspCharacterization.AssertExact(Local_Expected[2],
                Local_Filter.Transform(DspCharacterization.Noise(8, 19302UL), Local_Stream), "Block 2 (back to 8 samples)");
        }
        finally
        {
            Program.DSP_Info.InSampleRate = Local_SavedRate;
        }
    }

    /// <summary>
    /// SetTaps mid-sequence rebuilds the taps spectrum and resets the overlap buffer. With
    /// unit-impulse taps and a matched rate the block that follows is the input again, modulo the
    /// transform residue and the Gaussian roll-off - pinned exactly, alongside the untouched input
    /// for comparison.
    /// </summary>
    [TestMethod]
    public void Stateful_SetTaps_MidSequenceResetsTheOverlapBuffer()
    {
        int Local_SavedRate = Program.DSP_Info.InSampleRate;
        try
        {
            Program.DSP_Info.InSampleRate = 48000;
            var Local_Stream = new DSP_Stream();
            var Local_Filter = new ULF_FIR { FFTSize = 32, TapsSampleRate = 48000 };
            Local_Filter.SetTaps(DspCharacterization.Copy(Golden_Taps));

            Local_Filter.Transform(DspCharacterization.Noise(8, 19300UL), Local_Stream);
            Local_Filter.Transform(DspCharacterization.Noise(4, 19301UL), Local_Stream);
            Local_Filter.Transform(DspCharacterization.Noise(8, 19302UL), Local_Stream);

            Local_Filter.SetTaps(new double[] { 1.0, 0.0, 0.0, 0.0 });

            DspCharacterization.AssertExact(
                new double[]
                {
                    -0.8059639266103766d, 0.28325705226479847d, 0.026455822779031666d, -0.30199678578680167d,
                    0.47318450520145283d, 0.5927550216595647d, 0.3803407704039986d, -0.030516144136989522d
                },
                Local_Filter.Transform(DspCharacterization.Noise(8, 19400UL), Local_Stream),
                "The first block after SetTaps runs through a clean overlap buffer");

            //The untouched input for comparison - the roll-off is what makes the two differ.
            DspCharacterization.AssertExact(
                new double[]
                {
                    -0.8235621067716108d, 0.3002818933835949d, 0.010214567298094224d, -0.28671924978252106d,
                    0.4590137873135991d, 0.605718357190725d, 0.3686389824429388d, -0.02008158839380192d
                },
                DspCharacterization.Noise(8, 19400UL), "The deterministic source itself must not drift");
        }
        finally
        {
            Program.DSP_Info.InSampleRate = Local_SavedRate;
        }
    }

    /// <summary>
    /// SetTaps stores the taps by reference and allocates the overlap buffer at FFTSize.
    /// </summary>
    [TestMethod]
    public void Contract_SetTaps_StoresTheTapsByReference()
    {
        var Local_Filter = new ULF_FIR { FFTSize = 32 };
        var Local_Taps = DspCharacterization.Copy(Golden_Taps);
        Local_Filter.SetTaps(Local_Taps);

        Assert.IsTrue(ReferenceEquals(Local_Taps, Local_Filter.Taps), "Taps is stored by reference, not copied");
        DspCharacterization.AssertExact(Golden_Taps, Local_Filter.Taps!, "Taps contents");
    }

    #endregion

    #region Property Tests

    /// <summary>
    /// Silence in, silence out.
    ///
    /// TOLERANCE NOTE: 1e-15, for the same reason as the FIR equivalent - the result is a sum of
    /// signed products that is mathematically but not structurally zero, and asserting bit-exactness
    /// on it would be brittle without adding any protection.
    /// </summary>
    [TestMethod]
    public void Property_Transform_SilenceStaysSilent()
    {
        int Local_SavedRate = Program.DSP_Info.InSampleRate;
        try
        {
            Program.DSP_Info.InSampleRate = 48000;
            var Local_Stream = new DSP_Stream();
            var Local_Filter = new ULF_FIR { FFTSize = 32, TapsSampleRate = 48000 };
            Local_Filter.SetTaps(DspCharacterization.Copy(Golden_Taps));

            for (int Local_Block = 0; Local_Block < 6; Local_Block++)
            {
                var Local_Result = Local_Filter.Transform(new double[8], Local_Stream);
                for (int i = 0; i < Local_Result.Length; i++)
                    Assert.AreEqual(0.0d, Local_Result[i], 1e-15, "Block " + Local_Block + " sample " + i);
            }
        }
        finally
        {
            Program.DSP_Info.InSampleRate = Local_SavedRate;
        }
    }

    /// <summary>
    /// The output is always finite, whatever the taps-rate to stream-rate relationship - including
    /// the degenerate cases where the index mapping sends every bin out of range.
    /// </summary>
    [TestMethod]
    public void Property_Transform_ProducesFiniteOutputAcrossTapsRateRatios()
    {
        int Local_SavedRate = Program.DSP_Info.InSampleRate;
        try
        {
            Program.DSP_Info.InSampleRate = 48000;

            foreach (int Local_TapsRate in new[] { 96, 960, 4800, 48000, 96000 })
            {
                var Local_Stream = new DSP_Stream();
                var Local_Filter = new ULF_FIR { FFTSize = 32, TapsSampleRate = Local_TapsRate };
                Local_Filter.SetTaps(DspCharacterization.Copy(Golden_Taps));

                for (int Local_Block = 0; Local_Block < 4; Local_Block++)
                {
                    var Local_Result = Local_Filter.Transform(DspCharacterization.Noise(8, (ulong)(44000 + Local_Block)), Local_Stream);
                    for (int i = 0; i < Local_Result.Length; i++)
                    {
                        Assert.IsFalse(double.IsNaN(Local_Result[i]) || double.IsInfinity(Local_Result[i]),
                            "TapsSampleRate " + Local_TapsRate + " block " + Local_Block + " sample " + i);
                    }
                }
            }
        }
        finally
        {
            Program.DSP_Info.InSampleRate = Local_SavedRate;
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
        int Local_SavedRate = Program.DSP_Info.InSampleRate;
        try
        {
            Program.DSP_Info.InSampleRate = 48000;
            var Local_Filter = new ULF_FIR { FFTSize = 32, TapsSampleRate = 48000 };
            Local_Filter.SetTaps(DspCharacterization.Copy(Golden_Taps));

            var Local_Input = DspCharacterization.Noise(8, 19300UL);
            var Local_Before = DspCharacterization.Copy(Local_Input);
            var Local_Result = Local_Filter.Transform(Local_Input, new DSP_Stream());

            Assert.IsTrue(ReferenceEquals(Local_Input, Local_Result), "Transform returns the input instance");
            Assert.AreNotEqual(Local_Before[0], Local_Input[0], "The caller's array was written in place");
        }
        finally
        {
            Program.DSP_Info.InSampleRate = Local_SavedRate;
        }
    }

    /// <summary>
    /// A block LONGER than FFTSize makes the internal Array.Copy throw; ULF_FIR swallows the
    /// exception and returns the input unchanged rather than aborting the filter chain.
    /// </summary>
    [TestMethod]
    public void Contract_Transform_OversizedBlockIsSwallowedAndPassesThrough()
    {
        int Local_SavedRate = Program.DSP_Info.InSampleRate;
        try
        {
            Program.DSP_Info.InSampleRate = 48000;
            var Local_Filter = new ULF_FIR { FFTSize = 32, TapsSampleRate = 48000 };
            Local_Filter.SetTaps(DspCharacterization.Copy(Golden_Taps));

            var Local_Original = DspCharacterization.Noise(64, 44100UL);
            var Local_Input = DspCharacterization.Copy(Local_Original);
            var Local_Result = Local_Filter.Transform(Local_Input, new DSP_Stream());

            Assert.IsTrue(ReferenceEquals(Local_Input, Local_Result));
            DspCharacterization.AssertExact(Local_Original, Local_Result,
                "An oversized block must be returned untouched, not partially processed");
        }
        finally
        {
            Program.DSP_Info.InSampleRate = Local_SavedRate;
        }
    }

    #endregion
}
