#nullable enable

namespace BassThatHz_ASIO_DSP_Processor;

using BassThatHz_ASIO_DSP_Processor.GUI.Tabs;

#region Usings
using NAudio.Wave.Asio;
using System;
using System.IO;
using System.Runtime.CompilerServices;
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
public partial class FormMain : Form
{
    #region Constructor
    public FormMain()
    {
        InitializeComponent();

        this.Shown += FormMain_Shown;

        this.tabControl1.SelectedIndexChanged += TabControl1_SelectedIndexChanged;
    }

    protected void TabControl1_SelectedIndexChanged(object? sender, EventArgs e)
    {
        var selectedTab = this.tabControl1.SelectedTab;
        if (selectedTab != null && selectedTab.Controls.Count > 0 &&
            selectedTab.Controls[0] is IHasFocus focusItem)
        {
            focusItem.HasFocus();
        }
    }
    #endregion

    #region Variables
    /// <summary>
    /// Set only by the system tray "Close" command (and any other genuine shutdown path).
    /// While false, a user-initiated close hides the app to the system tray instead of exiting,
    /// which keeps the ASIO engine running.
    /// </summary>
    protected bool IsExitingToDesktop;

    /// <summary>
    /// The WindowState the form had before it was hidden to the tray, so that "Open"
    /// restores a maximized window as maximized rather than forcing it to Normal.
    /// </summary>
    protected FormWindowState PreTrayWindowState = FormWindowState.Maximized;

    /// <summary>
    /// The tray balloon is only shown the first time, purely so the user learns that
    /// Close did not terminate the DSP engine.
    /// </summary>
    protected bool HasShownTrayBalloon;
    #endregion

    #region Public Properties
    public TabControl Get_tabControl1 => this.tabControl1;
    public ctl_DSPConfigPage Get_DSPConfigPage1 => this.ctl_DSPConfigPage1;
    public NotifyIcon Get_SysTrayIcon => this.SysTrayIcon;
    public ContextMenuStrip Get_SysTrayMenu => this.SysTrayMenu;
    #endregion

    #region System Tray
    /// <summary>
    /// Restores the window from the system tray and brings it to the foreground.
    /// </summary>
    public void RestoreFromSysTray()
    {
        this.SafeInvoke(() =>
        {
            this.Show();
            this.WindowState = this.PreTrayWindowState;
            this.Activate();
        });
    }

    /// <summary>
    /// Hides the window to the system tray. The ASIO engine and all processing keep running.
    /// </summary>
    public void HideToSysTray()
    {
        this.SafeInvoke(() =>
        {
            if (this.WindowState != FormWindowState.Minimized)
                this.PreTrayWindowState = this.WindowState;

            this.SysTrayIcon.Visible = true;
            this.Hide();

            if (!this.HasShownTrayBalloon)
            {
                this.HasShownTrayBalloon = true;
                this.SysTrayIcon.ShowBalloonTip(3000, "Still running",
                    "The DSP engine is still processing audio. Right-click this icon for Open/Close.",
                    ToolTipIcon.Info);
            }
        });
    }

    /// <summary>
    /// Performs a real application shutdown, bypassing the hide-to-tray behaviour.
    /// </summary>
    public void ExitToDesktop()
    {
        this.SafeInvoke(() =>
        {
            this.IsExitingToDesktop = true;
            this.Close();
        });
    }
    #endregion

    #region Public Functions
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public void ApplyXMLConfig(string xml)
    {
        if (!string.IsNullOrEmpty(xml))
        {
            var serializer = new ExtendedXmlSerialization.ExtendedXmlSerializer();
            var info = serializer.Deserialize<DSP_Info>(xml);
            this.ApplyXMLConfig(info);
        }
    }

    // Overload that accepts an already-deserialized DSP_Info to avoid redundant deserialization
    public void ApplyXMLConfig(DSP_Info info)
    {
        if (info is null)
            return;

        this.SafeInvoke(() =>
        {
            Program.DSP_Info = info;

            // Fixes Legacy Channel Index Mappings for backwards support
            CommonFunctions.FixLegacyChannelIndexMappings();

            // Refresh configuration UI once after all pages have been updated
            this.LoadConfigRefresh();
        });
    }
    #endregion

