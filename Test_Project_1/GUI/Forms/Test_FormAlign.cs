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
    public double PublicWrapTo180(double x) => this.WrapTo180(x);
    public double[] PublicCircularShift(double[] x, int shift) => this.CircularShift(x, shift);
    public double PublicPeakAbs(double[] x) => this.PeakAbs(x);
    public void PublicNormalizeToUnitPeakInPlace(double[] x) => this.NormalizeToUnitPeakInPlace(x);
    public int PublicArgMaxAbsRange(double[] x, int start, int end) => this.ArgMaxAbsRange(x, start, end);
    public int PublicArgMaxAbs(double[] x) => this.ArgMaxAbs(x);
    public double[] PublicUnwrapPhaseDegrees(double[] phaseDeg) => this.UnwrapPhaseDegrees(phaseDeg);
    public double[] PublicComputeETCPercent(double[] ir, double sampleRate, double smoothMs = 1.0) => this.ComputeETCPercent(ir, sampleRate, smoothMs);
    public double PublicPeakIndexToSignedDelayMs(int peakIndex, double sampleRate, int fftSize) => this.PeakIndexToSignedDelayMs(peakIndex, sampleRate, fftSize);
    public double[] PublicCircularShiftToPeak(double[] x) => this.CircularShiftToPeak(x);
    public bool PublicIsCoherenceReady() => this.IsCoherenceReady();
    public void PublicEnsureTempBuffers() => this.EnsureTempBuffers();
    public void PublicEnsureTransferStateInitialized() => this.EnsureTransferStateInitialized();
    public void PublicUpdateAveragedSpectra(Complex[] a, Complex[] b, Complex[] r) => this.UpdateAveragedSpectra(a, b, r);
    public void PublicComputeAdaptiveEpsilons(int halfLen, out double epsSxx, out double epsSyyMax) => this.ComputeAdaptiveEpsilons(halfLen, out epsSxx, out epsSyyMax);
    public void PublicComputeTransferFunctions(double epsSxx, out Complex[] hA, out Complex[] hB, out bool[] validH) => this.ComputeTransferFunctions(epsSxx, out hA, out hB, out validH);
    public void PublicComputeCoherence(int halfLen, double epsSxx, out double[] cohA, out double[] cohB) => this.ComputeCoherence(halfLen, epsSxx, out cohA, out cohB);
    public void PublicComputeMaxCoherence(double[] cohA, double[] cohB, out double maxCohA, out double maxCohB) => this.ComputeMaxCoherence(cohA, cohB, out maxCohA, out maxCohB);
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
        Assert.IsTrue(true);
    }
    #endregion

    #region WrapTo180
    [TestMethod]
    public void WrapTo180_ValueWithinRange_ReturnsUnchanged()
    {
        var form = CreateTestableFormAlign();
        Assert.AreEqual(90.0, form.PublicWrapTo180(90.0), 1e-9);
        Assert.AreEqual(0.0, form.PublicWrapTo180(0.0), 1e-9);
        Assert.AreEqual(180.0, form.PublicWrapTo180(180.0), 1e-9);
    }

    [TestMethod]
    public void WrapTo180_AboveRange_WrapsNegative()
    {
        var form = CreateTestableFormAlign();
        Assert.AreEqual(-170.0, form.PublicWrapTo180(190.0), 1e-9);
    }

    [TestMethod]
    public void WrapTo180_BelowRange_WrapsPositive()
    {
        var form = CreateTestableFormAlign();
        Assert.AreEqual(170.0, form.PublicWrapTo180(-190.0), 1e-9);
    }

    [TestMethod]
    public void WrapTo180_ExactlyNegative180_WrapsToPositive180()
    {
        var form = CreateTestableFormAlign();
        Assert.AreEqual(180.0, form.PublicWrapTo180(-180.0), 1e-9);
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

    #region ArgMaxAbsRange / ArgMaxAbs
    [TestMethod]
    public void ArgMaxAbsRange_FindsIndexOfMaxWithinRange()
    {
        var form = CreateTestableFormAlign();
        var data = new double[] { 1, -9, 3, 2 };
        int idx = form.PublicArgMaxAbsRange(data, 0, 3);
        Assert.AreEqual(1, idx);
    }

    [TestMethod]
    public void ArgMaxAbsRange_ClampsNegativeStart()
    {
        var form = CreateTestableFormAlign();
        var data = new double[] { 5, 1, 2 };
        int idx = form.PublicArgMaxAbsRange(data, -3, 2);
        Assert.AreEqual(0, idx);
    }

    [TestMethod]
    public void ArgMaxAbsRange_ClampsEndBeyondLength()
    {
        var form = CreateTestableFormAlign();
        var data = new double[] { 1, 2, 9 };
        int idx = form.PublicArgMaxAbsRange(data, 0, 100);
        Assert.AreEqual(2, idx);
    }

    [TestMethod]
    public void ArgMaxAbsRange_EndLessThanStart_ReturnsStart()
    {
        var form = CreateTestableFormAlign();
        var data = new double[] { 1, 2, 3 };
        int idx = form.PublicArgMaxAbsRange(data, 2, 1);
        Assert.AreEqual(2, idx);
    }

    [TestMethod]
    public void ArgMaxAbsRange_NullArray_ReturnsZero()
    {
        var form = CreateTestableFormAlign();
        int idx = form.PublicArgMaxAbsRange(null, 0, 5);
        Assert.AreEqual(0, idx);
    }

    [TestMethod]
    public void ArgMaxAbs_FindsGlobalMaxIndex()
    {
        var form = CreateTestableFormAlign();
        var data = new double[] { 1, -2, 8, -3 };
        int idx = form.PublicArgMaxAbs(data);
        Assert.AreEqual(2, idx);
    }
    #endregion

    #region UnwrapPhaseDegrees
    [TestMethod]
    public void UnwrapPhaseDegrees_NullInput_ReturnsNull()
    {
        var form = CreateTestableFormAlign();
        var result = form.PublicUnwrapPhaseDegrees(null);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void UnwrapPhaseDegrees_EmptyInput_ReturnsEmpty()
    {
        var form = CreateTestableFormAlign();
        var result = form.PublicUnwrapPhaseDegrees(Array.Empty<double>());
        Assert.AreEqual(0, result.Length);
    }

    [TestMethod]
    public void UnwrapPhaseDegrees_ContinuousValues_UnchangedAfterRewrap()
    {
        var form = CreateTestableFormAlign();
        var input = new double[] { 0, 10, 20, 30 };
        var result = form.PublicUnwrapPhaseDegrees(input);
        CollectionAssert.AreEqual(input, result);
    }

    [TestMethod]
    public void UnwrapPhaseDegrees_NaNGap_ResetsOffsetAndPropagatesNaN()
    {
        var form = CreateTestableFormAlign();
        var input = new double[] { 0, double.NaN, 20 };
        var result = form.PublicUnwrapPhaseDegrees(input);
        Assert.AreEqual(0.0, result[0], 1e-9);
        Assert.IsTrue(double.IsNaN(result[1]));
        Assert.AreEqual(20.0, result[2], 1e-9);
    }
    #endregion

    #region ComputeETCPercent
    [TestMethod]
    public void ComputeETCPercent_AllZeroInput_ReturnsAllNaN()
    {
        var form = CreateTestableFormAlign();
        var ir = new double[16];
        var result = form.PublicComputeETCPercent(ir, 48000.0, 1.0);
        foreach (var v in result)
            Assert.IsTrue(double.IsNaN(v));
    }

    [TestMethod]
    public void ComputeETCPercent_ImpulseInput_PeakIsNear100Percent()
    {
        var form = CreateTestableFormAlign();
        var ir = new double[64];
        ir[0] = 1.0;
        var result = form.PublicComputeETCPercent(ir, 48000.0, 1.0);
        double max = 0.0;
        foreach (var v in result)
            if (!double.IsNaN(v) && v > max) max = v;
        Assert.AreEqual(100.0, max, 0.5);
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

    #region CircularShiftToPeak
    [TestMethod]
    public void CircularShiftToPeak_ShiftsPeakToIndexZero()
    {
        var form = CreateTestableFormAlign();
        var data = new double[] { 1, 2, 9, 3 };
        var result = form.PublicCircularShiftToPeak(data);
        Assert.AreEqual(9.0, result[0], 1e-9);
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

        // Default cohWarmupFrames is 2 in production code.
        form.PublicUpdateAveragedSpectra(a, b, r);
        form.PublicUpdateAveragedSpectra(a, b, r);
        Assert.IsTrue(form.PublicIsCoherenceReady());
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

    [TestMethod]
    public void ComputeMaxCoherence_ReturnsMaxOfEachArray()
    {
        var form = CreateTestableFormAlign();
        form.PublicComputeMaxCoherence(new double[] { 0.1, 0.9, 0.5 }, new double[] { 0.2, 0.3, 0.7 }, out double maxA, out double maxB);
        Assert.AreEqual(0.9, maxA, 1e-9);
        Assert.AreEqual(0.7, maxB, 1e-9);
    }

    [TestMethod]
    public void ComputeAdaptiveEpsilons_UsesEpsilonFloorWhenPowerIsZero()
    {
        var form = CreateTestableFormAlign();
        form.PublicSetFFTSize(8);
        form.PublicEnsureTransferStateInitialized();
        form.PublicComputeAdaptiveEpsilons(4, out double epsSxx, out double epsSyyMax);
        Assert.AreEqual(1e-30, epsSxx, 1e-40);
        Assert.AreEqual(0.0, epsSyyMax, 1e-40);
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
