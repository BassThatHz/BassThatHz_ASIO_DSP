#nullable enable

namespace Test_Project_1.TestHelpers;

#region Usings
using System;
using System.Globalization;
using System.Numerics;
using System.Text;
#endregion

/// <summary>
/// Shared infrastructure for the DSP characterization ("golden vector") test suite.
///
/// PURPOSE
/// The DSP surface (DSP_Lib, DSP_Lib_FFT and the 17 filters under DSP\Filters\) is scheduled for
/// real-time optimization work. These helpers exist so that every characterization test can pin the
/// CURRENT numerical behavior BIT-EXACTLY. An optimization that is genuinely behavior-preserving
/// will keep every one of those bits identical; anything that changes the arithmetic - reassociating
/// a sum, folding a multiply, switching to FMA, changing an accumulation order - will show up here
/// instead of in someone's loudspeakers.
///
/// DETERMINISM
/// Every signal generator here is a pure function of its arguments. There is deliberately no use of
/// System.Random (whose algorithm is not contractually stable), no wall-clock, and no dependence on
/// test ordering. <see cref="Noise"/> implements its own fixed 64-bit LCG so that the produced
/// samples are byte-for-byte reproducible on any machine and any framework version.
/// </summary>
public static class DspCharacterization
{
    #region Deterministic Signal Generators

    /// <summary>
    /// Deterministic pseudo-random signal in [-1, +1), produced by a self-contained 64-bit LCG
    /// (the Knuth/MMIX constants). Deliberately NOT System.Random: the framework does not guarantee
    /// a stable sequence across versions, which would make golden vectors rot.
    /// </summary>
    /// <param name="length">Sample count.</param>
    /// <param name="seed">Fixed seed. Same seed always yields the same samples.</param>
    public static double[] Noise(int length, ulong seed)
    {
        var Local_Result = new double[length];
        ulong Local_State = seed;
        for (int i = 0; i < length; i++)
        {
            Local_State = unchecked(Local_State * 6364136223846793005UL + 1442695040888963407UL);
            ulong Local_Bits = Local_State >> 11; // top 53 bits -> exactly representable
            double Local_Unit = Local_Bits * (1.0 / 9007199254740992.0); // [0, 1)
            Local_Result[i] = Local_Unit * 2.0 - 1.0;
        }
        return Local_Result;
    }

    /// <summary>
    /// Sine wave with an exact integer number of cycles per block, so it lands precisely on an FFT
    /// bin centre. x[i] = amplitude * sin(2*pi*cycles*i/length).
    /// </summary>
    public static double[] Sine(int length, double cycles, double amplitude)
    {
        var Local_Result = new double[length];
        for (int i = 0; i < length; i++)
            Local_Result[i] = amplitude * Math.Sin(2.0 * Math.PI * cycles * i / length);
        return Local_Result;
    }

    /// <summary>
    /// Cosine wave with an exact integer number of cycles per block.
    /// </summary>
    public static double[] Cosine(int length, double cycles, double amplitude)
    {
        var Local_Result = new double[length];
        for (int i = 0; i < length; i++)
            Local_Result[i] = amplitude * Math.Cos(2.0 * Math.PI * cycles * i / length);
        return Local_Result;
    }

    /// <summary>
    /// Unit impulse: all zeros except <paramref name="index"/>, which holds <paramref name="amplitude"/>.
    /// </summary>
    public static double[] Impulse(int length, int index, double amplitude)
    {
        var Local_Result = new double[length];
        if (index >= 0 && index < length)
            Local_Result[index] = amplitude;
        return Local_Result;
    }

    /// <summary>
    /// Constant (DC) block.
    /// </summary>
    public static double[] Constant(int length, double value)
    {
        var Local_Result = new double[length];
        for (int i = 0; i < length; i++)
            Local_Result[i] = value;
        return Local_Result;
    }

    /// <summary>
    /// Alternating +amplitude / -amplitude (Nyquist-rate square). Exercises the worst case for any
    /// filter with feedback state.
    /// </summary>
    public static double[] Alternating(int length, double amplitude)
    {
        var Local_Result = new double[length];
        for (int i = 0; i < length; i++)
            Local_Result[i] = (i % 2 == 0) ? amplitude : -amplitude;
        return Local_Result;
    }

    /// <summary>
    /// Linear ramp: start, start+step, start+2*step, ...
    /// </summary>
    public static double[] Ramp(int length, double start, double step)
    {
        var Local_Result = new double[length];
        for (int i = 0; i < length; i++)
            Local_Result[i] = start + step * i;
        return Local_Result;
    }