    #region Event Handlers
    protected void FormMain_Load(object? sender, EventArgs e)
    {
        // Deliberately not set in the designer: the tray icon is only registered with the shell
        // once the app is actually running, so merely constructing a FormMain (as the test suite
        // does) does not leave an orphaned icon in the notification area.
        this.SysTrayIcon.Visible = true;
    }

    /// <summary>
    /// The window close button hides the app to the system tray instead of exiting, so the
    /// ASIO engine keeps processing. Only the tray "Close" command, Application.Exit or a
    /// Windows shutdown performs a real exit.
    /// </summary>
    protected void FormMain_FormClosing(object? sender, FormClosingEventArgs e)
    {
        try
        {
            if (!this.IsExitingToDesktop && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.HideToSysTray();
                return;
            }

            // Real shutdown: drop the icon now rather than waiting for Dispose, otherwise a
            // stale icon can linger in the notification area until the user hovers over it.
            this.SysTrayIcon.Visible = false;
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }

    protected void SysTrayIcon_DoubleClick(object? sender, EventArgs e)
    {
        this.RestoreFromSysTray();
    }

    protected void SysTrayMenu_Open_Click(object? sender, EventArgs e)
    {
        this.RestoreFromSysTray();
    }

    protected void SysTrayMenu_Close_Click(object? sender, EventArgs e)
    {
        this.ExitToDesktop();
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    protected void FormMain_Shown(object? sender, EventArgs e)
    {
        try
        {
            var filePath = Path.Combine(AppContext.BaseDirectory, "DSP.xml");
            if (File.Exists(filePath))
            {
                var xml = File.ReadAllText(filePath);
                xml = CommonFunctions.RemoveDeprecatedXMLInputTags(xml);
                var temp = new ExtendedXmlSerialization.ExtendedXmlSerializer().Deserialize<DSP_Info>(xml);

                // Perform StartUp Delay (gives the ASIO driver time to load for auto-startup appliances)
                if (temp.StartUpDelay > 0)
                {
                    //DEFECT FIX: without the finally, a ThreadInterruptedException during the sleep
                    //left the main form permanently disabled with no way for the user to recover.
                    this.Enabled = false;
                    try
                    {
                        System.Threading.Thread.Sleep(temp.StartUpDelay);
                    }
                    finally
                    {
                        this.Enabled = true;
                    }
                }

                // Perform startup using DSP file settings
                this.tabControl1.SelectedTab = this.InputsConfigPage;
                // Apply already-deserialized config to avoid re-parsing the XML
                this.ApplyXMLConfig(temp);

                // Place filename into the General Config tab text
                var generalConfigTab = this.tabControl1.TabPages[0];
                if (generalConfigTab != null)
                    generalConfigTab.Text = "General Config (DSP.xml)";
            }
            else
            {
                this.tabControl1.SelectedTab = this.InputsConfigPage;
            }
        }
        catch (AsioException ex)
        {
            _ = ex;
            _ = Debug.ShowMessage("Could not successfully load the DSP config file. " + ex.Message, "ASIO init error");
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }
    #endregion

    #region Protected Functions
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    protected void LoadConfigRefresh()
    {
        // Refresh all configuration pages once without repeated Application.DoEvents calls
        // to minimize UI thread churn and allocations.
        this.ctl_GeneralConfigPage1.LoadConfigRefresh();
        this.ctl_BusesPage1.LoadConfigRefresh();
        this.ctl_DSPConfigPage1.LoadConfigRefresh();
        // this.ctl_MonitorPage1.LoadConfigRefresh();
        this.ctl_OutputsConfigPage1.LoadConfigRefresh();
        this.ctl_InputsConfigPage1.LoadConfigRefresh();
        this.ctl_StatsPage1.LoadConfigRefresh();

        // Single UI pulse after bulk updates
        Application.DoEvents();
    }
    #endregion

    #region Error Handling
    protected void Error(Exception ex)
    {
        Debug.Error(ex);
    }
    #endregion
}