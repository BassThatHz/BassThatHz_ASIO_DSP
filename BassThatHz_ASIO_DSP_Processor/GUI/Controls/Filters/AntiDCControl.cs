#nullable enable

namespace BassThatHz_ASIO_DSP_Processor.GUI.Controls;

#region Usings
using System;
using System.Drawing;
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
public partial class AntiDCControl : UserControl, IFilterControl
{
    #region Variables
    protected AntiDC Filter = new();
    protected readonly string MutedTextStringFormat = string.Empty;
    protected readonly string Default_MutedTextString = string.Empty;
    #endregion

    #region Constructor and MapEventHandlers
    public AntiDCControl()
    {
        InitializeComponent();

        try
        {
            this.MutedTextStringFormat = this.chkOutputMuted.Text;
            this.Default_MutedTextString = string.Format(this.MutedTextStringFormat, 0, 0);
            this.chkOutputMuted.Text = this.Default_MutedTextString;

            this.MapEventHandlers();
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }

    public void MapEventHandlers()
    {
        this.Filter.OutputMutedEvent += this.Filter_OutputMutedEvent;
        this.Filter.ClipEvent += this.Filter_ClipEvent;
        this.ClipThreshold.VolumeChanged += this.ClipThresholdChanged;
        this.DCThreshold.VolumeChanged += this.DCThresholdChanged;

        this.txtConsecutiveDCSamples.KeyPress += TxtConsecutiveDCSamples_KeyPress;
        this.txtDuration.KeyPress += txtDuration_KeyPress;

        InputValidator.Set_TextBox_MaxLength(this.txtConsecutiveDCSamples);
        InputValidator.Set_TextBox_MaxLength(this.txtDuration);
    }
    #endregion

    #region Event Handlers

    #region InputValidation
    protected void TxtConsecutiveDCSamples_KeyPress(object? sender, KeyPressEventArgs e)
    {
        InputValidator.Validate_IsNumeric_NonNegative(e);
        this.txtConsecutiveDCSamples.Text = InputValidator.LimitTo_ReasonableSizedNumber(this.txtConsecutiveDCSamples.Text);
    }

    protected void txtDuration_KeyPress(object? sender, KeyPressEventArgs e)
    {
        InputValidator.Validate_IsNumeric_NonNegative(e);
        this.txtDuration.Text = InputValidator.LimitTo_ReasonableSizedNumber(this.txtDuration.Text);
    }
    #endregion

    protected void ClipThresholdChanged(object? sender, EventArgs e)
    {
        this.Filter.Clip_Threshold = this.ClipThreshold.Volume;
    }

    protected void DCThresholdChanged(object? sender, EventArgs e)
    {
        this.Filter.DC_Threshold = this.DCThreshold.Volume;
    }

    protected void Filter_ClipEvent(object? sender, AntiDC.ClippedInfoArgs e)
    {
        try
        {
            //We might not be on the UI thread or Multi-Thread mode, use SafeInvoke
            this.SafeInvoke(() =>
            {
                if (!this.chkOutputMuted.Checked)
                {
                    this.chkOutputMuted.Text = string.Format(this.MutedTextStringFormat,
                                                            e.ClippedSamples,
                                                            e.ClippedEvents);
                }
            });
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }

    protected void Filter_OutputMutedEvent(object? sender, AntiDC.ClippedInfoArgs e)
    {
        try
        {
            //We might not be on the UI thread or Multi-Thread mode, use SafeInvoke
            this.SafeInvoke(() =>
            {
                if (!this.chkOutputMuted.Checked)
                {
                    this.chkOutputMuted.Text = string.Format(this.MutedTextStringFormat,
                                                            e.ClippedSamples,
                                                            e.ClippedEvents);
                    //Only enabled when it is muted and checked
                    this.chkOutputMuted.ForeColor = Color.Red;
                    this.chkOutputMuted.Checked = true;
                    this.chkOutputMuted.Enabled = true;
                }
            });
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }

    protected void chkOutputMuted_CheckedChanged(object? sender, EventArgs e)
    {
        try
        {
            if (this.chkOutputMuted.Enabled)
            {
                //Only enabled when it is muted and checked, thus false is assumed
                this.chkOutputMuted.Enabled = false;
                this.chkOutputMuted.Text = this.Default_MutedTextString;
                this.chkOutputMuted.ForeColor = Color.Black;
                this.SetFilterSettings();
                this.Filter.ResetDetection();
            }
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }

    protected void btnApply_Click(object? sender, System.EventArgs e)
    {
        try
        {
            this.SetFilterSettings();
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }
    #endregion

    #region Protected Functions
    protected void SetFilterSettings()
    {
        this.Filter.DetectionDuration = TimeSpan.FromMilliseconds(int.Parse(this.txtDuration.Text));
        this.Filter.MaxClipEventsPerDuration = int.Parse(this.txtEvents.Text);
        this.Filter.MaxConsecutiveDCSamples = int.Parse(this.txtConsecutiveDCSamples.Text);
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
        if (input is AntiDC antiDC)
        {
            this.Filter = antiDC;

            this.txtConsecutiveDCSamples.Text = antiDC.MaxConsecutiveDCSamples.ToString();
            this.txtDuration.Text = antiDC.DetectionDuration.TotalMilliseconds.ToString();
            this.txtEvents.Text = antiDC.MaxClipEventsPerDuration.ToString();
            this.DCThreshold.Volume = antiDC.DC_Threshold;
            this.ClipThreshold.Volume = antiDC.Clip_Threshold;
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