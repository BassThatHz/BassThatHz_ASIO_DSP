#nullable enable

namespace BassThatHz_ASIO_DSP_Processor;

#region Usings
using DSPLib;
using NAudio.Utils;
using System;
using System.Numerics;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
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

    #region Series Colours
    //One definition per signal, shared by the chart series, the 0 ms reference line and the
    //swatch labels beside the source combo boxes. Defining them once is the point: a swatch
    //whose colour has drifted away from its trace is worse than no swatch at all.
    protected static readonly System.Drawing.Color Source1_Color = System.Drawing.Color.Blue;
    protected static readonly System.Drawing.Color Source2_Color = System.Drawing.Color.Red;

    //The Ref signal is not plotted as a trace: it IS the time reference, so it appears as the
    //0 ms vertical line on the impulse response chart.
    protected static readonly System.Drawing.Color Ref_Color = System.Drawing.Color.LimeGreen;
    #endregion

    #region Tuning Constants
    protected const int DefaultFFTSize = 4096;

    /// <summary>
    /// Ring buffer depth as a multiple of the FFT size. Deep enough to ride out a stalled UI
    /// thread (a window drag, a chart re-layout) without overflowing, shallow enough that the
    /// displayed measurement is never seconds behind the audio.
    /// </summary>
    protected const int BufferFrames = 4;

    /// <summary>
    /// Upper bound on analysis frames consumed per timer tick. The tick runs on the UI thread,
    /// so this is what stops a backlog of audio from freezing the window.
    /// </summary>
    protected const int MaxFramesPerTick = 16;

    /// <summary>
    /// Frames of averaging required before the coherence estimate is trusted enough to mask with.
    /// <para>
    /// DEFECT FIX: this used to be 2. Coherence measured over n averaged frames is biased UP by
    /// roughly 1/n even for two completely unrelated signals, so after 2 frames every bin reads
    /// about 0.5 whatever the truth is - the mask was gating on noise. 16 frames puts the floor
    /// near 0.06, which is low enough for a threshold in the 0.3-0.9 range to mean something.
    /// </para>
    /// </summary>
    protected const int CoherenceWarmupFrames = 16;

    /// <summary>
    /// The selectable smoothing widths, as a fraction of an octave. The combo box is populated
    /// FROM this table on load, so a label and the width it stands for cannot drift apart.
    /// </summary>
    protected static readonly (string Label, double Fraction)[] SmoothingOptions =
    {
        ("1/48 oct", 1.0 / 48.0),
        ("1/24 oct", 1.0 / 24.0),
        ("1/12 oct", 1.0 / 12.0),
        ("1/6 oct", 1.0 / 6.0),
        ("1/3 oct", 1.0 / 3.0),
        ("1 oct", 1.0),
    };

    /// <summary>1/12 octave - fine enough to keep a crossover's real shape, wide enough to read.</summary>
    protected const int DefaultSmoothingIndex = 2;

    #endregion

    #region User Settings (mirrored from the UI on every tick)
    // Exponential averaging coefficient.
    protected double alpha = 0.005;

    // Coherence mask threshold. Zero (or below) disables masking entirely.
    protected double cohMin = 0;

    // Fractional-octave complex smoothing of the displayed transfer functions.
    protected bool smoothingEnabled;
    protected double smoothingOctaveFraction = 1.0 / 12.0;

    // Adaptive epsilon floor - see ComputeAdaptiveEpsilons.
    protected double epsFloor = 1e-30;

    protected int FFTSize = DefaultFFTSize;
    #endregion

    #region States, Counters, Buffers
    protected bool IsClosing;

    protected IStreamItem? SourceA;
    protected IStreamItem? SourceB;
    protected IStreamItem? SourceRef;

    // Averaged cross/auto spectra. Owned by the UI thread only.
    protected Complex[]? _SxyA;
    protected Complex[]? _SxyB;
    protected double[]? _SyyA;
    protected double[]? _SyyB;
    protected double[]? _Sxx;
    protected int _TfAvgFrames = 0;

    // Reusable per-frame scratch, all exactly FFTSize long.
    protected double[]? _tmpA;
    protected double[]? _tmpB;
    protected double[]? _tmpRef;
    protected Complex[]? _fftA;
    protected Complex[]? _fftB;
    protected Complex[]? _fftRef;
    protected double[]? _irA;
    protected double[]? _irB;
    #endregion

    #region Analysis Configuration
    /// <summary>
    /// An immutable snapshot of everything one measurement needs: the transform, the window, the
    /// frequency axis and the three ring buffers feeding it.
    /// <para>
    /// This exists as ONE object published by ONE reference assignment because the three ring
    /// buffers are only meaningful as a set. A cross-spectrum measures the phase difference
    /// between two channels, so the three buffers must always hold the SAME span of samples; a
    /// config swap that let the ASIO thread write channel A to the old buffer and channel B to
    /// the new one would inject a delay that is not in the audio.
    /// </para>
    /// </summary>
    protected sealed class AnalysisConfig
    {
        /// <summary>The transform length, which is also the analysis frame length in samples.</summary>
        public required int FFTSize { get; init; }

        /// <summary>The input sample rate this snapshot was built for.</summary>
        public required int SampleRate { get; init; }

        /// <summary>Stateful transform instance. UI thread only - never share across threads.</summary>
        public required FFT Fft { get; init; }

        /// <summary>Window coefficients, exactly <see cref="FFTSize"/> long.</summary>
        public required double[] Window { get; init; }

        /// <summary>The plotted frequency axis, one entry per half-spectrum bin.</summary>
        public required double[] FrequencySpan { get; init; }

        public required CircularBuffer BufferA { get; init; }
        public required CircularBuffer BufferB { get; init; }
        public required CircularBuffer BufferRef { get; init; }

        /// <summary>
        /// Guards the THREE buffers as a group. Each <see cref="CircularBuffer"/> is already
        /// individually thread safe, which is not sufficient: a write that overflowed BufferA and
        /// discarded its oldest block before BufferB had been written would slide A forward
        /// relative to B and Ref. Producer and consumer both take this around their three-buffer
        /// operation, so a block is present in all three buffers or in none of them.
        /// </summary>
        public object SyncRoot { get; } = new();
    }

    protected AnalysisConfig? Config;
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

            this.Smoothing_CBO.Items.Clear();
            for (int i = 0; i < SmoothingOptions.Length; i++)
                _ = this.Smoothing_CBO.Items.Add(SmoothingOptions[i].Label);

            this.Smoothing_CBO.SelectedIndex = DefaultSmoothingIndex;
            this.Smoothing_CBO.Enabled = this.Smoothing_CHK.Checked;

            //Colour the swatches from the same constants the traces use.
            this.Source1_Color_LBL.BackColor = Source1_Color;
            this.Source2_Color_LBL.BackColor = Source2_Color;
            this.Ref_Color_LBL.BackColor = Ref_Color;

            this.Clear_Stats();

            Program.ASIO.OutputDataAvailable += this.ASIO_OutputDataAvailable;

            //Arm the refresh timer HERE, not in the designer. Load runs on the thread that pumps
            //this form and therefore owns its control handles, and a WinForms timer delivers its
            //tick to whichever thread armed it - so starting it here is what keeps
            //RefreshTimer_Tick on the same thread as the controls it reads. See the RefreshTimer
            //comment in FormAlign.Designer.cs.
            this.RefreshTimer.Start();
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }

    /// <summary>
    /// DEFECT FIX: the ASIO OutputDataAvailable subscription was never removed. Closing this
    /// window left a dead form pinned in memory and still doing per-buffer work on the audio
    /// notification thread for the rest of the session.
    /// </summary>
    protected virtual void FormAlign_FormClosing(object? sender, FormClosingEventArgs e)
    {
        try
        {
            this.IsClosing = true;
            this.RefreshTimer.Stop();
            Program.ASIO.OutputDataAvailable -= this.ASIO_OutputDataAvailable;
        }
        catch (Exception ex)
        {
            //The form is going away; there is nothing useful left to do, but do not lose the error.
            Debug.ReportSwallowed(ex);
        }
    }

    protected virtual void cboSource1_SelectedIndexChanged(object sender, EventArgs e)
    {
        try
        {
            if (this.cboSource1.SelectedItem is IStreamItem Local_Item)
            {
                this.SourceA = Local_Item;
                //Averaged spectra from the previous channel say nothing about this one.
                this.Reset_Measurement();
            }
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
            if (this.cboSource2.SelectedItem is IStreamItem Local_Item)
            {
                this.SourceB = Local_Item;
                this.Reset_Measurement();
            }
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
            if (this.cboRef.SelectedItem is IStreamItem Local_Item)
            {
                this.SourceRef = Local_Item;
                this.Reset_Measurement();
            }
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }

    /// <summary>
    /// Smoothing is purely a display transform applied to the already-averaged transfer function,
    /// so toggling it takes effect on the very next refresh - it never costs a re-average.
    /// </summary>
    protected virtual void Smoothing_CHK_CheckedChanged(object sender, EventArgs e)
    {
        try
        {
            this.Smoothing_CBO.Enabled = this.Smoothing_CHK.Checked;
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }

    protected virtual void Reset_BTN_Click(object sender, EventArgs e)
    {
        try
        {
            this.Reset_Measurement();
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }

    /// <summary>
    /// Captures one ASIO block of all three channels into the ring buffers.
    /// <para>
    /// DEFECT FIX: this used to store a REFERENCE to each channel's buffer and leave the
    /// analysis to pick them up on the next 200 ms timer tick. For a Bus source that reference
    /// is the live buffer the audio thread is writing, so the analyser read a torn mixture of
    /// blocks, and the three channels were sampled at whatever instants the timer happened to
    /// see - which is precisely the information a cross-spectrum needs to be exact about.
    /// </para>
    /// </summary>
    protected virtual void ASIO_OutputDataAvailable()
    {
        try
        {
            if (this.IsClosing)
                return;

            var Local_SourceA = this.SourceA;
            var Local_SourceB = this.SourceB;
            var Local_SourceRef = this.SourceRef;

            if (Local_SourceA == null || Local_SourceB == null || Local_SourceRef == null)
                return;

            //Read the snapshot ONCE - see AnalysisConfig for why this must not be re-read.
            var Local_Config = this.Config;
            if (Local_Config == null)
                return;

            var Local_A = CommonFunctions.GetStreamOutputDataSnapshotByStreamItem(Local_SourceA);
            var Local_B = CommonFunctions.GetStreamOutputDataSnapshotByStreamItem(Local_SourceB);
            var Local_Ref = CommonFunctions.GetStreamOutputDataSnapshotByStreamItem(Local_SourceRef);

            //A short read on one channel only would slide that channel's buffer relative to the
            //other two, i.e. fabricate a delay. Drop the whole block instead.
            int Local_Count = Local_A.Length;
            if (Local_Count <= 0 || Local_B.Length != Local_Count || Local_Ref.Length != Local_Count)
                return;

            lock (Local_Config.SyncRoot)
            {
                _ = Local_Config.BufferA.Write(Local_A, 0, Local_Count);
                _ = Local_Config.BufferB.Write(Local_B, 0, Local_Count);
                _ = Local_Config.BufferRef.Write(Local_Ref, 0, Local_Count);
            }
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
            if (this.IsClosing || this.IsDisposed)
                return;

            //Stop/Start have to run on whichever thread owns the timer's message queue - which is
            //the thread the tick arrived on - so they stay OUTSIDE the marshalling below. Calling
            //KillTimer from another thread fails silently, which would leave the timer firing
            //while an analysis pass was still running.
            this.RefreshTimer.Stop();
            try
            {
                //Backstop for the defect described on RefreshTimer in the designer: everything
                //Refresh_Analysis touches belongs to the thread that pumps this form, so if a
                //tick ever arrives anywhere else, marshal rather than crash. With the timer armed
                //from Load this should never engage - InvokeRequired is a cheap check when false.
                if (this.InvokeRequired)
                    this.SafeInvoke(this.Refresh_Analysis);
                else
                    this.Refresh_Analysis();
            }
            finally
            {
                if (!this.IsClosing && !this.IsDisposed)
                    this.RefreshTimer.Start();
            }
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }

    /// <summary>
    /// One analysis pass. Reads the settings, folds in every frame that has arrived and redraws.
    /// Must run on the thread that owns this form's controls.
    /// </summary>
    protected void Refresh_Analysis()
    {
        this.Read_UI_Settings();

        int Local_SampleRate = Program.DSP_Info.InSampleRate;
        if (Local_SampleRate <= 0)
            return;

        var Local_Config = this.Get_Config(Local_SampleRate);

        _ = this.Accumulate_Available_Frames(Local_Config);

        //Nothing has ever been averaged: leave the charts as the Reset button left them.
        if (this._TfAvgFrames <= 0)
            return;

        this.Display_Align_Results(Local_Config);
    }
    #endregion

    #region Protected Methods

    #region UI Settings
    /// <summary>
    /// Mirrors the user-editable settings into fields.
    /// <para>
    /// DEFECT FIX: double.Parse/int.Parse on user-editable text boxes, executed every tick. A
    /// half-typed value ("-", ".", "1e") threw FormatException, which this.Error escalated into
    /// the "A fatal error has occured / abort the app?" dialog. Keep the previous value when the
    /// text is not (yet) a valid number.
    /// </para>
    /// </summary>
    protected void Read_UI_Settings()
    {
        if (double.TryParse(this.Averaging_TXT.Text, out double Local_Alpha) &&
            double.IsFinite(Local_Alpha) && Local_Alpha > 0 && Local_Alpha <= 1)
            this.alpha = Local_Alpha;

        if (double.TryParse(this.Coherence_Mask_TXT.Text, out double Local_CohMin) &&
            double.IsFinite(Local_CohMin))
            this.cohMin = Math.Clamp(Local_CohMin, 0, 1);

        this.smoothingEnabled = this.Smoothing_CHK.Checked;
        this.smoothingOctaveFraction = this.Read_SmoothingFraction();
    }

    /// <summary>
    /// The selected smoothing width as a fraction of an octave.
    /// </summary>
    protected double Read_SmoothingFraction()
    {
        int Local_Index = this.Smoothing_CBO.SelectedIndex;

        if (Local_Index < 0 || Local_Index >= SmoothingOptions.Length)
            Local_Index = DefaultSmoothingIndex;

        return SmoothingOptions[Local_Index].Fraction;
    }

    /// <summary>
    /// The FFT length the user has selected.
    /// <para>
    /// DEFECT FIX: this read <c>FFTSize_CBO.SelectedText</c>, which is the selected portion of a
    /// combo box's EDIT control. FFTSize_CBO is a DropDownList and has no edit control, so
    /// SelectedText was always empty and the FFT size was permanently pinned to 4096 - the size
    /// selector did nothing at all.
    /// </para>
    /// </summary>
    protected int Read_Requested_FFTSize()
    {
        string? Local_Text = this.FFTSize_CBO.SelectedItem?.ToString();

        //FFT.Init throws unless the length is an exact power of two, and this value comes from a
        //combo box whose items could be edited in the designer, so verify rather than trust.
        if (int.TryParse(Local_Text, out int Local_Size) &&
            Local_Size >= 64 && Local_Size <= 1 << 18 &&
            (Local_Size & (Local_Size - 1)) == 0)
            return Local_Size;

        return DefaultFFTSize;
    }
    #endregion

    #region Configuration
    protected AnalysisConfig Get_Config(int sampleRate)
    {
        int Local_Requested = this.Read_Requested_FFTSize();
        var Local_Config = this.Config;

        if (Local_Config != null &&
            Local_Config.FFTSize == Local_Requested &&
            Local_Config.SampleRate == sampleRate)
            return Local_Config;

        return this.Rebuild_Config(Local_Requested, sampleRate);
    }

    protected AnalysisConfig Rebuild_Config(int fftSize, int sampleRate)
    {
        var Local_Fft = new FFT(fftSize, 0);
        int Local_Capacity = Math.Max(fftSize * BufferFrames, fftSize + 1);

        var Local_New = new AnalysisConfig
        {
            FFTSize = fftSize,
            SampleRate = sampleRate,
            Fft = Local_Fft,
            Window = DSPLib.DSP.Window.Coefficients(DSPLib.DSP.Window.Type.Hanning, fftSize),
            FrequencySpan = this.BuildFrequencyAxis(Local_Fft, sampleRate, fftSize / 2 + 1),
            BufferA = new CircularBuffer(Local_Capacity),
            BufferB = new CircularBuffer(Local_Capacity),
            BufferRef = new CircularBuffer(Local_Capacity),
        };

        this.FFTSize = fftSize;
        this.Reset_Averaging();

        //Single reference assignment - see AnalysisConfig.
        this.Config = Local_New;
        return Local_New;
    }

    protected double[] BuildFrequencyAxis(FFT fft, double sampleRate, int halfLen)
    {
        double[] Local_Span = fft.FrequencySpan(sampleRate);
        double[] Local_Hz;

        if (Local_Span != null && Local_Span.Length == halfLen)
        {
            Local_Hz = Local_Span;
        }
        else
        {
            Local_Hz = new double[halfLen];
            double Local_BinHz = sampleRate / (halfLen - 1) / 2.0;
            for (int i = 0; i < halfLen; i++)
                Local_Hz[i] = i * Local_BinHz;
        }

        //The X axis is logarithmic and starts at 1 Hz, so DC has no representable position.
        //Park it off-scale; its magnitude and phase are NaN anyway.
        if (Local_Hz.Length > 0)
            Local_Hz[0] = 0.0001;

        return Local_Hz;
    }
    #endregion

    #region Reset
    /// <summary>
    /// Drops the averaged measurement and blanks the charts. They stay blank until the analyser
    /// has a fresh frame, which is one FFT length of audio away.
    /// </summary>
    protected void Reset_Measurement()
    {
        this.Reset_Averaging();

        var Local_Config = this.Config;
        if (Local_Config != null)
            lock (Local_Config.SyncRoot)
            {
                Local_Config.BufferA.Reset();
                Local_Config.BufferB.Reset();
                Local_Config.BufferRef.Reset();
            }

        this.Blank_Frequency_Charts();

        //The time axis is linear, so simply emptying it is safe.
        this.Clear_Series(this.Chart_IR);
        this.Clear_Stats();
    }

    /// <summary>
    /// Blanks the magnitude and phase charts by plotting an all-NaN trace over the real frequency
    /// axis, rather than by emptying them.
    /// </summary>
    /// <remarks>
    /// DEFECT FIX: Reset used to call Points.Clear() on these two charts. That left them with a
    /// LOGARITHMIC x axis and no data whatsoever, and a chart in that state derives its scale from
    /// point INDEXES - index 0 has no logarithm, so it throws
    /// "Chart Area Axes - A logarithmic scale cannot be used for this axis."
    /// from ChartArea.SetDefaultFromIndexesOrData, reached through Chart.OnPaint ->
    /// Control.WmPaint -> Control.WndProc -> NativeWindow.Callback. An exception escaping a
    /// native-to-managed callback like that takes the process down with
    /// STATUS_FATAL_USER_CALLBACK_EXCEPTION (0xC000041D) instead of surfacing as a catchable error.
    /// It fired on the Reset button AND on every Source/Ref change, because all four call
    /// Reset_Measurement.
    /// <para>
    /// A single placeholder point does not help - one point is a zero-width range, which is just
    /// as unusable - and clearing IsLogarithmic does not stick, because the chart restores it when
    /// the points go away. So keep the real x values and set every y to NaN: a NaN y is stored as
    /// an EMPTY point, so the traces are invisible, while the axis still has a genuine positive
    /// range to scale from. This is the same path a normal refresh takes, which is why it is known
    /// to be safe.
    /// </para>
    /// <para>
    /// The old code kept a "Dummy" series for this same reason, but parked it at x = 0 - precisely
    /// the value a logarithmic axis cannot represent - which is why it looked like dead weight.
    /// </para>
    /// </remarks>
    protected void Blank_Frequency_Charts()
    {
        var Local_Config = this.Config;

        //Nothing has ever been plotted, so there is no frequency axis to blank against - and the
        //x axis has never been switched to logarithmic either, so emptying these is safe here.
        if (Local_Config == null)
        {
            this.Clear_Series(this.Chart_Mag);
            this.Clear_Series(this.Chart_Phase);
            return;
        }

        int Local_HalfLen = Local_Config.FFTSize / 2 + 1;
        if (Local_Config.FrequencySpan.Length != Local_HalfLen)
            return;

        var Local_Blank = new double[Local_HalfLen];
        Array.Fill(Local_Blank, double.NaN);

        int Local_MinHz = 1;
        int Local_MaxHz = Math.Max(2, Local_Config.SampleRate / 2);

        this.Plot_Mag_Chart(this.Chart_Mag, Local_MinHz, Local_MaxHz,
            Local_Config.FrequencySpan, Local_Blank, Local_Blank);

        this.Plot_Phase_Chart(this.Chart_Phase, Local_MinHz, Local_MaxHz,
            Local_Config.FrequencySpan, Local_Blank, Local_Blank);
    }

    protected void Reset_Averaging()
    {
        this._SxyA = null;
        this._SxyB = null;
        this._Sxx = null;
        this._SyyA = null;
        this._SyyB = null;
        this._TfAvgFrames = 0;

        this.EnsureTransferStateInitialized();
        this.EnsureTempBuffers();
    }

    /// <summary>
    /// Empties every trace on a chart whose x axis is LINEAR.
    /// </summary>
    /// <param name="chartControl">The chart to blank.</param>
    /// <remarks>
    /// Do not use this on a chart with a logarithmic x axis - see <see cref="Bind_Trace"/> for why
    /// leaving one of those with no points crashes the process from inside a paint.
    /// </remarks>
    protected void Clear_Series(Chart chartControl)
    {
        for (int i = 0; i < chartControl.Series.Count; i++)
            chartControl.Series[i].Points.Clear();
    }

    /// <summary>
    /// Replaces a trace's contents.
    /// </summary>
    /// <param name="series">The trace to bind.</param>
    /// <param name="xData">The x values.</param>
    /// <param name="yData">The y values; a NaN means "nothing to show at this x".</param>
    /// <remarks>
    /// A NaN y is kept as an EMPTY point: the trace shows a gap there, but the x value stays in
    /// the chart. That distinction is what makes an all-NaN trace a safe way to blank a chart,
    /// whereas emptying the series outright is not - see <see cref="Blank_Frequency_Charts"/>.
    /// </remarks>
    protected void Bind_Trace(Series series, double[] xData, double[] yData)
    {
        series.Points.Clear();
        series.Points.DataBindXY(xData, yData);
    }

    protected void Clear_Stats()
    {
        this.Coherence_LBL.Text = "-";
        this.Coherence1_LBL.Text = "-";
        this.Coherence2_LBL.Text = "-";
        this.Delay1_LBL.Text = "-";
        this.Delay2_LBL.Text = "-";
        this.DelayDelta_LBL.Text = "-";
        this.Frames_LBL.Text = "0";
    }
    #endregion

    #region Frame Accumulation
    /// <summary>
    /// Consumes every whole 50%-overlapped frame currently available and folds it into the
    /// averaged spectra.
    /// </summary>
    /// <param name="config">The snapshot to analyse under. Never re-read the field.</param>
    /// <returns>The number of frames accumulated.</returns>
    protected int Accumulate_Available_Frames(AnalysisConfig config)
    {
        int Local_FFTSize = config.FFTSize;

        var Local_A = this._tmpA;
        var Local_B = this._tmpB;
        var Local_Ref = this._tmpRef;

        if (Local_A == null || Local_B == null || Local_Ref == null ||
            Local_A.Length != Local_FFTSize)
            return 0;

        //50% overlap is the right hop for a Hann window: consecutive frames overlap, so no audio
        //is under-weighted by the window taper and the averaged estimate converges evenly.
        int Local_Hop = Math.Max(1, Local_FFTSize / 2);
        int Local_Frames = 0;

        while (Local_Frames < MaxFramesPerTick)
        {
            //See AnalysisConfig.SyncRoot: the three buffers advance as a group or not at all.
            lock (config.SyncRoot)
            {
                int Local_Available = Math.Min(config.BufferA.Count,
                                      Math.Min(config.BufferB.Count, config.BufferRef.Count));

                if (Local_Available < Local_FFTSize)
                    break;

                _ = config.BufferA.Peek(Local_A, 0, Local_FFTSize);
                _ = config.BufferB.Peek(Local_B, 0, Local_FFTSize);
                _ = config.BufferRef.Peek(Local_Ref, 0, Local_FFTSize);

                config.BufferA.Advance(Local_Hop);
                config.BufferB.Advance(Local_Hop);
                config.BufferRef.Advance(Local_Hop);
            }

            this.ComputeFFTs(config, Local_A, Local_B, Local_Ref,
                             out var Local_A_fft, out var Local_B_fft, out var Local_R_fft);

            this.UpdateAveragedSpectra(Local_A_fft, Local_B_fft, Local_R_fft);
            Local_Frames++;
        }

        return Local_Frames;
    }

    /// <summary>
    /// Transforms one frame of each channel.
    /// <para>
    /// DEFECT FIX: the caller used to hand this ONE ASIO block (512-1024 samples) padded out to
    /// FFTSize with zeros, together with a window of the full FFTSize. The real audio therefore
    /// landed in the first quarter of a 4096 point Hann window - the part that rises from zero -
    /// so every frame was severely attenuated and shape-distorted, the resulting spectra were
    /// dominated by the window rather than the signal, and the low frequency phase estimate was
    /// effectively random from frame to frame. That is what produced the near-vertical phase
    /// scribble and the low coherence that then punched the mask gaps. Frames now come from a
    /// ring buffer and are FFTSize samples of genuinely contiguous audio.
    /// </para>
    /// </summary>
    protected void ComputeFFTs(
        AnalysisConfig config,
        double[] dataA, double[] dataB, double[] dataRef,
        out Complex[] A_fft, out Complex[] B_fft, out Complex[] R_fft)
    {
        //The *_Into overloads are bit-identical to the allocating ones; at up to 16 frames per
        //tick the allocating form churned ~3 MB of Complex[] per second for nothing.
        A_fft = config.Fft.Perform_FFT_Into(dataA, config.Window, this._fftA!);
        B_fft = config.Fft.Perform_FFT_Into(dataB, config.Window, this._fftB!);
        R_fft = config.Fft.Perform_FFT_Into(dataRef, config.Window, this._fftRef!);
    }
    #endregion

    #region Averaging spectra
    /// <summary>
    /// The averaging coefficient to apply to the next frame.
    /// <para>
    /// For the first frames this is 1/n, i.e. a plain cumulative mean, easing into the
    /// configured exponential coefficient once n exceeds 1/alpha. A pure exponential average
    /// starting from zeroed state is biased towards zero for its first ~1/alpha frames: at the
    /// default alpha of 0.005 that is 200 frames during which every derived quantity was
    /// meaningless, which is what the old two-frame "warm-up" was papering over.
    /// </para>
    /// </summary>
    protected double GetEffectiveAlpha()
    {
        double Local_Alpha = this.alpha;
        if (!double.IsFinite(Local_Alpha) || Local_Alpha <= 0)
            Local_Alpha = 0.005;
        if (Local_Alpha > 1)
            Local_Alpha = 1;

        double Local_Cumulative = 1.0 / (this._TfAvgFrames + 1);
        return Math.Max(Local_Alpha, Local_Cumulative);
    }

    protected void UpdateAveragedSpectra(Complex[] A_fft, Complex[] B_fft, Complex[] R_fft)
    {
        double Local_Alpha = this.GetEffectiveAlpha();
        double Local_Retain = 1.0 - Local_Alpha;

        int Local_Length = Math.Min(this.FFTSize,
                           Math.Min(A_fft.Length, Math.Min(B_fft.Length, R_fft.Length)));

        for (int k = 0; k < Local_Length; k++)
        {
            Complex Local_X = R_fft[k];
            Complex Local_YA = A_fft[k];
            Complex Local_YB = B_fft[k];

            Complex Local_SxyA = Local_YA * Complex.Conjugate(Local_X);
            Complex Local_SxyB = Local_YB * Complex.Conjugate(Local_X);

            double Local_Sxx = Local_X.Real * Local_X.Real + Local_X.Imaginary * Local_X.Imaginary;
            double Local_SyyA = Local_YA.Real * Local_YA.Real + Local_YA.Imaginary * Local_YA.Imaginary;
            double Local_SyyB = Local_YB.Real * Local_YB.Real + Local_YB.Imaginary * Local_YB.Imaginary;

            if (!double.IsFinite(Local_Sxx)) Local_Sxx = 0.0;
            if (!double.IsFinite(Local_SyyA)) Local_SyyA = 0.0;
            if (!double.IsFinite(Local_SyyB)) Local_SyyB = 0.0;

            if (!double.IsFinite(Local_SxyA.Real) || !double.IsFinite(Local_SxyA.Imaginary))
                Local_SxyA = Complex.Zero;

            if (!double.IsFinite(Local_SxyB.Real) || !double.IsFinite(Local_SxyB.Imaginary))
                Local_SxyB = Complex.Zero;

            this._SxyA![k] = Local_Retain * this._SxyA[k] + Local_Alpha * Local_SxyA;
            this._SxyB![k] = Local_Retain * this._SxyB[k] + Local_Alpha * Local_SxyB;

            this._Sxx![k] = Local_Retain * this._Sxx[k] + Local_Alpha * Local_Sxx;
            this._SyyA![k] = Local_Retain * this._SyyA[k] + Local_Alpha * Local_SyyA;
            this._SyyB![k] = Local_Retain * this._SyyB[k] + Local_Alpha * Local_SyyB;
        }

        this._TfAvgFrames++;
    }
    #endregion

    #region Display
    protected void Display_Align_Results(AnalysisConfig config)
    {
        double Local_SampleRate = config.SampleRate;
        int Local_HalfLen = config.FFTSize / 2 + 1;

        int Local_MinHz = 1;
        int Local_MaxHz = Math.Max(2, (int)(Local_SampleRate / 2.0));

        bool Local_CoherenceReady = this.IsCoherenceReady();

        this.ComputeAdaptiveEpsilons(Local_HalfLen,
            out double Local_EpsSxx, out double Local_EpsSyyA, out double Local_EpsSyyB);

        this.ComputeTransferFunctions(Local_EpsSxx,
            out Complex[] Local_H_A, out Complex[] Local_H_B, out bool[] Local_ValidH);

        this.ComputeCoherence(Local_HalfLen, Local_EpsSxx, Local_EpsSyyA, Local_EpsSyyB,
            out double[] Local_CohA, out double[] Local_CohB);

        double Local_MeanCohA = this.ComputeWeightedMeanCoherence(Local_CohA, Local_HalfLen);
        double Local_MeanCohB = this.ComputeWeightedMeanCoherence(Local_CohB, Local_HalfLen);

        this.PrepareMagPhaseForPlot(
            Local_H_A, Local_H_B, Local_ValidH,
            Local_CohA, Local_CohB, Local_CoherenceReady,
            out double[] Local_MagA_dB, out double[] Local_MagB_dB,
            out double[] Local_PhaseA_deg, out double[] Local_PhaseB_deg);

        this.PrepareImpulseResponses(
            config, Local_H_A, Local_H_B,
            out double[] Local_tMs, out double[] Local_IrA, out double[] Local_IrB,
            out double Local_DelayMsA, out double Local_DelayMsB);

        this.Plot_Mag_Chart(this.Chart_Mag, Local_MinHz, Local_MaxHz,
            config.FrequencySpan, Local_MagA_dB, Local_MagB_dB);

        this.Plot_Phase_Chart(this.Chart_Phase, Local_MinHz, Local_MaxHz,
            config.FrequencySpan, Local_PhaseA_deg, Local_PhaseB_deg);

        this.Plot_IR_Chart(this.Chart_IR, Local_tMs, Local_IrA, Local_IrB);

        this.Coherence_LBL.Text = this.cohMin <= 0 ? "off" : this.cohMin.ToString("F2");
        this.Coherence1_LBL.Text = Local_MeanCohA.ToString("F4");
        this.Coherence2_LBL.Text = Local_MeanCohB.ToString("F4");
        this.Delay1_LBL.Text = Local_DelayMsA.ToString("F4");
        this.Delay2_LBL.Text = Local_DelayMsB.ToString("F4");
        this.DelayDelta_LBL.Text = (Local_DelayMsA - Local_DelayMsB).ToString("F4");
        this.Frames_LBL.Text = this._TfAvgFrames.ToString();
    }
    #endregion

    #region Compute epsilon
    /// <summary>
    /// Power thresholds below which a bin carries no usable signal. Derived from the observed
    /// spectra rather than fixed, so they track the input level instead of gating on it.
    /// </summary>
    protected void ComputeAdaptiveEpsilons(int halfLen, out double epsSxx, out double epsSyyA, out double epsSyyB)
    {
        double Local_MaxSxx = 0.0, Local_MaxSyyA = 0.0, Local_MaxSyyB = 0.0;

        int Local_End = Math.Min(halfLen, this._Sxx?.Length ?? 0);
        for (int i = 1; i < Local_End; i++)
        {
            double Local_Sxx = this._Sxx![i];
            double Local_SyyA = this._SyyA![i];
            double Local_SyyB = this._SyyB![i];

            if (double.IsFinite(Local_Sxx) && Local_Sxx > Local_MaxSxx) Local_MaxSxx = Local_Sxx;
            if (double.IsFinite(Local_SyyA) && Local_SyyA > Local_MaxSyyA) Local_MaxSyyA = Local_SyyA;
            if (double.IsFinite(Local_SyyB) && Local_SyyB > Local_MaxSyyB) Local_MaxSyyB = Local_SyyB;
        }

        epsSxx = Math.Max(this.epsFloor, Local_MaxSxx * 1e-12);
        epsSyyA = Math.Max(this.epsFloor, Local_MaxSyyA * 1e-12);
        epsSyyB = Math.Max(this.epsFloor, Local_MaxSyyB * 1e-12);
    }
    #endregion

    #region Transfer function
    /// <summary>
    /// H1 estimate of each source's transfer function relative to the Ref signal:
    /// H = Sxy / Sxx. Being a ratio it is independent of the window and of the transform's own
    /// scaling, so 0 dB on the magnitude chart is genuinely unity gain.
    /// </summary>
    protected void ComputeTransferFunctions(double epsSxx, out Complex[] H_A, out Complex[] H_B, out bool[] validH)
    {
        H_A = new Complex[this.FFTSize];
        H_B = new Complex[this.FFTSize];
        validH = new bool[this.FFTSize];

        for (int k = 0; k < this.FFTSize; k++)
        {
            double Local_Sxx = this._Sxx![k];

            if (!double.IsFinite(Local_Sxx) || Local_Sxx <= epsSxx)
            {
                H_A[k] = Complex.Zero;
                H_B[k] = Complex.Zero;
                validH[k] = false;
                continue;
            }

            H_A[k] = this._SxyA![k] / Local_Sxx;
            H_B[k] = this._SxyB![k] / Local_Sxx;
            validH[k] = true;
        }
    }
    #endregion

    #region Coherence
    protected bool IsCoherenceReady() => this._TfAvgFrames >= CoherenceWarmupFrames;

    protected void ComputeCoherence(
        int halfLen, double epsSxx, double epsSyyA, double epsSyyB,
        out double[] cohA, out double[] cohB)
    {
        cohA = new double[halfLen];
        cohB = new double[halfLen];

        int Local_End = Math.Min(halfLen, this._Sxx?.Length ?? 0);

        for (int i = 0; i < Local_End; i++)
        {
            double Local_Sxx = this._Sxx![i];
            double Local_SyyA = this._SyyA![i];
            double Local_SyyB = this._SyyB![i];

            if (!double.IsFinite(Local_Sxx)) Local_Sxx = 0.0;
            if (!double.IsFinite(Local_SyyA)) Local_SyyA = 0.0;
            if (!double.IsFinite(Local_SyyB)) Local_SyyB = 0.0;

            Complex Local_SxyA = this._SxyA![i];
            Complex Local_SxyB = this._SxyB![i];

            double Local_SxyA_mag2 = Local_SxyA.Real * Local_SxyA.Real + Local_SxyA.Imaginary * Local_SxyA.Imaginary;
            double Local_SxyB_mag2 = Local_SxyB.Real * Local_SxyB.Real + Local_SxyB.Imaginary * Local_SxyB.Imaginary;

            double Local_DenomA = Local_Sxx * Local_SyyA;
            double Local_DenomB = Local_Sxx * Local_SyyB;

            double Local_CohA = (Local_Sxx > epsSxx && Local_SyyA > epsSyyA && Local_DenomA > this.epsFloor)
                                ? Local_SxyA_mag2 / Local_DenomA : 0.0;

            double Local_CohB = (Local_Sxx > epsSxx && Local_SyyB > epsSyyB && Local_DenomB > this.epsFloor)
                                ? Local_SxyB_mag2 / Local_DenomB : 0.0;

            if (!double.IsFinite(Local_CohA)) Local_CohA = 0.0;
            if (!double.IsFinite(Local_CohB)) Local_CohB = 0.0;

            cohA[i] = Math.Clamp(Local_CohA, 0, 1);
            cohB[i] = Math.Clamp(Local_CohB, 0, 1);
        }
    }

    /// <summary>
    /// A single figure of merit for the whole measurement: coherence averaged over the band,
    /// weighted by how much Ref energy each bin actually carries.
    /// <para>
    /// This replaces a MAXIMUM. The maximum coherence over ~2000 bins is essentially always
    /// close to 1 whatever the state of the measurement, so it could not tell a good capture
    /// from a bad one - which is the only thing the reading is there for.
    /// </para>
    /// </summary>
    protected double ComputeWeightedMeanCoherence(double[] coh, int halfLen)
    {
        if (coh == null || this._Sxx == null)
            return 0.0;

        double Local_Num = 0.0;
        double Local_Den = 0.0;

        int Local_End = Math.Min(Math.Min(halfLen, coh.Length), this._Sxx.Length);
        for (int i = 1; i < Local_End; i++)
        {
            double Local_Weight = this._Sxx[i];
            double Local_Coh = coh[i];

            if (!double.IsFinite(Local_Weight) || Local_Weight <= 0.0 || !double.IsFinite(Local_Coh))
                continue;

            Local_Num += Local_Weight * Local_Coh;
            Local_Den += Local_Weight;
        }

        return Local_Den > 0.0 ? Local_Num / Local_Den : 0.0;
    }
    #endregion

    #region Mag/phase prep
    protected void PrepareMagPhaseForPlot(
        Complex[] H_A, Complex[] H_B, bool[] validH,
        double[] cohA, double[] cohB, bool coherenceReady,
        out double[] magA_dB, out double[] magB_dB,
        out double[] phaseA_deg, out double[] phaseB_deg)
    {
        int Local_HalfLen = this.FFTSize / 2 + 1;

        var Local_H_A_half = new Complex[Local_HalfLen];
        var Local_H_B_half = new Complex[Local_HalfLen];
        Array.Copy(H_A, Local_H_A_half, Local_HalfLen);
        Array.Copy(H_B, Local_H_B_half, Local_HalfLen);

        var Local_ValidHalf = new bool[Local_HalfLen];
        Array.Copy(validH, Local_ValidHalf, Local_HalfLen);

        var Local_ValidA = Local_ValidHalf;
        var Local_ValidB = Local_ValidHalf;

        //Optional fractional-octave smoothing. This is a DISPLAY transform only: the impulse
        //response and the delay readouts are built from the UNSMOOTHED transfer function, because
        //narrowing the spectrum broadens the impulse response and would blunt the very peak the
        //delay is measured from.
        if (this.smoothingEnabled && this.smoothingOctaveFraction > 0)
        {
            Local_H_A_half = this.SmoothComplexFractionalOctave(
                Local_H_A_half, Local_ValidHalf, this.smoothingOctaveFraction, out Local_ValidA);

            Local_H_B_half = this.SmoothComplexFractionalOctave(
                Local_H_B_half, Local_ValidHalf, this.smoothingOctaveFraction, out Local_ValidB);
        }

        magA_dB = DSPLib.DSP.ConvertMagnitude.ToMagnitudeDBV(
                      DSPLib.DSP.ConvertComplex.ToMagnitude(Local_H_A_half));
        magB_dB = DSPLib.DSP.ConvertMagnitude.ToMagnitudeDBV(
                      DSPLib.DSP.ConvertComplex.ToMagnitude(Local_H_B_half));

        //Atan2 already returns (-180, 180]. The old code unwrapped this and then re-wrapped it to
        //(-180, 180] again, which is the identity - the whole pass was a no-op. A wrapped display
        //is what every other alignment tool shows, so keep it wrapped and drop the round trip.
        phaseA_deg = DSPLib.DSP.ConvertComplex.ToPhaseDegrees(Local_H_A_half);
        phaseB_deg = DSPLib.DSP.ConvertComplex.ToPhaseDegrees(Local_H_B_half);

        //The mask deliberately gates on the RAW coherence. Smoothing coherence would average its
        //dips away and report confidence the measurement does not have.
        bool Local_MaskOn = coherenceReady && this.cohMin > 0;

        for (int i = 0; i < Local_HalfLen; i++)
        {
            //DC has no position on a logarithmic frequency axis.
            if (i == 0)
            {
                magA_dB[i] = double.NaN;
                magB_dB[i] = double.NaN;
                phaseA_deg[i] = double.NaN;
                phaseB_deg[i] = double.NaN;
                continue;
            }

            //A bin the Ref signal carries no energy in has no measurable transfer function.
            if (!Local_ValidA[i])
            {
                magA_dB[i] = double.NaN;
                phaseA_deg[i] = double.NaN;
            }

            if (!Local_ValidB[i])
            {
                magB_dB[i] = double.NaN;
                phaseB_deg[i] = double.NaN;
            }

            if (!Local_MaskOn)
                continue;

            //A bin whose coherence is too low to trust for phase is not trustworthy for
            //magnitude either - noise only ever ADDS to the magnitude estimate - so mask both or
            //neither, rather than leaving a magnitude trace with no phase underneath it.
            if (cohA[i] < this.cohMin)
            {
                magA_dB[i] = double.NaN;
                phaseA_deg[i] = double.NaN;
            }

            if (cohB[i] < this.cohMin)
            {
                magB_dB[i] = double.NaN;
                phaseB_deg[i] = double.NaN;
            }
        }
    }
    #endregion

    #region Fractional-octave complex smoothing
    /// <summary>
    /// Averages a transfer function over a constant-percentage bandwidth centred on each bin.
    /// </summary>
    /// <remarks>
    /// The averaging is COMPLEX - real and imaginary parts averaged together - which is the only
    /// correct way to smooth a transfer function. Averaging magnitude and WRAPPED phase separately
    /// would average across the +/-180 degree seam, so two neighbouring bins at +179 and -179
    /// degrees (the same direction) would come out near 0 degrees (the opposite direction) and
    /// destroy exactly the phase relationship this window exists to show.
    /// <para>
    /// Bin spacing is linear while the band is proportional to frequency, so the band is a single
    /// bin wide in the bass - where the resolution is already coarser than the fraction, and
    /// nothing is altered - and hundreds of bins wide at the top, where a noise measurement is at
    /// its noisiest. That is the whole point: it cleans up the treble without inventing detail in
    /// the bass.
    /// </para>
    /// <para>
    /// Running sums make every output bin O(1), so the cost does not grow with the chosen width.
    /// </para>
    /// </remarks>
    /// <param name="halfSpectrum">The DC..Nyquist transfer function.</param>
    /// <param name="valid">Which bins carry a real measurement. Invalid bins are excluded, so
    /// they neither drag the average towards zero nor make it look better sampled than it is.</param>
    /// <param name="octaveFraction">Band width in octaves, e.g. 1.0/12.0 for 1/12 octave.</param>
    /// <param name="validSmoothed">Which output bins ended up with at least one valid contributor.</param>
    /// <returns>A new array; the input is not modified.</returns>
    protected Complex[] SmoothComplexFractionalOctave(
        Complex[] halfSpectrum, bool[] valid, double octaveFraction, out bool[] validSmoothed)
    {
        int Local_N = halfSpectrum.Length;
        var Local_Out = new Complex[Local_N];
        validSmoothed = new bool[Local_N];

        if (Local_N == 0)
            return Local_Out;

        if (!double.IsFinite(octaveFraction) || octaveFraction <= 0.0)
        {
            Array.Copy(halfSpectrum, Local_Out, Local_N);
            Array.Copy(valid, validSmoothed, Local_N);
            return Local_Out;
        }

        //Half the band on each side of the centre bin.
        double Local_Ratio = Math.Pow(2.0, octaveFraction / 2.0);

        var Local_CumRe = new double[Local_N + 1];
        var Local_CumIm = new double[Local_N + 1];
        var Local_CumCount = new int[Local_N + 1];

        for (int i = 0; i < Local_N; i++)
        {
            bool Local_Ok = valid[i] &&
                            double.IsFinite(halfSpectrum[i].Real) &&
                            double.IsFinite(halfSpectrum[i].Imaginary);

            Local_CumRe[i + 1] = Local_CumRe[i] + (Local_Ok ? halfSpectrum[i].Real : 0.0);
            Local_CumIm[i + 1] = Local_CumIm[i] + (Local_Ok ? halfSpectrum[i].Imaginary : 0.0);
            Local_CumCount[i + 1] = Local_CumCount[i] + (Local_Ok ? 1 : 0);
        }

        //DC has no centre frequency to build a proportional band around, and it is never plotted.
        Local_Out[0] = halfSpectrum[0];
        validSmoothed[0] = valid[0];

        for (int i = 1; i < Local_N; i++)
        {
            int Local_Lo = (int)Math.Ceiling(i / Local_Ratio);
            int Local_Hi = (int)Math.Floor(i * Local_Ratio);

            if (Local_Lo < 1) Local_Lo = 1;
            if (Local_Hi > Local_N - 1) Local_Hi = Local_N - 1;

            //Low down, the proportional band is narrower than one bin. Averaging that bin with
            //itself is the correct answer there, not an error condition.
            if (Local_Lo > i) Local_Lo = i;
            if (Local_Hi < i) Local_Hi = i;

            int Local_Count = Local_CumCount[Local_Hi + 1] - Local_CumCount[Local_Lo];
            if (Local_Count <= 0)
            {
                Local_Out[i] = Complex.Zero;
                validSmoothed[i] = false;
                continue;
            }

            double Local_Inv = 1.0 / Local_Count;
            Local_Out[i] = new Complex(
                (Local_CumRe[Local_Hi + 1] - Local_CumRe[Local_Lo]) * Local_Inv,
                (Local_CumIm[Local_Hi + 1] - Local_CumIm[Local_Lo]) * Local_Inv);

            validSmoothed[i] = true;
        }

        return Local_Out;
    }
    #endregion

    #region Impulse response and delay
    /// <summary>
    /// Inverse-transforms both transfer functions into impulse responses, measures each source's
    /// arrival time relative to the Ref signal, and shifts both for display on an axis whose
    /// origin is the Ref arrival.
    /// </summary>
    /// <remarks>
    /// H = Sxy/Sxx is the deconvolved Ref -> Source response, so if the source is the Ref delayed
    /// by d samples then H[k] = exp(-j2*pi*k*d/N) and the impulse response is a delta at index d.
    /// A source that ARRIVES LATER than Ref therefore gives a POSITIVE delay, and one that leads
    /// Ref appears near index N-1, i.e. at a negative time.
    /// </remarks>
    protected void PrepareImpulseResponses(
        AnalysisConfig config,
        Complex[] H_A, Complex[] H_B,
        out double[] tMs_centered,
        out double[] irA_disp, out double[] irB_disp,
        out double delayMsA, out double delayMsB)
    {
        int Local_N = config.FFTSize;
        double Local_SampleRate = config.SampleRate;

        double[] Local_IrA = config.Fft.Perform_IFFT_Into(H_A, this._irA!);
        double[] Local_IrB = config.Fft.Perform_IFFT_Into(H_B, this._irB!);

        //DEFECT FIX: the delay was found with a peak search restricted to [0, N/2] and commented
        //"causal peak search only => delays cannot be negative". Nothing here is causal: index 0
        //is the Ref arrival, not the start of time, and a source that leads the Ref by even one
        //sample peaks at index N-1. That peak fell outside the search window, index 0 won by
        //default, and the readout was a hard 0.0000 ms - which is exactly the "no delay in the
        //signal, which I don't believe is correct" symptom. Search the whole circle and let the
        //answer be signed.
        delayMsA = this.EstimateDelayMs(Local_IrA, Local_SampleRate);
        delayMsB = this.EstimateDelayMs(Local_IrB, Local_SampleRate);

        //Each response is scaled to its own peak. Absolute IFFT scaling is therefore irrelevant,
        //and a quiet source stays readable next to a loud one - this chart is for reading
        //TIMING, and the magnitude chart is where relative level belongs.
        this.NormalizeToUnitPeakInPlace(Local_IrA);
        this.NormalizeToUnitPeakInPlace(Local_IrB);

        for (int i = 0; i < Local_N; i++)
        {
            Local_IrA[i] *= 100.0;
            Local_IrB[i] *= 100.0;
        }

        //Put sample 0 - the Ref arrival - at the centre of the axis, so the wrapped negative-time
        //tail lands immediately to the LEFT of 0 ms where it belongs.
        int Local_Center = Local_N / 2;
        irA_disp = this.CircularShift(Local_IrA, Local_Center);
        irB_disp = this.CircularShift(Local_IrB, Local_Center);

        double Local_MsPerSample = Local_SampleRate > 0 ? 1000.0 / Local_SampleRate : 0.0;
        tMs_centered = new double[Local_N];
        for (int i = 0; i < Local_N; i++)
            tMs_centered[i] = (i - Local_Center) * Local_MsPerSample;
    }

    /// <summary>
    /// Signed arrival time of an impulse response, refined to a fraction of a sample.
    /// </summary>
    /// <param name="ir">The impulse response, sample 0 being zero delay.</param>
    /// <param name="sampleRate">The sample rate the response was measured at.</param>
    /// <returns>Milliseconds; positive means the source arrives AFTER the Ref signal.</returns>
    protected double EstimateDelayMs(double[] ir, double sampleRate)
    {
        if (ir == null || ir.Length == 0 || sampleRate <= 0)
            return 0.0;

        int Local_Peak = this.ArgMaxAbs(ir);
        double Local_Refined = this.RefinePeakIndex(ir, Local_Peak);

        return this.PeakIndexToSignedDelayMs(Local_Refined, sampleRate, ir.Length);
    }

    /// <summary>
    /// Sub-sample peak position by fitting a parabola through the peak and its two neighbours.
    /// <para>
    /// Without this the readout is quantised to one sample - 20.8 us at 48 kHz - which is 7 mm of
    /// path length and coarser than the alignment this window exists to set.
    /// </para>
    /// </summary>
    /// <param name="x">The signal, treated as circular so a peak at index 0 keeps both neighbours.</param>
    /// <param name="peakIndex">The integer peak index.</param>
    /// <returns>The refined, possibly fractional, peak index.</returns>
    protected double RefinePeakIndex(double[] x, int peakIndex)
    {
        if (x == null || x.Length < 3)
            return peakIndex;

        int Local_N = x.Length;
        double Local_Prev = Math.Abs(x[(peakIndex - 1 + Local_N) % Local_N]);
        double Local_Peak = Math.Abs(x[peakIndex]);
        double Local_Next = Math.Abs(x[(peakIndex + 1) % Local_N]);

        double Local_Denom = Local_Prev - 2.0 * Local_Peak + Local_Next;
        if (!double.IsFinite(Local_Denom) || Local_Denom == 0.0)
            return peakIndex;

        double Local_Delta = 0.5 * (Local_Prev - Local_Next) / Local_Denom;

        //A well-formed peak puts the vertex inside its own sample. Anything else means the three
        //samples were not a peak at all, so trust the integer index.
        if (!double.IsFinite(Local_Delta) || Math.Abs(Local_Delta) > 0.5)
            return peakIndex;

        return peakIndex + Local_Delta;
    }
    #endregion

    #region Helper Functions

    protected double PeakAbs(double[] x)
    {
        double Local_Peak = 0.0;
        for (int i = 0; i < x.Length; i++)
        {
            double Local_Abs = Math.Abs(x[i]);
            if (double.IsFinite(Local_Abs) && Local_Abs > Local_Peak)
                Local_Peak = Local_Abs;
        }
        return Local_Peak;
    }

    protected void NormalizeToUnitPeakInPlace(double[] x)
    {
        double Local_Peak = this.PeakAbs(x);
        if (Local_Peak <= 0.0)
            return;

        double Local_Inv = 1.0 / Local_Peak;
        for (int i = 0; i < x.Length; i++)
            x[i] *= Local_Inv;
    }

    protected double[] CircularShift(double[] x, int shift)
    {
        int Local_N = x.Length;
        if (Local_N == 0)
            return x;

        // normalize shift to [0..n-1]
        shift %= Local_N;
        if (shift < 0)
            shift += Local_N;

        var Local_Y = new double[Local_N];
        for (int i = 0; i < Local_N; i++)
            Local_Y[(i + shift) % Local_N] = x[i];

        return Local_Y;
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
            this._fftA = new Complex[this.FFTSize];
            this._fftB = new Complex[this.FFTSize];
            this._fftRef = new Complex[this.FFTSize];
            this._irA = new double[this.FFTSize];
            this._irB = new double[this.FFTSize];
        }
    }

    protected int ArgMaxAbs(double[] x)
    {
        if (x == null || x.Length == 0)
            return 0;

        int Local_Peak = 0;
        double Local_MaxAbs = 0.0;

        for (int i = 0; i < x.Length; i++)
        {
            double Local_Abs = Math.Abs(x[i]);
            if (Local_Abs > Local_MaxAbs)
            {
                Local_MaxAbs = Local_Abs;
                Local_Peak = i;
            }
        }

        return Local_Peak;
    }

    /// <summary>
    /// Converts a position in the circular impulse response into a signed delay.
    /// </summary>
    /// <param name="peakIndex">The peak position, possibly fractional after sub-sample refinement.</param>
    /// <param name="sampleRate">The sample rate the response was measured at.</param>
    /// <param name="fftSize">The length of the impulse response.</param>
    /// <returns>Milliseconds; positive means the source arrives AFTER the Ref signal.</returns>
    protected double PeakIndexToSignedDelayMs(double peakIndex, double sampleRate, int fftSize)
    {
        //Indices above N/2 are the wrapped NEGATIVE-time half of the circular response, i.e. a
        //source that arrives BEFORE the Ref signal.
        double Local_SignedSamples = peakIndex > fftSize / 2.0 ? peakIndex - fftSize : peakIndex;

        return sampleRate > 0 ? 1000.0 * Local_SignedSamples / sampleRate : 0.0;
    }

    /// <summary>
    /// Applies an explicit axis range without ever leaving Minimum above Maximum.
    /// <para>
    /// DEFECT FIX: the plot methods assigned Minimum and then Maximum from two text boxes. If the
    /// pending Minimum was above the CURRENT Maximum the chart threw, and because these charts run
    /// with SuppressExceptions the failure was invisible - the axis silently kept a stale range.
    /// Clearing both to auto first makes the assignment order irrelevant.
    /// </para>
    /// </summary>
    protected static void SetAxisRange(Axis axis, double min, double max, double fallbackMin, double fallbackMax)
    {
        if (!double.IsFinite(min) || !double.IsFinite(max) || min >= max)
        {
            min = fallbackMin;
            max = fallbackMax;
        }

        axis.Minimum = double.NaN;
        axis.Maximum = double.NaN;
        axis.Minimum = min;
        axis.Maximum = max;
    }

    protected static double ParseOr(TextBox box, double fallback)
    {
        return double.TryParse(box.Text, out double Local_Value) && double.IsFinite(Local_Value)
               ? Local_Value : fallback;
    }
    #endregion

    #region Charts
    protected void Plot_Mag_Chart(Chart chartControl, double min, double max, double[] xData, double[] magData1, double[] magData2)
    {
        chartControl.SuspendLayout();

        var Local_Area = chartControl.ChartAreas[0];

        // Configure magnitude axis (primary Y-axis)
        Local_Area.AxisY.Interval = 12;
        Local_Area.AxisY.IntervalType = DateTimeIntervalType.Number;
        Local_Area.AxisY.MinorGrid.Enabled = true;
        Local_Area.AxisY.MinorGrid.Interval = 3;
        Local_Area.AxisY.Title = "Magnitude (dB)";

        SetAxisRange(Local_Area.AxisY,
            ParseOr(this.mindB_TXT, -48), ParseOr(this.maxdB_TXT, 12), -48, 12);

        // Configure X-axis (frequency)
        Local_Area.AxisX.IntervalType = DateTimeIntervalType.Number;
        Local_Area.AxisX.MinorGrid.Enabled = true;
        Local_Area.AxisX.MinorGrid.Interval = 1;
        Local_Area.AxisX.IsLogarithmic = true;
        Local_Area.AxisX.Title = "Frequency (Hz)";
        SetAxisRange(Local_Area.AxisX, min, max, 1, 20000);

        var Local_S1 = chartControl.Series["Series1"];
        Local_S1.YAxisType = AxisType.Primary;
        Local_S1.ChartType = SeriesChartType.Line;
        Local_S1.Color = Source1_Color;
        Local_S1.BorderWidth = 2;

        var Local_S2 = chartControl.Series["Series2"];
        Local_S2.YAxisType = AxisType.Secondary;
        Local_S2.ChartType = SeriesChartType.Line;
        Local_S2.Color = Source2_Color;
        Local_S2.BorderWidth = 2;

        // Secondary Y-axis mirrors the primary so the right hand scale reads the same.
        Local_Area.AxisY2.Title = "Magnitude2 (dB)";
        Local_Area.AxisY2.MajorGrid.Enabled = false;
        Local_Area.AxisY2.MinorGrid.Enabled = false;
        Local_Area.AxisY2.Interval = Local_Area.AxisY.Interval;
        Local_Area.AxisY2.IntervalType = Local_Area.AxisY.IntervalType;
        SetAxisRange(Local_Area.AxisY2,
            Local_Area.AxisY.Minimum, Local_Area.AxisY.Maximum, -48, 12);

        this.Bind_Trace(Local_S1, xData, magData1);
        this.Bind_Trace(Local_S2, xData, magData2);

        Local_S1.Enabled = true;
        Local_S2.Enabled = true;

        chartControl.ResumeLayout();
    }

    protected void Plot_Phase_Chart(Chart chartControl, double min, double max, double[] xData, double[] phaseData1, double[] phaseData2)
    {
        chartControl.SuspendLayout();

        var Local_Area = chartControl.ChartAreas[0];

        // Configure phase axis (primary Y-axis)
        Local_Area.AxisY.IntervalType = DateTimeIntervalType.Number;
        Local_Area.AxisY.MajorGrid.Enabled = true;
        Local_Area.AxisY.MinorGrid.Enabled = false;
        Local_Area.AxisY.Interval = 90;
        Local_Area.AxisY.Title = "Phase1 (Degrees)";
        SetAxisRange(Local_Area.AxisY, -180, 180, -180, 180);

        // Configure X-axis (frequency)
        Local_Area.AxisX.IntervalType = DateTimeIntervalType.Number;
        Local_Area.AxisX.MinorGrid.Enabled = true;
        Local_Area.AxisX.MinorGrid.Interval = 1;
        Local_Area.AxisX.IsLogarithmic = true;
        Local_Area.AxisX.Title = "Frequency (Hz)";
        SetAxisRange(Local_Area.AxisX, min, max, 1, 20000);

        var Local_S1 = chartControl.Series["Series1"];
        Local_S1.YAxisType = AxisType.Primary;
        Local_S1.ChartType = SeriesChartType.Line;
        Local_S1.Color = Source1_Color;
        Local_S1.BorderWidth = 2;

        var Local_S2 = chartControl.Series["Series2"];
        Local_S2.YAxisType = AxisType.Secondary;
        Local_S2.ChartType = SeriesChartType.Line;
        Local_S2.Color = Source2_Color;
        Local_S2.BorderWidth = 2;

        Local_Area.AxisY2.Title = "Phase2 (Degrees)";
        Local_Area.AxisY2.MajorGrid.Enabled = false;
        Local_Area.AxisY2.MinorGrid.Enabled = false;
        Local_Area.AxisY2.Interval = Local_Area.AxisY.Interval;
        Local_Area.AxisY2.IntervalType = Local_Area.AxisY.IntervalType;
        SetAxisRange(Local_Area.AxisY2, -180, 180, -180, 180);

        this.Bind_Trace(Local_S1, xData, phaseData1);
        this.Bind_Trace(Local_S2, xData, phaseData2);

        Local_S1.Enabled = true;
        Local_S2.Enabled = true;

        chartControl.ResumeLayout();
    }

    protected void Plot_IR_Chart(Chart chartControl, double[] tMs, double[] irA, double[] irB)
    {
        chartControl.SuspendLayout();

        var Local_Area = chartControl.ChartAreas[0];

        // --- 0 ms vertical reference line: this IS the Ref signal's arrival ---
        const string Local_ZeroLineName = "ZeroMsLine";

        // remove existing one if present (so it doesn't accumulate every refresh)
        for (int i = Local_Area.AxisX.StripLines.Count - 1; i >= 0; i--)
        {
            if (Local_Area.AxisX.StripLines[i].Tag as string == Local_ZeroLineName)
                Local_Area.AxisX.StripLines.RemoveAt(i);
        }

        Local_Area.AxisX.StripLines.Add(new StripLine
        {
            Tag = Local_ZeroLineName,
            Interval = 0,          // required for a single line
            IntervalOffset = 0.0,  // x = 0 ms
            StripWidth = 0.0,      // 0 => line (not a band)
            BorderColor = Ref_Color,
            BorderWidth = 2,
            BorderDashStyle = ChartDashStyle.Solid
        });

        // Y axis: each response normalised to its own peak, so -100% .. +100%
        Local_Area.AxisY.Title = "Impulse Response (%)";
        Local_Area.AxisY.MajorGrid.Enabled = true;
        Local_Area.AxisY.MinorGrid.Enabled = false;
        Local_Area.AxisY.Interval = 20;
        Local_Area.AxisY.IsReversed = false;
        SetAxisRange(Local_Area.AxisY, -100, 100, -100, 100);

        // X axis: time
        Local_Area.AxisX.Title = "Time (ms)";
        Local_Area.AxisX.IsLogarithmic = false;
        Local_Area.AxisX.MajorGrid.Enabled = true;
        Local_Area.AxisX.MajorGrid.Interval = 1;
        Local_Area.AxisX.MinorGrid.Enabled = true;
        Local_Area.AxisX.MinorGrid.Interval = 0.1;
        SetAxisRange(Local_Area.AxisX,
            ParseOr(this.min_ms_TXT, -2), ParseOr(this.max_ms_TXT, 2), -2, 2);

        var Local_S1 = chartControl.Series["Series1"];
        Local_S1.ChartType = SeriesChartType.Line;
        Local_S1.Color = Source1_Color;
        Local_S1.BorderWidth = 2;
        Local_S1.YAxisType = AxisType.Primary;

        var Local_S2 = chartControl.Series["Series2"];
        Local_S2.ChartType = SeriesChartType.Line;
        Local_S2.Color = Source2_Color;
        Local_S2.BorderWidth = 2;
        Local_S2.YAxisType = AxisType.Primary;

        this.Bind_Trace(Local_S1, tMs, irA);
        this.Bind_Trace(Local_S2, tMs, irB);

        Local_S1.Enabled = true;
        Local_S2.Enabled = true;

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
