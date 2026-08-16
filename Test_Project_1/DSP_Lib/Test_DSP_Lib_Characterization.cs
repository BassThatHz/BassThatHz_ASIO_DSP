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
/// CHARACTERIZATION / GUARD SUITE for DSP_Lib\DSP_Lib.cs - the window functions, window scale
/// factors, signal generators, magnitude/complex converters, array math, analysis helpers and the
/// Largest-Triangle downsampler.
///
/// Window functions are pinned two ways: against their ANALYTIC formula where one exists (a real
/// correctness check), and as bit-exact golden vectors (a drift detector for the shared
/// SineExpansion kernel, which is the obvious target for vectorization).
/// </summary>
[TestClass]
public class Test_DSP_Lib_Characterization
{
    #region Constants
    /// <summary>
    /// Analytic-comparison tolerance. The library computes cos(k*angle) by multiplying the angle,
    /// whereas the reference formula here computes it directly, so the two differ by a few ULP.
    /// </summary>
    private const double AnalyticTolerance = 1e-12;
    #endregion

    #region Property Tests - Windows

    /// <summary>
    /// Rectangular and None are exactly all-ones, with no rounding anywhere.
    /// </summary>
    [TestMethod]
    public void Property_Window_RectangularAndNone_AreExactlyOne()
    {
        foreach (var Local_Type in new[] { DspLib.Window.Type.Rectangular, DspLib.Window.Type.None })
        {
            var Local_Coefficients = DspLib.Window.Coefficients(Local_Type, 16);
            Assert.AreEqual(16, Local_Coefficients.Length);
            for (int i = 0; i < Local_Coefficients.Length; i++)
                DspCharacterization.AssertExact(1.0d, Local_Coefficients[i], Local_Type + " coefficient " + i);
        }
    }

    /// <summary>
    /// Every cosine-sum window produced by SineExpansion is periodic (DFT-even): it peaks at N/2
    /// and satisfies w[k] == w[N-k] for k = 1..N-1.
    /// </summary>
    [TestMethod]
    public void Property_Window_CosineSumWindows_AreSymmetricAboutTheMidpoint()
    {
        DspLib.Window.Type[] Local_Types =
        {
            DspLib.Window.Type.Hann, DspLib.Window.Type.Hamming, DspLib.Window.Type.FlatTop,
            DspLib.Window.Type.BH92, DspLib.Window.Type.Nutall3, DspLib.Window.Type.Nutall4B,
            DspLib.Window.Type.SFT3F, DspLib.Window.Type.SFT5M, DspLib.Window.Type.HFT90D,
            DspLib.Window.Type.HFT248D
        };

        foreach (var Local_Type in Local_Types)
        {
            int Local_N = 32;
            var Local_Coefficients = DspLib.Window.Coefficients(Local_Type, Local_N);
            for (int k = 1; k < Local_N; k++)
            {
                Assert.AreEqual(Local_Coefficients[k], Local_Coefficients[Local_N - k], AnalyticTolerance,
                    Local_Type + " must be symmetric at k=" + k);
            }
        }
    }

    /// <summary>
    /// The first coefficient of a cosine-sum window is the plain sum of its coefficients (cos(0)=1),
    /// and the midpoint coefficient is the alternating sum. Checked against the documented
    /// Heinzel/Rudiger/Schilling coefficient sets.
    /// </summary>
    [TestMethod]
    public void Property_Window_EndpointAndPeakMatchTheCoefficientSums()
    {
        //(type, coefficients c0..cn)
        (DspLib.Window.Type Type, double[] C)[] Local_Cases =
        {
            (DspLib.Window.Type.Hann, new[] { 0.5, -0.5 }),
            (DspLib.Window.Type.Hamming, new[] { 0.54, -0.46 }),
            (DspLib.Window.Type.BH92, new[] { 0.35875, -0.48829, 0.14128, -0.01168 }),
            (DspLib.Window.Type.Nutall3, new[] { 0.375, -0.5, 0.125 }),
            (DspLib.Window.Type.FlatTop, new[] { 0.21557895, -0.41663158, 0.277263158, -0.083578947, 0.006947368 })
        };

        foreach (var Local_Case in Local_Cases)
        {
            int Local_N = 16;
            var Local_Coefficients = DspLib.Window.Coefficients(Local_Case.Type, Local_N);

            double Local_PlainSum = 0;
            double Local_AlternatingSum = 0;
            for (int i = 0; i < Local_Case.C.Length; i++)
            {
                Local_PlainSum += Local_Case.C[i];
                Local_AlternatingSum += (i % 2 == 0) ? Local_Case.C[i] : -Local_Case.C[i];
            }

            Assert.AreEqual(Local_PlainSum, Local_Coefficients[0], AnalyticTolerance,
                Local_Case.Type + " w[0] must equal the plain coefficient sum");
            Assert.AreEqual(Local_AlternatingSum, Local_Coefficients[Local_N / 2], AnalyticTolerance,
                Local_Case.Type + " w[N/2] must equal the alternating coefficient sum");
        }
    }

