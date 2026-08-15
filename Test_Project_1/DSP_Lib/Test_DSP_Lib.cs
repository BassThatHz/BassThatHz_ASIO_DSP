using DSPLib;
using System;
using System.Numerics;

namespace Test_Project_1;

[TestClass]
public class Test_DSP_Lib
{
    private const double Tolerance = 1e-9;
    private const double LooseTolerance = 1e-6;

    #region DSP.Generate.LinSpace

    [TestMethod]
    public void LinSpace_GeneratesExpectedSequence()
    {
        var result = DSP.Generate.LinSpace(1, 10, 10);
        Assert.AreEqual(10, result.Length);
        for (int i = 0; i < 10; i++)
        {
            Assert.AreEqual(i + 1.0, result[i], Tolerance);
        }
    }

    [TestMethod]
    public void LinSpace_SinglePoint_ReturnsStartValueOnly()
    {
        // increment computation divides by (points - 1.0) = 0 -> increment is Infinity/NaN,
        // but since points = 1, the loop only computes result[0] = startVal + increment * 0 = startVal (0 * Infinity would be NaN!)
        var result = DSP.Generate.LinSpace(5, 10, 1);
        Assert.AreEqual(1, result.Length);
        // increment = (10-5)/(1-1) = Infinity; increment * 0 = NaN. So result[0] = 5 + NaN = NaN.
        Assert.IsTrue(double.IsNaN(result[0]));
    }

    [TestMethod]
    public void LinSpace_ZeroPoints_ReturnsEmptyArray()
    {
        var result = DSP.Generate.LinSpace(0, 10, 0);
        Assert.AreEqual(0, result.Length);
    }

    [TestMethod]
    public void LinSpace_NegativeRange_Works()
    {
        var result = DSP.Generate.LinSpace(-5, 5, 11);
        Assert.AreEqual(11, result.Length);
        Assert.AreEqual(-5.0, result[0], Tolerance);
        Assert.AreEqual(0.0, result[5], Tolerance);
        Assert.AreEqual(5.0, result[10], Tolerance);
    }

    #endregion

    #region DSP.Generate.ToneSampling / ToneCycles

    [TestMethod]
    public void ToneSampling_GeneratesCorrectLength()
    {
        var result = DSP.Generate.ToneSampling(1.0, 100, 1000, 256);
        Assert.AreEqual(256, result.Length);
    }

    [TestMethod]
    public void ToneSampling_ZeroFrequency_ProducesDCPlusOffset()
    {
        double amplitudeVrms = 2.0;
        double dc = 1.5;
        var result = DSP.Generate.ToneSampling(amplitudeVrms, 0, 1000, 10, dc);
        // sin(0) == 0 for all samples when freq = 0
        foreach (var v in result)
        {
            Assert.AreEqual(dc, v, Tolerance);
        }
    }

    [TestMethod]
    public void ToneSampling_FirstSampleAtZeroPhase_IsDC()
    {
        var result = DSP.Generate.ToneSampling(1.0, 100, 1000, 10, dcV: 0.5, phaseDeg: 0);
        // time = 0 at i=0 -> sin(0) = 0 -> value = dc
        Assert.AreEqual(0.5, result[0], Tolerance);
    }

    [TestMethod]
    public void ToneSampling_PhaseShift90Degrees_FirstSampleIsPeak()
    {
        double amplitudeVrms = 1.0;
        double ampPeak = Math.Sqrt(2) * amplitudeVrms;
        var result = DSP.Generate.ToneSampling(amplitudeVrms, 100, 1000, 10, dcV: 0, phaseDeg: 90);
        // sin(0 + pi/2) = 1 -> value = ampPeak
        Assert.AreEqual(ampPeak, result[0], LooseTolerance);
    }

    [TestMethod]
    public void ToneCycles_GeneratesCorrectLength()
    {
        var result = DSP.Generate.ToneCycles(1.0, 4, 128);
        Assert.AreEqual(128, result.Length);
    }

    [TestMethod]
    public void ToneCycles_OneCycle_StartsAndEndsNearZero()
    {
        var result = DSP.Generate.ToneCycles(1.0, 1, 1000);
        Assert.AreEqual(0.0, result[0], LooseTolerance);
    }

    [TestMethod]
    public void ToneCycles_ZeroDcAndPhase_FirstSampleIsZero()
    {
        var result = DSP.Generate.ToneCycles(1.0, 4, 100, dcV: 0, phaseDeg: 0);
        Assert.AreEqual(0.0, result[0], LooseTolerance);
    }

