#nullable enable

namespace Test_Project_1;

#region Usings
using BassThatHz_ASIO_DSP_Processor;
using BassThatHz_ASIO_DSP_Processor.DSP.Filters;
using Test_Project_1.TestHelpers;
#endregion

/// <summary>
/// CHARACTERIZATION / GUARD SUITE for DSP\Filters\AuxSet.cs.
///
/// AuxSet's whole job is a side effect - it publishes the current block onto the per-stream aux
/// bus - so BOTH the returned block and the resulting DSP_Stream.AuxBuffer contents are pinned,
/// over multiple consecutive blocks and across a block-size change (which forces the aux row to be
/// reallocated).
/// </summary>
[TestClass]
public class Test_AuxSet_Characterization
{
    #region Golden Vectors

    /// <summary>
    /// Four consecutive blocks. The block passes through untouched and is mirrored into aux row 3.
    /// </summary>
    [TestMethod]
    public void Golden_Transform_PublishesEachBlockToTheAuxBus()
    {
        double[][] Local_Expected =
        {
            new[] { -0.8394547348884016d, 0.8728582160142064d, 0.49921735082729723d, 0.9799269110662463d, 0.34287266418247686d, -0.6650850445645731d, -0.24169305535226693d, -0.768874244197139d },
            new[] { -0.14945370289956306d, -0.31172441881193835d, 0.5852894935036914d, 0.9432212846921653d, -0.8334834305497874d, -0.7857817456423812d, 0.7079569662086287d, 0.6500055475337572d },
            new[] { 0.5405473290892757d, 0.5036929463619169d, 0.6713616361800856d, 0.906515658318084d, -0.009839525282051564d, -0.9064784467201894d, -0.34239301223047547d, 0.06888533926465357d },
            new[] { -0.7694516389218855d, -0.6808896884642281d, 0.75743377885648d, 0.869810031944003d, 0.8138043799856842d, 0.9728248522020027d, 0.6072570093304204d, -0.51223486900445d },
        };

        var Local_Stream = new DSP_Stream();
        var Local_Filter = new AuxSet { AuxSetIndex = 3 };

        for (int Local_Block = 0; Local_Block < Local_Expected.Length; Local_Block++)
        {
            var Local_Input = DspCharacterization.Noise(8, (ulong)(4000 + Local_Block));
            var Local_Result = Local_Filter.Transform(Local_Input, Local_Stream);

            DspCharacterization.AssertExact(Local_Expected[Local_Block], Local_Result, "Returned block " + Local_Block);
            DspCharacterization.AssertExact(Local_Expected[Local_Block], Local_Stream.AuxBuffer[3], "Aux row after block " + Local_Block);
        }

        Assert.AreEqual(DSP_Stream.NumberOfAuxBuffers, Local_Stream.AuxBuffer.Length,
            "The aux array is grown to the full NumberOfAuxBuffers on first use");
        Assert.AreEqual(256, Local_Stream.AuxBuffer.Length, "NumberOfAuxBuffers is 256");
    }

    /// <summary>
    /// MuteAfter clears the block AFTER publishing it, so the aux bus keeps the audio and the
    /// downstream chain sees silence.
    /// </summary>
    [TestMethod]
    public void Golden_Transform_MuteAfterClearsTheBlockButNotTheAuxBus()
    {
        var Local_Stream = new DSP_Stream();
        var Local_Filter = new AuxSet { AuxSetIndex = 0, MuteAfter = true };

        double[] Local_ExpectedAux =
        {
            -0.8394547348884016d, 0.8728582160142064d, 0.49921735082729723d, 0.9799269110662463d,
            0.34287266418247686d, -0.6650850445645731d, -0.24169305535226693d, -0.768874244197139d
        };

        var Local_Result = Local_Filter.Transform(DspCharacterization.Noise(8, 4000UL), Local_Stream);

        DspCharacterization.AssertExact(new double[8], Local_Result, "MuteAfter must zero the block");
        DspCharacterization.AssertExact(Local_ExpectedAux, Local_Stream.AuxBuffer[0], "The aux bus keeps the pre-mute audio");
    }

    #endregion

    #region Stateful Behavior