    /// <summary>
    /// Hann, Hamming, Bartlett and Welch against their closed-form definitions.
    /// </summary>
    [TestMethod]
    public void Property_Window_MatchTheirAnalyticFormulas()
    {
        int Local_N = 32;

        var Local_Hann = DspLib.Window.Coefficients(DspLib.Window.Type.Hann, Local_N);
        var Local_Hamming = DspLib.Window.Coefficients(DspLib.Window.Type.Hamming, Local_N);
        var Local_Bartlett = DspLib.Window.Coefficients(DspLib.Window.Type.Bartlett, Local_N);
        var Local_Welch = DspLib.Window.Coefficients(DspLib.Window.Type.Welch, Local_N);

        for (int i = 0; i < Local_N; i++)
        {
            double Local_Angle = 2.0 * Math.PI * i / Local_N;
            Assert.AreEqual(0.5 - 0.5 * Math.Cos(Local_Angle), Local_Hann[i], AnalyticTolerance, "Hann " + i);
            Assert.AreEqual(0.54 - 0.46 * Math.Cos(Local_Angle), Local_Hamming[i], AnalyticTolerance, "Hamming " + i);
            Assert.AreEqual(2.0 / Local_N * (Local_N / 2.0 - Math.Abs(i - (Local_N - 1.0) / 2.0)), Local_Bartlett[i], AnalyticTolerance, "Bartlett " + i);
            Assert.AreEqual(1.0 - Math.Pow(2.0 * i / Local_N - 1.0, 2.0), Local_Welch[i], AnalyticTolerance, "Welch " + i);
        }
    }

    /// <summary>
    /// Coherent gain (the mean coefficient) for the classic windows, and its reciprocal relationship
    /// with ScaleFactor.Signal.
    /// </summary>
    [TestMethod]
    public void Property_Window_CoherentGainAndSignalScaleFactorAreReciprocal()
    {
        (DspLib.Window.Type Type, double CoherentGain)[] Local_Cases =
        {
            (DspLib.Window.Type.Rectangular, 1.0),
            (DspLib.Window.Type.Hann, 0.5),
            (DspLib.Window.Type.Hamming, 0.54),
            (DspLib.Window.Type.BH92, 0.35875),
            (DspLib.Window.Type.Nutall3, 0.375)
        };

        foreach (var Local_Case in Local_Cases)
        {
            int Local_N = 64;
            var Local_Coefficients = DspLib.Window.Coefficients(Local_Case.Type, Local_N);

            double Local_Mean = 0;
            for (int i = 0; i < Local_N; i++)
                Local_Mean += Local_Coefficients[i];
            Local_Mean /= Local_N;

            Assert.AreEqual(Local_Case.CoherentGain, Local_Mean, AnalyticTolerance,
                Local_Case.Type + " coherent gain");
            Assert.AreEqual(1.0 / Local_Case.CoherentGain, DspLib.Window.ScaleFactor.Signal(Local_Coefficients), AnalyticTolerance,
                Local_Case.Type + " ScaleFactor.Signal must be 1/coherentGain");
        }
    }

    /// <summary>
    /// Normalized equivalent noise bandwidth: 1.0 for rectangular, 1.5 for Hann.
    /// </summary>
    [TestMethod]
    public void Property_Window_NENBW_MatchesTheTextbookValues()
    {
        Assert.AreEqual(1.0, DspLib.Window.ScaleFactor.NENBW(DspLib.Window.Coefficients(DspLib.Window.Type.Rectangular, 64)), AnalyticTolerance);
        Assert.AreEqual(1.5, DspLib.Window.ScaleFactor.NENBW(DspLib.Window.Coefficients(DspLib.Window.Type.Hann, 64)), AnalyticTolerance);
    }

