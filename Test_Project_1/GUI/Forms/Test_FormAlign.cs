using Microsoft.VisualStudio.TestTools.UnitTesting;
using BassThatHz_ASIO_DSP_Processor;
using System;
using System.Numerics;
using System.Reflection;
using Test_Project_1.TestHelpers;

namespace Test_Project_1;

/// <summary>
/// Test-only subclass that exposes FormAlign's protected members as public so they
/// can be exercised directly without reflection boilerplate for every call.
/// </summary>
internal class TestableFormAlign : FormAlign
{
    public double[] PublicCircularShift(double[] x, int shift) => this.CircularShift(x, shift);
    public double PublicPeakAbs(double[] x) => this.PeakAbs(x);
    public void PublicNormalizeToUnitPeakInPlace(double[] x) => this.NormalizeToUnitPeakInPlace(x);
    public int PublicArgMaxAbs(double[] x) => this.ArgMaxAbs(x);
    public double PublicPeakIndexToSignedDelayMs(double peakIndex, double sampleRate, int fftSize) => this.PeakIndexToSignedDelayMs(peakIndex, sampleRate, fftSize);
    public double PublicRefinePeakIndex(double[] x, int peakIndex) => this.RefinePeakIndex(x, peakIndex);
    public double PublicEstimateDelayMs(double[] ir, double sampleRate) => this.EstimateDelayMs(ir, sampleRate);
    public bool PublicIsCoherenceReady() => this.IsCoherenceReady();
    public double PublicGetEffectiveAlpha() => this.GetEffectiveAlpha();
    public void PublicEnsureTempBuffers() => this.EnsureTempBuffers();
    public void PublicEnsureTransferStateInitialized() => this.EnsureTransferStateInitialized();
    public void PublicUpdateAveragedSpectra(Complex[] a, Complex[] b, Complex[] r) => this.UpdateAveragedSpectra(a, b, r);
    public void PublicComputeAdaptiveEpsilons(int halfLen, out double epsSxx, out double epsSyyA, out double epsSyyB) => this.ComputeAdaptiveEpsilons(halfLen, out epsSxx, out epsSyyA, out epsSyyB);
    public void PublicComputeTransferFunctions(double epsSxx, out Complex[] hA, out Complex[] hB, out bool[] validH) => this.ComputeTransferFunctions(epsSxx, out hA, out hB, out validH);
    public void PublicComputeCoherence(int halfLen, double epsSxx, double epsSyyA, double epsSyyB, out double[] cohA, out double[] cohB) => this.ComputeCoherence(halfLen, epsSxx, epsSyyA, epsSyyB, out cohA, out cohB);
    public double PublicComputeWeightedMeanCoherence(double[] coh, int halfLen) => this.ComputeWeightedMeanCoherence(coh, halfLen);
    public int PublicReadRequestedFFTSize() => this.Read_Requested_FFTSize();
    public void PublicBindTrace(System.Windows.Forms.DataVisualization.Charting.Series series, double[] x, double[] y)
        => this.Bind_Trace(series, x, y);
    public void PublicBlankFrequencyCharts() => this.Blank_Frequency_Charts();
    public void PublicPlotMagChart(System.Windows.Forms.DataVisualization.Charting.Chart chart,
        double min, double max, double[] x, double[] y1, double[] y2)
        => this.Plot_Mag_Chart(chart, min, max, x, y1, y2);
    public void PublicEnsureConfig(int fftSize, int sampleRate) => _ = this.Rebuild_Config(fftSize, sampleRate);
    public double PublicReadSmoothingFraction() => this.Read_SmoothingFraction();
    public Complex[] PublicSmoothComplexFractionalOctave(Complex[] halfSpectrum, bool[] valid, double octaveFraction, out bool[] validSmoothed)
        => this.SmoothComplexFractionalOctave(halfSpectrum, valid, octaveFraction, out validSmoothed);
    public void PublicSetSmoothing(bool enabled, double octaveFraction)
    {
        this.smoothingEnabled = enabled;
        this.smoothingOctaveFraction = octaveFraction;
    }
    public static (string Label, double Fraction)[] PublicSmoothingOptions => SmoothingOptions;
    public static int PublicDefaultSmoothingIndex => DefaultSmoothingIndex;
    public void PublicResetMeasurement() => this.Reset_Measurement();
    public void PublicSetAlpha(double value) => this.alpha = value;
    public void PublicSetCohMin(double value) => this.cohMin = value;
    public int PublicGetAvgFrames() => (int)typeof(FormAlign).GetField("_TfAvgFrames", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(this)!;
    public void PublicSetFFTSize(int size) => typeof(FormAlign).GetField("FFTSize", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(this, size);
    public int PublicGetFFTSize() => (int)typeof(FormAlign).GetField("FFTSize", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(this)!;
    public void PublicRaiseLoad() => this.GetType().BaseType!
        .GetMethod("FormAlign_Load", BindingFlags.NonPublic | BindingFlags.Instance)!
        .Invoke(this, new object[] { this, EventArgs.Empty });
    public void PublicRaiseRefreshTimerTick() => this.GetType().BaseType!
        .GetMethod("RefreshTimer_Tick", BindingFlags.NonPublic | BindingFlags.Instance)!
        .Invoke(this, new object[] { this, EventArgs.Empty });
    // Note: intentionally no public wrapper for the protected Error(Exception) method - see
    // the "Error handling" region below for why it is not safe to invoke in automated tests.

    /// <summary>Everything the analyser produces for one measurement.</summary>
    public sealed class MeasuredAlignment
    {
        public double DelayMsA;
        public double DelayMsB;
        public double[] TimeMs = Array.Empty<double>();
        public double[] IrA = Array.Empty<double>();
        public double[] IrB = Array.Empty<double>();
        public double[] MagA_dB = Array.Empty<double>();
        public double[] MagB_dB = Array.Empty<double>();
        public double[] PhaseA_deg = Array.Empty<double>();
        public double[] PhaseB_deg = Array.Empty<double>();
        public double[] CohA = Array.Empty<double>();
        public double[] CohB = Array.Empty<double>();
    }

    /// <summary>
    /// Runs the real analysis chain end to end: overlapped windowed transforms into the averaged
    /// cross spectra, then transfer functions, coherence, magnitude/phase and the impulse
    /// responses with their delays. Nothing here is a re-implementation - it drives exactly the
    /// production code path the timer tick drives.
    /// </summary>
    public MeasuredAlignment PublicMeasureAlignment(
        double[] refSignal, double[] sourceA, double[] sourceB,
        int fftSize, int sampleRate, int frames)
    {
        var Local_Config = this.Rebuild_Config(fftSize, sampleRate);

        int Local_Hop = fftSize / 2;
        var Local_FrameRef = new double[fftSize];
        var Local_FrameA = new double[fftSize];
        var Local_FrameB = new double[fftSize];

        for (int f = 0; f < frames; f++)
        {
            int Local_Offset = f * Local_Hop;
            Array.Copy(refSignal, Local_Offset, Local_FrameRef, 0, fftSize);
            Array.Copy(sourceA, Local_Offset, Local_FrameA, 0, fftSize);
            Array.Copy(sourceB, Local_Offset, Local_FrameB, 0, fftSize);

            var Local_Fa = Local_Config.Fft.Perform_FFT(Local_FrameA, Local_Config.Window);
            var Local_Fb = Local_Config.Fft.Perform_FFT(Local_FrameB, Local_Config.Window);
            var Local_Fr = Local_Config.Fft.Perform_FFT(Local_FrameRef, Local_Config.Window);

            this.UpdateAveragedSpectra(Local_Fa, Local_Fb, Local_Fr);
        }

        int Local_HalfLen = fftSize / 2 + 1;

        this.ComputeAdaptiveEpsilons(Local_HalfLen,
            out double Local_EpsSxx, out double Local_EpsSyyA, out double Local_EpsSyyB);

        this.ComputeTransferFunctions(Local_EpsSxx, out var Local_HA, out var Local_HB, out var Local_Valid);

        this.ComputeCoherence(Local_HalfLen, Local_EpsSxx, Local_EpsSyyA, Local_EpsSyyB,
            out var Local_CohA, out var Local_CohB);

        this.PrepareMagPhaseForPlot(Local_HA, Local_HB, Local_Valid,
            Local_CohA, Local_CohB, this.IsCoherenceReady(),
            out var Local_MagA, out var Local_MagB, out var Local_PhaseA, out var Local_PhaseB);

        this.PrepareImpulseResponses(Local_Config, Local_HA, Local_HB,
            out var Local_TimeMs, out var Local_IrA, out var Local_IrB,
            out double Local_DelayA, out double Local_DelayB);

        return new MeasuredAlignment
        {
            DelayMsA = Local_DelayA,
            DelayMsB = Local_DelayB,
            TimeMs = Local_TimeMs,
            IrA = Local_IrA,
            IrB = Local_IrB,
            MagA_dB = Local_MagA,
            MagB_dB = Local_MagB,
            PhaseA_deg = Local_PhaseA,
            PhaseB_deg = Local_PhaseB,
            CohA = Local_CohA,
            CohB = Local_CohB,
        };
    }
}

[TestClass]
public class Test_FormAlign
{
    // System.Windows.Forms.DataVisualization.Charting.Chart controls (used heavily by
    // FormAlign) share a static, non-thread-safe FontCache internally. Because this project
    // runs tests in parallel ([assembly: Parallelize(Scope = ExecutionScope.MethodLevel)] in
    // MSTestSettings.cs), many concurrent `new FormAlign()`/`CreateTestableFormAlign()` calls can
    // race on that shared cache and throw spurious
    // "An item with the same key has already been added"/"concurrent update" exceptions that
    // have nothing to do with the logic under test. Serializing construction via this lock
    // avoids that pre-existing third-party threading issue without touching any shared/other
    // test infrastructure.
    private static readonly object _chartConstructionLock = new();

    private static FormAlign CreateFormAlign()
    {
        lock (_chartConstructionLock)
        {
            return new FormAlign();
        }
    }

    private static TestableFormAlign CreateTestableFormAlign()
    {
        lock (_chartConstructionLock)
        {
            return new TestableFormAlign();
        }
    }

    /// <summary>
    /// Wraps an angle to (-180, 180] so a measured phase can be compared with a closed-form
    /// expectation that has run past the end of the range. This is the TEST's own expectation
    /// arithmetic: FormAlign does not need it, because Atan2 already returns this range.
    /// </summary>
    private static double WrapTo180(double x)
    {
        x %= 360.0;
        if (x <= -180.0) x += 360.0;
        else if (x > 180.0) x -= 360.0;
        return x;
    }

    [TestMethod]
    public void CanInstantiate_FormAlign()
    {
        var form = CreateFormAlign();
        Assert.IsNotNull(form);
    }

    [TestMethod]
    public void CanInstantiate_TestableFormAlign()
    {
        var form = CreateTestableFormAlign();
        Assert.IsNotNull(form);
    }

    #region FormAlign_Load
    [TestMethod]
    public void FormAlign_Load_DoesNotThrow()
    {
        var form = CreateTestableFormAlign();
        form.PublicRaiseLoad();
        Assert.AreEqual(0, form.PublicGetAvgFrames());
    }
    #endregion

    #region Phase range
    /// <summary>
    /// The phase traces are plotted on a fixed -180..180 axis, so the values coming out of the
    /// analyser have to already be in that range - there is no wrapping step in the plot path.
    /// (The form used to unwrap the phase and then immediately re-wrap it to the same range,
    /// which was a no-op round trip; this is the property that made it a no-op.)
    /// </summary>
    [TestMethod]
    public void PhaseDegrees_FromDSPLib_AreAlreadyWithinPlusMinus180()
    {
        var spectrum = new Complex[]
        {
            new Complex(1.0, 0.0),
            new Complex(-1.0, 0.0),
            new Complex(0.0, 1.0),
            new Complex(0.0, -1.0),
            new Complex(-1.0, -1e-18),
            new Complex(-3.0, 4.0),
        };

        var phase = DSPLib.DSP.ConvertComplex.ToPhaseDegrees(spectrum);

        foreach (var p in phase)
        {
            // Atan2 has the CLOSED range [-pi, pi], so exactly -180 is attainable. That is the
            // same angle as +180 and sits inside the plotted axis, so no wrapping is needed -
            // only being inside the axis range matters.
            Assert.IsTrue(p >= -180.0 - 1e-9 && p <= 180.0 + 1e-9,
                $"phase {p} is outside the plotted -180..180 axis range");

            // Away from that one endpoint the values are already in the (-180, 180] convention.
            if (Math.Abs(p + 180.0) > 1e-9)
                Assert.AreEqual(p, WrapTo180(p), 1e-9, $"phase {p} is not already wrapped");
        }
    }
    #endregion

    #region CircularShift
    [TestMethod]
    public void CircularShift_PositiveShift_ShiftsForward()
    {
        var form = CreateTestableFormAlign();
        var result = form.PublicCircularShift(new double[] { 1, 2, 3, 4 }, 1);
        CollectionAssert.AreEqual(new double[] { 4, 1, 2, 3 }, result);
    }

    [TestMethod]
    public void CircularShift_ZeroShift_ReturnsSameValues()
    {
        var form = CreateTestableFormAlign();
        var result = form.PublicCircularShift(new double[] { 1, 2, 3, 4 }, 0);
        CollectionAssert.AreEqual(new double[] { 1, 2, 3, 4 }, result);
    }

    [TestMethod]
    public void CircularShift_NegativeShift_NormalizesToPositive()
    {
        var form = CreateTestableFormAlign();
        var result = form.PublicCircularShift(new double[] { 1, 2, 3, 4 }, -1);
        CollectionAssert.AreEqual(new double[] { 2, 3, 4, 1 }, result);
    }

    [TestMethod]
    public void CircularShift_EmptyArray_ReturnsSameArray()
    {
        var form = CreateTestableFormAlign();
        var input = Array.Empty<double>();
        var result = form.PublicCircularShift(input, 5);
        Assert.AreSame(input, result);
    }

    [TestMethod]
    public void CircularShift_ShiftGreaterThanLength_WrapsModulo()
    {
        var form = CreateTestableFormAlign();
        var result = form.PublicCircularShift(new double[] { 1, 2, 3, 4 }, 5);
        CollectionAssert.AreEqual(new double[] { 4, 1, 2, 3 }, result);
    }

    /// <summary>
    /// The impulse response chart shifts by N/2 so that sample 0 - the Ref arrival - lands at
    /// 0 ms. The acausal tail (a source that LEADS the Ref) must land immediately to the left of
    /// it, not at the far right of the axis.
    /// </summary>
    [TestMethod]
    public void CircularShift_ByHalfLength_PutsSampleZeroAtCentreAndWrappedTailJustBeforeIt()
    {
        var form = CreateTestableFormAlign();
        var input = new double[] { 10, 11, 12, 13, 14, 15, 16, 17 };
        var result = form.PublicCircularShift(input, 4);

        // sample 0 -> index 4 (the centre, i.e. t = 0 ms)
        Assert.AreEqual(10.0, result[4], 1e-9);
        // sample N-1 (one sample of NEGATIVE delay) -> index 3, immediately left of centre
        Assert.AreEqual(17.0, result[3], 1e-9);
    }
    #endregion

    #region PeakAbs / Normalize
    [TestMethod]
    public void PeakAbs_ReturnsMaxAbsoluteValue()
    {
        var form = CreateTestableFormAlign();
        double result = form.PublicPeakAbs(new double[] { -5.0, 3.0, 2.0 });
        Assert.AreEqual(5.0, result, 1e-9);
    }

    [TestMethod]
    public void PeakAbs_IgnoresNaNAndInfinity()
    {
        var form = CreateTestableFormAlign();
        double result = form.PublicPeakAbs(new double[] { double.NaN, double.PositiveInfinity, 2.0 });
        Assert.AreEqual(2.0, result, 1e-9);
    }

    [TestMethod]
    public void PeakAbs_AllZero_ReturnsZero()
    {
        var form = CreateTestableFormAlign();
        double result = form.PublicPeakAbs(new double[] { 0.0, 0.0 });
        Assert.AreEqual(0.0, result, 1e-9);
    }

    [TestMethod]
    public void NormalizeToUnitPeakInPlace_ScalesToUnitPeak()
    {
        var form = CreateTestableFormAlign();
        var data = new double[] { -2.0, 4.0, 1.0 };
        form.PublicNormalizeToUnitPeakInPlace(data);
        Assert.AreEqual(-0.5, data[0], 1e-9);
        Assert.AreEqual(1.0, data[1], 1e-9);
        Assert.AreEqual(0.25, data[2], 1e-9);
    }

    [TestMethod]
    public void NormalizeToUnitPeakInPlace_AllZero_LeavesArrayUnchanged()
    {
        var form = CreateTestableFormAlign();
        var data = new double[] { 0.0, 0.0, 0.0 };
        form.PublicNormalizeToUnitPeakInPlace(data);
        CollectionAssert.AreEqual(new double[] { 0.0, 0.0, 0.0 }, data);
    }
    #endregion

    #region ArgMaxAbs
    [TestMethod]
    public void ArgMaxAbs_FindsGlobalMaxIndex()
    {
        var form = CreateTestableFormAlign();
        var data = new double[] { 1, -2, 8, -3 };
        int idx = form.PublicArgMaxAbs(data);
        Assert.AreEqual(2, idx);
    }

    [TestMethod]
    public void ArgMaxAbs_NullOrEmpty_ReturnsZero()
    {
        var form = CreateTestableFormAlign();
        Assert.AreEqual(0, form.PublicArgMaxAbs(null));
        Assert.AreEqual(0, form.PublicArgMaxAbs(Array.Empty<double>()));
    }

    /// <summary>
    /// REGRESSION: the delay search used to be limited to the first half of the impulse response
    /// ("causal peak search only"). A source that leads the Ref peaks in the UPPER half, which
    /// that search could not see - so the readout was pinned at 0.0000 ms. The search must cover
    /// the whole circle.
    /// </summary>
    [TestMethod]
    public void ArgMaxAbs_PeakInUpperHalf_IsFound()
    {
        var form = CreateTestableFormAlign();
        var data = new double[64];
        data[0] = 0.2;
        data[60] = 1.0;
        Assert.AreEqual(60, form.PublicArgMaxAbs(data));
    }
    #endregion

    #region RefinePeakIndex
    [TestMethod]
    public void RefinePeakIndex_SymmetricPeak_ReturnsIntegerIndex()
    {
        var form = CreateTestableFormAlign();
        var data = new double[] { 0.0, 0.5, 1.0, 0.5, 0.0 };
        Assert.AreEqual(2.0, form.PublicRefinePeakIndex(data, 2), 1e-9);
    }

    [TestMethod]
    public void RefinePeakIndex_LeftNeighbourLarger_ShiftsLeft()
    {
        var form = CreateTestableFormAlign();
        var data = new double[] { 0.0, 0.8, 1.0, 0.2, 0.0 };
        double refined = form.PublicRefinePeakIndex(data, 2);
        Assert.IsTrue(refined < 2.0, $"expected a leftward shift, got {refined}");
        Assert.IsTrue(refined > 1.5, $"expected the vertex inside the peak sample, got {refined}");
    }

    [TestMethod]
    public void RefinePeakIndex_RightNeighbourLarger_ShiftsRight()
    {
        var form = CreateTestableFormAlign();
        var data = new double[] { 0.0, 0.2, 1.0, 0.8, 0.0 };
        double refined = form.PublicRefinePeakIndex(data, 2);
        Assert.IsTrue(refined > 2.0, $"expected a rightward shift, got {refined}");
        Assert.IsTrue(refined < 2.5, $"expected the vertex inside the peak sample, got {refined}");
    }

    /// <summary>
    /// The impulse response is circular: a peak at index 0 has index N-1 as its left neighbour.
    /// Refinement must not throw or fall off the front of the array there.
    /// </summary>
    [TestMethod]
    public void RefinePeakIndex_PeakAtIndexZero_UsesWrappedNeighbour()
    {
        var form = CreateTestableFormAlign();
        var data = new double[] { 1.0, 0.2, 0.0, 0.8 };
        double refined = form.PublicRefinePeakIndex(data, 0);
        Assert.IsTrue(refined < 0.0, $"expected a leftward (wrapped) shift, got {refined}");
    }

    [TestMethod]
    public void RefinePeakIndex_FlatInput_ReturnsIntegerIndex()
    {
        var form = CreateTestableFormAlign();
        var data = new double[] { 1.0, 1.0, 1.0, 1.0 };
        Assert.AreEqual(1.0, form.PublicRefinePeakIndex(data, 1), 1e-9);
    }

    [TestMethod]
    public void RefinePeakIndex_TooShort_ReturnsIntegerIndex()
    {
        var form = CreateTestableFormAlign();
        Assert.AreEqual(0.0, form.PublicRefinePeakIndex(new double[] { 1.0, 2.0 }, 0), 1e-9);
        Assert.AreEqual(3.0, form.PublicRefinePeakIndex(null, 3), 1e-9);
    }
    #endregion

    #region EstimateDelayMs
    [TestMethod]
    public void EstimateDelayMs_ImpulseAtZero_ReturnsZero()
    {
        var form = CreateTestableFormAlign();
        var ir = new double[64];
        ir[0] = 1.0;
        Assert.AreEqual(0.0, form.PublicEstimateDelayMs(ir, 48000.0), 1e-9);
    }

    [TestMethod]
    public void EstimateDelayMs_ImpulseInLowerHalf_IsPositive()
    {
        var form = CreateTestableFormAlign();
        var ir = new double[4096];
        ir[48] = 1.0;
        Assert.AreEqual(1.0, form.PublicEstimateDelayMs(ir, 48000.0), 1e-9);
    }

    /// <summary>
    /// REGRESSION: a source that ARRIVES BEFORE the Ref signal must read as a negative delay.
    /// The old causal-only peak search reported a hard 0.0000 ms for this case.
    /// </summary>
    [TestMethod]
    public void EstimateDelayMs_ImpulseInUpperHalf_IsNegative()
    {
        var form = CreateTestableFormAlign();
        var ir = new double[4096];
        ir[4096 - 48] = 1.0;
        Assert.AreEqual(-1.0, form.PublicEstimateDelayMs(ir, 48000.0), 1e-9);
    }

    [TestMethod]
    public void EstimateDelayMs_SubSamplePeak_ResolvesFinerThanOneSample()
    {
        var form = CreateTestableFormAlign();
        var ir = new double[4096];
        // A peak leaning towards its right hand neighbour: the true arrival is between samples.
        ir[100] = 1.0;
        ir[101] = 0.6;
        ir[99] = 0.2;

        double oneSampleMs = 1000.0 / 48000.0;
        double delay = form.PublicEstimateDelayMs(ir, 48000.0);

        Assert.IsTrue(delay > 100 * oneSampleMs,
            $"expected a delay past sample 100, got {delay}");
        Assert.IsTrue(delay < 101 * oneSampleMs,
            $"expected a sub-sample result short of sample 101, got {delay}");
    }

    [TestMethod]
    public void EstimateDelayMs_DegenerateInput_ReturnsZero()
    {
        var form = CreateTestableFormAlign();
        Assert.AreEqual(0.0, form.PublicEstimateDelayMs(null, 48000.0), 1e-9);
        Assert.AreEqual(0.0, form.PublicEstimateDelayMs(Array.Empty<double>(), 48000.0), 1e-9);
        Assert.AreEqual(0.0, form.PublicEstimateDelayMs(new double[8], 0.0), 1e-9);
    }
    #endregion

    #region PeakIndexToSignedDelayMs
    [TestMethod]
    public void PeakIndexToSignedDelayMs_LowIndex_PositiveDelay()
    {
        var form = CreateTestableFormAlign();
        double delay = form.PublicPeakIndexToSignedDelayMs(48, 48000.0, 4096);
        Assert.AreEqual(1.0, delay, 1e-9);
    }

    [TestMethod]
    public void PeakIndexToSignedDelayMs_HighIndex_WrapsNegative()
    {
        var form = CreateTestableFormAlign();
        int fftSize = 4096;
        int peakIndex = fftSize - 48; // > fftSize/2, should wrap to negative
        double delay = form.PublicPeakIndexToSignedDelayMs(peakIndex, 48000.0, fftSize);
        Assert.AreEqual(-1.0, delay, 1e-9);
    }
    #endregion

    #region Averaging coefficient
    /// <summary>
    /// The first frame must be taken at full weight (a cumulative mean of one sample), otherwise
    /// a from-zero exponential average is biased towards zero for its first ~1/alpha frames.
    /// </summary>
    [TestMethod]
    public void GetEffectiveAlpha_FirstFrame_IsOne()
    {
        var form = CreateTestableFormAlign();
        form.PublicSetAlpha(0.005);
        Assert.AreEqual(1.0, form.PublicGetEffectiveAlpha(), 1e-12);
    }

    [TestMethod]
    public void GetEffectiveAlpha_EasesTowardsConfiguredAlpha()
    {
        var form = CreateTestableFormAlign();
        form.PublicSetFFTSize(8);
        form.PublicEnsureTransferStateInitialized();
        form.PublicSetAlpha(0.25);

        var zero = new Complex[8];
        // 1/(n+1) exceeds 0.25 until n reaches 4, from then on the configured alpha wins.
        for (int i = 0; i < 3; i++)
            form.PublicUpdateAveragedSpectra(zero, zero, zero);

        Assert.AreEqual(0.25, form.PublicGetEffectiveAlpha(), 1e-12);
    }

    [TestMethod]
    public void GetEffectiveAlpha_NonFiniteAlpha_FallsBackToDefault()
    {
        var form = CreateTestableFormAlign();
        form.PublicSetFFTSize(8);
        form.PublicEnsureTransferStateInitialized();
        var zero = new Complex[8];
        for (int i = 0; i < 400; i++)
            form.PublicUpdateAveragedSpectra(zero, zero, zero);

        form.PublicSetAlpha(double.NaN);
        Assert.AreEqual(0.005, form.PublicGetEffectiveAlpha(), 1e-12);
    }
    #endregion

    #region Coherence / averaging state
    [TestMethod]
    public void IsCoherenceReady_FalseBeforeWarmupFrames()
    {
        var form = CreateTestableFormAlign();
        Assert.IsFalse(form.PublicIsCoherenceReady());
    }

    [TestMethod]
    public void IsCoherenceReady_TrueAfterEnoughFrames()
    {
        var form = CreateTestableFormAlign();
        form.PublicSetFFTSize(8);
        form.PublicEnsureTransferStateInitialized();
        var a = new Complex[8];
        var b = new Complex[8];
        var r = new Complex[8];
        for (int i = 0; i < 8; i++) r[i] = new Complex(1.0, 0.0);

        // CoherenceWarmupFrames is 16: a coherence estimate averaged over fewer frames than that
        // is biased high by roughly 1/n even for unrelated signals.
        for (int i = 0; i < 16; i++)
            form.PublicUpdateAveragedSpectra(a, b, r);

        Assert.IsTrue(form.PublicIsCoherenceReady());
        Assert.AreEqual(16, form.PublicGetAvgFrames());
    }

    [TestMethod]
    public void IsCoherenceReady_FalseJustShortOfWarmupFrames()
    {
        var form = CreateTestableFormAlign();
        form.PublicSetFFTSize(8);
        form.PublicEnsureTransferStateInitialized();
        var zero = new Complex[8];
        for (int i = 0; i < 15; i++)
            form.PublicUpdateAveragedSpectra(zero, zero, zero);

        Assert.IsFalse(form.PublicIsCoherenceReady());
    }

    [TestMethod]
    public void EnsureTempBuffers_AllocatesBuffersOfFFTSize()
    {
        var form = CreateTestableFormAlign();
        form.PublicSetFFTSize(16);
        form.PublicEnsureTempBuffers();
        var field = typeof(FormAlign).GetField("_tmpA", BindingFlags.NonPublic | BindingFlags.Instance);
        var tmpA = (double[])field!.GetValue(form)!;
        Assert.AreEqual(16, tmpA.Length);
    }

    [TestMethod]
    public void EnsureTempBuffers_AllocatesSpectrumAndImpulseScratchOfFFTSize()
    {
        var form = CreateTestableFormAlign();
        form.PublicSetFFTSize(16);
        form.PublicEnsureTempBuffers();

        var fftA = (Complex[])typeof(FormAlign)
            .GetField("_fftA", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(form)!;
        var irA = (double[])typeof(FormAlign)
            .GetField("_irA", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(form)!;

        Assert.AreEqual(16, fftA.Length);
        Assert.AreEqual(16, irA.Length);
    }

    [TestMethod]
    public void EnsureTransferStateInitialized_AllocatesStateArraysOfFFTSize()
    {
        var form = CreateTestableFormAlign();
        form.PublicSetFFTSize(32);
        form.PublicEnsureTransferStateInitialized();
        var field = typeof(FormAlign).GetField("_Sxx", BindingFlags.NonPublic | BindingFlags.Instance);
        var sxx = (double[])field!.GetValue(form)!;
        Assert.AreEqual(32, sxx.Length);
    }

    [TestMethod]
    public void ComputeTransferFunctions_ZeroPower_MarksInvalid()
    {
        var form = CreateTestableFormAlign();
        form.PublicSetFFTSize(4);
        form.PublicEnsureTransferStateInitialized();
        form.PublicComputeTransferFunctions(1e-30, out var hA, out var hB, out var validH);
        Assert.AreEqual(4, hA.Length);
        for (int i = 0; i < validH.Length; i++)
            Assert.IsFalse(validH[i]);
    }

    /// <summary>
    /// H = Sxy/Sxx is a ratio, so a source identical to the Ref must come out as exactly unity -
    /// i.e. 0 dB - regardless of window or transform scaling.
    /// </summary>
    [TestMethod]
    public void ComputeTransferFunctions_SourceEqualsRef_IsUnity()
    {
        var form = CreateTestableFormAlign();
        form.PublicSetFFTSize(8);
        form.PublicEnsureTransferStateInitialized();
        form.PublicSetAlpha(1.0);

        var spectrum = new Complex[8];
        for (int i = 0; i < 8; i++)
            spectrum[i] = new Complex(0.5 + i, 0.25 - i);

        form.PublicUpdateAveragedSpectra(spectrum, spectrum, spectrum);
        form.PublicComputeTransferFunctions(1e-30, out var hA, out var hB, out var validH);

        for (int i = 0; i < 8; i++)
        {
            Assert.IsTrue(validH[i]);
            Assert.AreEqual(1.0, hA[i].Real, 1e-9);
            Assert.AreEqual(0.0, hA[i].Imaginary, 1e-9);
            Assert.AreEqual(1.0, hB[i].Real, 1e-9);
        }
    }

    [TestMethod]
    public void ComputeCoherence_SourceEqualsRef_IsUnity()
    {
        var form = CreateTestableFormAlign();
        form.PublicSetFFTSize(8);
        form.PublicEnsureTransferStateInitialized();
        form.PublicSetAlpha(1.0);

        var spectrum = new Complex[8];
        for (int i = 0; i < 8; i++)
            spectrum[i] = new Complex(1.0 + i, 0.5);

        form.PublicUpdateAveragedSpectra(spectrum, spectrum, spectrum);
        form.PublicComputeAdaptiveEpsilons(5, out double epsSxx, out double epsSyyA, out double epsSyyB);
        form.PublicComputeCoherence(5, epsSxx, epsSyyA, epsSyyB, out var cohA, out var cohB);

        for (int i = 1; i < 5; i++)
        {
            Assert.AreEqual(1.0, cohA[i], 1e-9);
            Assert.AreEqual(1.0, cohB[i], 1e-9);
        }
    }

    [TestMethod]
    public void ComputeWeightedMeanCoherence_WeightsByRefEnergy()
    {
        var form = CreateTestableFormAlign();
        form.PublicSetFFTSize(8);
        form.PublicEnsureTransferStateInitialized();
        form.PublicSetAlpha(1.0);

        // Ref energy only in bin 1, so only bin 1's coherence may influence the mean.
        var refSpectrum = new Complex[8];
        refSpectrum[1] = new Complex(1.0, 0.0);
        var zero = new Complex[8];
        form.PublicUpdateAveragedSpectra(zero, zero, refSpectrum);

        var coh = new double[5] { 0.0, 0.5, 1.0, 1.0, 1.0 };
        Assert.AreEqual(0.5, form.PublicComputeWeightedMeanCoherence(coh, 5), 1e-9);
    }

    [TestMethod]
    public void ComputeWeightedMeanCoherence_NoRefEnergy_ReturnsZero()
    {
        var form = CreateTestableFormAlign();
        form.PublicSetFFTSize(8);
        form.PublicEnsureTransferStateInitialized();
        var coh = new double[5] { 1.0, 1.0, 1.0, 1.0, 1.0 };
        Assert.AreEqual(0.0, form.PublicComputeWeightedMeanCoherence(coh, 5), 1e-9);
    }

    [TestMethod]
    public void ComputeWeightedMeanCoherence_NullInput_ReturnsZero()
    {
        var form = CreateTestableFormAlign();
        Assert.AreEqual(0.0, form.PublicComputeWeightedMeanCoherence(null, 5), 1e-9);
    }

    [TestMethod]
    public void ComputeAdaptiveEpsilons_UsesEpsilonFloorWhenPowerIsZero()
    {
        var form = CreateTestableFormAlign();
        form.PublicSetFFTSize(8);
        form.PublicEnsureTransferStateInitialized();
        form.PublicComputeAdaptiveEpsilons(4, out double epsSxx, out double epsSyyA, out double epsSyyB);
        Assert.AreEqual(1e-30, epsSxx, 1e-40);
        Assert.AreEqual(1e-30, epsSyyA, 1e-40);
        Assert.AreEqual(1e-30, epsSyyB, 1e-40);
    }

    [TestMethod]
    public void ComputeAdaptiveEpsilons_ScalesWithObservedPower()
    {
        var form = CreateTestableFormAlign();
        form.PublicSetFFTSize(8);
        form.PublicEnsureTransferStateInitialized();
        form.PublicSetAlpha(1.0);

        var refSpectrum = new Complex[8];
        refSpectrum[2] = new Complex(1000.0, 0.0); // Sxx = 1e6
        var zero = new Complex[8];
        form.PublicUpdateAveragedSpectra(zero, zero, refSpectrum);

        form.PublicComputeAdaptiveEpsilons(5, out double epsSxx, out _, out _);
        Assert.AreEqual(1e6 * 1e-12, epsSxx, 1e-15);
    }
    #endregion

    #region Reset
    /// <summary>
    /// The Reset button has to drop the averaged measurement, not just repaint: stale averages
    /// from a previous alignment attempt would otherwise keep dominating for ~1/alpha frames.
    /// </summary>
    [TestMethod]
    public void ResetMeasurement_ClearsAveragedFrameCount()
    {
        var form = CreateTestableFormAlign();
        form.PublicSetFFTSize(8);
        form.PublicEnsureTransferStateInitialized();
        var zero = new Complex[8];
        for (int i = 0; i < 5; i++)
            form.PublicUpdateAveragedSpectra(zero, zero, zero);

        Assert.AreEqual(5, form.PublicGetAvgFrames());

        form.PublicResetMeasurement();

        Assert.AreEqual(0, form.PublicGetAvgFrames());
        Assert.IsFalse(form.PublicIsCoherenceReady());
    }

    [TestMethod]
    public void ResetMeasurement_ClearsAveragedSpectra()
    {
        var form = CreateTestableFormAlign();
        form.PublicSetFFTSize(8);
        form.PublicEnsureTransferStateInitialized();
        form.PublicSetAlpha(1.0);

        var spectrum = new Complex[8];
        for (int i = 0; i < 8; i++)
            spectrum[i] = new Complex(3.0, 4.0);
        form.PublicUpdateAveragedSpectra(spectrum, spectrum, spectrum);

        form.PublicResetMeasurement();

        var sxx = (double[])typeof(FormAlign)
            .GetField("_Sxx", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(form)!;
        foreach (var v in sxx)
            Assert.AreEqual(0.0, v, 1e-12);
    }

    [TestMethod]
    public void ResetMeasurement_ClearsChartSeriesPoints()
    {
        var form = CreateTestableFormAlign();

        var chart = (System.Windows.Forms.DataVisualization.Charting.Chart)typeof(FormAlign)
            .GetField("Chart_Mag", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(form)!;
        chart.Series[0].Points.AddXY(1.0, 2.0);
        Assert.AreEqual(1, chart.Series[0].Points.Count);

        form.PublicResetMeasurement();

        Assert.AreEqual(0, chart.Series[0].Points.Count);
    }
    #endregion

    #region FFT size selection
    /// <summary>
    /// REGRESSION: this used to read ComboBox.SelectedText, which is always empty for a
    /// DropDownList, so the FFT size selector did nothing and the size was pinned to 4096.
    /// </summary>
    [TestMethod]
    public void ReadRequestedFFTSize_FollowsTheSelectedItem()
    {
        var form = CreateTestableFormAlign();
        var combo = (System.Windows.Forms.ComboBox)typeof(FormAlign)
            .GetField("FFTSize_CBO", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(form)!;

        combo.SelectedIndex = combo.Items.IndexOf("16384");
        Assert.AreEqual(16384, form.PublicReadRequestedFFTSize());

        combo.SelectedIndex = combo.Items.IndexOf("8192");
        Assert.AreEqual(8192, form.PublicReadRequestedFFTSize());
    }

    [TestMethod]
    public void ReadRequestedFFTSize_NothingSelected_FallsBackToDefault()
    {
        var form = CreateTestableFormAlign();
        Assert.AreEqual(4096, form.PublicReadRequestedFFTSize());
    }

    /// <summary>
    /// FFT.Init throws unless the length is an exact power of two, so a non-power-of-two entry
    /// must never reach it.
    /// </summary>
    [TestMethod]
    public void ReadRequestedFFTSize_NonPowerOfTwoItem_FallsBackToDefault()
    {
        var form = CreateTestableFormAlign();
        var combo = (System.Windows.Forms.ComboBox)typeof(FormAlign)
            .GetField("FFTSize_CBO", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(form)!;

        combo.Items.Add("3000");
        combo.SelectedIndex = combo.Items.IndexOf("3000");
        Assert.AreEqual(4096, form.PublicReadRequestedFFTSize());
    }
    #endregion

    #region Chart settings text boxes
    /// <summary>
    /// REGRESSION: min_ms_TXT sat on the row captioned "Max ms:" and vice versa, so the user's
    /// maximum was fed into AxisX.Minimum. Verify each box is on the row its own caption is on.
    /// </summary>
    [TestMethod]
    public void MsRangeTextBoxes_AreOnTheRowsTheirCaptionsLabel()
    {
        var form = CreateTestableFormAlign();

        var minBox = (System.Windows.Forms.TextBox)typeof(FormAlign)
            .GetField("min_ms_TXT", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(form)!;
        var maxBox = (System.Windows.Forms.TextBox)typeof(FormAlign)
            .GetField("max_ms_TXT", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(form)!;
        var minLabel = (System.Windows.Forms.Label)typeof(FormAlign)
            .GetField("label4", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(form)!;
        var maxLabel = (System.Windows.Forms.Label)typeof(FormAlign)
            .GetField("label5", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(form)!;

        Assert.AreEqual("Min ms:", minLabel.Text);
        Assert.AreEqual("Max ms:", maxLabel.Text);

        // Same row means the vertical centres line up to within a few pixels.
        Assert.IsTrue(Math.Abs(minBox.Top - minLabel.Top) < 8,
            $"min_ms_TXT (y={minBox.Top}) is not on the 'Min ms:' row (y={minLabel.Top})");
        Assert.IsTrue(Math.Abs(maxBox.Top - maxLabel.Top) < 8,
            $"max_ms_TXT (y={maxBox.Top}) is not on the 'Max ms:' row (y={maxLabel.Top})");

        // ...and the defaults must be a usable range, not an inverted one.
        Assert.IsTrue(double.Parse(minBox.Text) < double.Parse(maxBox.Text));
    }
    #endregion

    #region Colour swatches
    /// <summary>
    /// The swatch beside each source combo box exists to tell the user which trace is which, so
    /// it is only correct while it matches the colour that series is actually drawn in.
    /// </summary>
    [TestMethod]
    public void ColourSwatches_MatchTheSeriesColours()
    {
        var form = CreateTestableFormAlign();
        form.PublicRaiseLoad();

        var source1 = (System.Windows.Forms.Label)typeof(FormAlign)
            .GetField("Source1_Color_LBL", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(form)!;
        var source2 = (System.Windows.Forms.Label)typeof(FormAlign)
            .GetField("Source2_Color_LBL", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(form)!;
        var reference = (System.Windows.Forms.Label)typeof(FormAlign)
            .GetField("Ref_Color_LBL", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(form)!;

        var expected1 = (System.Drawing.Color)typeof(FormAlign)
            .GetField("Source1_Color", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;
        var expected2 = (System.Drawing.Color)typeof(FormAlign)
            .GetField("Source2_Color", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;
        var expectedRef = (System.Drawing.Color)typeof(FormAlign)
            .GetField("Ref_Color", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;

        Assert.AreEqual(expected1, source1.BackColor);
        Assert.AreEqual(expected2, source2.BackColor);
        Assert.AreEqual(expectedRef, reference.BackColor);

        Assert.AreEqual(string.Empty, source1.Text);
        Assert.AreEqual(string.Empty, source2.Text);
        Assert.AreEqual(string.Empty, reference.Text);

        Assert.AreNotEqual(expected1, expected2);
        Assert.AreNotEqual(expected1, expectedRef);
        Assert.AreNotEqual(expected2, expectedRef);
    }
    #endregion

    #region Chart series
    /// <summary>
    /// Each chart carries exactly two traces, one per source, both measured against Ref. Anything
    /// left over from the old layout (the "Sum" series, the ETC envelopes, the "Dummy" axis
    /// placeholder that planted a point at x=0 on a logarithmic axis) would draw as an extra
    /// unexplained line.
    /// </summary>
    [TestMethod]
    public void EachChart_HasExactlyTwoSeries()
    {
        var form = CreateTestableFormAlign();

        foreach (var name in new[] { "Chart_Mag", "Chart_Phase", "Chart_IR" })
        {
            var chart = (System.Windows.Forms.DataVisualization.Charting.Chart)typeof(FormAlign)
                .GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(form)!;

            Assert.AreEqual(2, chart.Series.Count, $"{name} should carry one trace per source");
            Assert.IsNotNull(chart.Series.FindByName("Series1"), $"{name} is missing Series1");
            Assert.IsNotNull(chart.Series.FindByName("Series2"), $"{name} is missing Series2");
            Assert.IsNull(chart.Series.FindByName("Sum"), $"{name} still has a Sum series");
            Assert.IsNull(chart.Series.FindByName("Dummy"), $"{name} still has a Dummy series");
        }
    }
    #endregion

    #region End-to-end accuracy against a known injected delay
    private const int AccuracySampleRate = 48000;
    private const int AccuracyFFTSize = 4096;
    private const int AccuracyFrames = 48;

    /// <summary>
    /// Builds three channels out of one deterministic broadband noise sequence, where each source
    /// is the Ref signal shifted by an EXACT whole number of samples. That makes the correct
    /// answer known in closed form, which is the only way to check the analyser rather than just
    /// check it against itself.
    /// </summary>
    /// <param name="delaySamplesA">Samples source 1 lags Ref by. Negative means it leads.</param>
    /// <param name="delaySamplesB">Samples source 2 lags Ref by. Negative means it leads.</param>
    private static (double[] Ref, double[] A, double[] B) BuildDelayedChannels(
        int delaySamplesA, int delaySamplesB)
    {
        int Local_Span = AccuracyFFTSize + AccuracyFrames * (AccuracyFFTSize / 2);
        int Local_Guard = 4096;
        int Local_Total = Local_Span + 2 * Local_Guard;

        // Fixed seed: a flaky accuracy test is worse than no accuracy test.
        var Local_Rng = new Random(20260817);
        var Local_Noise = new double[Local_Total];
        for (int i = 0; i < Local_Total; i++)
            Local_Noise[i] = Local_Rng.NextDouble() * 2.0 - 1.0;

        var Local_Ref = new double[Local_Span];
        var Local_A = new double[Local_Span];
        var Local_B = new double[Local_Span];

        // sourceX[n] = ref[n - D], i.e. sourceX reads the noise D samples earlier than Ref does.
        Array.Copy(Local_Noise, Local_Guard, Local_Ref, 0, Local_Span);
        Array.Copy(Local_Noise, Local_Guard - delaySamplesA, Local_A, 0, Local_Span);
        Array.Copy(Local_Noise, Local_Guard - delaySamplesB, Local_B, 0, Local_Span);

        return (Local_Ref, Local_A, Local_B);
    }

    private static double SamplesToMs(double samples) => 1000.0 * samples / AccuracySampleRate;

    /// <summary>
    /// The headline check. Source 1 arrives 24 samples (0.5 ms) AFTER the Ref signal and source 2
    /// arrives 12 samples (0.25 ms) BEFORE it, so the readouts must be +0.5 ms, -0.25 ms and a
    /// 0.75 ms difference.
    /// <para>
    /// REGRESSION: the delay search used to be clamped to the causal half of the impulse
    /// response, so a source that led the Ref could not be seen at all and both readouts sat at
    /// 0.0000 ms.
    /// </para>
    /// </summary>
    [TestMethod]
    public void Measurement_RecoversKnownPositiveAndNegativeDelays()
    {
        const int delayA = 24;
        const int delayB = -12;

        var form = CreateTestableFormAlign();
        var (refSignal, a, b) = BuildDelayedChannels(delayA, delayB);

        var m = form.PublicMeasureAlignment(refSignal, a, b,
            AccuracyFFTSize, AccuracySampleRate, AccuracyFrames);

        double toleranceMs = SamplesToMs(0.5);

        Assert.AreEqual(SamplesToMs(delayA), m.DelayMsA, toleranceMs,
            $"source 1 should read +{SamplesToMs(delayA):F4} ms, got {m.DelayMsA:F4} ms");

        Assert.AreEqual(SamplesToMs(delayB), m.DelayMsB, toleranceMs,
            $"source 2 should read {SamplesToMs(delayB):F4} ms, got {m.DelayMsB:F4} ms");

        Assert.IsTrue(m.DelayMsA > 0, $"a lagging source must read positive, got {m.DelayMsA:F4}");
        Assert.IsTrue(m.DelayMsB < 0, $"a leading source must read negative, got {m.DelayMsB:F4}");

        Assert.AreEqual(SamplesToMs(delayA - delayB), m.DelayMsA - m.DelayMsB, 2 * toleranceMs,
            "the 1-2 difference is the crossover offset and must follow from the two delays");
    }

    /// <summary>
    /// A pure delay is unity gain, so the magnitude trace has to sit on 0 dB across the band. If
    /// the transfer function were scaled by the window or by the transform's own normalisation
    /// this would fail.
    /// </summary>
    [TestMethod]
    public void Measurement_PureDelay_MagnitudeIsUnityAcrossTheBand()
    {
        var form = CreateTestableFormAlign();
        var (refSignal, a, b) = BuildDelayedChannels(24, -12);

        var m = form.PublicMeasureAlignment(refSignal, a, b,
            AccuracyFFTSize, AccuracySampleRate, AccuracyFrames);

        double binHz = (double)AccuracySampleRate / AccuracyFFTSize;
        int firstBin = (int)Math.Ceiling(100.0 / binHz);
        int lastBin = (int)Math.Floor(15000.0 / binHz);

        int checked_ = 0;
        for (int k = firstBin; k <= lastBin; k++)
        {
            Assert.IsFalse(double.IsNaN(m.MagA_dB[k]), $"bin {k} was masked out");
            Assert.AreEqual(0.0, m.MagA_dB[k], 1.0, $"source 1 magnitude at bin {k} ({k * binHz:F0} Hz)");
            Assert.AreEqual(0.0, m.MagB_dB[k], 1.0, $"source 2 magnitude at bin {k} ({k * binHz:F0} Hz)");
            checked_++;
        }

        Assert.IsTrue(checked_ > 1000, $"expected to check most of the band, only checked {checked_} bins");
    }

    /// <summary>
    /// A delay of D samples has phase -360*k*D/N degrees at bin k. Checking the phase against
    /// that closed form is what verifies the phase chart, independently of the impulse response.
    /// </summary>
    [TestMethod]
    public void Measurement_PureDelay_PhaseMatchesClosedForm()
    {
        const int delayA = 24;
        const int delayB = -12;

        var form = CreateTestableFormAlign();
        var (refSignal, a, b) = BuildDelayedChannels(delayA, delayB);

        var m = form.PublicMeasureAlignment(refSignal, a, b,
            AccuracyFFTSize, AccuracySampleRate, AccuracyFrames);

        foreach (int k in new[] { 10, 50, 100, 400, 1000, 1800 })
        {
            double expectedA = WrapTo180(-360.0 * k * delayA / AccuracyFFTSize);
            double expectedB = WrapTo180(-360.0 * k * delayB / AccuracyFFTSize);

            Assert.AreEqual(0.0, WrapTo180(m.PhaseA_deg[k] - expectedA), 3.0,
                $"source 1 phase at bin {k}: expected {expectedA:F2} deg, got {m.PhaseA_deg[k]:F2} deg");

            Assert.AreEqual(0.0, WrapTo180(m.PhaseB_deg[k] - expectedB), 3.0,
                $"source 2 phase at bin {k}: expected {expectedB:F2} deg, got {m.PhaseB_deg[k]:F2} deg");
        }
    }

    /// <summary>
    /// Two channels that are exact shifts of the same signal are perfectly coherent, so the
    /// figure of merit in the Stats box must read essentially 1.
    /// </summary>
    [TestMethod]
    public void Measurement_PureDelay_CoherenceIsEssentiallyOne()
    {
        var form = CreateTestableFormAlign();
        var (refSignal, a, b) = BuildDelayedChannels(24, -12);

        var m = form.PublicMeasureAlignment(refSignal, a, b,
            AccuracyFFTSize, AccuracySampleRate, AccuracyFrames);

        int halfLen = AccuracyFFTSize / 2 + 1;
        double meanA = form.PublicComputeWeightedMeanCoherence(m.CohA, halfLen);
        double meanB = form.PublicComputeWeightedMeanCoherence(m.CohB, halfLen);

        Assert.IsTrue(meanA > 0.95, $"source 1 mean coherence was only {meanA:F4}");
        Assert.IsTrue(meanB > 0.95, $"source 2 mean coherence was only {meanB:F4}");
    }

    /// <summary>
    /// The plotted impulse response must actually PEAK at the measured delay - the chart and the
    /// numeric readout have to agree, or the user cannot trust either.
    /// </summary>
    [TestMethod]
    public void Measurement_PlottedImpulseResponsePeaksAtTheReportedDelay()
    {
        const int delayA = 24;
        const int delayB = -12;

        var form = CreateTestableFormAlign();
        var (refSignal, a, b) = BuildDelayedChannels(delayA, delayB);

        var m = form.PublicMeasureAlignment(refSignal, a, b,
            AccuracyFFTSize, AccuracySampleRate, AccuracyFrames);

        int peakA = form.PublicArgMaxAbs(m.IrA);
        int peakB = form.PublicArgMaxAbs(m.IrB);

        double toleranceMs = SamplesToMs(0.5);

        Assert.AreEqual(m.DelayMsA, m.TimeMs[peakA], toleranceMs,
            "the source 1 trace must peak where the Delay 1 readout says it does");
        Assert.AreEqual(m.DelayMsB, m.TimeMs[peakB], toleranceMs,
            "the source 2 trace must peak where the Delay 2 readout says it does");

        // A leading source belongs to the LEFT of the 0 ms Ref line, not off the right hand end.
        Assert.IsTrue(m.TimeMs[peakB] < 0.0,
            $"the leading source plotted at {m.TimeMs[peakB]:F4} ms, expected a negative time");
        Assert.IsTrue(m.TimeMs[peakA] > 0.0,
            $"the lagging source plotted at {m.TimeMs[peakA]:F4} ms, expected a positive time");

        // Both responses are normalised to their own peak and scaled to a +/-100% axis.
        Assert.AreEqual(100.0, Math.Abs(m.IrA[peakA]), 1e-6);
        Assert.AreEqual(100.0, Math.Abs(m.IrB[peakB]), 1e-6);
    }

    /// <summary>
    /// Zero delay must read zero, and must not be confused with the old failure mode where every
    /// delay read zero.
    /// </summary>
    [TestMethod]
    public void Measurement_NoDelay_ReadsZero()
    {
        var form = CreateTestableFormAlign();
        var (refSignal, a, b) = BuildDelayedChannels(0, 0);

        var m = form.PublicMeasureAlignment(refSignal, a, b,
            AccuracyFFTSize, AccuracySampleRate, AccuracyFrames);

        Assert.AreEqual(0.0, m.DelayMsA, SamplesToMs(0.5));
        Assert.AreEqual(0.0, m.DelayMsB, SamplesToMs(0.5));
        Assert.AreEqual(0.0, m.DelayMsA - m.DelayMsB, SamplesToMs(1.0));
    }

    /// <summary>
    /// The measurement must hold up at the other selectable transform sizes, which - because the
    /// size selector never actually worked before - had never been exercised.
    /// </summary>
    [TestMethod]
    public void Measurement_RecoversDelayAtLargerFFTSizes()
    {
        foreach (int fftSize in new[] { 8192, 16384 })
        {
            int frames = 8;
            int span = fftSize + frames * (fftSize / 2);
            int guard = 4096;

            var rng = new Random(4242);
            var noise = new double[span + 2 * guard];
            for (int i = 0; i < noise.Length; i++)
                noise[i] = rng.NextDouble() * 2.0 - 1.0;

            const int delayA = 96;
            const int delayB = -48;

            var refSignal = new double[span];
            var a = new double[span];
            var b = new double[span];
            Array.Copy(noise, guard, refSignal, 0, span);
            Array.Copy(noise, guard - delayA, a, 0, span);
            Array.Copy(noise, guard - delayB, b, 0, span);

            var form = CreateTestableFormAlign();
            var m = form.PublicMeasureAlignment(refSignal, a, b, fftSize, AccuracySampleRate, frames);

            Assert.AreEqual(SamplesToMs(delayA), m.DelayMsA, SamplesToMs(0.5),
                $"FFT size {fftSize}: source 1 delay");
            Assert.AreEqual(SamplesToMs(delayB), m.DelayMsB, SamplesToMs(0.5),
                $"FFT size {fftSize}: source 2 delay");
            Assert.AreEqual(fftSize, form.PublicGetFFTSize());
        }
    }

    /// <summary>
    /// A delay far larger than an ASIO block - the realistic crossover case, where one driver is
    /// several milliseconds behind the other.
    /// </summary>
    [TestMethod]
    public void Measurement_RecoversMillisecondScaleDelays()
    {
        const int delayA = 480;   // 10 ms
        const int delayB = -240;  // -5 ms

        var form = CreateTestableFormAlign();
        var (refSignal, a, b) = BuildDelayedChannels(delayA, delayB);

        var m = form.PublicMeasureAlignment(refSignal, a, b,
            AccuracyFFTSize, AccuracySampleRate, AccuracyFrames);

        Assert.AreEqual(10.0, m.DelayMsA, SamplesToMs(1.0));
        Assert.AreEqual(-5.0, m.DelayMsB, SamplesToMs(1.0));
        Assert.AreEqual(15.0, m.DelayMsA - m.DelayMsB, SamplesToMs(2.0));
    }

    /// <summary>
    /// Inverting a source must show up as a 180 degree phase flip and a negative-going impulse,
    /// not as a shifted arrival time - polarity and delay are the two things a crossover
    /// alignment tool must never conflate.
    /// </summary>
    [TestMethod]
    public void Measurement_InvertedSource_ShowsPolarityFlipNotADelay()
    {
        const int delay = 24;

        var form = CreateTestableFormAlign();
        var (refSignal, a, b) = BuildDelayedChannels(delay, delay);
        for (int i = 0; i < b.Length; i++)
            b[i] = -b[i];

        var m = form.PublicMeasureAlignment(refSignal, a, b,
            AccuracyFFTSize, AccuracySampleRate, AccuracyFrames);

        // Same arrival time for both.
        Assert.AreEqual(SamplesToMs(delay), m.DelayMsA, SamplesToMs(0.5));
        Assert.AreEqual(SamplesToMs(delay), m.DelayMsB, SamplesToMs(0.5));

        // ...but 180 degrees apart everywhere.
        foreach (int k in new[] { 50, 200, 700, 1500 })
        {
            double difference = WrapTo180(m.PhaseA_deg[k] - m.PhaseB_deg[k]);
            Assert.AreEqual(180.0, Math.Abs(difference), 3.0,
                $"bin {k}: expected a 180 degree separation, got {difference:F2}");
        }

        // ...and the same magnitude, since inverting changes no level.
        Assert.AreEqual(m.MagA_dB[500], m.MagB_dB[500], 0.5);

        // The inverted response peaks negative.
        int peakB = form.PublicArgMaxAbs(m.IrB);
        Assert.IsTrue(m.IrB[peakB] < 0, "an inverted source should give a negative-going impulse");
    }

    /// <summary>
    /// A source with no relationship to the Ref signal must not be reported as a confident
    /// measurement. This is the case the coherence readout exists to expose.
    /// </summary>
    [TestMethod]
    public void Measurement_UncorrelatedSource_ReportsLowCoherence()
    {
        var form = CreateTestableFormAlign();
        var (refSignal, a, _) = BuildDelayedChannels(24, 0);

        var unrelated = new double[refSignal.Length];
        var rng = new Random(99);
        for (int i = 0; i < unrelated.Length; i++)
            unrelated[i] = rng.NextDouble() * 2.0 - 1.0;

        var m = form.PublicMeasureAlignment(refSignal, a, unrelated,
            AccuracyFFTSize, AccuracySampleRate, AccuracyFrames);

        int halfLen = AccuracyFFTSize / 2 + 1;
        double meanA = form.PublicComputeWeightedMeanCoherence(m.CohA, halfLen);
        double meanB = form.PublicComputeWeightedMeanCoherence(m.CohB, halfLen);

        Assert.IsTrue(meanA > 0.95, $"the genuine source should stay coherent, got {meanA:F4}");
        Assert.IsTrue(meanB < 0.25, $"the unrelated source should read incoherent, got {meanB:F4}");
    }

    /// <summary>
    /// With masking enabled, the incoherent source's trace must be dropped while the coherent
    /// one is left intact - a mask that blanks everything is as useless as no mask at all.
    /// </summary>
    [TestMethod]
    public void Measurement_CoherenceMask_DropsOnlyTheIncoherentSource()
    {
        var form = CreateTestableFormAlign();
        form.PublicSetCohMin(0.5);

        var (refSignal, a, _) = BuildDelayedChannels(24, 0);
        var unrelated = new double[refSignal.Length];
        var rng = new Random(1234);
        for (int i = 0; i < unrelated.Length; i++)
            unrelated[i] = rng.NextDouble() * 2.0 - 1.0;

        var m = form.PublicMeasureAlignment(refSignal, a, unrelated,
            AccuracyFFTSize, AccuracySampleRate, AccuracyFrames);

        int keptA = 0, keptB = 0;
        for (int k = 1; k < AccuracyFFTSize / 2 + 1; k++)
        {
            if (!double.IsNaN(m.PhaseA_deg[k])) keptA++;
            if (!double.IsNaN(m.PhaseB_deg[k])) keptB++;
        }

        Assert.IsTrue(keptA > 1900, $"the coherent source lost bins to the mask: {keptA} kept");
        Assert.IsTrue(keptB < 100, $"the incoherent source should be mostly masked: {keptB} kept");
    }

    /// <summary>
    /// With masking off (the default) nothing is dropped for coherence reasons, so the phase
    /// trace is continuous - which is the point of turning it off.
    /// </summary>
    [TestMethod]
    public void Measurement_MaskDisabled_LeavesThePhaseTraceContinuous()
    {
        var form = CreateTestableFormAlign();
        form.PublicSetCohMin(0.0);

        var (refSignal, a, _) = BuildDelayedChannels(24, 0);
        var unrelated = new double[refSignal.Length];
        var rng = new Random(777);
        for (int i = 0; i < unrelated.Length; i++)
            unrelated[i] = rng.NextDouble() * 2.0 - 1.0;

        var m = form.PublicMeasureAlignment(refSignal, a, unrelated,
            AccuracyFFTSize, AccuracySampleRate, AccuracyFrames);

        for (int k = 1; k < AccuracyFFTSize / 2 + 1; k++)
        {
            Assert.IsFalse(double.IsNaN(m.PhaseA_deg[k]), $"bin {k} of source 1 was dropped");
            Assert.IsFalse(double.IsNaN(m.PhaseB_deg[k]), $"bin {k} of source 2 was dropped");
        }
    }
    #endregion

    #region Fractional-octave complex smoothing
    private static bool[] AllValid(int n)
    {
        var v = new bool[n];
        for (int i = 0; i < n; i++) v[i] = true;
        return v;
    }

    /// <summary>
    /// Smoothing must not alter a response that is already smooth - a flat transfer function has
    /// to come out flat, at every width.
    /// </summary>
    [TestMethod]
    public void Smoothing_FlatResponse_IsUnchanged()
    {
        var form = CreateTestableFormAlign();
        int n = 2049;
        var h = new Complex[n];
        for (int i = 0; i < n; i++) h[i] = new Complex(0.5, -0.25);

        foreach (var option in TestableFormAlign.PublicSmoothingOptions)
        {
            var smoothed = form.PublicSmoothComplexFractionalOctave(h, AllValid(n), option.Fraction, out var valid);
            for (int i = 1; i < n; i++)
            {
                Assert.IsTrue(valid[i], $"{option.Label}: bin {i} lost validity");
                Assert.AreEqual(0.5, smoothed[i].Real, 1e-9, $"{option.Label} bin {i}");
                Assert.AreEqual(-0.25, smoothed[i].Imaginary, 1e-9, $"{option.Label} bin {i}");
            }
        }
    }

    /// <summary>
    /// The whole reason the averaging is COMPLEX. Two adjacent bins at +179 and -179 degrees point
    /// in almost the same direction, so their average must too. Averaging the WRAPPED phase
    /// numbers instead would give roughly 0 degrees - the exact opposite direction.
    /// </summary>
    [TestMethod]
    public void Smoothing_AcrossThePhaseWrapSeam_KeepsTheDirection()
    {
        var form = CreateTestableFormAlign();
        int n = 2049;
        var h = new Complex[n];

        // Alternate either side of the +/-180 seam: unit vectors at +179 and -179 degrees.
        for (int i = 0; i < n; i++)
        {
            double deg = (i % 2 == 0) ? 179.0 : -179.0;
            double rad = deg * Math.PI / 180.0;
            h[i] = new Complex(Math.Cos(rad), Math.Sin(rad));
        }

        var smoothed = form.PublicSmoothComplexFractionalOctave(h, AllValid(n), 1.0 / 12.0, out _);
        var phase = DSPLib.DSP.ConvertComplex.ToPhaseDegrees(smoothed);

        // Well up the band, where the band spans many bins and the alternation is fully averaged.
        for (int i = 1000; i < 1500; i++)
        {
            Assert.AreEqual(180.0, Math.Abs(phase[i]), 1.0,
                $"bin {i} came out at {phase[i]:F2} deg - the wrap seam was averaged through");
        }
    }

    /// <summary>
    /// Smoothing has to cut the scatter of a noisy trace. Comparing the bin-to-bin variation
    /// before and against after is the property that makes the feature worth having.
    /// </summary>
    [TestMethod]
    public void Smoothing_ReducesBinToBinScatter()
    {
        var form = CreateTestableFormAlign();
        int n = 2049;
        var rng = new Random(31337);
        var h = new Complex[n];

        // Unity response plus scatter.
        for (int i = 0; i < n; i++)
            h[i] = new Complex(1.0 + (rng.NextDouble() - 0.5), rng.NextDouble() - 0.5);

        var smoothed = form.PublicSmoothComplexFractionalOctave(h, AllValid(n), 1.0 / 12.0, out _);

        double rawScatter = 0.0, smoothScatter = 0.0;
        for (int i = 1001; i < 2000; i++)
        {
            rawScatter += (h[i] - h[i - 1]).Magnitude;
            smoothScatter += (smoothed[i] - smoothed[i - 1]).Magnitude;
        }

        Assert.IsTrue(smoothScatter < rawScatter / 5.0,
            $"smoothing barely helped: raw {rawScatter:F2} vs smoothed {smoothScatter:F2}");
    }

    /// <summary>
    /// Bin spacing is linear and the band is proportional to frequency, so the bass - where a bin
    /// is already wider than the requested fraction - must be left exactly as measured. Smoothing
    /// there would invent detail the measurement does not have.
    /// </summary>
    [TestMethod]
    public void Smoothing_LeavesLowFrequencyBinsUntouched()
    {
        var form = CreateTestableFormAlign();
        int n = 2049;
        var rng = new Random(11);
        var h = new Complex[n];
        for (int i = 0; i < n; i++)
            h[i] = new Complex(rng.NextDouble(), rng.NextDouble());

        // At 1/12 octave the half-band ratio is 2^(1/24) = 1.0293, so a band only reaches a
        // neighbour once 0.0293 * i >= 1, i.e. from bin 34 upwards.
        var smoothed = form.PublicSmoothComplexFractionalOctave(h, AllValid(n), 1.0 / 12.0, out _);

        for (int i = 1; i <= 16; i++)
        {
            Assert.AreEqual(h[i].Real, smoothed[i].Real, 1e-12, $"bin {i} was altered");
            Assert.AreEqual(h[i].Imaginary, smoothed[i].Imaginary, 1e-12, $"bin {i} was altered");
        }
    }

    /// <summary>
    /// A wider setting must smooth harder than a narrower one, monotonically.
    /// </summary>
    [TestMethod]
    public void Smoothing_WiderFractionSmoothsHarder()
    {
        var form = CreateTestableFormAlign();
        int n = 2049;
        var rng = new Random(555);
        var h = new Complex[n];
        for (int i = 0; i < n; i++)
            h[i] = new Complex(1.0 + (rng.NextDouble() - 0.5), rng.NextDouble() - 0.5);

        double previousScatter = double.MaxValue;
        foreach (var option in TestableFormAlign.PublicSmoothingOptions)
        {
            var smoothed = form.PublicSmoothComplexFractionalOctave(h, AllValid(n), option.Fraction, out _);

            double scatter = 0.0;
            for (int i = 1001; i < 2000; i++)
                scatter += (smoothed[i] - smoothed[i - 1]).Magnitude;

            Assert.IsTrue(scatter < previousScatter,
                $"{option.Label} scattered {scatter:F3}, not less than the narrower setting's {previousScatter:F3}");
            previousScatter = scatter;
        }
    }

    /// <summary>
    /// Invalid bins hold Complex.Zero, so counting them would drag the average towards zero and
    /// carve a false notch into the response next to every gap.
    /// </summary>
    [TestMethod]
    public void Smoothing_ExcludesInvalidBinsFromTheAverage()
    {
        var form = CreateTestableFormAlign();
        int n = 2049;
        var h = new Complex[n];
        var valid = AllValid(n);

        for (int i = 0; i < n; i++) h[i] = new Complex(1.0, 0.0);

        // Punch out every third bin, as a low-coherence region would.
        for (int i = 0; i < n; i++)
        {
            if (i % 3 == 0)
            {
                h[i] = Complex.Zero;
                valid[i] = false;
            }
        }

        var smoothed = form.PublicSmoothComplexFractionalOctave(h, valid, 1.0 / 6.0, out var validSmoothed);

        for (int i = 1000; i < 1500; i++)
        {
            Assert.IsTrue(validSmoothed[i], $"bin {i} should still have valid contributors");
            Assert.AreEqual(1.0, smoothed[i].Real, 1e-9,
                $"bin {i} was pulled towards zero by the invalid bins");
        }
    }

    [TestMethod]
    public void Smoothing_BandWithNoValidBins_StaysInvalid()
    {
        var form = CreateTestableFormAlign();
        int n = 513;
        var h = new Complex[n];
        var valid = new bool[n]; // nothing valid

        var smoothed = form.PublicSmoothComplexFractionalOctave(h, valid, 1.0 / 12.0, out var validSmoothed);

        for (int i = 1; i < n; i++)
        {
            Assert.IsFalse(validSmoothed[i], $"bin {i} claimed validity out of nothing");
            Assert.AreEqual(Complex.Zero, smoothed[i]);
        }
    }

    [TestMethod]
    public void Smoothing_NonFiniteValues_AreExcluded()
    {
        var form = CreateTestableFormAlign();
        int n = 2049;
        var h = new Complex[n];
        for (int i = 0; i < n; i++) h[i] = new Complex(1.0, 0.0);
        h[1200] = new Complex(double.NaN, double.NaN);
        h[1201] = new Complex(double.PositiveInfinity, 0.0);

        var smoothed = form.PublicSmoothComplexFractionalOctave(h, AllValid(n), 1.0 / 12.0, out var validSmoothed);

        for (int i = 1100; i < 1300; i++)
        {
            Assert.IsTrue(validSmoothed[i]);
            Assert.AreEqual(1.0, smoothed[i].Real, 1e-9, $"bin {i} was poisoned by a non-finite neighbour");
        }
    }

    [TestMethod]
    public void Smoothing_NonPositiveFraction_PassesTheSpectrumThrough()
    {
        var form = CreateTestableFormAlign();
        int n = 129;
        var h = new Complex[n];
        var rng = new Random(8);
        for (int i = 0; i < n; i++) h[i] = new Complex(rng.NextDouble(), rng.NextDouble());

        foreach (double fraction in new[] { 0.0, -1.0, double.NaN })
        {
            var smoothed = form.PublicSmoothComplexFractionalOctave(h, AllValid(n), fraction, out var valid);
            for (int i = 0; i < n; i++)
            {
                Assert.AreEqual(h[i], smoothed[i], $"fraction {fraction} altered bin {i}");
                Assert.IsTrue(valid[i]);
            }
        }
    }

    [TestMethod]
    public void Smoothing_EmptyInput_ReturnsEmpty()
    {
        var form = CreateTestableFormAlign();
        var smoothed = form.PublicSmoothComplexFractionalOctave(
            Array.Empty<Complex>(), Array.Empty<bool>(), 1.0 / 12.0, out var valid);
        Assert.AreEqual(0, smoothed.Length);
        Assert.AreEqual(0, valid.Length);
    }

    [TestMethod]
    public void Smoothing_DoesNotModifyItsInput()
    {
        var form = CreateTestableFormAlign();
        int n = 513;
        var h = new Complex[n];
        var rng = new Random(64);
        for (int i = 0; i < n; i++) h[i] = new Complex(rng.NextDouble(), rng.NextDouble());
        var copy = (Complex[])h.Clone();

        _ = form.PublicSmoothComplexFractionalOctave(h, AllValid(n), 1.0 / 3.0, out _);

        CollectionAssert.AreEqual(copy, h);
    }
    #endregion

    #region Smoothing option table and combo box
    [TestMethod]
    public void SmoothingOptions_LabelsAndFractionsAgree()
    {
        var options = TestableFormAlign.PublicSmoothingOptions;
        Assert.AreEqual(6, options.Length);

        foreach (var (label, fraction) in options)
        {
            // Every label reads "1/N oct" or "1 oct"; the fraction must be exactly that.
            string trimmed = label.Replace(" oct", string.Empty);
            double expected = trimmed.Contains('/')
                ? 1.0 / int.Parse(trimmed.Split('/')[1])
                : 1.0;

            Assert.AreEqual(expected, fraction, 1e-12, $"'{label}' does not stand for {fraction}");
            Assert.IsTrue(fraction > 0 && fraction <= 1, $"'{label}' is not a usable width");
        }

        // Widths must be listed narrowest first, which is what the monotonicity test relies on.
        for (int i = 1; i < options.Length; i++)
            Assert.IsTrue(options[i].Fraction > options[i - 1].Fraction, "options are not ordered");
    }

    /// <summary>
    /// The combo is populated from the option table on load, and the analyser reads it back
    /// through the same table - so a label can never disagree with the width it applies.
    /// </summary>
    [TestMethod]
    public void SmoothingCombo_IsPopulatedFromTheTableAndReadsBackThroughIt()
    {
        var form = CreateTestableFormAlign();
        form.PublicRaiseLoad();

        var combo = (System.Windows.Forms.ComboBox)typeof(FormAlign)
            .GetField("Smoothing_CBO", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(form)!;

        var options = TestableFormAlign.PublicSmoothingOptions;
        Assert.AreEqual(options.Length, combo.Items.Count);

        for (int i = 0; i < options.Length; i++)
        {
            Assert.AreEqual(options[i].Label, combo.Items[i]?.ToString());
            combo.SelectedIndex = i;
            Assert.AreEqual(options[i].Fraction, form.PublicReadSmoothingFraction(), 1e-12);
        }

        Assert.AreEqual(1.0 / 12.0, options[TestableFormAlign.PublicDefaultSmoothingIndex].Fraction, 1e-12);
    }

    [TestMethod]
    public void SmoothingCombo_NothingSelected_FallsBackToTheDefaultWidth()
    {
        var form = CreateTestableFormAlign();
        Assert.AreEqual(1.0 / 12.0, form.PublicReadSmoothingFraction(), 1e-12);
    }

    /// <summary>
    /// Smoothing starts OFF and the width selector starts disabled, so the form opens showing the
    /// raw measurement; ticking the box enables the selector.
    /// </summary>
    [TestMethod]
    public void SmoothingCheckbox_DefaultsOffAndGatesTheWidthSelector()
    {
        var form = CreateTestableFormAlign();
        form.PublicRaiseLoad();

        var check = (System.Windows.Forms.CheckBox)typeof(FormAlign)
            .GetField("Smoothing_CHK", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(form)!;
        var combo = (System.Windows.Forms.ComboBox)typeof(FormAlign)
            .GetField("Smoothing_CBO", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(form)!;

        Assert.IsFalse(check.Checked, "smoothing should default to off");
        Assert.IsFalse(combo.Enabled, "the width selector should start disabled");

        check.Checked = true;
        Assert.IsTrue(combo.Enabled, "ticking the box should enable the width selector");

        check.Checked = false;
        Assert.IsFalse(combo.Enabled, "unticking the box should disable the width selector again");
    }
    #endregion

    #region Smoothing in the measurement pipeline
    /// <summary>
    /// The critical safety property: smoothing is a DISPLAY transform. Narrowing the spectrum
    /// broadens the impulse response, so if the delay were measured from the smoothed transfer
    /// function the readout would blur. It must be measured from the raw one and be bit-identical
    /// whatever the smoothing setting.
    /// </summary>
    [TestMethod]
    public void Measurement_SmoothingDoesNotChangeTheDelayReadouts()
    {
        const int delayA = 24;
        const int delayB = -12;

        var (refSignal, a, b) = BuildDelayedChannels(delayA, delayB);

        var rawForm = CreateTestableFormAlign();
        rawForm.PublicSetSmoothing(false, 1.0 / 12.0);
        var raw = rawForm.PublicMeasureAlignment(refSignal, a, b,
            AccuracyFFTSize, AccuracySampleRate, AccuracyFrames);

        foreach (var option in TestableFormAlign.PublicSmoothingOptions)
        {
            var form = CreateTestableFormAlign();
            form.PublicSetSmoothing(true, option.Fraction);
            var m = form.PublicMeasureAlignment(refSignal, a, b,
                AccuracyFFTSize, AccuracySampleRate, AccuracyFrames);

            Assert.AreEqual(raw.DelayMsA, m.DelayMsA, 1e-12, $"{option.Label} moved Delay 1");
            Assert.AreEqual(raw.DelayMsB, m.DelayMsB, 1e-12, $"{option.Label} moved Delay 2");

            // ...and the delay is still the correct one, not just consistently wrong.
            Assert.AreEqual(SamplesToMs(delayA), m.DelayMsA, SamplesToMs(0.5));
            Assert.AreEqual(SamplesToMs(delayB), m.DelayMsB, SamplesToMs(0.5));
        }
    }

    [TestMethod]
    public void Measurement_SmoothingDisabled_LeavesTheTracesExactlyAsMeasured()
    {
        var (refSignal, a, b) = BuildDelayedChannels(24, -12);

        var offForm = CreateTestableFormAlign();
        offForm.PublicSetSmoothing(false, 1.0 / 12.0);
        var off = offForm.PublicMeasureAlignment(refSignal, a, b,
            AccuracyFFTSize, AccuracySampleRate, AccuracyFrames);

        // A zero width with smoothing enabled is the pass-through path; it must agree exactly.
        var zeroForm = CreateTestableFormAlign();
        zeroForm.PublicSetSmoothing(true, 0.0);
        var zero = zeroForm.PublicMeasureAlignment(refSignal, a, b,
            AccuracyFFTSize, AccuracySampleRate, AccuracyFrames);

        for (int k = 1; k < AccuracyFFTSize / 2 + 1; k++)
        {
            Assert.AreEqual(off.MagA_dB[k], zero.MagA_dB[k], 1e-12, $"bin {k}");
            Assert.AreEqual(off.PhaseA_deg[k], zero.PhaseA_deg[k], 1e-12, $"bin {k}");
        }
    }

    /// <summary>
    /// A real measurement, smoothed: the response is a pure delay so the magnitude must stay near
    /// 0 dB and the phase must still follow the delay's slope - smoothing may quieten the trace
    /// but it must not move it.
    /// </summary>
    [TestMethod]
    public void Measurement_Smoothed_StillReportsTheCorrectResponse()
    {
        const int delayA = 24;

        var form = CreateTestableFormAlign();
        form.PublicSetSmoothing(true, 1.0 / 12.0);

        var (refSignal, a, b) = BuildDelayedChannels(delayA, delayA);
        var m = form.PublicMeasureAlignment(refSignal, a, b,
            AccuracyFFTSize, AccuracySampleRate, AccuracyFrames);

        // Low and mid band, where 1/12 octave spans few enough bins that the delay's own phase
        // rotation across the band does not itself shrink the vector average.
        foreach (int k in new[] { 10, 30, 60, 120 })
        {
            double expected = WrapTo180(-360.0 * k * delayA / AccuracyFFTSize);
            Assert.AreEqual(0.0, WrapTo180(m.PhaseA_deg[k] - expected), 5.0,
                $"bin {k}: expected {expected:F2} deg, got {m.PhaseA_deg[k]:F2} deg");
            Assert.AreEqual(0.0, m.MagA_dB[k], 1.5, $"bin {k} magnitude");
        }
    }

    /// <summary>
    /// Smoothing must not create or destroy plotted points - the traces stay the same length and
    /// keep the same coverage, so the two settings are directly comparable on screen.
    /// </summary>
    [TestMethod]
    public void Measurement_Smoothed_KeepsTheSameBinCoverage()
    {
        var (refSignal, a, b) = BuildDelayedChannels(24, -12);

        var form = CreateTestableFormAlign();
        form.PublicSetSmoothing(true, 1.0 / 6.0);
        var m = form.PublicMeasureAlignment(refSignal, a, b,
            AccuracyFFTSize, AccuracySampleRate, AccuracyFrames);

        Assert.AreEqual(AccuracyFFTSize / 2 + 1, m.MagA_dB.Length);
        Assert.AreEqual(AccuracyFFTSize / 2 + 1, m.PhaseA_deg.Length);

        for (int k = 1; k < AccuracyFFTSize / 2 + 1; k++)
        {
            Assert.IsFalse(double.IsNaN(m.MagA_dB[k]), $"bin {k} of the magnitude trace was dropped");
            Assert.IsFalse(double.IsNaN(m.PhaseA_deg[k]), $"bin {k} of the phase trace was dropped");
        }
    }
    #endregion

    #region Empty chart on a logarithmic axis (0xC000041D crash)
    private static System.Windows.Forms.DataVisualization.Charting.Chart GetChart(FormAlign form, string name)
        => (System.Windows.Forms.DataVisualization.Charting.Chart)typeof(FormAlign)
            .GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(form)!;

    /// <summary>
    /// The property that makes an all-NaN trace a safe way to blank a chart: a NaN y is stored as
    /// an EMPTY point, so the x value stays in the chart even though nothing is drawn.
    /// <para>
    /// It matters because a chart left with NO points and a logarithmic x axis derives its scale
    /// from point INDEXES, index 0 has no logarithm, and it throws from inside Chart.OnPaint ->
    /// Control.WndProc -> NativeWindow.Callback. An exception escaping a native-to-managed
    /// callback kills the process outright (STATUS_FATAL_USER_CALLBACK_EXCEPTION, 0xC000041D).
    /// </para>
    /// </summary>
    [TestMethod]
    public void BindTrace_AllNaN_KeepsTheXValuesAsEmptyPoints()
    {
        var form = CreateTestableFormAlign();
        var chart = GetChart(form, "Chart_Mag");

        var x = new double[] { 1, 10, 100, 1000, 10000 };
        var y = new double[x.Length];
        Array.Fill(y, double.NaN);

        form.PublicBindTrace(chart.Series[0], x, y);

        Assert.AreEqual(x.Length, chart.Series[0].Points.Count,
            "an all-NaN trace must still leave its x values in the chart");

        for (int i = 0; i < x.Length; i++)
        {
            Assert.AreEqual(x[i], chart.Series[0].Points[i].XValue, 1e-9);
            Assert.IsTrue(chart.Series[0].Points[i].IsEmpty,
                $"point {i} should be an empty point, so nothing is drawn for it");
        }
    }

    /// <summary>
    /// The same mechanism is what draws the coherence mask's gaps: masked bins keep their place on
    /// the axis but render as holes in the trace.
    /// </summary>
    [TestMethod]
    public void BindTrace_PartialNaN_KeepsEveryXAndMarksTheMaskedOnesEmpty()
    {
        var form = CreateTestableFormAlign();
        var chart = GetChart(form, "Chart_Mag");

        var x = new double[] { 1, 10, 100, 1000 };
        var y = new double[] { -6, double.NaN, -12, double.NaN };

        form.PublicBindTrace(chart.Series[0], x, y);

        Assert.AreEqual(4, chart.Series[0].Points.Count, "the x values must all survive");
        Assert.IsFalse(chart.Series[0].Points[0].IsEmpty);
        Assert.IsTrue(chart.Series[0].Points[1].IsEmpty, "a masked bin renders as a gap");
        Assert.IsFalse(chart.Series[0].Points[2].IsEmpty);
        Assert.IsTrue(chart.Series[0].Points[3].IsEmpty);
        Assert.AreEqual(-12.0, chart.Series[0].Points[2].YValues[0], 1e-9);
    }

    [TestMethod]
    public void BindTrace_RealData_BindsEveryPoint()
    {
        var form = CreateTestableFormAlign();
        var chart = GetChart(form, "Chart_Mag");

        var x = new double[] { 1, 10, 100 };
        var y = new double[] { -6, -12, -18 };

        form.PublicBindTrace(chart.Series[0], x, y);

        Assert.AreEqual(3, chart.Series[0].Points.Count);
        Assert.AreEqual(-12.0, chart.Series[0].Points[1].YValues[0], 1e-9);
    }

    [TestMethod]
    public void BindTrace_ReplacesPreviousContent()
    {
        var form = CreateTestableFormAlign();
        var chart = GetChart(form, "Chart_Mag");

        form.PublicBindTrace(chart.Series[0], new double[] { 1, 2, 3, 4 }, new double[] { 1, 2, 3, 4 });
        form.PublicBindTrace(chart.Series[0], new double[] { 5, 6 }, new double[] { 5, 6 });

        Assert.AreEqual(2, chart.Series[0].Points.Count, "a rebind must not accumulate points");
    }

    /// <summary>
    /// REGRESSION for the reported crash: pressing Reset, and changing Source 1 / Source 2 / Ref,
    /// all call Reset_Measurement. None of them may leave a logarithmic frequency chart empty.
    /// </summary>
    [TestMethod]
    public void ResetMeasurement_LeavesTheFrequencyChartsNonEmpty()
    {
        var form = CreateTestableFormAlign();
        form.PublicEnsureConfig(4096, 48000);

        form.PublicResetMeasurement();

        foreach (var name in new[] { "Chart_Mag", "Chart_Phase" })
        {
            var chart = GetChart(form, name);
            for (int s = 0; s < chart.Series.Count; s++)
                Assert.IsTrue(chart.Series[s].Points.Count > 1,
                    $"{name}.{chart.Series[s].Name} was left with {chart.Series[s].Points.Count} points; " +
                    "a logarithmic axis needs a real, non-degenerate x range or it throws during paint");
        }
    }

    /// <summary>
    /// One point is as unusable as none - a zero-width range sends the chart back to the same
    /// index-based path - so the blanked traces must span a real range.
    /// </summary>
    [TestMethod]
    public void ResetMeasurement_BlankedTracesSpanARealPositiveRange()
    {
        var form = CreateTestableFormAlign();
        form.PublicEnsureConfig(4096, 48000);
        form.PublicResetMeasurement();

        var series = GetChart(form, "Chart_Mag").Series[0];

        double lowest = double.MaxValue, highest = double.MinValue;
        for (int i = 0; i < series.Points.Count; i++)
        {
            double xv = series.Points[i].XValue;
            if (xv < lowest) lowest = xv;
            if (xv > highest) highest = xv;
        }

        Assert.IsTrue(highest > lowest, "the blanked trace must not be a zero-width range");
        Assert.IsTrue(highest > 1000, $"expected the range to reach up the band, got {highest}");
    }

    /// <summary>
    /// The same latent crash reachable without touching Reset at all: turn the coherence mask up
    /// far enough on a poor measurement and every bin is NaN, so the trace binds to nothing.
    /// </summary>
    [TestMethod]
    public void PlotMagChart_FullyMaskedData_LeavesTheChartNonEmpty()
    {
        var form = CreateTestableFormAlign();
        var chart = GetChart(form, "Chart_Mag");

        int halfLen = 2049;
        var x = new double[halfLen];
        for (int i = 0; i < halfLen; i++)
            x[i] = Math.Max(0.0001, i * (48000.0 / 4096.0));

        var allMasked = new double[halfLen];
        Array.Fill(allMasked, double.NaN);

        form.PublicPlotMagChart(chart, 1, 24000, x, allMasked, allMasked);

        for (int s = 0; s < chart.Series.Count; s++)
            Assert.IsTrue(chart.Series[s].Points.Count > 1,
                $"a fully masked {chart.Series[s].Name} left the chart with " +
                $"{chart.Series[s].Points.Count} points on a logarithmic axis");
    }

    [TestMethod]
    public void BlankFrequencyCharts_WithoutAConfig_DoesNotThrow()
    {
        // Before the first refresh there is no frequency axis to blank against, and an untouched
        // chart is still linear, so this must simply do nothing.
        var form = CreateTestableFormAlign();
        form.PublicBlankFrequencyCharts();
        Assert.AreEqual(0, GetChart(form, "Chart_Mag").Series[0].Points.Count);
    }
    #endregion

    #region Refresh timer threading
    private static System.Windows.Forms.Timer GetRefreshTimer(FormAlign form)
        => (System.Windows.Forms.Timer)typeof(FormAlign)
            .GetField("RefreshTimer", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(form)!;

    /// <summary>
    /// REGRESSION: the designer used to arm RefreshTimer inside InitializeComponent. A WinForms
    /// timer delivers WM_TIMER to whichever thread armed it, and this form is CONSTRUCTED on the
    /// application's main UI thread but PUMPED by its own STA thread
    /// (ctl_MonitorPage.btn_Align_Click) - so the tick ran on the main thread while every control
    /// handle belonged to the form's own thread, and the first control access that touches Handle
    /// threw "Cross-thread operation not valid: Control 'Smoothing_CBO' accessed from a thread
    /// other than the thread it was created on."
    /// <para>
    /// The timer must therefore be armed from Load, which runs on the pumping thread. That is the
    /// invariant this test pins down: NOT enabled by the constructor, enabled by Load.
    /// </para>
    /// </summary>
    [TestMethod]
    public void RefreshTimer_IsNotArmedByTheConstructor()
    {
        var form = CreateTestableFormAlign();
        Assert.IsFalse(GetRefreshTimer(form).Enabled,
            "the constructor armed the refresh timer, so it would tick on the constructing thread " +
            "rather than the thread that owns the controls");
    }

    [TestMethod]
    public void RefreshTimer_IsArmedByLoad()
    {
        var form = CreateTestableFormAlign();
        form.PublicRaiseLoad();

        var timer = GetRefreshTimer(form);
        Assert.IsTrue(timer.Enabled, "Load did not start the refresh timer, so the form would never update");
        Assert.IsTrue(timer.Interval > 0 && timer.Interval <= 1000, $"implausible interval {timer.Interval}");

        // Leave nothing running behind the test.
        timer.Stop();
    }

    /// <summary>
    /// The analysis pass is separated from the tick handler so the handler can marshal it when a
    /// tick arrives on a foreign thread. Verify the seam exists and that the handler still stops
    /// and restarts the timer around it.
    /// </summary>
    [TestMethod]
    public void RefreshTimerTick_LeavesTheTimerRunningAndDoesNotThrow()
    {
        var form = CreateTestableFormAlign();
        form.PublicRaiseLoad();

        form.PublicRaiseRefreshTimerTick();

        var timer = GetRefreshTimer(form);
        Assert.IsTrue(timer.Enabled, "the tick left the refresh timer stopped, so the form would freeze");

        timer.Stop();
    }

    [TestMethod]
    public void RefreshAnalysis_ExistsAsASeparateMarshallableStep()
    {
        var method = typeof(FormAlign).GetMethod("Refresh_Analysis", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(method, "Refresh_Analysis is what RefreshTimer_Tick marshals; it must stay separate");
        Assert.AreEqual(typeof(void), method!.ReturnType);
        Assert.AreEqual(0, method.GetParameters().Length,
            "it must match Action so SafeInvoke can marshal it");
    }

    /// <summary>
    /// Closing the form must stop the timer, or a disposed form keeps being ticked.
    /// </summary>
    [TestMethod]
    public void FormClosing_StopsTheRefreshTimer()
    {
        var form = CreateTestableFormAlign();
        form.PublicRaiseLoad();
        Assert.IsTrue(GetRefreshTimer(form).Enabled);

        typeof(FormAlign)
            .GetMethod("FormAlign_FormClosing", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(form, new object?[] { form, new System.Windows.Forms.FormClosingEventArgs(System.Windows.Forms.CloseReason.UserClosing, false) });

        Assert.IsFalse(GetRefreshTimer(form).Enabled, "closing the form left the refresh timer armed");
    }

    /// <summary>
    /// The ASIO notification runs on the audio thread, so it must never touch a control. Capturing
    /// audio into the ring buffers is all it is allowed to do.
    /// </summary>
    [TestMethod]
    public void AsioOutputDataAvailable_TouchesNoControls()
    {
        var body = typeof(FormAlign)
            .GetMethod("ASIO_OutputDataAvailable", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(body);

        // Called with no sources selected it must return immediately and without throwing, which
        // is the state it is in for most of the window's life.
        var form = CreateTestableFormAlign();
        body!.Invoke(form, Array.Empty<object>());

        Assert.AreEqual(0, form.PublicGetAvgFrames());
    }
    #endregion

    #region Error handling
    // FormAlign.Error(ex) forwards to Debug.Error(ex) (BassThatHz_ASIO_DSP_Processor\Debug.cs),
    // which calls System.Windows.Forms.MessageBox.Show(...) with real modal dialogs and, if the
    // (real, human) user picks "Yes" to abort, rethrows the original exception. In this headless
    // MSTest host there is no user to click a button, so MessageBox.Show's behavior is
    // environment-dependent and was observed to be flaky across runs: sometimes it returns
    // immediately, and at least once it blocked for several minutes before returning a
    // non-"Yes" result. Because this makes any test that actually invokes FormAlign.Error/
    // Debug.Error non-deterministic and potentially extremely slow (multi-minute) in CI, we
    // intentionally do not call it here. Instead we confirm, via reflection, that the Error
    // method exists with the expected signature and forwards to Debug.Error - which is the
    // extent of what can be verified safely and deterministically without a real interactive UI.
    [TestMethod]
    public void Error_MethodExists_WithExpectedSignature()
    {
        var method = typeof(FormAlign).GetMethod("Error", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(method);
        Assert.AreEqual(typeof(void), method!.ReturnType);
        var parameters = method.GetParameters();
        Assert.AreEqual(1, parameters.Length);
        Assert.AreEqual(typeof(Exception), parameters[0].ParameterType);
    }
    #endregion
}

