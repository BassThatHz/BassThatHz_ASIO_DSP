#nullable enable

namespace Test_Project_1;

#region Usings
using BassThatHz_ASIO_DSP_Processor;
using System;
using Test_Project_1.TestHelpers;
#endregion

/// <summary>
/// CHARACTERIZATION / GUARD SUITE for DSP\Filters\Floor.cs.
///
/// Floor is a downward expander that engages on samples whose magnitude falls below MinValue, and
/// it holds that engagement for HoldInMS after the last detection. IsActive, StartTime and
/// LastDetection carry between blocks, so the sequences below drive several consecutive blocks.
///
/// WALL-CLOCK NOTE: Floor calls DateTime.UtcNow once per SAMPLE and compares the elapsed time
/// against HoldInMS. Every test here sets HoldInMS to one hour, which makes the hold window
/// unconditionally open for the duration of a test and therefore removes all timing sensitivity -
/// the outputs then depend only on the sample values.
/// </summary>
[TestClass]
public class Test_Floor_Characterization
{
    #region Golden Inputs

    private static double[][] BuildInputs()
    {
        return new[]
        {
            DspCharacterization.Sine(8, 1, 0.5),
            DspCharacterization.Sine(8, 1, 0.1),
            DspCharacterization.Constant(8, 0.0),
            DspCharacterization.Constant(8, 0.05),
            DspCharacterization.Alternating(8, 1.0),
            DspCharacterization.Noise(8, 16000UL),
        };
    }

    #endregion

    #region Multi-Block Stateful Sequence

    /// <summary>
    /// Seven consecutive blocks with MinValue 0.25 and Ratio 1.1: a loud sine (mostly above the
    /// floor), a quiet sine (entirely below it), silence, a constant well below the floor,
    /// full-scale alternating, deterministic noise, and finally a SHORTER block.
    ///
    /// Note block 0: the sine's near-zero sample at index 4 (about 6.1e-17) IS below the floor and
    /// is non-zero, so it is expanded down further - exactly the sort of denormal-adjacent detail an
    /// optimization could quietly change.
    /// </summary>
    [TestMethod]
    public void Stateful_Transform_SevenBlocksIncludingABlockSizeChange()
    {
        double[][] Local_Expected =
        {
            new[] { 0.0d, 0.3535533905932738d, 0.5d, 0.3535533905932738d, 2.3323984883273815E-18d, -0.35355339059327373d, -0.5d, -0.35355339059327384d },
            new[] { 0.0d, 0.06304134293587772d, 0.0920075858503551d, 0.06304134293587772d, 4.029860561809188E-19d, -0.06304134293587771d, -0.0920075858503551d, -0.06304134293587772d },
            new[] { 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d },
            new[] { 0.04319438318512952d, 0.04319438318512952d, 0.04319438318512952d, 0.04319438318512952d, 0.04319438318512952d, 0.04319438318512952d, 0.04319438318512952d, 0.04319438318512952d },
            new[] { 1.0d, -1.0d, 1.0d, -1.0d, 1.0d, -1.0d, 1.0d, -1.0d },
            new[] { -0.8270708688233699d, -0.11098922467523004d, -0.6350705324422115d, 0.5124104220931809d, 0.06209385943113206d, 0.9745020217382567d, -0.4414343246027559d, -0.2081728862893819d },
            new[] { 0.04319438318512952d, 0.04319438318512952d, 0.04319438318512952d, 0.04319438318512952d },
        };

        var Local_Stream = new DSP_Stream();
        var Local_Floor = new Floor { MinValue = 0.25, Ratio = 1.1, HoldInMS = TimeSpan.FromHours(1) };
        var Local_Inputs = BuildInputs();

        for (int Local_Block = 0; Local_Block < Local_Inputs.Length; Local_Block++)
        {
            var Local_Result = Local_Floor.Transform(DspCharacterization.Copy(Local_Inputs[Local_Block]), Local_Stream);
            DspCharacterization.AssertExact(Local_Expected[Local_Block], Local_Result, "Block " + Local_Block);
        }

        DspCharacterization.AssertExact(Local_Expected[6],
            Local_Floor.Transform(DspCharacterization.Constant(4, 0.05), Local_Stream), "Block 6 (size drops to 4)");
    }

    /// <summary>
    /// A steeper Ratio over four consecutive blocks of the same signal. Floor's expansion depends
    /// only on the sample value (not on any smoothed envelope), so every block must be identical -
    /// which is itself a useful invariant to pin.
    /// </summary>
    [TestMethod]
    public void Stateful_Transform_SteeperRatioIsBlockIndependent()
    {
        double[] Local_Expected =
        {
            0.0d, 0.054849635135241485d, 0.10059467437463486d, 0.054849635135241485d,
            1.4341480266529716E-29d, -0.05484963513524145d, -0.10059467437463486d, -0.054849635135241485d
        };

        var Local_Stream = new DSP_Stream();
        var Local_Floor = new Floor { MinValue = 0.5, Ratio = 4.0, HoldInMS = TimeSpan.FromHours(1) };

        for (int Local_Block = 0; Local_Block < 4; Local_Block++)
        {
            var Local_Result = Local_Floor.Transform(DspCharacterization.Sine(8, 1, 0.2), Local_Stream);
            DspCharacterization.AssertExact(Local_Expected, Local_Result, "Block " + Local_Block);
        }
    }

    #endregion

    #region Property Tests

