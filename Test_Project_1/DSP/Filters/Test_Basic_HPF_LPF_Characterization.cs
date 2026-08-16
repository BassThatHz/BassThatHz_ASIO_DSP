#nullable enable

namespace Test_Project_1;

#region Usings
using BassThatHz_ASIO_DSP_Processor;
using Test_Project_1.TestHelpers;
#endregion

/// <summary>
/// CHARACTERIZATION / GUARD SUITE for DSP\Filters\Basic_HPF_LPF.cs.
///
/// Basic_HPF_LPF is a cascade of up to eight BiQuadFilters, so it is stateful in exactly the way
/// that matters: block N depends on every earlier block through eight sets of z-delays.
///
/// It also reads the PROCESS-WIDE Program.DSP_Info.InSampleRate in its constructor, so each test
/// saves and restores that singleton.
///
/// Both the "with ApplySettings" and "without ApplySettings" behaviors are pinned; without it every
/// biquad is left disabled and the filter is a pass-through.
/// </summary>
[TestClass]
public class Test_Basic_HPF_LPF_Characterization
{
    #region Without ApplySettings

    /// <summary>
    /// A freshly constructed Basic_HPF_LPF has all eight biquads disabled, so Transform is a
    /// bit-exact pass-through until ApplySettings (or ResetSampleRate) is called. This matters:
    /// the biquads' default coefficients are all zero, so if the enabled flags were ignored the
    /// stream would go silent instead.
    /// </summary>
    [TestMethod]
    public void Golden_Transform_WithoutApplySettingsIsAnExactPassThrough()
    {
        int Local_SavedRate = Program.DSP_Info.InSampleRate;
        try
        {
            Program.DSP_Info.InSampleRate = 48000;
            var Local_Filter = new Basic_HPF_LPF { HPFFreq = 80, LPFFreq = 5000 };

            var Local_Original = DspCharacterization.Noise(8, 8000UL);
            DspCharacterization.AssertExact(
                new double[]
                {
                    -0.8353267795333912d, 0.5423189114348268d, 0.7877880564041277d, 0.15742141474189109d,
                    0.9184937351256255d, 0.5481106442030368d, 0.35839318823090327d, 0.7502926793881433d
                },
                Local_Original, "The deterministic source itself must not drift");

            var Local_Result = Local_Filter.Transform(DspCharacterization.Copy(Local_Original), new DSP_Stream());
            DspCharacterization.AssertExact(Local_Original, Local_Result,
                "Without ApplySettings every biquad is disabled, so the block passes through untouched");
        }
        finally
        {
            Program.DSP_Info.InSampleRate = Local_SavedRate;
        }
    }

    /// <summary>
    /// FilterOrder.None on both sides short-circuits before any biquad runs, even after
    /// ApplySettings.
    /// </summary>
    [TestMethod]
    public void Golden_Transform_NoneOnBothSidesIsAnExactPassThrough()
    {
        int Local_SavedRate = Program.DSP_Info.InSampleRate;
        try
        {
            Program.DSP_Info.InSampleRate = 48000;
            var Local_Filter = new Basic_HPF_LPF
            {
                HPFFilter = Basic_HPF_LPF.FilterOrder.None,
                LPFFilter = Basic_HPF_LPF.FilterOrder.None
            };
            Local_Filter.ResetSampleRate(48000);

            var Local_Original = DspCharacterization.Noise(8, 8400UL);
            DspCharacterization.AssertExact(
                new double[]
                {
                    -0.8349139839978901d, 0.709264980976889d, -0.7833548730381892d, -0.5248291348905445d,
                    0.37605584221994026d, 0.26943021307979786d, 0.21840181258922042d, 0.3022093717466716d
                },
                Local_Filter.Transform(DspCharacterization.Copy(Local_Original), new DSP_Stream()),
                "None/None must be a pass-through");
        }
        finally
        {
            Program.DSP_Info.InSampleRate = Local_SavedRate;
        }
    }

    #endregion

    #region Multi-Block Stateful Sequences

