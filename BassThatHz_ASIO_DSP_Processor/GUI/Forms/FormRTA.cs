#nullable enable

namespace BassThatHz_ASIO_DSP_Processor.GUI.Forms;

#region Usings
using DSPLib;
using NAudio.Utils;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Buffers;
using System.Numerics;
using System.Runtime.Versioning;
using System.Threading.Tasks;
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
// 
/// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
/// IMPLIED, INCLUDING BUT NOT LIMITED TO FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
/// IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
/// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
/// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
/// SOFTWARE. ENFORCEABLE PORTIONS SHALL REMAIN IF NOT FOUND CONTRARY UNDER LAW.
/// </summary>
public partial class FormRTA : Form
{
    #region Variables

    #region Multi-Threading and Closing State
    protected bool IsClosing = false;

    //DEFECT FIX: every plot tick is 'async void'. It disables its own timer on entry and only
    //re-arms it in the finally, and THAT was the only thing stopping two ticks from running at
    //once - which matters because the two directions share a stateful FFT instance apiece, a
    //stateful decimator apiece and one reusable scratch array apiece.
    //
    //Pause_CHK_CheckedChanged writes Enabled directly, from the UI thread, with no idea whether a
    //tick is in flight. The UI thread is back in the message pump while a tick sits on its await,
    //so ticking Pause off and on again during that window re-armed a 1 ms timer and delivered a
    //second, concurrent tick into the same handler: two pool threads inside one FFT object,
    //scribbling over one decimator's prefix sums, and two Advance calls for one plotted frame.
    //
    //These latches make Enabled a scheduling hint rather than the mutex. A tick that arrives while
    //its predecessor is still running simply returns.
    protected bool ULF_Tick_InFlight;
    protected bool Top_Tick_InFlight;
    protected bool Waveform_Tick_InFlight;
    protected List<Task> ULF_FFT_Tasks = new();
    protected List<Task> Top_FFT_Tasks = new();
    protected List<Task<ChartUpdateData>> Waveform_Tasks = new();
    #endregion

    #region Default Values
    protected int Default_ULF_FFTSize = 2048;
    protected int Default_Top_FFTSize = 2048;

    protected IStreamItem? Input_Channel;
    protected IStreamItem? Output_Channel;
    protected double[]? InputBuffer;
    protected double[]? OutputBuffer;

    //The ULF analyser downsamples its frame to Default_ULF_FFTSize points, so one second of audio
    //gives a 2048 Hz effective rate and 1 Hz bins across the plotted 1..100 Hz span. One second is
    //therefore exactly what a frame needs - and anything the buffer holds BEYOND the frame is pure
    //display latency, so keep only a single extra second for ASIO/timer jitter.
    //DEFECT FIX: this store used to be ten seconds deep, which is what made the charts show
    //never-written zeros for ten seconds after opening and take another ten seconds to fall silent
    //once the signal stopped.
    protected const int ULF_AnalysisSeconds = 1;
    protected const int ULF_BufferSeconds = 2;

    //Enough headroom for the largest selectable Top FFT size plus jitter, without hoarding audio.
    protected const int Top_BufferFrames = 4;

    protected int ULF_FFT_OverLapPercentage = 90;
    protected int Top_FFT_OverLapPercentage = 10;
    protected readonly int MouseMoveThrottleMs = 50;
    protected readonly int FFTThrottleMs = 50;
    #endregion

    #region FFT Object and Data Refs
    protected CircularBuffer RTA_InputTopBuffer;
    protected CircularBuffer RTA_OutputTopBuffer;
    protected CircularBuffer RTA_InputULFBuffer;
    protected CircularBuffer RTA_OutputULFBuffer;

    //DEFECT FIX: the FFT objects, the window coefficients, the window scale factor, the frequency
    //span and the FFT size used to be five (six for ULF) separate mutable fields. The plot timers
    //are 'async void' and await with pool tasks still in flight, so the UI thread returns to the
    //message pump mid-tick; a combo box change then reassigned those fields ONE AT A TIME while a
    //task was reading them. Shrinking the Top FFT from 16384 to 2048 that way handed a 16384 point
    //FFT a 2048 sample input, sailed past both length guards in Perform_FFT_Into, and ran off the
    //end of the input array - an IndexOutOfRangeException that surfaced as the modal "A fatal error
    //has occured / Press Yes to abort the app" dialog.
    //
    //Everything that has to agree now lives in one immutable snapshot published by a single
    //reference assignment. A tick reads the field ONCE into a local and uses only that, so it
    //either sees the whole old configuration or the whole new one.
    protected AnalysisConfig Top_Config;
    protected AnalysisConfig ULF_Config;
    #endregion

    #region Analysis Configuration
    /// <summary>
    /// An immutable snapshot of everything one FFT chart pair needs to analyse a frame. Publish a
    /// new instance rather than mutating one; readers take a single reference and never look at any
    /// of this through <c>this</c> again.
    /// </summary>
    protected sealed class AnalysisConfig
    {
        /// <summary>The FFT length, which is also the required frame length in samples.</summary>
        public required int FFTSize { get; init; }

        /// <summary>The FFT instance for the input chart. Not shared with the output chart.</summary>
        public required FFT InputFFT { get; init; }

        /// <summary>The FFT instance for the output chart.</summary>
        public required FFT OutputFFT { get; init; }

        /// <summary>Window coefficients, always exactly <see cref="FFTSize"/> long.</summary>
        public required double[] WindowCoefficients { get; init; }

        /// <summary>Amplitude correction for the chosen window.</summary>
        public required double WindowScaleFactor { get; init; }

        /// <summary>The X axis, one entry per plotted bin.</summary>
        public required double[] FrequencySpan { get; init; }

        /// <summary>
        /// The input sample rate this snapshot was built for. The Top chart's frequency span is
        /// derived from it, so a rate change has to rebuild the snapshot.
        /// </summary>
        public required int SampleRate { get; init; }

        /// <summary>
        /// Band-limiting decimator for the input chart, or null when the chart analyses the frame
        /// at its native rate. Stateful, so the two directions never share one.
        /// </summary>
        public AntiAliasDecimator? InputDecimator { get; init; }

        /// <summary>Band-limiting decimator for the output chart.</summary>
        public AntiAliasDecimator? OutputDecimator { get; init; }
    }
    #endregion

    #region FFT Read Scratch Buffers
    //PERF: one reusable read buffer per FFT direction. Each is filled from its circular buffer,
    //consumed by the FFT and dropped within the same timer tick (the timer disables itself and
    //awaits Task.WhenAll before re-arming), so no consumer outlives the tick that produced it.
    //Kept at EXACTLY the requested length because Compute_*_FFT_Data derives its FFT size from
    //timeSeries.Length.
    protected double[]? InputULF_Scratch;
    protected double[]? OutputULF_Scratch;

    //The ULF frame is band-limited and decimated from InSampleRate samples down to the FFT length
    //before it is transformed; these hold that decimated frame.
    protected double[]? InputULF_Decimated;
    protected double[]? OutputULF_Decimated;

    //One decimator per direction, for the lifetime of the window. They carry nothing but scratch -
    //a second of doubles each, 1.5 MB at 192 kHz - and nothing about them depends on the window
    //type or the FFT size, so a new snapshot borrows these rather than allocating its own. Building
    //them per snapshot churned ~6 MB of large-object heap on every step through the 33 entry window
    //type combo. Never share one between the two directions: Decimate is not thread safe.
    protected readonly AntiAliasDecimator InputULF_Decimator = new();
    protected readonly AntiAliasDecimator OutputULF_Decimator = new();

    protected double[]? InputTop_Scratch;
    protected double[]? OutputTop_Scratch;

    /// <summary>
    /// Returns a scratch buffer of exactly <paramref name="length"/> entries, re-allocating only
    /// when the required length changes (sample-rate or FFT-size change).
    /// </summary>
    /// <param name="buffer">The field holding the current buffer.</param>
    /// <param name="length">The exact required length.</param>
    /// <returns>A buffer whose Length equals <paramref name="length"/>.</returns>
    protected static double[] EnsureExactScratch(ref double[]? buffer, int length)
    {
        if (length < 0)
            length = 0;

        var Local_Buffer = buffer;
        if (Local_Buffer == null || Local_Buffer.Length != length)
        {
            Local_Buffer = new double[length];
            buffer = Local_Buffer;
        }
        return Local_Buffer;
    }
    #endregion

    #region FFT Framing and Buffer Sizing
    /// <summary>
    /// Samples in one ULF analysis frame: exactly one second, which is what 1 Hz bins require.
    /// </summary>
    /// <param name="sampleRate">The current input sample rate.</param>
    /// <returns>The frame length in samples, or 0 if the sample rate is not yet known.</returns>
    protected static int Get_ULF_FrameLength(int sampleRate)
    {
        return sampleRate <= 0 ? 0 : sampleRate * ULF_AnalysisSeconds;
    }

    /// <summary>
    /// Samples of storage behind the ULF charts. One analysis frame plus one second of headroom -
    /// the buffer is a jitter cushion, not a history, so anything deeper is display latency.
    /// </summary>
    /// <param name="sampleRate">The current input sample rate.</param>
    /// <returns>The circular buffer capacity in samples, never less than 1.</returns>
    protected static int Get_ULF_BufferLength(int sampleRate)
    {
        return sampleRate <= 0 ? 1 : sampleRate * ULF_BufferSeconds;
    }

