#nullable enable

namespace BassThatHz_ASIO_DSP_Processor;

#region Usings
using DSPLib;
using System;
using System.Numerics;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using static System.Runtime.InteropServices.JavaScript.JSType;
#endregion

/// <summary>
///  BassThatHz ASIO DSP Processor Engine
///  Copyright (c) 2026 BassThatHz
/// 
/// Permission is hereby granted to use this software 
/// and associated documentation files (the "Software"), 
/// for educational purposess, scientific purposess or private purposess
/// or as part of an open-source community project, 
/// (and NOT for commerical use or resale in substaintial part or whole without prior authorization)
/// and all copies of the Software subject to the following conditions:
/// 
/// The copyright notice and this permission notice shall be included in all
/// copies or substantial portions of the Software.
/// 
/// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
/// IMPLIED, INCLUDING BUT NOT LIMITED TO FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
/// IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
/// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
/// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
/// SOFTWARE. ENFORCEABLE PORTIONS SHALL REMAIN IF NOT FOUND CONTRARY UNDER LAW.
/// </summary>

public partial class FormAlign : Form
{
    #region Variables
    // Averaging param
    protected double alpha = 0.005;

    // Coherence mask threshold (tune)
    protected double cohMin = 0.3;

    // Warm-up frames (prevents “empty phase” at startup)
    protected int cohWarmupFrames = 2;

    // We will use an ADAPTIVE epsilon based on observed power to avoid underflow gating.
    protected double epsFloor = 1e-30;

    protected int FFTSize = 4096;

    #region States, Counters, Buffers
    protected bool NewDataAvailable = false;
    protected double[]? DataBufferA;
    protected double[]? DataBufferB;
    protected double[]? DataBufferRef;

    protected IStreamItem? SourceA;
    protected IStreamItem? SourceB;
    protected IStreamItem? SourceRef;

    // Add these fields to your class (near other variables):
    protected Complex[]? _SxyA;
    protected Complex[]? _SxyB;
    protected double[]? _SyyA;
    protected double[]? _SyyB;
    protected double[]? _Sxx;
    protected int _TfAvgFrames = 0;
    // Cached FFT + window to avoid reallocating each refresh
    protected FFT? _fftCached;
    protected double[]? _windowCached;
    protected int _cachedFFTSize = 0;

    // Reusable temporary buffers for input copies to avoid per-tick allocations
    protected double[]? _tmpA;
    protected double[]? _tmpB;
    protected double[]? _tmpRef;
    #endregion

    #endregion

    #region Constructor
    public FormAlign()
    {
        InitializeComponent();
    }
    #endregion

