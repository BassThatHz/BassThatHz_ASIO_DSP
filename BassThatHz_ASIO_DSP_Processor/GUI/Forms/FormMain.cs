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
        var SelectedTab = this.tabControl1.SelectedTab;
        if (SelectedTab != null && SelectedTab.Controls.Count > 0 &&
            SelectedTab.Controls[0] is IHasFocus FocusItem)
        {
            FocusItem.HasFocus();
        }
    }
    #endregion

    #region Public Properties
    public TabControl Get_tabControl1 => this.tabControl1;
    public ctl_DSPConfigPage Get_DSPConfigPage1 => this.ctl_DSPConfigPage1;
    #endregion

    #region Public Functions
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public void ApplyXMLConfig(string xml)
    {
        if (!string.IsNullOrEmpty(xml))
        {
            this.SafeInvoke(() =>
            {
                Program.DSP_Info = new ExtendedXmlSerialization.ExtendedXmlSerializer().Deserialize<DSP_Info>(xml);

                //Fixes Legacy Channel Index Mappings for backwards support
                CommonFunctions.FixLegacyChannelIndexMappings();

                Application.DoEvents();
                this.LoadConfigRefresh();
            });
        }
    }
    #endregion

    #region Event Handlers
    protected void FormMain_Load(object? sender, EventArgs e)
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    protected void FormMain_Shown(object? sender, EventArgs e)
    {
        try
        {
            var FilePath = string.Concat(AppContext.BaseDirectory, "DSP.xml");
            if (File.Exists(FilePath))
            {
                var XML = File.ReadAllText(FilePath);
                XML = CommonFunctions.RemoveDeprecatedXMLTags(XML);
                var temp = new ExtendedXmlSerialization.ExtendedXmlSerializer().Deserialize<DSP_Info>(XML);
                //Perform StartUp Delay (this gives the ASIO driver time to load for auto-startup appliances)
                if (temp.StartUpDelay > 0)
                {
                    this.Enabled = false;
                    System.Threading.Thread.Sleep(temp.StartUpDelay);
                    this.Enabled = true;
                }

                //Perform startup using DSP file settings
                this.tabControl1.SelectedTab = this.InputsConfigPage;
                Application.DoEvents();
                this.ApplyXMLConfig(XML);

                //Place filename into the General Config tab text
                var GeneralConfigTab = this.tabControl1.TabPages[0];
                if (GeneralConfigTab != null)
                    GeneralConfigTab.Text = "General Config (DSP.xml)";
            }
            else
            { 
                this.tabControl1.SelectedTab = this.InputsConfigPage;
            }                
        }
        catch (AsioException ex)
        {
            _ = ex;
            _ = MessageBox.Show("Could not successfully load the DSP config file. " + ex.Message, "ASIO init error");
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
        Application.DoEvents();
        this.ctl_GeneralConfigPage1.LoadConfigRefresh();
        Application.DoEvents();
        this.ctl_BusesPage1.LoadConfigRefresh();
        Application.DoEvents();
        this.ctl_DSPConfigPage1.LoadConfigRefresh();
        //Application.DoEvents();
        //this.ctl_MonitorPage1.LoadConfigRefresh();
        Application.DoEvents();
        this.ctl_OutputsConfigPage1.LoadConfigRefresh();
        Application.DoEvents();
        this.ctl_InputsConfigPage1.LoadConfigRefresh();
        Application.DoEvents();
        this.ctl_StatsPage1.LoadConfigRefresh();
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