    /// <summary>
    /// Seven consecutive blocks through the DEFAULT LR 12 dB/oct pair (HPF 80 Hz, LPF 5 kHz) at
    /// 48 kHz, with a block-size change on the last one. Five biquads are active in this
    /// configuration, so the cascade's carried state is thoroughly exercised.
    /// </summary>
    [TestMethod]
    public void Stateful_Transform_LR12_SevenBlocksIncludingABlockSizeChange()
    {
        double[][] Local_Expected =
        {
            new[] { -0.010472869995392022d, -0.03639923331544281d, -0.01529599076796894d, 0.03477472955415533d, 0.05969906229082239d, 0.07931176522074196d, 0.08994774548586451d, 0.08238020211963323d },
            new[] { -0.03544110396541701d, -0.2617765792308003d, -0.38134094234573235d, -0.3255892996088683d, -0.2524109750136342d, -0.22713668524532948d, -0.22348132986816127d, -0.21618828468206905d },
            new[] { -0.14337741129079232d, -0.00556327827154228d, 0.13130685109559923d, 0.18488752897233013d, 0.07232746424952566d, -0.12548284048418135d, -0.17027001194819358d, -0.008787569303767048d },
            new[] { 0.14279320880725008d, 0.11944158258188384d, 0.03002398405021527d, -0.004625903065972754d, -0.013540263485078047d, -0.003674025975332596d, -0.0019634945440417956d, -0.0786866068308218d },
            new[] { -0.2504232324964516d, -0.34684796721093564d, -0.22382847881310874d, -0.0680097274265072d, -0.06206451634469324d, -0.12438258431773816d, -0.07116468229867584d, 0.06808705343425284d },
            new[] { 0.13877275611937562d, 0.15193930890064394d, 0.14345312448789896d, 0.10894576931041021d, 0.09486389301316031d, 0.14314625745806542d, 0.1772642419533404d, 0.1644067775717712d },
            new[] { 0.20963106445441146d, 0.3199325177549203d, 0.36812866162606245d, 0.2578021722821466d },
        };

        int Local_SavedRate = Program.DSP_Info.InSampleRate;
        try
        {
            Program.DSP_Info.InSampleRate = 48000;
            var Local_Stream = new DSP_Stream();
            var Local_Filter = new Basic_HPF_LPF { HPFFreq = 80, LPFFreq = 5000 };
            Local_Filter.ResetSampleRate(48000);

            for (int Local_Block = 0; Local_Block < 6; Local_Block++)
            {
                var Local_Result = Local_Filter.Transform(DspCharacterization.Noise(8, (ulong)(8100 + Local_Block)), Local_Stream);
                DspCharacterization.AssertExact(Local_Expected[Local_Block], Local_Result, "Block " + Local_Block);
            }

            DspCharacterization.AssertExact(Local_Expected[6],
                Local_Filter.Transform(DspCharacterization.Noise(4, 8200UL), Local_Stream), "Block 6 (size drops to 4)");
        }
        finally
        {
            Program.DSP_Info.InSampleRate = Local_SavedRate;
        }
    }

    /// <summary>
    /// Four consecutive blocks through the steepest configuration - Butterworth 48 dB/oct high-pass
    /// with a Linkwitz-Riley 48 dB/oct low-pass - which enables all eight biquads. The Q arrays
    /// derived by ProcessFilterOrders are pinned alongside the audio.
    /// </summary>
    [TestMethod]
    public void Stateful_Transform_BW48AndLR48_FourBlocksWithQArrays()
    {
        double[][] Local_Expected =
        {
            new[] { 4.932481409176244E-06d, 3.7020119918268215E-05d, 7.87153796527084E-05d, -0.00023652194362891273d, -0.002177394172753396d, -0.008199049383967765d, -0.021281065167425593d, -0.04321720374400245d },
            new[] { -0.07263839436411282d, -0.10394676341681507d, -0.1277605899201524d, -0.1326596010901493d, -0.10817484476368885d, -0.04924418637327596d, 0.039130446244551476d, 0.14045306369319743d },
            new[] { 0.23120705108488912d, 0.2895893324920679d, 0.30337042844109796d, 0.2730484904867151d, 0.21047810518566124d, 0.13474208273284213d, 0.0657877237073726d, 0.016600447136524935d },
            new[] { -0.012694461964520433d, -0.03473633979520631d, -0.06792163015432953d, -0.12555102323796075d, -0.20642516088927673d, -0.29218139881760663d, -0.35388122504419106d, -0.3651072145556838d },
        };

        int Local_SavedRate = Program.DSP_Info.InSampleRate;
        try
        {
            Program.DSP_Info.InSampleRate = 48000;
            var Local_Stream = new DSP_Stream();
            var Local_Filter = new Basic_HPF_LPF
            {
                HPFFreq = 80,
                LPFFreq = 5000,
                HPFFilter = Basic_HPF_LPF.FilterOrder.BW_48db,
                LPFFilter = Basic_HPF_LPF.FilterOrder.LR_48db
            };
            Local_Filter.ResetSampleRate(48000);

            DspCharacterization.AssertExact(
                new double[] { 0.5097853293977905d, 0.6013590715015936d, 0.900009000090001d, 2.5627883136852896d },
                Local_Filter.Q_Array_HPF, "BW_48db Q array");
            DspCharacterization.AssertExact(
                new double[] { 0.5411841108345059d, 1.3065064018813692d, 0.5411841108345059d, 1.3065064018813692d },
                Local_Filter.Q_Array_LPF, "LR_48db Q array");

            for (int Local_Block = 0; Local_Block < Local_Expected.Length; Local_Block++)
            {
                var Local_Result = Local_Filter.Transform(DspCharacterization.Noise(8, (ulong)(8300 + Local_Block)), Local_Stream);
                DspCharacterization.AssertExact(Local_Expected[Local_Block], Local_Result, "Block " + Local_Block);
            }
        }
        finally
        {
            Program.DSP_Info.InSampleRate = Local_SavedRate;
        }
    }