    /// <summary>
    /// An unrecognized Window.Type falls through the switch and returns an all-zero array rather
    /// than throwing. Pinned as-is.
    /// </summary>
    [TestMethod]
    public void Property_Window_UnknownTypeReturnsAllZeros()
    {
        var Local_Coefficients = DspLib.Window.Coefficients((DspLib.Window.Type)9999, 8);
        Assert.AreEqual(8, Local_Coefficients.Length);
        for (int i = 0; i < 8; i++)
            DspCharacterization.AssertExact(0.0d, Local_Coefficients[i], "Unknown window type coefficient " + i);
    }

    #endregion

    #region Golden Vectors - Windows

    [TestMethod]
    public void Golden_Window_Coefficients_ClassicWindowsAt8Points()
    {
        DspCharacterization.AssertExact(
            new double[] { 1.0d, 1.0d, 1.0d, 1.0d, 1.0d, 1.0d, 1.0d, 1.0d },
            DspLib.Window.Coefficients(DspLib.Window.Type.Rectangular, 8), "Rectangular");

        double[] Local_Hann =
        {
            0.0d, 0.1464466094067262d, 0.49999999999999994d, 0.8535533905932737d,
            1.0d, 0.8535533905932738d, 0.5000000000000001d, 0.14644660940672632d
        };
        DspCharacterization.AssertExact(Local_Hann, DspLib.Window.Coefficients(DspLib.Window.Type.Hann, 8), "Hann");
        DspCharacterization.AssertExact(Local_Hann, DspLib.Window.Coefficients(DspLib.Window.Type.Hanning, 8),
            "Hanning must be bit-identical to Hann");

        DspCharacterization.AssertExact(
            new double[]
            {
                0.08000000000000002d, 0.21473088065418816d, 0.54d, 0.8652691193458119d,
                1.0d, 0.865269119345812d, 0.5400000000000001d, 0.21473088065418822d
            },
            DspLib.Window.Coefficients(DspLib.Window.Type.Hamming, 8), "Hamming");

        DspCharacterization.AssertExact(
            new double[] { 0.125d, 0.375d, 0.625d, 0.875d, 0.875d, 0.625d, 0.375d, 0.125d },
            DspLib.Window.Coefficients(DspLib.Window.Type.Bartlett, 8), "Bartlett");

        DspCharacterization.AssertExact(
            new double[] { 0.0d, 0.4375d, 0.75d, 0.9375d, 1.0d, 0.9375d, 0.75d, 0.4375d },
            DspLib.Window.Coefficients(DspLib.Window.Type.Welch, 8), "Welch");
    }

    [TestMethod]
    public void Golden_Window_Coefficients_FlatTopFamilyAt8Points()
    {
        DspCharacterization.AssertExact(
            new double[]
            {
                -0.0004210510000000013d, -0.026872193286334545d, -0.05473684000000003d, 0.4441353572863344d,
                1.000000003d, 0.44413535728633485d, -0.05473683999999998d, -0.026872193286334587d
            },
            DspLib.Window.Coefficients(DspLib.Window.Type.FlatTop, 8), "FlatTop");

        DspCharacterization.AssertExact(
            new double[]
            {
                6.0000000000001025E-05d, 0.021735837018679604d, 0.21746999999999997d, 0.6957641629813204d,
                1.0d, 0.6957641629813205d, 0.21747000000000014d, 0.021735837018679642d
            },
            DspLib.Window.Coefficients(DspLib.Window.Type.BH92, 8), "BH92");

        DspCharacterization.AssertExact(
            new double[]
            {
                0.0d, 0.02144660940672622d, 0.24999999999999994d, 0.7285533905932737d,
                1.0d, 0.7285533905932738d, 0.2500000000000001d, 0.021446609406726273d
            },
            DspLib.Window.Coefficients(DspLib.Window.Type.Nutall3, 8), "Nutall3");

        DspCharacterization.AssertExact(
            new double[]
            {
                -2.42861286636753E-17d, 0.020039357146876702d, 0.21153599999999992d, 0.6914966428531232d,
                1.0d, 0.6914966428531233d, 0.21153600000000009d, 0.02003935714687674d
            },
            DspLib.Window.Coefficients(DspLib.Window.Type.Nutall4B, 8), "Nutall4B");

        DspCharacterization.AssertExact(
            new double[]
            {
                0.0d, -0.08829339059327378d, 0.030519999999999936d, 0.6188133905932738d,
                1.0d, 0.6188133905932739d, 0.030520000000000103d, -0.08829339059327378d
            },
            DspLib.Window.Coefficients(DspLib.Window.Type.SFT3F, 8), "SFT3F");

        DspCharacterization.AssertExact(
            new double[]
            {
                -1.0408340855860843E-16d, -0.10502501423848876d, -0.29722099999999996d, 2.018831014238488d,
                4.766830000000001d, 2.0188310142384895d, -0.2972209999999997d, -0.10502501423848898d
            },
            DspLib.Window.Coefficients(DspLib.Window.Type.HFT90D, 8), "HFT90D");

        DspCharacterization.AssertExact(
            new double[]
            {
                3.3126664473727635E-17d, -0.0007397079822929469d, -0.17943087832800006d, 0.6664337367502922d,
                7.032470056319999d, 0.6664337367502952d, -0.17943087832799967d, -0.0007397079822940172d
            },
            DspLib.Window.Coefficients(DspLib.Window.Type.HFT248D, 8), "HFT248D");
    }

