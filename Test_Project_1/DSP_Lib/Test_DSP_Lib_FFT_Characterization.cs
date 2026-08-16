#nullable enable

namespace Test_Project_1;

#region Usings
using DSPLib;
using System;
using System.Numerics;
using Test_Project_1.TestHelpers;
using DspLib = DSPLib.DSP;
#endregion

/// <summary>
/// CHARACTERIZATION / GUARD SUITE for <see cref="FFT"/> (DSP_Lib\DSP_Lib_FFT.cs).
///
/// Two kinds of test live here:
///
/// 1. MATHEMATICAL PROPERTY tests - round trip, linearity, Parseval, known analytic spectra.
///    These catch real breakage rather than mere drift, and use a numerical tolerance because
///    exactness is genuinely unattainable for an accumulated transform.
///
/// 2. GOLDEN VECTOR tests - a fixed deterministic input, with the CURRENT output embedded as
///    literal expected values and compared BIT-EXACTLY. An optimization to the butterfly loop,
///    the twiddle recurrence or the unswizzle step must not move a single bit.
///
/// Every input is analytic or comes from <see cref="DspCharacterization.Noise"/> (a self-contained
/// fixed-seed LCG). Nothing here reads the clock or System.Random.
/// </summary>
[TestClass]
public class Test_DSP_Lib_FFT_Characterization
{
    #region Constants
    /// <summary>
    /// Round-trip / linearity tolerance. A radix-2 FFT of these sizes accumulates roughly
    /// N*eps of rounding, so ~1e-12 is several orders of magnitude tighter than any real error
    /// while still being unconditionally satisfiable.
    /// </summary>
    private const double RoundTripTolerance = 1e-12;

    /// <summary>
    /// Parseval tolerance. Energy sums are relative quantities, so this is applied to a
    /// relative error rather than an absolute one.
    /// </summary>
    private const double RelativeEnergyTolerance = 1e-12;
    #endregion

    #region Golden Inputs
    private static readonly double[] Golden_Input_Noise8 =
    {
        -0.7808427880290107d, -0.4692294081645243d, 0.7712479853369596d, 0.6714748193595603d,
        -0.3487378765623792d, 0.12094446112685309d, 0.587736870323122d, -0.2170122498673437d
    };

    private static readonly double[] Golden_Input_Noise4 =
    {
        -0.7127808349157014d, 0.7826902606790416d, -0.9112984951912337d, -0.7178392852704045d
    };

    private static readonly double[] Golden_Input_Noise10 =
    {
        0.14079500641057652d, 0.20386058808715402d, -0.6713241214853045d, 0.0971653285386227d,
        -0.33530226102546257d, 0.12631329460586715d, -0.42638783761548527d, 0.6000353546797705d,
        0.18137983127174584d, 0.6939375286727543d
    };

    private static readonly double[] Golden_Window_Hann8 =
    {
        0.0d, 0.1464466094067262d, 0.49999999999999994d, 0.8535533905932737d,
        1.0d, 0.8535533905932738d, 0.5000000000000001d, 0.14644660940672632d
    };
    #endregion

    #region Property Tests - Round Trip

    /// <summary>
    /// The unscaled forward transform followed by the unscaled inverse reconstructs the original
    /// samples. Covers every power-of-two size the app plausibly uses, plus the degenerate 1 and 2.
    /// </summary>
    [TestMethod]
    public void Property_ForwardInverse_Unscaled_RoundTripsAcrossPowerOfTwoSizes()
    {
        int[] Local_Sizes = { 1, 2, 4, 8, 16, 32, 64, 128 };
        foreach (int Local_N in Local_Sizes)
        {
            var Local_Input = DspCharacterization.Noise(Local_N, (ulong)(20000 + Local_N));
            var Local_Spectrum = new FFT(Local_N, 0).Perform_FFT(DspCharacterization.Copy(Local_Input), false);
            var Local_Reconstructed = new FFT(Local_N, 0).Perform_IFFT(Local_Spectrum, false);

            DspCharacterization.AssertClose(Local_Input, Local_Reconstructed, RoundTripTolerance,
                "IFFT(FFT(x)) must reconstruct x for N=" + Local_N);
        }
    }

    /// <summary>
    /// Non-power-of-two lengths, including odd ones, route through Perform_FFT_NonPower2 which
    /// zero-pads up to the next power of two. The first input.Length samples of the inverse must
    /// still be the original signal.
    /// </summary>
    [TestMethod]
    public void Property_ForwardInverse_NonPower2_RoundTripsIncludingOddLengths()
    {
        int[] Local_Sizes = { 3, 5, 6, 7, 9, 10, 13, 17 };
        foreach (int Local_N in Local_Sizes)
        {
            var Local_Input = DspCharacterization.Noise(Local_N, (ulong)(21000 + Local_N));
            var Local_Spectrum = new FFT().Perform_FFT_NonPower2(DspCharacterization.Copy(Local_Input), false, false);
            var Local_Reconstructed = new FFT().Perform_IFFT_NonPower2(Local_Spectrum, false, true);

            Assert.IsTrue(Local_Reconstructed.Length >= Local_N,
                "Reconstruction must be at least as long as the original for N=" + Local_N);
            for (int i = 0; i < Local_N; i++)
            {
                Assert.AreEqual(Local_Input[i], Local_Reconstructed[i], RoundTripTolerance,
                    "Non-power-of-2 round trip failed at N=" + Local_N + " index " + i);
            }
        }
    }

    #endregion

    #region Property Tests - Known Analytic Cases

    /// <summary>
    /// A unit impulse at n=0 has a perfectly flat unscaled spectrum: every bin is exactly 1+0i.
    /// </summary>
    [TestMethod]
    public void Property_Impulse_ProducesFlatUnscaledSpectrum()
    {
        int Local_N = 32;
        var Local_Result = new FFT(Local_N, 0).Perform_FFT(DspCharacterization.Impulse(Local_N, 0, 1.0), false);

        Assert.AreEqual(Local_N, Local_Result.Length);
        for (int i = 0; i < Local_N; i++)
        {
            Assert.AreEqual(1.0, Local_Result[i].Real, RoundTripTolerance, "Bin " + i + " real");
            Assert.AreEqual(0.0, Local_Result[i].Imaginary, RoundTripTolerance, "Bin " + i + " imaginary");
        }
    }

    /// <summary>
    /// A pure DC signal puts all of its energy in bin 0. Unscaled, bin 0 = N * dc.
    /// </summary>
    [TestMethod]
    public void Property_DC_PutsAllEnergyInBinZero()
    {
        int Local_N = 32;
        double Local_DC = 0.375;
        var Local_Result = new FFT(Local_N, 0).Perform_FFT(DspCharacterization.Constant(Local_N, Local_DC), false);

        Assert.AreEqual(Local_N * Local_DC, Local_Result[0].Real, RoundTripTolerance);
        Assert.AreEqual(0.0, Local_Result[0].Imaginary, RoundTripTolerance);
        for (int i = 1; i < Local_N; i++)
            Assert.AreEqual(0.0, Local_Result[i].Magnitude, RoundTripTolerance, "Bin " + i + " must be empty for pure DC");
    }

