#nullable enable

namespace BassThatHz_ASIO_DSP_Processor.GUI.Controls;

#region Usings
using GUI.Forms;
using NAudio.Utils;
using System;
using System.ComponentModel;
using System.Runtime.Versioning;
using System.Windows.Forms;
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
// 
/// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
/// IMPLIED, INCLUDING BUT NOT LIMITED TO FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
/// IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
/// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
/// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
/// SOFTWARE. ENFORCEABLE PORTIONS SHALL REMAIN IF NOT FOUND CONTRARY UNDER LAW.
/// </summary>
public partial class BTH_VolumeLevelControl : UserControl
{
    #region Variables
    public DSP_Stream? Stream;
    protected double Input_StreamVolume = 0;
    protected IStreamItem? InputChannel;
    protected IStreamItem? OutputChannel;

    /// <summary>
    /// Clip threshold in dB, i.e. 0 dBFS.
    /// <para>
    /// DEFECT FIX: this used to be 1 while every value compared against it
    /// (<see cref="Input_DB"/>, <see cref="Input_DB_Peak"/>, ...) is in dB, so the box only lit
    /// at +1 dBFS - unreachable for samples clamped to +/-1.0, which is 0 dBFS exactly. That is
    /// why the indicator looked dead.
    /// </para>
    /// </summary>
    protected double ClipLevel = 0;

    /// <summary>Latched until the box is clicked or the Monitor screen's Reset button is pressed.</summary>
    protected bool Input_Clipped;

    /// <summary>Latched until the box is clicked or the Monitor screen's Reset button is pressed.</summary>
    protected bool Output_Clipped;

    protected double Input_Peak = 0;
    protected double Input_RMS = 0;
    protected double Input_DB_Peak = 0;
    protected double Input_DB = 0;

    protected double Output_Peak = 0;
    protected double Output_RMS = 0;
    protected double Output_DB_Peak = 0;
    protected double Output_DB = 0;

    /// <summary>
    /// How many refreshes the red peak bar holds its maximum for. Raise it to make the bar linger
    /// longer; at the default 100 ms refresh interval 3 refreshes is roughly a third of a second.
    /// </summary>
    protected const int PeakHoldRefreshCount = 3;

    /// <summary>Rolling window of the last <see cref="PeakHoldRefreshCount"/> raw input peaks, in dB.</summary>
    protected readonly double[] Input_PeakWindow = CreatePeakWindow();

    /// <summary>Rolling window of the last <see cref="PeakHoldRefreshCount"/> raw output peaks, in dB.</summary>
    protected readonly double[] Output_PeakWindow = CreatePeakWindow();

    /// <summary>Write position for both windows; they advance together, exactly once per refresh.</summary>
    protected int PeakWindowIndex;

    /// <summary>
    /// Maximum of the last <see cref="PeakHoldRefreshCount"/> input peaks. This - not the raw
    /// per-refresh peak - is what the red peak bar and the peak dB label show, so the two agree.
    /// </summary>
    protected double Input_DB_Peak_Held = double.NegativeInfinity;

    /// <summary>Maximum of the last <see cref="PeakHoldRefreshCount"/> output peaks.</summary>
    protected double Output_DB_Peak_Held = double.NegativeInfinity;
    // Last rendered values to avoid unnecessary UI updates
    protected double Prev_Input_DB = double.NaN;
    protected double Prev_Input_DB_Peak = double.NaN;
    protected double Prev_Output_DB = double.NaN;
    protected double Prev_Output_DB_Peak = double.NaN;
    #endregion

    #region Public Properties
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Button Get_btn_View => this.btn_View;
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Timer Get_timer_Refresh => this.timer_Refresh;
    #endregion

    #region Constructor and MapEventHandlers
    public BTH_VolumeLevelControl()
    {
        InitializeComponent();
        this.MapEventHandlers();
    }

    public void MapEventHandlers()
    {
        this.pnl_InputClip.Click += Pnl_InputClip_Click;
        this.pnl_OutputClip.Click += Pnl_OutputClip_Click;
    }
    #endregion