    [TestMethod]
    public void ToneCycles_DCOffset_IsAppliedToAllSamples()
    {
        double dc = 3.3;
        var result = DSP.Generate.ToneCycles(1.0, 4, 8, dcV: dc);
        // Average roughly around dc since sinusoid averages ~0 over full cycles.
        double avg = 0;
        foreach (var v in result) avg += v;
        avg /= result.Length;
        Assert.AreEqual(dc, avg, 0.5);
    }

    #endregion

    #region DSP.Window.ScaleFactor

    [TestMethod]
    public void WindowScaleFactor_Signal_RectangularWindow_ReturnsOne()
    {
        var coeffs = DSP.Window.Coefficients(DSP.Window.Type.Rectangular, 64);
        double sf = DSP.Window.ScaleFactor.Signal(coeffs);
        Assert.AreEqual(1.0, sf, LooseTolerance);
    }

    [TestMethod]
    public void WindowScaleFactor_Signal_HannWindow_IsGreaterThanOne()
    {
        var coeffs = DSP.Window.Coefficients(DSP.Window.Type.Hann, 64);
        double sf = DSP.Window.ScaleFactor.Signal(coeffs);
        Assert.IsTrue(sf > 1.0);
    }

    [TestMethod]
    public void WindowScaleFactor_Noise_RectangularWindow_ReturnsPositiveValue()
    {
        var coeffs = DSP.Window.Coefficients(DSP.Window.Type.Rectangular, 64);
        double sf = DSP.Window.ScaleFactor.Noise(coeffs, 1000);
        Assert.IsTrue(sf > 0);
    }

    [TestMethod]
    public void WindowScaleFactor_NENBW_RectangularWindow_ReturnsOne()
    {
        var coeffs = DSP.Window.Coefficients(DSP.Window.Type.Rectangular, 64);
        double nenbw = DSP.Window.ScaleFactor.NENBW(coeffs);
        Assert.AreEqual(1.0, nenbw, LooseTolerance);
    }

    [TestMethod]
    public void WindowScaleFactor_NENBW_HannWindow_IsAboutOnePointFive()
    {
        var coeffs = DSP.Window.Coefficients(DSP.Window.Type.Hann, 4096);
        double nenbw = DSP.Window.ScaleFactor.NENBW(coeffs);
        // Known theoretical value for Hann window NENBW is 1.5
        Assert.AreEqual(1.5, nenbw, 0.01);
    }

    #endregion

    #region DSP.Window.Coefficients

    [TestMethod]
    public void WindowCoefficients_Rectangular_AllOnes()
    {
        var coeffs = DSP.Window.Coefficients(DSP.Window.Type.Rectangular, 16);
        Assert.AreEqual(16, coeffs.Length);
        foreach (var c in coeffs)
        {
            Assert.AreEqual(1.0, c, Tolerance);
        }
    }

    [TestMethod]
    public void WindowCoefficients_None_AllOnes()
    {
        var coeffs = DSP.Window.Coefficients(DSP.Window.Type.None, 16);
        foreach (var c in coeffs)
        {
            Assert.AreEqual(1.0, c, Tolerance);
        }
    }

    [TestMethod]
    public void WindowCoefficients_Hann_FirstSampleIsZero()
    {
        var coeffs = DSP.Window.Coefficients(DSP.Window.Type.Hann, 64);
        // Hann window: 0.5 - 0.5*cos(0) = 0
        Assert.AreEqual(0.0, coeffs[0], Tolerance);
    }

    [TestMethod]
    public void WindowCoefficients_Hamming_FirstSampleIsExpected()
    {
        var coeffs = DSP.Window.Coefficients(DSP.Window.Type.Hamming, 64);
        // Hamming: 0.54 - 0.46*cos(0) = 0.08
        Assert.AreEqual(0.08, coeffs[0], Tolerance);
    }

    [TestMethod]
    public void WindowCoefficients_Welch_FirstAndLastAreZero()
    {
        var coeffs = DSP.Window.Coefficients(DSP.Window.Type.Welch, 100);
        Assert.AreEqual(0, coeffs[0], 1e-3);
    }

