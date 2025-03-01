#nullable enable

namespace BassThatHz_ASIO_DSP_Processor;

#region Usings
using GUI;
using GUI.Controls;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
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

public partial class FormMonitoring : Form
{
    #region Variables
    List<BTH_VolumeLevelControl> VolControlList = new();
    #endregion

    #region Constructor and MapEventHandlers
    public FormMonitoring()
    {
        InitializeComponent();

        this.MapEventHandlers();
    }

    protected void MapEventHandlers()
    {
        this.Resize += Resize_Handler;
        this.msb_RefreshInterval.TextChanged += Msb_RefreshInterval_TextChanged;
        this.msb_RefreshInterval.KeyPress += Msb_RefreshInterval_KeyPress;
        this.Shown += FormMonitoring_Shown;
    }
    #endregion

    #region Event Handlers

    #region Shown
    protected void FormMonitoring_Shown(object? sender, EventArgs e)
    {
        try
        {
            this.Set_Pause_States();
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
            this.Set_Pause_States();
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }
    #endregion

    #region Timer
    protected void timer_Refresh_Tick(object? sender, EventArgs e)
    {
        try
        {
            this.timer_Refresh.Enabled = false;
            //Refresh all of the Volume Level controls
            this.RefreshVolumeLevels();
            this.timer_Refresh.Enabled = true;
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }
    #endregion

    #region Refresh Interval
    protected void Msb_RefreshInterval_TextChanged(object? sender, EventArgs e)
    {
        try
        {
            if (string.IsNullOrEmpty(this.msb_RefreshInterval.Text) || this.msb_RefreshInterval.Text == "0")
                this.msb_RefreshInterval.Text = "1";

            this.timer_Refresh.Interval = Math.Max(1, int.Parse(this.msb_RefreshInterval.Text));
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }
    #endregion

    #region InputValidation
    protected void Msb_RefreshInterval_KeyPress(object? sender, KeyPressEventArgs e)
    {
        InputValidator.Validate_IsNumeric_NonNegative(e);
        this.msb_RefreshInterval.Text = InputValidator.LimitTo_ReasonableSizedNumber(this.msb_RefreshInterval.Text);
    }
    #endregion

    #region Reset
    protected void btn_ResetClip_Click(object? sender, EventArgs e)
    {
        try
        {
            foreach (var item in this.VolControlList)
                item.Reset_ClipIndicator();
            this.timer_Refresh.Enabled = true;
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }
    #endregion

    #region Resize
    protected void Resize_Handler(object? sender, EventArgs e)
    {
        try
        {
            this.RelocateControls();
            this.timer_Refresh.Enabled = true;
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }
    #endregion

    #endregion

    #region Public Functions
    public void CreateStreamVolumeLevelControl(DSP_Stream stream)
    {
        this.SafeInvoke(() =>
        {
            try
            {
                var VolControl = new BTH_VolumeLevelControl();
                this.VolControlList.Add(VolControl);
                VolControl.Get_btn_View.Text = "[" + (this.VolControlList.IndexOf(VolControl) + 1).ToString() + "] View";
                VolControl.Set_StreamInfo(stream);
                this.PlaceControl(this.VolControlList.Count() - 1, VolControl);
                this.pnl_Main.Controls.Add(VolControl);
            }
            catch (Exception ex)
            {
                this.Error(ex);
            }
        });
    }

    public void RemoveStreamVolumeLevelControl(DSP_Stream stream)
    {
        this.SafeInvoke(() =>
        {
            try
            {
                BTH_VolumeLevelControl? FoundControl = null;
                foreach (var item in this.VolControlList)
                    if (item.Stream == stream)
                    {
                        FoundControl = item;
                        break;
                    }

                if (FoundControl != null)
                {
                    this.pnl_Main.Controls.Remove(FoundControl);
                    _ = this.VolControlList.Remove(FoundControl);
                }
            }
            catch (Exception ex)
            {
                this.Error(ex);
            }
        });
    }

    public void RefreshStreamInfo(DSP_Stream stream)
    {
        this.SafeInvoke(() =>
        {
            try
            {
                foreach (var item in this.VolControlList)
                    if (item.Stream == stream)
                    {
                        item.Set_StreamInfo(stream);
                        break;
                    }
            }
            catch (Exception ex)
            {
                this.Error(ex);
            }
        });
    }

    #endregion

    #region Protected Functions
    protected void Set_Pause_States()
    {
        this.timer_Refresh.Enabled = !this.Pause_CHK.Checked;
        foreach (var item in this.VolControlList)
            item.Get_timer_Refresh.Enabled = !this.Pause_CHK.Checked;
    }
    protected void RelocateControls()
    {
        for (var i = 0; i < this.VolControlList.Count; i++)
            this.PlaceControl(i, this.VolControlList[i]);
    }

    protected void RefreshVolumeLevels()
    {
        foreach (var item in this.VolControlList)
            item.ComputeLevels();
    }

    protected void PlaceControl(int controlIndex, Control input)
    {
        var ElementsPerWidth = (int)Math.Max(1, Math.Floor((double)this.Width / input.Width));
        var x = input.Width * (controlIndex % ElementsPerWidth);
        var y = controlIndex / ElementsPerWidth * (input.Height + 1);
        input.Location = new Point(-this.pnl_Main.HorizontalScroll.Value + x, -this.pnl_Main.VerticalScroll.Value + y);
    }
    #endregion

    #region Error Handling
    protected void Error(Exception ex)
    {
        Debug.Error(ex);
    }
    #endregion
}