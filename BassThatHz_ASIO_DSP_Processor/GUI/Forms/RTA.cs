#nullable enable

namespace BassThatHz_ASIO_DSP_Processor.GUI.Forms;

#region Usings
using DSPLib;
using NAudio.Utils;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
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
public partial class RTA : Form
{
    #region Variables
    protected bool IsClosing = false;
    protected List<Task> ULF_FFT_Tasks = new();
    protected List<Task> Top_FFT_Tasks = new();
    protected List<Task<ChartUpdateData>> Waveform_Tasks = new();

    protected int Default_ULF_FFTSize = 2048;
    protected int Default_Top_FFTSize = 2048;
    protected int Input_ChannelIndex = -1;
    protected int Output_ChannelIndex = -1;

    protected int ULF_FFT_OverLapPercentage = 90;
    protected int Top_FFT_OverLapPercentage = 50;

    protected FFT InputTop_FFT;
    protected FFT OutputTop_FFT;
    protected double[] Top_FFT_FSpan;
    protected double[] Top_FFT_WindowCoefficients;
    protected double Top_FFT_WindowScaleFactor = 1;

    protected FFT InputULF_FFT;
    protected FFT OutputULF_FFT;
    protected double[] ULF_FFT_FSpan;
    protected double[] ULF_FFT_WindowCoefficients;
    protected double ULF_FFT_WindowScaleFactor = 1;

    protected CircularBuffer RTA_InputULFBuffer;
    protected CircularBuffer RTA_OutputULFBuffer;

    protected CircularBuffer RTA_InputTopBuffer;
    protected CircularBuffer RTA_OutputTopBuffer;

    protected bool chart_InputWaveform_ResetAutoRange = false;
    protected bool chart_OutputWaveform_ResetAutoRange = false;
    #endregion

    #region Constructor
    [SupportedOSPlatform("windows")]
    public RTA()
    {
        InitializeComponent();

        this.chart_InputWaveform.SuppressExceptions = true;
        this.chart_Input_Top_FFT.SuppressExceptions = true;
        this.chart_Input_ULF_FFT.SuppressExceptions = true;
        this.chart_OutputWaveform.SuppressExceptions = true;
        this.chart_Output_Top_FFT.SuppressExceptions = true;
        this.chart_Output_ULF_FFT.SuppressExceptions = true;

        this.InputTop_FFT = new FFT(this.Default_Top_FFTSize, 0);
        this.OutputTop_FFT = new FFT(this.Default_Top_FFTSize, 0);
        this.InputULF_FFT = new FFT(this.Default_ULF_FFTSize, 0);
        this.OutputULF_FFT = new FFT(this.Default_ULF_FFTSize, 0);
        this.Top_FFT_WindowCoefficients = new double[this.Default_Top_FFTSize];
        this.ULF_FFT_WindowCoefficients = new double[this.Default_ULF_FFTSize];
        this.Top_FFT_FSpan = new double[this.Default_Top_FFTSize];
        this.ULF_FFT_FSpan = new double[this.Default_ULF_FFTSize];

        var ULF_Length = (int)(Program.DSP_Info.InSampleRate * 10);
        this.RTA_InputULFBuffer = new(ULF_Length);
        this.RTA_OutputULFBuffer = new(ULF_Length);

        this.RTA_InputTopBuffer = new(this.Default_Top_FFTSize * 10);
        this.RTA_OutputTopBuffer = new(this.Default_Top_FFTSize * 10);

        this.Load += RTA_Load;
    }
    #endregion

    #region Public Functions