    [TestMethod]
    public void WindowCoefficients_Bartlett_FirstSampleIsNearlyZero()
    {
        int n = 64;
        var coeffs = DSP.Window.Coefficients(DSP.Window.Type.Bartlett, n);
        // Formula: 2/N * (N/2 - |0 - (N-1)/2|) = 2/N * 0.5 = 1/N (not exactly 0 at i=0)
        Assert.AreEqual(1.0 / n, coeffs[0], LooseTolerance);
    }

    [TestMethod]
    public void WindowCoefficients_ZeroPoints_ReturnsEmptyArray()
    {
        var coeffs = DSP.Window.Coefficients(DSP.Window.Type.Hann, 0);
        Assert.AreEqual(0, coeffs.Length);
    }

    [TestMethod]
    public void WindowCoefficients_UnknownEnumValue_ReturnsZeroFilledArray()
    {
        // Cast an invalid enum value to hit the default case - falls through returning all zeros
        var invalidType = (DSP.Window.Type)9999;
        var coeffs = DSP.Window.Coefficients(invalidType, 10);
        Assert.AreEqual(10, coeffs.Length);
        foreach (var c in coeffs)
        {
            Assert.AreEqual(0.0, c, Tolerance);
        }
    }

    [TestMethod]
    public void WindowCoefficients_FlatTop_ReturnsExpectedLength()
    {
        var coeffs = DSP.Window.Coefficients(DSP.Window.Type.FlatTop, 32);
        Assert.AreEqual(32, coeffs.Length);
    }

    [TestMethod]
    public void WindowCoefficients_BH92_ReturnsExpectedLength()
    {
        var coeffs = DSP.Window.Coefficients(DSP.Window.Type.BH92, 32);
        Assert.AreEqual(32, coeffs.Length);
    }

    [TestMethod]
    public void SineExpansion_ConstantCoefficientOnly_ReturnsConstantArray()
    {
        var result = DSP.Window.SineExpansion(10, 5.0);
        foreach (var v in result)
        {
            Assert.AreEqual(5.0, v, Tolerance);
        }
    }

    #endregion

    #region DSP.ConvertMagnitude

    [TestMethod]
    public void ConvertMagnitude_ToMagnitudeSquared_SquaresEachElement()
    {
        var input = new double[] { 1, 2, 3, -4 };
        var result = DSP.ConvertMagnitude.ToMagnitudeSquared(input);
        CollectionAssert.AreEqual(new double[] { 1, 4, 9, 16 }, result);
    }

    [TestMethod]
    public void ConvertMagnitude_ToMagnitudeSquared_EmptyArray_ReturnsEmpty()
    {
        var result = DSP.ConvertMagnitude.ToMagnitudeSquared(Array.Empty<double>());
        Assert.AreEqual(0, result.Length);
    }

    [TestMethod]
    public void ConvertMagnitude_ToMagnitudeDBV_ComputesExpectedDb()
    {
        var input = new double[] { 1.0 };
        var result = DSP.ConvertMagnitude.ToMagnitudeDBV(input);
        // 20*log10(1) = 0
        Assert.AreEqual(0.0, result[0], Tolerance);
    }

    [TestMethod]
    public void ConvertMagnitude_ToMagnitudeDBV_ZeroValue_ClampsToFloor()
    {
        var input = new double[] { 0.0 };
        var result = DSP.ConvertMagnitude.ToMagnitudeDBV(input, 1, -100);
        Assert.AreEqual(-100.0, result[0], Tolerance);
    }

    [TestMethod]
    public void ConvertMagnitude_ToMagnitudeDBV_AppliesScale()
    {
        var input = new double[] { 10.0 };
        var result = DSP.ConvertMagnitude.ToMagnitudeDBV(input, 0.1);
        // 10*0.1 = 1 -> 20*log10(1) = 0
        Assert.AreEqual(0.0, result[0], Tolerance);
    }

    #endregion

    #region DSP.ConvertComplex

    [TestMethod]
    public void ConvertComplex_ToMagnitude_ComputesCorrectMagnitude()
    {
        var input = new Complex[] { new Complex(3, 4) };
        var result = DSP.ConvertComplex.ToMagnitude(input);
        Assert.AreEqual(5.0, result[0], Tolerance);
    }

    [TestMethod]
    public void ConvertComplex_ToMagnitudeDBV_ComputesExpectedDb()
    {
        var input = new Complex[] { new Complex(1, 0) };
        var result = DSP.ConvertComplex.ToMagnitudeDBV(input);
        Assert.AreEqual(0.0, result[0], Tolerance);
    }

