#nullable enable

namespace BassThatHz_ASIO_DSP_Processor.GUI.Tabs;

using NAudio.Wave.Asio;

#region Usings
using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Diagnostics = System.Diagnostics;
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
public partial class ctl_StatsPage : UserControl
{
    #region Variables

    // Cached current process to avoid repeated allocations from Process.GetCurrentProcess()
    private readonly Process CurrentProcess;

    // Preserve previous GC latency mode so we can restore it when stopping DSP
    private readonly GCLatencyMode PreviousGCLatencyMode;

    #region String Formats
    protected readonly string ms_TimeFormat = "00.0000";
    protected readonly string Percentage_StringFormat = "00.00";
    protected string TimeSpanFormat = @"d\ \D\a\y\s\ \:\ hh\ \H\o\u\r\s\ \:\ mm\ \M\i\n\u\t\e\s\ \:\ ss\ \S\e\c\o\n\d\s";
    #endregion

    #region MS Diag PerformanceCounters
    //These Microsoft Counters cause a memory leak, so I don't use them. Should probably delete this code actually.
    //protected Diagnostics.PerformanceCounter PerformanceCounter_CPUTotal;
    //protected Diagnostics.PerformanceCounter PerformanceCounter_AppCPU;
    //protected Diagnostics.PerformanceCounter PerformanceCounter_UserTime;
    #endregion

    #region DSP Running Start/Stop Times
    protected DateTime DSP_StartTime = DateTime.MinValue;
    protected DateTime DSP_StopTime = DateTime.MinValue;
    #endregion

    #region Other misc State \ Stat variables
    protected double Input_Lat_ms = 0;
    protected double Output_Lat_ms = 0;
    protected double BufferSize_Lat_ms = 0;
    protected double Total_Buffer_Lat_ms = 0;
    protected double TotalDSP_Processing_Lat_ms = 0;
    protected double MaxDSP_Processing_Lat_ms = 0;
    protected double AverageDSP_Processing_Lat_ms = 0;
    protected bool No_GC_Set = false;
    protected long No_GC_CleanupLimitMB = 900L;
    #endregion

    #region Per-tick label caches
    //PERF: Update_Stats_Timer_Tick runs every second and used to allocate ~16 strings per tick
    //(plus a Label.Text set + repaint each) regardless of whether anything had actually changed.
    //These caches hold the LAST RAW VALUE so the ToString() is skipped entirely when it repeats,
    //which is the steady state whenever the DSP is idle.
    private long Last_RAM_Limit_MB = long.MinValue;
    private string? Last_RAM_Limit_Text;
    private long Last_RAM_MB = long.MinValue;
    private ProcessPriorityClass Last_PriorityClass = (ProcessPriorityClass)int.MinValue;
    private int Last_Underruns = int.MinValue;
    private int Last_UI_ThreadID = int.MinValue;
    private int Last_ASIO_ThreadID = int.MinValue;
    private int Last_TotalStreams = int.MinValue;
    private int Last_FilterCount = int.MinValue;
    private int Last_EnabledFilterCount = int.MinValue;
    private double Last_InputBufferConversion_ms = double.NaN;
    private double Last_OutputBufferConversion_ms = double.NaN;
    private double Last_TotalDSP_ms = double.NaN;
    private double Last_DSP_ms = double.NaN;
    private double Last_AverageDSP_ms = double.NaN;
    private double Last_MaxDSP_ms = double.NaN;
    private double Last_CurrentLoad_pct = double.NaN;
    private double Last_AverageLoad_pct = double.NaN;
    private double Last_MaxLoad_pct = double.NaN;
    private TimeSpan Last_AppUpTime = TimeSpan.MinValue;
    private TimeSpan Last_DSPRunTime = TimeSpan.MinValue;
    #endregion

    #endregion