    /// <summary>
    /// MinValue 0 disables the filter completely: no sample can be strictly below zero in
    /// magnitude, so the block is a bit-exact pass-through.
    /// </summary>
    [TestMethod]
    public void Golden_Transform_MinValueZeroIsAnExactPassThrough()
    {
        var Local_Stream = new DSP_Stream();
        var Local_Floor = new Floor { MinValue = 0.0, Ratio = 1.1, HoldInMS = TimeSpan.FromHours(1) };

        var Local_Original = DspCharacterization.Noise(8, 16100UL);
        DspCharacterization.AssertExact(
            new double[]
            {
                0.17303233006050545d, -0.5770231803384169d, -0.027856264802790687d, 0.8418477846850718d,
                0.4341264037855015d, 0.9048319139574468d, 0.5235678314868233d, -0.32339430035165995d
            },
            Local_Floor.Transform(DspCharacterization.Copy(Local_Original), Local_Stream),
            "MinValue 0 must pass everything through untouched");
    }

    /// <summary>
    /// Exact zero is EXCLUDED from detection (the code tests currentSample != 0.0), so silence
    /// passes through as exactly zero no matter what MinValue is.
    /// </summary>
    [TestMethod]
    public void Property_Transform_ExactZeroIsNeverExpanded()
    {
        var Local_Stream = new DSP_Stream();
        var Local_Floor = new Floor { MinValue = 0.5, Ratio = 4.0, HoldInMS = TimeSpan.FromHours(1) };

        for (int Local_Block = 0; Local_Block < 4; Local_Block++)
        {
            var Local_Result = Local_Floor.Transform(new double[8], Local_Stream);
            for (int i = 0; i < Local_Result.Length; i++)
                DspCharacterization.AssertExact(0.0d, Local_Result[i], "Block " + Local_Block + " sample " + i);
        }
    }

    /// <summary>
    /// Samples at or above MinValue are passed through bit-exactly, and the expansion only ever
    /// makes a sample quieter (never louder) and never flips its sign.
    /// </summary>
    [TestMethod]
    public void Property_Transform_OnlyAttenuatesAndOnlyBelowTheFloor()
    {
        var Local_Stream = new DSP_Stream();
        var Local_Floor = new Floor { MinValue = 0.25, Ratio = 2.0, HoldInMS = TimeSpan.FromHours(1) };

        for (int Local_Block = 0; Local_Block < 6; Local_Block++)
        {
            var Local_Original = DspCharacterization.Noise(32, (ulong)(41000 + Local_Block));
            var Local_Result = Local_Floor.Transform(DspCharacterization.Copy(Local_Original), Local_Stream);

            for (int i = 0; i < Local_Original.Length; i++)
            {
                double Local_In = Local_Original[i];
                double Local_Out = Local_Result[i];

                if (Math.Abs(Local_In) >= 0.25d)
                {
                    DspCharacterization.AssertExact(Local_In, Local_Out,
                        "Block " + Local_Block + " sample " + i + " is at or above the floor and must pass through untouched");
                }
                else
                {
                    Assert.IsTrue(Math.Abs(Local_Out) <= Math.Abs(Local_In) + 1e-15,
                        "Block " + Local_Block + " sample " + i + " was amplified: " + Local_In + " -> " + Local_Out);
                    Assert.IsTrue(Math.Sign(Local_Out) == Math.Sign(Local_In) || Local_Out == 0.0,
                        "Block " + Local_Block + " sample " + i + " changed sign");
                }
            }
        }
    }

    /// <summary>
    /// Full-scale content is far above any sensible floor and passes through untouched.
    /// </summary>
    [TestMethod]
    public void Property_Transform_FullScaleIsUntouched()
    {
        var Local_Stream = new DSP_Stream();
        var Local_Floor = new Floor { MinValue = 0.25, Ratio = 1.1, HoldInMS = TimeSpan.FromHours(1) };

        foreach (var Local_Signal in new[] { DspCharacterization.Alternating(8, 1.0), DspCharacterization.Constant(8, 1.0), DspCharacterization.Constant(8, -1.0) })
        {
            var Local_Original = DspCharacterization.Copy(Local_Signal);
            DspCharacterization.AssertExact(Local_Original,
                Local_Floor.Transform(DspCharacterization.Copy(Local_Signal), Local_Stream), "Full-scale block");
        }
    }

    #endregion

    #region Aliasing / Mutation Contract

    /// <summary>
    /// Transform mutates the caller's array in place and returns that same instance; an empty block
    /// short-circuits.
    /// </summary>
    [TestMethod]
    public void Contract_Transform_MutatesInPlaceAndReturnsTheInputInstance()
    {
        var Local_Floor = new Floor { MinValue = 0.25, Ratio = 2.0, HoldInMS = TimeSpan.FromHours(1) };

        var Local_Input = DspCharacterization.Constant(8, 0.05);
        var Local_Result = Local_Floor.Transform(Local_Input, new DSP_Stream());
        Assert.IsTrue(ReferenceEquals(Local_Input, Local_Result));
        Assert.AreNotEqual(0.05d, Local_Input[0], "The caller's array was written in place");

        var Local_Empty = new double[0];
        var Local_EmptyResult = Local_Floor.Transform(Local_Empty, new DSP_Stream());
        Assert.IsTrue(ReferenceEquals(Local_Empty, Local_EmptyResult));
        Assert.AreEqual(0, Local_EmptyResult.Length);
    }

    #endregion
}