    #region Event Handlers
    protected virtual void FormAlign_Load(object sender, EventArgs e)
    {
        try
        {
            this.Chart_Mag.SuppressExceptions = true;
            this.Chart_Phase.SuppressExceptions = true;
            this.Chart_IR.SuppressExceptions = true;

            CommonFunctions.Set_DropDownTargetLists(new ComboBox(), this.cboSource1, false);
            CommonFunctions.Set_DropDownTargetLists(new ComboBox(), this.cboSource2, false);
            CommonFunctions.Set_DropDownTargetLists(new ComboBox(), this.cboRef, false);

            this.FFTSize_CBO.SelectedIndex = 0;

            Program.ASIO.OutputDataAvailable += ASIO_OutputDataAvailable;
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }

    protected virtual void cboSource1_SelectedIndexChanged(object sender, EventArgs e)
    {
        try
        {
            if (this.cboSource1.SelectedItem is IStreamItem)
                this.SourceA = (IStreamItem)this.cboSource1.SelectedItem;
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }

    protected virtual void cboSource2_SelectedIndexChanged(object sender, EventArgs e)
    {
        try
        {
            if (this.cboSource2.SelectedItem is IStreamItem)
                this.SourceB = (IStreamItem)this.cboSource2.SelectedItem;
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }

    protected virtual void cboRef_SelectedIndexChanged(object sender, EventArgs e)
    {
        try
        {
            if (this.cboRef.SelectedItem is IStreamItem)
                this.SourceRef = (IStreamItem)this.cboRef.SelectedItem;
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }

    protected virtual void ASIO_OutputDataAvailable()
    {
        try
        {
            if (this.SourceA == null)
                return;
            if (this.SourceB == null)
                return;
            if (this.SourceRef == null)
                return;

            this.DataBufferA = CommonFunctions.GetStreamOutputDataByStreamItem(this.SourceA);
            this.DataBufferB = CommonFunctions.GetStreamOutputDataByStreamItem(this.SourceB);
            this.DataBufferRef = CommonFunctions.GetStreamOutputDataByStreamItem(this.SourceRef);

            if (DataBufferA is not null && DataBufferB is not null && DataBufferRef is not null)
                this.NewDataAvailable = true;
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }

    protected virtual void RefreshTimer_Tick(object sender, EventArgs e)
    {
        try
        {
            if (!this.NewDataAvailable)
                return;

            if (this.DataBufferA is not null &&
                this.DataBufferB is not null &&
                this.DataBufferRef is not null)
            {
                this.NewDataAvailable = false;
                this.RefreshTimer.Stop();

                this.SafeInvoke(() =>
                {
                    this.alpha = double.Parse(this.Averaging_TXT.Text);
                    this.cohMin = double.Parse(this.Coherence_Mask_TXT.Text);

                    int size = string.IsNullOrEmpty(this.FFTSize_CBO.SelectedText) ? 4096 :
                                        int.Parse(this.FFTSize_CBO.SelectedText);
                    this.FFTSize = size;
                });

                // Reuse temporary buffers to avoid per-tick allocations
                this.EnsureTempBuffers();

                int copyA = Math.Min(this.DataBufferA.Length, this.FFTSize);
                int copyB = Math.Min(this.DataBufferB.Length, this.FFTSize);
                int copyR = Math.Min(this.DataBufferRef.Length, this.FFTSize);

                Array.Copy(this.DataBufferA, 0, this._tmpA!, 0, copyA);
                if (copyA < this.FFTSize) Array.Clear(this._tmpA!, copyA, this.FFTSize - copyA);

                Array.Copy(this.DataBufferB, 0, this._tmpB!, 0, copyB);
                if (copyB < this.FFTSize) Array.Clear(this._tmpB!, copyB, this.FFTSize - copyB);

                Array.Copy(this.DataBufferRef, 0, this._tmpRef!, 0, copyR);
                if (copyR < this.FFTSize) Array.Clear(this._tmpRef!, copyR, this.FFTSize - copyR);

                this.Display_RTA_Results(this._tmpA!, this._tmpB!, this._tmpRef!);
                this.RefreshTimer.Start();
            }
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }
    #endregion

    #region Protected Methods
    // Patch checkpoint
    protected void Display_RTA_Results(double[] dataA, double[] dataB, double[] dataRef)
    {
        // 1) Setup
        double sampleRate = Program.DSP_Info.InSampleRate;
        // Patch checkpoint
        var fft = this.CreateFFT(out double[] window);

        // 2) FFTs
        Complex[] A_fft, B_fft, R_fft;
        this.ComputeFFTs(fft, window, dataA, dataB, dataRef, out A_fft, out B_fft, out R_fft);

        // 3) Frequency axis
        int halfLen = this.FFTSize / 2 + 1;
        double[] xHz = this.BuildFrequencyAxis(fft, sampleRate, halfLen, out int minHz, out int maxHz);

        // 4) Update averaged spectra (Sxy/Sxx/Syy)
        this.EnsureTransferStateInitialized();
        this.UpdateAveragedSpectra(A_fft, B_fft, R_fft);

        // 5) Compute epsilons + coherence-ready
        bool coherenceReady = this.IsCoherenceReady();
        this.ComputeAdaptiveEpsilons(halfLen, out double epsSxx, out _); // epsSyy not needed for H

        // 6) Transfer functions + validity mask
        Complex[] H_A, H_B;
        bool[] validH;
        this.ComputeTransferFunctions(epsSxx, out H_A, out H_B, out validH);

        // 7) Coherence
        double[] cohA, cohB;
        this.ComputeCoherence(halfLen, epsSxx, out cohA, out cohB);
        this.ComputeMaxCoherence(cohA, cohB, out double maxCohA, out double maxCohB);

        // 8) Half-spectrum prep + mag/phase + masking + unwrap
        this.PrepareMagPhaseForPlot(
            H_A, H_B, validH,
            cohA, cohB, coherenceReady,
            out double[] magA_dB, out double[] magB_dB, out double[] magSum_dB,
            out double[] phaseA_deg, out double[] phaseB_deg);

        // 9) IR/ETC display referenced to SourceRef=0ms (NO negative delay)
        this.PrepareETCReferencedToRefZero(
                     fft, H_A, H_B, sampleRate,
                     out double[] tMs_centered,
                     out double[] etcA_disp, out double[] etcB_disp,
                     out double[] irA_disp, out double[] irB_disp,
                     out double delayMsA, out double delayMsB);

        // 10) UI
        this.SafeInvoke(() =>
        {
            this.Plot_Mag_Chart(this.Chart_Mag, minHz, maxHz, xHz, magA_dB, magB_dB, magSum_dB);
            this.Plot_Phase_Chart(this.Chart_Phase, minHz, maxHz, xHz, phaseA_deg, phaseB_deg);
            this.Plot_ETC_Chart(this.Chart_IR, tMs_centered, etcA_disp, etcB_disp, irA_disp, irB_disp);

            this.Coherence1_LBL.Text = maxCohA.ToString("F4");
            this.Coherence2_LBL.Text = maxCohB.ToString("F4");
            this.Coherence_LBL.Text = this.cohMin.ToString("F4");
            this.Delay1_LBL.Text = delayMsA.ToString("F4");
            this.Delay2_LBL.Text = delayMsB.ToString("F4");
        });
    }

    #region Setup helpers
    protected FFT CreateFFT(out double[] window)
    {
        // Reuse cached FFT and window when possible to avoid reallocations
        int zeroPadding = 0;
        var windowType = DSPLib.DSP.Window.Type.Hanning;

        if (this._fftCached == null || this._cachedFFTSize != this.FFTSize)
        {
            this._fftCached = new FFT(this.FFTSize, zeroPadding);
            this._windowCached = DSPLib.DSP.Window.Coefficients(windowType, this.FFTSize);
            this._cachedFFTSize = this.FFTSize;
        }

        window = this._windowCached!;
        return this._fftCached!;
    }

    protected void ComputeFFTs(
        FFT fft,
        double[] window,
        double[] dataA, double[] dataB, double[] dataRef,
        out Complex[] A_fft, out Complex[] B_fft, out Complex[] R_fft)
    {
        A_fft = fft.Perform_FFT(dataA, window);
        B_fft = fft.Perform_FFT(dataB, window);
        R_fft = fft.Perform_FFT(dataRef, window);
    }

    protected double[] BuildFrequencyAxis(FFT fft, double sampleRate, int halfLen, out int minHz, out int maxHz)
    {
        minHz = 1;
        maxHz = (int)(sampleRate / 2.0);

        double[] freqSpan = fft.FrequencySpan(sampleRate);
        double[] xHz;

        if (freqSpan != null && freqSpan.Length == halfLen)
        {
            xHz = freqSpan;
            xHz[0] = 0.0001;
        }
        else
        {
            xHz = new double[halfLen];
            double binHz = sampleRate / this.FFTSize;
            for (int i = 0; i < halfLen; i++)
                xHz[i] = Math.Max(0.0001, i * binHz);
        }

        return xHz;
    }
    #endregion

    #region Averaging spectra
    protected void UpdateAveragedSpectra(Complex[] A_fft, Complex[] B_fft, Complex[] R_fft)
    {
        // uses this.alpha and state arrays _SxyA/_SxyB/_Sxx/_SyyA/_SyyB
        for (int k = 0; k < this.FFTSize; k++)
        {
            Complex X = R_fft[k];
            Complex YA = A_fft[k];
            Complex YB = B_fft[k];

            Complex SxyA_inst = YA * Complex.Conjugate(X);
            Complex SxyB_inst = YB * Complex.Conjugate(X);

            double Sxx_inst = X.Real * X.Real + X.Imaginary * X.Imaginary;
            double SyyA_inst = YA.Real * YA.Real + YA.Imaginary * YA.Imaginary;
            double SyyB_inst = YB.Real * YB.Real + YB.Imaginary * YB.Imaginary;

            if (double.IsNaN(Sxx_inst) || double.IsInfinity(Sxx_inst)) Sxx_inst = 0.0;
            if (double.IsNaN(SyyA_inst) || double.IsInfinity(SyyA_inst)) SyyA_inst = 0.0;
            if (double.IsNaN(SyyB_inst) || double.IsInfinity(SyyB_inst)) SyyB_inst = 0.0;

            if (double.IsNaN(SxyA_inst.Real) || double.IsNaN(SxyA_inst.Imaginary) ||
                double.IsInfinity(SxyA_inst.Real) || double.IsInfinity(SxyA_inst.Imaginary))
                SxyA_inst = Complex.Zero;

            if (double.IsNaN(SxyB_inst.Real) || double.IsNaN(SxyB_inst.Imaginary) ||
                double.IsInfinity(SxyB_inst.Real) || double.IsInfinity(SxyB_inst.Imaginary))
                SxyB_inst = Complex.Zero;

            this._SxyA![k] = (1.0 - this.alpha) * this._SxyA[k] + this.alpha * SxyA_inst;
            this._SxyB![k] = (1.0 - this.alpha) * this._SxyB[k] + this.alpha * SxyB_inst;

            this._Sxx![k] = (1.0 - this.alpha) * this._Sxx[k] + this.alpha * Sxx_inst;
            this._SyyA![k] = (1.0 - this.alpha) * this._SyyA[k] + this.alpha * SyyA_inst;
            this._SyyB![k] = (1.0 - this.alpha) * this._SyyB[k] + this.alpha * SyyB_inst;
        }

        this._TfAvgFrames++;
    }
    #endregion

    #region Compute epsilon
    protected void ComputeAdaptiveEpsilons(int halfLen, out double epsSxx, out double epsSyyMax /*unused*/)
    {
        double maxSxx = 0.0, maxSyyA = 0.0, maxSyyB = 0.0;

        for (int i = 1; i < halfLen; i++)
        {
            double sxx = this._Sxx![i];
            double syA = this._SyyA![i];
            double syB = this._SyyB![i];

            if (!double.IsNaN(sxx) && !double.IsInfinity(sxx) && sxx > maxSxx) maxSxx = sxx;
            if (!double.IsNaN(syA) && !double.IsInfinity(syA) && syA > maxSyyA) maxSyyA = syA;
            if (!double.IsNaN(syB) && !double.IsInfinity(syB) && syB > maxSyyB) maxSyyB = syB;
        }

        epsSxx = Math.Max(this.epsFloor, maxSxx * 1e-12);
        // kept only to preserve your previous intent; not used for H
        epsSyyMax = Math.Max(maxSyyA, maxSyyB);
    }
    #endregion

    #region Transfer function
    protected void ComputeTransferFunctions(double epsSxx, out Complex[] H_A, out Complex[] H_B, out bool[] validH)
    {
        H_A = new Complex[this.FFTSize];
        H_B = new Complex[this.FFTSize];
        validH = new bool[this.FFTSize];

        for (int k = 0; k < this.FFTSize; k++)
        {
            double sxx = this._Sxx![k];

            if (double.IsNaN(sxx) || double.IsInfinity(sxx) || sxx <= epsSxx)
            {
                H_A[k] = Complex.Zero;
                H_B[k] = Complex.Zero;
                validH[k] = false;
                continue;
            }

            H_A[k] = this._SxyA![k] / sxx;
            H_B[k] = this._SxyB![k] / sxx;
            validH[k] = true;
        }
    }
    #endregion

    #region Coherence
    protected bool IsCoherenceReady() => this._TfAvgFrames >= this.cohWarmupFrames;

    protected void ComputeCoherence(int halfLen, double epsSxx, out double[] cohA, out double[] cohB)
    {
        cohA = new double[halfLen];
        cohB = new double[halfLen];

        // adaptive eps for Syy (derived from observed power)
        double maxSyyA = 0.0, maxSyyB = 0.0;
        for (int i = 1; i < halfLen; i++)
        {
            double syA = this._SyyA![i];
            double syB = this._SyyB![i];
            if (!double.IsNaN(syA) && !double.IsInfinity(syA) && syA > maxSyyA) maxSyyA = syA;
            if (!double.IsNaN(syB) && !double.IsInfinity(syB) && syB > maxSyyB) maxSyyB = syB;
        }
        double epsSyyA = Math.Max(this.epsFloor, maxSyyA * 1e-12);
        double epsSyyB = Math.Max(this.epsFloor, maxSyyB * 1e-12);

        for (int i = 0; i < halfLen; i++)
        {
            double sxx = this._Sxx![i];
            double syyA = this._SyyA![i];
            double syyB = this._SyyB![i];

            if (double.IsNaN(sxx) || double.IsInfinity(sxx)) sxx = 0.0;
            if (double.IsNaN(syyA) || double.IsInfinity(syyA)) syyA = 0.0;
            if (double.IsNaN(syyB) || double.IsInfinity(syyB)) syyB = 0.0;

            Complex sxyA = this._SxyA![i];
            Complex sxyB = this._SxyB![i];

            double sxyA_mag2 = sxyA.Real * sxyA.Real + sxyA.Imaginary * sxyA.Imaginary;
            double sxyB_mag2 = sxyB.Real * sxyB.Real + sxyB.Imaginary * sxyB.Imaginary;

            double denomA = sxx * syyA;
            double denomB = sxx * syyB;

            double cA = (sxx > epsSxx && syyA > epsSyyA && denomA > this.epsFloor) ? (sxyA_mag2 / denomA) : 0.0;
            double cB = (sxx > epsSxx && syyB > epsSyyB && denomB > this.epsFloor) ? (sxyB_mag2 / denomB) : 0.0;

            if (double.IsNaN(cA) || double.IsInfinity(cA)) cA = 0.0;
            if (double.IsNaN(cB) || double.IsInfinity(cB)) cB = 0.0;

            if (cA < 0) cA = 0; else if (cA > 1) cA = 1;
            if (cB < 0) cB = 0; else if (cB > 1) cB = 1;

            cohA[i] = cA;
            cohB[i] = cB;
        }
    }

    protected void ComputeMaxCoherence(double[] cohA, double[] cohB, out double maxCohA, out double maxCohB)
    {
        maxCohA = 0.0;
        maxCohB = 0.0;

        for (int i = 0; i < cohA.Length; i++)
        {
            double a = cohA[i];
            double b = cohB[i];
            if (!double.IsNaN(a) && a > maxCohA) maxCohA = a;
            if (!double.IsNaN(b) && b > maxCohB) maxCohB = b;
        }
    }
    #endregion

    #region Mag/phase prep
    protected void PrepareMagPhaseForPlot(
        Complex[] H_A, Complex[] H_B, bool[] validH,
        double[] cohA, double[] cohB, bool coherenceReady,
        out double[] magA_dB, out double[] magB_dB, out double[] magSum_dB,
        out double[] phaseA_deg, out double[] phaseB_deg)
    {
        int halfLen = this.FFTSize / 2 + 1;

        var H_A_half = new Complex[halfLen];
        var H_B_half = new Complex[halfLen];
        Array.Copy(H_A, H_A_half, halfLen);
        Array.Copy(H_B, H_B_half, halfLen);

        var H_sum_half = new Complex[halfLen];
        for (int i = 0; i < halfLen; i++)
            H_sum_half[i] = H_A_half[i] + H_B_half[i];

        double[] magA = DSPLib.DSP.ConvertComplex.ToMagnitude(H_A_half);
        double[] magB = DSPLib.DSP.ConvertComplex.ToMagnitude(H_B_half);
        double[] magS = DSPLib.DSP.ConvertComplex.ToMagnitude(H_sum_half);

        magA_dB = DSPLib.DSP.ConvertMagnitude.ToMagnitudeDBV(magA);
        magB_dB = DSPLib.DSP.ConvertMagnitude.ToMagnitudeDBV(magB);
        magSum_dB = DSPLib.DSP.ConvertMagnitude.ToMagnitudeDBV(magS);

        phaseA_deg = DSPLib.DSP.ConvertComplex.ToPhaseDegrees(H_A_half);
        phaseB_deg = DSPLib.DSP.ConvertComplex.ToPhaseDegrees(H_B_half);

        for (int i = 0; i < halfLen; i++)
        {
            if (!validH[i])
            {
                magA_dB[i] = double.NaN;
                magB_dB[i] = double.NaN;
                magSum_dB[i] = double.NaN;

                phaseA_deg[i] = double.NaN;
                phaseB_deg[i] = double.NaN;
                continue;
            }

            if (i == 0)
            {
                phaseA_deg[i] = double.NaN;
                phaseB_deg[i] = double.NaN;
                continue;
            }

            if (coherenceReady)
            {
                if (cohA[i] < this.cohMin) phaseA_deg[i] = double.NaN;
                if (cohB[i] < this.cohMin) phaseB_deg[i] = double.NaN;
            }
        }

        phaseA_deg = this.UnwrapPhaseDegrees(phaseA_deg);
        phaseB_deg = this.UnwrapPhaseDegrees(phaseB_deg);
    }
    #endregion

    #region IR/ETC referenced to ref=0ms (ETC + IR on same +/-100% axis)

    protected void PrepareETCReferencedToRefZero(
        FFT fft,
        Complex[] H_A, Complex[] H_B,
        double sampleRate,
        out double[] tMs_centered,
        out double[] etcA_disp, out double[] etcB_disp,
        out double[] irA_disp, out double[] irB_disp,
        out double delayMsA, out double delayMsB)
    {
        // --- IR (time domain) ---
        double[] irA = fft.Perform_IFFT(H_A);
        double[] irB = fft.Perform_IFFT(H_B);

        // Optional scaling (keep if DSPLib IFFT is unscaled; remove if already 1/N)
        double invN = 1.0 / this.FFTSize;
        for (int i = 0; i < this.FFTSize; i++)
        {
            irA[i] *= invN;
            irB[i] *= invN;
        }

        // Normalize IR to +-1, then scale to "percent axis" units (-100..+100)
        this.NormalizeToUnitPeakInPlace(irA);
        this.NormalizeToUnitPeakInPlace(irB);

        double[] irA_pct = new double[this.FFTSize];
        double[] irB_pct = new double[this.FFTSize];
        for (int i = 0; i < this.FFTSize; i++)
        {
            irA_pct[i] = 100.0 * irA[i];
            irB_pct[i] = 100.0 * irB[i];
        }

        // --- ETC (from unshifted IR) ---
        double[] etcA = this.ComputeETCPercent(irA, sampleRate, smoothMs: 1.0);
        double[] etcB = this.ComputeETCPercent(irB, sampleRate, smoothMs: 1.0);

        int n = this.FFTSize;
        int center = n / 2;

        // Causal peak search only => delays cannot be negative
        int peakA = this.ArgMaxAbsRange(irA, 0, center);
        int peakB = this.ArgMaxAbsRange(irB, 0, center);

        delayMsA = 1000.0 * peakA / sampleRate;
        delayMsB = 1000.0 * peakB / sampleRate;

        // Common shift so sample 0 appears at 0ms on the centered axis
        int shiftCommon = center;

        // Shift for display
        etcA_disp = this.CircularShift(etcA, shiftCommon);
        etcB_disp = this.CircularShift(etcB, shiftCommon);
        irA_disp = this.CircularShift(irA_pct, shiftCommon);
        irB_disp = this.CircularShift(irB_pct, shiftCommon);

        // Signed time axis centered at 0
        double msPerSample = 1000.0 / sampleRate;
        tMs_centered = new double[n];
        for (int i = 0; i < n; i++)
            tMs_centered[i] = (i - center) * msPerSample;
    }

    #endregion

    #region Helper Functions

    #region IR helpers (normalize to +-1)

    protected double PeakAbs(double[] x)
    {
        double p = 0.0;
        for (int i = 0; i < x.Length; i++)
        {
            double a = Math.Abs(x[i]);
            if (!double.IsNaN(a) && !double.IsInfinity(a) && a > p)
                p = a;
        }
        return p;
    }

    protected void NormalizeToUnitPeakInPlace(double[] x)
    {
        double p = this.PeakAbs(x);
        if (p <= 0.0) return;

        double inv = 1.0 / p;
        for (int i = 0; i < x.Length; i++)
            x[i] *= inv;
    }

    #endregion

    protected double[] CircularShift(double[] x, int shift)
    {
        int n = x.Length;
        if (n == 0) return x;

        // normalize shift to [0..n-1]
        shift %= n;
        if (shift < 0) shift += n;

        var y = new double[n];
        for (int i = 0; i < n; i++)
            y[(i + shift) % n] = x[i];

        return y;
    }

    protected void EnsureTransferStateInitialized()
    {
        if (this._SxyA == null || this._SxyA.Length != this.FFTSize)
            this._SxyA = new Complex[this.FFTSize];

        if (this._SxyB == null || this._SxyB.Length != this.FFTSize)
            this._SxyB = new Complex[this.FFTSize];

        if (this._Sxx == null || this._Sxx.Length != this.FFTSize)
            this._Sxx = new double[this.FFTSize];

        if (this._SyyA == null || this._SyyA.Length != this.FFTSize)
            this._SyyA = new double[this.FFTSize];

        if (this._SyyB == null || this._SyyB.Length != this.FFTSize)
            this._SyyB = new double[this.FFTSize];
    }

    protected void EnsureTempBuffers()
    {
        if (this._tmpA == null || this._tmpA.Length != this.FFTSize)
        {
            this._tmpA = new double[this.FFTSize];
            this._tmpB = new double[this.FFTSize];
            this._tmpRef = new double[this.FFTSize];
        }
    }

    protected int ArgMaxAbsRange(double[] x, int startInclusive, int endInclusive)
    {
        if (x == null || x.Length == 0) return 0;

        if (startInclusive < 0) startInclusive = 0;
        if (endInclusive >= x.Length) endInclusive = x.Length - 1;
        if (endInclusive < startInclusive) return startInclusive;

        int peak = startInclusive;
        double maxAbs = 0.0;

        for (int i = startInclusive; i <= endInclusive; i++)
        {
            double a = Math.Abs(x[i]);
            if (a > maxAbs)
            {
                maxAbs = a;
                peak = i;
            }
        }

        return peak;
    }

    protected int ArgMaxAbs(double[] x)
    {
        int peak = 0;
        double maxAbs = 0;

        for (int i = 0; i < x.Length; i++)
        {
            double a = Math.Abs(x[i]);
            if (a > maxAbs)
            {
                maxAbs = a;
                peak = i;
            }
        }

        return peak;
    }

    protected double PeakIndexToSignedDelayMs(int peakIndex, double sampleRate, int fftSize)
    {
        int signedSamples = peakIndex;

        // interpret indices > N/2 as negative (wrapped)
        if (peakIndex > fftSize / 2)
            signedSamples = peakIndex - fftSize;

        return 1000.0 * signedSamples / sampleRate;
    }

    protected double[] CircularShiftToPeak(double[] x)
    {
        int peak = 0;
        double maxAbs = 0;

        for (int i = 0; i < x.Length; i++)
        {
            double a = Math.Abs(x[i]);
            if (a > maxAbs)
            {
                maxAbs = a;
                peak = i;
            }
        }

        // shift so peak is at index 0 (simple + common for IR display)
        double[] y = new double[x.Length];
        int n = x.Length;
        for (int i = 0; i < n; i++)
            y[i] = x[(i + peak) % n];

        return y;
    }

    protected double[] UnwrapPhaseDegrees(double[] phaseDeg)
    {
        if (phaseDeg == null || phaseDeg.Length == 0)
            return phaseDeg;

        double[] y = new double[phaseDeg.Length];

        double prev = phaseDeg[0];
        y[0] = prev;

        double offset = 0.0;

        for (int i = 1; i < phaseDeg.Length; i++)
        {
            double p = phaseDeg[i];

            if (double.IsNaN(p) || double.IsNaN(prev))
            {
                // reset across gaps
                y[i] = p;
                prev = p;
                offset = 0.0;
                continue;
            }

            double dp = p - prev;

            // Wrap correction: keep step within [-180, 180]
            if (dp > 180.0) offset -= 360.0;
            else if (dp < -180.0) offset += 360.0;

            y[i] = p + offset;
            prev = p;
        }

        // Optional: bring it back into a pleasant viewing range.
        // Comment out if you want the continuous unwrapped line.
        for (int i = 0; i < y.Length; i++)
        {
            if (double.IsNaN(y[i])) continue;
            // Keep roughly centered by wrapping to [-180, 180]
            y[i] = this.WrapTo180(y[i]);
        }

        return y;
    }

    protected double WrapTo180(double x)
    {
        // Wrap any angle to (-180, 180]
        x %= 360.0;
        if (x <= -180.0) x += 360.0;
        else if (x > 180.0) x -= 360.0;
        return x;
    }

    protected double[] ComputeETCPercent(double[] ir, double sampleRate, double smoothMs = 1.0)
    {
        int n = ir.Length;
        double[] env = new double[n];

        // Window length in samples for smoothing (moving average of energy)
        int win = (int)Math.Round(sampleRate * (smoothMs / 1000.0));
        if (win < 1) win = 1;

        // 1) Energy
        double[] e = new double[n];
        for (int i = 0; i < n; i++)
        {
            double v = ir[i];
            e[i] = v * v;
        }

        // 2) Smooth energy using running average (O(N))
        //    avgE[i] = mean(e[i .. i+win-1])  (forward-looking window)
        //    This feels "ETC-like" because it doesn't smear earlier arrivals backward.
        double sum = 0.0;
        int r = 0;

        // prime initial window
        while (r < n && r < win)
            sum += e[r++];

        for (int i = 0; i < n; i++)
        {
            int curWin = Math.Min(win, n - i); // shrink near end
            double avgE = (curWin > 0) ? (sum / curWin) : 0.0;

            // envelope in amplitude units
            env[i] = Math.Sqrt(Math.Max(0.0, avgE));

            // slide window forward by 1
            if (i + win < n) sum += e[i + win];
            sum -= e[i];
        }

        // 3) Normalize to peak => 100%
        double peak = 0.0;
        for (int i = 0; i < n; i++)
            if (!double.IsNaN(env[i]) && !double.IsInfinity(env[i]) && env[i] > peak)
                peak = env[i];

        if (peak <= 0.0)
        {
            for (int i = 0; i < n; i++) env[i] = double.NaN;
            return env;
        }

        for (int i = 0; i < n; i++)
            env[i] = 100.0 * (env[i] / peak);

        return env;
    }
    #endregion

    #region Charts
    protected void Plot_Mag_Chart(Chart chartControl, double min, double max, double[] xData, double[] magData1, double[] magData2, double[]? magSum = null)
    {
        chartControl.SuspendLayout();

        // Configure magnitude axis (primary Y-axis)
        chartControl.ChartAreas[0].AxisY.Interval = 12;
        chartControl.ChartAreas[0].AxisY.IntervalType = DateTimeIntervalType.Number;
        chartControl.ChartAreas[0].AxisY.Maximum = Double.Parse(this.maxdB_TXT.Text);
        chartControl.ChartAreas[0].AxisY.Minimum = Double.Parse(this.mindB_TXT.Text);
        chartControl.ChartAreas[0].AxisY.MinorGrid.Enabled = true;
        chartControl.ChartAreas[0].AxisY.MinorGrid.Interval = 3;
        chartControl.ChartAreas[0].AxisY.Title = "Magnitude (dB)";

        // Configure X-axis (frequency)
        chartControl.ChartAreas[0].AxisX.IntervalType = DateTimeIntervalType.Number;
        chartControl.ChartAreas[0].AxisX.MinorGrid.Enabled = true;
        chartControl.ChartAreas[0].AxisX.MinorGrid.Interval = 1;
        chartControl.ChartAreas[0].AxisX.Minimum = min;
        chartControl.ChartAreas[0].AxisX.Maximum = max;
        chartControl.ChartAreas[0].AxisX.IsLogarithmic = true;
        chartControl.ChartAreas[0].AxisX.Title = "Frequency (Hz)";

        // Series1 (Blue)
        var s1 = chartControl.Series["Series1"];
        s1.YAxisType = AxisType.Primary;
        s1.ChartType = SeriesChartType.Line;
        s1.Color = System.Drawing.Color.Blue;
        s1.BorderWidth = 2;

        // Series2 (Red)
        var s2 = chartControl.Series["Series2"];
        s2.YAxisType = AxisType.Secondary;
        s2.ChartType = SeriesChartType.Line;
        s2.Color = System.Drawing.Color.Red;
        s2.BorderWidth = 2;

        // Secondary Y-axis matches primary
        chartControl.ChartAreas[0].AxisY2.Title = "Magnitude2 (dB)";
        chartControl.ChartAreas[0].AxisY2.MajorGrid.Enabled = false;
        chartControl.ChartAreas[0].AxisY2.MinorGrid.Enabled = false;
        chartControl.ChartAreas[0].AxisY2.Interval = chartControl.ChartAreas[0].AxisY.Interval;
        chartControl.ChartAreas[0].AxisY2.IntervalType = chartControl.ChartAreas[0].AxisY.IntervalType;
        chartControl.ChartAreas[0].AxisY2.Maximum = chartControl.ChartAreas[0].AxisY.Maximum;
        chartControl.ChartAreas[0].AxisY2.Minimum = chartControl.ChartAreas[0].AxisY.Minimum;

        // Bind A/B
        s1.Points.Clear();
        s1.Points.DataBindXY(xData, magData1);

        s2.Points.Clear();
        s2.Points.DataBindXY(xData, magData2);

        // Sum (Green) on PRIMARY axis (usually what you want for "total")
        if (chartControl.Series.FindByName("Sum") == null)
        {
            var sumSeries = new Series("Sum");
            sumSeries.ChartType = SeriesChartType.Line;
            sumSeries.YAxisType = AxisType.Primary;
            sumSeries.Color = System.Drawing.Color.Green;
            sumSeries.BorderWidth = 2;
            chartControl.Series.Add(sumSeries);
        }

        var sSum = chartControl.Series["Sum"];
        sSum.ChartType = SeriesChartType.Line;
        sSum.YAxisType = AxisType.Primary;
        sSum.Color = System.Drawing.Color.Green;
        sSum.BorderWidth = 2;

        sSum.Points.Clear();
        if (magSum != null)
            sSum.Points.DataBindXY(xData, magSum);

        // Dummy to keep axis alive
        if (chartControl.Series.FindByName("Dummy") == null)
        {
            var dummySeries = new Series("Dummy");
            dummySeries.ChartType = SeriesChartType.Point;
            dummySeries.YAxisType = AxisType.Primary;
            dummySeries.IsVisibleInLegend = false;
            dummySeries.Points.AddXY(0, 0);
            chartControl.Series.Add(dummySeries);
        }
        chartControl.Series["Dummy"].Enabled = true;

        // toggles
        s1.Enabled = true;
        s2.Enabled = true;
        sSum.Enabled = magSum != null; // or tie to checkbox if you add one

        chartControl.ResumeLayout();
    }

    protected void Plot_Phase_Chart(Chart chartControl, double min, double max, double[] xData, double[] phaseData1, double[] phaseData2)
    {
        chartControl.SuspendLayout();

        // Configure phase axis (primary Y-axis)
        chartControl.ChartAreas[0].AxisY.IntervalType = DateTimeIntervalType.Number;
        chartControl.ChartAreas[0].AxisY.MajorGrid.Enabled = false; // optional
        chartControl.ChartAreas[0].AxisY.MinorGrid.Enabled = false; // optional
        chartControl.ChartAreas[0].AxisY.Minimum = -180;
        chartControl.ChartAreas[0].AxisY.Maximum = 180;
        chartControl.ChartAreas[0].AxisY.Interval = 90;
        chartControl.ChartAreas[0].AxisY.Title = "Phase1 (Degrees)";

        // Configure X-axis (frequency)
        chartControl.ChartAreas[0].AxisX.IntervalType = DateTimeIntervalType.Number;
        chartControl.ChartAreas[0].AxisX.MinorGrid.Enabled = true;
        chartControl.ChartAreas[0].AxisX.MinorGrid.Interval = 1;
        chartControl.ChartAreas[0].AxisX.Minimum = min;
        chartControl.ChartAreas[0].AxisX.Maximum = max;
        chartControl.ChartAreas[0].AxisX.IsLogarithmic = true;
        chartControl.ChartAreas[0].AxisX.Title = "Frequency (Hz)";

        // Configure "Series2" to use the secondary Y-axis
        chartControl.Series["Series1"].YAxisType = AxisType.Primary;
        chartControl.Series["Series1"].ChartType = SeriesChartType.Line;
        chartControl.Series["Series1"].Color = System.Drawing.Color.Blue;
        chartControl.Series["Series1"].BorderWidth = 2;

        chartControl.Series["Series2"].YAxisType = AxisType.Secondary;
        chartControl.Series["Series2"].ChartType = SeriesChartType.Line;
        chartControl.Series["Series2"].Color = System.Drawing.Color.Red;
        chartControl.Series["Series2"].BorderWidth = 2;

        // Enable and configure secondary Y-axis for Phase2
        chartControl.ChartAreas[0].AxisY2.Title = "Phase2 (Degrees)";
        chartControl.ChartAreas[0].AxisY2.MajorGrid.Enabled = false; // optional
        chartControl.ChartAreas[0].AxisY2.MinorGrid.Enabled = false; // optional
        chartControl.ChartAreas[0].AxisY2.Interval = chartControl.ChartAreas[0].AxisY.Interval;
        chartControl.ChartAreas[0].AxisY2.IntervalType = chartControl.ChartAreas[0].AxisY.IntervalType;
        chartControl.ChartAreas[0].AxisY2.Maximum = chartControl.ChartAreas[0].AxisY.Maximum;
        chartControl.ChartAreas[0].AxisY2.Minimum = chartControl.ChartAreas[0].AxisY.Minimum;

        chartControl.Series["Series1"].Points.Clear();
        chartControl.Series["Series1"].Points.DataBindXY(xData, phaseData1);

        chartControl.Series["Series2"].Points.Clear();
        chartControl.Series["Series2"].Points.DataBindXY(xData, phaseData2);

        if (chartControl.Series.FindByName("Dummy") == null)
        {
            var dummySeries = new Series("Dummy");
            dummySeries.ChartType = SeriesChartType.Point;
            dummySeries.YAxisType = AxisType.Primary;
            dummySeries.IsVisibleInLegend = false;
            dummySeries.Points.AddXY(0, 0);
            chartControl.Series.Add(dummySeries);
        }

        // Ensure the dummy series remains visible to prevent AxisY from hiding
        chartControl.Series["Dummy"].Enabled = true;

        chartControl.Series["Series1"].Enabled = true; // this.ShowTotalMag_CHK.Checked;
        chartControl.Series["Series2"].Enabled = true; // this.ShowTotalPhase_CHK.Checked;

        chartControl.ResumeLayout();
    }

    protected void Plot_ETC_Chart(Chart chartControl, double[] tMs, double[] etcA, double[] etcB, double[] irA, double[] irB)
    {
        chartControl.SuspendLayout();

        var area = chartControl.ChartAreas[0];

        // --- 0ms vertical reference line (green) ---
        const string zeroLineName = "ZeroMsLine";

        // remove existing one if present (so it doesn't accumulate every refresh)
        for (int i = area.AxisX.StripLines.Count - 1; i >= 0; i--)
        {
            if (area.AxisX.StripLines[i].Tag as string == zeroLineName)
                area.AxisX.StripLines.RemoveAt(i);
        }

        var zero = new StripLine
        {
            Tag = zeroLineName,
            Interval = 0,          // required for a single line
            IntervalOffset = 0.0,  // x = 0ms
            StripWidth = 0.0,      // 0 => line (not a band)
            BorderColor = System.Drawing.Color.LimeGreen,
            BorderWidth = 2,
            BorderDashStyle = ChartDashStyle.Solid
        };

        area.AxisX.StripLines.Add(zero);

        // Y axis: -100% .. +100% (ETC is 0..100, IR is -100..+100)
        area.AxisY.Title = "ETC Envelope (%) / IR (%)";
        area.AxisY.MajorGrid.Enabled = true;
        area.AxisY.MinorGrid.Enabled = false;
        area.AxisY.Minimum = -100;
        area.AxisY.Maximum = 100;
        area.AxisY.Interval = 20;
        area.AxisY.IsReversed = false;

        // X axis: time
        area.AxisX.Title = "Time (ms)";
        area.AxisX.IsLogarithmic = false;
        area.AxisX.MajorGrid.Enabled = true;
        area.AxisX.MajorGrid.Interval = 1;
        area.AxisX.MinorGrid.Enabled = true;
        area.AxisX.MinorGrid.Interval = 0.1;

        area.AxisX.Minimum = Double.Parse(this.min_ms_TXT.Text);
        area.AxisX.Maximum = Double.Parse(this.max_ms_TXT.Text);

        // ETC Series styling (existing)
        var s1 = chartControl.Series["Series1"];
        s1.ChartType = SeriesChartType.Line;
        s1.Color = System.Drawing.Color.Blue;
        s1.BorderWidth = 2;
        s1.YAxisType = AxisType.Primary;

        var s2 = chartControl.Series["Series2"];
        s2.ChartType = SeriesChartType.Line;
        s2.Color = System.Drawing.Color.Red;
        s2.BorderWidth = 2;
        s2.YAxisType = AxisType.Primary;

        s1.Points.Clear();
        s1.Points.DataBindXY(tMs, etcA);

        s2.Points.Clear();
        s2.Points.DataBindXY(tMs, etcB);

        // --- Add IR overlay series (two more plots) ---
        if (chartControl.Series.FindByName("IR_A") == null)
        {
            var s = new Series("IR_A");
            s.ChartType = SeriesChartType.Line;
            s.YAxisType = AxisType.Primary;
            s.BorderWidth = 2;
            s.IsVisibleInLegend = true;
            chartControl.Series.Add(s);
        }

        if (chartControl.Series.FindByName("IR_B") == null)
        {
            var s = new Series("IR_B");
            s.ChartType = SeriesChartType.Line;
            s.YAxisType = AxisType.Primary;
            s.BorderWidth = 2;
            s.IsVisibleInLegend = true;
            chartControl.Series.Add(s);
        }

        var irAseries = chartControl.Series["IR_A"];
        irAseries.ChartType = SeriesChartType.Line;
        irAseries.BorderWidth = 2;
        irAseries.Color = System.Drawing.Color.Cyan;   // choose whatever you like
        irAseries.YAxisType = AxisType.Primary;
        irAseries.Points.Clear();
        irAseries.Points.DataBindXY(tMs, irA);

        var irBseries = chartControl.Series["IR_B"];
        irBseries.ChartType = SeriesChartType.Line;
        irBseries.BorderWidth = 2;
        irBseries.Color = System.Drawing.Color.Orange; // choose whatever you like
        irBseries.YAxisType = AxisType.Primary;
        irBseries.Points.Clear();
        irBseries.Points.DataBindXY(tMs, irB);

        chartControl.ResumeLayout();
    }

    #endregion

    #endregion

    #region Error Handling
    protected void Error(Exception ex)
    {
        Debug.Error(ex);
    }
    #endregion
}