    /// <summary>
    /// A sine exactly on a bin centre puts its energy in that bin and its conjugate mirror only.
    /// For sin(), the energy is purely imaginary: -N/2 at k, +N/2 at N-k.
    /// </summary>
    [TestMethod]
    public void Property_SineOnBinCentre_EnergyOnlyInThatBinAndItsConjugate()
    {
        int Local_N = 32;
        int Local_Bin = 5;
        var Local_Result = new FFT(Local_N, 0).Perform_FFT(DspCharacterization.Sine(Local_N, Local_Bin, 1.0), false);

        Assert.AreEqual(0.0, Local_Result[Local_Bin].Real, RoundTripTolerance);
        Assert.AreEqual(-Local_N / 2.0, Local_Result[Local_Bin].Imaginary, RoundTripTolerance);
        Assert.AreEqual(0.0, Local_Result[Local_N - Local_Bin].Real, RoundTripTolerance);
        Assert.AreEqual(Local_N / 2.0, Local_Result[Local_N - Local_Bin].Imaginary, RoundTripTolerance);

        for (int i = 0; i < Local_N; i++)
        {
            if (i == Local_Bin || i == Local_N - Local_Bin)
                continue;
            Assert.AreEqual(0.0, Local_Result[i].Magnitude, RoundTripTolerance, "Bin " + i + " must be empty");
        }
    }

    /// <summary>
    /// A cosine exactly on a bin centre is the real-valued counterpart: +N/2 real at k and N-k.
    /// </summary>
    [TestMethod]
    public void Property_CosineOnBinCentre_EnergyOnlyInThatBinAndItsConjugate()
    {
        int Local_N = 32;
        int Local_Bin = 7;
        var Local_Result = new FFT(Local_N, 0).Perform_FFT(DspCharacterization.Cosine(Local_N, Local_Bin, 1.0), false);

        Assert.AreEqual(Local_N / 2.0, Local_Result[Local_Bin].Real, RoundTripTolerance);
        Assert.AreEqual(0.0, Local_Result[Local_Bin].Imaginary, RoundTripTolerance);
        Assert.AreEqual(Local_N / 2.0, Local_Result[Local_N - Local_Bin].Real, RoundTripTolerance);

        for (int i = 0; i < Local_N; i++)
        {
            if (i == Local_Bin || i == Local_N - Local_Bin)
                continue;
            Assert.AreEqual(0.0, Local_Result[i].Magnitude, RoundTripTolerance, "Bin " + i + " must be empty");
        }
    }

    /// <summary>
    /// The transform is linear: FFT(a+b) == FFT(a) + FFT(b).
    /// </summary>
    [TestMethod]
    public void Property_Transform_IsLinear()
    {
        int Local_N = 64;
        var Local_A = DspCharacterization.Noise(Local_N, 22001UL);
        var Local_B = DspCharacterization.Sine(Local_N, 9, 0.4);
        var Local_Sum = new double[Local_N];
        for (int i = 0; i < Local_N; i++)
            Local_Sum[i] = Local_A[i] + Local_B[i];

        var Local_FftA = new FFT(Local_N, 0).Perform_FFT(DspCharacterization.Copy(Local_A), false);
        var Local_FftB = new FFT(Local_N, 0).Perform_FFT(DspCharacterization.Copy(Local_B), false);
        var Local_FftSum = new FFT(Local_N, 0).Perform_FFT(Local_Sum, false);

        for (int i = 0; i < Local_N; i++)
        {
            Assert.AreEqual(Local_FftA[i].Real + Local_FftB[i].Real, Local_FftSum[i].Real, RoundTripTolerance, "Bin " + i + " real");
            Assert.AreEqual(Local_FftA[i].Imaginary + Local_FftB[i].Imaginary, Local_FftSum[i].Imaginary, RoundTripTolerance, "Bin " + i + " imaginary");
        }
    }

    /// <summary>
    /// Parseval / energy conservation for the unscaled (plain DFT) transform:
    /// sum_k |X[k]|^2 == N * sum_n x[n]^2.
    /// </summary>
    [TestMethod]
    public void Property_Parseval_EnergyIsConserved()
    {
        int[] Local_Sizes = { 8, 16, 64, 256 };
        foreach (int Local_N in Local_Sizes)
        {
            var Local_Input = DspCharacterization.Noise(Local_N, (ulong)(23000 + Local_N));
            var Local_Spectrum = new FFT(Local_N, 0).Perform_FFT(DspCharacterization.Copy(Local_Input), false);

            double Local_TimeEnergy = 0;
            for (int i = 0; i < Local_N; i++)
                Local_TimeEnergy += Local_Input[i] * Local_Input[i];

            double Local_FreqEnergy = 0;
            for (int i = 0; i < Local_N; i++)
                Local_FreqEnergy += Local_Spectrum[i].Real * Local_Spectrum[i].Real
                                  + Local_Spectrum[i].Imaginary * Local_Spectrum[i].Imaginary;

            double Local_Expected = Local_N * Local_TimeEnergy;
            Assert.AreEqual(1.0, Local_FreqEnergy / Local_Expected, RelativeEnergyTolerance,
                "Parseval violated for N=" + Local_N);
        }
    }

    /// <summary>
    /// The windowed overload equals the plain overload applied to the pre-windowed samples.
    /// </summary>
    [TestMethod]
    public void Property_WindowedOverload_EqualsPreMultipliedInput()
    {
        int Local_N = 32;
        var Local_Input = DspCharacterization.Noise(Local_N, 24000UL);
        var Local_Window = DspLib.Window.Coefficients(DspLib.Window.Type.Hamming, Local_N);

        var Local_PreMultiplied = new double[Local_N];
        for (int i = 0; i < Local_N; i++)
            Local_PreMultiplied[i] = Local_Input[i] * Local_Window[i];

        var Local_Expected = new FFT(Local_N, 0).Perform_FFT(Local_PreMultiplied, false);
        var Local_Actual = new FFT(Local_N, 0).Perform_FFT(DspCharacterization.Copy(Local_Input), Local_Window, false);

        DspCharacterization.AssertExact(Local_Expected, Local_Actual,
            "The windowed overload must be exactly equivalent to pre-multiplying the input");
    }

    #endregion

    #region Property Tests - Scaling Flags

