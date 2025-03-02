#nullable enable

namespace BassThatHz_ASIO_DSP_Processor.GUI.Tabs;

using NAudio.Wave.Asio;

#region Usings
using System;
using System.Diagnostics;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Diagnostics = System.Diagnostics;
#endregion

/// <summary>
///  BassThatHz ASIO DSP Processor Engine
///  Copyright (c) 2025 BassThatHz
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
    }
    #endregion

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
                _ = MessageBox.Show("Cannot start. No ASIO Device found.");
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
                _ = MessageBox.Show("Cannot start. Can't fetch Driver Capabilities.");
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
                GCSettings.LatencyMode = GCLatencyMode.Interactive;
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
        var currentProcess = Process.GetCurrentProcess();
        if (currentProcess.WorkingSet64 >= this.No_GC_CleanupLimitMB * 1024L * 1024L)
        {
            this.TrySetNoGC_Limit();
        }
    }

    protected void TrySetNoGC_Limit()
    {
        if (this.No_GC_Set)
        {
            GC.EndNoGCRegion();
            this.No_GC_Set = false;
            GC.Collect();
        }

        this.No_GC_Set = GC.TryStartNoGCRegion(1024L * 1024L * 1023L * 2L); //2GB
        if (this.No_GC_Set)
        {
            this.No_GC_CleanupLimitMB = 2000L;
        }
        else
        {
            this.No_GC_CleanupLimitMB = 900L;
            this.No_GC_Set = GC.TryStartNoGCRegion(1024L * 1024L * 1024L); //1GB
        }
        this.lblRAM_Limit.Text = this.No_GC_CleanupLimitMB + "MB";
    }
    
    protected void chkNoGCMode_CheckedChanged(object sender, EventArgs e)
    {
        //If on, turn off
        if (this.No_GC_Set)
        {
            GC.EndNoGCRegion();
            this.No_GC_Set = false;
            this.NoGC_Timer.Enabled = false;
        }

        if (this.chkNoGCMode.Checked)
        {
            var result = MessageBox.Show("This is an experimental feature which disables the\n" +
                                          ".Net memory-manager for processing of critical audio.\n" +
                                          "This trades high memory usage for less audio glitches.\n" + 
                                          "This is useful for critical audio sessions.\n" +
                                          "It can use up to 1-2gb of additional ram. Would you like to try it?"
                                    , "Warning", MessageBoxButtons.OKCancel, MessageBoxIcon.Exclamation);
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

                this.lbl_TotalChannels.Text = Program.ASIO.NumberOf_IO_Channels_Total.ToString();
                this.lbl_InputChannels.Text = Program.ASIO.NumberOf_Input_Channels.ToString();
                this.lbl_OutputChannels.Text = Program.ASIO.NumberOf_Output_Channels.ToString();
                this.lblSampleRate.Text = Program.ASIO.DriverCapabilities?.SampleRate.ToString();
                this.lblASIOBitType.Text = Program.ASIO.DriverCapabilities?.InputChannelInfos[0].type.ToString();

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
            this.Input_Lat_ms = (double)Lat.Item1 / (double)Program.ASIO.SampleRate_Current * 1000;
            this.Output_Lat_ms = (double)Lat.Item2 / (double)Program.ASIO.SampleRate_Current * 1000;

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
        using var p = Diagnostics.Process.GetCurrentProcess();
        this.lbl_ProcessPriorityLevel.Text = p.PriorityClass.ToString();
        
        long totalBytesOfMemoryUsed_MB = p.WorkingSet64 / 1024 / 1024;
        this.lblRAM.Text = totalBytesOfMemoryUsed_MB.ToString();
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
        TimeSpan InputBufferTime = Program.ASIO.InputBufferConversion_ProcessingTime?.Elapsed ?? TimeSpan.Zero;
        TimeSpan OutputBufferTime = Program.ASIO.OutputBufferConversion_ProcessingTime?.Elapsed ?? TimeSpan.Zero;
        TimeSpan DSP_ProcessingTime = Program.ASIO.DSP_ProcessingTime?.Elapsed ?? TimeSpan.Zero;

        this.lbl_InputBufferConversionLatency.Text = InputBufferTime.TotalMilliseconds.ToString(this.ms_TimeFormat);
        this.lbl_OutputBufferConversionLatency.Text = OutputBufferTime.TotalMilliseconds.ToString(this.ms_TimeFormat);
        this.lbl_TotalDSP_Processing_Latency.Text = DSP_ProcessingTime.TotalMilliseconds.ToString(this.ms_TimeFormat);

        this.lbl_DSP_Processing_Latency.Text =
                                                (
                                                DSP_ProcessingTime
                                                - InputBufferTime
                                                - OutputBufferTime
                                                )
                                               .TotalMilliseconds.ToString(this.ms_TimeFormat);

        this.AverageDSP_Processing_Lat_ms = (this.TotalDSP_Processing_Lat_ms + DSP_ProcessingTime.TotalMilliseconds) * 0.5;
        this.TotalDSP_Processing_Lat_ms = DSP_ProcessingTime.TotalMilliseconds;
        this.lbl_Average_DSP_Latency.Text = this.AverageDSP_Processing_Lat_ms.ToString(this.ms_TimeFormat);

        this.MaxDSP_Processing_Lat_ms = Program.ASIO.DSP_PeakProcessingTime.TotalMilliseconds;
        this.lbl_Max_Detected_DSP_Latency.Text = this.MaxDSP_Processing_Lat_ms.ToString(this.ms_TimeFormat);


        //Avoid Div by 0 error. We don't know what Lat format the ASIO Device is using (we can't trust it.)
        if (this.BufferSize_Lat_ms > 0)
        {
            this.lbl_Current_DSP_Load.Text = (this.TotalDSP_Processing_Lat_ms / this.BufferSize_Lat_ms * 100)
                .ToString(this.Percentage_StringFormat);

            this.lbl_Average_DSP_Load.Text = (this.AverageDSP_Processing_Lat_ms / this.BufferSize_Lat_ms * 100)
                .ToString(this.Percentage_StringFormat);

            this.lbl_Max_DSP_Load.Text = (this.MaxDSP_Processing_Lat_ms / this.BufferSize_Lat_ms * 100)
                .ToString(this.Percentage_StringFormat);
        }
    }

    protected void Show_Total_Streams()
    {
        this.lbl_TotalStreams.Text = Program.DSP_Info.Streams.Count.ToString();
    }

    protected void Show_UpTimes()
    {
        this.lbl_AppUpTime.Text = (DateTime.Now - Program.App_StartTime).ToString(this.TimeSpanFormat);

        if (this.DSP_StartTime != DateTime.MinValue && this.DSP_StopTime == DateTime.MinValue)
            this.lbl_DSPRunTime.Text = (DateTime.Now - this.DSP_StartTime).ToString(this.TimeSpanFormat);
        else
            this.lbl_DSPRunTime.Text = (this.DSP_StopTime - this.DSP_StartTime).ToString(this.TimeSpanFormat);
    }

    protected void Show_Total_DSP_Filters()
    {
        var FilterCount = 0;
        var EnabledFilterCount = 0;

        try
        {
            foreach (var Stream in Program.DSP_Info.Streams)
                if (Stream != null
                    && Stream.Filters != null
                    && Stream.InputSource != null 
                    && Stream.InputSource.Index != -1
                    && Stream.OutputDestination != null
                    && Stream.OutputDestination.Index != -1)
                    foreach (var Filter in Stream.Filters)
                        if (Filter != null)
                        {
                            FilterCount++;
                            if (Filter.FilterEnabled)
                                EnabledFilterCount++;
                        }
        }
        catch (Exception ex)
        {
            //The UI thread uses async / time-slice "multithreading"
            //We don't care if this errors due to the user adding\deleting filters and streams while we are trying to calculate the values.
            //Once they stop messing with the config, the stats will show up correctly on the next pass
            _ = ex;
        }
        this.lbl_Total_DSP_Filters.Text = FilterCount.ToString();
        this.lbl_Total_Enabled_DSP_Filters.Text = EnabledFilterCount.ToString();
    }

    protected void Show_Underruns()
    {
        this.lbl_Underruns.Text = Program.ASIO.Underruns.ToString();
    }
    protected void Show_ThreadID()
    {
        this.lbl_UI_Thread_ID.Text = Thread.CurrentThread.ManagedThreadId.ToString();
        this.lbl_ASIO_Thread_ID.Text = Program.ASIO.ASIO_THreadID.ToString();
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