    /// <summary>
    /// ApplySettings called mid-sequence redesigns every active biquad, which also zeroes their
    /// z-delays. The block that follows must therefore be identical to a fresh filter's first
    /// block - this pins the reset semantics.
    /// </summary>
    [TestMethod]
    public void Stateful_ApplySettings_MidSequenceResetsTheCascade()
    {
        int Local_SavedRate = Program.DSP_Info.InSampleRate;
        try
        {
            Program.DSP_Info.InSampleRate = 48000;
            var Local_Stream = new DSP_Stream();

            var Local_Fresh = new Basic_HPF_LPF { HPFFreq = 80, LPFFreq = 5000 };
            Local_Fresh.ResetSampleRate(48000);
            var Local_FreshFirstBlock = Local_Fresh.Transform(DspCharacterization.Noise(8, 8100UL), Local_Stream);

            var Local_Primed = new Basic_HPF_LPF { HPFFreq = 80, LPFFreq = 5000 };
            Local_Primed.ResetSampleRate(48000);
            for (int Local_Block = 0; Local_Block < 5; Local_Block++)
                Local_Primed.Transform(DspCharacterization.Noise(8, (ulong)(9500 + Local_Block)), Local_Stream);

            Local_Primed.ApplySettings();
            var Local_AfterReset = Local_Primed.Transform(DspCharacterization.Noise(8, 8100UL), Local_Stream);

            DspCharacterization.AssertExact(Local_FreshFirstBlock, Local_AfterReset,
                "ApplySettings must fully reset the cascade state");
        }
        finally
        {
            Program.DSP_Info.InSampleRate = Local_SavedRate;
        }
    }

    #endregion

    #region Filter Order Coverage

    /// <summary>
    /// Every FilterOrder is designable at 48 kHz without throwing, and each leaves the documented
    /// number of biquads enabled. This is the cheap structural guard that complements the audio
    /// golden vectors above.
    /// </summary>
    [TestMethod]
    public void Property_ApplySettings_EveryFilterOrderIsDesignable()
    {
        (Basic_HPF_LPF.FilterOrder Order, int EnabledHpf, int EnabledLpf)[] Local_Cases =
        {
            (Basic_HPF_LPF.FilterOrder.LR_12db, 3, 2),
            (Basic_HPF_LPF.FilterOrder.LR_24db, 2, 2),
            (Basic_HPF_LPF.FilterOrder.LR_48db, 4, 4),
            (Basic_HPF_LPF.FilterOrder.BW_6db, 1, 1),
            (Basic_HPF_LPF.FilterOrder.BW_12db, 2, 1),
            (Basic_HPF_LPF.FilterOrder.BW_18db, 2, 2),
            (Basic_HPF_LPF.FilterOrder.BW_24db, 2, 2),
            (Basic_HPF_LPF.FilterOrder.BW_30db, 3, 3),
            (Basic_HPF_LPF.FilterOrder.BW_36db, 4, 3),
            (Basic_HPF_LPF.FilterOrder.BW_42db, 4, 4),
            (Basic_HPF_LPF.FilterOrder.BW_48db, 4, 4),
        };

        int Local_SavedRate = Program.DSP_Info.InSampleRate;
        try
        {
            Program.DSP_Info.InSampleRate = 48000;
            foreach (var Local_Case in Local_Cases)
            {
                var Local_Filter = new Basic_HPF_LPF
                {
                    HPFFreq = 80,
                    LPFFreq = 5000,
                    HPFFilter = Local_Case.Order,
                    LPFFilter = Local_Case.Order
                };
                Local_Filter.ResetSampleRate(48000);

                int Local_Hpf = 0;
                for (int i = 0; i < 4; i++)
                    if (Local_Filter.BiQuads[i].FilterEnabled) Local_Hpf++;
                int Local_Lpf = 0;
                for (int i = 4; i < 8; i++)
                    if (Local_Filter.BiQuads[i].FilterEnabled) Local_Lpf++;

                Assert.AreEqual(Local_Case.EnabledHpf, Local_Hpf, Local_Case.Order + " HPF stage count");
                Assert.AreEqual(Local_Case.EnabledLpf, Local_Lpf, Local_Case.Order + " LPF stage count");

                var Local_Output = Local_Filter.Transform(DspCharacterization.Noise(16, 38000UL), new DSP_Stream());
                for (int i = 0; i < Local_Output.Length; i++)
                {
                    Assert.IsFalse(double.IsNaN(Local_Output[i]) || double.IsInfinity(Local_Output[i]),
                        Local_Case.Order + " produced a non-finite sample at index " + i);
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
    /// Transform always returns the caller's instance, and mutates it in place when any stage is
    /// active.
    /// </summary>
    [TestMethod]
    public void Contract_Transform_MutatesInPlaceAndReturnsTheInputInstance()
    {
        int Local_SavedRate = Program.DSP_Info.InSampleRate;
        try
        {
            Program.DSP_Info.InSampleRate = 48000;
            var Local_Filter = new Basic_HPF_LPF { HPFFreq = 80, LPFFreq = 5000 };
            Local_Filter.ResetSampleRate(48000);

            var Local_Input = DspCharacterization.Noise(8, 8100UL);
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

    #endregion
}