    /// <summary>
    /// Pins exactly what shouldScale:true does. Every bin is multiplied by
    /// FFTScale = sqrt(2)/LengthTotal * LengthTotal/N, EXCEPT the two endpoint bins of the
    /// one-sided spectrum - DC (0) and Nyquist (LengthTotal/2) - which are additionally divided by
    /// sqrt(2) and have their imaginary part forced to zero.
    ///
    /// NOTE - the endpoint index is LengthTotal/2, NOT LengthHalf. LengthHalf (= LengthTotal/2 + 1)
    /// is the BIN COUNT of the DC..Fs/2 half spectrum, not the index of its last bin. See
    /// <see cref="ScaledFFT_AppliesNyquistCorrectionToTheNyquistBin"/>.
    /// </summary>
    [TestMethod]
    public void Property_ShouldScaleTrue_IsShouldScaleFalseTimesFFTScale()
    {
        int Local_N = 16;
        var Local_Input = DspCharacterization.Noise(Local_N, 25000UL);
        var Local_Unscaled = new FFT(Local_N, 0).Perform_FFT(DspCharacterization.Copy(Local_Input), false);
        var Local_Scaled = new FFT(Local_N, 0).Perform_FFT(DspCharacterization.Copy(Local_Input), true);

        double Local_FftScale = Math.Sqrt(2) / Local_N;
        int Local_NyquistIndex = Local_N / 2;

        for (int i = 0; i < Local_N; i++)
        {
            if (i == 0 || i == Local_NyquistIndex)
            {
                Assert.AreEqual(Local_Unscaled[i].Real * Local_FftScale / Math.Sqrt(2), Local_Scaled[i].Real, 1e-15,
                    "Bin " + i + " gets the extra 1/sqrt(2)");
                DspCharacterization.AssertExact(0.0, Local_Scaled[i].Imaginary,
                    "Bin " + i + " has its imaginary part forced to zero by the scaling branch");
            }
            else
            {
                Assert.AreEqual(Local_Unscaled[i].Real * Local_FftScale, Local_Scaled[i].Real, 1e-15, "Bin " + i + " real");
                Assert.AreEqual(Local_Unscaled[i].Imaginary * Local_FftScale, Local_Scaled[i].Imaginary, 1e-15, "Bin " + i + " imaginary");
            }
        }
    }

    /// <summary>
    /// FIXED (was Bug_ScaledFFT_AppliesNyquistCorrectionToTheWrongBin).
    ///
    /// Perform_FFT's shouldScale branch applies the DC/Nyquist 1/sqrt(2) correction to bins 0 and
    /// LengthTotal/2. It used to use LengthHalf (= LengthTotal/2 + 1), which is the BIN COUNT of
    /// the DC..Fs/2 half spectrum rather than the index of its last bin, so the correction landed
    /// one bin too high:
    ///
    ///   * the true Nyquist bin (N/2) never got its correction (it read sqrt(2), ~+3 dB, high), and
    ///   * bin N/2+1 - an ordinary mirror bin with a genuine imaginary part - was both scaled by an
    ///     extra 1/sqrt(2) AND had its imaginary part destroyed.
    ///
    /// This test now asserts the CORRECT placement. It fails against the old code on all four
    /// assertions.
    /// </summary>
    [TestMethod]
    public void ScaledFFT_AppliesNyquistCorrectionToTheNyquistBin()
    {
        int Local_N = 8;
        var Local_Unscaled = new FFT(Local_N, 0).Perform_FFT(DspCharacterization.Copy(Golden_Input_Noise8), false);
        var Local_Scaled = new FFT(Local_N, 0).Perform_FFT(DspCharacterization.Copy(Golden_Input_Noise8), true);
        double Local_FftScale = Math.Sqrt(2) / Local_N;

        //Bin 4 IS Nyquist for N=8, so it - and only it, besides DC - gets the 1/sqrt(2) correction.
        Assert.AreEqual(Local_Unscaled[4].Real * Local_FftScale / Math.Sqrt(2), Local_Scaled[4].Real, 1e-16,
            "The true Nyquist bin must receive the 1/sqrt(2) correction");
        DspCharacterization.AssertExact(0.0, Local_Scaled[4].Imaginary,
            "The true Nyquist bin of a real-input FFT is purely real");

        //Bin 5 is an ordinary mirror bin: plain FFTScale, imaginary part preserved.
        Assert.AreEqual(Local_Unscaled[5].Real * Local_FftScale, Local_Scaled[5].Real, 1e-16,
            "Bin N/2+1 is an ordinary bin and must NOT receive the 1/sqrt(2) correction");
        Assert.AreNotEqual(0.0, Local_Unscaled[5].Imaginary,
            "Bin N/2+1 genuinely has an imaginary part before scaling");
        Assert.AreEqual(Local_Unscaled[5].Imaginary * Local_FftScale, Local_Scaled[5].Imaginary, 1e-16,
            "Bin N/2+1 must keep its genuine imaginary part, merely scaled");
    }

    /// <summary>
    /// FIXED (was Bug_ScaledFFT_ThrowsForLengthOneAndTwo). The old index LengthTotal/2 + 1 was
    /// past the end of the output array for LengthTotal 1 and 2, so the scaled forward transform
    /// threw IndexOutOfRangeException. It now returns the theoretically correct values.
    ///
    /// N=1: the single bin IS DC, and there is no distinct Nyquist bin. X[0] = 0.5, times
    /// FFTScale = sqrt(2)/1, divided by sqrt(2) for the DC endpoint = 0.5 - the mean of the block.
    ///
    /// N=2: X[0] = a+b = 0.25 and X[1] = a-b = 0.75. Both are endpoint bins (DC and Nyquist) so
    /// both get FFTScale = sqrt(2)/2 and then /sqrt(2), i.e. both are simply halved: 0.125 and
    /// 0.375. Reading that back, x[n] = 0.125 + 0.375*(-1)^n gives x = {0.5, -0.25} exactly, so
    /// the DC level is 0.125 and the Nyquist component has amplitude (and RMS) 0.375.
    /// </summary>
    [TestMethod]
    public void ScaledFFT_ProducesCorrectValuesForLengthOneAndTwo()
    {
        var Local_S1 = new FFT(1, 0).Perform_FFT(new double[] { 0.5 }, true);
        Assert.AreEqual(1, Local_S1.Length, "N=1 scaled must return one bin");
        DspCharacterization.AssertExact(new Complex[] { new(0.5d, 0.0d) }, Local_S1,
            "N=1 scaled: the single bin is DC and equals the block mean");

        var Local_S2 = new FFT(2, 0).Perform_FFT(new double[] { 0.5, -0.25 }, true);
        Assert.AreEqual(2, Local_S2.Length, "N=2 scaled must return two bins");
        Assert.AreEqual(0.125d, Local_S2[0].Real, 1e-16, "N=2 scaled DC bin == (a+b)/2");
        DspCharacterization.AssertExact(0.0, Local_S2[0].Imaginary, "N=2 scaled DC bin is purely real");
        Assert.AreEqual(0.375d, Local_S2[1].Real, 1e-16, "N=2 scaled Nyquist bin == (a-b)/2");
        DspCharacterization.AssertExact(0.0, Local_S2[1].Imaginary, "N=2 scaled Nyquist bin is purely real");

        //The unscaled path at the same sizes was always fine and is a plain DFT. Unchanged.
        var Local_R1 = new FFT(1, 0).Perform_FFT(new double[] { 0.5 }, false);
        DspCharacterization.AssertExact(new Complex[] { new(0.5d, 0.0d) }, Local_R1, "N=1 unscaled");

        var Local_R2 = new FFT(2, 0).Perform_FFT(new double[] { 0.5, -0.25 }, false);
        DspCharacterization.AssertExact(new Complex[] { new(0.25d, 0.0d), new(0.75d, 0.0d) }, Local_R2, "N=2 unscaled");
    }

