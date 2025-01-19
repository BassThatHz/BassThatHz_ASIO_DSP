#nullable enable

namespace BassThatHz_ASIO_DSP_Processor
{
    using BassThatHz_ASIO_DSP_Processor.GUI;
    using BassThatHz_ASIO_DSP_Processor.GUI.Controls;
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;
    using System.Windows.Forms;

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
        List<BTH_VolumeLevel> VolControlList = new();
        #endregion

        #region Constructor and MapEventHandlers
        public FormMonitoring()
        {
            InitializeComponent();

            this.MapEventHandlers();
        }

        public void MapEventHandlers()
        {
            this.Activated += Activated_Handler;
            this.Deactivate += Deactivate_Handler;
            this.Resize += Resize_Handler;
            this.msb_RefreshInterval.TextChanged += Msb_RefreshInterval_TextChanged;

            this.msb_RefreshInterval.KeyPress += Msb_RefreshInterval_KeyPress;
        }
        #endregion

        #region Event Handlers

        #region InputValidation
        private void Msb_RefreshInterval_KeyPress(object? sender, KeyPressEventArgs e)
        {
            InputValidator.Validate_IsNumeric_NonNegative(e);
            this.msb_RefreshInterval.Text = InputValidator.LimitTo_ReasonableSizedNumber(this.msb_RefreshInterval.Text);
        }
        #endregion

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

        protected void Deactivate_Handler(object? sender, EventArgs e)
        {
            try
            {
                this.timer_Refresh.Enabled = false;
                foreach (var item in this.VolControlList)
                    item.timer_Refresh.Enabled = false;
            }
            catch (Exception ex)
            {
                this.Error(ex);
            }
        }

        protected void Activated_Handler(object? sender, EventArgs e)
        {
            try
            {
                this.timer_Refresh.Enabled = true;
                foreach (var item in this.VolControlList)
                    item.timer_Refresh.Enabled = true;
            }
            catch (Exception ex)
            {
                this.Error(ex);
            }
        }

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

        #region Public Functions
        public void CreateStreamVolumeLevelControl(DSP_Stream stream)
        {
            this.SafeInvoke(() =>
            {
                try
                {
                    var VolControl = new BTH_VolumeLevel();
                    this.VolControlList.Add(VolControl);
                    VolControl.btn_View.Text = "[" + (this.VolControlList.IndexOf(VolControl) + 1).ToString() + "] View";
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
                    BTH_VolumeLevel? FoundControl = null;
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
}