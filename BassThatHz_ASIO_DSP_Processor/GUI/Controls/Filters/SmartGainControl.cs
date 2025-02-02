#nullable enable

namespace BassThatHz_ASIO_DSP_Processor.GUI.Controls;

#region Usings
using NAudio.Utils;
using System;
using System.Windows.Forms;
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
        this.txtGain.Text = InputValidator.LimitTo_ReasonableSizedNumber(this.txtGain.Text);
    }

    protected void TxtDuration_KeyPress(object? sender, KeyPressEventArgs e)
    {
        InputValidator.Validate_IsNumeric_NonNegative(e);
        this.txtDuration.Text = InputValidator.LimitTo_ReasonableSizedNumber(this.txtDuration.Text);
    }
    #endregion

    protected void btnApply_Click(object? sender, EventArgs e)
    {
        try
        {
            this.Filter.GaindB = double.Parse(this.txtGain.Text);
            this.Filter.Duration = TimeSpan.FromMilliseconds(double.Parse(this.txtDuration.Text));
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
            this.lblAppliedGain.Text = this.Filter.ActualGaindB.ToString("000.0");
            this.lblPeakLevel.Text = Decibels.LinearToDecibels(this.Filter.PeakLevelLinear).ToString("000.0");
            this.lblHeadroom.Text = Decibels.LinearToDecibels(this.Filter.HeadroomLinear).ToString("000.0");
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

            this.txtDuration.Text = smartGain.Duration.TotalMilliseconds.ToString();
            this.txtGain.Text = smartGain.GaindB.ToString();
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