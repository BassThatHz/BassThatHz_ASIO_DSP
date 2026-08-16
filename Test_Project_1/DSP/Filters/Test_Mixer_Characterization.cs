#nullable enable

namespace Test_Project_1;

#region Usings
using BassThatHz_ASIO_DSP_Processor;
using System.Numerics;
using Test_Project_1.TestHelpers;
#endregion

/// <summary>
/// CHARACTERIZATION / GUARD SUITE for DSP\Filters\Mixer.cs.
///
/// Mixer reads the PROCESS-WIDE Program.ASIO.InputBuffer. Every test here saves that singleton,
/// installs a deterministic buffer, and restores the original in a finally block, so nothing leaks
/// into the rest of the (serialized) suite.
///
/// Mixer also has a SIMD fast path (taken when Vector.IsHardwareAccelerated and the block is at
/// least 2 vectors long) plus a scalar remainder; both are pinned.
/// </summary>
[TestClass]
public class Test_Mixer_Characterization
{
    #region Golden Vectors

    /// <summary>
    /// Four consecutive blocks mixing two source channels at different attenuations. Mixer is
    /// stateless, but it accumulates into the block in list order, so the sequence pins both the
    /// arithmetic and the ordering.
    /// </summary>
    [TestMethod]
    public void Golden_Transform_MixesTwoChannelsAcrossFourConsecutiveBlocks()
    {
        double[][] Local_Expected =
        {
            new[] { -0.8096893444741189d, -0.6347970449307673d, 0.9763191396443476d, 0.10131010907138056d, 0.4124520622769803d, 0.609267209907218d, -0.3917061919297387d, 0.1314002644362921d },
            new[] { -0.5187183219223122d, -0.29093839320200515d, 1.0126154612535891d, 0.08583147477327455d, 0.759779817198647d, 0.5583698330873416d, 0.008757901643368843d, -0.11365609546247846d },
            new[] { -0.22774729937050553d, -0.7904727483304076d, 1.0489117828628305d, 0.07035284047516863d, 0.263714565263149d, 0.5074724562674651d, 0.4092219952164764d, -0.35871245536124896d },
            new[] { -0.7801692836758634d, -0.44661409660164547d, 1.085208104472072d, 0.05487420617706269d, 0.6110423201848156d, 0.4565750794475886d, -0.03370691806758069d, -0.6037688152600194d },
        };

        var Local_SavedBuffer = Program.ASIO.InputBuffer;
        try
        {
            Program.ASIO.InputBuffer = new double[2][];
            Program.ASIO.InputBuffer[0] = DspCharacterization.Noise(8, 5000UL);
            Program.ASIO.InputBuffer[1] = DspCharacterization.Noise(8, 5001UL);

            var Local_Mixer = new Mixer();
            Local_Mixer.MixerInputs.Add(new MixerInput { ChannelIndex = 0, Enabled = true });
            Local_Mixer.MixerInputs.Add(new MixerInput { ChannelIndex = 1, Enabled = true, Attenuation = -3.0, StreamAttenuation = -1.5 });

            var Local_Stream = new DSP_Stream();
            for (int Local_Block = 0; Local_Block < Local_Expected.Length; Local_Block++)
            {
                var Local_Result = Local_Mixer.Transform(DspCharacterization.Noise(8, (ulong)(6000 + Local_Block)), Local_Stream);
                DspCharacterization.AssertExact(Local_Expected[Local_Block], Local_Result, "Block " + Local_Block);
            }
        }
        finally
        {
            Program.ASIO.InputBuffer = Local_SavedBuffer;
        }
    }

    /// <summary>
    /// A 3-sample block is below the SIMD threshold, so the whole mix runs through the scalar
    /// remainder loop. Pinned separately because the two loops must agree bit for bit.
    /// </summary>
    [TestMethod]
    public void Golden_Transform_ScalarRemainderPath()
    {
        var Local_SavedBuffer = Program.ASIO.InputBuffer;
        try
        {
            Program.ASIO.InputBuffer = new double[1][];
            Program.ASIO.InputBuffer[0] = DspCharacterization.Noise(3, 5100UL);

            var Local_Mixer = new Mixer();
            Local_Mixer.MixerInputs.Add(new MixerInput { ChannelIndex = 0, Enabled = true });

            var Local_Result = Local_Mixer.Transform(DspCharacterization.Noise(3, 6100UL), new DSP_Stream());
            DspCharacterization.AssertExact(
                new double[] { 0.16210718360050444d, -0.4599551812650843d, -0.7848089834827207d },
                Local_Result, "3-sample scalar path");
        }
        finally
        {
            Program.ASIO.InputBuffer = Local_SavedBuffer;
        }
    }

    #endregion

    #region Property Tests