    /// <summary>
    /// ANALYTIC PROOF that the corrected Nyquist bin now matches theory, with no reference to what
    /// the code prints.
    ///
    /// An alternating sequence x[n] = A * (-1)^n is EXACTLY the Nyquist basis function, so all of
    /// its energy sits in bin N/2 and nowhere else. Its unscaled DFT there is
    /// sum_n A(-1)^n e^(-j*2*pi*(N/2)*n/N) = sum_n A(-1)^n(-1)^n = N*A, purely real.
    /// FFTScale = sqrt(2)/N turns that into sqrt(2)*A, and the endpoint correction divides the
    /// sqrt(2) back out, leaving exactly A - which is also the RMS value of the alternating
    /// sequence, matching how the scaled transform reports every other bin.
    ///
    /// Against the OLD code this bin read sqrt(2)*A (about +3.01 dB too high) and bin N/2+1 was
    /// forced to zero, so this test fails on the old code at every size.
    /// </summary>
    [TestMethod]
    public void Analytic_ScaledNyquistBin_EqualsTheAlternatingSequenceAmplitude()
    {
        double Local_Amplitude = 0.75;
        foreach (int Local_N in new[] { 2, 4, 8, 16, 32, 64, 128 })
        {
            var Local_Input = new double[Local_N];
            for (int i = 0; i < Local_N; i++)
                Local_Input[i] = (i % 2 == 0) ? Local_Amplitude : -Local_Amplitude;

            var Local_Scaled = new FFT(Local_N, 0).Perform_FFT(Local_Input, true);
            int Local_NyquistIndex = Local_N / 2;

            Assert.AreEqual(Local_Amplitude, Local_Scaled[Local_NyquistIndex].Real, 1e-14,
                "Nyquist bin must read the alternating amplitude exactly for N=" + Local_N);
            Assert.AreEqual(0.0, Local_Scaled[Local_NyquistIndex].Imaginary, 1e-14,
                "Nyquist bin is purely real for N=" + Local_N);

            for (int i = 0; i < Local_N; i++)
            {
                if (i == Local_NyquistIndex)
                    continue;
                Assert.AreEqual(0.0, Local_Scaled[i].Magnitude, 1e-14,
                    "Bin " + i + " must be empty for a pure Nyquist tone at N=" + Local_N);
            }
        }
    }

    /// <summary>
    /// The formerly-corrupted bin, LengthTotal/2 + 1, is an ordinary mirror bin. It must carry the
    /// full FFTScale (not an extra 1/sqrt(2)) and must retain a NON-ZERO imaginary part. Its
    /// magnitude must also match its conjugate partner at LengthTotal/2 - 1, which is the defining
    /// property of a real-input spectrum and which the old code broke.
    ///
    /// Against the old code the imaginary part was exactly 0 and the magnitude was 1/sqrt(2) of
    /// its partner's, so both assertions fail.
    /// </summary>
    [TestMethod]
    public void ScaledFFT_BinAboveNyquist_KeepsMagnitudeAndImaginaryPart()
    {
        foreach (int Local_N in new[] { 8, 16, 64 })
        {
            var Local_Input = DspCharacterization.Noise(Local_N, (ulong)(26000 + Local_N));
            var Local_Unscaled = new FFT(Local_N, 0).Perform_FFT(DspCharacterization.Copy(Local_Input), false);
            var Local_Scaled = new FFT(Local_N, 0).Perform_FFT(DspCharacterization.Copy(Local_Input), true);

            int Local_Above = Local_N / 2 + 1;
            int Local_Below = Local_N / 2 - 1;
            double Local_FftScale = Math.Sqrt(2) / Local_N;

            Assert.AreNotEqual(0.0, Local_Scaled[Local_Above].Imaginary,
                "Bin N/2+1 must keep a non-zero imaginary part at N=" + Local_N);
            Assert.AreEqual(Local_Unscaled[Local_Above].Imaginary * Local_FftScale,
                Local_Scaled[Local_Above].Imaginary, 1e-15,
                "Bin N/2+1 imaginary part must be exactly FFTScale times unscaled at N=" + Local_N);
            Assert.AreEqual(Local_Unscaled[Local_Above].Magnitude * Local_FftScale,
                Local_Scaled[Local_Above].Magnitude, 1e-15,
                "Bin N/2+1 must keep its true magnitude at N=" + Local_N);
            Assert.AreEqual(Local_Scaled[Local_Below].Magnitude, Local_Scaled[Local_Above].Magnitude, 1e-15,
                "A real-input spectrum is conjugate symmetric about Nyquist at N=" + Local_N);
        }
    }

    /// <summary>
    /// Parseval on the SCALED path. With the correction in the right place the one-sided scaled
    /// spectrum is an RMS-per-bin reading: an ordinary bin already folds in its mirror partner via
    /// the sqrt(2) in FFTScale, and the two endpoint bins (DC and Nyquist), which have no mirror
    /// partner, have that sqrt(2) divided back out. So sum_{k=0..N/2} |scaled[k]|^2 is exactly the
    /// mean square of the time series - i.e. (1/N^2) * sum over the FULL two-sided spectrum.
    ///
    /// Against the old code this fails: the Nyquist bin comes out sqrt(2) too large, so its
    /// squared contribution is DOUBLE what it should be and the total over-counts.
    /// </summary>
    [TestMethod]
    public void Analytic_ScaledPath_ConservesEnergy()
    {
        foreach (int Local_N in new[] { 8, 16, 64, 256 })
        {
            var Local_Input = DspCharacterization.Noise(Local_N, (ulong)(27000 + Local_N));
            var Local_Scaled = new FFT(Local_N, 0).Perform_FFT(DspCharacterization.Copy(Local_Input), true);

            double Local_MeanSquare = 0;
            for (int i = 0; i < Local_N; i++)
                Local_MeanSquare += Local_Input[i] * Local_Input[i];
            Local_MeanSquare /= Local_N;

            int Local_NyquistIndex = Local_N / 2;
            double Local_SpectralEnergy = 0;
            for (int i = 0; i <= Local_NyquistIndex; i++)
                Local_SpectralEnergy += Local_Scaled[i].Magnitude * Local_Scaled[i].Magnitude;

            Assert.AreEqual(1.0, Local_SpectralEnergy / Local_MeanSquare, RelativeEnergyTolerance,
                "Scaled-path Parseval violated for N=" + Local_N);
        }
    }

    /// <summary>
    /// shouldUnpad on the NonPower2 entry points truncates the result back to the caller's length;
    /// with shouldUnpad:false the zero-padded length is returned. The overlapping prefix is
    /// bit-identical either way.
    /// </summary>
    [TestMethod]
    public void Property_ShouldUnpad_OnlyTruncatesAndDoesNotChangeValues()
    {
        var Local_Padded = new FFT().Perform_FFT_NonPower2(DspCharacterization.Copy(Golden_Input_Noise10), false, false);
        var Local_Unpadded = new FFT().Perform_FFT_NonPower2(DspCharacterization.Copy(Golden_Input_Noise10), false, true);

        Assert.AreEqual(16, Local_Padded.Length, "10 samples pad up to the next power of two");
        Assert.AreEqual(10, Local_Unpadded.Length, "shouldUnpad truncates back to the caller's length");
        for (int i = 0; i < Local_Unpadded.Length; i++)
        {
            DspCharacterization.AssertExact(Local_Padded[i].Real, Local_Unpadded[i].Real, "Bin " + i + " real");
            DspCharacterization.AssertExact(Local_Padded[i].Imaginary, Local_Unpadded[i].Imaginary, "Bin " + i + " imaginary");
        }
    }

