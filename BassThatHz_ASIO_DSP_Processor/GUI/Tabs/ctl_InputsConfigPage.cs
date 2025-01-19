#nullable enable

namespace BassThatHz_ASIO_DSP_Processor.GUI.Tabs
{
    using NAudio.Wave.Asio;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;
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
    public partial class ctl_InputsConfigPage : UserControl
    {
        #region Constructor
        public ctl_InputsConfigPage()
        {
            InitializeComponent();
        }
        #endregion

        #region LoadConfigRefresh
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public void LoadConfigRefresh()
        {
            this.volMaster.Volume = Program.DSP_Info.InMasterVolume;
            Program.ASIO.InputMasterVolume = this.volMaster.Volume;
            this.Select_Existing_Device();
            this.Select_Existing_SampleRate();
        }
        #endregion

        #region Event Handlers
        protected void ctl_InputsConfigPage_Load(object? sender, EventArgs e)
        {
            try
            {
                this.volMaster.VolumeChanged += VolMaster_VolumeChanged;
                this.Populate_ASIO_Device_Names();

                //Select the first Device, if necessary
                if (this.cboDevices.Items.Count >= 1 && this.cboDevices.SelectedIndex == -1)
                    this.cboDevices.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                this.Error(ex);
            }
        }

        protected void cboDevices_SelectedIndexChanged(object? sender, EventArgs e)
        {
            try
            {
                string? InputDevice = this.cboDevices.SelectedItem?.ToString();
                if (!string.IsNullOrEmpty(InputDevice))
                {
                    Program.DSP_Info.ASIO_InputDevice = InputDevice;
                }
                
                this.DisplayBufferSize();
                this.DisplayChannelList();
                this.DisplaySupportedSampleRates();
            }
            catch (AsioException ex)
            {
                _ = ex;
                _ = MessageBox.Show("Cannot init ASIO device or no ASIO drivers detected. " + ex.Message, "ASIO Error");
            }
            catch (Exception ex)
            {
                this.Error(ex);
            }
        }

        protected void cboSampleRate_SelectedIndexChanged(object? sender, EventArgs e)
        {
            try
            {
                string? SampleRateString = this.cboSampleRate.SelectedItem?.ToString();
                if (!String.IsNullOrEmpty(SampleRateString) && int.TryParse(SampleRateString, out int SampleRate))
                {
                    Program.ASIO?.Stop();
                    Program.DSP_Info.InSampleRate = SampleRate;
                    SampleRateChangeNotifier.NotifySampleRateChange(SampleRate);
                }
            }
            catch (Exception ex)
            {
                this.Error(ex);
            }
        }

        protected void btnASIOControlPanel_Click(object? sender, EventArgs e)
        {
            try
            {
                string? DeviceName = this.cboDevices.SelectedItem?.ToString();
                if (!String.IsNullOrEmpty(DeviceName))
                {
                    Program.ASIO.Show_ControlPanel(DeviceName);
                }
            }
            catch (Exception ex)
            {
                this.Error(ex);
            }
        }

        protected void VolMaster_VolumeChanged(object? sender, EventArgs e)
        {
            try
            {
                Program.ASIO.InputMasterVolume = this.volMaster.Volume;
                Program.DSP_Info.InMasterVolume = this.volMaster.Volume;
            }
            catch (Exception ex)
            {
                this.Error(ex);
            }
        }

        protected void cboBufferSize_SelectedIndexChanged(object? sender, EventArgs e)
        {
            //try
            //{
            //    this.cboBufferSize.SelectedIndex = 1;
            //}
            //catch (Exception ex)
            //{
            //    this.Error(ex);
            //}
        }
        #endregion

        #region Protected Functions
        protected void Populate_ASIO_Device_Names()
        {
            var DriverNames = Program.ASIO.GetDriverNames();
            if (DriverNames != null && DriverNames.Count() > 0) 
                this.cboDevices.Items.AddRange(DriverNames);
            else
                _ = MessageBox.Show("Cannot find any ASIO drivers or devices, application may not run correctly.");
        }