    /// <summary>
    /// Defensive copy so a test can prove whether Transform mutated the caller's array.
    /// </summary>
    public static double[] Copy(double[] source)
    {
        var Local_Result = new double[source.Length];
        Array.Copy(source, Local_Result, source.Length);
        return Local_Result;
    }

    #endregion

    #region Exact (bit-identical) Assertions

    /// <summary>
    /// Bit-identical double comparison. This is the DEFAULT for golden vectors: an optimization must
    /// not perturb the arithmetic at all, so "close enough" is not good enough. Compares the raw IEEE
    /// 754 bit patterns, which also distinguishes +0.0 from -0.0 and makes NaN compare equal to NaN.
    /// </summary>
    public static void AssertExact(double expected, double actual, string message)
    {
        long Local_ExpectedBits = BitConverter.DoubleToInt64Bits(expected);
        long Local_ActualBits = BitConverter.DoubleToInt64Bits(actual);
        if (Local_ExpectedBits != Local_ActualBits)
        {
            Assert.Fail(
                message + Environment.NewLine +
                "  expected: " + ToLiteral(expected) + " (bits 0x" + Local_ExpectedBits.ToString("X16", CultureInfo.InvariantCulture) + ")" + Environment.NewLine +
                "  actual:   " + ToLiteral(actual) + " (bits 0x" + Local_ActualBits.ToString("X16", CultureInfo.InvariantCulture) + ")");
        }
    }

    /// <summary>
    /// Bit-identical comparison of a whole block.
    /// </summary>
    public static void AssertExact(double[] expected, double[] actual, string message)
    {
        Assert.IsNotNull(actual, message + " (actual array was null)");
        Assert.AreEqual(expected.Length, actual.Length, message + " (length mismatch)");
        for (int i = 0; i < expected.Length; i++)
            AssertExact(expected[i], actual[i], message + " [index " + i.ToString(CultureInfo.InvariantCulture) + "]");
    }

    /// <summary>
    /// Bit-identical comparison of a complex spectrum, real and imaginary parts independently.
    /// </summary>
    public static void AssertExact(Complex[] expected, Complex[] actual, string message)
    {
        Assert.IsNotNull(actual, message + " (actual array was null)");
        Assert.AreEqual(expected.Length, actual.Length, message + " (length mismatch)");
        for (int i = 0; i < expected.Length; i++)
        {
            AssertExact(expected[i].Real, actual[i].Real, message + " [bin " + i.ToString(CultureInfo.InvariantCulture) + "].Real");
            AssertExact(expected[i].Imaginary, actual[i].Imaginary, message + " [bin " + i.ToString(CultureInfo.InvariantCulture) + "].Imaginary");
        }
    }

    /// <summary>
    /// Tolerant comparison, used ONLY for mathematical property tests (round trip, Parseval, ...)
    /// where floating point rounding makes exactness genuinely unattainable. Never used for golden
    /// vectors.
    /// </summary>
    public static void AssertClose(double[] expected, double[] actual, double tolerance, string message)
    {
        Assert.IsNotNull(actual, message + " (actual array was null)");
        Assert.AreEqual(expected.Length, actual.Length, message + " (length mismatch)");
        for (int i = 0; i < expected.Length; i++)
            Assert.AreEqual(expected[i], actual[i], tolerance, message + " [index " + i.ToString(CultureInfo.InvariantCulture) + "]");
    }

    #endregion

    #region Golden Vector Emission (developer aid)

    /// <summary>
    /// Renders a double as a C# literal that round-trips to the identical bit pattern, so a
    /// regenerated golden vector can be pasted straight into a test file.
    /// </summary>
    public static string ToLiteral(double value)
    {
        if (double.IsNaN(value))
            return "double.NaN";
        if (double.IsPositiveInfinity(value))
            return "double.PositiveInfinity";
        if (double.IsNegativeInfinity(value))
            return "double.NegativeInfinity";

        string Local_Text = value.ToString("R", CultureInfo.InvariantCulture);
        if (Local_Text.IndexOf('.') < 0 && Local_Text.IndexOf('E') < 0 && Local_Text.IndexOf('e') < 0)
            Local_Text += ".0";
        return Local_Text + "d";
    }

    /// <summary>
    /// Renders a whole block as a C# array initializer body.
    /// </summary>
    public static string ToLiteralArray(double[] values)
    {
        var Local_Builder = new StringBuilder();
        for (int i = 0; i < values.Length; i++)
        {
            if (i > 0)
                Local_Builder.Append(", ");
            Local_Builder.Append(ToLiteral(values[i]));
        }
        return Local_Builder.ToString();
    }

    #endregion
}
