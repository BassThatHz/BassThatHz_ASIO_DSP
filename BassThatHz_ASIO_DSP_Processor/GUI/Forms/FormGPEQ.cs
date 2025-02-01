#nullable enable

namespace BassThatHz_ASIO_DSP_Processor.GUI.Forms;

#region Usings
using DSPLib;
using NAudio.Dsp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
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

public partial class FormGPEQ : Form
{
    #region Variables       
    protected List<IFilter>? ParentFilters;

    protected List<IFilter> Filters = new();
    #endregion

    #region Constructor and Init
    public FormGPEQ()
    {
        InitializeComponent();
        this.InitDefaults();
    }
    #endregion

    #region Public
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool SavedChanges { get; protected set; } = false;

    public ListBox.ObjectCollection GetListBoxItems()
    {
        if (this.SavedChanges)
        {
            return this.Filters_LSB.Items;
        }
        else
        {
            return new ListBox().Items;
        }
    }

    public void SetFilters(List<IFilter>? filters)
    {
        this.ParentFilters = filters;

        if (filters != null)
        {
            foreach (var Filter in filters)
            {
                if (Filter == null)
                    continue;

                if (Filter is BiQuadFilter BiQuadFilter)
                {
                    var TempFilter = new BiQuadFilter()
                    {
                        aa0 = BiQuadFilter.aa0,
                        aa1 = BiQuadFilter.aa1,
                        aa2 = BiQuadFilter.aa2,
                        a0 = BiQuadFilter.a0,
                        a1 = BiQuadFilter.a1,
                        a2 = BiQuadFilter.a2,
                        a3 = BiQuadFilter.a3,
                        a4 = BiQuadFilter.a4,
                        b0 = BiQuadFilter.b0,
                        b1 = BiQuadFilter.b1,
                        b2 = BiQuadFilter.b2,
                        FilterEnabled = BiQuadFilter.FilterEnabled,
                        BiQuadFilterType = BiQuadFilter.BiQuadFilterType,
                        FilterType = BiQuadFilter.FilterType,
                        Frequency = BiQuadFilter.Frequency,
                        Gain = BiQuadFilter.Gain,
                        Q = BiQuadFilter.Q,
                        Slope = BiQuadFilter.Slope,
                        SampleRate = BiQuadFilter.SampleRate,
                    };
                    TempFilter.ApplySettings();
                    this.Filters.Add(TempFilter);
                }
                else if (Filter is Basic_HPF_LPF HPF_LPF)
                {
                    var TempFilter = new Basic_HPF_LPF()
                    {
                        FilterEnabled = HPF_LPF.FilterEnabled,
                        HPFFreq = HPF_LPF.HPFFreq,
                        LPFFreq = HPF_LPF.LPFFreq,
                        HPFFilter = HPF_LPF.HPFFilter,
                        LPFFilter = HPF_LPF.LPFFilter,
                    };
                    TempFilter.ApplySettings();
                    this.Filters.Add(TempFilter);
                }
            }
        }

        this.DisplayFilters();
        this.SelectFirstListBoxItem();
        this.DisplayMagnitudeResponse();
    }

    public string GetListText(IFilter input)
    {
        var EnabledStatus = input.FilterEnabled ? "Enabled" : "Disabled";
        var ReturnValue = EnabledStatus;

        if (input is BiQuadFilter Biquad)
        {
            ReturnValue += " G: " + Biquad.Gain + " Q: " + Biquad.Q + " Hz: " + Biquad.Frequency;
        }
        else if (input is Basic_HPF_LPF HPF_LPF)
        {
            string HPFText = " HPF: Hz(" + HPF_LPF.HPFFreq + ") " + HPF_LPF.HPFFilter.ToString();
            string LPFText = " LPF: Hz(" + HPF_LPF.LPFFreq + ") " + HPF_LPF.LPFFilter.ToString();
            ReturnValue += HPFText + LPFText;
        }

        return ReturnValue;
    }
    #endregion