    /// <summary>
    /// A power-of-two length short-circuits the NonPower2 wrapper: shouldUnpad is irrelevant and
    /// the result is bit-identical to calling Perform_FFT directly.
    /// </summary>
    [TestMethod]
    public void Property_NonPower2_WithPowerOfTwoInput_MatchesDirectCallExactly()
    {
        var Local_Direct = new FFT(8, 0).Perform_FFT(DspCharacterization.Copy(Golden_Input_Noise8), false);
        var Local_ViaWrapper = new FFT().Perform_FFT_NonPower2(DspCharacterization.Copy(Golden_Input_Noise8), false, false);
        DspCharacterization.AssertExact(Local_Direct, Local_ViaWrapper, "Power-of-two input must bypass padding entirely");
    }

    #endregion

    #region Property Tests - FrequencySpan

    /// <summary>
    /// FrequencySpan returns LengthTotal/2 + 1 points from DC to Fs/2, evenly spaced at Fs/LengthTotal.
    /// </summary>
    [TestMethod]
    public void Property_FrequencySpan_BinSpacingIsSampleRateOverLengthTotal()
    {
        (int Size, double Rate)[] Local_Cases =
        {
            (16, 48000d), (8, 44100d), (32, 96000d), (64, 192000d), (1024, 48000d)
        };

        foreach (var Local_Case in Local_Cases)
        {
            var Local_Span = new FFT(Local_Case.Size, 0).FrequencySpan(Local_Case.Rate);
            Assert.AreEqual(Local_Case.Size / 2 + 1, Local_Span.Length,
                "Point count for size " + Local_Case.Size);

            double Local_ExpectedSpacing = Local_Case.Rate / Local_Case.Size;
            Assert.AreEqual(0.0, Local_Span[0], 1e-9, "DC bin");
            Assert.AreEqual(Local_Case.Rate / 2.0, Local_Span[^1], 1e-6, "Nyquist bin");
            for (int i = 1; i < Local_Span.Length; i++)
            {
                Assert.AreEqual(Local_ExpectedSpacing, Local_Span[i] - Local_Span[i - 1], 1e-6,
                    "Spacing between bin " + (i - 1) + " and " + i + " at size " + Local_Case.Size);
            }
        }
    }

    /// <summary>
    /// Zero padding widens LengthTotal, so the span reflects the PADDED length, not the data length.
    /// </summary>
    [TestMethod]
    public void Property_FrequencySpan_AccountsForZeroPadding()
    {
        var Local_Span = new FFT(8, 8).FrequencySpan(48000);
        Assert.AreEqual(9, Local_Span.Length, "8 data + 8 pad = 16 total -> 9 points");
        Assert.AreEqual(3000.0, Local_Span[1] - Local_Span[0], 1e-9);
    }

    /// <summary>
    /// An FFT built with the parameterless constructor has LengthHalf = 0, so FrequencySpan
    /// returns an empty array rather than throwing.
    /// </summary>
    [TestMethod]
    public void Property_FrequencySpan_UninitializedReturnsEmpty()
    {
        var Local_Span = new FFT().FrequencySpan(48000);
        Assert.AreEqual(0, Local_Span.Length);
    }

    #endregion

    #region Golden Vectors - Perform_FFT(double[], bool)

    /// <summary>
    /// REGENERATED by the Nyquist off-by-one fix, bins 4 and 5 only.
    /// Bin 4 is the true Nyquist bin of an N=8 real-input FFT and now carries the 1/sqrt(2)
    /// endpoint correction: 0.021783585572353055 / sqrt(2) == 0.015403321076768284.
    /// Bin 5 is an ordinary mirror bin and is now simply unscaled[5] * FFTScale, i.e.
    /// (0.61346626523531, 0.027428171578031763) * sqrt(2)/8 - so it regains both its true
    /// magnitude and the imaginary part the old code destroyed. Every other bin is untouched.
    /// </summary>
    [TestMethod]
    public void Golden_PerformFFT_Noise8_Scaled()
    {
        Complex[] Local_Expected =
        {
            new(0.04194772669040464d, 0.0d), new(-0.2612186955853022d, -0.0697296384542881d),
            new(-0.4399203886992111d, 0.14190705312973598d), new(0.10844653904426822d, -0.0048486615295935875d),
            new(0.015403321076768284d, 0.0d), new(0.10844653904426822d, 0.004848661529593597d),
            new(-0.4399203886992111d, -0.14190705312973598d), new(-0.2612186955853022d, 0.0697296384542881d)
        };
        var Local_Actual = new FFT(8, 0).Perform_FFT(DspCharacterization.Copy(Golden_Input_Noise8), true);
        DspCharacterization.AssertExact(Local_Expected, Local_Actual, "Perform_FFT(noise8, shouldScale:true)");
    }

    [TestMethod]
    public void Golden_PerformFFT_Noise8_Unscaled()
    {
        Complex[] Local_Expected =
        {
            new(0.3355818135232371d, 0.0d), new(-1.477676088168573d, -0.3944504016057069d),
            new(-2.4885655202514716d, 0.8027475165298879d), new(0.61346626523531d, -0.027428171578031707d),
            new(0.12322656861414627d, 0.0d), new(0.61346626523531d, 0.027428171578031763d),
            new(-2.4885655202514716d, -0.8027475165298879d), new(-1.477676088168573d, 0.3944504016057069d)
        };
        var Local_Actual = new FFT(8, 0).Perform_FFT(DspCharacterization.Copy(Golden_Input_Noise8), false);
        DspCharacterization.AssertExact(Local_Expected, Local_Actual, "Perform_FFT(noise8, shouldScale:false)");
    }

    /// <summary>
    /// REGENERATED by the Nyquist off-by-one fix, SCALED vector only, bins 2 and 3.
    /// Bin 2 is Nyquist for N=4: -0.5971270359907643 / sqrt(2) == -0.42223257637889305.
    /// Bin 3 is an ordinary mirror bin: unscaled[3] * FFTScale =
    /// (0.19851766027553222, 1.500529545949446) * sqrt(2)/4. The UNSCALED vector is unchanged.
    /// </summary>
    [TestMethod]
    public void Golden_PerformFFT_Noise4_BothScalings()
    {
        Complex[] Local_Scaled =
        {
            new(-0.3898070886745745d, 0.0d), new(0.07018659188305813d, -0.5305173086558123d),
            new(-0.42223257637889305d, 0.0d), new(0.07018659188305808d, 0.5305173086558123d)
        };
        Complex[] Local_Unscaled =
        {
            new(-1.5592283546982981d, 0.0d), new(0.1985176602755324d, -1.500529545949446d),
            new(-1.6889303055155722d, 0.0d), new(0.19851766027553222d, 1.500529545949446d)
        };
        DspCharacterization.AssertExact(Local_Scaled,
            new FFT(4, 0).Perform_FFT(DspCharacterization.Copy(Golden_Input_Noise4), true), "N=4 scaled");
        DspCharacterization.AssertExact(Local_Unscaled,
            new FFT(4, 0).Perform_FFT(DspCharacterization.Copy(Golden_Input_Noise4), false), "N=4 unscaled");
    }

