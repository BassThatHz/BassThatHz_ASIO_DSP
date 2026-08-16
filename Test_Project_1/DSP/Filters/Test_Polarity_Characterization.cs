#nullable enable

namespace Test_Project_1;

#region Usings
using BassThatHz_ASIO_DSP_Processor;
using Test_Project_1.TestHelpers;
#endregion

/// <summary>
/// CHARACTERIZATION / GUARD SUITE for DSP\Filters\Polarity.cs.
///
/// Polarity is stateless, but it has TWO code paths - a SIMD path taken when
/// Vector.IsHardwareAccelerated and the block is at least Vector&lt;double&gt;.Count long, and a scalar
/// remainder path. Both are pinned so a change to the vector width or the loop structure cannot
/// alter a single output bit.
/// </summary>
[TestClass]
public class Test_Polarity_Characterization
{
    #region Golden Vectors

    /// <summary>
    /// Six consecutive blocks through the SIMD path. Polarity carries no state, so each block must
    /// depend only on its own input - a golden sequence proves that too.
    /// </summary>
    [TestMethod]
    public void Golden_Transform_NegatesAcrossSixConsecutiveBlocks()
    {
        double[][] Local_Expected =
        {
            new[] { 0.8425507014046596d, -0.6207626944487412d, -0.2827893216446744d, 0.9031939666904873d, 0.5888431390248847d, 0.5749818111402807d, -0.8082422619603551d, -0.5917505631138991d },
            new[] { 0.15254966941582082d, 0.5638199403774036d, -0.3688614643210686d, 0.9398995930645684d, -0.23480076624285107d, 0.6956785122180886d, 0.24210771647874907d, -0.010630354844795464d },
            new[] { -0.5374513625730177d, -0.2515974247964514d, -0.454933606997463d, 0.9766052194386494d, 0.9415553284894131d, 0.8163752132958968d, -0.7075423050821468d, 0.5704898534243081d },
            new[] { 0.7725476054381435d, 0.9329852100296934d, -0.5410057496738572d, -0.9866891541872693d, 0.11791142322167736d, 0.937071914373705d, 0.3428076733569574d, -0.848389938306588d },
            new[] { 0.08254657344930472d, 0.11756784485583816d, -0.6270778923502514d, -0.9499835278131883d, -0.7057324820460584d, -0.9422313845484871d, -0.6068423482039385d, -0.26726973003748444d },
            new[] { -0.6074544585395341d, -0.6978495203180171d, -0.7131500350266455d, -0.9132779014391073d, 0.4706236126862058d, -0.8215346834706789d, 0.44350763023516593d, 0.31385047823161916d },
        };

        var Local_Stream = new DSP_Stream();
        var Local_Filter = new Polarity { Positive = false };

        for (int Local_Block = 0; Local_Block < Local_Expected.Length; Local_Block++)
        {
            var Local_Input = DspCharacterization.Noise(8, (ulong)(1000 + Local_Block));
            var Local_Result = Local_Filter.Transform(Local_Input, Local_Stream);
            DspCharacterization.AssertExact(Local_Expected[Local_Block], Local_Result, "Block " + Local_Block);
        }
    }

    /// <summary>
    /// A 3-sample block is shorter than a double vector on every current x64 ISA, so it exercises
    /// the scalar remainder loop.
    /// </summary>
    [TestMethod]
    public void Golden_Transform_ScalarRemainderPath()
    {
        var Local_Stream = new DSP_Stream();
        var Local_Filter = new Polarity { Positive = false };

        DspCharacterization.AssertExact(
            new double[] { -0.8935259308572809d, 0.05135260515561968d, 0.9446144924521482d },
            DspCharacterization.Noise(3, 55UL), "The deterministic source itself must not drift");

        DspCharacterization.AssertExact(
            new double[] { 0.8935259308572809d, -0.05135260515561968d, -0.9446144924521482d },
            Local_Filter.Transform(DspCharacterization.Noise(3, 55UL), Local_Stream), "3-sample block");
    }

    #endregion

    #region Property Tests

    /// <summary>
    /// Negation is exact in IEEE 754, so double negation must return the ORIGINAL bits, including
    /// the sign of zero. A vectorized rewrite that used 0.0 - x instead of -x would break this at
    /// negative zero.
    /// </summary>
    [TestMethod]
    public void Property_Transform_IsAnExactInvolution()
    {
        var Local_Stream = new DSP_Stream();
        var Local_Filter = new Polarity { Positive = false };

        foreach (int Local_Length in new[] { 1, 2, 3, 4, 7, 8, 13, 64, 512 })
        {
            var Local_Original = DspCharacterization.Noise(Local_Length, (ulong)(30000 + Local_Length));
            var Local_Working = DspCharacterization.Copy(Local_Original);

            Local_Filter.Transform(Local_Working, Local_Stream);
            Local_Filter.Transform(Local_Working, Local_Stream);

            DspCharacterization.AssertExact(Local_Original, Local_Working,
                "Double negation must be bit-exact for length " + Local_Length);
        }
    }

    /// <summary>
    /// The sign of zero is flipped, not normalized.
    /// </summary>
    [TestMethod]
    public void Property_Transform_FlipsTheSignOfZero()
    {
        var Local_Stream = new DSP_Stream();
        var Local_Filter = new Polarity { Positive = false };

        var Local_Input = new double[8];
        Local_Input[3] = -0.0d;
        var Local_Result = Local_Filter.Transform(Local_Input, Local_Stream);

        DspCharacterization.AssertExact(-0.0d, Local_Result[0], "+0.0 must become -0.0");
        DspCharacterization.AssertExact(0.0d, Local_Result[3], "-0.0 must become +0.0");
    }

    #endregion

    #region Aliasing / Mutation Contract

    /// <summary>
    /// Positive:true is a pure pass-through - the SAME array instance comes back with untouched
    /// contents. Positive:false mutates the caller's array in place and also returns it.
    /// </summary>
    [TestMethod]
    public void Contract_Transform_AlwaysReturnsTheInputInstance()
    {
        var Local_Stream = new DSP_Stream();

        var Local_PassThroughInput = DspCharacterization.Noise(8, 31000UL);
        var Local_PassThroughBefore = DspCharacterization.Copy(Local_PassThroughInput);
        var Local_PassThroughResult = new Polarity { Positive = true }.Transform(Local_PassThroughInput, Local_Stream);
        Assert.IsTrue(ReferenceEquals(Local_PassThroughInput, Local_PassThroughResult), "Positive:true returns the same instance");
        DspCharacterization.AssertExact(Local_PassThroughBefore, Local_PassThroughInput, "Positive:true must not touch the samples");

        var Local_NegateInput = DspCharacterization.Noise(8, 31000UL);
        var Local_NegateResult = new Polarity { Positive = false }.Transform(Local_NegateInput, Local_Stream);
        Assert.IsTrue(ReferenceEquals(Local_NegateInput, Local_NegateResult), "Positive:false returns the same instance");
        DspCharacterization.AssertExact(-Local_PassThroughBefore[0], Local_NegateInput[0], "Positive:false mutates in place");
    }

    /// <summary>
    /// An empty block is handled without throwing and returns the same instance.
    /// </summary>
    [TestMethod]
    public void Contract_Transform_HandlesEmptyBlock()
    {
        var Local_Empty = new double[0];
        var Local_Result = new Polarity { Positive = false }.Transform(Local_Empty, new DSP_Stream());
        Assert.IsTrue(ReferenceEquals(Local_Empty, Local_Result));
        Assert.AreEqual(0, Local_Result.Length);
    }

    #endregion
}