    /// <summary>
    /// SineExpansion is the shared kernel behind almost every window, so it is pinned directly with
    /// all eleven coefficients non-zero.
    /// </summary>
    [TestMethod]
    public void Golden_Window_SineExpansion_AllElevenCoefficients()
    {
        DspCharacterization.AssertExact(
            new double[]
            {
                1.9990234375d, 1.1913795456929006d, 0.7998046875d, 0.6914329543070994d,
                0.6669921875d, 0.6914329543070992d, 0.7998046874999999d, 1.1913795456929006d
            },
            DspLib.Window.SineExpansion(8, 1.0, 0.5, 0.25, 0.125, 0.0625, 0.03125, 0.015625, 0.0078125, 0.00390625, 0.001953125, 0.0009765625),
            "SineExpansion with every coefficient exercised");
    }

    [TestMethod]
    public void Golden_Window_ScaleFactors()
    {
        var Local_Hann16 = DspLib.Window.Coefficients(DspLib.Window.Type.Hann, 16);
        DspCharacterization.AssertExact(2.0000000000000004d, DspLib.Window.ScaleFactor.Signal(Local_Hann16), "Signal(Hann16)");
        DspCharacterization.AssertExact(0.029814239699997195d, DspLib.Window.ScaleFactor.Noise(Local_Hann16, 48000), "Noise(Hann16, 48 kHz)");
        DspCharacterization.AssertExact(1.5000000000000004d, DspLib.Window.ScaleFactor.NENBW(Local_Hann16), "NENBW(Hann16)");

        var Local_FlatTop16 = DspLib.Window.Coefficients(DspLib.Window.Type.FlatTop, 16);
        DspCharacterization.AssertExact(4.638671818375586d, DspLib.Window.ScaleFactor.Signal(Local_FlatTop16), "Signal(FlatTop16)");
        DspCharacterization.AssertExact(3.770246447443427d, DspLib.Window.ScaleFactor.NENBW(Local_FlatTop16), "NENBW(FlatTop16)");
    }

    #endregion

    #region Golden Vectors - Generate

    [TestMethod]
    public void Golden_Generate_LinSpace()
    {
        DspCharacterization.AssertExact(
            new double[] { 1.0d, 2.0d, 3.0d, 4.0d, 5.0d, 6.0d, 7.0d, 8.0d, 9.0d, 10.0d },
            DspLib.Generate.LinSpace(1, 10, 10), "LinSpace(1, 10, 10)");
    }

    [TestMethod]
    public void Golden_Generate_ToneSamplingAndToneCycles()
    {
        DspCharacterization.AssertExact(
            new double[]
            {
                0.9571067811865475d, 1.1109186691537587d, 1.2500000000000002d, 1.371971053593862d,
                1.4747448713915892d, 1.5565629648763766d, 1.6160254037844386d, 1.6521147692999558d
            },
            DspLib.Generate.ToneSampling(1.0, 1000, 48000, 8, 0.25, 30), "ToneSampling");

        DspCharacterization.AssertExact(
            new double[]
            {
                1.1000000000000003d, 1.1000000000000003d, -0.9d, -0.9000000000000002d,
                1.0999999999999999d, 1.1000000000000014d, -0.9000000000000007d, -0.9000000000000014d
            },
            DspLib.Generate.ToneCycles(1.0, 2, 8, 0.1, 45), "ToneCycles");
    }

    #endregion

    #region Golden Vectors - Converters