    [TestMethod]
    public void Golden_PerformFFT_Impulse16_BothScalings()
    {
        //REGENERATED by the Nyquist off-by-one fix, SCALED vector only: the 0.0625 endpoint value
        //moved from bin 9 (an ordinary mirror bin) to bin 8 (the true Nyquist bin for N=16).
        //An impulse at n=0 has a perfectly flat unscaled spectrum of 1+0i, so every scaled bin is
        //exactly FFTScale = sqrt(2)/16 = 0.08838834764831845, and the two endpoint bins - DC and
        //Nyquist - are that divided by sqrt(2) = 1/16 = 0.0625 exactly. The UNSCALED vector is
        //unchanged and still perfectly flat.
        Complex[] Local_Scaled =
        {
            new(0.0625d, 0.0d), new(0.08838834764831845d, 0.0d), new(0.08838834764831845d, 0.0d),
            new(0.08838834764831845d, 0.0d), new(0.08838834764831845d, 0.0d), new(0.08838834764831845d, 0.0d),
            new(0.08838834764831845d, 0.0d), new(0.08838834764831845d, 0.0d), new(0.0625d, 0.0d),
            new(0.08838834764831845d, 0.0d), new(0.08838834764831845d, 0.0d), new(0.08838834764831845d, 0.0d),
            new(0.08838834764831845d, 0.0d), new(0.08838834764831845d, 0.0d), new(0.08838834764831845d, 0.0d),
            new(0.08838834764831845d, 0.0d)
        };
        DspCharacterization.AssertExact(Local_Scaled,
            new FFT(16, 0).Perform_FFT(DspCharacterization.Impulse(16, 0, 1.0), true), "impulse16 scaled");

        var Local_Unscaled = new Complex[16];
        for (int i = 0; i < 16; i++)
            Local_Unscaled[i] = new Complex(1.0d, 0.0d);
        DspCharacterization.AssertExact(Local_Unscaled,
            new FFT(16, 0).Perform_FFT(DspCharacterization.Impulse(16, 0, 1.0), false), "impulse16 unscaled");
    }

    [TestMethod]
    public void Golden_PerformFFT_DC16_Scaled()
    {
        var Local_Expected = new Complex[16];
        Local_Expected[0] = new Complex(2.5d, 0.0d);
        DspCharacterization.AssertExact(Local_Expected,
            new FFT(16, 0).Perform_FFT(DspCharacterization.Constant(16, 2.5), true),
            "A scaled DC block reads back as its own amplitude in bin 0");
    }

    /// <summary>
    /// REGENERATED by the Nyquist off-by-one fix, SCALED vector only, bins 8 and 9.
    /// LengthTotal is 16 here (8 data + 8 pad), so Nyquist is bin 8:
    /// 0.021783585572353055 / sqrt(2) == 0.015403321076768284. Bin 9 is an ordinary mirror bin and
    /// is now unscaled[9] * FFTScale = (-0.6287414109903723, -0.14272288912351355) * sqrt(2)/8,
    /// which restores its imaginary part. The UNSCALED vector is unchanged.
    /// </summary>
    [TestMethod]
    public void Golden_PerformFFT_ZeroPadded8plus8_BothScalings()
    {
        Complex[] Local_Scaled =
        {
            new(0.04194772669040464d, 0.0d), new(-0.1190450076434798d, -0.19121867454564961d),
            new(-0.2612186955853022d, -0.0697296384542881d), new(-0.2679488869366318d, -0.06583781634851159d),
            new(-0.4399203886992111d, 0.14190705312973598d), new(-0.053998507044789326d, 0.39720585625343663d),
            new(0.10844653904426822d, -0.0048486615295935875d), new(-0.11114682883102259d, 0.025230080682443135d),
            new(0.015403321076768284d, 0.0d), new(-0.11114682883102259d, -0.02523008068244305d),
            new(0.10844653904426822d, 0.004848661529593597d), new(-0.053998507044789326d, -0.3972058562534367d),
            new(-0.4399203886992111d, -0.14190705312973598d), new(-0.26794888693663177d, 0.06583781634851155d),
            new(-0.2612186955853022d, 0.0697296384542881d), new(-0.11904500764347972d, 0.1912186745456497d)
        };
        DspCharacterization.AssertExact(Local_Scaled,
            new FFT(8, 8).Perform_FFT(DspCharacterization.Copy(Golden_Input_Noise8), true), "FFT(8,8) scaled");

        Complex[] Local_Unscaled =
        {
            new(0.3355818135232371d, 0.0d), new(-0.6734202573688716d, -1.0816961716858584d),
            new(-1.477676088168573d, -0.3944504016057069d), new(-1.515747799714239d, -0.37243493118837667d),
            new(-2.4885655202514716d, 0.8027475165298879d), new(-0.3054616840425607d, 2.2469356358705124d),
            new(0.61346626523531d, -0.027428171578031707d), new(-0.6287414109903723d, 0.14272288912351405d),
            new(0.12322656861414627d, 0.0d), new(-0.6287414109903723d, -0.14272288912351355d),
            new(0.61346626523531d, 0.027428171578031763d), new(-0.3054616840425607d, -2.246935635870513d),
            new(-2.4885655202514716d, -0.8027475165298879d), new(-1.5157477997142386d, 0.37243493118837645d),
            new(-1.477676088168573d, 0.3944504016057069d), new(-0.6734202573688711d, 1.0816961716858589d)
        };
        DspCharacterization.AssertExact(Local_Unscaled,
            new FFT(8, 8).Perform_FFT(DspCharacterization.Copy(Golden_Input_Noise8), false), "FFT(8,8) unscaled");
    }

    #endregion

    #region Golden Vectors - Perform_FFT(double[], double[], bool)

    /// <summary>
    /// REGENERATED by the Nyquist off-by-one fix, SCALED vector only, bins 4 and 5.
    /// Bin 4 is Nyquist for N=8: -0.043331476735957566 / sqrt(2) == -0.03063998103882272.
    /// Bin 5 is an ordinary mirror bin: unscaled[5] * FFTScale =
    /// (0.8980678705269863, 0.21440096492148797) * sqrt(2)/8. The UNSCALED vector and the Hann
    /// coefficients are unchanged, confirming the fix touched only the scaling branch.
    /// </summary>
    [TestMethod]
    public void Golden_PerformFFT_WindowedOverload_Hann8()
    {
        DspCharacterization.AssertExact(Golden_Window_Hann8,
            DspLib.Window.Coefficients(DspLib.Window.Type.Hann, 8),
            "The Hann coefficients feeding this golden vector must not drift either");

        Complex[] Local_Scaled =
        {
            new(0.11332861885573813d, 0.0d), new(-0.03546001161692086d, -0.07034158250957802d),
            new(-0.18176715521434703d, 0.08959810156083839d), new(0.15875747030384862d, -0.037901094047230806d),
            new(-0.03063998103882272d, 0.0d), new(0.15875747030384862d, 0.03790109404723081d),
            new(-0.18176715521434703d, -0.08959810156083839d), new(-0.03546001161692086d, 0.07034158250957802d)
        };
        DspCharacterization.AssertExact(Local_Scaled,
            new FFT(8, 0).Perform_FFT(DspCharacterization.Copy(Golden_Input_Noise8), Golden_Window_Hann8, true),
            "Windowed FFT, scaled");

        Complex[] Local_Unscaled =
        {
            new(0.906628950845905d, 0.0d), new(-0.20059211740222793d, -0.3979120799353253d),
            new(-1.02823030439242d, 0.5068434015608785d), new(0.8980678705269863d, -0.2144009649214879d),
            new(-0.24511984831058176d, 0.0d), new(0.8980678705269863d, 0.21440096492148797d),
            new(-1.02823030439242d, -0.5068434015608785d), new(-0.20059211740222793d, 0.39791207993532524d)
        };
        DspCharacterization.AssertExact(Local_Unscaled,
            new FFT(8, 0).Perform_FFT(DspCharacterization.Copy(Golden_Input_Noise8), Golden_Window_Hann8, false),
            "Windowed FFT, unscaled");
    }

