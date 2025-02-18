#nullable enable

namespace BassThatHz_ASIO_DSP_Processor.GUI.Forms;

#region Usings
using DSPLib;
using NAudio.Utils;
using System;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using Windows.Devices.WiFiDirect.Services;
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
    protected int Default_ULF_FFTSize = 2048;
    protected int Default_Top_FFTSize = 2048;
    protected int Input_ChannelIndex = -1;
    protected int Output_ChannelIndex = -1;

    protected int ULF_FFT_OverLapPercentage = 90;
    protected int Top_FFT_OverLapPercentage = 50;

    protected FFT Top_FFT;
    protected double[] Top_FFT_FSpan;
    protected double[] Top_FFT_WindowCoefficients;
    protected double Top_FFT_WindowScaleFactor = 1;

    protected FFT ULF_FFT;
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

        this.Top_FFT = new FFT(this.Default_Top_FFTSize, 0);
        this.ULF_FFT = new FFT(this.Default_ULF_FFTSize, 0);
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
    protected void RTA_FormClosing(object? sender, FormClosingEventArgs e)
    {
        try
        {
            Program.ASIO.InputDataAvailable -= ASIO_InputDataAvailable;
            Program.ASIO.OutputDataAvailable -= ASIO_OutputDataAvailable;
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
                    this.Top_FFT_OverLapPercentage = 25;
                    break;
                case 2:
                    this.Top_FFT_OverLapPercentage = 50;
                    break;
                case 3:
                    this.Top_FFT_OverLapPercentage = 75;
                    break;
                case 4:
                    this.Top_FFT_OverLapPercentage = 90;
                    break;
                case 5:
                    this.Top_FFT_OverLapPercentage = 95;
                    break;
                default:
                    this.Top_FFT_OverLapPercentage = 50;
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

    #region Plot Timers
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
        this.timer_ResetWaveform.Enabled = true;
    }

    [SupportedOSPlatform("windows")]
    protected void timer_PlotWaveforms_Tick(object sender, EventArgs e)
    {
        this.timer_PlotWaveforms.Enabled = false;
        try
        {
            if (this.Input_ChannelIndex > -1 && this.chart_InputWaveform.Visible && Program.ASIO.InputBuffer != null)
            {
                if (this.chart_InputWaveform_ResetAutoRange)
                {
                    this.chart_InputWaveform.ChartAreas[0].AxisY.Maximum = 0;
                    this.chart_InputWaveform.ChartAreas[0].AxisY.Minimum = 0;
                    this.chart_InputWaveform.ChartAreas[0].AxisY.Interval = 0;
                    this.chart_InputWaveform_ResetAutoRange = false;
                }

                double[] timeSeries_Input = Program.ASIO.InputBuffer[this.Input_ChannelIndex];
                var scaleYAxis = 1.5;
                this.Plot_Waveform(this.chart_InputWaveform, timeSeries_Input, scaleYAxis);
            }

            if (this.Output_ChannelIndex > -1 && this.chart_OutputWaveform.Visible && Program.ASIO.OutputBuffer != null)
            {
                if (this.chart_OutputWaveform_ResetAutoRange)
                {
                    this.chart_OutputWaveform.ChartAreas[0].AxisY.Maximum = 0;
                    this.chart_OutputWaveform.ChartAreas[0].AxisY.Minimum = 0;
                    this.chart_OutputWaveform.ChartAreas[0].AxisY.Interval = 0;
                    this.chart_OutputWaveform_ResetAutoRange = false;
                }

                double[] timeSeries_Output = Program.ASIO.OutputBuffer[this.Output_ChannelIndex];
                var scaleYAxis = 1.5;
                this.Plot_Waveform(this.chart_OutputWaveform, timeSeries_Output, scaleYAxis);
            }
        }
        catch (Exception ex)
        {   //MsChart is buggy and sensitive, it crashing shouldn't kill the DSP app.
            _ = ex;
        }
        this.timer_PlotWaveforms.Enabled = true;
    }

    [SupportedOSPlatform("windows")]
    protected void timer_PlotTopFFTs_Tick(object sender, EventArgs e)
    {
        this.timer_Plot_Top_FFTs.Enabled = false;
        try
        {
            double Overlap = this.Top_FFT_OverLapPercentage / 100d;
            double OverlapAdd = 1d + Overlap;
            double OverlapRemove = 1d - Overlap;
            var RemoveLength = (int)(this.Default_Top_FFTSize * OverlapRemove);

            if (this.chart_Input_Top_FFT.Visible && this.RTA_InputTopBuffer.Count > this.Default_Top_FFTSize * OverlapAdd)
            {
                var Data = new double[this.Default_Top_FFTSize];
                _ = this.RTA_InputTopBuffer.Read(Data, 0, this.Default_Top_FFTSize);
                this.Render_Top_FFT(this.chart_Input_Top_FFT, Data);
                this.RTA_InputTopBuffer.Advance(RemoveLength);
            }

            if (this.chart_Output_Top_FFT.Visible && this.RTA_OutputTopBuffer.Count > this.Default_Top_FFTSize * OverlapAdd)
            {
                var Data = new double[this.Default_Top_FFTSize];
                _ = this.RTA_OutputTopBuffer.Read(Data, 0, this.Default_Top_FFTSize);
                this.Render_Top_FFT(this.chart_Output_Top_FFT, Data);
                this.RTA_OutputTopBuffer.Advance(RemoveLength);
            }
        }
        catch (Exception ex)
        {   //MsChart is buggy and sensitive, it crashing shouldn't kill the DSP app.
            _ = ex;
        }
        this.timer_Plot_Top_FFTs.Enabled = true;
    }

    [SupportedOSPlatform("windows")]
    protected void timer_Plot_ULF_FFT_Tick(object sender, EventArgs e)
    {
        this.timer_Plot_ULF_FFT.Enabled = false;
        try
        {
            var InSampleRate = Program.DSP_Info.InSampleRate;
            double Overlap = this.ULF_FFT_OverLapPercentage / 100d;
            double OverlapAdd = 1d + Overlap;
            double OverlapRemove = 1d - Overlap;
            var RemoveLength = (int)(InSampleRate * OverlapRemove);

            if (this.chart_Input_ULF_FFT.Visible && this.RTA_InputULFBuffer.Count > InSampleRate * OverlapAdd)
            {
                var Data = new double[InSampleRate];
                _ = this.RTA_InputULFBuffer.Read(Data, 0, InSampleRate);
                this.Render_ULF_FFT(this.chart_Input_ULF_FFT, Data);
                this.RTA_InputULFBuffer.Advance(RemoveLength);
            }

            if (this.chart_Output_ULF_FFT.Visible && this.RTA_OutputULFBuffer.Count > InSampleRate * OverlapAdd)
            {
                var Data = new double[InSampleRate];
                _ = this.RTA_OutputULFBuffer.Read(Data, 0, InSampleRate);
                this.Render_ULF_FFT(this.chart_Output_ULF_FFT, Data);
                this.RTA_OutputULFBuffer.Advance(RemoveLength);
            }
        }
        catch (Exception ex)
        {   //MsChart is buggy and sensitive, it crashing shouldn't kill the DSP app.
            _ = ex;
        }
        this.timer_Plot_ULF_FFT.Enabled = true;
    }
    #endregion

    #region ChartMouseMove
    [SupportedOSPlatform("windows")]
    protected void Chart_MouseMove(object? sender, MouseEventArgs e)
    {
        try
        {
            if (sender == null) return;
            var chart = (Chart)sender;
            if (chart == null || chart.ChartAreas.Count < 1) return;
            ChartArea ca = chart.ChartAreas[0];
            if (ca == null || chart.Titles.Count < 6) return;

            if (InnerPlotPositionClientRectangle(chart, ca).Contains(e.Location))
            {
                Axis ax = ca.AxisX;
                Axis ay = ca.AxisY;
                double x = Math.Pow(10, ax.PixelPositionToValue(e.X));
                double y = ay.PixelPositionToValue(e.Y);
                if (x < Program.DSP_Info.InSampleRate * 0.5) //Limit X data to nyquist
                    chart.Titles[5].Text = "Mouse: " + x.ToString("0.0") + " | " + y.ToString("0.0");
            }
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

        this.Top_FFT = new FFT(this.Default_Top_FFTSize);
        int SampleRate = Program.DSP_Info.InSampleRate;
        // Calculate the frequency span
        this.Top_FFT_FSpan = this.Top_FFT.FrequencySpan(SampleRate);
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

        this.ULF_FFT = new FFT(this.Default_ULF_FFTSize);
        // Calculate the frequency span
        this.ULF_FFT_FSpan = this.ULF_FFT.FrequencySpan(this.Default_ULF_FFTSize);
        this.ULF_FFT_FSpan[0] = 0.0001;
    }
    #endregion

    #region Perform ULF and Top FFTs
    [SupportedOSPlatform("windows")]
    protected void Render_ULF_FFT(Chart chart, double[] timeSeries)
    {
        if (timeSeries.Length < this.Default_ULF_FFTSize)
            return;

        int FFTSize = Math.Min(this.Default_ULF_FFTSize, timeSeries.Length);
        //Downsample for ULF
        var Down = DownSampler.downsample(timeSeries, FFTSize);

        // Perform a FFT
        Complex[] FFTResult = this.ULF_FFT.Perform_FFT(Down, this.ULF_FFT_WindowCoefficients);
        Application.DoEvents();
        //We only care about the Real non-mirrored half
        var HalfLength = FFTResult.Length / 2 + 1;
        var RealResult = new Complex[HalfLength];
        Array.Copy(FFTResult, RealResult, HalfLength);

        // Convert the complex result to a scalar magnitude 
        double[] magResult = DSP.ConvertComplex.ToMagnitude(RealResult);

        // Convert and Plot Log Magnitude
        double[] magLog = DSP.ConvertMagnitude.ToMagnitudeDBV(magResult, this.ULF_FFT_WindowScaleFactor * Math.Sqrt(2), -400d);

        this.Plot_ULF_FFT(chart, this.ULF_FFT_FSpan, magLog);
    }

    [SupportedOSPlatform("windows")]
    protected void Render_Top_FFT(Chart chart, double[] timeSeries)
    {
        int FFTSize = this.Default_Top_FFTSize;

        if (timeSeries.Length < FFTSize)
            return;

        Array.Resize(ref timeSeries, FFTSize);

        // Perform a FFT
        Complex[] FFTResult = this.Top_FFT.Perform_FFT(timeSeries, this.Top_FFT_WindowCoefficients);
        Application.DoEvents();
        //We only care about the Real non-mirrored half
        var HalfLength = FFTResult.Length / 2 + 1;
        var RealResult = new Complex[HalfLength];
        Array.Copy(FFTResult, RealResult, HalfLength);

        // Convert the complex result to a scalar magnitude 
        double[] magResult = DSP.ConvertComplex.ToMagnitude(RealResult);

        // Convert and Plot Log Magnitude
        double[] magLog = DSP.ConvertMagnitude.ToMagnitudeDBV(magResult, this.Top_FFT_WindowScaleFactor * Math.Sqrt(2), -400d);

        this.Plot_Top_FFT(chart, this.Top_FFT_FSpan, magLog);
    }
    #endregion

    #region Plot Charts
    [SupportedOSPlatform("windows")]
    protected void Plot_Waveform(Chart chartControl, double[] yData, double scaleYAxis)
    {
        if (this.IsDisposed || !this.IsHandleCreated)
            return;
        if (chartControl.IsDisposed || !chartControl.IsHandleCreated || chartControl.ChartAreas.Count < 1)
            return;
        if (chartControl.Series.IndexOf("Series1") < 0)
            return;

        try
        {
            chartControl.SuspendLayout();

            chartControl.Series["Series1"].Points.Clear();

            //// Set basic axis properties.
            chartControl.ChartAreas[0].AxisY.IntervalType = DateTimeIntervalType.Number;
            chartControl.ChartAreas[0].AxisX.IntervalType = DateTimeIntervalType.Number;
            chartControl.ChartAreas[0].AxisX.IsStartedFromZero = true;

            chartControl.ChartAreas[0].AxisX.Minimum = 0;
            chartControl.ChartAreas[0].AxisX.Maximum = yData.Length;
            chartControl.ChartAreas[0].AxisX.Interval = yData.Length * 0.25;

            // Generate X Data starting at 0.
            double[] xData = DSP.Generate.LinSpace(0, yData.Length - 1, yData.Length);
            if (xData[0] < 0) //This should never be negative
            {
                chartControl.ResumeLayout();
                return;
            }

            // Add a baseline strip line at y = 0.
            var line = new StripLine()
            {
                BorderColor = Color.Black,
                Interval = 0,
                IntervalOffset = 0,
                StripWidth = 0,
                StripWidthType = DateTimeIntervalType.NotSet
            };
            chartControl.ChartAreas[0].AxisY.StripLines.Clear();
            chartControl.ChartAreas[0].AxisY.StripLines.Add(line);

            //MS Chart can't handle full doubles. LOL!
            var yDataDec = new decimal[yData.Length];
            for (int i = 0; i < yData.Length; i++)
                yDataDec[i] = (decimal)yData[i];

            chartControl.Series["Series1"].Points.DataBindXY(xData, yDataDec);

            var max = Math.Min(yData.Max() * scaleYAxis, scaleYAxis);
            var min = Math.Min(yData.Min() * scaleYAxis, -0.0001);
            var mag = Math.Max(Math.Abs(max), Math.Abs(min));
            mag = Math.Max(mag, 0.0001);

            var axisY = chartControl.ChartAreas[0].AxisY;
            if (axisY.Maximum < mag || axisY.Minimum > -mag)
            {
                axisY.Maximum = mag;
                axisY.Minimum = -mag;
            }

            // Format Y-axis labels.
            chartControl.ChartAreas[0].AxisY.LabelStyle.Format = "0.0000";

            chartControl.ResumeLayout();
        }
        catch (Exception ex) 
        {
            _ = ex;
        }
    }

    [SupportedOSPlatform("windows")]
    protected void Plot_FFT(Chart chartControl, double min, double max, double[] xData, double[] yData)
    {
        try
        {
            if (this.IsDisposed || !this.IsHandleCreated)
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

        _ = Task.Run(() =>
        {
            if (chartControl.IsDisposed || !chartControl.IsHandleCreated)
                return;

            var MaxIndex = DSP.Analyze.FindMaxPosition(yData, 0, MaxHz);
            var MinIndex = DSP.Analyze.FindMinPosition(yData, 0, MaxHz);
            chartControl.Titles[3].Text = "Max: " + xData[MaxIndex].ToString("0.0") + " | " + yData[MaxIndex].ToString("0.0");
            chartControl.Titles[4].Text = "Min: " + xData[MinIndex].ToString("0.0") + " | " + yData[MinIndex].ToString("0.0");
        });
    }

    [SupportedOSPlatform("windows")]
    protected void Plot_Top_FFT(Chart chartControl, double[] xData, double[] yData)
    {
        int MinHz = 10;
        int MaxHz = 20000;
        this.Plot_FFT(chartControl, MinHz, MaxHz, xData, yData);

        _ = Task.Run(() =>
        {
            if (chartControl.IsDisposed || !chartControl.IsHandleCreated)
                return;
            var MaxIndex = DSP.Analyze.FindMaxPosition(yData, 0, MaxHz);
            var MinIndex = DSP.Analyze.FindMinPosition(yData, 0, MaxHz);
            chartControl.Titles[3].Text = "Max: " + xData[MaxIndex].ToString("0.0") + " | " + yData[MaxIndex].ToString("0.0");
            chartControl.Titles[4].Text = "Min: " + xData[MinIndex].ToString("0.0") + " | " + yData[MinIndex].ToString("0.0");
        });
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