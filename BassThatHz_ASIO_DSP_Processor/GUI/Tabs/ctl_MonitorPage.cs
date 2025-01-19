#nullable enable

namespace BassThatHz_ASIO_DSP_Processor.GUI.Tabs
{
    using System;
    using System.Threading;
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
    public partial class ctl_MonitorPage : UserControl
    {
        #region Variables
        public Thread? _Form_Monitoring_Thread;

        #endregion

        #region Constructor
        public ctl_MonitorPage()
        {
            InitializeComponent();
        }
        #endregion

        #region LoadConfigRefresh
        public void LoadConfigRefresh()
        {

        }
        #endregion

        #region Event Handlers

        protected void btn_Monitor_Click(object? sender, EventArgs e)
        {
            if (_Form_Monitoring_Thread == null || !_Form_Monitoring_Thread.IsAlive)
            {
                Program.Form_Monitoring = new();

                foreach (var item in Program.DSP_Info.Streams)
                    Program.Form_Monitoring.CreateStreamVolumeLevelControl(item);

                _Form_Monitoring_Thread = new(() =>
                                                Application.Run(Program.Form_Monitoring)
                                              );

                _ = _Form_Monitoring_Thread.TrySetApartmentState(ApartmentState.STA);
                _Form_Monitoring_Thread.Start();
            }
        }

        protected void ctl_MonitorPage_Load(object? sender, EventArgs e)
        {
        }
        #endregion

    }
}