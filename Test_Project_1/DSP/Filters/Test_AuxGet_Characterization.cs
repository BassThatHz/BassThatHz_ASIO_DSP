#nullable enable

namespace Test_Project_1;

#region Usings
using BassThatHz_ASIO_DSP_Processor;
using BassThatHz_ASIO_DSP_Processor.DSP.Filters;
using Test_Project_1.TestHelpers;
#endregion

/// <summary>
/// CHARACTERIZATION / GUARD SUITE for DSP\Filters\AuxGet.cs.
///
/// AuxGet reads the per-stream aux bus (DSP_Stream.AuxBuffer) and mixes it into the block, so the
/// stream state is set up deterministically here and never leaks - each test builds its own
/// DSP_Stream rather than touching a global.
/// </summary>
[TestClass]
public class Test_AuxGet_Characterization
{
    #region Golden Vectors

    /// <summary>
    /// Six consecutive blocks with a changing aux bus. AuxGet is stateless, but the aux buffer it
    /// reads is not, so the multi-block sequence proves the read side has not shifted.
    /// Default gains: StreamAttenuation -6.0 dB, AuxAttenuation -6.051 dB.
    /// </summary>
    [TestMethod]
    public void Golden_Transform_MixesStreamAndAuxAcrossSixConsecutiveBlocks()
    {
        double[][] Local_Expected =
        {
            new[] { -0.8405305140884434d, -0.2539026077255472d, 0.3908903390965288d, 0.03730979448751884d, -0.12050467761472261d, 0.37873180271024814d, 0.282602617313942d, -0.08722155647856977d },
            new[] { -0.15091565995292433d, 0.5610583869313903d, 0.4769143091561465d, 0.0006247114160426726d, -0.29969621432546656d, 0.25810265282550243d, -0.7671595041784286d, 0.32848965331289526d },
            new[] { 0.5386991941825947d, -0.6228612639892401d, 0.5629382792157642d, -0.03606037165543338d, 0.5234867162183339d, 0.13747350294075647d, 0.18195901990676877d, -0.25230531521866295d },
            new[] { -0.7705665972594542d, 0.19209973066769756d, 0.6489622492753819d, -0.07274545472690944d, -0.6522109988154337d, 0.01684435305601048d, 0.13457136566894268d, 0.16927418350432308d },
            new[] { -0.08095174312393513d, 0.010554547001611514d, 0.7349862193349995d, -0.10943053779838559d, 0.17097193172836692d, -0.10378479682873525d, 0.08131542249959547d, -0.4115207850272354d },
            new[] { 0.6086631110115841d, -0.17685892559599548d, 0.8210101893946172d, -0.14611562086986174d, -0.002351316050856145d, -0.22441394671348122d, 0.03392776826176935d, 0.004190424764229872d },
        };

        var Local_Stream = new DSP_Stream { AuxBuffer = new double[4][] };
        var Local_Filter = new AuxGet { AuxGetIndex = 2 };

        for (int Local_Block = 0; Local_Block < Local_Expected.Length; Local_Block++)
        {
            Local_Stream.AuxBuffer[2] = DspCharacterization.Noise(8, (ulong)(2000 + Local_Block));
            var Local_Input = DspCharacterization.Noise(8, (ulong)(3000 + Local_Block));
            var Local_Result = Local_Filter.Transform(Local_Input, Local_Stream);
            DspCharacterization.AssertExact(Local_Expected[Local_Block], Local_Result, "Block " + Local_Block);
        }
    }

    /// <summary>
    /// MuteBefore replaces the block outright with the aux bus - no gains applied at all.
    /// </summary>
    [TestMethod]
    public void Golden_Transform_MuteBeforeCopiesTheAuxBusVerbatim()
    {
        var Local_Stream = new DSP_Stream { AuxBuffer = new double[4][] };
        Local_Stream.AuxBuffer[2] = DspCharacterization.Noise(8, 2000UL);
        var Local_Filter = new AuxGet { AuxGetIndex = 2, MuteBefore = true };

        double[] Local_Expected =
        {
            -0.8415187125659069d, 0.03812786830389636d, 0.3549319980388821d, 0.3911796592284238d,
            -0.9449378712890975d, 0.7283171110516218d, 0.45826382285614775d, -0.5284577059897801d
        };

        var Local_Result = Local_Filter.Transform(DspCharacterization.Noise(8, 3000UL), Local_Stream);
        DspCharacterization.AssertExact(Local_Expected, Local_Result, "MuteBefore output");
        DspCharacterization.AssertExact(Local_Stream.AuxBuffer[2], Local_Result,
            "MuteBefore must reproduce the aux bus exactly");
    }