    [TestMethod]
    public void Golden_ConvertMagnitude()
    {
        var Local_Magnitude = new double[] { 0.0, 0.5, 1.0, 2.0, -3.0 };

        DspCharacterization.AssertExact(
            new double[] { 0.0d, 0.25d, 1.0d, 4.0d, 9.0d },
            DspLib.ConvertMagnitude.ToMagnitudeSquared(Local_Magnitude), "ToMagnitudeSquared");

        //Zero and negative magnitudes are substituted with double.Epsilon and then floored.
        DspCharacterization.AssertExact(
            new double[] { -400.0d, -6.020599913279624d, 0.0d, 6.020599913279624d, -400.0d },
            DspLib.ConvertMagnitude.ToMagnitudeDBV(Local_Magnitude), "ToMagnitudeDBV (defaults)");

        DspCharacterization.AssertExact(
            new double[] { -60.0d, 0.0d, 6.020599913279624d, 12.041199826559248d, -60.0d },
            DspLib.ConvertMagnitude.ToMagnitudeDBV(Local_Magnitude, 2.0, -60.0), "ToMagnitudeDBV (scale 2, floor -60)");
    }

    [TestMethod]
    public void Golden_ConvertComplex()
    {
        var Local_Spectrum = new Complex[] { new(0, 0), new(1, 0), new(0, 1), new(-1, -1), new(3, 4) };

        DspCharacterization.AssertExact(
            new double[] { 0.0d, 1.0d, 1.0d, 1.4142135623730951d, 5.0d },
            DspLib.ConvertComplex.ToMagnitude(Local_Spectrum), "ToMagnitude");

        //A zero magnitude becomes double.Epsilon, i.e. about -6466 dBV - there is no floor here.
        DspCharacterization.AssertExact(
            new double[] { -6466.124306862316d, 0.0d, 0.0d, 3.0102999566398125d, 13.979400086720377d },
            DspLib.ConvertComplex.ToMagnitudeDBV(Local_Spectrum), "ToMagnitudeDBV");

        DspCharacterization.AssertExact(
            new double[] { 0.0d, 0.0d, 90.0d, -135.0d, 53.13010235415598d },
            DspLib.ConvertComplex.ToPhaseDegrees(Local_Spectrum), "ToPhaseDegrees");
    }

    #endregion

    #region Golden Vectors - Array Math

    [TestMethod]
    public void Golden_Math_ArrayOperators()
    {
        var Local_A = new double[] { 1.5, -2.25, 0.125, 4.0 };
        var Local_B = new double[] { 0.5, 3.0, -1.25, 2.0 };

        DspCharacterization.AssertExact(new double[] { 0.75d, -6.75d, -0.15625d, 8.0d },
            DspLib.Math.Multiply(Local_A, Local_B), "Multiply(a[], b[])");
        DspCharacterization.AssertExact(new double[] { 0.44999999999999996d, -0.6749999999999999d, 0.0375d, 1.2d },
            DspLib.Math.Multiply(Local_A, 0.3), "Multiply(a[], b)");
        DspCharacterization.AssertExact(new double[] { 1.8d, -1.95d, 0.425d, 4.3d },
            DspLib.Math.Add(Local_A, 0.3), "Add(a[], b)");
        DspCharacterization.AssertExact(new double[] { 1.2d, -2.55d, -0.175d, 3.7d },
            DspLib.Math.Subtract(Local_A, 0.3), "Subtract(a[], b)");
    }

    [TestMethod]
    public void Golden_Math_MultiplyRejectsMismatchedLengths()
    {
        Assert.ThrowsExactly<ArgumentException>(() => DspLib.Math.Multiply(new double[3], new double[4]));
    }

    #endregion

    #region Golden Vectors - Analyze

    [TestMethod]
    public void Golden_Analyze_FindMaxAndMinPosition()
    {
        var Local_Data = new double[] { 0.2, -0.9, 0.7, 0.7, -0.1, 0.35 };

        Assert.AreEqual(2, DspLib.Analyze.FindMaxPosition(Local_Data, 0, Local_Data.Length), "First of the tied maxima wins");
        Assert.AreEqual(3, DspLib.Analyze.FindMaxPosition(Local_Data, 3, 5), "Sub-range");
        Assert.AreEqual(2, DspLib.Analyze.FindMaxPosition(Local_Data, -5, 100), "Out-of-range bounds are clamped");
        Assert.AreEqual(4, DspLib.Analyze.FindMaxPosition(Local_Data, 4, 2), "An inverted range returns min(minIndex, len-1)");
        Assert.AreEqual(0, DspLib.Analyze.FindMaxPosition(Array.Empty<double>(), 0, 0), "An empty array returns 0");

        Assert.AreEqual(1, DspLib.Analyze.FindMinPosition(Local_Data, 0, Local_Data.Length));
        Assert.AreEqual(4, DspLib.Analyze.FindMinPosition(Local_Data, 2, 6));
        Assert.AreEqual(4, DspLib.Analyze.FindMinPosition(Local_Data, 4, 2), "An inverted range returns min(minIndex, len-1)");
    }