    /// <summary>
    /// A block-size change forces the aux row to be reallocated at the new length; the previous
    /// contents must not survive.
    /// </summary>
    [TestMethod]
    public void Stateful_Transform_ReallocatesTheAuxRowOnABlockSizeChange()
    {
        var Local_Stream = new DSP_Stream();
        var Local_Filter = new AuxSet { AuxSetIndex = 5 };

        Local_Filter.Transform(DspCharacterization.Constant(8, 0.5), Local_Stream);
        var Local_FirstRow = Local_Stream.AuxBuffer[5];
        Assert.AreEqual(8, Local_FirstRow.Length);

        Local_Filter.Transform(DspCharacterization.Constant(4, 0.25), Local_Stream);
        Assert.AreEqual(4, Local_Stream.AuxBuffer[5].Length, "The row is resized to the new block length");
        for (int i = 0; i < 4; i++)
            DspCharacterization.AssertExact(0.25d, Local_Stream.AuxBuffer[5][i], "Sample " + i);

        Local_Filter.Transform(DspCharacterization.Constant(8, 0.125), Local_Stream);
        Assert.AreEqual(8, Local_Stream.AuxBuffer[5].Length, "And back again");
        for (int i = 0; i < 8; i++)
            DspCharacterization.AssertExact(0.125d, Local_Stream.AuxBuffer[5][i], "Sample " + i);
    }

    /// <summary>
    /// A same-size block reuses the existing aux row instance rather than allocating - a property
    /// the real-time path depends on and which an optimization must preserve.
    /// </summary>
    [TestMethod]
    public void Stateful_Transform_ReusesTheAuxRowInstanceWhenTheSizeIsUnchanged()
    {
        var Local_Stream = new DSP_Stream();
        var Local_Filter = new AuxSet { AuxSetIndex = 2 };

        Local_Filter.Transform(DspCharacterization.Constant(8, 0.5), Local_Stream);
        var Local_Row = Local_Stream.AuxBuffer[2];

        for (int Local_Block = 0; Local_Block < 6; Local_Block++)
        {
            Local_Filter.Transform(DspCharacterization.Noise(8, (ulong)(33000 + Local_Block)), Local_Stream);
            Assert.IsTrue(ReferenceEquals(Local_Row, Local_Stream.AuxBuffer[2]),
                "Block " + Local_Block + " must reuse the same aux row instance");
        }
    }

    /// <summary>
    /// An existing but undersized aux array is grown to NumberOfAuxBuffers, preserving the rows it
    /// already held.
    /// </summary>
    [TestMethod]
    public void Stateful_Transform_GrowsAnUndersizedAuxArrayPreservingExistingRows()
    {
        var Local_Stream = new DSP_Stream { AuxBuffer = new double[2][] };
        var Local_Preexisting = DspCharacterization.Constant(8, 0.75);
        Local_Stream.AuxBuffer[0] = Local_Preexisting;

        new AuxSet { AuxSetIndex = 1 }.Transform(DspCharacterization.Constant(8, 0.5), Local_Stream);

        Assert.AreEqual(256, Local_Stream.AuxBuffer.Length);
        Assert.IsTrue(ReferenceEquals(Local_Preexisting, Local_Stream.AuxBuffer[0]),
            "Rows that already existed must be carried over, not dropped");
    }

    #endregion

    #region Guard Paths

    /// <summary>
    /// An out-of-range aux index is a silent no-op that neither touches the block nor creates an
    /// aux array.
    /// </summary>
    [TestMethod]
    public void Contract_Transform_OutOfRangeIndexIsANoOp()
    {
        var Local_Original = DspCharacterization.Noise(8, 34000UL);

        foreach (int Local_Index in new[] { -1, DSP_Stream.NumberOfAuxBuffers, DSP_Stream.NumberOfAuxBuffers + 10 })
        {
            var Local_Stream = new DSP_Stream();
            var Local_Input = DspCharacterization.Copy(Local_Original);
            var Local_Result = new AuxSet { AuxSetIndex = Local_Index, MuteAfter = true }.Transform(Local_Input, Local_Stream);

            Assert.IsTrue(ReferenceEquals(Local_Input, Local_Result), "Index " + Local_Index);
            DspCharacterization.AssertExact(Local_Original, Local_Result, "Index " + Local_Index + " must leave the block alone");
            Assert.AreEqual(0, Local_Stream.AuxBuffer.Length, "Index " + Local_Index + " must not allocate an aux array");
        }
    }

    /// <summary>
    /// Transform returns the caller's array instance.
    /// </summary>
    [TestMethod]
    public void Contract_Transform_ReturnsTheInputInstance()
    {
        var Local_Input = DspCharacterization.Noise(8, 34100UL);
        var Local_Result = new AuxSet { AuxSetIndex = 0 }.Transform(Local_Input, new DSP_Stream());
        Assert.IsTrue(ReferenceEquals(Local_Input, Local_Result));
    }

    #endregion
}