    #endregion

    #region Property Tests

    /// <summary>
    /// At 0 dB on both attenuations the mix is a plain sum. Both gain factors go through
    /// NAudio's Decibels.DecibelsToLinear, so 0 dB must stay exactly 1.0.
    /// </summary>
    [TestMethod]
    public void Property_Transform_AtZeroDbIsAPlainSum()
    {
        var Local_Stream = new DSP_Stream { AuxBuffer = new double[4][] };
        Local_Stream.AuxBuffer[0] = DspCharacterization.Constant(8, 0.25);
        var Local_Filter = new AuxGet { AuxGetIndex = 0, StreamAttenuation = 0, AuxAttenuation = 0 };

        var Local_Result = Local_Filter.Transform(DspCharacterization.Constant(8, 0.5), Local_Stream);
        for (int i = 0; i < Local_Result.Length; i++)
            DspCharacterization.AssertExact(0.75d, Local_Result[i], "Sample " + i);
    }

    #endregion

    #region Guard Paths

    /// <summary>
    /// Every guard path is a silent no-op that leaves the block untouched: a negative index, an
    /// index past the end of the aux array, a null aux row, and an aux row whose length does not
    /// match the block.
    /// </summary>
    [TestMethod]
    public void Contract_Transform_GuardPathsLeaveTheBlockUntouched()
    {
        var Local_Original = DspCharacterization.Noise(8, 32000UL);

        void AssertUntouched(AuxGet filter, DSP_Stream stream, string reason)
        {
            var Local_Input = DspCharacterization.Copy(Local_Original);
            var Local_Result = filter.Transform(Local_Input, stream);
            Assert.IsTrue(ReferenceEquals(Local_Input, Local_Result), reason + " - must return the same instance");
            DspCharacterization.AssertExact(Local_Original, Local_Result, reason);
        }

        var Local_Populated = new DSP_Stream { AuxBuffer = new double[4][] };
        Local_Populated.AuxBuffer[1] = DspCharacterization.Constant(8, 0.5);

        AssertUntouched(new AuxGet { AuxGetIndex = -1 }, Local_Populated, "Negative aux index");
        AssertUntouched(new AuxGet { AuxGetIndex = 99 }, Local_Populated, "Aux index past the end");
        AssertUntouched(new AuxGet { AuxGetIndex = 0 }, Local_Populated, "Null aux row");

        var Local_WrongLength = new DSP_Stream { AuxBuffer = new double[4][] };
        Local_WrongLength.AuxBuffer[0] = new double[4];
        AssertUntouched(new AuxGet { AuxGetIndex = 0 }, Local_WrongLength, "Aux row length mismatch");

        AssertUntouched(new AuxGet { AuxGetIndex = 0 }, new DSP_Stream(), "Empty aux array");
    }

    /// <summary>
    /// Transform mutates the caller's array in place and returns that same instance.
    /// </summary>
    [TestMethod]
    public void Contract_Transform_MutatesInPlaceAndReturnsTheInputInstance()
    {
        var Local_Stream = new DSP_Stream { AuxBuffer = new double[4][] };
        Local_Stream.AuxBuffer[0] = DspCharacterization.Constant(8, 0.25);

        var Local_Input = DspCharacterization.Constant(8, 0.5);
        var Local_Result = new AuxGet { AuxGetIndex = 0, StreamAttenuation = 0, AuxAttenuation = 0 }
            .Transform(Local_Input, Local_Stream);

        Assert.IsTrue(ReferenceEquals(Local_Input, Local_Result));
        DspCharacterization.AssertExact(0.75d, Local_Input[0], "The caller's array was written in place");
    }

    #endregion
}
