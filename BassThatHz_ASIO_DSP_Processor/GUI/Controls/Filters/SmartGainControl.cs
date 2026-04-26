#nullable enable

namespace BassThatHz_ASIO_DSP_Processor.GUI.Controls;

#region Usings
using NAudio.Utils;
using System;
using System.Globalization;
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
public partial class SmartGainControl : UserControl, IFilterControl
{
    #region Variables
    protected SmartGain Filter = new();
    #endregion

    #region Constructor and MapEventHandlers
    public SmartGainControl()
    {
        InitializeComponent();
        this.MapEventHandlers();
    }

    public void MapEventHandlers()
    {
        this.txtGain.KeyPress += TxtGain_KeyPress;
        InputValidator.Set_TextBox_MaxLength(this.txtGain);

        this.txtDuration.KeyPress += TxtDuration_KeyPress;
        this.txtDuration.MaxLength = 5;
    }

    #endregion

    #region Event Handlers

    #region InputValidation
    protected void TxtGain_KeyPress(object? sender, KeyPressEventArgs e)
    {
        InputValidator.Validate_IsNumeric_Negative(e);
        var limited = InputValidator.LimitTo_ReasonableSizedNumber(this.txtGain.Text);
        if (!string.Equals(this.txtGain.Text, limited, StringComparison.Ordinal))
        {
            this.txtGain.Text = limited;
            this.txtGain.SelectionStart = this.txtGain.Text.Length;
        }
    }

    protected void TxtDuration_KeyPress(object? sender, KeyPressEventArgs e)
    {
        InputValidator.Validate_IsNumeric_NonNegative(e);
        var limited = InputValidator.LimitTo_ReasonableSizedNumber(this.txtDuration.Text);
        if (!string.Equals(this.txtDuration.Text, limited, StringComparison.Ordinal))
        {
            this.txtDuration.Text = limited;
            this.txtDuration.SelectionStart = this.txtDuration.Text.Length;
        }
    }
    #endregion

    protected void btnApply_Click(object? sender, EventArgs e)
    {
        try
        {
            // Use TryParse with invariant culture to avoid exceptions and unnecessary allocations
            if (!double.TryParse(this.txtGain.Text, NumberStyles.Float | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var gain))
                throw new FormatException("Invalid gain value");

            if (!double.TryParse(this.txtDuration.Text, NumberStyles.Float | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var durationMs))
                throw new FormatException("Invalid duration value");

            this.Filter.GaindB = gain;
            this.Filter.Duration = TimeSpan.FromMilliseconds(durationMs);
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }

    protected void chkPeak_CheckedChanged(object? sender, EventArgs e)
    {
        try
        {
            this.chkPeakHold.Checked = !this.chkPeak.Checked;
            this.Filter.PeakHold = this.chkPeakHold.Checked;
            this.txtDuration.Enabled = this.chkPeak.Checked;
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }

    protected void chkPeakHold_CheckedChanged(object? sender, EventArgs e)
    {
        try
        {
            this.chkPeak.Checked = !this.chkPeakHold.Checked;
            this.Filter.PeakHold = this.chkPeakHold.Checked;
            this.txtDuration.Enabled = this.chkPeak.Checked;
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }

    protected void RefreshStats_Timer_Tick(object? sender, EventArgs e)
    {
        try
        {
            // Cache filter locally to avoid repeated property access and use invariant culture for formatting
            var filter = this.Filter;
            var applied = filter.ActualGaindB;
            var peak = Decibels.LinearToDecibels(filter.PeakLevelLinear);
            var head = Decibels.LinearToDecibels(filter.HeadroomLinear);

            var fmt = "000.0";
            this.lblAppliedGain.Text = applied.ToString(fmt, CultureInfo.InvariantCulture);
            this.lblPeakLevel.Text = peak.ToString(fmt, CultureInfo.InvariantCulture);
            this.lblHeadroom.Text = head.ToString(fmt, CultureInfo.InvariantCulture);
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }
    #endregion

    #region Interfaces
    public IFilter GetFilter =>
        this.Filter;

    public void ApplySettings()
    {
        this.btnApply_Click(this, EventArgs.Empty);
        this.Filter.ApplySettings();
    }

    public void SetDeepClonedFilter(IFilter input)
    {
        if (input is SmartGain smartGain)
        {
            this.Filter = smartGain;

            // Use invariant culture when converting numbers to strings to avoid culture-related allocations
            this.txtDuration.Text = smartGain.Duration.TotalMilliseconds.ToString(CultureInfo.InvariantCulture);
            this.txtGain.Text = smartGain.GaindB.ToString(CultureInfo.InvariantCulture);
            this.chkPeakHold.Checked = smartGain.PeakHold;
            this.chkPeak.Checked = !smartGain.PeakHold;
        }
    }
    #endregion

    #region Error Handling
    protected void Error(Exception ex)
    {
        Debug.Error(ex);
    }
    #endregion
}