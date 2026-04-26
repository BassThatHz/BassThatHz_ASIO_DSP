#nullable enable

namespace BassThatHz_ASIO_DSP_Processor.GUI.Tabs;

#region Usings
using System;
using System.Threading;
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
public partial class ctl_MonitorPage : UserControl
{
    #region Variables
    // Locks to protect thread/form lifecycle access from multiple threads
    private readonly object monitorLock = new();
    private readonly object alignLock = new();

    // Keep thread references volatile to ensure latest value is observed across threads
    private volatile Thread? Form_Monitoring_Thread;
    private volatile Thread? Form_Align_Thread;
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
    protected void ctl_MonitorPage_Load(object? sender, EventArgs e)
    {
    }

    protected void btn_Monitor_Click(object? sender, EventArgs e)
    {
        lock (monitorLock)
        {
            if (this.Form_Monitoring_Thread == null || !this.Form_Monitoring_Thread.IsAlive)
            {
                Program.Form_Monitoring = new();

                // populate controls for the monitoring form
                foreach (var item in Program.DSP_Info.Streams)
                    Program.Form_Monitoring.CreateStreamVolumeLevelControl(item);

                // ensure we clear references when the form closes to avoid leaks
                Program.Form_Monitoring.FormClosed += (s, ev) =>
                {
                    lock (monitorLock)
                    {
                        this.Form_Monitoring_Thread = null;
                    }

                    try { Program.Form_Monitoring = null; } catch { }
                };

                this.Form_Monitoring_Thread = new(() => Application.Run(Program.Form_Monitoring));
                this.Form_Monitoring_Thread.IsBackground = true;
                _ = this.Form_Monitoring_Thread.TrySetApartmentState(ApartmentState.STA);
                this.Form_Monitoring_Thread.Start();
            }
        }
    }

    protected void btn_Align_Click(object sender, EventArgs e)
    {
        lock (alignLock)
        {
            if (this.Form_Align_Thread == null || !this.Form_Align_Thread.IsAlive)
            {
                Program.Form_Align = new();

                Program.Form_Align.FormClosed += (s, ev) =>
                {
                    lock (alignLock)
                    {
                        this.Form_Align_Thread = null;
                    }

                    try { Program.Form_Align = null; } catch { }
                };

                this.Form_Align_Thread = new(() => Application.Run(Program.Form_Align));
                this.Form_Align_Thread.IsBackground = true;
                _ = this.Form_Align_Thread.TrySetApartmentState(ApartmentState.STA);
                this.Form_Align_Thread.Start();
            }
        }
    }   

    #endregion    
}