#nullable enable

namespace BassThatHz_ASIO_DSP_Processor.GUI.Forms;

#region Usings
using DSPLib;
using NAudio.Dsp;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Text;
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

public partial class FormGPEQ : Form
{
    #region Variables       
    protected List<IFilter>? ParentFilters;

    protected List<IFilter> Filters = new();
    // Cached chart/series references to avoid repeated name lookups and allocations
    private ChartArea? _chartArea0;
    private Series? _seriesMag;
    private Series? _seriesPhase;
    private Series? _seriesDummy;

    // Per-filter (Component / Individual) traces. Unlike the three series above these are created
    // and destroyed on demand, because how many of them there are depends on the filter list.
    private readonly List<Series> _perFilterSeries = new();

    // Every trace gets its own colour - a filter's magnitude and phase included - so each one can be
    // picked out of a chart that carries all of them at once. Colours are keyed on the filter
    // INSTANCE, not on its list index, so they survive a filter being moved up/down or deleted. A
    // plain list is enough: the lookup runs once per filter per redraw over a handful of filters.
    private readonly List<(IFilter Filter, System.Drawing.Color MagColor, System.Drawing.Color PhaseColor)> _filterColors = new();

    // Where the last trace colour was taken from along the usable hue arcs. Negative until the
    // first colour is handed out, which is when the random starting point is chosen.
    private double _nextHuePosition = -1;
    private readonly Random _colorRandom = new();
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
                var TempFilter = CopyFilter(Filter);
                if (TempFilter != null)
                    this.Filters.Add(TempFilter);
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
        if (_seriesMag != null)
            _seriesMag.Enabled = this.ShowTotalMag_CHK.Checked;
        if (_chartArea0 != null)
            _chartArea0.AxisY.Enabled = AxisEnabled.True;
    }

    protected void ShowTotalPhase_CHK_CheckedChanged(object sender, EventArgs e)
    {
        if (_seriesPhase != null)
            _seriesPhase.Enabled = this.ShowTotalPhase_CHK.Checked;
        if (_chartArea0 != null)
            _chartArea0.AxisY2.Enabled = AxisEnabled.True;
    }

    protected void ShowComponentMag_CHK_CheckedChanged(object sender, EventArgs e)
    {
        this.RedrawChart();
    }

    protected void ShowComponentPhase_CHK_CheckedChanged(object sender, EventArgs e)
    {
        this.RedrawChart();
    }

    protected void ShowIndividualMag_CHK_CheckedChanged(object sender, EventArgs e)
    {
        this.RedrawChart();
    }

    protected void ShowIndividualPhase_CHK_CheckedChanged(object sender, EventArgs e)
    {
        this.RedrawChart();
    }

    protected void SaveAndClose_BTN_Click(object sender, EventArgs e)
    {
        try
        {
            //Suppressed (non-interactive/test) default is Yes: the save was explicitly requested.
            DialogResult result = Debug.ShowMessage(
            "Are you sure you want to save changes and close this form?",
            "Confirm Save Changes",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question, DialogResult.Yes);

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
        this.RedrawChart();
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

            // The Individual traces follow the list selection, so they have to be recomputed here.
            // Nothing to do when the Component traces are the ones on screen: they already cover
            // every filter and do not depend on which one is selected.
            if (this.IsIndividualMagVisible || this.IsIndividualPhaseVisible)
                this.DisplayMagnitudeResponse();
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
            //Suppressed (non-interactive/test) default is Yes: the discard was explicitly requested.
            DialogResult result = Debug.ShowMessage(
            "Are you sure you want to discard changes and close this form?",
            "Confirm Discard Changes",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question, DialogResult.Yes);

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

        // Cache chart elements for performance to avoid repeated dictionary lookups
        if (this.GPEQ_Chart.ChartAreas.Count > 0)
            _chartArea0 = this.GPEQ_Chart.ChartAreas[0];

        if (this.GPEQ_Chart.Series.FindByName("Series1") != null)
            _seriesMag = this.GPEQ_Chart.Series["Series1"];
        if (this.GPEQ_Chart.Series.FindByName("Series2") != null)
            _seriesPhase = this.GPEQ_Chart.Series["Series2"];

        // Ensure a Dummy series exists once
        if (this.GPEQ_Chart.Series.FindByName("Dummy") == null)
        {
            _seriesDummy = new Series("Dummy");
            _seriesDummy.ChartType = SeriesChartType.Point;
            _seriesDummy.YAxisType = AxisType.Primary;
            _seriesDummy.IsVisibleInLegend = false;
            _seriesDummy.Points.AddXY(0, 0);
            this.GPEQ_Chart.Series.Add(_seriesDummy);
        }
        else
        {
            _seriesDummy = this.GPEQ_Chart.Series["Dummy"];
        }
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
        this.Filters_LSB.BeginUpdate();
        try
        {
            int i = 1;
            this.Filters_LSB.Items.Clear();
            foreach (var filter in this.Filters)
            {
                if (filter != null)
                {
                    this.Filters_LSB.Items.Add(string.Concat(i, " ", this.GetListText(filter)));
                }
                i++;
            }
        }
        finally
        {
            this.Filters_LSB.EndUpdate();
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
            double Freq = Temp_Biquad.Frequency;
            double Q = Temp_Biquad.Q;
            double Gain = Temp_Biquad.Gain;
            if (!string.IsNullOrWhiteSpace(this.txtF.Text) && double.TryParse(this.txtF.Text, out var tF))
                Freq = tF;
            if (!string.IsNullOrWhiteSpace(this.txtQ.Text) && double.TryParse(this.txtQ.Text, out var tQ))
                Q = tQ;
            if (!string.IsNullOrWhiteSpace(this.txtG.Text) && double.TryParse(this.txtG.Text, out var tG))
                Gain = tG;

            Temp_Biquad.BiQuadFilterType = BiQuadFilter.BiQuadFilterTypes.PEQ;
            Temp_Biquad.PeakingEQ(Program.DSP_Info.InSampleRate, Freq, Q, Gain);

            Temp_Biquad.FilterEnabled = this.PEQEnabled_CHK.Checked;
        }
        else if (input is Basic_HPF_LPF Temp_HPF_LPF)
        {
            if (!string.IsNullOrWhiteSpace(this.HPFFreq_TXT.Text) && double.TryParse(this.HPFFreq_TXT.Text, out var hpf))
                Temp_HPF_LPF.HPFFreq = hpf;
            if (this.HPF_CBO.SelectedItem != null)
                Temp_HPF_LPF.HPFFilter = (Basic_HPF_LPF.FilterOrder)this.HPF_CBO.SelectedItem;

            if (!string.IsNullOrWhiteSpace(this.LPFFreq_TXT.Text) && double.TryParse(this.LPFFreq_TXT.Text, out var lpf))
                Temp_HPF_LPF.LPFFreq = lpf;
            if (this.LPF_CBO.SelectedItem != null)
                Temp_HPF_LPF.LPFFilter = (Basic_HPF_LPF.FilterOrder)this.LPF_CBO.SelectedItem;

            Temp_HPF_LPF.FilterEnabled = this.HPF_LPF_Enabled_CHK.Checked;
        }
    }

    protected void RedrawChart()
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

    protected void DisplayMagnitudeResponse()
    {
        int FFTSize = this.FFTSize_CBO.SelectedIndex == 0 ? 8192 : 262144; // Size of FFT
        int ZeroPadding = 0;              // Additional zero-padding (if desired)
        double WindowScaleFactor = 1.0;   // Window compensation factor (if using window)
        var WindowType = DSP.Window.Type.None;
        double sampleRate = Program.DSP_Info.InSampleRate;
        var temp_FFT = new FFT(FFTSize, ZeroPadding);

        double[] TestSignal = BuildTestSignal(temp_FFT, FFTSize);

        // Put test signal through the filter stack, and collect the output results.
        // Transform() works in place, so the total pass gets its own copy of the test signal and
        // leaves the original for the per-filter passes below.
        double[] DataBuffer = (double[])TestSignal.Clone();
        int FilterCount = this.Filters.Count;
        for (int i = 0; i < FilterCount; i++)
        {
            if (this.Filters[i] != null && this.Filters[i].FilterEnabled)
                DataBuffer = this.Filters[i].Transform(DataBuffer, new DSP_Stream());
        }

        // Calculate windowing
        var WindowCoefficients = DSP.Window.Coefficients(WindowType, FFTSize);
        WindowScaleFactor = DSP.Window.ScaleFactor.Signal(WindowCoefficients);

        // Calculate frequency axis for plotting
        double[] freqSpan = temp_FFT.FrequencySpan(sampleRate);
        freqSpan[0] = 0.0001; // avoid log(0)

        // Convert FFT results to magnitude in decibels and phase
        ComputeResponse(temp_FFT, WindowCoefficients, DataBuffer, out var magLog, out var phaseDeg);

        // Plot both magnitude and phase
        int MinHz = 1;
        int MaxHz = (int)(sampleRate / 2.0);
        Plot_FFT(this.GPEQ_Chart, MinHz, MaxHz, freqSpan, magLog, phaseDeg);

        // Plot the Component / Individual traces on top of the total
        this.Plot_PerFilterResponses(temp_FFT, WindowCoefficients, TestSignal, freqSpan);
    }

    /// <summary>
    /// Builds the flat-magnitude / zero-phase test signal that every response measurement uses.
    /// </summary>
    protected static double[] BuildTestSignal(FFT fft, int fftSize)
    {
        // Create test signal (symmetric spectrum -> real time-domain signal)
        var TestSignal = new Complex[fftSize];
        for (int i = 0; i < fftSize; i++)
        {
            if (i <= fftSize / 2)
                TestSignal[i] = new Complex(1.0, 0.0); // Flat magnitude, zero phase
            else
                TestSignal[i] = Complex.Conjugate(TestSignal[fftSize - i]);
        }

        return fft.Perform_IFFT(TestSignal);
    }

    /// <summary>
    /// FFTs a filtered test signal and hands back the real (non-mirrored) half of the spectrum as
    /// magnitude in dBV and phase in degrees.
    /// </summary>
    protected static void ComputeResponse(FFT fft, double[] windowCoefficients, double[] dataBuffer,
                                          out double[] magLog, out double[] phaseDeg)
    {
        // Perform FFT of the result
        Complex[] FreqResponseFFT = fft.Perform_FFT(dataBuffer, windowCoefficients);

        // Keep only the real (non-mirrored) half of the spectrum
        int HalfLen = FreqResponseFFT.Length / 2 + 1;
        var RealHalf = new Complex[HalfLen];
        Array.Copy(FreqResponseFFT, RealHalf, HalfLen);

        double[] Mag = DSP.ConvertComplex.ToMagnitude(RealHalf);
        magLog = DSP.ConvertMagnitude.ToMagnitudeDBV(Mag);
        phaseDeg = DSP.ConvertComplex.ToPhaseDegrees(RealHalf);
    }

    /// <summary>
    /// True when the Individual Mag trace should be drawn. The Component trace wins when both are
    /// ticked, because the Individual trace would only duplicate one of the Component traces.
    /// </summary>
    protected bool IsIndividualMagVisible =>
        this.ShowIndividualMag_CHK.Checked && !this.ShowComponentMag_CHK.Checked;

    /// <summary>
    /// True when the Individual Phase trace should be drawn. See <see cref="IsIndividualMagVisible"/>.
    /// </summary>
    protected bool IsIndividualPhaseVisible =>
        this.ShowIndividualPhase_CHK.Checked && !this.ShowComponentPhase_CHK.Checked;

    /// <summary>
    /// Draws one trace per filter: every enabled filter for Component, or only the filter currently
    /// selected in the list box for Individual. Each filter is measured on its own copy of the test
    /// signal, so what is drawn is that filter's own contribution rather than the running total.
    /// </summary>
    protected void Plot_PerFilterResponses(FFT fft, double[] windowCoefficients, double[] testSignal, double[] freqSpan)
    {
        this.ClearPerFilterSeries();

        bool ShowComponentMag = this.ShowComponentMag_CHK.Checked;
        bool ShowComponentPhase = this.ShowComponentPhase_CHK.Checked;
        bool ShowIndividualMag = this.IsIndividualMagVisible;
        bool ShowIndividualPhase = this.IsIndividualPhaseVisible;

        if (!ShowComponentMag && !ShowComponentPhase && !ShowIndividualMag && !ShowIndividualPhase)
            return;

        var SelectedIndex = this.Filters_LSB.SelectedIndex;

        this.GPEQ_Chart.SuspendLayout();
        try
        {
            int FilterCount = this.Filters.Count;
            for (int i = 0; i < FilterCount; i++)
            {
                var Filter = this.Filters[i];

                // A disabled filter contributes nothing to the total, so it contributes no
                // component trace either.
                if (Filter == null || !Filter.FilterEnabled)
                    continue;

                bool IsSelected = i == SelectedIndex;
                bool NeedsMag = ShowComponentMag || (ShowIndividualMag && IsSelected);
                bool NeedsPhase = ShowComponentPhase || (ShowIndividualPhase && IsSelected);
                if (!NeedsMag && !NeedsPhase)
                    continue;

                var FilterOutput = GetSingleFilterResponse(Filter, testSignal);
                ComputeResponse(fft, windowCoefficients, FilterOutput, out var MagLog, out var PhaseDeg);

                var TraceColors = this.GetFilterColors(Filter);

                if (NeedsMag)
                    this.AddPerFilterSeries("Filter_Mag_" + i, AxisType.Primary, TraceColors.MagColor, freqSpan, MagLog);

                if (NeedsPhase)
                    this.AddPerFilterSeries("Filter_Phase_" + i, AxisType.Secondary, TraceColors.PhaseColor, freqSpan, PhaseDeg);
            }
        }
        finally
        {
            this.GPEQ_Chart.ResumeLayout();
        }
    }

    /// <summary>
    /// Runs the test signal through a single filter, on a copy of both the signal and the filter, so
    /// neither the buffer nor the filter state used by the total trace is disturbed.
    /// </summary>
    protected static double[] GetSingleFilterResponse(IFilter filter, double[] testSignal)
    {
        var DataBuffer = (double[])testSignal.Clone();

        var TempFilter = CopyFilter(filter);
        if (TempFilter == null)
            return DataBuffer;

        return TempFilter.Transform(DataBuffer, new DSP_Stream());
    }

    /// <summary>
    /// Copies a filter's settings onto a brand new instance of the same type, or returns null for a
    /// filter type this form does not handle. The copy starts with clean biquad history, so it can
    /// be measured in isolation.
    /// </summary>
    protected static IFilter? CopyFilter(IFilter? input)
    {
        if (input is BiQuadFilter BiQuadFilter)
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
            return TempFilter;
        }

        if (input is Basic_HPF_LPF HPF_LPF)
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
            return TempFilter;
        }

        return null;
    }

    protected void ClearPerFilterSeries()
    {
        foreach (var TempSeries in this._perFilterSeries)
            this.GPEQ_Chart.Series.Remove(TempSeries);

        this._perFilterSeries.Clear();
    }

    protected void AddPerFilterSeries(string name, AxisType axisType, System.Drawing.Color color,
                                      double[] xData, double[] yData)
    {
        var TempSeries = new Series(name)
        {
            ChartArea = (this._chartArea0 ?? this.GPEQ_Chart.ChartAreas[0]).Name,
            ChartType = SeriesChartType.Line,
            YAxisType = axisType,
            Color = color,
            BorderWidth = 1,
            IsVisibleInLegend = false,
        };

        // Phase traces are dashed, which is what says a trace reads against the right hand axis
        // rather than the left. The colour says WHICH trace it is; the dashes say what it measures.
        if (axisType == AxisType.Secondary)
            TempSeries.BorderDashStyle = ChartDashStyle.Dash;

        TempSeries.Points.DataBindXY(xData, yData);

        this.GPEQ_Chart.Series.Add(TempSeries);
        this._perFilterSeries.Add(TempSeries);
    }

    /// <summary>
    /// Hands out a pair of random colours per filter, one for its magnitude trace and a different
    /// one for its phase trace. They are remembered for as long as the form is open, so the traces
    /// do not change colour on every redraw.
    /// </summary>
    protected (System.Drawing.Color MagColor, System.Drawing.Color PhaseColor) GetFilterColors(IFilter filter)
    {
        foreach (var Entry in this._filterColors)
        {
            if (ReferenceEquals(Entry.Filter, filter))
                return (Entry.MagColor, Entry.PhaseColor);
        }

        var MagColor = ColorFromHue(this.NextTraceHue());
        var PhaseColor = ColorFromHue(this.NextTraceHue());

        this._filterColors.Add((filter, MagColor, PhaseColor));
        return (MagColor, PhaseColor);
    }

    #region Hue Arcs
    // The total magnitude is blue (hue 240) and the total phase is red (hue 0/360), so a 20 degree
    // band around each of those is skipped: arc A is 20..220, arc B is 260..340. Positions run
    // 0..280 across the two arcs end to end, and map back onto a hue below.
    protected const double ArcAStart = 20.0;
    protected const double ArcAWidth = 200.0;
    protected const double ArcBStart = 260.0;
    protected const double ArcBWidth = 80.0;
    protected const double UsableHueWidth = ArcAWidth + ArcBWidth;

    // Stepping by the golden ratio drops each new trace into the widest gap left by the previous
    // ones, so colours stay well apart however many traces there are. Picking a hue at random each
    // time cannot promise that: two traces can land on top of each other by chance.
    protected const double GoldenHueStep = UsableHueWidth * 0.6180339887498949;
    #endregion

    /// <summary>
    /// Hands out the next trace hue: a random starting point, then a golden-ratio step per trace.
    /// The start is what makes the palette come out different each time the form is opened; the step
    /// is what keeps the traces on one chart tellable apart.
    /// </summary>
    protected double NextTraceHue()
    {
        this._nextHuePosition = this._nextHuePosition < 0
            ? this._colorRandom.NextDouble() * UsableHueWidth
            : (this._nextHuePosition + GoldenHueStep) % UsableHueWidth;

        return this._nextHuePosition < ArcAWidth
            ? ArcAStart + this._nextHuePosition
            : ArcBStart + (this._nextHuePosition - ArcAWidth);
    }

    /// <summary>
    /// Random hue at a fixed saturation and brightness, which keeps every generated colour readable
    /// against the white plot area (a plain random RGB triple can land on near-white or near-black).
    /// </summary>
    protected static System.Drawing.Color ColorFromHue(double hue)
    {
        const double Saturation = 0.85;
        const double Brightness = 0.75;

        double C = Brightness * Saturation;
        double X = C * (1.0 - Math.Abs(hue / 60.0 % 2.0 - 1.0));
        double M = Brightness - C;

        double R, G, B;
        if (hue < 60) { R = C; G = X; B = 0; }
        else if (hue < 120) { R = X; G = C; B = 0; }
        else if (hue < 180) { R = 0; G = C; B = X; }
        else if (hue < 240) { R = 0; G = X; B = C; }
        else if (hue < 300) { R = X; G = 0; B = C; }
        else { R = C; G = 0; B = X; }

        return System.Drawing.Color.FromArgb(
            (int)Math.Round((R + M) * 255.0),
            (int)Math.Round((G + M) * 255.0),
            (int)Math.Round((B + M) * 255.0));
    }

    protected void Plot_FFT(Chart chartControl, double min, double max, double[] xData, double[] magData, double[] phaseData)
    {

        chartControl.SuspendLayout();

        var chartArea = _chartArea0 ?? chartControl.ChartAreas[0];

        // Configure magnitude axis (primary Y-axis)
        chartArea.AxisY.Interval = 12;
        chartArea.AxisY.IntervalType = DateTimeIntervalType.Number;
        //DEFECT FIX: double.Parse on user-editable text boxes escalated a half-typed value into
        //the "A fatal error has occured / abort the app?" dialog. Fall back to the existing axis
        //bounds when the text is not a valid number.
        if (double.TryParse(this.maxdB_TXT.Text, out double Local_MaxDb))
            chartArea.AxisY.Maximum = Local_MaxDb;
        if (double.TryParse(this.mindB_TXT.Text, out double Local_MinDb))
            chartArea.AxisY.Minimum = Local_MinDb;
        chartArea.AxisY.MinorGrid.Enabled = true;
        chartArea.AxisY.MinorGrid.Interval = 3;
        chartArea.AxisY.Title = "Magnitude (dB)";

        // Configure X-axis (frequency)
        chartArea.AxisX.IntervalType = DateTimeIntervalType.Number;
        chartArea.AxisX.MinorGrid.Enabled = true;
        chartArea.AxisX.MinorGrid.Interval = 1;
        chartArea.AxisX.Minimum = min;
        chartArea.AxisX.Maximum = max;
        chartArea.AxisX.IsLogarithmic = true;
        chartArea.AxisX.Title = "Frequency (Hz)";

        // Configure secondary Y-axis for Phase
        chartArea.AxisY2.Title = "Phase (Degrees)";
        chartArea.AxisY2.MajorGrid.Enabled = false;
        chartArea.AxisY2.MinorGrid.Enabled = false;
        chartArea.AxisY2.Minimum = -180;
        chartArea.AxisY2.Maximum = 180;
        chartArea.AxisY2.Interval = 90;

        var series1 = _seriesMag ?? chartControl.Series.FindByName("Series1") ?? chartControl.Series.Add("Series1");
        var series2 = _seriesPhase ?? chartControl.Series.FindByName("Series2") ?? chartControl.Series.Add("Series2");

        series1.YAxisType = AxisType.Primary;
        series1.ChartType = SeriesChartType.Line;
        series1.Color = System.Drawing.Color.Blue;
        series1.BorderWidth = 2;

        series2.YAxisType = AxisType.Secondary;
        series2.ChartType = SeriesChartType.Line;
        series2.Color = System.Drawing.Color.Red;
        series2.BorderWidth = 2;

        series1.Points.Clear();
        series1.Points.DataBindXY(xData, magData);

        series2.Points.Clear();
        series2.Points.DataBindXY(xData, phaseData);

        if (_seriesDummy == null)
        {
            if (chartControl.Series.FindByName("Dummy") == null)
            {
                _seriesDummy = new Series("Dummy");
                _seriesDummy.ChartType = SeriesChartType.Point;
                _seriesDummy.YAxisType = AxisType.Primary;
                _seriesDummy.IsVisibleInLegend = false;
                _seriesDummy.Points.AddXY(0, 0);
                chartControl.Series.Add(_seriesDummy);
            }
            else
            {
                _seriesDummy = chartControl.Series["Dummy"];
            }
        }
        if (_seriesDummy != null)
            _seriesDummy.Enabled = true;

        if (series1 != null)
            series1.Enabled = this.ShowTotalMag_CHK.Checked;
        if (series2 != null)
            series2.Enabled = this.ShowTotalPhase_CHK.Checked;

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