    #region Init
    public void Init_Channels(int input_ChannelIndex, int output_ChannelIndex)
    {
        this.Input_ChannelIndex = input_ChannelIndex;
        this.Output_ChannelIndex = output_ChannelIndex;
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
            this.timer_ResetWaveform.Enabled = false;
            this.Pause_CHK.Checked = true;

            Program.ASIO.InputDataAvailable -= ASIO_InputDataAvailable;
            Program.ASIO.OutputDataAvailable -= ASIO_OutputDataAvailable;

            await Task.WhenAll(this.ULF_FFT_Tasks);
            await Task.WhenAll(this.Top_FFT_Tasks);
            await Task.WhenAll(this.Waveform_Tasks);
        }
        catch (Exception ex)
        {
            _ = ex;
        }
    }
    #endregion

    #region FFT Comboboxes
    protected void cbo_ULF_FFT_Window_Type_SelectedIndexChanged(object? sender, EventArgs e)
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

    protected void cbo_Top_FFT_Window_Type_SelectedIndexChanged(object? sender, EventArgs e)
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

    protected void cbo_Top_FFT_Size_SelectedIndexChanged(object? sender, EventArgs e)
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

    protected void cbo_ULF_FFT_Overlap_SelectedIndexChanged(object? sender, EventArgs e)
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

    protected void cbo_Top_FFT_Overlap_SelectedIndexChanged(object? sender, EventArgs e)
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
            if (this.Output_ChannelIndex > -1 && Program.ASIO.OutputBuffer != null)
            {
                var LocalBuffer = Program.ASIO.OutputBuffer[this.Output_ChannelIndex];
                if (this.chart_Output_ULF_FFT.Visible)
                    _ = this.RTA_OutputULFBuffer.Write(LocalBuffer, 0, LocalBuffer.Length);

                if (this.chart_Output_Top_FFT.Visible)
                    _ = this.RTA_OutputTopBuffer.Write(LocalBuffer, 0, LocalBuffer.Length);
            }
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
            if (this.Input_ChannelIndex > -1 && Program.ASIO.InputBuffer != null)
            {
                var LocalBuffer = Program.ASIO.InputBuffer[this.Input_ChannelIndex];
                if (this.chart_Input_ULF_FFT.Visible)
                    _ = this.RTA_InputULFBuffer.Write(LocalBuffer, 0, LocalBuffer.Length);

                if (this.chart_Input_Top_FFT.Visible)
                    _ = this.RTA_InputTopBuffer.Write(LocalBuffer, 0, LocalBuffer.Length);
            }
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }
    #endregion

    #region Waveform Plot Timers
    [SupportedOSPlatform("windows")]
    protected void timer_ResetWaveform_Tick(object sender, EventArgs e)
    {
        this.timer_ResetWaveform.Enabled = false;
        try
        {
            this.chart_InputWaveform_ResetAutoRange = true;
            this.chart_OutputWaveform_ResetAutoRange = true;
        }
        catch (Exception ex)
        {   //MsChart is buggy and sensitive, it crashing shouldn't kill the DSP app.
            _ = ex;
        }
        this.timer_ResetWaveform.Enabled = !this.Pause_CHK.Checked;
    }

    [SupportedOSPlatform("windows")]
    protected async void timer_PlotWaveforms_Tick(object sender, EventArgs e)
    {
        // Disable the timer while processing.
        this.timer_PlotWaveforms.Enabled = false;
        try
        {
            this.Waveform_Tasks.Clear();

            // Process input waveform if conditions are met.
            if (this.Input_ChannelIndex > -1 &&
                this.chart_InputWaveform.Visible &&
                Program.ASIO.InputBuffer != null)
            {
                double[] yDataInput = Program.ASIO.InputBuffer[this.Input_ChannelIndex];
                double scaleYAxis = 1.5;
                bool resetAutoRange = this.chart_InputWaveform_ResetAutoRange;

                this.Waveform_Tasks.Add(Task.Run(() =>
                {
                    WaveformPlotData plotData = this.ComputeWaveformPlotData(yDataInput, scaleYAxis);
                    return new ChartUpdateData
                    {
                        Chart = this.chart_InputWaveform,
                        PlotData = plotData,
                        ResetAutoRange = resetAutoRange
                    };
                }));
            }

            // Process output waveform if conditions are met.
            if (this.Output_ChannelIndex > -1 &&
                this.chart_OutputWaveform.Visible &&
                Program.ASIO.OutputBuffer != null)
            {
                double[] yDataOutput = Program.ASIO.OutputBuffer[this.Output_ChannelIndex];
                double scaleYAxis = 1.5;
                bool resetAutoRange = this.chart_OutputWaveform_ResetAutoRange;

                this.Waveform_Tasks.Add(Task.Run(() =>
                {
                    WaveformPlotData plotData = this.ComputeWaveformPlotData(yDataOutput, scaleYAxis);
                    return new ChartUpdateData
                    {
                        Chart = this.chart_OutputWaveform,
                        PlotData = plotData,
                        ResetAutoRange = resetAutoRange
                    };
                }));
            }

            // Await all background tasks.
            ChartUpdateData[] updates = await Task.WhenAll(this.Waveform_Tasks);

            // Batch all UI updates on the UI thread.
            if (!this.IsClosing && !this.IsDisposed && this.IsHandleCreated)
                this.SafeInvoke(() =>
                {
                    foreach (var update in updates)
                    {
                        if (update.Chart == null || update.PlotData == null)
                            continue;

                        this.UpdateChartWithPlotData(update.Chart, update.PlotData, update.ResetAutoRange);
                        // Reset the auto-range flags after updating.
                        if (update.Chart == this.chart_InputWaveform)
                            this.chart_InputWaveform_ResetAutoRange = false;
                        else if (update.Chart == this.chart_OutputWaveform)
                            this.chart_OutputWaveform_ResetAutoRange = false;
                    }
                });
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
        finally
        {
            this.timer_PlotWaveforms.Enabled = !this.Pause_CHK.Checked;
        }
    }
    #endregion

    #region FFT Plot Timers
    [SupportedOSPlatform("windows")]
    protected async void timer_Plot_ULF_FFT_Tick(object sender, EventArgs e)
    {
        this.timer_Plot_ULF_FFT.Enabled = false;
        try
        {
            int inSampleRate = Program.DSP_Info.InSampleRate;
            double overlap = this.ULF_FFT_OverLapPercentage / 100d;
            double overlapAdd = 1d + overlap;
            double overlapRemove = 1d - overlap;
            int removeLength = (int)(inSampleRate * overlapRemove);

            this.ULF_FFT_Tasks.Clear();

            // Input ULF
            if (this.chart_Input_ULF_FFT.Visible &&
                this.RTA_InputULFBuffer.Count > inSampleRate * overlapAdd)
            {
                this.ULF_FFT_Tasks.Add(Task.Run(() =>
                {
                    var data = new double[inSampleRate];
                    _ = this.RTA_InputULFBuffer.Read(data, 0, inSampleRate);

                    return this.Compute_ULF_FFT_Data(this.InputULF_FFT, data);
                })
                .ContinueWith(t =>
                 {
                     var (xData, magLog) = t.Result;
                     if (xData.Length > 0 && magLog.Length > 0)
                     {
                         if (!this.IsClosing && !this.IsDisposed && this.IsHandleCreated)
                            this.SafeInvoke(() =>
                                this.Plot_ULF_FFT(this.chart_Input_ULF_FFT, xData, magLog));
                     }
                     this.RTA_InputULFBuffer.Advance(removeLength);
                 }));
            }

            // Output ULF
            if (this.chart_Output_ULF_FFT.Visible &&
                this.RTA_OutputULFBuffer.Count > inSampleRate * overlapAdd)
            {
                this.ULF_FFT_Tasks.Add(Task.Run(() =>
                {
                    var data = new double[inSampleRate];
                    _ = this.RTA_OutputULFBuffer.Read(data, 0, inSampleRate);

                    return this.Compute_ULF_FFT_Data(this.OutputULF_FFT, data);
                })
                .ContinueWith(t =>
                 {
                     var (xData, magLog) = t.Result;
                     if (xData.Length > 0 && magLog.Length > 0)
                     {
                         if (!this.IsClosing && !this.IsDisposed && this.IsHandleCreated)
                             this.SafeInvoke(() =>
                               this.Plot_ULF_FFT(this.chart_Output_ULF_FFT, xData, magLog));
                     }
                     this.RTA_OutputULFBuffer.Advance(removeLength);
                 }));
            }

            // Wait for all background tasks to finish
            await Task.WhenAll(this.ULF_FFT_Tasks);
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
        finally
        {
            this.timer_Plot_ULF_FFT.Enabled = !this.Pause_CHK.Checked;
        }
    }

    [SupportedOSPlatform("windows")]
    protected async void timer_PlotTopFFTs_Tick(object sender, EventArgs e)
    {
        this.timer_Plot_Top_FFTs.Enabled = false;
        try
        {
            double overlap = this.Top_FFT_OverLapPercentage / 100d;
            double overlapAdd = 1d + overlap;
            double overlapRemove = 1d - overlap;
            int removeLength = (int)(this.Default_Top_FFTSize * overlapRemove);

            this.Top_FFT_Tasks.Clear();

            // Input Top FFT
            if (this.chart_Input_Top_FFT.Visible &&
                this.RTA_InputTopBuffer.Count > this.Default_Top_FFTSize * overlapAdd)
            {
                this.Top_FFT_Tasks.Add(Task.Run(() =>
                {
                    var data = new double[this.Default_Top_FFTSize];
                    _ = this.RTA_InputTopBuffer.Read(data, 0, this.Default_Top_FFTSize);
                    return this.Compute_Top_FFT_Data(this.InputTop_FFT, data);
                })
                .ContinueWith(t =>
                 {
                     var (xData, magLog) = t.Result;
                     if (xData.Length > 0 && magLog.Length > 0)
                     {
                         if (!this.IsClosing && !this.IsDisposed && this.IsHandleCreated)
                            this.SafeInvoke(() =>
                                this.Plot_Top_FFT(this.chart_Input_Top_FFT, xData, magLog));
                     }
                     this.RTA_InputTopBuffer.Advance(removeLength);
                 }));
            }

            // Output Top FFT
            if (this.chart_Output_Top_FFT.Visible &&
                this.RTA_OutputTopBuffer.Count > this.Default_Top_FFTSize * overlapAdd)
            {
                this.Top_FFT_Tasks.Add(Task.Run(() =>
                {
                    var data = new double[this.Default_Top_FFTSize];
                    _ = this.RTA_OutputTopBuffer.Read(data, 0, this.Default_Top_FFTSize);
                    return this.Compute_Top_FFT_Data(this.OutputTop_FFT, data);
                })
                .ContinueWith(t =>
                 {
                     var (xData, magLog) = t.Result;
                     if (xData.Length > 0 && magLog.Length > 0)
                     {
                         if (!this.IsClosing && !this.IsDisposed && this.IsHandleCreated)
                             this.SafeInvoke(() =>
                                this.Plot_Top_FFT(this.chart_Output_Top_FFT, xData, magLog));
                     }
                     this.RTA_OutputTopBuffer.Advance(removeLength);
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
            this.timer_Plot_Top_FFTs.Enabled = !this.Pause_CHK.Checked;
        }
    }

    protected (double[] xData, double[] magLog) Compute_ULF_FFT_Data(FFT fft, double[] timeSeries)
    {
        if (timeSeries.Length < this.Default_ULF_FFTSize)
            return (Array.Empty<double>(), Array.Empty<double>());

        int FFTSize = Math.Min(this.Default_ULF_FFTSize, timeSeries.Length);
        //Downsample for ULF
        var Down = DownSampler.downsample(timeSeries, FFTSize);

        // Perform a FFT
        Complex[] FFTResult = fft.Perform_FFT(Down, this.ULF_FFT_WindowCoefficients);
        var HalfLength = FFTResult.Length / 2 + 1;
        var RealResult = new Complex[HalfLength];
        Array.Copy(FFTResult, RealResult, HalfLength);

        double[] magResult = DSP.ConvertComplex.ToMagnitude(RealResult);
        double[] magLog = DSP.ConvertMagnitude.ToMagnitudeDBV(
                             magResult,
                             this.ULF_FFT_WindowScaleFactor * Math.Sqrt(2),
                             -400d);

        // Return just the data needed to plot.
        return (this.ULF_FFT_FSpan, magLog);
    }

    protected (double[] xData, double[] magLog) Compute_Top_FFT_Data(FFT fft, double[] timeSeries)
    {
        int FFTSize = this.Default_Top_FFTSize;

        // If the incoming buffer is too short, return empty arrays so we skip plotting.
        if (timeSeries.Length < FFTSize)
            return (Array.Empty<double>(), Array.Empty<double>());

        // Exactly as in Render_Top_FFT: resize to the chosen FFT length.
        Array.Resize(ref timeSeries, FFTSize);

        // Perform the FFT using your existing Top_FFT instance & window coefficients.
        Complex[] fftResult = fft.Perform_FFT(timeSeries, this.Top_FFT_WindowCoefficients);

        // Keep only the real, non-mirrored half.
        int halfLength = fftResult.Length / 2 + 1;
        Complex[] realResult = new Complex[halfLength];
        Array.Copy(fftResult, realResult, halfLength);

        // Convert to magnitude.
        double[] magResult = DSP.ConvertComplex.ToMagnitude(realResult);

        // Convert magnitude to dBV with your original scale factor & floor of -400 dB.
        double[] magLog = DSP.ConvertMagnitude.ToMagnitudeDBV(
            magResult,
            this.Top_FFT_WindowScaleFactor * Math.Sqrt(2),
            -400d
        );

        // Return the frequency span (already stored in Top_FFT_FSpan) plus the final magnitude array.
        return (this.Top_FFT_FSpan, magLog);
    }

    #endregion

    #region ChartMouseMove
    [SupportedOSPlatform("windows")]
    protected async void Chart_MouseMove(object? sender, MouseEventArgs e)
    {
        try
        {
            if (sender == null) return;
            if (!(sender is Chart chart)) return;
            if (chart.ChartAreas.Count < 1 || chart.Titles.Count < 6) return;

            ChartArea ca = chart.ChartAreas[0];
            if (ca == null) return;

            // Check if the mouse is within the inner plot area (UI thread)
            RectangleF innerRect = this.InnerPlotPositionClientRectangle(chart, ca);
            if (!innerRect.Contains(e.Location))
                return;

            // Capture values from the UI thread to pass to the background task.
            int pixelX = e.X;
            int pixelY = e.Y;
            var ax = ca.AxisX;
            var ay = ca.AxisY;
            double sampleRate = Program.DSP_Info.InSampleRate;

            // Offload the computations to a background thread.
            string newTitle = await Task.Run(() =>
            {
                // Perform computations.
                // (Note: Ensure that these methods are thread-safe in your context.)
                double xValue = Math.Pow(10, ax.PixelPositionToValue(pixelX));
                double yValue = ay.PixelPositionToValue(pixelY);

                // Only update the title if within Nyquist limit.
                if (xValue < sampleRate * 0.5)
                    return $"Mouse: {xValue:0.0} | {yValue:0.0}";
                return string.Empty;
            });

            // Update the UI on the UI thread.
            if (!string.IsNullOrEmpty(newTitle))
                if (!this.IsClosing && !this.IsDisposed && this.IsHandleCreated)
                    this.SafeInvoke(() =>
                        chart.Titles[5].Text = newTitle);
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }
    #endregion

    #region IntervalChanged
    protected void Msb_WaveForm_RefreshInterval_TextChanged(object? sender, EventArgs e)
    {
        try
        {
            if (string.IsNullOrEmpty(this.msb_WaveForm_RefreshInterval.Text))
                this.msb_WaveForm_RefreshInterval.Text = "1";

            this.timer_PlotWaveforms.Interval = Math.Max(1, int.Parse(this.msb_WaveForm_RefreshInterval.Text));
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

            this.timer_Plot_Top_FFTs.Interval = Math.Max(1, int.Parse(this.msb_Top_FFT_RefreshInterval.Text));
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

            this.timer_Plot_ULF_FFT.Interval = Math.Max(1, int.Parse(this.msb_ULF_FFT_RefreshInterval.Text));
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
            this.timer_ResetWaveform.Enabled = !this.Pause_CHK.Checked;
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
        this.cbo_ULF_FFT_Window_Type.DataSource = Enum.GetNames(typeof(DSP.Window.Type));
        this.cbo_Top_FFT_Window_Type.DataSource = Enum.GetNames(typeof(DSP.Window.Type));
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
        this.timer_ResetWaveform.Enabled = true;
    }
    #endregion

    #region MapEventHandlers
    [SupportedOSPlatform("windows")]
    protected void MapEventHandlers()
    {
        this.msb_Top_FFT_RefreshInterval.TextChanged += Msb_TopFFT_RefreshInterval_TextChanged;
        this.msb_WaveForm_RefreshInterval.TextChanged += Msb_WaveForm_RefreshInterval_TextChanged;
        this.msb_ULF_FFT_RefreshInterval.TextChanged += Msb_ULF_FFT_RefreshInterval_TextChanged;

        this.cbo_ULF_FFT_Window_Type.SelectedIndexChanged += cbo_ULF_FFT_Window_Type_SelectedIndexChanged;
        this.cbo_Top_FFT_Window_Type.SelectedIndexChanged += cbo_Top_FFT_Window_Type_SelectedIndexChanged;
        this.cbo_Top_FFT_Size.SelectedIndexChanged += cbo_Top_FFT_Size_SelectedIndexChanged;
        this.cbo_ULF_FFT_Overlap.SelectedIndexChanged += cbo_ULF_FFT_Overlap_SelectedIndexChanged;
        this.cbo_Top_FFT_Overlap.SelectedIndexChanged += cbo_Top_FFT_Overlap_SelectedIndexChanged;

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
                foreach (var item in this.checkedListBox1.CheckedIndices)
                {
                    var CheckedIndex = (int)item;
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
    #endregion

    #region PreCalculateWindowCoefficients
    protected void ReCalculate_Top_FFT()
    {
        //Precalculate Window
        var SelectedItem = this.cbo_Top_FFT_Window_Type.SelectedItem?.ToString();
        if (!string.IsNullOrEmpty(SelectedItem))
        {
            var WindowType = (DSP.Window.Type)Enum.Parse(typeof(DSP.Window.Type), SelectedItem);
            this.Top_FFT_WindowCoefficients = DSP.Window.Coefficients(WindowType, this.Default_Top_FFTSize);
            this.Top_FFT_WindowScaleFactor = DSP.Window.ScaleFactor.Signal(this.Top_FFT_WindowCoefficients);
        }

        this.InputTop_FFT = new FFT(this.Default_Top_FFTSize);
        this.OutputTop_FFT = new FFT(this.Default_Top_FFTSize);
        int SampleRate = Program.DSP_Info.InSampleRate;
        // Calculate the frequency span
        this.Top_FFT_FSpan = this.InputTop_FFT.FrequencySpan(SampleRate);
        this.Top_FFT_FSpan[0] = 0.0001;

        this.RTA_InputTopBuffer = new(this.Default_Top_FFTSize * 10);
        this.RTA_OutputTopBuffer = new(this.Default_Top_FFTSize * 10);
    }

    protected void ReCalculate_ULF_FFT()
    {
        //Precalculate Window
        var SelectedItem = this.cbo_ULF_FFT_Window_Type.SelectedItem?.ToString();
        if (!string.IsNullOrEmpty(SelectedItem))
        {
            var WindowType = (DSP.Window.Type)Enum.Parse(typeof(DSP.Window.Type), SelectedItem);
            this.ULF_FFT_WindowCoefficients = DSP.Window.Coefficients(WindowType, this.Default_ULF_FFTSize);
            this.ULF_FFT_WindowScaleFactor = DSP.Window.ScaleFactor.Signal(this.ULF_FFT_WindowCoefficients);
        }

        this.InputULF_FFT = new FFT(this.Default_ULF_FFTSize);
        this.OutputULF_FFT = new FFT(this.Default_ULF_FFTSize);
        // Calculate the frequency span
        this.ULF_FFT_FSpan = this.InputULF_FFT.FrequencySpan(this.Default_ULF_FFTSize);
        this.ULF_FFT_FSpan[0] = 0.0001;
    }
    #endregion

    #region FFT Charts Logic
    [SupportedOSPlatform("windows")]
    protected void Plot_FFT(Chart chartControl, double min, double max, double[] xData, double[] yData)
    {
        try
        {
            if (this.IsClosing || this.IsDisposed || !this.IsHandleCreated)
                return;
            if (chartControl.IsDisposed || !chartControl.IsHandleCreated || chartControl.ChartAreas.Count < 1)
                return;
            if (chartControl.Series.IndexOf("Series1") < 0)
                return;

            chartControl.SuspendLayout();
            chartControl.Series["Series1"].Points.Clear();
            chartControl.Series["Series1"].Points.DataBindXY(xData, yData);

            chartControl.ChartAreas[0].AxisY.Interval = 12;
            chartControl.ChartAreas[0].AxisY.IntervalType = DateTimeIntervalType.Number;
            chartControl.ChartAreas[0].AxisY.Maximum = 0;
            chartControl.ChartAreas[0].AxisY.Minimum = -144;

            chartControl.ChartAreas[0].AxisX.IntervalType = DateTimeIntervalType.Number;
            chartControl.ChartAreas[0].AxisX.MinorGrid.Enabled = true;
            chartControl.ChartAreas[0].AxisX.MinorGrid.Interval = 1;
            chartControl.ChartAreas[0].AxisX.Minimum = min;
            chartControl.ChartAreas[0].AxisX.Maximum = max;
            chartControl.ChartAreas[0].AxisX.IsLogarithmic = true;
            chartControl.ResumeLayout();
        }
        catch (Exception ex)
        {
            _ = ex;
        }
    }

    [SupportedOSPlatform("windows")]
    protected void Plot_ULF_FFT(Chart chartControl, double[] xData, double[] yData)
    {
        int MinHz = 1;
        int MaxHz = 100;
        this.Plot_FFT(chartControl, MinHz, MaxHz, xData, yData);

        var MaxIndex = DSP.Analyze.FindMaxPosition(yData, 0, MaxHz);
        var MinIndex = DSP.Analyze.FindMinPosition(yData, 0, MaxHz);
        if (this.IsClosing || this.IsDisposed || !this.IsHandleCreated)
            return;
        if (chartControl.IsDisposed || !chartControl.IsHandleCreated)
            return;
        chartControl.Titles[3].Text = "Max: " + xData[MaxIndex].ToString("0.0") + " | " + yData[MaxIndex].ToString("0.0");
        chartControl.Titles[4].Text = "Min: " + xData[MinIndex].ToString("0.0") + " | " + yData[MinIndex].ToString("0.0");    
    }

    [SupportedOSPlatform("windows")]
    protected void Plot_Top_FFT(Chart chartControl, double[] xData, double[] yData)
    {
        int MinHz = 10;
        int MaxHz = 20000;
        this.Plot_FFT(chartControl, MinHz, MaxHz, xData, yData);

        var MaxIndex = DSP.Analyze.FindMaxPosition(yData, 0, MaxHz);
        var MinIndex = DSP.Analyze.FindMinPosition(yData, 0, MaxHz);
        if (this.IsClosing || this.IsDisposed || !this.IsHandleCreated)
            return;
        if (chartControl.IsDisposed || !chartControl.IsHandleCreated)
            return;
        chartControl.Titles[3].Text = "Max: " + xData[MaxIndex].ToString("0.0") + " | " + yData[MaxIndex].ToString("0.0");
        chartControl.Titles[4].Text = "Min: " + xData[MinIndex].ToString("0.0") + " | " + yData[MinIndex].ToString("0.0");
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
        if (chartControl.IsDisposed ||!chartControl.IsHandleCreated || chartControl == null || chartControl.ChartAreas.Count < 1)
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
    protected void UpdateChartWithPlotData(Chart chartControl, WaveformPlotData plotData, bool resetAutoRange)
    {
        if (this.IsClosing || this.IsDisposed || !this.IsHandleCreated)
            return;
        if (chartControl.IsDisposed || !chartControl.IsHandleCreated || chartControl.ChartAreas.Count < 1)
            return;
        if (chartControl.Series.IndexOf("Series1") < 0)
            return;

        try
        {
            // Perform one‑time initialization if not already done.
            this.InitializeChart(chartControl);

            chartControl.SuspendLayout();

            // If auto‑range needs to be reset, set the Y‑axis values to 0.
            if (resetAutoRange)
            {
                chartControl.ChartAreas[0].AxisY.Maximum = 0;
                chartControl.ChartAreas[0].AxisY.Minimum = 0;
                chartControl.ChartAreas[0].AxisY.Interval = 0;
            }

            // Set basic axis properties.
            ChartArea area = chartControl.ChartAreas[0];
            area.AxisX.IntervalType = DateTimeIntervalType.Number;
            area.AxisY.IntervalType = DateTimeIntervalType.Number;
            area.AxisX.Minimum = plotData.XMinimum;
            area.AxisX.Maximum = plotData.XMaximum;
            area.AxisX.Interval = plotData.XInterval;

            // Clear existing data points and bind new data.
            chartControl.Series["Series1"].Points.Clear();
            chartControl.Series["Series1"].Points.DataBindXY(plotData.XData, plotData.YDataDec);

            // Update Y‑axis scaling if needed.
            if (area.AxisY.Maximum < plotData.YMaximum || area.AxisY.Minimum > plotData.YMinimum)
            {
                area.AxisY.Maximum = plotData.YMaximum;
                area.AxisY.Minimum = plotData.YMinimum;
            }

            chartControl.ResumeLayout();
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }

    // Computes all the data needed to plot a waveform. This method is called on a
    // background thread and performs operations like generating X‑axis data,
    // converting the Y data to decimals, and computing axis scales.
    protected WaveformPlotData ComputeWaveformPlotData(double[] yData, double scaleYAxis)
    {
        // Generate X‑axis data.
        double[] xData = DSP.Generate.LinSpace(0, yData.Length - 1, yData.Length);
        // If xData is not valid, return empty plot data.
        if (xData.Length == 0 || xData[0] < 0)
        {
            return new WaveformPlotData();
        }

        // Convert yData to decimals because MSChart cannot handle full doubles.
        decimal[] yDataDec = new decimal[yData.Length];
        for (int i = 0; i < yData.Length; i++)
        {
            yDataDec[i] = (decimal)yData[i];
        }

        double xMin = 0;
        double xMax = yData.Length;
        double xInterval = yData.Length * 0.25;

        // Compute Y‑axis limits.
        double maxCandidate = yData.Max() * scaleYAxis;
        maxCandidate = Math.Min(maxCandidate, scaleYAxis);
        double minCandidate = yData.Min() * scaleYAxis;
        minCandidate = Math.Min(minCandidate, -0.0001);
        double mag = Math.Max(Math.Abs(maxCandidate), Math.Abs(minCandidate));
        mag = Math.Max(mag, 0.0001);

        return new WaveformPlotData
        {
            XData = xData,
            YDataDec = yDataDec,
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
        public double[] XData { get; set; } = Array.Empty<double>();
        public decimal[] YDataDec { get; set; } = Array.Empty<decimal>();
        public double XMinimum { get; set; }
        public double XMaximum { get; set; }
        public double XInterval { get; set; }
        public double YMaximum { get; set; }
        public double YMinimum { get; set; }
    }

    // Container that pairs a Chart control with its computed waveform data and
    // whether auto-range needs to be reset.
    protected class ChartUpdateData
    {
        public Chart? Chart { get; set; }
        public WaveformPlotData? PlotData { get; set; }
        public bool ResetAutoRange { get; set; }
    }

    #endregion

    #region ChartMouseArea
    [SupportedOSPlatform("windows")]
    protected RectangleF InnerPlotPositionClientRectangle(Chart chart, ChartArea CA)
    {
        RectangleF IPP = CA.InnerPlotPosition.ToRectangleF();
        RectangleF CArp = ChartAreaClientRectangle(chart, CA);

        float pw = CArp.Width / 100f;
        float ph = CArp.Height / 100f;

        return new RectangleF(CArp.X + pw * IPP.X, CArp.Y + ph * IPP.Y,
                              pw * IPP.Width, ph * IPP.Height);
    }

    [SupportedOSPlatform("windows")]
    protected RectangleF ChartAreaClientRectangle(Chart chart, ChartArea CA)
    {
        RectangleF CAR = CA.Position.ToRectangleF();
        float pw = chart.ClientSize.Width / 100f;
        float ph = chart.ClientSize.Height / 100f;
        return new RectangleF(pw * CAR.X, ph * CAR.Y, pw * CAR.Width, ph * CAR.Height);
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