    /// <summary>
    /// Samples of storage behind the Top FFT charts, scaled to the selected FFT size.
    /// </summary>
    /// <param name="fftSize">The currently selected Top FFT size.</param>
    /// <returns>The circular buffer capacity in samples, never less than 1.</returns>
    protected static int Get_Top_BufferLength(int fftSize)
    {
        return fftSize <= 0 ? 1 : fftSize * Top_BufferFrames;
    }

    /// <summary>
    /// Samples to step forward between overlapped frames.
    /// </summary>
    /// <param name="frameLength">The analysis frame length in samples.</param>
    /// <param name="overlapPercentage">The configured overlap, 0..99.</param>
    /// <returns>The hop, clamped to 1..frameLength so the analyser can never stall or skip audio.</returns>
    protected static int Get_HopLength(int frameLength, int overlapPercentage)
    {
        if (frameLength <= 0)
            return 0;

        //Integer arithmetic on purpose: (int)(48000 * (1d - 90/100d)) truncates to 4799 because
        //1d - 0.9 is 0.09999999999999998, which quietly shifted every frame by one sample.
        int Local_Overlap = Math.Clamp(overlapPercentage, 0, 99);
        int Local_Hop = (int)((long)frameLength * (100 - Local_Overlap) / 100);
        return Math.Clamp(Local_Hop, 1, frameLength);
    }

    /// <summary>
    /// True once a whole analysis frame is available.
    /// </summary>
    /// <param name="buffer">The circular buffer feeding the analyser.</param>
    /// <param name="frameLength">The analysis frame length in samples.</param>
    /// <returns>Whether a frame can be analysed now.</returns>
    /// <remarks>
    /// DEFECT FIX: this used to demand Count > frameLength * (1 + overlap) - nearly TWO seconds of
    /// ULF audio at the default 90% overlap - before the first frame could be plotted, and it held
    /// that extra second of latency in the buffer permanently. A frame needs a frame, no more.
    /// </remarks>
    protected static bool HasFullFrame(CircularBuffer buffer, int frameLength)
    {
        return frameLength > 0 && buffer.Count >= frameLength;
    }

    /// <summary>
    /// Re-allocates the ULF buffers if the input sample rate changed while the window is open.
    /// Without this the buffers would keep the capacity captured at construction time and a rate
    /// increase would leave them permanently too small to hold a frame.
    /// </summary>
    /// <param name="sampleRate">The current input sample rate.</param>
    protected void EnsureULFBufferCapacity(int sampleRate)
    {
        int Local_Length = Get_ULF_BufferLength(sampleRate);

        if (this.RTA_InputULFBuffer.MaxLength != Local_Length)
            this.RTA_InputULFBuffer = new CircularBuffer(Local_Length);

        if (this.RTA_OutputULFBuffer.MaxLength != Local_Length)
            this.RTA_OutputULFBuffer = new CircularBuffer(Local_Length);
    }

    /// <summary>
    /// Re-allocates the Top FFT buffers if the user picked a different FFT size. The largest
    /// selectable size is eight times the default, so a fixed constructor-time capacity could not
    /// hold a frame at every setting.
    /// </summary>
    /// <param name="fftSize">The currently selected Top FFT size.</param>
    protected void EnsureTopBufferCapacity(int fftSize)
    {
        int Local_Length = Get_Top_BufferLength(fftSize);

        if (this.RTA_InputTopBuffer.MaxLength != Local_Length)
            this.RTA_InputTopBuffer = new CircularBuffer(Local_Length);

        if (this.RTA_OutputTopBuffer.MaxLength != Local_Length)
            this.RTA_OutputTopBuffer = new CircularBuffer(Local_Length);
    }
    #endregion

    #region Waveform Rolling Y Axis Range
    //DEFECT FIX: the waveform Y axis used to be a five second ratchet. timer_ResetWaveform slammed
    //AxisY to 0/0 and the axis then re-grew from whatever single ASIO block arrived next - and a
    //512 sample block of a sub-bass tone can peak anywhere between zero and the full amplitude, so
    //the range collapsed and climbed back every five seconds. Worse, the only assignment sat behind
    //a "grow" comparison against AxisY.Maximum, which starts as NaN: every comparison against NaN
    //is false, so a freshly opened window had NO range at all until the first reset fired.
    //
    //It is now a ten second rolling peak per chart: the axis follows the signal up immediately and
    //eases back down as loud blocks age out of the window, with no discontinuity anywhere.
    protected const int WaveformRangeWindowSeconds = 10;
    protected const int WaveformRangeBuckets = 40;
    //The ten second window is the authority on how long a peak is remembered; this time constant
    //only smooths the step a bucket makes as it retires, so it is deliberately short - long enough
    //to glide, short enough that the axis has settled a few seconds after the window releases.
    protected const double WaveformRangeDecaySeconds = 1d;

    //Below this relative change the axis is left alone: this runs on every waveform plot and every
    //assignment forces the chart to re-layout.
    protected const double WaveformRangeUpdateThreshold = 0.01d;

    //Far beyond any real sample yet far inside decimal's range, so the plotted waveform is never
    //altered but the (decimal) conversion can never overflow.
    protected const double DecimalSafeSample = 1e9d;

    protected readonly RollingPeakEnvelope InputWaveformRange = new(
        TimeSpan.FromSeconds(WaveformRangeWindowSeconds),
        WaveformRangeBuckets,
        TimeSpan.FromSeconds(WaveformRangeDecaySeconds));

    protected readonly RollingPeakEnvelope OutputWaveformRange = new(
        TimeSpan.FromSeconds(WaveformRangeWindowSeconds),
        WaveformRangeBuckets,
        TimeSpan.FromSeconds(WaveformRangeDecaySeconds));

    /// <summary>
    /// The rolling Y axis range belonging to a waveform chart.
    /// </summary>
    /// <param name="chartControl">The chart being plotted.</param>
    /// <returns>Its envelope, or null for a chart that has no rolling range.</returns>
    protected RollingPeakEnvelope? Get_WaveformRange(Chart chartControl)
    {
        if (ReferenceEquals(chartControl, this.chart_InputWaveform))
            return this.InputWaveformRange;

        if (ReferenceEquals(chartControl, this.chart_OutputWaveform))
            return this.OutputWaveformRange;

        return null;
    }
    #endregion

    #region Time throttle the mouse moves and min\max chart updates 
    protected DateTime LastMouseMoveUpdate = DateTime.MinValue;
    protected DateTime LastFFTUpdateULF = DateTime.MinValue;
    protected DateTime LastFFTUpdateTop = DateTime.MinValue;
    #endregion

    #endregion

    #region Constructor
    [SupportedOSPlatform("windows")]
    public FormRTA()
    {
        InitializeComponent();

        this.chart_InputWaveform.SuppressExceptions = true;
        this.chart_Input_Top_FFT.SuppressExceptions = true;
        this.chart_Input_ULF_FFT.SuppressExceptions = true;
        this.chart_OutputWaveform.SuppressExceptions = true;
        this.chart_Output_Top_FFT.SuppressExceptions = true;
        this.chart_Output_ULF_FFT.SuppressExceptions = true;

        var Top_Length = Get_Top_BufferLength(this.Default_Top_FFTSize);
        this.RTA_InputTopBuffer = new(Top_Length);
        this.RTA_OutputTopBuffer = new(Top_Length);

        //Seed both snapshots so no code path can ever see a null configuration. The combo boxes are
        //populated on Load, which re-publishes both with the real window coefficients.
        this.Top_Config = this.Build_Top_Config();
        this.ULF_Config = this.Build_ULF_Config();

        var ULF_Length = Get_ULF_BufferLength(Program.DSP_Info.InSampleRate);
        this.RTA_InputULFBuffer = new(ULF_Length);
        this.RTA_OutputULFBuffer = new(ULF_Length);

        this.Load += RTA_Load;
        this.Shown += RTA_Shown;
    }
    #endregion

    #region Public Functions

    #region Init
    public void Init_Channels(IStreamItem input_Channel, IStreamItem output_Channel)
    {
        this.Input_Channel = input_Channel;
        this.Output_Channel = output_Channel;
    }
    #endregion

    #endregion

    #region Event Handlers

