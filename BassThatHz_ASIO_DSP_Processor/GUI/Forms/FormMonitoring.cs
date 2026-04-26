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

public partial class FormMonitoring : Form
{
    #region Variables
    // Use a readonly list to avoid accidental reassignments and slightly reduce overhead.
    private readonly List<BTH_VolumeLevelControl> VolControlList = new();
    private bool IsClosing = false;
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
        this.FormClosing += FormMonitoring_FormClosing;
    }

    #endregion

    #region Event Handlers

    #region Closing
    protected void FormMonitoring_FormClosing(object? sender, FormClosingEventArgs e)
    {
        try
        {
            this.IsClosing = true;
            //Gracefully stop
            this.Resize -= Resize_Handler;
            this.Shown -= FormMonitoring_Shown;
            this.FormClosing -= FormMonitoring_FormClosing;
            this.msb_RefreshInterval.TextChanged -= Msb_RefreshInterval_TextChanged;
            this.msb_RefreshInterval.KeyPress -= Msb_RefreshInterval_KeyPress;
            this.timer_Refresh.Stop();
            this.timer_Refresh.Enabled = false;

            this.Set_Pause_States();
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }
    #endregion

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
    protected void Refresh_Timer_Tick(object? sender, EventArgs e)
    {
        try
        {
            if (ShouldExit())
                return;

            // Stop timer to avoid reentrancy while updating controls.
            this.timer_Refresh.Stop();

            // Refresh all of the Volume Level controls
            this.RefreshVolumeLevels();

            if (ShouldExit())
                return;
            this.timer_Refresh.Start();
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

            // Use TryParse to avoid exceptions from transient invalid input and clamp to reasonable range.
            if (int.TryParse(this.msb_RefreshInterval.Text, out var val))
                this.timer_Refresh.Interval = Math.Max(1, val);
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
    protected void ResetClipBTN_Click(object? sender, EventArgs e)
    {
        try
        {
            if (ShouldExit())
                return;

            // Use indexed loop to avoid any enumerator overhead in case the collection type changes.
            var list = this.VolControlList;
            for (var i = 0; i < list.Count; i++)
            {
                if (ShouldExit())
                    return;
                list[i].Reset_ClipIndicator();
            }

            if (ShouldExit())
                return;
            this.timer_Refresh.Start();
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
            if (ShouldExit())
                return;
            this.RelocateControls();
            if (ShouldExit())
                return;
            this.timer_Refresh.Start();
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
                if (ShouldExit())
                    return;

                var VolControl = new BTH_VolumeLevelControl();

                // Compute new index without scanning the list (avoid IndexOf which is O(n)).
                var newIndex = this.VolControlList.Count;
                this.VolControlList.Add(VolControl);
                VolControl.Get_btn_View.Text = "[" + (newIndex + 1).ToString() + "] View";
                VolControl.Set_StreamInfo(stream);

                // Place control based on the index we just computed.
                this.PlaceControl(newIndex, VolControl);
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
                if (ShouldExit())
                    return;

                // Find index using a simple loop to avoid allocating a LINQ iterator.
                var list = this.VolControlList;
                var foundIndex = -1;
                for (var i = 0; i < list.Count; i++)
                {
                    if (list[i].Stream == stream)
                    {
                        foundIndex = i;
                        break;
                    }
                }

                if (foundIndex >= 0)
                {
                    if (ShouldExit())
                        return;
                    var FoundControl = list[foundIndex];
                    this.pnl_Main.Controls.Remove(FoundControl);
                    list.RemoveAt(foundIndex);
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
                var list = this.VolControlList;
                for (var i = 0; i < list.Count; i++)
                {
                    var item = list[i];
                    if (item.Stream == stream)
                    {
                        if (ShouldExit())
                            return;
                        item.Set_StreamInfo(stream);
                        break;
                    }
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
    // Helper to centralize shutdown checks to avoid repeated property lookups.
    private bool ShouldExit()
    {
        return this.Disposing || this.IsDisposed || this.IsClosing;
    }
    protected void Set_Pause_States()
    {
        var enabled = !this.Pause_CHK.Checked;
        if (enabled)
            this.timer_Refresh.Start();
        else
            this.timer_Refresh.Stop();

        var list = this.VolControlList;
        for (var i = 0; i < list.Count; i++)
        {
            if (this.IsDisposed)
                return;
            list[i].Get_timer_Refresh.Enabled = enabled;
        }
    }
    protected void RelocateControls()
    {
        var list = this.VolControlList;
        for (var i = 0; i < list.Count; i++)
        {
            if (ShouldExit())
                return;
            this.PlaceControl(i, list[i]);
        }
    }

    protected void RefreshVolumeLevels()
    {
        var list = this.VolControlList;
        for (var i = 0; i < list.Count; i++)
        {
            if (ShouldExit())
                return;
            list[i].ComputeLevels();
        }
    }

    protected void PlaceControl(int controlIndex, Control input)
    {
        if (this.Disposing || this.IsDisposed || this.IsClosing)
            return;

        // Protect against zero width on the control to avoid divide-by-zero.
        var controlW = Math.Max(1, input.Width);
        var ElementsPerWidth = Math.Max(1, this.Width / controlW);
        var x = controlW * (controlIndex % ElementsPerWidth);
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