    [TestMethod]
    public void Golden_Analyze_UnwrapPhaseDegrees()
    {
        DspCharacterization.AssertExact(
            new double[] { 0.0d, 170.0d, 190.0d, 350.0d, 179.0d, 181.0d, 5.0d },
            DspLib.Analyze.UnwrapPhaseDegrees(new double[] { 0, 170, -170, -10, 179, -179, 5 }),
            "UnwrapPhaseDegrees");

        Assert.AreEqual(0, DspLib.Analyze.UnwrapPhaseDegrees(Array.Empty<double>()).Length);
    }

    /// <summary>
    /// Unwrapping never introduces a step larger than 180 degrees.
    /// </summary>
    [TestMethod]
    public void Property_Analyze_UnwrapNeverStepsMoreThan180Degrees()
    {
        var Local_Wrapped = new double[64];
        for (int i = 0; i < Local_Wrapped.Length; i++)
        {
            double Local_Continuous = -37.5 * i;
            double Local_Value = Local_Continuous % 360.0;
            if (Local_Value > 180.0) Local_Value -= 360.0;
            if (Local_Value < -180.0) Local_Value += 360.0;
            Local_Wrapped[i] = Local_Value;
        }

        var Local_Unwrapped = DspLib.Analyze.UnwrapPhaseDegrees(Local_Wrapped);
        for (int i = 1; i < Local_Unwrapped.Length; i++)
        {
            Assert.IsTrue(Math.Abs(Local_Unwrapped[i] - Local_Unwrapped[i - 1]) <= 180.0 + 1e-9,
                "Step at index " + i + " exceeded 180 degrees");
        }
    }

    #endregion

    #region Golden Vectors - DownSampler

    [TestMethod]
    public void Golden_DownSampler_ReducesTwentyPointsToSix()
    {
        var Local_Input = DspCharacterization.Noise(20, 999UL);
        DspCharacterization.AssertExact(
            new double[]
            {
                0.46744826660650163d, -0.194654670725114d, 0.19671717896828023d, -0.8664883403164063d,
                0.5875129557073795d, -0.4542851100624725d, -0.14140775960054053d, -0.8271292286169971d,
                -0.8831689179947861d, 0.11427299484603326d, 0.9678346236434965d, 0.5181136324342224d,
                -0.28564609764289184d, 0.24136428939664567d, -0.7471368842511152d, 0.7275898274421315d,
                0.8279867889915848d, 0.6350260830195891d, 0.07464174317727368d, -0.1912391321462108d
            },
            Local_Input, "The deterministic LCG source itself must not drift");

        DspCharacterization.AssertExact(
            new double[]
            {
                0.46744826660650163d, 0.5875129557073795d, -0.8831689179947861d,
                0.9678346236434965d, 0.8279867889915848d, -0.1912391321462108d
            },
            DownSampler.downsample(Local_Input, 6), "downsample(20 -> 6)");
    }

    /// <summary>
    /// Downsampling always keeps the first and last points, and a threshold at or above the data
    /// length is a no-op that returns the SAME array instance.
    /// </summary>
    [TestMethod]
    public void Property_DownSampler_PreservesEndpointsAndShortCircuits()
    {
        var Local_Input = DspCharacterization.Noise(50, 31337UL);

        var Local_Result = DownSampler.downsample(Local_Input, 12);
        Assert.AreEqual(12, Local_Result.Length);
        DspCharacterization.AssertExact(Local_Input[0], Local_Result[0], "First point is always kept");
        DspCharacterization.AssertExact(Local_Input[^1], Local_Result[^1], "Last point is always kept");

        Assert.IsTrue(ReferenceEquals(Local_Input, DownSampler.downsample(Local_Input, 50)),
            "threshold >= length short-circuits and returns the original instance");
        Assert.IsTrue(ReferenceEquals(Local_Input, DownSampler.downsample(Local_Input, 0)),
            "threshold <= 0 short-circuits and returns the original instance");
    }

    #endregion
}