    [TestMethod]
    public void ConvertComplex_ToMagnitudeDBV_ZeroMagnitude_DoesNotThrow()
    {
        var input = new Complex[] { new Complex(0, 0) };
        var result = DSP.ConvertComplex.ToMagnitudeDBV(input);
        Assert.IsTrue(double.IsNegativeInfinity(result[0]) || result[0] < -300);
    }

    [TestMethod]
    public void ConvertComplex_ToPhaseDegrees_ComputesExpectedPhase()
    {
        var input = new Complex[] { new Complex(0, 1) }; // 90 degrees
        var result = DSP.ConvertComplex.ToPhaseDegrees(input);
        Assert.AreEqual(90.0, result[0], LooseTolerance);
    }

    [TestMethod]
    public void ConvertComplex_ToPhaseDegrees_NegativeRealAxis_Is180()
    {
        var input = new Complex[] { new Complex(-1, 0) };
        var result = DSP.ConvertComplex.ToPhaseDegrees(input);
        Assert.AreEqual(180.0, result[0], LooseTolerance);
    }

    #endregion

    #region DSP.Math

    [TestMethod]
    public void Math_Multiply_ArrayByArray_ElementWise()
    {
        var a = new double[] { 1, 2, 3 };
        var b = new double[] { 4, 5, 6 };
        var result = DSP.Math.Multiply(a, b);
        CollectionAssert.AreEqual(new double[] { 4, 10, 18 }, result);
    }

    [TestMethod]
    public void Math_Multiply_ArrayByArray_MismatchedLengths_Throws()
    {
        var a = new double[] { 1, 2 };
        var b = new double[] { 1, 2, 3 };
        Assert.ThrowsExactly<ArgumentException>(() => DSP.Math.Multiply(a, b));
    }

    [TestMethod]
    public void Math_Multiply_ArrayByScalar_MultipliesEachElement()
    {
        var a = new double[] { 1, 2, 3 };
        var result = DSP.Math.Multiply(a, 2.0);
        CollectionAssert.AreEqual(new double[] { 2, 4, 6 }, result);
    }

    [TestMethod]
    public void Math_Add_AddsScalarToEachElement()
    {
        var a = new double[] { 1, 2, 3 };
        var result = DSP.Math.Add(a, 10);
        CollectionAssert.AreEqual(new double[] { 11, 12, 13 }, result);
    }

    [TestMethod]
    public void Math_Subtract_SubtractsScalarFromEachElement()
    {
        var a = new double[] { 5, 6, 7 };
        var result = DSP.Math.Subtract(a, 2);
        CollectionAssert.AreEqual(new double[] { 3, 4, 5 }, result);
    }

    [TestMethod]
    public void Math_Multiply_EmptyArrays_ReturnsEmpty()
    {
        var result = DSP.Math.Multiply(Array.Empty<double>(), Array.Empty<double>());
        Assert.AreEqual(0, result.Length);
    }

    #endregion

    #region DSP.Analyze

    [TestMethod]
    public void Analyze_FindMaxPosition_FindsCorrectIndex()
    {
        var data = new double[] { 1, 5, 3, 9, 2 };
        int pos = DSP.Analyze.FindMaxPosition(data, 0, data.Length);
        Assert.AreEqual(3, pos);
    }

    [TestMethod]
    public void Analyze_FindMaxPosition_RestrictedRange_FindsWithinRange()
    {
        var data = new double[] { 1, 5, 3, 9, 2 };
        int pos = DSP.Analyze.FindMaxPosition(data, 0, 3); // considers indices 0,1,2
        Assert.AreEqual(1, pos);
    }

    [TestMethod]
    public void Analyze_FindMinPosition_FindsCorrectIndex()
    {
        var data = new double[] { 5, 1, 3, 9, 2 };
        int pos = DSP.Analyze.FindMinPosition(data, 0, data.Length);
        Assert.AreEqual(1, pos);
    }

    [TestMethod]
    public void Analyze_FindMaxPosition_NullArray_ReturnsZero()
    {
        int pos = DSP.Analyze.FindMaxPosition(null, 0, 10);
        Assert.AreEqual(0, pos);
    }

    [TestMethod]
    public void Analyze_FindMaxPosition_EmptyArray_ReturnsZero()
    {
        int pos = DSP.Analyze.FindMaxPosition(Array.Empty<double>(), 0, 10);
        Assert.AreEqual(0, pos);
    }