    #region Load, Closing and MapEventHandlers
    [SupportedOSPlatform("windows")]
    protected void RTA_Load(object? sender, EventArgs e)
    {
        try
        {
            this.Init_CheckedListBoxList();
            this.MapEventHandlers();
            this.Init_Comboboxes();
            this.Init_SetDefault_Combobox_Options();
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }

    [SupportedOSPlatform("windows")]
    protected void RTA_Shown(object? sender, EventArgs e)
    {
        try
        {
            this.Init_Timers();
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }

    [SupportedOSPlatform("windows")]
    protected async void RTA_FormClosing(object? sender, FormClosingEventArgs e)
    {
        try
        {
            this.IsClosing = true;

            this.timer_PlotWaveforms.Enabled = false;
            this.timer_Plot_Top_FFTs.Enabled = false;
            this.timer_Plot_ULF_FFT.Enabled = false;
            this.Pause_CHK.Checked = true;

            this.Load -= RTA_Load;
            this.Shown -= RTA_Shown;

            Program.ASIO.InputDataAvailable -= ASIO_InputDataAvailable;
            Program.ASIO.OutputDataAvailable -= ASIO_OutputDataAvailable;

            await Task.WhenAll(this.ULF_FFT_Tasks);
            await Task.WhenAll(this.Top_FFT_Tasks);
            await Task.WhenAll(this.Waveform_Tasks);
        }
        catch (Exception ex)
        {
            //The form is closing: in-flight FFT/waveform tasks may fault on a disposed chart or a
            //torn buffer. Nothing useful can be done at this point, but do not lose the error.
            Debug.ReportSwallowed(ex);
        }
    }
    #endregion

    #region FFT Comboboxes
    protected void ULF_FFT_Window_Type_CBO_SelectedIndexChanged(object? sender, EventArgs e)
    {
        try
        {
            this.ReCalculate_ULF_FFT();
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }

    protected void Top_FFT_Window_Type_CBO_SelectedIndexChanged(object? sender, EventArgs e)
    {
        try
        {
            this.ReCalculate_Top_FFT();
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }

    protected void Top_FFT_Size_CBO_SelectedIndexChanged(object? sender, EventArgs e)
    {
        try
        {
            string? Selected_FFTSize_String = this.cbo_Top_FFT_Size.SelectedItem?.ToString();
            if (!string.IsNullOrEmpty(Selected_FFTSize_String))
            {
                this.Default_Top_FFTSize = int.Parse(Selected_FFTSize_String);
                this.ReCalculate_Top_FFT();
            }
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }

    protected void ULF_FFT_Overlap_CBO_SelectedIndexChanged(object? sender, EventArgs e)
    {
        try
        {
            switch (this.cbo_ULF_FFT_Overlap.SelectedIndex)
            {
                case 0:
                    this.ULF_FFT_OverLapPercentage = 0;
                    break;
                case 1:
                    this.ULF_FFT_OverLapPercentage = 25;
                    break;
                case 2:
                    this.ULF_FFT_OverLapPercentage = 50;
                    break;
                case 3:
                    this.ULF_FFT_OverLapPercentage = 75;
                    break;
                case 4:
                    this.ULF_FFT_OverLapPercentage = 90;
                    break;
                case 5:
                    this.ULF_FFT_OverLapPercentage = 95;
                    break;
                default:
                    this.ULF_FFT_OverLapPercentage = 90;
                    break;
            }
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }

    protected void Top_FFT_Overlap_CBO_SelectedIndexChanged(object? sender, EventArgs e)
    {
        try
        {
            switch (this.cbo_Top_FFT_Overlap.SelectedIndex)
            {
                case 0:
                    this.Top_FFT_OverLapPercentage = 0;
                    break;
                case 1:
                    this.Top_FFT_OverLapPercentage = 5;
                    break;
                case 2:
                    this.Top_FFT_OverLapPercentage = 10;
                    break;
                case 3:
                    this.Top_FFT_OverLapPercentage = 25;
                    break;
                case 4:
                    this.Top_FFT_OverLapPercentage = 50;
                    break;
                case 5:
                    this.Top_FFT_OverLapPercentage = 75;
                    break;
                case 6:
                    this.Top_FFT_OverLapPercentage = 90;
                    break;
                case 7:
                    this.Top_FFT_OverLapPercentage = 95;
                    break;
                default:
                    this.Top_FFT_OverLapPercentage = 10;
                    break;
            }
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }
    #endregion

    #region ASIO DataAvailable
    protected void ASIO_OutputDataAvailable()
    {
        try
        {
            if (this.Output_Channel == null)
                return;
            //Snapshot helper does exactly one copy; the old GetStream...().ToArray() did two,
            //once per ASIO buffer switch while the RTA window is open. The array must still be
            //freshly allocated (not pooled): it is published to the plotting timer thread, and
            //re-using one buffer would let the plot read a half-updated block.
            this.OutputBuffer = CommonFunctions.GetStreamOutputDataSnapshotByStreamItem(this.Output_Channel);

            if (this.chart_Output_ULF_FFT.Visible)
                _ = this.RTA_OutputULFBuffer.Write(this.OutputBuffer, 0, this.OutputBuffer.Length);

            if (this.chart_Output_Top_FFT.Visible)
                _ = this.RTA_OutputTopBuffer.Write(this.OutputBuffer, 0, this.OutputBuffer.Length);
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }

    [SupportedOSPlatform("windows")]
    protected void ASIO_InputDataAvailable()
    {
        try
        {
            if (this.Input_Channel == null)
                return;
            //See ASIO_OutputDataAvailable: one copy instead of two, still a fresh array.
            this.InputBuffer = CommonFunctions.GetStreamInputDataSnapshotByStreamItem(this.Input_Channel);

            if (this.chart_Input_ULF_FFT.Visible)
                _ = this.RTA_InputULFBuffer.Write(this.InputBuffer, 0, this.InputBuffer.Length);

            if (this.chart_Input_Top_FFT.Visible)
                _ = this.RTA_InputTopBuffer.Write(this.InputBuffer, 0, this.InputBuffer.Length);
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }
    #endregion

    #region Waveform Plot Timers
    [SupportedOSPlatform("windows")]
    protected async void PlotWaveforms_Timer_Tick(object sender, EventArgs e)
    {
        // Disable the timer while processing.
        this.timer_PlotWaveforms.Enabled = false;

        //See the Waveform_Tick_InFlight field: the waveform tasks share this.InputBuffer and the
        //two rolling range envelopes, neither of which tolerates two concurrent ticks.
        if (this.Waveform_Tick_InFlight)
            return;

        this.Waveform_Tick_InFlight = true;
        try
        {
            this.Waveform_Tasks.Clear();

            // Process input waveform if conditions are met.
            if (this.Input_Channel != null && this.Input_Channel.Index > -1 &&
                this.chart_InputWaveform.Visible &&
                this.InputBuffer != null && this.InputBuffer.Length > 0)
            {
                double[] yDataInput = this.InputBuffer;
                double scaleYAxis = 1.5;

                this.Waveform_Tasks.Add(Task.Run(() =>
                {
                    WaveformPlotData plotData = this.ComputeWaveformPlotData(yDataInput, scaleYAxis);
                    return new ChartUpdateData
                    {
                        Chart = this.chart_InputWaveform,
                        PlotData = plotData
                    };
                }));
            }

            // Process output waveform if conditions are met.
            if (this.Output_Channel != null && this.Output_Channel.Index > -1 &&
                this.chart_OutputWaveform.Visible &&
                this.OutputBuffer != null && this.OutputBuffer.Length > 0)
            {
                double[] yDataOutput = this.OutputBuffer;
                double scaleYAxis = 1.5;

                this.Waveform_Tasks.Add(Task.Run(() =>
                {
                    WaveformPlotData plotData = this.ComputeWaveformPlotData(yDataOutput, scaleYAxis);
                    return new ChartUpdateData
                    {
                        Chart = this.chart_OutputWaveform,
                        PlotData = plotData
                    };
                }));
            }

            // Await all background tasks.
            ChartUpdateData[] updates = await Task.WhenAll(this.Waveform_Tasks);

            //One instant for the whole batch, so both waveform ranges age by the same amount.
            DateTime Local_PlottedAt = DateTime.UtcNow;

            // Batch all UI updates on the UI thread.
            if (!this.IsClosing && !this.IsDisposed && this.IsHandleCreated)
                this.SafeInvoke(() =>
                {
                    foreach (var update in updates)
                    {
                        if (update.Chart == null || update.PlotData == null)
                            continue;

                        this.UpdateChartWithPlotData(update.Chart, update.PlotData, Local_PlottedAt);
                    }
                });
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
        finally
        {
            this.Waveform_Tick_InFlight = false;

            //DEFECT FIX: this is an 'async void' handler that awaits; the form can be closed and
            //disposed during the await. Touching this.timer_*/this.Pause_CHK unguarded threw
            //ObjectDisposedException FROM A FINALLY - unhandled (async void) and masking whatever
            //was propagating. Every other member access in this method already has this guard.
            this.RestartTimerIfAlive(this.timer_PlotWaveforms);
        }
    }

    /// <summary>
    /// Re-arms a plot timer unless the form is closing/disposed, and unless the user has paused.
    /// Safe to call from the finally of an async void handler.
    /// </summary>
    /// <param name="timer">The timer to re-arm.</param>
    protected void RestartTimerIfAlive(System.Windows.Forms.Timer? timer)
    {
        try
        {
            if (timer == null || this.IsClosing || this.IsDisposed || this.Disposing)
                return;

            timer.Enabled = !this.Pause_CHK.Checked;
        }
        catch (ObjectDisposedException ex)
        {
            //The form was disposed between the guard and the assignment - nothing left to re-arm.
            Debug.ReportSwallowed(ex);
        }
    }
    #endregion

    #region FFT Plot Timers
    [SupportedOSPlatform("windows")]
    protected async void Plot_ULF_FFT_Timer_Tick(object sender, EventArgs e)
    {
        this.timer_Plot_ULF_FFT.Enabled = false;

        //See the ULF_Tick_InFlight field: Pause can re-arm this timer while the previous tick is
        //still awaiting, so Enabled alone is not mutual exclusion.
        if (this.ULF_Tick_InFlight)
            return;

        this.ULF_Tick_InFlight = true;
        try
        {
            //Read the snapshot ONCE. Everything below - the FFT instances, the window, the scale
            //factor, the frequency span and the decimators - comes from this one local, so a combo
            //box change during the await cannot split a frame across two configurations.
            var Local_Config = this.ULF_Config;

            int inSampleRate = Program.DSP_Info.InSampleRate;
            int frameLength = Get_ULF_FrameLength(inSampleRate);
            int removeLength = Get_HopLength(frameLength, this.ULF_FFT_OverLapPercentage);

            //Track a sample-rate change made while this window is open; the buffers are sized from
            //the rate, so a stale capacity could no longer hold a frame.
            this.EnsureULFBufferCapacity(inSampleRate);

            this.ULF_FFT_Tasks.Clear();

            if (frameLength <= 0 || frameLength < Local_Config.FFTSize)
                return;

            // Process Input ULF.
            //Capture the buffer instance so the Peek and the matching Advance can never land on
            //two different instances if the sample rate changes underneath us.
            var Local_InputULF_Buffer = this.RTA_InputULFBuffer;
            if (this.chart_Input_ULF_FFT.Visible &&
                HasFullFrame(Local_InputULF_Buffer, frameLength))
            {
                //PERF: this allocated a fresh double[InSampleRate] (384 KB at 96 kHz) on a timer
                //whose interval can be as low as 1 ms. The array is filled from the circular
                //buffer, consumed by the FFT and dropped, and the timer disables itself and awaits
                //WhenAll before the next tick, so a per-direction reusable buffer is safe.
                var Local_InputULF_Scratch = EnsureExactScratch(ref this.InputULF_Scratch, frameLength);
                var Local_InputULF_Decimated = EnsureExactScratch(ref this.InputULF_Decimated, Local_Config.FFTSize);
                this.ULF_FFT_Tasks.Add(Task.Run(() =>
                {
                    var data = Local_InputULF_Scratch;
                    //Peek, not Read: the frame is a whole second but only the hop is consumed, so
                    //consecutive frames overlap. Reading destructively here and then advancing by
                    //the hop as well was what drove the read pointer away from the write pointer.
                    _ = Local_InputULF_Buffer.Peek(data, 0, frameLength);
                    return this.Compute_ULF_FFT_Data(Local_Config, Local_Config.InputFFT,
                        Local_Config.InputDecimator, data, Local_InputULF_Decimated);
                })
                .ContinueWith(t =>
                {
                    //DEFECT FIX: this read t.Result straight into a deconstruction. A faulted
                    //analysis task therefore rethrew here, skipped the Advance below, and left the
                    //frame in the buffer - so the next tick re-analysed the same bad frame and
                    //faulted again, once per millisecond, each one reaching the user as the modal
                    //"A fatal error has occured / Press Yes to abort the app" dialog. Consume the
                    //hop either way and report the fault once.
                    try
                    {
                        if (!t.IsCompletedSuccessfully)
                        {
                            if (t.Exception != null)
                                Debug.ReportSwallowed(t.Exception);
                            return;
                        }

                        var (xData, magLog) = t.Result;
                        if (xData.Length > 0 && magLog.Length > 0)
                        {
                            if (!this.IsClosing && !this.IsDisposed && this.IsHandleCreated)
                                this.SafeInvoke(() =>
                                    // Use frequency range constants directly here: 1 Hz to 100 Hz.
                                    this.Plot_FFT(this.chart_Input_ULF_FFT, 1, 100, xData, magLog, ref this.LastFFTUpdateULF));
                        }
                    }
                    finally
                    {
                        Local_InputULF_Buffer.Advance(removeLength);
                    }
                }));
            }

            // Process Output ULF.
            var Local_OutputULF_Buffer = this.RTA_OutputULFBuffer;
            if (this.chart_Output_ULF_FFT.Visible &&
                HasFullFrame(Local_OutputULF_Buffer, frameLength))
            {
                var Local_OutputULF_Scratch = EnsureExactScratch(ref this.OutputULF_Scratch, frameLength);
                var Local_OutputULF_Decimated = EnsureExactScratch(ref this.OutputULF_Decimated, Local_Config.FFTSize);
                this.ULF_FFT_Tasks.Add(Task.Run(() =>
                {
                    var data = Local_OutputULF_Scratch;
                    _ = Local_OutputULF_Buffer.Peek(data, 0, frameLength);
                    return this.Compute_ULF_FFT_Data(Local_Config, Local_Config.OutputFFT,
                        Local_Config.OutputDecimator, data, Local_OutputULF_Decimated);
                })
                .ContinueWith(t =>
                {
                    //Fault handling as in the input path above.
                    try
                    {
                        if (!t.IsCompletedSuccessfully)
                        {
                            if (t.Exception != null)
                                Debug.ReportSwallowed(t.Exception);
                            return;
                        }

                        var (xData, magLog) = t.Result;
                        if (xData.Length > 0 && magLog.Length > 0)
                        {
                            if (!this.IsClosing && !this.IsDisposed && this.IsHandleCreated)
                                this.SafeInvoke(() =>
                                    // Use frequency range constants directly here: 1 Hz to 100 Hz.
                                    this.Plot_FFT(this.chart_Output_ULF_FFT, 1, 100, xData, magLog, ref this.LastFFTUpdateULF));
                        }
                    }
                    finally
                    {
                        Local_OutputULF_Buffer.Advance(removeLength);
                    }
                }));
            }

            await Task.WhenAll(this.ULF_FFT_Tasks);
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
        finally
        {
            this.ULF_Tick_InFlight = false;

            //DEFECT FIX: see RestartTimerIfAlive - unguarded member access from the finally of an
            //async void handler crashed the app when the form closed mid-await.
            this.RestartTimerIfAlive(this.timer_Plot_ULF_FFT);
        }
    }

    [SupportedOSPlatform("windows")]
    protected async void PlotTopFFTs_Timer_Tick(object sender, EventArgs e)
    {
        this.timer_Plot_Top_FFTs.Enabled = false;

        //See the Top_Tick_InFlight field.
        if (this.Top_Tick_InFlight)
            return;

        this.Top_Tick_InFlight = true;
        try
        {
            //Read the snapshot ONCE - see Plot_ULF_FFT_Timer_Tick. This is the tick that used to
            //crash the app: shrinking the FFT size mid-await handed the old 16384 point FFT a 2048
            //sample frame and it ran off the end of the input array.
            var Local_Config = this.Top_Config;

            //A sample rate change while the window is open invalidates the Top frequency span,
            //which is derived from it. Rebuild the snapshot and use the new one from here on.
            if (Local_Config.SampleRate != Program.DSP_Info.InSampleRate)
            {
                Local_Config = this.Build_Top_Config();
                this.Top_Config = Local_Config;
            }

            int fftSize = Local_Config.FFTSize;
            int removeLength = Get_HopLength(fftSize, this.Top_FFT_OverLapPercentage);

            //Track a Top FFT size change made while this window is open.
            this.EnsureTopBufferCapacity(fftSize);

            this.Top_FFT_Tasks.Clear();

            if (fftSize <= 0)
                return;

            // Process Input Top FFT.
            //Same Peek/Advance pairing as the ULF path, see Plot_ULF_FFT_Timer_Tick.
            var Local_InputTop_Buffer = this.RTA_InputTopBuffer;
            if (this.chart_Input_Top_FFT.Visible &&
                HasFullFrame(Local_InputTop_Buffer, fftSize))
            {
                //PERF: reusable per-direction scratch, see Plot_ULF_FFT_Timer_Tick.
                var Local_InputTop_Scratch = EnsureExactScratch(ref this.InputTop_Scratch, fftSize);
                this.Top_FFT_Tasks.Add(Task.Run(() =>
                {
                    var data = Local_InputTop_Scratch;
                    _ = Local_InputTop_Buffer.Peek(data, 0, fftSize);
                    return this.Compute_Top_FFT_Data(Local_Config, Local_Config.InputFFT, data);
                })
                .ContinueWith(t =>
                {
                    //Fault handling as in the ULF path, see Plot_ULF_FFT_Timer_Tick.
                    try
                    {
                        if (!t.IsCompletedSuccessfully)
                        {
                            if (t.Exception != null)
                                Debug.ReportSwallowed(t.Exception);
                            return;
                        }

                        var (xData, magLog) = t.Result;
                        if (xData.Length > 0 && magLog.Length > 0)
                        {
                            if (!this.IsClosing && !this.IsDisposed && this.IsHandleCreated)
                                this.SafeInvoke(() =>
                                    // Use frequency range constants directly here: 10 Hz to 20000 Hz.
                                    this.Plot_FFT(this.chart_Input_Top_FFT, 10, 20000, xData, magLog, ref this.LastFFTUpdateTop));
                        }
                    }
                    finally
                    {
                        Local_InputTop_Buffer.Advance(removeLength);
                    }
                }));
            }

            // Process Output Top FFT.
            var Local_OutputTop_Buffer = this.RTA_OutputTopBuffer;
            if (this.chart_Output_Top_FFT.Visible &&
                HasFullFrame(Local_OutputTop_Buffer, fftSize))
            {
                var Local_OutputTop_Scratch = EnsureExactScratch(ref this.OutputTop_Scratch, fftSize);
                this.Top_FFT_Tasks.Add(Task.Run(() =>
                {
                    var data = Local_OutputTop_Scratch;
                    _ = Local_OutputTop_Buffer.Peek(data, 0, fftSize);
                    return this.Compute_Top_FFT_Data(Local_Config, Local_Config.OutputFFT, data);
                })
                .ContinueWith(t =>
                {
                    //Fault handling as in the ULF path, see Plot_ULF_FFT_Timer_Tick.
                    try
                    {
                        if (!t.IsCompletedSuccessfully)
                        {
                            if (t.Exception != null)
                                Debug.ReportSwallowed(t.Exception);
                            return;
                        }

                        var (xData, magLog) = t.Result;
                        if (xData.Length > 0 && magLog.Length > 0)
                        {
                            if (!this.IsClosing && !this.IsDisposed && this.IsHandleCreated)
                                this.SafeInvoke(() =>
                                    // Use frequency range constants directly here: 10 Hz to 20000 Hz.
                                    this.Plot_FFT(this.chart_Output_Top_FFT, 10, 20000, xData, magLog, ref this.LastFFTUpdateTop));
                        }
                    }
                    finally
                    {
                        Local_OutputTop_Buffer.Advance(removeLength);
                    }
                }));
            }

            await Task.WhenAll(this.Top_FFT_Tasks);
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
        finally
        {
            this.Top_Tick_InFlight = false;

            //DEFECT FIX: see RestartTimerIfAlive - unguarded member access from the finally of an
            //async void handler crashed the app when the form closed mid-await.
            this.RestartTimerIfAlive(this.timer_Plot_Top_FFTs);
        }
    }

    /// <summary>
    /// Band-limits, decimates and transforms one ULF frame.
    /// </summary>
    /// <param name="config">The snapshot this frame is being analysed under. Never re-read fields.</param>
    /// <param name="fft">The FFT instance belonging to this direction, taken from the snapshot.</param>
    /// <param name="decimator">The decimator belonging to this direction, taken from the snapshot.</param>
    /// <param name="timeSeries">One second of audio at the input sample rate.</param>
    /// <param name="decimated">Scratch of exactly <c>config.FFTSize</c> entries.</param>
    /// <returns>The frequency span and the magnitudes in dBV, or empty arrays to skip the plot.</returns>
    protected (double[] xData, double[] magLog) Compute_ULF_FFT_Data(
        AnalysisConfig config, FFT fft, AntiAliasDecimator? decimator, double[] timeSeries, double[] decimated)
    {
        //A snapshot without decimators is not a ULF snapshot; skip the frame rather than plot
        //something that was never band-limited.
        if (decimator == null)
            return (Array.Empty<double>(), Array.Empty<double>());

        if (timeSeries.Length < config.FFTSize || decimated.Length != config.FFTSize)
            return (Array.Empty<double>(), Array.Empty<double>());

        //DEFECT FIX: this used DownSampler.downsample - Largest Triangle Three Buckets, a PLOT
        //THINNING algorithm. It emits the most extreme sample of each bucket at that sample's own
        //position, so the result was non-uniformly spaced, biased towards peaks, and band-limited
        //not at all: every component above 1 kHz folded straight down into the plotted 1-100 Hz
        //span. AntiAliasDecimator filters first and then resamples uniformly.
        decimator.Decimate(timeSeries, timeSeries.Length, decimated);

        // Perform a FFT
        Complex[] FFTResult = fft.Perform_FFT(decimated, config.WindowCoefficients);
        var HalfLength = FFTResult.Length / 2 + 1;
        var RealResult = new Complex[HalfLength];
        Array.Copy(FFTResult, RealResult, HalfLength);

        double[] magResult = DSP.ConvertComplex.ToMagnitude(RealResult);
        double[] magLog = DSP.ConvertMagnitude.ToMagnitudeDBV(
                             magResult,
                             config.WindowScaleFactor * Math.Sqrt(2),
                             -400d);

        // Return just the data needed to plot.
        return (config.FrequencySpan, magLog);
    }

    /// <summary>
    /// Transforms one Top FFT frame.
    /// </summary>
    /// <param name="config">The snapshot this frame is being analysed under. Never re-read fields.</param>
    /// <param name="fft">The FFT instance belonging to this direction, taken from the snapshot.</param>
    /// <param name="timeSeries">Exactly <c>config.FFTSize</c> samples at the input sample rate.</param>
    /// <returns>The frequency span and the magnitudes in dBV, or empty arrays to skip the plot.</returns>
    protected (double[] xData, double[] magLog) Compute_Top_FFT_Data(
        AnalysisConfig config, FFT fft, double[] timeSeries)
    {
        //The frame, the window and the FFT all come from one snapshot, so these lengths agree by
        //construction. The guard stays as a backstop against a mis-sized scratch buffer.
        if (timeSeries.Length != config.FFTSize)
            return (Array.Empty<double>(), Array.Empty<double>());

        // Perform the FFT using the snapshot's FFT instance & window coefficients.
        Complex[] fftResult = fft.Perform_FFT(timeSeries, config.WindowCoefficients);

        // Keep only the real, non-mirrored half.
        int halfLength = fftResult.Length / 2 + 1;
        Complex[] realResult = new Complex[halfLength];
        Array.Copy(fftResult, realResult, halfLength);

        // Convert to magnitude.
        double[] magResult = DSP.ConvertComplex.ToMagnitude(realResult);

        // Convert magnitude to dBV with your original scale factor & floor of -400 dB.
        double[] magLog = DSP.ConvertMagnitude.ToMagnitudeDBV(
            magResult,
            config.WindowScaleFactor * Math.Sqrt(2),
            -400d
        );

        return (config.FrequencySpan, magLog);
    }

    #endregion

    #region ChartMouseMove
    [SupportedOSPlatform("windows")]
    protected async void Chart_MouseMove(object? sender, MouseEventArgs e)
    {
        try
        {
            if (sender is not Chart chart || chart.ChartAreas.Count < 1 || chart.Titles.Count < 6)
                return;

            ChartArea ca = chart.ChartAreas[0];

            // Check if the mouse is within the inner plot area.
            RectangleF innerRect = GetInnerPlotPositionClientRect(chart, ca);
            if (!innerRect.Contains(e.Location))
                return;

            // Throttle: only process if enough time has passed.
            if ((DateTime.Now - LastMouseMoveUpdate).TotalMilliseconds < MouseMoveThrottleMs)
                return;
            LastMouseMoveUpdate = DateTime.Now;

            // Capture values needed and compute synchronously on UI thread to avoid
            // cross-thread chart API usage and extra Task allocations.
            int pixelX = e.X;
            int pixelY = e.Y;
            double sampleRate = Program.DSP_Info.InSampleRate;

            double xValue = Math.Pow(10, ca.AxisX.PixelPositionToValue(pixelX));
            double yValue = ca.AxisY.PixelPositionToValue(pixelY);
            string newTitle = xValue < sampleRate * 0.5 ? $"Mouse: {xValue:0.0} | {yValue:0.0}" : string.Empty;

            if (!string.IsNullOrEmpty(newTitle) && !this.IsClosing && !this.IsDisposed && this.IsHandleCreated)
                chart.Titles[5].Text = newTitle;
        }
        catch (Exception ex)
        {
            Error(ex);
        }
    }

    #endregion

    #region IntervalChanged
    //DEFECT FIX: these three TextChanged handlers used int.Parse on live keystrokes. Typing "-",
    //"." or an over-long number raised FormatException/OverflowException, which this.Error routed
    //to Debug.Error - i.e. the user got "A fatal error has occured" and "Press Yes to abort the
    //app" for a partially typed number. TryParse and simply ignore incomplete input, matching the
    //existing TryParse handlers in ctl_GeneralConfigPage and FormMixer.
    protected void Msb_WaveForm_RefreshInterval_TextChanged(object? sender, EventArgs e)
    {
        try
        {
            if (string.IsNullOrEmpty(this.msb_WaveForm_RefreshInterval.Text))
                this.msb_WaveForm_RefreshInterval.Text = "1";

            if (int.TryParse(this.msb_WaveForm_RefreshInterval.Text, out int Local_Interval))
                this.timer_PlotWaveforms.Interval = Math.Max(1, Local_Interval);
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }

    protected void Msb_TopFFT_RefreshInterval_TextChanged(object? sender, EventArgs e)
    {
        try
        {
            if (string.IsNullOrEmpty(this.msb_Top_FFT_RefreshInterval.Text))
                this.msb_Top_FFT_RefreshInterval.Text = "1";

            if (int.TryParse(this.msb_Top_FFT_RefreshInterval.Text, out int Local_Interval))
                this.timer_Plot_Top_FFTs.Interval = Math.Max(1, Local_Interval);
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }

    protected void Msb_ULF_FFT_RefreshInterval_TextChanged(object? sender, EventArgs e)
    {
        try
        {
            if (string.IsNullOrEmpty(this.msb_ULF_FFT_RefreshInterval.Text))
                this.msb_ULF_FFT_RefreshInterval.Text = "1";

            if (int.TryParse(this.msb_ULF_FFT_RefreshInterval.Text, out int Local_Interval))
                this.timer_Plot_ULF_FFT.Interval = Math.Max(1, Local_Interval);
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }
    #endregion

    #region Pause
    protected void Pause_CHK_CheckedChanged(object sender, EventArgs e)
    {
        try
        {
            this.timer_PlotWaveforms.Enabled = !this.Pause_CHK.Checked;
            this.timer_Plot_Top_FFTs.Enabled = !this.Pause_CHK.Checked;
            this.timer_Plot_ULF_FFT.Enabled = !this.Pause_CHK.Checked;
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }
    #endregion

    #endregion

    #region Protected Functions

    #region Form Init
    protected void Init_CheckedListBoxList()
    {
        //Enable all checkboxes in the checkbox list               
        this.checkedListBox1.SetItemChecked(0, true);
        this.checkedListBox1.SetItemChecked(1, true);
        this.checkedListBox1.SetItemChecked(2, true);
        this.checkedListBox1.SetItemChecked(3, true);
        this.checkedListBox1.SetItemChecked(4, true);
        this.checkedListBox1.SetItemChecked(5, true);
    }

    protected void Init_Comboboxes()
    {
        this.cbo_ULF_FFT_Window_Type.DataSource = Enum.GetNames<DSP.Window.Type>();
        this.cbo_Top_FFT_Window_Type.DataSource = Enum.GetNames<DSP.Window.Type>();
    }

    protected void Init_SetDefault_Combobox_Options()
    {
        this.cbo_Top_FFT_Window_Type.SelectedIndex = 4;
        this.cbo_ULF_FFT_Window_Type.SelectedIndex = 4;
        this.cbo_Top_FFT_Size.SelectedIndex = 1;
        this.cbo_Top_FFT_Overlap.SelectedIndex = 2;
        this.cbo_ULF_FFT_Overlap.SelectedIndex = 4;
    }

    protected void Init_Timers()
    {
        //Enable all timers
        this.timer_PlotWaveforms.Enabled = true;
        this.timer_Plot_Top_FFTs.Enabled = true;
        this.timer_Plot_ULF_FFT.Enabled = true;
    }
    #endregion

    #region MapEventHandlers
    [SupportedOSPlatform("windows")]
    protected void MapEventHandlers()
    {
        this.msb_Top_FFT_RefreshInterval.TextChanged += Msb_TopFFT_RefreshInterval_TextChanged;
        this.msb_WaveForm_RefreshInterval.TextChanged += Msb_WaveForm_RefreshInterval_TextChanged;
        this.msb_ULF_FFT_RefreshInterval.TextChanged += Msb_ULF_FFT_RefreshInterval_TextChanged;

        this.cbo_ULF_FFT_Window_Type.SelectedIndexChanged += ULF_FFT_Window_Type_CBO_SelectedIndexChanged;
        this.cbo_Top_FFT_Window_Type.SelectedIndexChanged += Top_FFT_Window_Type_CBO_SelectedIndexChanged;
        this.cbo_Top_FFT_Size.SelectedIndexChanged += Top_FFT_Size_CBO_SelectedIndexChanged;
        this.cbo_ULF_FFT_Overlap.SelectedIndexChanged += ULF_FFT_Overlap_CBO_SelectedIndexChanged;
        this.cbo_Top_FFT_Overlap.SelectedIndexChanged += Top_FFT_Overlap_CBO_SelectedIndexChanged;

        Program.ASIO.InputDataAvailable += this.ASIO_InputDataAvailable;
        Program.ASIO.OutputDataAvailable += this.ASIO_OutputDataAvailable;

        this.FormClosing += this.RTA_FormClosing;

        this.chart_Input_ULF_FFT.MouseMove += this.Chart_MouseMove;
        this.chart_Input_Top_FFT.MouseMove += this.Chart_MouseMove;
        this.chart_Output_ULF_FFT.MouseMove += this.Chart_MouseMove;
        this.chart_Output_Top_FFT.MouseMove += this.Chart_MouseMove;

        this.checkedListBox1.ItemCheck += CheckedListBox1_ItemCheck;
    }
    #endregion

    #region Chart Visibility Changed
    protected void CheckedListBox1_ItemCheck(object? sender, ItemCheckEventArgs e)
    {
        var Checked = e.NewValue == CheckState.Checked;
        Control? Control = this.GetChartByCheckboxIndex(e.Index);

        if (Control != null)
            Control.Visible = Checked;

        //DEFECT FIX: both the ASIO writes and the plot ticks are gated on chart.Visible, so a chart
        //that gets unchecked leaves roughly a frame of audio frozen in its buffer and replays it,
        //frame by overlapped frame, when it is re-checked - stale audio spliced onto live audio,
        //which reads as a burst of broadband noise across the whole span. Empty it on hide; nothing
        //writes to it again until it is shown.
        if (!Checked)
            this.Discard_BufferedAudio(e.Index);

        //If one item is left checked
        if (Checked & this.checkedListBox1.CheckedItems.Count == 0 || !Checked & this.checkedListBox1.CheckedItems.Count == 2)
        {
            //Hide all panels
            this.tableLayoutPanel1.RowStyles[0].SizeType = SizeType.Absolute;
            this.tableLayoutPanel1.RowStyles[0].Height = 0;

            this.tableLayoutPanel1.RowStyles[1].SizeType = SizeType.Absolute;
            this.tableLayoutPanel1.RowStyles[1].Height = 0;

            this.tableLayoutPanel1.ColumnStyles[0].SizeType = SizeType.Absolute;
            this.tableLayoutPanel1.ColumnStyles[0].Width = 0;

            this.tableLayoutPanel1.ColumnStyles[1].SizeType = SizeType.Absolute;
            this.tableLayoutPanel1.ColumnStyles[1].Width = 0;

            this.tableLayoutPanel1.ColumnStyles[2].SizeType = SizeType.Absolute;
            this.tableLayoutPanel1.ColumnStyles[2].Width = 0;

            //If one checked item remains, find it
            if (!Checked)
            {
                //PERF: CheckedIndexCollection is non-generic IEnumerable - foreach allocated an
                //enumerator AND boxed every int only to immediately unbox it. Index instead.
                var Local_CheckedIndices = this.checkedListBox1.CheckedIndices;
                for (int i = 0; i < Local_CheckedIndices.Count; i++)
                {
                    var CheckedIndex = Local_CheckedIndices[i];
                    if (CheckedIndex != e.Index)
                    {
                        Control = this.GetChartByCheckboxIndex(CheckedIndex);
                        break;
                    }
                }
            }

            //Maximize the remaining table layout control
            if (Control != null)
            {
                var RowIndex = this.tableLayoutPanel1.GetRow(Control);
                var ColIndex = this.tableLayoutPanel1.GetColumn(Control);

                var RowStyle = this.tableLayoutPanel1.RowStyles[RowIndex];
                RowStyle.SizeType = SizeType.Percent;
                RowStyle.Height = 100;

                var ColStyle = this.tableLayoutPanel1.ColumnStyles[ColIndex];
                ColStyle.SizeType = SizeType.Percent;
                ColStyle.Width = 100;
            }
        }
        else //Set table layout back to normal
        {
            this.tableLayoutPanel1.RowStyles[0].SizeType = SizeType.Percent;
            this.tableLayoutPanel1.RowStyles[0].Height = 50;

            this.tableLayoutPanel1.RowStyles[1].SizeType = SizeType.Percent;
            this.tableLayoutPanel1.RowStyles[1].Height = 50;

            this.tableLayoutPanel1.ColumnStyles[0].SizeType = SizeType.Percent;
            this.tableLayoutPanel1.ColumnStyles[0].Width = 33;

            this.tableLayoutPanel1.ColumnStyles[1].SizeType = SizeType.Percent;
            this.tableLayoutPanel1.ColumnStyles[1].Width = 33;

            this.tableLayoutPanel1.ColumnStyles[2].SizeType = SizeType.Percent;
            this.tableLayoutPanel1.ColumnStyles[2].Width = 33;
        }
    }

    protected Control? GetChartByCheckboxIndex(int index)
    {
        switch (index)
        {
            case 0:
                return this.chart_InputWaveform;
            case 1:
                return this.chart_OutputWaveform;
            case 2:
                return this.chart_Input_ULF_FFT;
            case 3:
                return this.chart_Output_ULF_FFT;
            case 4:
                return this.chart_Input_Top_FFT;
            case 5:
                return this.chart_Output_Top_FFT;
        }
        return null;
    }

    /// <summary>
    /// Throws away whatever a hidden chart's analysis buffer is still holding, so it cannot splice
    /// stale audio onto live audio when the chart is shown again.
    /// </summary>
    /// <param name="checkboxIndex">The checked-list index of the chart being hidden.</param>
    /// <remarks>
    /// The buffer INSTANCE is replaced rather than reset: a tick already in flight captured the old
    /// instance into a local, so its Peek and its matching Advance both complete harmlessly on the
    /// orphan instead of racing a reset between them.
    /// </remarks>
    protected void Discard_BufferedAudio(int checkboxIndex)
    {
        switch (checkboxIndex)
        {
            //0 and 1 are the waveform charts, which plot the raw ASIO snapshot and buffer nothing.
            case 2:
                this.RTA_InputULFBuffer = new(Get_ULF_BufferLength(Program.DSP_Info.InSampleRate));
                break;
            case 3:
                this.RTA_OutputULFBuffer = new(Get_ULF_BufferLength(Program.DSP_Info.InSampleRate));
                break;
            //Sized from the published snapshot, not from Default_Top_FFTSize, so this agrees with
            //whatever the plot timer is currently analysing under.
            case 4:
                this.RTA_InputTopBuffer = new(Get_Top_BufferLength(this.Top_Config.FFTSize));
                break;
            case 5:
                this.RTA_OutputTopBuffer = new(Get_Top_BufferLength(this.Top_Config.FFTSize));
                break;
        }
    }
    #endregion

    #region PreCalculateWindowCoefficients
    /// <summary>
    /// Reads the selected window type out of a combo box, falling back to the current setting when
    /// the combo has not been populated yet (the constructor runs before Init_Comboboxes).
    /// </summary>
    /// <param name="comboBox">The window type combo box.</param>
    /// <param name="fftSize">The FFT length the coefficients must match.</param>
    /// <returns>The coefficients and their amplitude correction factor.</returns>
    protected static (double[] Coefficients, double ScaleFactor) Build_Window(ComboBox comboBox, int fftSize)
    {
        var Local_Selected = comboBox.SelectedItem?.ToString();

        //Fall back to a rectangular (all ones) window rather than an all-zero array: zeros multiply
        //the whole frame away and hand the dB conversion a spectrum of silence, so a snapshot built
        //before the combo boxes are populated would plot the noise floor instead of the signal.
        if (string.IsNullOrEmpty(Local_Selected) || !Enum.TryParse<DSP.Window.Type>(Local_Selected, out var Local_Type))
            Local_Type = DSP.Window.Type.Rectangular;

        var Local_Coefficients = DSP.Window.Coefficients(Local_Type, fftSize);
        return (Local_Coefficients, DSP.Window.ScaleFactor.Signal(Local_Coefficients));
    }

    /// <summary>
    /// Builds a fresh Top FFT snapshot. Every field a running analysis depends on is constructed
    /// here, so publishing it is one reference assignment and a reader sees all of it or none.
    /// </summary>
    /// <returns>The new snapshot.</returns>
    protected AnalysisConfig Build_Top_Config()
    {
        int Local_FFTSize = this.Default_Top_FFTSize;
        int Local_SampleRate = Program.DSP_Info.InSampleRate;

        var (Local_Coefficients, Local_ScaleFactor) = Build_Window(this.cbo_Top_FFT_Window_Type, Local_FFTSize);

        var Local_InputFFT = new FFT(Local_FFTSize);
        var Local_OutputFFT = new FFT(Local_FFTSize);

        var Local_FSpan = Local_InputFFT.FrequencySpan(Local_SampleRate);
        if (Local_FSpan.Length > 0)
            Local_FSpan[0] = 0.0001;

        return new AnalysisConfig
        {
            FFTSize = Local_FFTSize,
            InputFFT = Local_InputFFT,
            OutputFFT = Local_OutputFFT,
            WindowCoefficients = Local_Coefficients,
            WindowScaleFactor = Local_ScaleFactor,
            FrequencySpan = Local_FSpan,
            SampleRate = Local_SampleRate
        };
    }

    /// <summary>
    /// Publishes a fresh Top FFT snapshot in one reference assignment.
    /// </summary>
    protected void ReCalculate_Top_FFT()
    {
        var Local_Config = this.Build_Top_Config();
        this.Top_Config = Local_Config;

        //DEFECT FIX: this re-allocated the Top buffers at ten frames deep, contradicting
        //Get_Top_BufferLength. Every window-type and FFT-size change installed a ten-frame buffer
        //that the very next tick discarded through EnsureTopBufferCapacity, throwing buffered audio
        //away twice and swapping the instance under the ASIO producer an extra time.
        this.EnsureTopBufferCapacity(Local_Config.FFTSize);
    }

    /// <summary>
    /// Builds a fresh ULF FFT snapshot, including the two band-limiting decimators.
    /// </summary>
    /// <returns>The new snapshot.</returns>
    /// <remarks>
    /// The ULF frequency span is derived from the DECIMATED rate, which is the FFT size itself: one
    /// second of audio reduced to <c>Default_ULF_FFTSize</c> points is that many samples per second,
    /// so the span runs 0 Hz to half of it in exactly 1 Hz steps.
    /// </remarks>
    protected AnalysisConfig Build_ULF_Config()
    {
        int Local_FFTSize = this.Default_ULF_FFTSize;

        var (Local_Coefficients, Local_ScaleFactor) = Build_Window(this.cbo_ULF_FFT_Window_Type, Local_FFTSize);

        var Local_InputFFT = new FFT(Local_FFTSize);
        var Local_OutputFFT = new FFT(Local_FFTSize);

        var Local_FSpan = Local_InputFFT.FrequencySpan(Local_FFTSize);
        if (Local_FSpan.Length > 0)
            Local_FSpan[0] = 0.0001;

        return new AnalysisConfig
        {
            FFTSize = Local_FFTSize,
            InputFFT = Local_InputFFT,
            OutputFFT = Local_OutputFFT,
            WindowCoefficients = Local_Coefficients,
            WindowScaleFactor = Local_ScaleFactor,
            FrequencySpan = Local_FSpan,
            SampleRate = Program.DSP_Info.InSampleRate,
            //Borrowed, not built: see the fields. Stateful scratch, so one per direction. This
            //replaces the Largest-Triangle plot thinner that used to feed the ULF FFT unfiltered,
            //aliased and non-uniformly spaced samples.
            InputDecimator = this.InputULF_Decimator,
            OutputDecimator = this.OutputULF_Decimator
        };
    }

    /// <summary>
    /// Publishes a fresh ULF FFT snapshot in one reference assignment.
    /// </summary>
    protected void ReCalculate_ULF_FFT()
    {
        this.ULF_Config = this.Build_ULF_Config();
    }
    #endregion

    #region FFT Charts Logic
    /// <summary>
    /// Converts a displayed frequency range into the half-open bin range covering it, so the
    /// Max/Min readout can only ever name a bin that is actually on screen.
    /// </summary>
    /// <param name="frequencySpan">The chart's X data, ascending, one entry per bin.</param>
    /// <param name="minimumHz">The lowest displayed frequency.</param>
    /// <param name="maximumHz">The highest displayed frequency.</param>
    /// <returns>The first bin at or above the minimum, and one past the last bin at or below the maximum.</returns>
    protected static (int From, int To) Get_BinRangeForFrequencies(double[] frequencySpan, double minimumHz, double maximumHz)
    {
        if (frequencySpan == null || frequencySpan.Length == 0)
            return (0, 0);

        int Local_From = -1;
        int Local_To = 0;

        for (int i = 0; i < frequencySpan.Length; i++)
        {
            double Local_Frequency = frequencySpan[i];
            if (Local_Frequency < minimumHz)
                continue;
            if (Local_Frequency > maximumHz)
                break;

            if (Local_From < 0)
                Local_From = i;
            Local_To = i + 1;
        }

        return Local_From < 0 ? (0, 0) : (Local_From, Local_To);
    }

    [SupportedOSPlatform("windows")]
    protected void Plot_FFT(Chart chartControl, int min, int max, double[] xData, double[] yData, ref DateTime lastUpdate)
    {
        try
        {
            // Check that the control and chart are ready.
            if (this.IsClosing || this.IsDisposed || !this.IsHandleCreated)
                return;
            if (chartControl.IsDisposed || !chartControl.IsHandleCreated || chartControl.ChartAreas.Count < 1)
                return;
            if (chartControl.Series.IndexOf("Series1") < 0)
                return;

            // Update chart series and axes.
            chartControl.SuspendLayout();
            chartControl.Series["Series1"].Points.Clear();
            chartControl.Series["Series1"].Points.DataBindXY(xData, yData);

            var area = chartControl.ChartAreas[0];
            area.AxisY.Interval = 12;
            area.AxisY.IntervalType = DateTimeIntervalType.Number;
            area.AxisY.Maximum = 0;
            area.AxisY.Minimum = -144;

            area.AxisX.IntervalType = DateTimeIntervalType.Number;
            area.AxisX.MinorGrid.Enabled = true;
            area.AxisX.MinorGrid.Interval = 1;
            area.AxisX.Minimum = min;
            area.AxisX.Maximum = max;
            area.AxisX.IsLogarithmic = true;
            chartControl.ResumeLayout();

            // Throttle title updates.
            if ((DateTime.Now - lastUpdate).TotalMilliseconds < FFTThrottleMs)
                return;
            lastUpdate = DateTime.Now;

            //DEFECT FIX: min and max are HERTZ - they are the axis limits set above - but they were
            //handed straight to FindMaxPosition/FindMinPosition, which take BIN INDICES. The ULF
            //charts searched bins 0..100 of a 1 Hz-per-bin spectrum, which happens to be close, but
            //still started at the DC bin that the axis deliberately excludes; the Top charts passed
            //20000, which clamps to the whole spectrum, so the readout could name a bin far below
            //the 10 Hz the axis starts at. That is why the screenshot reads "Min: 0.0" on a chart
            //whose X axis begins at 1 Hz.
            var (searchFrom, searchTo) = Get_BinRangeForFrequencies(xData, min, max);
            if (searchTo <= searchFrom)
                return;

            int maxIndex = DSP.Analyze.FindMaxPosition(yData, searchFrom, searchTo);
            int minIndex = DSP.Analyze.FindMinPosition(yData, searchFrom, searchTo);

            if (maxIndex < 0 || maxIndex >= xData.Length || minIndex < 0 || minIndex >= xData.Length)
                return;

            if (this.IsClosing || this.IsDisposed || !this.IsHandleCreated)
                return;
            if (chartControl.IsDisposed || !chartControl.IsHandleCreated)
                return;

            chartControl.Titles[3].Text = $"Max: {xData[maxIndex]:0.0} | {yData[maxIndex]:0.0}";
            chartControl.Titles[4].Text = $"Min: {xData[minIndex]:0.0} | {yData[minIndex]:0.0}";
        }
        catch (Exception ex)
        {
            //Cosmetic min/max chart titles only - never fail a plot over them, but keep the error
            //observable rather than discarding it silently.
            Debug.ReportSwallowed(ex);
        }
    }

    #endregion

    #region Waveform Charts Logic
    // One‑time initialization for a Chart control. This sets up properties
    // that don't change per tick (like the baseline strip line, Y‑axis label format,
    // and X‑axis starting point).
    [SupportedOSPlatform("windows")]
    protected void InitializeChart(Chart chartControl)
    {
        if (this.IsClosing || this.IsDisposed || !this.IsHandleCreated)
            return;
        if (chartControl.IsDisposed || !chartControl.IsHandleCreated || chartControl == null || chartControl.ChartAreas.Count < 1)
            return;

        // Use the Tag property to store a flag indicating initialization.
        if (chartControl.Tag is bool initialized && initialized)
            return;

        ChartArea area = chartControl.ChartAreas[0];

        // Create and add a baseline strip line at y = 0.
        var line = new StripLine()
        {
            BorderColor = Color.Black,
            Interval = 0,
            IntervalOffset = 0,
            StripWidth = 0,
            StripWidthType = DateTimeIntervalType.NotSet
        };
        area.AxisY.StripLines.Clear();
        area.AxisY.StripLines.Add(line);

        // Format the Y‑axis labels.
        area.AxisY.LabelStyle.Format = "0.0000";

        // Set the X‑axis to start from zero.
        area.AxisX.IsStartedFromZero = true;

        // Mark this chart as initialized.
        chartControl.Tag = true;
    }

    // Updates a Chart control with the computed waveform data. This method is
    // invoked on the UI thread and assumes that the chart has been initialized.
    [SupportedOSPlatform("windows")]
    protected void UpdateChartWithPlotData(Chart chartControl, WaveformPlotData plotData, DateTime timestamp)
    {
        //DEFECT FIX: these four early-returns sat OUTSIDE the try, so whenever the form was
        //closing / the chart was not ready the rented ArrayPool buffers were dropped on the floor
        //instead of being returned - a slow pool drain on exactly the path that runs most often
        //during shutdown. Ownership of the rented arrays is now released in a finally.
        try
        {
            if (this.IsClosing || this.IsDisposed || !this.IsHandleCreated)
                return;
            if (chartControl.IsDisposed || !chartControl.IsHandleCreated || chartControl.ChartAreas.Count < 1)
                return;
            if (chartControl.Series.IndexOf("Series1") < 0)
                return;

            // Perform one‑time initialization if not already done.
            this.InitializeChart(chartControl);

            chartControl.SuspendLayout();

            // Set basic axis properties.
            ChartArea area = chartControl.ChartAreas[0];
            area.AxisX.IntervalType = DateTimeIntervalType.Number;
            area.AxisY.IntervalType = DateTimeIntervalType.Number;
            area.AxisX.Minimum = plotData.XMinimum;
            area.AxisX.Maximum = plotData.XMaximum;
            area.AxisX.Interval = plotData.XInterval;

            // Clear existing data points and bind new data.
            chartControl.Series["Series1"].Points.Clear();
            // Use the ArraySegment<T> instances to avoid allocating new arrays when possible.
            chartControl.Series["Series1"].Points.DataBindXY((System.Collections.IEnumerable)plotData.XData, (System.Collections.IEnumerable)plotData.YDataDec);

            // Roll this block's magnitude into the chart's ten second range envelope. The envelope
            // rises with the signal at once and eases back down, so the axis never steps.
            var Local_Range = this.Get_WaveformRange(chartControl);
            double Local_Magnitude = Local_Range == null
                ? plotData.YMaximum
                : Local_Range.Update(plotData.YMaximum, timestamp);

            //An unset MS Chart axis reads back as NaN, and every comparison against NaN is false -
            //which is why the old grow-only test never assigned a range to a fresh chart at all.
            if (double.IsNaN(area.AxisY.Maximum) || double.IsNaN(area.AxisY.Minimum) ||
                Math.Abs(area.AxisY.Maximum - Local_Magnitude) > Local_Magnitude * WaveformRangeUpdateThreshold)
            {
                area.AxisY.Maximum = Local_Magnitude;
                area.AxisY.Minimum = -Local_Magnitude;
                area.AxisY.Interval = 0;
            }

            chartControl.ResumeLayout();
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
        finally
        {
            // Return rented arrays to the shared pool to reduce GC pressure.
            try
            {
                if (plotData.RentedXArray != null)
                {
                    ArrayPool<double>.Shared.Return(plotData.RentedXArray);
                    plotData.RentedXArray = null;
                }

                if (plotData.RentedYArray != null)
                {
                    ArrayPool<decimal>.Shared.Return(plotData.RentedYArray);
                    plotData.RentedYArray = null;
                }
            }
            catch (Exception ex)
            {
                // Returning to the pool must never crash a UI update, but do keep it observable -
                // a repeated failure here means the pool is being corrupted.
                Debug.ReportSwallowed(ex);
            }
        }
    }

    // Computes all the data needed to plot a waveform. This method is called on a
    // background thread and performs operations like generating X‑axis data,
    // converting the Y data to decimals, and computing axis scales.
    protected WaveformPlotData ComputeWaveformPlotData(double[] yData, double scaleYAxis)
    {
        int length = yData?.Length ?? 0;
        if (length == 0)
            return new WaveformPlotData();

        // Rent arrays from the shared pool to avoid per-frame allocations.
        double[] rentedX = ArrayPool<double>.Shared.Rent(length);
        decimal[] rentedY = ArrayPool<decimal>.Shared.Rent(length);

        double mag;
        try
        {
            // Fill X as simple linear indices (0 .. length-1) — faster than LinSpace.
            for (int i = 0; i < length; i++)
                rentedX[i] = i;

            //DEFECT FIX: the Y limits were computed as
            //  maxCandidate = Min(yData.Max() * scale, scale)
            //  minCandidate = Min(yData.Min() * scale, -0.0001)
            //  mag          = Max(|maxCandidate|, |minCandidate|)
            //Math.Min returns the SMALLER value, so the second line only floors minCandidate at
            //-0.0001; it never bounds its magnitude the way the first line bounds the positive
            //side. A -4.0 sample therefore produced mag = 6.0 while an otherwise identical +4.0
            //sample produced the clamped mag = 1.5. Since mag drives the axis and now feeds a ten
            //second peak envelope, one negative overshoot zoomed the chart out four-fold and held
            //it there. Fold a symmetric absolute peak into the conversion loop instead, which also
            //drops the two extra LINQ passes over the block.
            double Local_BlockPeak = 0d;
            for (int i = 0; i < length; i++)
            {
                double Local_Sample = yData![i];

                //(decimal)NaN, (decimal)Infinity and anything beyond decimal's range all throw
                //OverflowException - on a background thread, after both pool rentals. Sanitising
                //here leaves every realistic sample untouched; only the RANGE is clamped, below.
                if (!double.IsFinite(Local_Sample))
                    Local_Sample = 0d;
                else if (Local_Sample > DecimalSafeSample)
                    Local_Sample = DecimalSafeSample;
                else if (Local_Sample < -DecimalSafeSample)
                    Local_Sample = -DecimalSafeSample;

                rentedY[i] = (decimal)Local_Sample;

                double Local_Absolute = Math.Abs(Local_Sample);
                if (Local_Absolute > Local_BlockPeak)
                    Local_BlockPeak = Local_Absolute;
            }

            mag = Math.Min(Local_BlockPeak * scaleYAxis, scaleYAxis);
            mag = Math.Max(mag, 0.0001);
        }
        catch
        {
            //The rentals are only released in UpdateChartWithPlotData's finally, which is never
            //reached if this method throws - every failed block would leak one array of each pool.
            ArrayPool<double>.Shared.Return(rentedX);
            ArrayPool<decimal>.Shared.Return(rentedY);
            throw;
        }

        double xMin = 0;
        double xMax = length;
        double xInterval = length * 0.25;

        return new WaveformPlotData
        {
            RentedXArray = rentedX,
            XData = new ArraySegment<double>(rentedX, 0, length),
            RentedYArray = rentedY,
            YDataDec = new ArraySegment<decimal>(rentedY, 0, length),
            XMinimum = xMin,
            XMaximum = xMax,
            XInterval = xInterval,
            YMaximum = mag,
            YMinimum = -mag
        };
    }

    // Data container for all the computed data needed for a chart update.
    protected class WaveformPlotData
    {
        // Rented arrays (returned to ArrayPool after UI bind).
        public double[]? RentedXArray { get; set; }
        public decimal[]? RentedYArray { get; set; }

        // Use ArraySegment to represent the valid slice of the rented arrays.
        public ArraySegment<double> XData { get; set; } = new(Array.Empty<double>());
        public ArraySegment<decimal> YDataDec { get; set; } = new(Array.Empty<decimal>());

        public double XMinimum { get; set; }
        public double XMaximum { get; set; }
        public double XInterval { get; set; }
        public double YMaximum { get; set; }
        public double YMinimum { get; set; }
    }

    // Container that pairs a Chart control with its computed waveform data.
    protected class ChartUpdateData
    {
        public Chart? Chart { get; set; }
        public WaveformPlotData? PlotData { get; set; }
    }

    #endregion

    #region ChartMouseArea
    [SupportedOSPlatform("windows")]
    private RectangleF GetInnerPlotPositionClientRect(Chart chart, ChartArea ca)
    {
        RectangleF innerPlot = ca.InnerPlotPosition.ToRectangleF();
        RectangleF areaRect = GetChartAreaClientRect(chart, ca);
        float widthFactor = areaRect.Width / 100f;
        float heightFactor = areaRect.Height / 100f;

        return new RectangleF(
            areaRect.X + widthFactor * innerPlot.X,
            areaRect.Y + heightFactor * innerPlot.Y,
            widthFactor * innerPlot.Width,
            heightFactor * innerPlot.Height);
    }

    [SupportedOSPlatform("windows")]
    private RectangleF GetChartAreaClientRect(Chart chart, ChartArea ca)
    {
        RectangleF area = ca.Position.ToRectangleF();
        float widthFactor = chart.ClientSize.Width / 100f;
        float heightFactor = chart.ClientSize.Height / 100f;
        return new RectangleF(
            widthFactor * area.X,
            heightFactor * area.Y,
            widthFactor * area.Width,
            heightFactor * area.Height);
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