        protected void DisplayBufferSize()
        {
            try
            {
                string? DeviceName = this.cboDevices.SelectedItem?.ToString();
                if (!String.IsNullOrEmpty(DeviceName))
                {
                    string Min = Program.ASIO.GetMinBufferSize(DeviceName).ToString();
                    string Preferred = Program.ASIO.GetPreferredBufferSize(DeviceName).ToString();
                    string Max = Program.ASIO.GetMaxBufferSize(DeviceName).ToString();

                    //var PlaybackLatency = Program.ASIO.GetPlaybackLatency(DeviceName);
                    this.cboBufferSize.Items.Clear();
                    _ = this.cboBufferSize.Items.Add("Hardware Min (~ ms)".Replace("~", Min));
                    _ = this.cboBufferSize.Items.Add("Hardware Recommended (~ ms)".Replace("~", Preferred));
                    _ = this.cboBufferSize.Items.Add("Hardware Max (~ ms)".Replace("~", Max));
                    this.cboBufferSize.SelectedItem = this.cboBufferSize.Items[1];
                }
            }
            catch (Exception ex)
            {
                this.cboBufferSize.Items.Clear();
                _ = this.cboBufferSize.Items.Add("ASIO Driver Error Querying Device Buffer Size");
                _ = this.cboBufferSize.Items.Add("Driver Error: " + ex.Message);
                _ = this.cboBufferSize.Items.Add("ASIO Driver Error Querying Device Buffer Size");
            }
        }

        protected void DisplayChannelList()
        {
            this.lstChannels.Items.Clear();

            string? DeviceName = this.cboDevices.SelectedItem?.ToString();
            if (!String.IsNullOrEmpty(DeviceName))
            {
                var Capabilities = Program.ASIO.GetDriverCapabilities(DeviceName);
                if (Capabilities.InputChannelInfos != null)
                    foreach (var item in Capabilities.InputChannelInfos)
                        _ = this.lstChannels.Items.Add("(" + item.channel + ") " + item.name);
            }
        }

        protected void DisplaySupportedSampleRates()
        {
            this.cboSampleRate.Items.Clear();
            var SupportedSampleRates = this.GetSupportedSampleRates();
            if (SupportedSampleRates.Count > 0)
            {
                this.cboSampleRate.Items.AddRange(SupportedSampleRates.ToArray());

                this.Select_Existing_SampleRate();

                //Otherwise just select the first sample rate listed
                if (this.cboSampleRate.SelectedIndex == -1)
                    this.cboSampleRate.SelectedIndex = 0;
            }
        }

        protected void Select_Existing_Device()
        {
            foreach (var Device in this.cboDevices.Items)
            {
                if (Device.ToString() == Program.DSP_Info.ASIO_OutputDevice)
                {
                    this.cboDevices.SelectedItem = Device;
                    break;
                }
            }
        }

        protected void Select_Existing_SampleRate()
        {
            //Select the previous matching sample rate
            foreach (var SampleRate in this.cboSampleRate.Items)
            {
                if (SampleRate.ToString() == Program.DSP_Info.InSampleRate.ToString())
                {
                    this.cboSampleRate.SelectedItem = SampleRate;
                    break;
                }
            }
        }

        protected List<string> GetSupportedSampleRates()
        {
            var ReturnValue = new List<string>();

            string? DeviceName = this.cboDevices.SelectedItem?.ToString();
            if (!String.IsNullOrEmpty(DeviceName))
            {
                //I don't know what sample rates NAudio can handle(if it has any limitations)
                //but this is a good starting list for now...
                var SampleRates = new List<int>()
                                {
                                    44100,
                                    48000,
                                    88200,
                                    96000,
                                    176400,
                                    192000
                                };

                //Add only the supported rates to the list
                foreach (var SampleRate in SampleRates)
                    if (Program.ASIO.IsSampleRateSupported(DeviceName, SampleRate))
                        ReturnValue.Add(SampleRate.ToString());
            }
            return ReturnValue;
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