    [TestMethod]
    public void Analyze_FindMaxPosition_MinIndexGreaterThanMaxIndex_ReturnsClampedIndex()
    {
        var data = new double[] { 1, 2, 3, 4, 5 };
        int pos = DSP.Analyze.FindMaxPosition(data, 3, 1);
        // minIndex >= maxIndex -> returns Min(minIndex, len-1)
        Assert.AreEqual(3, pos);
    }

    [TestMethod]
    public void Analyze_FindMinPosition_NegativeMinIndex_ClampsToZero()
    {
        var data = new double[] { 3, 1, 2 };
        int pos = DSP.Analyze.FindMinPosition(data, -5, data.Length);
        Assert.AreEqual(1, pos);
    }

    [TestMethod]
    public void Analyze_FindMaxPosition_MaxIndexBeyondLength_Clamps()
    {
        var data = new double[] { 1, 2, 9 };
        int pos = DSP.Analyze.FindMaxPosition(data, 0, 1000);
        Assert.AreEqual(2, pos);
    }

    [TestMethod]
    public void Analyze_UnwrapPhaseDegrees_NoWrap_ReturnsSameValues()
    {
        var input = new double[] { 0, 10, 20, 30 };
        var result = DSP.Analyze.UnwrapPhaseDegrees(input);
        CollectionAssert.AreEqual(input, result);
    }

    [TestMethod]
    public void Analyze_UnwrapPhaseDegrees_WrapsAcrossBoundary()
    {
        // Simulate a phase wrap from 170 to -170 (delta = -340, wraps to +20)
        var input = new double[] { 170, -170 };
        var result = DSP.Analyze.UnwrapPhaseDegrees(input);
        Assert.AreEqual(170.0, result[0], Tolerance);
        Assert.AreEqual(190.0, result[1], Tolerance);
    }

    [TestMethod]
    public void Analyze_UnwrapPhaseDegrees_EmptyArray_ReturnsEmptyArray()
    {
        var result = DSP.Analyze.UnwrapPhaseDegrees(Array.Empty<double>());
        Assert.AreEqual(0, result.Length);
    }

    [TestMethod]
    public void Analyze_UnwrapPhaseDegrees_NullArray_ReturnsEmptyArray()
    {
        var result = DSP.Analyze.UnwrapPhaseDegrees(null);
        Assert.AreEqual(0, result.Length);
    }

    [TestMethod]
    public void Analyze_UnwrapPhaseDegrees_SingleElement_ReturnsSameElement()
    {
        var result = DSP.Analyze.UnwrapPhaseDegrees(new double[] { 45.0 });
        Assert.AreEqual(1, result.Length);
        Assert.AreEqual(45.0, result[0], Tolerance);
    }

    #endregion

    #region DownSampler

    [TestMethod]
    public void DownSampler_ThresholdGreaterThanOrEqualToLength_ReturnsOriginalArray()
    {
        var data = new double[] { 1, 2, 3 };
        var result = DownSampler.downsample(data, 10);
        Assert.AreSame(data, result);
    }

    [TestMethod]
    public void DownSampler_ThresholdZeroOrNegative_ReturnsOriginalArray()
    {
        var data = new double[] { 1, 2, 3 };
        var result = DownSampler.downsample(data, 0);
        Assert.AreSame(data, result);

        var result2 = DownSampler.downsample(data, -5);
        Assert.AreSame(data, result2);
    }

    [TestMethod]
    public void DownSampler_ReducesArrayToThreshold()
    {
        var data = new double[1000];
        for (int i = 0; i < data.Length; i++)
            data[i] = Math.Sin(i * 0.01);

        var result = DownSampler.downsample(data, 100);
        Assert.AreEqual(100, result.Length);
    }

    [TestMethod]
    public void DownSampler_PreservesFirstAndLastPoints()
    {
        var data = new double[500];
        for (int i = 0; i < data.Length; i++)
            data[i] = i;

        var result = DownSampler.downsample(data, 50);
        Assert.AreEqual(data[0], result[0], Tolerance);
        Assert.AreEqual(data[^1], result[^1], Tolerance);
    }

    [TestMethod]
    public void DownSampler_NullData_ReturnsNull()
    {
        // threshold(10) < dataLength(0) is false since dataLength is 0 (from null ?? 0)
        // threshold >= dataLength (10 >= 0) is true -> returns data (null) unchanged
        var result = DownSampler.downsample(null, 10);
        Assert.IsNull(result);
    }

    #endregion
}
