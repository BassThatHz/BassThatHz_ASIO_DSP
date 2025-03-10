#nullable enable

namespace BassThatHz_ASIO_DSP_Processor.GUI.Tabs;

#region Usings
using ExtendedXmlSerialization;
using Networking;
using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using Diagnostics = System.Diagnostics;
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
public partial class ctl_GeneralConfigPage : UserControl
{
    #region Variables
    protected NetworkConfigAPI? NetworkConfigAPI;
    #endregion

    #region Constructor
    public ctl_GeneralConfigPage()
    {
        InitializeComponent();
        this.maskStartUpDelay.TextChanged += this.MaskStartUpDelay_TextChanged;
        this.txt_NetworkConfigAPI_Host.TextChanged += txt_NetworkConfigAPI_Host_TextChanged;
        this.maskNetworkConfig_Port.TextChanged += maskNetworkConfig_Port_TextChanged;
    }
    #endregion

    #region Event Handlers
    protected void ctl_GeneralConfigPage_Load(object? sender, EventArgs e)
    {
        try
        {
            //Select All Cores
            if (this.lstProcesAffinty.Items.Count > 0)
                this.lstProcesAffinty.SelectedIndex = 0;

            //Add Process Priority List to GUI
            var ProcessPriorityClassList = Enum.GetValues(typeof(Diagnostics.ProcessPriorityClass))
                                .Cast<Diagnostics.ProcessPriorityClass>();

            foreach (var item in ProcessPriorityClassList)
            {
                _ = this.cboProcessPriority.Items.Add(item);
                if (item == Diagnostics.ProcessPriorityClass.High)
                    this.cboProcessPriority.SelectedItem = item;
            }
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }

    protected void cboProcessPriority_SelectedIndexChanged(object? sender, EventArgs e)
    {
        try
        {
            using var p = Diagnostics.Process.GetCurrentProcess();
            if (this.cboProcessPriority.SelectedItem is System.Diagnostics.ProcessPriorityClass selectedPriority)
            {
                p.PriorityClass = selectedPriority;
                Program.DSP_Info.ProcessPriority = p.PriorityClass;
            }
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }

    protected void chkThreading_CheckedChanged(object? sender, EventArgs e)
    {
        try
        {
            Program.DSP_Info.IsMultiThreadingEnabled = this.chkThreading.Checked;
            Program.ASIO.IsMultiThreadingEnabled = this.chkThreading.Checked;
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }

    protected void chkBackgroundThread_CheckedChanged(object? sender, EventArgs e)
    {
        try
        {
            Program.DSP_Info.IsBackgroundThreadEnabled = this.chkBackgroundThread.Checked;
            Program.ASIO.IsMT_BackgroundThreadEnabled = this.chkBackgroundThread.Checked;
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }

    protected void chkAutoStartDSP_CheckedChanged(object? sender, EventArgs e)
    {
        try
        {
            Program.DSP_Info.AutoStartDSP = this.chkAutoStartDSP.Checked;
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }

    protected void btnSaveConfig_Click(object? sender, EventArgs e)
    {
        try
        {
            this.saveFileDialog1.InitialDirectory = AppContext.BaseDirectory;
            if (this.saveFileDialog1.ShowDialog(this) == DialogResult.OK)
            {
                var xml = new ExtendedXmlSerializer().Serialize(Program.DSP_Info);
                xml = CommonFunctions.RemoveDeprecatedXMLOutputTags(xml);
                File.WriteAllText(this.saveFileDialog1.FileName, xml);
            }
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }

    protected void btnLoadConfig_Click(object? sender, EventArgs e)
    {
        try
        {
            this.openFileDialog1.InitialDirectory = AppContext.BaseDirectory;
            if (this.openFileDialog1.ShowDialog(this) == DialogResult.OK)
            {
                Program.ASIO.Stop();
                var XML = File.ReadAllText(this.openFileDialog1.FileName);
                XML = CommonFunctions.RemoveDeprecatedXMLInputTags(XML);
                Program.Form_Main?.ApplyXMLConfig(XML);

                //Place filename into the General Config tab text
                var GeneralConfigTab = Program.Form_Main?.Get_tabControl1.TabPages[0];
                if (GeneralConfigTab != null)
                    GeneralConfigTab.Text = "General Config (" + this.openFileDialog1.SafeFileName + ")";
            }
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }

    protected void MaskStartUpDelay_TextChanged(object? sender, System.EventArgs e)
    {
        try
        {
            Program.DSP_Info.StartUpDelay = Math.Max(0, int.Parse(this.maskStartUpDelay.Text));
        }
        catch (Exception ex)
        {
            _ = ex;
        }
    }

    protected async void chkNetworkConfigAPI_CheckedChanged(object sender, EventArgs e)
    {
        try
        {
            //Initialize NetworkConfigAPI as-needed
            this.NetworkConfigAPI ??= new();

            Program.DSP_Info.NetworkConfigAPI_Enabled = this.chkNetworkConfigAPI.Checked;
            if (this.chkNetworkConfigAPI.Checked) //Start the Network API
            {
                this.NetworkConfigListener_MapNetworkCallbacks();

                // Start the network config listener
                await this.StartNetworkConfigListener();
            }
            else //Stop the Network API
            {
                this.NetworkConfigListener_Cancel();
            }
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }

    protected void maskNetworkConfig_Port_TextChanged(object? sender, EventArgs e)
    {
        Program.DSP_Info.NetworkConfigAPI_Port = int.Parse(this.maskNetworkConfig_Port.Text);
    }

    protected void txt_NetworkConfigAPI_Host_TextChanged(object? sender, EventArgs e)
    {
        Program.DSP_Info.NetworkConfigAPI_Host = this.txt_NetworkConfigAPI_Host.Text;
    }

    #endregion

    #region Public LoadConfigRefresh Function
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public void LoadConfigRefresh()
    {
        this.chkNetworkConfigAPI.Checked = Program.DSP_Info.NetworkConfigAPI_Enabled;
        this.txt_NetworkConfigAPI_Host.Text = Program.DSP_Info.NetworkConfigAPI_Host;
        this.maskNetworkConfig_Port.Text = Program.DSP_Info.NetworkConfigAPI_Port.ToString();
        this.chkThreading.Checked = Program.DSP_Info.IsMultiThreadingEnabled;
        this.chkBackgroundThread.Checked = Program.DSP_Info.IsBackgroundThreadEnabled;
        this.chkAutoStartDSP.Checked = Program.DSP_Info.AutoStartDSP;
        this.maskStartUpDelay.Text = Program.DSP_Info.StartUpDelay.ToString();

        foreach (var item in this.cboProcessPriority.Items)
        {
            if (Program.DSP_Info.ProcessPriority == (Diagnostics.ProcessPriorityClass)item)
            {
                this.cboProcessPriority.SelectedItem = item;
                break;
            }
        }
        this.chkThreading_CheckedChanged(this, EventArgs.Empty);
        this.chkBackgroundThread_CheckedChanged(this, EventArgs.Empty);
        this.cboProcessPriority_SelectedIndexChanged(this, EventArgs.Empty);
    }
    #endregion

    #region Protected Functions

    #region NetworkConfigAPI Functions
    protected void NetworkConfigListener_MapNetworkCallbacks()
    {
        if (this.NetworkConfigAPI != null)
        {
            //Wire up the network callbacks
            this.NetworkConfigAPI.GetResponseString ??= this.NetworkConfigListener_GetResponseString;
            this.NetworkConfigAPI.OnDataStringPosted ??= this.NetworkConfigListener_OnDataStringPosted;
        }
    }

    protected string NetworkConfigListener_GetResponseString()
    {
        //Returns the XML serialization of the Config settings for the Network API response
        return new ExtendedXmlSerializer().Serialize(Program.DSP_Info);
    }

    protected string NetworkConfigListener_OnDataStringPosted(string data)
    {
        try
        {
            //Try validating the post data for xml configuration
            var ParseResult = CommonFunctions.TryParseXml(data);
            if (ParseResult == "Success") //If successfully parsed as XML Document
            {
                var XML = CommonFunctions.RemoveDeprecatedXMLOutputTags(data);

                //Try Deserializing the XML string, this will throw an error if it fails
                //A Try Deserialize XML if you will
                _ = new ExtendedXmlSerializer().Deserialize<DSP_Info>(XML);

                //No Errors occured so we are good to "actually" load the
                //configuration XML that was posted via the network api

                //Do the call in parellel so that the
                //NetworkAPI can go back to listening for more requests ASAP
                Task.Run(() =>
                    Program.Form_Main?.ApplyXMLConfig(XML)
                    );

                return "Success";
            }
            return ParseResult; //TryParseXml failed, send TryParseXml error to Network API
        }
        catch (Exception ex)
        {
            //Deserialize XML failed, send response error to Network API
            var ErrorResponse = ex.Message + ex.InnerException?.Message + ex.StackTrace;
            return ErrorResponse;
        }
    }

    protected async Task StartNetworkConfigListener()
    {
        if (this.NetworkConfigAPI != null)
        {
            // Start the network config listener
            string hostname = Program.DSP_Info.NetworkConfigAPI_Host;
            string port = Program.DSP_Info.NetworkConfigAPI_Port.ToString();

            if (string.IsNullOrEmpty(hostname) || string.IsNullOrEmpty(port))
                return;

            //This function validates if it is already running, so no need to check it
            await NetworkConfigAPI.NetworkConfigAPI_Listener(hostname, port);
        }
    }

    protected void NetworkConfigListener_Cancel()
    {
        // Cancel the network config listener
        this.NetworkConfigAPI?.CancellationTokenSource?.Cancel();
    }
    #endregion

    #endregion

    #region Error Handling
    protected void Error(Exception ex)
    {
        Debug.Error(ex);
    }
    #endregion
}