    #region Per-tick Label Helpers
    /// <summary>
    /// Sets a label from a double only when the source value actually changed, so the
    /// ToString allocation and the WinForms text-change/repaint are both skipped otherwise.
    /// </summary>
    /// <param name="label">The label to update.</param>
    /// <param name="value">The current value.</param>
    /// <param name="format">The numeric format string.</param>
    /// <param name="cache">The caller's cache of the previously rendered value.</param>
    private static void SetTextIfChanged(Control label, double value, string format, ref double cache)
    {
        if (value.Equals(cache)) //Equals (not ==) so NaN == NaN compares true for the seed value.
            return;

        cache = value;
        label.Text = value.ToString(format);
    }

    /// <summary>
    /// Sets a label from an int only when the source value actually changed.
    /// </summary>
    /// <param name="label">The label to update.</param>
    /// <param name="value">The current value.</param>
    /// <param name="cache">The caller's cache of the previously rendered value.</param>
    private static void SetTextIfChanged(Control label, int value, ref int cache)
    {
        if (value == cache)
            return;

        cache = value;
        label.Text = value.ToString(CultureInfo.InvariantCulture);
    }
    #endregion

    #region Constructor
    public ctl_StatsPage()
    {
        InitializeComponent();

        //Microsoft Causes memory leak, disabled for now, investigate later.
        //this.Init_PerformanceCounters();

        //Only wire these events up once, because that is all we want to do
        Program.ASIO.Driver_ResetRequest += ASIO_Driver_ResetRequest;
        Program.ASIO.Driver_BufferSizeChanged += ASIO_Driver_BufferSizeChanged;
        Program.ASIO.Driver_ResyncRequest += ASIO_Driver_ResyncRequest;
        Program.ASIO.Driver_LatenciesChanged += ASIO_Driver_LatenciesChanged;
        Program.ASIO.Driver_Overload += ASIO_Driver_Overload;
        Program.ASIO.Driver_SampleRateChanged += ASIO_Driver_SampleRateChanged;

        // Cache current process instance for stats (avoids allocating a new Process each timer tick)
        this.CurrentProcess = Process.GetCurrentProcess();
        // Save previous GC latency mode to restore later
        this.PreviousGCLatencyMode = GCSettings.LatencyMode;
    }
    #endregion

    // Unsubscribe from ASIO events when the control's handle is destroyed to avoid memory leaks
    protected override void OnHandleDestroyed(EventArgs e)
    {
        try
        {
            Program.ASIO.Driver_ResetRequest -= ASIO_Driver_ResetRequest;
            Program.ASIO.Driver_BufferSizeChanged -= ASIO_Driver_BufferSizeChanged;
            Program.ASIO.Driver_ResyncRequest -= ASIO_Driver_ResyncRequest;
            Program.ASIO.Driver_LatenciesChanged -= ASIO_Driver_LatenciesChanged;
            Program.ASIO.Driver_Overload -= ASIO_Driver_Overload;
            Program.ASIO.Driver_SampleRateChanged -= ASIO_Driver_SampleRateChanged;
        }
        catch
        {
            // Best-effort unsubscribe; ignore any issues during handle destruction
        }

        base.OnHandleDestroyed(e);
    }

    #region LoadConfigRefresh
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public void LoadConfigRefresh()
    {
        this.chkEnableStats.Checked = Program.DSP_Info.EnableStats;
        this.chkEnableStats_CheckedChanged(this, EventArgs.Empty);

        if (Program.DSP_Info.AutoStartDSP)
            this.btnStart_ASIO_DSP_Click(this, EventArgs.Empty);
    }
    #endregion

    #region Event Handlers