    [TestMethod]
    public void Golden_PerformFFT_WindowedOverload_RejectsLengthMismatch()
    {
        Assert.ThrowsExactly<InvalidOperationException>(
            () => new FFT(8, 0).Perform_FFT(new double[8], new double[7], false));
    }

    #endregion

    #region Golden Vectors - Perform_IFFT

    [TestMethod]
    public void Golden_PerformIFFT_Fixed8_BothScalings()
    {
        var Local_Spectrum = new Complex[8];
        for (int i = 0; i < 8; i++)
            Local_Spectrum[i] = new Complex(0.125 * (i + 1), -0.0625 * (i - 3));

        double[] Local_Scaled =
        {
            3.97747564417433d, -0.9754126073623883d, -0.6629126073623883d, -0.5334708691207962d,
            -0.4419417382415922d, -0.3504126073623883d, -0.22097086912079605d, 0.09152913087920407d
        };
        var Local_C1 = new Complex[8]; Array.Copy(Local_Spectrum, Local_C1, 8);
        DspCharacterization.AssertExact(Local_Scaled, new FFT(8, 0).Perform_IFFT(Local_C1, true), "IFFT scaled");

        double[] Local_Unscaled =
        {
            0.5625d, -0.13794417382415922d, -0.09375d, -0.07544417382415923d,
            -0.0625d, -0.04955582617584078d, -0.031249999999999993d, 0.012944173824159244d
        };
        var Local_C2 = new Complex[8]; Array.Copy(Local_Spectrum, Local_C2, 8);
        DspCharacterization.AssertExact(Local_Unscaled, new FFT(8, 0).Perform_IFFT(Local_C2, false), "IFFT unscaled");
    }

    [TestMethod]
    public void Golden_PerformIFFT_RoundTripOfNoise8()
    {
        double[] Local_ExpectedUnscaled =
        {
            -0.7808427880290107d, -0.4692294081645244d, 0.7712479853369596d, 0.6714748193595605d,
            -0.3487378765623792d, 0.12094446112685309d, 0.587736870323122d, -0.21701224986734374d
        };
        var Local_Spectrum = new FFT(8, 0).Perform_FFT(DspCharacterization.Copy(Golden_Input_Noise8), false);
        DspCharacterization.AssertExact(Local_ExpectedUnscaled, new FFT(8, 0).Perform_IFFT(Local_Spectrum, false),
            "Unscaled round trip returns the input, to the last bit as it stands today");

        double[] Local_ExpectedScaled =
        {
            -5.521392304559234d, -3.317952964452856d, 5.453546804082271d, 4.748043981651573d,
            -2.465949173738555d, 0.8552064860975062d, 4.155927265588382d, -1.5345083348174822d
        };
        var Local_Spectrum2 = new FFT(8, 0).Perform_FFT(DspCharacterization.Copy(Golden_Input_Noise8), false);
        DspCharacterization.AssertExact(Local_ExpectedScaled, new FFT(8, 0).Perform_IFFT(Local_Spectrum2, true),
            "shouldScale:true on the inverse multiplies by FFTScale*LengthHalf, i.e. it does NOT undo the forward scale");
    }

    [TestMethod]
    public void Golden_PerformIFFT_RejectsOverlongInput()
    {
        Assert.ThrowsExactly<InvalidOperationException>(
            () => new FFT(8, 0).Perform_IFFT(new Complex[16], false));
    }

    #endregion

    #region Golden Vectors - NonPower2 entry points

    [TestMethod]
    public void Golden_PerformFFT_NonPower2_Noise10_Unscaled()
    {
        Complex[] Local_Expected =
        {
            new(0.6104727121402387d, 0.0d), new(-1.2320678135798024d, 0.8629552418952251d),
            new(1.5585820535752672d, 0.05499694008206135d), new(-0.2576274231298646d, 0.42483054740042947d),
            new(1.0845845357576496d, -0.3269107281473822d), new(0.5228499879734906d, -0.45696407068812295d),
            new(-0.2436278561596974d, -0.4348756276575773d), new(0.8045059492914991d, -1.3600484202951777d),
            new(-2.8321514770280984d, 0.0d), new(0.8045059492914993d, 1.3600484202951773d),
            new(-0.2436278561596974d, 0.43487562765757726d), new(0.5228499879734909d, 0.45696407068812306d),
            new(1.0845845357576496d, 0.3269107281473822d), new(-0.25762742312986453d, -0.42483054740042914d),
            new(1.5585820535752672d, -0.054996940082061296d), new(-1.2320678135798029d, -0.8629552418952251d)
        };
        DspCharacterization.AssertExact(Local_Expected,
            new FFT().Perform_FFT_NonPower2(DspCharacterization.Copy(Golden_Input_Noise10), false, false),
            "Perform_FFT_NonPower2(noise10, scale:false, unpad:false)");
    }

    /// <summary>
    /// REGENERATED by the Nyquist off-by-one fix, bins 8 and 9 only. The wrapper pads 10 samples
    /// to LengthTotal 16 and re-Inits with inputDataLength 16, so FFTScale is sqrt(2)/16 and
    /// Nyquist is bin 8: -0.25032918934425813 / sqrt(2) == -0.17700946731425615. Bin 9 is an
    /// ordinary mirror bin: unscaled[9] * FFTScale =
    /// (0.8045059492914993, 1.3600484202951773) * sqrt(2)/16. The companion UNSCALED NonPower2
    /// golden is unchanged.
    /// </summary>
    [TestMethod]
    public void Golden_PerformFFT_NonPower2_Noise10_Scaled()
    {
        Complex[] Local_Expected =
        {
            new(0.03815454450876492d, 0.0d), new(-0.10890043823299518d, 0.07627518792557389d),
            new(0.1377604923898408d, 0.004861088659566978d), new(-0.022771262239342907d, 0.03755007011525459d),
            new(0.0958646350005372d, -0.028895099089455742d), new(0.046213846504920005d, -0.040390299142772576d),
            new(-0.021533863647057856d, -0.03843793816117863d), new(0.07110895153111747d, -0.12021243259159649d),
            new(-0.17700946731425615d, 0.0d), new(0.07110895153111749d, 0.12021243259159645d),
            new(-0.021533863647057856d, 0.038437938161178624d), new(0.04621384650492003d, 0.04039029914277258d),
            new(0.0958646350005372d, 0.028895099089455742d), new(-0.022771262239342904d, -0.03755007011525456d),
            new(0.1377604923898408d, -0.0048610886595669735d), new(-0.10890043823299522d, -0.07627518792557389d)
        };
        DspCharacterization.AssertExact(Local_Expected,
            new FFT().Perform_FFT_NonPower2(DspCharacterization.Copy(Golden_Input_Noise10), true, false),
            "Perform_FFT_NonPower2(noise10, scale:true, unpad:false)");
    }

