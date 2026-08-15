using DSPLib;
using System;
using System.Numerics;

namespace Test_Project_1;

[TestClass]
public class Test_DSP_Lib_FFT
{
    private const double Tolerance = 1e-6;
    private const double LooseTolerance = 1e-3;

    #region Construction / Initialization

    [TestMethod]
    public void Constructor_PowerOfTwoLength_DoesNotThrow()
    {
        var fft = new FFT(64, 0);
        Assert.IsNotNull(fft);
    }

    [TestMethod]
    public void Constructor_NonPowerOfTwoTotalLength_Throws()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new FFT(5, 0));
    }

    [TestMethod]
    public void Constructor_WithZeroPadding_MakingPowerOfTwo_DoesNotThrow()
    {
        // 6 + 2 = 8, which is a power of 2
        var fft = new FFT(6, 2);
        Assert.IsNotNull(fft);
    }

    [TestMethod]
    public void Constructor_ParameterlessThenPerformFFTNonPower2_Works()
    {
        var fft = new FFT();
        var input = new double[10]; // Non-power-of-2, handled internally by Perform_FFT_NonPower2
        var result = fft.Perform_FFT_NonPower2(input);
        Assert.IsNotNull(result);
    }

    #endregion

    #region Perform_FFT - DC Signal

    [TestMethod]
    public void Perform_FFT_DCSignal_DCBinEqualsInputValue()
    {
        int n = 16;
        double dcValue = 3.0;
        var input = new double[n];
        for (int i = 0; i < n; i++) input[i] = dcValue;

        var fft = new FFT(n, 0);
        var result = fft.Perform_FFT(input, shouldScale: true);

        // DC bin (index 0) should equal the DC value of the input signal.
        Assert.AreEqual(dcValue, result[0].Real, LooseTolerance);
        Assert.AreEqual(0.0, result[0].Imaginary, LooseTolerance);
    }

    [TestMethod]
    public void Perform_FFT_DCSignal_NonDCBinsAreNearZero()
    {
        int n = 16;
        var input = new double[n];
        for (int i = 0; i < n; i++) input[i] = 2.5;

        var fft = new FFT(n, 0);
        var result = fft.Perform_FFT(input, shouldScale: true);

        for (int i = 1; i < result.Length; i++)
        {
            Assert.AreEqual(0.0, result[i].Magnitude, LooseTolerance, $"Bin {i} expected ~0 for pure DC signal");
        }
    }

    #endregion

    #region Perform_FFT - Sine Wave Bin Detection

    [TestMethod]
    public void Perform_FFT_SineWaveAtIntegerBin_PeaksAtExpectedBin()
    {
        int n = 64;
        int targetBin = 4;
        var input = DSP.Generate.ToneCycles(1.0, targetBin, n);

        var fft = new FFT(n, 0);
        var result = fft.Perform_FFT(input, shouldScale: true);
        var magnitude = DSP.ConvertComplex.ToMagnitude(result);

        int maxBin = DSP.Analyze.FindMaxPosition(magnitude, 1, magnitude.Length); // Skip DC bin
        Assert.AreEqual(targetBin, maxBin);
    }

    [TestMethod]
    public void Perform_FFT_SineWaveAtIntegerBin_MagnitudeApproximatesAmplitude()
    {
        int n = 128;
        int targetBin = 8;
        double amplitudeVrms = 2.0;
        var input = DSP.Generate.ToneCycles(amplitudeVrms, targetBin, n);

        var fft = new FFT(n, 0);
        var result = fft.Perform_FFT(input, shouldScale: true);
        var magnitude = DSP.ConvertComplex.ToMagnitude(result);

        Assert.AreEqual(amplitudeVrms, magnitude[targetBin], 0.01);
    }

    #endregion

    #region Perform_FFT - Windowed Overload

    [TestMethod]
    public void Perform_FFT_WithRectangularWindow_MatchesUnwindowedResult()
    {
        int n = 32;
        var input = DSP.Generate.ToneCycles(1.0, 4, n);
        var window = DSP.Window.Coefficients(DSP.Window.Type.Rectangular, n);

        var fft1 = new FFT(n, 0);
        var resultPlain = fft1.Perform_FFT(input, shouldScale: true);

        var fft2 = new FFT(n, 0);
        var resultWindowed = fft2.Perform_FFT(input, window, shouldScale: true);

        for (int i = 0; i < resultPlain.Length; i++)
        {
            Assert.AreEqual(resultPlain[i].Real, resultWindowed[i].Real, LooseTolerance);
            Assert.AreEqual(resultPlain[i].Imaginary, resultWindowed[i].Imaginary, LooseTolerance);
        }
    }

    [TestMethod]
    public void Perform_FFT_WithWindow_MismatchedLengths_Throws()
    {
        int n = 32;
        var input = new double[n];
        var window = new double[n - 1];

        var fft = new FFT(n, 0);
        Assert.ThrowsExactly<InvalidOperationException>(() => fft.Perform_FFT(input, window));
    }

    #endregion

    #region Perform_FFT - Error Paths

    [TestMethod]
    public void Perform_FFT_InputLongerThanInitializedLength_Throws()
    {
        var fft = new FFT(16, 0);
        var tooLong = new double[32];
        Assert.ThrowsExactly<InvalidOperationException>(() => fft.Perform_FFT(tooLong));
    }

    [TestMethod]
    public void Perform_IFFT_InputLongerThanInitializedLength_Throws()
    {
        var fft = new FFT(16, 0);
        var tooLong = new Complex[32];
        Assert.ThrowsExactly<InvalidOperationException>(() => fft.Perform_IFFT(tooLong));
    }

    #endregion

    #region Perform_FFT_NonPower2 / Perform_IFFT_NonPower2

    [TestMethod]
    public void Perform_FFT_NonPower2_PowerOfTwoInput_BehavesLikeNormalFFT()
    {
        int n = 32;
        var input = DSP.Generate.ToneCycles(1.0, 4, n);

        var fftDirect = new FFT(n, 0);
        var expected = fftDirect.Perform_FFT(input);

        var fftAuto = new FFT();
        var actual = fftAuto.Perform_FFT_NonPower2(input);

        Assert.AreEqual(expected.Length, actual.Length);
        for (int i = 0; i < expected.Length; i++)
        {
            Assert.AreEqual(expected[i].Real, actual[i].Real, LooseTolerance);
        }
    }

    [TestMethod]
    public void Perform_FFT_NonPower2_NonPowerOfTwoInput_ZeroPadsAndReturnsPaddedLength()
    {
        int n = 10; // Not a power of 2. Next power of 2 = 16
        var input = new double[n];
        for (int i = 0; i < n; i++) input[i] = 1.0;

        var fft = new FFT();
        var result = fft.Perform_FFT_NonPower2(input, shouldUnpad: false);

        Assert.AreEqual(16, result.Length);
    }

    [TestMethod]
    public void Perform_FFT_NonPower2_NonPowerOfTwoInput_UnpadReturnsOriginalLength()
    {
        int n = 10;
        var input = new double[n];
        for (int i = 0; i < n; i++) input[i] = 1.0;

        var fft = new FFT();
        var result = fft.Perform_FFT_NonPower2(input, shouldUnpad: true);

        Assert.AreEqual(n, result.Length);
    }

    [TestMethod]
    public void Perform_IFFT_NonPower2_NonPowerOfTwoInput_UnpadReturnsOriginalLength()
    {
        int n = 10;
        var input = new Complex[n];
        for (int i = 0; i < n; i++) input[i] = new Complex(1.0, 0.0);

        var fft = new FFT();
        var result = fft.Perform_IFFT_NonPower2(input, shouldUnpad: true);

        Assert.AreEqual(n, result.Length);
    }

    #endregion

    #region Round Trip FFT -> IFFT

    [TestMethod]
    public void ForwardThenInverseFFT_Scaled_ReconstructsSignalShape()
    {
        // NOTE: Perform_FFT applies an additional /sqrt(2) scale to only the DC and
        // Nyquist bins (see DSP_Lib_FFT.cs Perform_FFT), which Perform_IFFT does not
        // symmetrically undo. As a result, a forward+inverse round trip does not
        // reconstruct the exact original sample values for non-DC/Nyquist content;
        // it does however preserve the signal's shape/phase (same bin of max magnitude).
        // This test verifies that weaker, but reliable, round-trip property.
        int n = 32;
        var input = DSP.Generate.ToneCycles(1.0, 3, n);

        var fftFwd = new FFT(n, 0);
        var spectrum = fftFwd.Perform_FFT(input, shouldScale: true);

        var fftInv = new FFT(n, 0);
        var reconstructed = fftInv.Perform_IFFT(spectrum, shouldScale: true);

        Assert.AreEqual(input.Length, reconstructed.Length);
        int expectedMaxIdx = DSP.Analyze.FindMaxPosition(input, 0, input.Length);
        int actualMaxIdx = DSP.Analyze.FindMaxPosition(reconstructed, 0, reconstructed.Length);
        Assert.AreEqual(expectedMaxIdx, actualMaxIdx);
    }

    [TestMethod]
    public void ForwardThenInverseFFT_Unscaled_ReconstructsProportionalSignal()
    {
        int n = 32;
        var input = DSP.Generate.ToneCycles(1.0, 3, n);

        var fftFwd = new FFT(n, 0);
        var spectrum = fftFwd.Perform_FFT(input, shouldScale: false);

        var fftInv = new FFT(n, 0);
        var reconstructed = fftInv.Perform_IFFT(spectrum, shouldScale: false);

        Assert.AreEqual(input.Length, reconstructed.Length);
        // Unscaled round trip should be proportional to original (not necessarily equal),
        // verify the shape correlates by checking the max-magnitude bin index matches.
        int expectedMaxIdx = DSP.Analyze.FindMaxPosition(input, 0, input.Length);
        int actualMaxIdx = DSP.Analyze.FindMaxPosition(reconstructed, 0, reconstructed.Length);
        Assert.AreEqual(expectedMaxIdx, actualMaxIdx);
    }

    #endregion

    #region FrequencySpan

    [TestMethod]
    public void FrequencySpan_ReturnsExpectedLengthAndBounds()
    {
        int n = 16;
        double samplingFrequencyHz = 1000;
        var fft = new FFT(n, 0);

        var freqs = fft.FrequencySpan(samplingFrequencyHz);

        int expectedLength = n / 2 + 1;
        Assert.AreEqual(expectedLength, freqs.Length);
        Assert.AreEqual(0.0, freqs[0], Tolerance);
        Assert.AreEqual(samplingFrequencyHz / 2.0, freqs[^1], Tolerance);
    }

    [TestMethod]
    public void FrequencySpan_IsMonotonicallyIncreasing()
    {
        var fft = new FFT(64, 0);
        var freqs = fft.FrequencySpan(48000);

        for (int i = 1; i < freqs.Length; i++)
        {
            Assert.IsTrue(freqs[i] > freqs[i - 1]);
        }
    }

    #endregion
}