    #region Event Handlers
    protected void ShowTotalMag_CHK_CheckedChanged(object sender, EventArgs e)
    {
        this.GPEQ_Chart.Series["Series1"].Enabled = this.ShowTotalMag_CHK.Checked;
        this.GPEQ_Chart.ChartAreas[0].AxisY.Enabled = AxisEnabled.True;
    }

    protected void ShowTotalPhase_CHK_CheckedChanged(object sender, EventArgs e)
    {
        this.GPEQ_Chart.Series["Series2"].Enabled = this.ShowTotalPhase_CHK.Checked;
        this.GPEQ_Chart.ChartAreas[0].AxisY2.Enabled = AxisEnabled.True;
    }

    protected void SaveAndClose_BTN_Click(object sender, EventArgs e)
    {
        try
        {
            DialogResult result = MessageBox.Show(
            "Are you sure you want to save changes and close this form?",
            "Confirm Save Changes",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                //Save Changes
                this.SavedChanges = true;
                this.ParentFilters?.Clear();
                this.ParentFilters?.AddRange(this.Filters);

                // Close the current form
                this.Close();
            }
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }

    protected void Refresh_BTN_Click(object sender, EventArgs e)
    {
        try
        {
            this.DisplayMagnitudeResponse();
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }

    protected void Filters_LSB_SelectedIndexChanged(object sender, EventArgs e)
    {
        try
        {
            this.Apply_PEQ_BTN.Enabled = false;
            this.Apply_HPFLPF_BTN.Enabled = false;

            var SelectedIndex = this.Filters_LSB.SelectedIndex;
            if (SelectedIndex > -1 && this.Filters.Count > 0 && SelectedIndex < this.Filters.Count)
            {
                var TempFilter = this.Filters[SelectedIndex];
                if (TempFilter != null)
                {
                    if (TempFilter is BiQuadFilter BiquadFilter)
                    {
                        this.txtF.Text = BiquadFilter.Frequency.ToString();
                        this.txtQ.Text = BiquadFilter.Q.ToString();
                        this.txtG.Text = BiquadFilter.Gain.ToString();

                        this.PEQEnabled_CHK.Checked = BiquadFilter.FilterEnabled;
                        this.Apply_PEQ_BTN.Enabled = true;
                    }
                    else if (TempFilter is Basic_HPF_LPF HPF_LPF_Filter)
                    {
                        this.HPFFreq_TXT.Text = HPF_LPF_Filter.HPFFreq.ToString();
                        this.HPF_CBO.SelectedItem = HPF_LPF_Filter.HPFFilter;

                        this.LPFFreq_TXT.Text = HPF_LPF_Filter.LPFFreq.ToString();
                        this.LPF_CBO.SelectedItem = HPF_LPF_Filter.LPFFilter;

                        this.HPF_LPF_Enabled_CHK.Checked = HPF_LPF_Filter.FilterEnabled;
                        this.Apply_HPFLPF_BTN.Enabled = true;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }

    protected void Add_HPFLPF_BTN_Click(object sender, EventArgs e)
    {
        try
        {
            var Temp_Filter = new Basic_HPF_LPF();
            this.AddFilter(Temp_Filter);
            this.DisplayMagnitudeResponse();
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }

    protected void Add_PEQ_BTN_Click(object sender, System.EventArgs e)
    {
        try
        {
            var Temp_Filter = new BiQuadFilter();
            this.AddFilter(Temp_Filter);
            this.DisplayMagnitudeResponse();
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }

    protected void Apply_HPFLPF_BTN_Click(object sender, EventArgs e)
    {
        try
        {
            this.ApplySettingsToCurrentSelectedFilterItem();
            this.DisplayMagnitudeResponse();
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }

    protected void Apply_PEQ_BTN_Click(object sender, EventArgs e)
    {
        try
        {
            this.ApplySettingsToCurrentSelectedFilterItem();
            this.DisplayMagnitudeResponse();
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }

    protected void DeleteFilter_BTN_Click(object sender, System.EventArgs e)
    {
        try
        {
            var Index = this.Filters_LSB.SelectedIndex;
            if (Index < 0)
                return;

            this.Filters_LSB.Items.RemoveAt(Index);
            this.Filters.RemoveAt(Index);

            int NewSelectedIndex = Index - 1;
            if (NewSelectedIndex < 0 && this.Filters_LSB.Items.Count > 0)
            {
                NewSelectedIndex = 0;
            }

            this.Filters_LSB.SelectedIndex = NewSelectedIndex;

            this.DisplayMagnitudeResponse();
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }

    protected void MoveFilterUp_BTN_Click(object sender, EventArgs e)
    {
        try
        {
            var SelectedIndex = this.Filters_LSB.SelectedIndex;
            if (SelectedIndex == -1)
                return;

            var OldIndex = SelectedIndex;
            var NewIndex = OldIndex - 1;
            if (NewIndex > -1)
            {
                var SelectedFilter = this.Filters[OldIndex];
                this.Filters.RemoveAt(OldIndex);
                this.Filters.Insert(NewIndex, SelectedFilter);
                this.DisplayFilters();
                this.Filters_LSB.SelectedIndex = NewIndex;
            }
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }

    protected void MoveFilterDown_BTN_Click(object sender, EventArgs e)
    {
        try
        {
            var SelectedIndex = this.Filters_LSB.SelectedIndex;
            if (SelectedIndex == -1)
                return;

            var OldIndex = SelectedIndex;
            var NewIndex = OldIndex + 1;
            if (NewIndex < this.Filters.Count)
            {
                var SelectedFilter = this.Filters[OldIndex];
                this.Filters.RemoveAt(OldIndex);
                this.Filters.Insert(NewIndex, SelectedFilter);
                this.DisplayFilters();
                this.Filters_LSB.SelectedIndex = NewIndex;
            }
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }

    protected void DiscardAndClose_BTN_Click(object sender, EventArgs e)
    {
        try
        {
            DialogResult result = MessageBox.Show(
            "Are you sure you want to discard changes and close this form?",
            "Confirm Discard Changes",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
                this.Close();
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }
    #endregion

    #region Protected Functions

    protected void InitDefaults()
    {
        this.FFTSize_CBO.SelectedIndex = 0;

        this.Filters_LSB.Items.Clear();

        this.HPF_CBO.Items.Clear();
        this.LPF_CBO.Items.Clear();
        var EnumArray = Enum.GetValues(typeof(Basic_HPF_LPF.FilterOrder)).Cast<object>().ToArray();
        this.HPF_CBO.Items.AddRange(EnumArray);
        this.HPF_CBO.SelectedIndex = this.HPF_CBO.Items.Count - 1;
        this.LPF_CBO.Items.AddRange(EnumArray);
        this.LPF_CBO.SelectedIndex = this.HPF_CBO.Items.Count - 1;

        this.HPFFreq_TXT.MaxLength = 9;
        this.LPFFreq_TXT.MaxLength = 9;

        this.GPEQ_Chart.SuppressExceptions = true;
    }

    protected void ApplySettingsToCurrentSelectedFilterItem()
    {
        var SelectedIndex = this.Filters_LSB.SelectedIndex;
        if (SelectedIndex > -1 && this.Filters.Count > 0 && SelectedIndex < this.Filters.Count)
        {
            var Filter = this.Filters[SelectedIndex];
            if (Filter != null)
            {
                this.SetFilterOptions(Filter);
                Filter.ApplySettings();
                this.DisplayFilters();
                this.Filters_LSB.SelectedIndex = SelectedIndex;
            }
        }
    }

    protected void AddFilter(IFilter input)
    {
        this.SetFilterOptions(input);
        input.ApplySettings();

        this.Filters.Add(input);
        this.DisplayFilters();
        this.Filters_LSB.SelectedIndex = this.Filters_LSB.Items.Count - 1;
    }

    protected void DisplayFilters()
    {
        int i = 0;
        this.Filters_LSB.Items.Clear();
        foreach (var filter in this.Filters)
        {
            i++;
            if (filter != null)
            {
                var Text = i + " " + this.GetListText(filter);
                this.Filters_LSB.Items.Add(Text);
            }
        }
    }

    protected void SelectFirstListBoxItem()
    {
        if (this.Filters_LSB.Items.Count > 0)
            this.Filters_LSB.SelectedIndex = 0;
    }

    protected void SetFilterOptions(IFilter input)
    {
        if (input is BiQuadFilter Temp_Biquad)
        {
            double Freq = double.Parse(this.txtF.Text);
            double Q = double.Parse(this.txtQ.Text);
            double Gain = double.Parse(this.txtG.Text);
            Temp_Biquad.BiQuadFilterType = BiQuadFilter.BiQuadFilterTypes.PEQ;
            Temp_Biquad.PeakingEQ(Program.DSP_Info.InSampleRate, Freq, Q, Gain);

            Temp_Biquad.FilterEnabled = this.PEQEnabled_CHK.Checked;
        }
        else if (input is Basic_HPF_LPF Temp_HPF_LPF)
        {
            Temp_HPF_LPF.HPFFreq = double.Parse(this.HPFFreq_TXT.Text);
            if (this.HPF_CBO.SelectedItem != null)
                Temp_HPF_LPF.HPFFilter = (Basic_HPF_LPF.FilterOrder)this.HPF_CBO.SelectedItem;

            Temp_HPF_LPF.LPFFreq = double.Parse(this.LPFFreq_TXT.Text);
            if (this.LPF_CBO.SelectedItem != null)
                Temp_HPF_LPF.LPFFilter = (Basic_HPF_LPF.FilterOrder)this.LPF_CBO.SelectedItem;

            Temp_HPF_LPF.FilterEnabled = this.HPF_LPF_Enabled_CHK.Checked;
        }
    }

    protected void DisplayMagnitudeResponse()
    {
        int FFTSize = this.FFTSize_CBO.SelectedIndex == 0 ? 8192 : 262144; // Size of FFT
        int ZeroPadding = 0;              // Additional zero-padding (if desired)
        double WindowScaleFactor = 1.0;   // Window compensation factor (if using window)
        var WindowType = DSP.Window.Type.None;
        double sampleRate = Program.DSP_Info.InSampleRate;

        var temp_FFT = new FFT(FFTSize, ZeroPadding);
        Application.DoEvents();

        // Create test signal
        var TestSignal = new Complex[FFTSize];
        for (int i = 0; i < FFTSize; i++)
        {
            if (i <= FFTSize / 2)
                TestSignal[i] = new Complex(1.0, 0.0); // Flat magnitude, zero phase
            else
                TestSignal[i] = Complex.Conjugate(TestSignal[FFTSize - i]);
        }

        Application.DoEvents();
        double[] DataBuffer = temp_FFT.Perform_IFFT(TestSignal);
        Application.DoEvents();

        // Put test signal through the filter stack, and collect the output results
        int FilterCount = this.Filters.Count;
        for (int i = 0; i < FilterCount; i++)
        {
            if (this.Filters[i] != null && this.Filters[i].FilterEnabled)
                DataBuffer = this.Filters[i].Transform(DataBuffer, new DSP_Stream());
        }

        Application.DoEvents();
        // Calculate windowing
        var WindowCoefficients = DSP.Window.Coefficients(WindowType, FFTSize);
        WindowScaleFactor = DSP.Window.ScaleFactor.Signal(WindowCoefficients);

        Application.DoEvents();
        // Perform FFT of the result
        Complex[] freqResponseFFT = temp_FFT.Perform_FFT(DataBuffer, WindowCoefficients);
        Application.DoEvents();

        // Calculate frequency axis for plotting
        double[] freqSpan = temp_FFT.FrequencySpan(sampleRate);
        freqSpan[0] = 0.0001; // avoid log(0)
        Application.DoEvents();

        // Keep only the real (non-mirrored) half of the spectrum
        int halfLen = freqResponseFFT.Length / 2 + 1;
        var realHalf = new Complex[halfLen];
        Array.Copy(freqResponseFFT, realHalf, halfLen);
        Application.DoEvents();

        // Convert FFT results to magnitude in decibels
        double[] mag = DSP.ConvertComplex.ToMagnitude(realHalf);
        Application.DoEvents();
        double[] magLog = DSP.ConvertMagnitude.ToMagnitudeDBV(mag);
        Application.DoEvents();
        double[] phaseDeg = DSP.ConvertComplex.ToPhaseDegrees(realHalf);
        Application.DoEvents();

        // ---------------------
        // PHASE CALCULATION
        // ---------------------
        // Compute phase in degrees: atan2(imag, real) * 180/π
        //double[] phaseDeg = new double[halfLen];
        //for (int i = 0; i < halfLen; i++)
        //{
        //    // Phase in degrees
        //    phaseDeg[i] = Math.Atan2(realHalf[i].Imaginary, realHalf[i].Real) * (180.0 / Math.PI);
        //}

        // Plot both magnitude and phase
        int MinHz = 1;
        int MaxHz = (int)(sampleRate / 2.0);
        Plot_FFT(this.GPEQ_Chart, MinHz, MaxHz, freqSpan, magLog, phaseDeg);
    }

    protected void Plot_FFT(Chart chartControl, double min, double max, double[] xData, double[] magData, double[] phaseData)
    {

        chartControl.SuspendLayout();

        // Configure magnitude axis (primary Y-axis)
        chartControl.ChartAreas[0].AxisY.Interval = 12;
        chartControl.ChartAreas[0].AxisY.IntervalType = DateTimeIntervalType.Number;
        chartControl.ChartAreas[0].AxisY.Maximum = double.Parse(this.maxdB_TXT.Text);
        chartControl.ChartAreas[0].AxisY.Minimum = double.Parse(this.mindB_TXT.Text);
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

        // Configure "Series2" to use the secondary Y-axis
        chartControl.Series["Series1"].YAxisType = AxisType.Primary;
        chartControl.Series["Series1"].ChartType = SeriesChartType.Line;
        chartControl.Series["Series1"].Color = System.Drawing.Color.Blue;
        chartControl.Series["Series1"].BorderWidth = 2;

        chartControl.Series["Series2"].YAxisType = AxisType.Secondary;
        chartControl.Series["Series2"].ChartType = SeriesChartType.Line;
        chartControl.Series["Series2"].Color = System.Drawing.Color.Red;
        chartControl.Series["Series2"].BorderWidth = 2;

        // Enable and configure secondary Y-axis for Phase
        chartControl.ChartAreas[0].AxisY2.Title = "Phase (Degrees)";
        chartControl.ChartAreas[0].AxisY2.MajorGrid.Enabled = false; // optional
        chartControl.ChartAreas[0].AxisY2.MinorGrid.Enabled = false; // optional
        chartControl.ChartAreas[0].AxisY2.Minimum = -180;
        chartControl.ChartAreas[0].AxisY2.Maximum = 180;
        chartControl.ChartAreas[0].AxisY2.Interval = 90;

        chartControl.Series["Series1"].Points.Clear();
        chartControl.Series["Series1"].Points.DataBindXY(xData, magData);

        chartControl.Series["Series2"].Points.Clear();
        chartControl.Series["Series2"].Points.DataBindXY(xData, phaseData);

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

        this.GPEQ_Chart.Series["Series1"].Enabled = this.ShowTotalMag_CHK.Checked;
        this.GPEQ_Chart.Series["Series2"].Enabled = this.ShowTotalPhase_CHK.Checked;

        chartControl.ResumeLayout();
    }

    #endregion

    #region Error Handling
    protected void Error(Exception ex)
    {
        Debug.Error(ex);
    }
    #endregion
}