    #region Start / Stop ASIO
    protected void btnStart_ASIO_DSP_Click(object? sender, EventArgs e)
    {
        try
        {
            var DSPInfo = Program.DSP_Info;

            if (String.IsNullOrEmpty(DSPInfo.ASIO_InputDevice))
            {
                _ = BassThatHz_ASIO_DSP_Processor.Debug.ShowMessage("Cannot start. No ASIO Device found.");
                return;
            }

            AsioDriverCapability? Capabilities = null;
            try
            {
                Capabilities = Program.ASIO.GetDriverCapabilities(DSPInfo.ASIO_InputDevice);
            }
            catch (Exception ex)
            {
                _ = ex;
                _ = BassThatHz_ASIO_DSP_Processor.Debug.ShowMessage("Cannot start. Can't fetch Driver Capabilities.");
            }
            if (Capabilities == null)
                return;

            var InputChannelCount = Capabilities.Value.InputChannelInfos.Length;
            var OutputChannelCount = Capabilities.Value.OutputChannelInfos.Length;
            DSPInfo.InChannelCount = InputChannelCount;
            DSPInfo.OutChannelCount = OutputChannelCount;

            this.DSP_StartTime = DateTime.Now;
            this.DSP_StopTime = DateTime.MinValue;

            if(!this.No_GC_Set)
                GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency;

            Program.ASIO.Start(DSPInfo.ASIO_InputDevice, DSPInfo.InSampleRate, InputChannelCount, OutputChannelCount);
            //Asynchronously update the Starting Stats after a delay (rather than synchronously with the UI thread that just initiatlized ASIO.)
            //I don't know if this is actually "needed" but it sounds like a good idea to me
            _ = Task.Run(async () =>
            {
                await Task.Delay(2000);
                this.ShowStartingStats();
            });
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }

    protected void btnStop_ASIO_DSP_Click(object? sender, EventArgs e)
    {
        try
        {
            Program.ASIO.Stop();
            if (!this.No_GC_Set)
                GCSettings.LatencyMode = this.PreviousGCLatencyMode;
            this.DSP_StopTime = DateTime.Now;
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }

    protected virtual void ASIO_Driver_SampleRateChanged()
    {
        this.btnStop_ASIO_DSP_Click(null, EventArgs.Empty);
    }

    protected virtual void ASIO_Driver_Overload()
    {
        this.btnStop_ASIO_DSP_Click(null, EventArgs.Empty);
    }

    protected virtual void ASIO_Driver_LatenciesChanged()
    {
        this.btnStop_ASIO_DSP_Click(null, EventArgs.Empty);
    }

    protected virtual void ASIO_Driver_ResyncRequest()
    {
        this.btnStop_ASIO_DSP_Click(null, EventArgs.Empty);
    }

    protected virtual void ASIO_Driver_BufferSizeChanged()
    {
        this.btnStop_ASIO_DSP_Click(null, EventArgs.Empty);
    }

    protected virtual void ASIO_Driver_ResetRequest()
    {
        this.btnStop_ASIO_DSP_Click(null, EventArgs.Empty);
    }
    #endregion

    #region Enable/Reset Stats handlers
    protected void chkEnableStats_CheckedChanged(object? sender, EventArgs e)
    {
        try
        {
            Program.DSP_Info.EnableStats = this.chkEnableStats.Checked;
            this.Update_Stats_Timer.Enabled = this.chkEnableStats.Checked;
            this.UpdateBiQuadsTotal_Timer.Enabled = this.chkEnableStats.Checked;
            if (this.UpdateBiQuadsTotal_Timer.Enabled)
                this.Show_Total_DSP_Filters();
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }

    protected void btn_ResetStats_Click(object? sender, EventArgs e)
    {
        try
        {
            this.ShowStartingStats();
            Program.ASIO.Clear_DSP_PeakProcessingTime();
            Program.ASIO.Clear_UnderrunsCounter();
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }
    #endregion

    #region Stat Calculation duration Timers
    protected void Update_Stats_Timer_Tick(object? sender, EventArgs e)
    {
        try
        {
            this.Update_Stats_Timer.Enabled = false;
            this.Show_Underruns();
            //this.Show_CPU_Usage();
            this.Show_DSPLatency();
            this.Show_ProcessPriorityAndRAMUsage();
            this.Show_UpTimes();
            this.Show_ThreadID();
            this.Update_Stats_Timer.Enabled = true;
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }

    protected void UpdateBiQuadsTotal_Timer_Tick(object? sender, EventArgs e)
    {
        try
        {
            this.UpdateBiQuadsTotal_Timer.Enabled = false;
            this.Show_Total_Streams();
            this.Show_Total_DSP_Filters();
            this.UpdateBiQuadsTotal_Timer.Enabled = true;
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }
    #endregion

    #region No Garbage Collection Timer
    
    protected void NoGC_Timer_Tick(object sender, EventArgs e)
    {
        try
        {
            // Refresh cached process info and check working set to avoid allocating a new Process
            this.CurrentProcess.Refresh();
            if (this.CurrentProcess.WorkingSet64 >= this.No_GC_CleanupLimitMB * 1024L * 1024L)
            {
                this.TrySetNoGC_Limit();
            }
        }
        catch (Exception ex)
        {
            // Best-effort working-set poll; recorded rather than silently discarded.
            BassThatHz_ASIO_DSP_Processor.Debug.ReportSwallowed(ex);
        }
    }

    /// <summary>
    /// Sets the RAM-limit label, skipping the format when the value has not changed.
    /// Avoids the per-call boxing of <c>long + string</c> (String.Concat(object, object)).
    /// </summary>
    protected void Set_RAM_Limit_Text()
    {
        if (this.No_GC_CleanupLimitMB == this.Last_RAM_Limit_MB && this.Last_RAM_Limit_Text != null)
        {
            this.lblRAM_Limit.Text = this.Last_RAM_Limit_Text;
            return;
        }

        this.Last_RAM_Limit_MB = this.No_GC_CleanupLimitMB;
        this.Last_RAM_Limit_Text = this.No_GC_CleanupLimitMB.ToString(CultureInfo.InvariantCulture) + "MB";
        this.lblRAM_Limit.Text = this.Last_RAM_Limit_Text;
    }

    protected void TrySetNoGC_Limit()
    {
        try
        {
            if (this.No_GC_Set)
            {
                this.TryEndNoGCRegion();
                GC.Collect();
            }

            // Use safe explicit sizes
            long twoGB = 2L * 1024L * 1024L * 1024L;
            long oneGB = 1L * 1024L * 1024L * 1024L;

            // Try 2GB first, fall back to 1GB
            this.No_GC_Set = GC.TryStartNoGCRegion(twoGB);
            if (this.No_GC_Set)
            {
                this.No_GC_CleanupLimitMB = 2000L;
            }
            else
            {
                this.No_GC_Set = GC.TryStartNoGCRegion(oneGB);
                this.No_GC_CleanupLimitMB = this.No_GC_Set ? 1000L : 900L;
            }

            this.Set_RAM_Limit_Text();
        }
        catch (Exception ex)
        {
            //DEFECT FIX: this used to blindly record No_GC_Set = false. TryStartNoGCRegion throws
            //InvalidOperationException when a no-GC region is ALREADY in progress, so the old code
            //claimed the GC was enabled while it was in fact still suppressed - unbounded working
            //set growth in a long-running real-time audio process. Ask the runtime instead of
            //guessing, and make the failure observable.
            BassThatHz_ASIO_DSP_Processor.Debug.ReportSwallowed(ex);
            this.No_GC_Set = GCSettings.LatencyMode == GCLatencyMode.NoGCRegion;
            this.No_GC_CleanupLimitMB = 900L;
            this.Set_RAM_Limit_Text();
        }
    }

    /// <summary>
    /// Ends the no-GC region if one is active, keeping <see cref="No_GC_Set"/> in sync with the
    /// runtime's actual latency mode rather than with what the caller assumed.
    /// </summary>
    protected void TryEndNoGCRegion()
    {
        try
        {
            if (GCSettings.LatencyMode == GCLatencyMode.NoGCRegion)
                GC.EndNoGCRegion();
        }
        catch (Exception ex)
        {
            //The runtime may already have exited the region on its own (budget exhausted).
            BassThatHz_ASIO_DSP_Processor.Debug.ReportSwallowed(ex);
        }
        finally
        {
            this.No_GC_Set = GCSettings.LatencyMode == GCLatencyMode.NoGCRegion;
        }
    }
    
    protected void chkNoGCMode_CheckedChanged(object sender, EventArgs e)
    {
        //If on, turn off
        if (this.No_GC_Set)
        {
            //DEFECT FIX: GC.EndNoGCRegion() throws InvalidOperationException when the runtime has
            //already exited the region on its own (budget exhausted). Unguarded, that escaped a
            //CheckedChanged handler that has no try/catch of its own.
            this.TryEndNoGCRegion();
            this.NoGC_Timer.Enabled = false;
        }

        if (this.chkNoGCMode.Checked)
        {
            //Suppressed (non-interactive/test) default is Cancel: never silently reserve 1-2GB
            //of NoGC region when nobody is there to confirm it.
            var result = BassThatHz_ASIO_DSP_Processor.Debug.ShowMessage(
                                          "This is an experimental feature which disables the\n" +
                                          ".Net memory-manager for processing of critical audio.\n" +
                                          "This trades high memory usage for less audio glitches.\n" +
                                          "This is useful for critical audio sessions.\n" +
                                          "It can use up to 1-2gb of additional ram. Would you like to try it?"
                                    , "Warning", MessageBoxButtons.OKCancel, MessageBoxIcon.Exclamation,
                                    DialogResult.Cancel);
            if (result == DialogResult.OK)
            {
                this.TrySetNoGC_Limit();
                this.NoGC_Timer.Enabled = this.No_GC_Set;
            }
            else
            {
                this.NoGC_Timer.Enabled = false;
                this.chkNoGCMode.Checked = false;
            }
        }
        else
        {
            this.NoGC_Timer.Enabled = false;
        }

        this.chkNoGCMode.BackColor = this.No_GC_Set ? System.Drawing.Color.Firebrick : System.Drawing.Color.Transparent;
     }
    #endregion

    #endregion

    #region Protected Functions

    #region Init
    protected void Init_PerformanceCounters()
    {
        //this.PerformanceCounter_CPUTotal = new Diagnostics.PerformanceCounter("Processor", "% Processor Time", "_Total", true);
        //this.PerformanceCounter_UserTime = new Diagnostics.PerformanceCounter("Processor", "% User Time", "_Total", true);

        //using var p = Diagnostics.Process.GetCurrentProcess();
        //this.PerformanceCounter_AppCPU = new Diagnostics.PerformanceCounter("Process", "% Processor Time", p.ProcessName, true);
    }
    #endregion

    #region Starting Stats
    protected void ShowStartingStats()
    {
        this.SafeInvoke(() =>
        {
            try
            {
                this.Input_Lat_ms = 0;
                this.Output_Lat_ms = 0;
                this.BufferSize_Lat_ms = 0;
                this.Total_Buffer_Lat_ms = 0;
                this.TotalDSP_Processing_Lat_ms = 0;
                this.MaxDSP_Processing_Lat_ms = 0;
                this.AverageDSP_Processing_Lat_ms = 0;

                // Cache repeated lookups
                var asio = Program.ASIO;
                this.lbl_TotalChannels.Text = asio.NumberOf_IO_Channels_Total.ToString();
                this.lbl_InputChannels.Text = asio.NumberOf_Input_Channels.ToString();
                this.lbl_OutputChannels.Text = asio.NumberOf_Output_Channels.ToString();
                this.lblSampleRate.Text = asio.DriverCapabilities?.SampleRate.ToString();
                this.lblASIOBitType.Text = asio.DriverCapabilities?.InputChannelInfos.Length > 0
                    ? asio.DriverCapabilities?.InputChannelInfos[0].type.ToString()
                    : string.Empty;

                this.Show_ThreadID();
                this.Show_Total_Streams();
                this.Show_ASIO_HW_Latency();
                this.Show_BufferSize_Latency();
                this.Show_TotalBuffer_Latency();
                this.Show_ProcessPriorityAndRAMUsage();
                this.Show_Total_DSP_Filters();
                this.Show_Underruns();
                this.Show_UpTimes();
            }
            catch (Exception ex)
            {
                this.Error(ex);
            }
        });
    }

    protected void Show_ASIO_HW_Latency()
    {
        var Lat = Program.ASIO.PlaybackLatency;
        if (Lat != null)
        {
            this.Input_Lat_ms = (double)Lat.Value.InputLatency / (double)Program.ASIO.SampleRate_Current * 1000;
            this.Output_Lat_ms = (double)Lat.Value.OutputLatency / (double)Program.ASIO.SampleRate_Current * 1000;

            this.lbl_ASIO_Input_Latency.Text = Math.Round(this.Input_Lat_ms, 4).ToString(this.ms_TimeFormat);
            this.lbl_ASIO_Output_Latency.Text = Math.Round(this.Output_Lat_ms, 4).ToString(this.ms_TimeFormat);
        }
    }

    protected void Show_BufferSize_Latency()
    {
        this.BufferSize_Lat_ms = Program.ASIO.BufferSize_Latency_ms;
        this.lbl_InputBufferSizeLatency.Text = Math.Round(this.BufferSize_Lat_ms, 4).ToString(this.ms_TimeFormat);
        this.lbl_OutputBufferSizeLatency.Text = Math.Round(this.BufferSize_Lat_ms, 4).ToString(this.ms_TimeFormat);
    }

    protected void Show_TotalBuffer_Latency()
    {
        this.Total_Buffer_Lat_ms = this.Input_Lat_ms + this.Output_Lat_ms + BufferSize_Lat_ms;
        this.lbl_TotalBufferLatency.Text = this.Total_Buffer_Lat_ms.ToString(this.ms_TimeFormat);
    }
    #endregion

    #region RealTime Stats
    protected void Show_ProcessPriorityAndRAMUsage()
    {
        try
        {
            // Refresh cached process info to get up-to-date values without allocating
            this.CurrentProcess.Refresh();

            //PERF: PriorityClass only changes on user action, but the enum-to-string allocated a
            //fresh string every single tick.
            var Local_Priority = this.CurrentProcess.PriorityClass;
            if (Local_Priority != this.Last_PriorityClass)
            {
                this.Last_PriorityClass = Local_Priority;
                this.lbl_ProcessPriorityLevel.Text = Local_Priority.ToString();
            }

            long totalBytesOfMemoryUsed_MB = this.CurrentProcess.WorkingSet64 / 1024 / 1024;
            if (totalBytesOfMemoryUsed_MB != this.Last_RAM_MB)
            {
                this.Last_RAM_MB = totalBytesOfMemoryUsed_MB;
                this.lblRAM.Text = totalBytesOfMemoryUsed_MB.ToString(CultureInfo.InvariantCulture);
            }
        }
        catch (Exception ex)
        {
            //DEFECT FIX: the old comment claimed this reported via the error helper, but the body
            //was a bare '_ = ex;'. Do not throw from a UI timer, but do keep it observable.
            BassThatHz_ASIO_DSP_Processor.Debug.ReportSwallowed(ex);
        }
    }
    protected void Show_CPU_Usage()
    {
        //double TotalCPU_Usage = this.PerformanceCounter_CPUTotal.NextValue();
        //this.lbl_TotalCPU.Text = Convert.ToInt32(TotalCPU_Usage).ToString();

        //double UserTime_Usage = this.PerformanceCounter_UserTime.NextValue();

        //double AppCPU_Usage = this.PerformanceCounter_AppCPU.NextValue();
        //var AppCPUPercentage = UserTime_Usage * (AppCPU_Usage / 100);
        //this.lbl_App_CPU_Usage.Text = Convert.ToInt32(AppCPUPercentage).ToString();
    }

    protected void Show_DSPLatency()
    {
        var asio = Program.ASIO;
        TimeSpan inputBufferTime = asio.InputBufferConversion_ProcessingTime?.Elapsed ?? TimeSpan.Zero;
        TimeSpan outputBufferTime = asio.OutputBufferConversion_ProcessingTime?.Elapsed ?? TimeSpan.Zero;
        TimeSpan dspProcessingTime = asio.DSP_ProcessingTime?.Elapsed ?? TimeSpan.Zero;

        //PERF: 9 unconditional string allocations + 9 Label.Text sets per tick became 9 cheap
        //double comparisons; nothing is formatted unless the underlying value moved.
        SetTextIfChanged(this.lbl_InputBufferConversionLatency, inputBufferTime.TotalMilliseconds,
                         this.ms_TimeFormat, ref this.Last_InputBufferConversion_ms);
        SetTextIfChanged(this.lbl_OutputBufferConversionLatency, outputBufferTime.TotalMilliseconds,
                         this.ms_TimeFormat, ref this.Last_OutputBufferConversion_ms);
        SetTextIfChanged(this.lbl_TotalDSP_Processing_Latency, dspProcessingTime.TotalMilliseconds,
                         this.ms_TimeFormat, ref this.Last_TotalDSP_ms);
        SetTextIfChanged(this.lbl_DSP_Processing_Latency,
                         (dspProcessingTime - inputBufferTime - outputBufferTime).TotalMilliseconds,
                         this.ms_TimeFormat, ref this.Last_DSP_ms);

        // Update averages/peaks
        this.AverageDSP_Processing_Lat_ms = (this.TotalDSP_Processing_Lat_ms + dspProcessingTime.TotalMilliseconds) * 0.5;
        this.TotalDSP_Processing_Lat_ms = dspProcessingTime.TotalMilliseconds;
        SetTextIfChanged(this.lbl_Average_DSP_Latency, this.AverageDSP_Processing_Lat_ms,
                         this.ms_TimeFormat, ref this.Last_AverageDSP_ms);

        this.MaxDSP_Processing_Lat_ms = asio.DSP_PeakProcessingTime.TotalMilliseconds;
        SetTextIfChanged(this.lbl_Max_Detected_DSP_Latency, this.MaxDSP_Processing_Lat_ms,
                         this.ms_TimeFormat, ref this.Last_MaxDSP_ms);

        // Avoid Div by 0 error.
        if (this.BufferSize_Lat_ms > 0)
        {
            SetTextIfChanged(this.lbl_Current_DSP_Load, this.TotalDSP_Processing_Lat_ms / this.BufferSize_Lat_ms * 100,
                             this.Percentage_StringFormat, ref this.Last_CurrentLoad_pct);
            SetTextIfChanged(this.lbl_Average_DSP_Load, this.AverageDSP_Processing_Lat_ms / this.BufferSize_Lat_ms * 100,
                             this.Percentage_StringFormat, ref this.Last_AverageLoad_pct);
            SetTextIfChanged(this.lbl_Max_DSP_Load, this.MaxDSP_Processing_Lat_ms / this.BufferSize_Lat_ms * 100,
                             this.Percentage_StringFormat, ref this.Last_MaxLoad_pct);
        }
    }

    protected void Show_Total_Streams()
    {
        SetTextIfChanged(this.lbl_TotalStreams, Program.DSP_Info.Streams.Count, ref this.Last_TotalStreams);
    }

    protected void Show_UpTimes()
    {
        //PERF: TimeSpan.ToString(format) allocates; the label only changes once a second at most,
        //so compare at whole-second resolution and skip the format when it has not ticked over.
        var Local_Now = DateTime.Now;

        var Local_AppUpTime = Local_Now - Program.App_StartTime;
        if (Local_AppUpTime.Seconds != this.Last_AppUpTime.Seconds
            || Local_AppUpTime.Days != this.Last_AppUpTime.Days
            || Local_AppUpTime.Hours != this.Last_AppUpTime.Hours
            || Local_AppUpTime.Minutes != this.Last_AppUpTime.Minutes)
        {
            this.Last_AppUpTime = Local_AppUpTime;
            this.lbl_AppUpTime.Text = Local_AppUpTime.ToString(this.TimeSpanFormat);
        }

        TimeSpan Local_RunTime;
        if (this.DSP_StartTime != DateTime.MinValue && this.DSP_StopTime == DateTime.MinValue)
            Local_RunTime = Local_Now - this.DSP_StartTime;
        else
            Local_RunTime = this.DSP_StopTime - this.DSP_StartTime;

        if (Local_RunTime.Seconds != this.Last_DSPRunTime.Seconds
            || Local_RunTime.Days != this.Last_DSPRunTime.Days
            || Local_RunTime.Hours != this.Last_DSPRunTime.Hours
            || Local_RunTime.Minutes != this.Last_DSPRunTime.Minutes)
        {
            this.Last_DSPRunTime = Local_RunTime;
            this.lbl_DSPRunTime.Text = Local_RunTime.ToString(this.TimeSpanFormat);
        }
    }

    protected void Show_Total_DSP_Filters()
    {
        var FilterCount = 0;
        var EnabledFilterCount = 0;

        try
        {
            var streams = Program.DSP_Info?.Streams;
            if (streams != null)
            {
                //PERF: ObservableCollection<T> derives from Collection<T>, which has no public
                //struct enumerator - 'foreach' here boxed an IEnumerator<T> on the heap every tick.
                //Index instead.
                for (int s = 0; s < streams.Count; s++)
                {
                    var stream = streams[s];
                    if (stream == null || stream.Filters == null || stream.InputSource == null || stream.OutputDestination == null)
                        continue;

                    if (stream.InputSource.Index == -1 || stream.OutputDestination.Index == -1)
                        continue;

                    var Local_Filters = stream.Filters;
                    for (int f = 0; f < Local_Filters.Count; f++)
                    {
                        var filter = Local_Filters[f];
                        if (filter == null) continue;
                        FilterCount++;
                        if (filter.FilterEnabled) EnabledFilterCount++;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            //The UI thread uses async / time-slice "multithreading"
            //We don't care if this errors due to the user adding\deleting filters and streams while we are trying to calculate the values.
            //Once they stop messing with the config, the stats will show up correctly on the next pass
            _ = ex;
        }

        SetTextIfChanged(this.lbl_Total_DSP_Filters, FilterCount, ref this.Last_FilterCount);
        SetTextIfChanged(this.lbl_Total_Enabled_DSP_Filters, EnabledFilterCount, ref this.Last_EnabledFilterCount);
    }

    protected void Show_Underruns()
    {
        SetTextIfChanged(this.lbl_Underruns, Program.ASIO.Underruns, ref this.Last_Underruns);
    }
    protected void Show_ThreadID()
    {
        //PERF: the UI thread id never changes, and the ASIO thread id changes only on start/stop.
        SetTextIfChanged(this.lbl_UI_Thread_ID, Environment.CurrentManagedThreadId, ref this.Last_UI_ThreadID);
        SetTextIfChanged(this.lbl_ASIO_Thread_ID, Program.ASIO.ASIO_THreadID, ref this.Last_ASIO_ThreadID);
    }

    #endregion

    #endregion

    #region Error Handling
    protected void Error(Exception ex)
    {
        BassThatHz_ASIO_DSP_Processor.Debug.Error(ex);
    }
    #endregion
}