    /// <summary>
    /// The SIMD path and the scalar remainder must produce bit-identical results. This is checked
    /// by mixing a long block and comparing against a hand-written scalar reference.
    ///
    /// TOLERANCE NOTE: exact equality is used, because a fused multiply-add in either loop would
    /// change the result and that is precisely what this guard exists to catch.
    /// </summary>
    [TestMethod]
    public void Property_Transform_SimdAndScalarPathsAgreeBitForBit()
    {
        var Local_SavedBuffer = Program.ASIO.InputBuffer;
        try
        {
            int Local_Length = 37; //deliberately not a multiple of the vector width
            var Local_Source = DspCharacterization.Noise(Local_Length, 35000UL);
            Program.ASIO.InputBuffer = new double[1][];
            Program.ASIO.InputBuffer[0] = Local_Source;

            var Local_Input = DspCharacterization.Noise(Local_Length, 35001UL);
            var Local_Reference = DspCharacterization.Copy(Local_Input);

            double Local_StreamGain = NAudio.Utils.Decibels.DecibelsToLinear(-6.000d);
            double Local_InputGain = NAudio.Utils.Decibels.DecibelsToLinear(-6.051d);
            for (int i = 0; i < Local_Length; i++)
                Local_Reference[i] = Local_Reference[i] * Local_StreamGain + Local_Source[i] * Local_InputGain;

            var Local_Mixer = new Mixer();
            Local_Mixer.MixerInputs.Add(new MixerInput { ChannelIndex = 0, Enabled = true });
            var Local_Result = Local_Mixer.Transform(Local_Input, new DSP_Stream());

            DspCharacterization.AssertExact(Local_Reference, Local_Result,
                "The vectorized mix must match a straightforward scalar mix exactly (vector width here is "
                + Vector<double>.Count + ")");
        }
        finally
        {
            Program.ASIO.InputBuffer = Local_SavedBuffer;
        }
    }

    /// <summary>
    /// Disabled inputs contribute nothing at all - not even their stream attenuation.
    /// </summary>
    [TestMethod]
    public void Property_Transform_DisabledInputsAreSkippedEntirely()
    {
        var Local_SavedBuffer = Program.ASIO.InputBuffer;
        try
        {
            Program.ASIO.InputBuffer = new double[1][];
            Program.ASIO.InputBuffer[0] = DspCharacterization.Constant(8, 1.0);

            var Local_Mixer = new Mixer();
            Local_Mixer.MixerInputs.Add(new MixerInput { ChannelIndex = 0, Enabled = false, Attenuation = 0, StreamAttenuation = -60 });

            var Local_Original = DspCharacterization.Noise(8, 35100UL);
            var Local_Result = Local_Mixer.Transform(DspCharacterization.Copy(Local_Original), new DSP_Stream());
            DspCharacterization.AssertExact(Local_Original, Local_Result, "A disabled input must be a complete no-op");
        }
        finally
        {
            Program.ASIO.InputBuffer = Local_SavedBuffer;
        }
    }

    #endregion

    #region Guard Paths / Aliasing

    /// <summary>
    /// An empty MixerInputs list, an empty block, a null InputBuffer and a null source row are all
    /// silent no-ops that return the caller's array instance unchanged.
    /// </summary>
    [TestMethod]
    public void Contract_Transform_GuardPathsLeaveTheBlockUntouched()
    {
        var Local_SavedBuffer = Program.ASIO.InputBuffer;
        try
        {
            var Local_Original = DspCharacterization.Noise(8, 35200UL);
            var Local_Stream = new DSP_Stream();

            Program.ASIO.InputBuffer = new double[2][]; //both rows null
            var Local_NullRowMixer = new Mixer();
            Local_NullRowMixer.MixerInputs.Add(new MixerInput { ChannelIndex = 0, Enabled = true });
            var Local_NullRowInput = DspCharacterization.Copy(Local_Original);
            DspCharacterization.AssertExact(Local_Original,
                Local_NullRowMixer.Transform(Local_NullRowInput, Local_Stream), "Null source row");

            var Local_EmptyMixer = new Mixer();
            var Local_EmptyListInput = DspCharacterization.Copy(Local_Original);
            var Local_EmptyListResult = Local_EmptyMixer.Transform(Local_EmptyListInput, Local_Stream);
            Assert.IsTrue(ReferenceEquals(Local_EmptyListInput, Local_EmptyListResult));
            DspCharacterization.AssertExact(Local_Original, Local_EmptyListResult, "Empty MixerInputs list");

            var Local_EmptyBlock = new double[0];
            Assert.IsTrue(ReferenceEquals(Local_EmptyBlock, Local_NullRowMixer.Transform(Local_EmptyBlock, Local_Stream)));
        }
        finally
        {
            Program.ASIO.InputBuffer = Local_SavedBuffer;
        }
    }

    /// <summary>
    /// Transform mutates the caller's array in place and returns that same instance.
    /// </summary>
    [TestMethod]
    public void Contract_Transform_MutatesInPlaceAndReturnsTheInputInstance()
    {
        var Local_SavedBuffer = Program.ASIO.InputBuffer;
        try
        {
            Program.ASIO.InputBuffer = new double[1][];
            Program.ASIO.InputBuffer[0] = DspCharacterization.Constant(8, 0.5);

            var Local_Mixer = new Mixer();
            Local_Mixer.MixerInputs.Add(new MixerInput { ChannelIndex = 0, Enabled = true, Attenuation = 0, StreamAttenuation = 0 });

            var Local_Input = DspCharacterization.Constant(8, 0.25);
            var Local_Result = Local_Mixer.Transform(Local_Input, new DSP_Stream());

            Assert.IsTrue(ReferenceEquals(Local_Input, Local_Result));
            for (int i = 0; i < Local_Input.Length; i++)
                DspCharacterization.AssertExact(0.75d, Local_Input[i], "Sample " + i + " was written in place");
        }
        finally
        {
            Program.ASIO.InputBuffer = Local_SavedBuffer;
        }
    }

    #endregion
}