    [TestMethod]
    public void Golden_PerformIFFT_NonPower2_Fixed10()
    {
        var Local_Spectrum = new Complex[10];
        for (int i = 0; i < 10; i++)
            Local_Spectrum[i] = new Complex(0.1 * (i + 1), 0.05 * (5 - i));

        double[] Local_Unpadded =
        {
            0.34375d, -0.18534314450281292d, 0.05410533905932737d, -0.07526756671517496d, 0.03125d,
            -0.019239033790107322d, -0.00928300858899106d, 0.024088682573403575d, -0.03125d, 0.034898970678653676d
        };
        var Local_C1 = new Complex[10]; Array.Copy(Local_Spectrum, Local_C1, 10);
        DspCharacterization.AssertExact(Local_Unpadded,
            new FFT().Perform_IFFT_NonPower2(Local_C1, false, true), "IFFT NonPower2, unpadded");

        double[] Local_Padded =
        {
            0.34375d, -0.18534314450281292d, 0.05410533905932737d, -0.07526756671517496d, 0.03125d,
            -0.019239033790107322d, -0.00928300858899106d, 0.024088682573403575d, -0.03125d, 0.034898970678653676d,
            -0.01660533905932738d, 0.00803407100967049d, 0.031249999999999997d, -0.04281679238573345d,
            0.09678300858899107d, -0.1443551868678991d
        };
        var Local_C2 = new Complex[10]; Array.Copy(Local_Spectrum, Local_C2, 10);
        DspCharacterization.AssertExact(Local_Padded,
            new FFT().Perform_IFFT_NonPower2(Local_C2, false, false), "IFFT NonPower2, padded");

        double[] Local_Scaled =
        {
            4.3752232085917635d, -2.3590330978387897d, 0.6886485386504598d, -0.9579997228987996d,
            0.39774756441743303d, -0.24487292261631627d, -0.11815340981559377d, 0.30659887435506994d,
            -0.39774756441743303d, 0.4441913788195171d
        };
        var Local_C3 = new Complex[10]; Array.Copy(Local_Spectrum, Local_C3, 10);
        DspCharacterization.AssertExact(Local_Scaled,
            new FFT().Perform_IFFT_NonPower2(Local_C3, true, true), "IFFT NonPower2, scaled + unpadded");
    }

    #endregion

    #region Golden Vectors - FrequencySpan

    [TestMethod]
    public void Golden_FrequencySpan_KnownSampleRates()
    {
        DspCharacterization.AssertExact(
            new double[] { 0.0d, 3000.0d, 6000.0d, 9000.0d, 12000.0d, 15000.0d, 18000.0d, 21000.0d, 24000.0d },
            new FFT(16, 0).FrequencySpan(48000), "FFT(16) @ 48 kHz");

        DspCharacterization.AssertExact(
            new double[] { 0.0d, 5512.5d, 11025.0d, 16537.5d, 22050.0d },
            new FFT(8, 0).FrequencySpan(44100), "FFT(8) @ 44.1 kHz");

        DspCharacterization.AssertExact(
            new double[]
            {
                0.0d, 3000.0d, 6000.0d, 9000.0d, 12000.0d, 15000.0d, 18000.0d, 21000.0d, 24000.0d,
                27000.0d, 30000.0d, 33000.0d, 36000.0d, 39000.0d, 42000.0d, 45000.0d, 48000.0d
            },
            new FFT(32, 0).FrequencySpan(96000), "FFT(32) @ 96 kHz");

        DspCharacterization.AssertExact(
            new double[] { 0.0d, 3000.0d, 6000.0d, 9000.0d, 12000.0d, 15000.0d, 18000.0d, 21000.0d, 24000.0d },
            new FFT(8, 8).FrequencySpan(48000), "FFT(8 + 8 pad) @ 48 kHz");
    }

    #endregion

    #region Aliasing / Mutation Contract

    /// <summary>
    /// Perform_FFT does NOT mutate the caller's time-series array, and always returns a freshly
    /// allocated Complex[]. An optimization that started writing into the input in place would
    /// silently corrupt callers such as FIR/ULF_FIR, which reuse their overlap buffer.
    /// </summary>
    [TestMethod]
    public void Contract_PerformFFT_DoesNotMutateInputAndReturnsANewArray()
    {
        var Local_Input = DspCharacterization.Copy(Golden_Input_Noise8);
        var Local_Before = DspCharacterization.Copy(Local_Input);

        var Local_First = new FFT(8, 0).Perform_FFT(Local_Input, false);
        DspCharacterization.AssertExact(Local_Before, Local_Input, "Perform_FFT must leave the input untouched");

        var Local_Second = new FFT(8, 0).Perform_FFT(Local_Input, false);
        Assert.IsFalse(ReferenceEquals(Local_First, Local_Second), "Each call must return a distinct array");
    }

    /// <summary>
    /// Perform_IFFT likewise leaves its spectrum argument alone and returns a new double[].
    /// </summary>
    [TestMethod]
    public void Contract_PerformIFFT_DoesNotMutateInputAndReturnsANewArray()
    {
        var Local_Spectrum = new Complex[8];
        for (int i = 0; i < 8; i++)
            Local_Spectrum[i] = new Complex(0.125 * (i + 1), -0.0625 * (i - 3));
        var Local_Before = new Complex[8];
        Array.Copy(Local_Spectrum, Local_Before, 8);

        var Local_First = new FFT(8, 0).Perform_IFFT(Local_Spectrum, false);
        DspCharacterization.AssertExact(Local_Before, Local_Spectrum, "Perform_IFFT must leave the spectrum untouched");

        var Local_Second = new FFT(8, 0).Perform_IFFT(Local_Spectrum, false);
        Assert.IsFalse(ReferenceEquals(Local_First, Local_Second), "Each call must return a distinct array");
    }

    /// <summary>
    /// A single FFT instance is reusable across calls: repeated transforms of the same input give
    /// bit-identical results, and a zero-padded instance correctly clears its tail between calls.
    /// </summary>
    [TestMethod]
    public void Contract_FFTInstance_IsReusableAndClearsPaddingBetweenCalls()
    {
        var Local_Fft = new FFT(8, 8);
        var Local_First = Local_Fft.Perform_FFT(DspCharacterization.Copy(Golden_Input_Noise8), false);
        _ = Local_Fft.Perform_FFT(DspCharacterization.Constant(8, 1.0), false);
        var Local_Third = Local_Fft.Perform_FFT(DspCharacterization.Copy(Golden_Input_Noise8), false);

        DspCharacterization.AssertExact(Local_First, Local_Third,
            "Reusing an FFT instance must give bit-identical results for the same input");
    }

    /// <summary>
    /// A non-power-of-two total length is rejected at construction time.
    /// </summary>
    [TestMethod]
    public void Contract_Constructor_RejectsNonPowerOfTwoTotalLength()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new FFT(5, 0));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new FFT(6, 1));
        Assert.IsNotNull(new FFT(6, 2));
    }

    #endregion
}