    #region Event Handlers
    protected void Pnl_OutputClip_Click(object? sender, EventArgs e)
    {
        try
        {
            this.pnl_OutputClip.BackColor = System.Drawing.Color.Black;
            this.Output_Clipped = false;
            this.Output_Peak = 0;

            ClearPeakWindow(this.Output_PeakWindow);
            this.Output_DB_Peak_Held = double.NegativeInfinity;
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }

    protected void Pnl_InputClip_Click(object? sender, EventArgs e)
    {
        try
        {
            this.pnl_InputClip.BackColor = System.Drawing.Color.Black;
            this.Input_Clipped = false;
            this.Input_Peak = 0;

            //"Reset Peak and Clip Indicators" - drop the held peak too, so the red bar falls away
            //at once instead of lingering for up to PeakHoldRefreshCount refreshes.
            ClearPeakWindow(this.Input_PeakWindow);
            this.Input_DB_Peak_Held = double.NegativeInfinity;
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }

    /// <summary>
    /// Low-rate backstop for the dB labels.
    /// <para>
    /// ComputeLevels is the authoritative update: it pushes the meters and the labels from one
    /// snapshot so they always agree. This tick only re-asserts the current field values, and
    /// SetDbLabel skips the write when the rounded value has not moved, so it is a no-op
    /// whenever the refresh timer is running. FormMonitoring still owns this timer's Enabled
    /// flag for its Pause checkbox.
    /// </para>
    /// </summary>
    protected void timer_Refresh_Tick(object? sender, EventArgs e)
    {
        try
        {
            this.Set_DB_Lables();
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }

    [SupportedOSPlatform("windows")]
    protected void Btn_View_Click(object? sender, EventArgs e)
    {
        try
        {
            if (this.Stream == null || this.Stream.InputSource == null || this.Stream.OutputDestination == null)
                return;

            //DEFECT FIX: FormRTA holds charts, timers, FFT buffers and ASIO event subscriptions.
            //If Init_Channels threw - or if dialogs are suppressed and the form is never shown -
            //it used to be abandoned undisposed, leaking GDI handles and live subscriptions.
            var Local_Form = new FormRTA();
            var Local_Shown = false;
            try
            {
                Local_Form.Text += "  " + this.Stream.InputSource.Name + "-> " + this.Stream.OutputDestination.Name;
                Local_Form.Init_Channels(this.Stream.InputSource, this.Stream.OutputDestination);

                if (!Debug.SuppressInteractiveDialogs)
                {
                    Debug.ShowFormSafe(Local_Form);
                    Local_Shown = true;
                }
            }
            finally
            {
                //Once shown the form owns its own lifetime (it disposes itself on close).
                if (!Local_Shown)
                    Local_Form.Dispose();
            }
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }
    #endregion

    #region Public Functions
    public void Set_StreamInfo(DSP_Stream? input)
    {
        this.Stream = input;
        if (Stream == null || Stream.InputSource == null || Stream.OutputDestination == null)
            return;

        this.InputChannel = Stream.InputSource;
        this.lbl_InputSource.Text = Stream.InputSource.DisplayMember;
        this.OutputChannel = Stream.OutputDestination;
        this.lbl_OutputSource.Text = Stream.OutputDestination.DisplayMember;
    }

    public void Reset_ClipIndicator()
    {
        this.SafeInvoke(() =>
        {
            this.Pnl_InputClip_Click(this, EventArgs.Empty);
            this.Pnl_OutputClip_Click(this, EventArgs.Empty);
        });
    }

    public void ComputeLevels()
    {
        if (this.Disposing || this.IsDisposed)
            return;
        this.SafeInvoke(() =>
        {
            if (this.Disposing || this.IsDisposed)
                return;
            this.CalculateInputLevels();
            if (this.Disposing || this.IsDisposed)
                return;
            this.CalculateOutputLevels();
            if (this.Disposing || this.IsDisposed)
                return;

            //Must run exactly once per refresh, and BEFORE the displays are pushed: the peak bar
            //shows a rolling maximum rather than the raw per-buffer peak.
            this.Update_PeakHold();
            if (this.Disposing || this.IsDisposed)
                return;
            this.Set_VolAndClipIndicators();
            if (this.Disposing || this.IsDisposed)
                return;

            //DEFECT FIX: the dB labels used to be driven ONLY by this control's own 1000 ms
            //timer_Refresh, while the meters were driven by FormMonitoring's refresh timer
            //(100 ms by default, user-settable down to 1 ms). Both paths read the same
            //Input_DB / Input_DB_Peak / Output_DB / Output_DB_Peak fields, so the two sampled
            //them up to a second apart and the numbers beside a meter belonged to a different
            //audio buffer than its bar and peak bar. Every display now comes from the one
            //snapshot the Calculate* calls above just produced.
            this.Set_DB_Lables();
        });
    }
    #endregion

    #region Protected Functions

    /// <summary>
    /// Reusable meter scratch buffer.
    /// <para>
    /// PERF: the meter path used to allocate a fresh <c>double[SamplesPerChannel]</c> for BOTH
    /// the input and the output channel on every refresh (FormMonitoring's refresh interval is
    /// user-settable down to 1 ms, per meter). The samples are only read, reduced to
    /// RMS/peak and thrown away, so a per-control buffer is safe: it is filled and consumed
    /// entirely inside a single <c>SafeInvoke</c> callback on the UI thread and is never
    /// published anywhere.
    /// </para>
    /// </summary>
    private double[]? MeterScratch;

    /// <summary>
    /// Returns the per-control scratch buffer, growing it only when the ASIO buffer size changes.
    /// </summary>
    /// <param name="minimumLength">The required capacity.</param>
    /// <returns>A buffer of at least <paramref name="minimumLength"/> samples.</returns>
    private double[] EnsureMeterScratch(int minimumLength)
    {
        var Local_Buffer = this.MeterScratch;
        if (Local_Buffer == null || Local_Buffer.Length < minimumLength)
        {
            Local_Buffer = new double[minimumLength < 1 ? 1 : minimumLength];
            this.MeterScratch = Local_Buffer;
        }
        return Local_Buffer;
    }

    /// <summary>
    /// Copies one channel's samples into the reusable scratch buffer without allocating.
    /// </summary>
    /// <param name="item">The stream item to read.</param>
    /// <param name="isInput">True for the input direction, false for the output direction.</param>
    /// <param name="samples">Receives the filled slice of the scratch buffer.</param>
    /// <returns>True when a slice was produced.</returns>
    private bool TryFillMeterScratch(IStreamItem item, bool isInput, out ReadOnlySpan<double> samples)
    {
        samples = default;

        var Local_Asio = Program.ASIO;
        int Local_Length = Local_Asio.SamplesPerChannel;

        switch (item.StreamType)
        {
            case StreamType.Bus:
            {
                //Live buffer, mutated by the audio thread - must be copied before reading.
                var Local_Live = Program.DSP_Info.Buses[item.Index].Buffer;
                if (Local_Live == null || Local_Live.Length == 0)
                    return false;

                var Local_Scratch = this.EnsureMeterScratch(Local_Live.Length);
                Local_Live.AsSpan().CopyTo(Local_Scratch);
                samples = new ReadOnlySpan<double>(Local_Scratch, 0, Local_Live.Length);
                return true;
            }
            case StreamType.AbstractBus:
            case StreamType.Stream:
            {
                //No real source - silence, exactly as the previous 'new double[SamplesPerChannel]'.
                if (Local_Length < 1)
                    return false;

                var Local_Scratch = this.EnsureMeterScratch(Local_Length);
                Array.Clear(Local_Scratch, 0, Local_Length);
                samples = new ReadOnlySpan<double>(Local_Scratch, 0, Local_Length);
                return true;
            }
            case StreamType.Channel:
            default:
            {
                if (Local_Length < 1)
                    return false;

                var Local_Scratch = this.EnsureMeterScratch(Local_Length);
                bool Local_Ok = isInput
                    ? Local_Asio.TryCopyInputAudioData(item.Index, Local_Scratch, out int Local_Copied)
                    : Local_Asio.TryCopyOutputAudioData(item.Index, Local_Scratch, out Local_Copied);

                if (!Local_Ok)
                {
                    //Matches the old '?? new double[SamplesPerChannel]' silence fallback.
                    Array.Clear(Local_Scratch, 0, Local_Length);
                    samples = new ReadOnlySpan<double>(Local_Scratch, 0, Local_Length);
                    return true;
                }

                samples = new ReadOnlySpan<double>(Local_Scratch, 0, Local_Copied);
                return true;
            }
        }
    }

    // Helper method that calculates RMS, peak, and decibel values.
    private void CalculateLevels(ReadOnlySpan<double> audioData, bool isInput)
    {
        if (audioData.Length == 0)
            return;

        double squareSum = 0;
        double peak = 0;
        // For input channels, apply the stream volume if available.
        double volume = isInput && this.Stream != null ? this.Stream.InputVolume : 1.0;

        // Use indexed loop to avoid enumerator allocation and use multiplication
        // instead of Math.Pow for square to reduce CPU overhead.
        int len = audioData.Length;
        for (int i = 0; i < len; i++)
        {
            double level = audioData[i] * volume;
            double absLevel = Math.Abs(level);

            if (absLevel > peak)
                peak = absLevel;

            squareSum += level * level;
        }

        double rms = Math.Sqrt(squareSum / audioData.Length);
        double db = Decibels.LinearToDecibels(rms);
        double dbPeak = Decibels.LinearToDecibels(peak);

        if (isInput)
        {
            this.Input_RMS = rms;
            this.Input_DB = db;
            this.Input_DB_Peak = dbPeak;
            this.Input_Peak = peak;
        }
        else
        {
            this.Output_RMS = rms;
            this.Output_DB = db;
            this.Output_DB_Peak = dbPeak;
            this.Output_Peak = peak;
        }
    }

    protected void CalculateInputLevels()
    {
        if (this.InputChannel != null && this.InputChannel.Index > -1)
        {
            // For input, pass 'true' to apply stream volume.
            if (this.TryFillMeterScratch(this.InputChannel, true, out var Local_Samples))
                this.CalculateLevels(Local_Samples, true);
        }
    }

    protected void CalculateOutputLevels()
    {
        if (this.OutputChannel != null && this.OutputChannel.Index > -1)
        {
            // For output, pass 'false' so that no volume multiplier is applied.
            if (this.TryFillMeterScratch(this.OutputChannel, false, out var Local_Samples))
                this.CalculateLevels(Local_Samples, false);
        }
    }

    protected void Set_DB_Lables()
    {
        //PERF: the old code always built all 4 strings (a ToString + a Concat each) and only then
        //compared them against the label text, so it allocated 8 strings per refresh even when
        //nothing had changed. Compare the ROUNDED SOURCE VALUE instead and format only on a change.
        //The peak labels read the HELD peak, the same value the red bars are drawn from, so a
        //label always describes the bar beside it.
        SetDbLabel(this.lbl_Input_DB_Peak, this.Input_DB_Peak_Held, ref this.Last_Input_DB_Peak_Rounded);
        SetDbLabel(this.lbl_Input_DB, this.Input_DB, ref this.Last_Input_DB_Rounded);
        SetDbLabel(this.lbl_Output_DB_Peak, this.Output_DB_Peak_Held, ref this.Last_Output_DB_Peak_Rounded);
        SetDbLabel(this.lbl_Output_DB, this.Output_DB, ref this.Last_Output_DB_Rounded);
    }

    #region dB label caches
    private double Last_Input_DB_Peak_Rounded = double.NaN;
    private double Last_Input_DB_Rounded = double.NaN;
    private double Last_Output_DB_Peak_Rounded = double.NaN;
    private double Last_Output_DB_Rounded = double.NaN;
    #endregion

    /// <summary>
    /// Sets a "&lt;n&gt;dB" label only when the rounded value actually changed.
    /// </summary>
    /// <param name="label">The label to update.</param>
    /// <param name="value">The current dB value.</param>
    /// <param name="cache">The caller's cache of the previously rendered rounded value.</param>
    private static void SetDbLabel(Control label, double value, ref double cache)
    {
        double Local_Rounded = Math.Round(value, 0);
        if (Local_Rounded.Equals(cache)) //Equals so the NaN seed compares true against itself.
            return;

        cache = Local_Rounded;
        label.Text = FormatDbLabel(Local_Rounded);
    }

    /// <summary>
    /// Renders a whole-dB reading for a level label.
    /// </summary>
    /// <param name="rounded">The already-rounded dB value.</param>
    /// <returns>The text to show.</returns>
    /// <remarks>
    /// DEFECT FIX: silence makes Decibels.LinearToDecibels(0) return negative infinity, which
    /// ToString renders as "-Infinity" - so an idle channel read "-InfinitydB" beside an empty
    /// meter. Non-finite readings now use the conventional infinity glyph, written as an escape
    /// so this file stays plain ASCII (the same trick NAudio's own meter used).
    /// </remarks>
    private static string FormatDbLabel(double rounded)
    {
        if (double.IsNegativeInfinity(rounded))
            return "-\x221edB";
        if (double.IsPositiveInfinity(rounded))
            return "+\x221edB";
        if (double.IsNaN(rounded))
            return "--dB";

        return rounded.ToString(System.Globalization.CultureInfo.InvariantCulture) + "dB";
    }

    /// <summary>
    /// Creates a peak-hold window that starts empty.
    /// </summary>
    /// <returns>A window filled with negative infinity.</returns>
    private static double[] CreatePeakWindow()
    {
        var Local_Window = new double[PeakHoldRefreshCount];

        //Deliberately not the 0.0 a plain array would give: an all-zero window would park the red
        //bar at 0 dBFS - a full-scale reading - until enough refreshes had overwritten it.
        for (int i = 0; i < Local_Window.Length; i++)
            Local_Window[i] = double.NegativeInfinity;

        return Local_Window;
    }

    /// <summary>
    /// Advances the peak-hold windows by one refresh and recomputes the held maxima.
    /// </summary>
    /// <remarks>
    /// The red bar used to jump straight to the raw peak of the buffer that had just been measured,
    /// so a transient was on screen for a single refresh and was easy to miss. It now shows the
    /// largest of the last <see cref="PeakHoldRefreshCount"/> refreshes.
    /// </remarks>
    protected void Update_PeakHold()
    {
        int Local_Slot = this.PeakWindowIndex;
        this.Input_PeakWindow[Local_Slot] = this.Input_DB_Peak;
        this.Output_PeakWindow[Local_Slot] = this.Output_DB_Peak;

        Local_Slot++;
        this.PeakWindowIndex = Local_Slot >= PeakHoldRefreshCount ? 0 : Local_Slot;

        this.Input_DB_Peak_Held = MaxOf(this.Input_PeakWindow);
        this.Output_DB_Peak_Held = MaxOf(this.Output_PeakWindow);
    }

    /// <summary>
    /// Returns the largest value in a peak window.
    /// </summary>
    /// <param name="values">The window to scan.</param>
    /// <returns>The maximum, or negative infinity for an empty window.</returns>
    private static double MaxOf(double[] values)
    {
        double Local_Max = double.NegativeInfinity;
        for (int i = 0; i < values.Length; i++)
        {
            //A '>' test means a NaN entry can never win, so one bad buffer cannot poison the hold.
            if (values[i] > Local_Max)
                Local_Max = values[i];
        }

        return Local_Max;
    }

    /// <summary>
    /// Empties a peak-hold window so the red bar drops immediately instead of lingering.
    /// </summary>
    /// <param name="window">The window to clear.</param>
    private static void ClearPeakWindow(double[] window)
    {
        for (int i = 0; i < window.Length; i++)
            window[i] = double.NegativeInfinity;
    }

    protected void Set_VolAndClipIndicators()
    {
        // Local refs to reduce repeated property access
        var volIn = this.vol_In;
        var volOut = this.vol_Out;

        // Only update DB level if changed beyond a small threshold to avoid frequent redraws
        const double threshold = 0.1; // dB
        if (double.IsNaN(this.Prev_Input_DB) || Math.Abs(this.Input_DB - this.Prev_Input_DB) > threshold)
        {
            //The setter repaints only the bar strip when the value actually moved.
            volIn.DB_Level = this.Input_DB;
            this.Prev_Input_DB = this.Input_DB;
        }

        if (double.IsNaN(this.Prev_Input_DB_Peak) || Math.Abs(this.Input_DB_Peak_Held - this.Prev_Input_DB_Peak) > threshold)
        {
            //Drives the red peak bar on the meter, from the held maximum rather than the raw peak.
            volIn.DB_Peak = this.Input_DB_Peak_Held;
            this.Prev_Input_DB_Peak = this.Input_DB_Peak_Held;
        }

        if (double.IsNaN(this.Prev_Output_DB) || Math.Abs(this.Output_DB - this.Prev_Output_DB) > threshold)
        {
            volOut.DB_Level = this.Output_DB;
            this.Prev_Output_DB = this.Output_DB;
        }

        if (double.IsNaN(this.Prev_Output_DB_Peak) || Math.Abs(this.Output_DB_Peak_Held - this.Prev_Output_DB_Peak) > threshold)
        {
            volOut.DB_Peak = this.Output_DB_Peak_Held;
            this.Prev_Output_DB_Peak = this.Output_DB_Peak_Held;
        }

        //DEFECT FIX: the clip test used to live INSIDE the 'peak moved by more than 0.1 dB'
        //blocks above, so a steadily clipped signal - whose peak sits still at full scale -
        //stopped being evaluated and the box never lit. Clipping is now tested on every refresh,
        //independently of the repaint throttling.
        this.Latch_ClipIndicator(this.pnl_InputClip, this.Input_DB, this.Input_DB_Peak, ref this.Input_Clipped);
        this.Latch_ClipIndicator(this.pnl_OutputClip, this.Output_DB, this.Output_DB_Peak, ref this.Output_Clipped);
    }

    /// <summary>
    /// Latches a clip indicator red once the level reaches <see cref="ClipLevel"/>, and leaves it
    /// red until the box is clicked or the Reset button at the top of the Monitor screen is used.
    /// </summary>
    /// <param name="indicator">The clip box to colour.</param>
    /// <param name="db">The current RMS level, in dB.</param>
    /// <param name="dbPeak">The current peak level, in dB.</param>
    /// <param name="latched">The caller's latch flag for this direction.</param>
    /// <remarks>
    /// DEFECT FIX: the old code cleared the box back to black as soon as the level dropped below
    /// the threshold, so a clip that lasted less than one refresh interval was invisible and a
    /// clip that had happened was forgotten. A clip indicator is a latch by definition - that is
    /// what the Reset button exists for - so nothing here clears it.
    /// </remarks>
    protected void Latch_ClipIndicator(Control indicator, double db, double dbPeak, ref bool latched)
    {
        if (latched)
            return;

        //Written as a positive test so a NaN level cannot latch the box.
        bool Local_IsClipping = db >= this.ClipLevel || dbPeak >= this.ClipLevel;
        if (!Local_IsClipping)
            return;

        latched = true;
        if (indicator.BackColor != System.Drawing.Color.Red)
            indicator.BackColor = System.Drawing.Color.Red;
    }
    #endregion

    #region Error Handling
    protected void Error(Exception ex)
    {
        Debug.Error(ex);
    }
    #endregion
}