#nullable enable

namespace BassThatHz_ASIO_DSP_Processor.GUI.Tabs;

using BassThatHz_ASIO_DSP_Processor.GUI.Controls;
using NAudio.Wave.Asio;

#region Usings
using System;
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
public partial class ctl_BusesPage : UserControl
{
    #region Variables

    #endregion

    #region Constructor
    public ctl_BusesPage()
    {
        InitializeComponent();
    }
    #endregion

    #region LoadConfigRefresh
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public void LoadConfigRefresh()
    {
        try
        {
            //if (Program.DSP_Info.Buses.Count > 0)
            //    this.SimpleBus_LSB.Items.AddRange(Program.DSP_Info.Buses);
            //if (Program.DSP_Info.AbstractBuses.Count > 0)
            //    this.AbstractBus_LSB.Items.AddRange(Program.DSP_Info.AbstractBuses);

            //if (string.IsNullOrEmpty(Program.DSP_Info.ASIO_InputDevice))
            //    return;

            //AsioDriverCapability? Capabilities = null;
            //try
            //{
            //    Capabilities = Program.ASIO.GetDriverCapabilities(Program.DSP_Info.ASIO_InputDevice);
            //}
            //catch (Exception ex)
            //{
            //    _ = ex;
            //    //throw new InvalidOperationException("Can't fetch Driver Capabilities", ex);
            //}
            //if (Capabilities == null)
            //    return;

            //for (int i = 0; i < Capabilities.Value.InputChannelInfos.Length; i++)
            //{
            //    var InputChannel = Capabilities.Value.InputChannelInfos[i];
            //    this.AbstractBusSource_CBO.Items.Add("(" + InputChannel.channel + ") " + InputChannel.name);
            //}
            //if (this.AbstractBusSource_CBO.Items.Count > 0)
            //    this.AbstractBusSource_CBO.SelectedIndex = 0;

            //for (int i = 0; i < Capabilities.Value.OutputChannelInfos.Length; i++)
            //{
            //    var OutputChannel = Capabilities.Value.OutputChannelInfos[i];
            //    this.AbstractBusDestination_CBO.Items.Add("(" + OutputChannel.channel + ") " + OutputChannel.name);
            //}
            //if (this.AbstractBusDestination_CBO.Items.Count > 0)
            //    this.AbstractBusDestination_CBO.SelectedIndex = 0;
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }
    #endregion

    #region Event Handlers

    #region Simple Bus
    protected void AddBus_BTN_Click(object sender, EventArgs e)
    {
        try
        {
            //var TempBus = new DSP_Bus();
            //TempBus.Name = this.SimpleBusName_TXT.Text;

            //Program.DSP_Info.Buses.Add(TempBus);
            //this.SimpleBus_LSB.Items.Add(TempBus);

            //this.SelectListboxIndexIfExists(this.SimpleBus_LSB, this.SimpleBus_LSB.Items.Count - 1);
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }

    protected void ChangeBus_BTN_Click(object sender, EventArgs e)
    {
        try
        {
            //var Buses = Program.DSP_Info.Buses;
            //int SelectedIndex = this.SimpleBus_LSB.SelectedIndex;
            //if (SelectedIndex < 0 || SelectedIndex >= Buses.Count || SelectedIndex >= SimpleBus_LSB.Items.Count)
            //    return;            
            //var TempBus = Buses[SelectedIndex];

            //TempBus.Name = this.SimpleBusName_TXT.Text;

            //this.SimpleBus_LSB.Items[SelectedIndex] = TempBus;
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }

    protected void DeleteBus_BTN_Click(object sender, EventArgs e)
    {
        try
        {
            //int SelectedIndex = this.SimpleBus_LSB.SelectedIndex;
            //if (SelectedIndex < 0 || SelectedIndex >= Program.DSP_Info.Buses.Count)
            //    return;

            //Program.DSP_Info.Buses.RemoveAt(SelectedIndex);
            //this.RemoveSelectedListboxItem(this.SimpleBus_LSB, SelectedIndex);
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }

    protected void SimpleBus_LSB_SelectedIndexChanged(object sender, EventArgs e)
    {
        try
        {
            if (this.SimpleBus_LSB.SelectedItem is DSP_Bus TempBus)
            {
                this.SimpleBusName_TXT.Text = TempBus.Name;
            }
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }

    #endregion

    #region Abstract Bus
    protected void AddAbstractBus_BTN_Click(object sender, EventArgs e)
    {
        try
        {
            //var Source = string.Empty + this.AbstractBusSource_CBO.SelectedItem?.ToString();
            //var Destination = string.Empty + this.AbstractBusDestination_CBO.SelectedItem?.ToString();
            //var AbstractBusName = this.AbstractBusName_TXT.Text;

            //var TempAbstractBus = new DSP_AbstractBus();
            //TempAbstractBus.Name = AbstractBusName;
            //TempAbstractBus.SourceName = Source;
            //TempAbstractBus.SourceIndex = this.AbstractBusSource_CBO.SelectedIndex;
            //TempAbstractBus.DestinationName = Destination;
            //TempAbstractBus.DestinationIndex = this.AbstractBusDestination_CBO.SelectedIndex;

            //Program.DSP_Info.AbstractBuses.Add(TempAbstractBus);
            //this.AbstractBus_LSB.Items.Add(TempAbstractBus);

            //this.SelectListboxIndexIfExists(this.AbstractBus_LSB, this.AbstractBus_LSB.Items.Count - 1);
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }

    protected void ChangeAbstractBus_BTN_Click(object sender, EventArgs e)
    {
        try
        {
            //var AbstractBuses = Program.DSP_Info.AbstractBuses;
            //int SelectedIndex = this.AbstractBus_LSB.SelectedIndex;
            //if (SelectedIndex < 0 || SelectedIndex >= AbstractBuses.Count || SelectedIndex >= this.AbstractBus_LSB.Items.Count)
            //    return;
            //var TempAbstractBus = AbstractBuses[SelectedIndex];

            //var Source = string.Empty + this.AbstractBusSource_CBO.SelectedItem?.ToString();
            //var Destination = string.Empty + this.AbstractBusDestination_CBO.SelectedItem?.ToString();
            //var AbstractBusName = this.AbstractBusName_TXT.Text;

            //TempAbstractBus.Name = AbstractBusName;
            //TempAbstractBus.SourceName = Source;
            //TempAbstractBus.SourceIndex = this.AbstractBusSource_CBO.SelectedIndex;
            //TempAbstractBus.DestinationName = Destination;
            //TempAbstractBus.DestinationIndex = this.AbstractBusDestination_CBO.SelectedIndex;

            //this.AbstractBus_LSB.Items[SelectedIndex] = TempAbstractBus;
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }

    protected void DeleteAbstractBus_BTN_Click(object sender, EventArgs e)
    {
        try
        {
            //int SelectedIndex = this.AbstractBus_LSB.SelectedIndex;
            //if (SelectedIndex < 0 || SelectedIndex >= Program.DSP_Info.AbstractBuses.Count)
            //    return;

            //Program.DSP_Info.AbstractBuses.RemoveAt(SelectedIndex);
            //this.RemoveSelectedListboxItem(this.AbstractBus_LSB, SelectedIndex);
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }

    protected void AbstractBus_LSB_SelectedIndexChanged(object sender, EventArgs e)
    {
        try
        {
            if (this.AbstractBus_LSB.SelectedItem is DSP_AbstractBus TempAbstractBus)
            {
                this.AbstractBusName_TXT.Text = TempAbstractBus.Name;
                this.AbstractBusSource_CBO.SelectedIndex = TempAbstractBus.SourceIndex;
                this.AbstractBusDestination_CBO.SelectedIndex = TempAbstractBus.DestinationIndex;
            }
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }
    #endregion

    #endregion

    #region Protected Functions
    protected void RemoveSelectedListboxItem(ListBox input, int selectedIndex)
    {
        if (selectedIndex >= 0)
            input.Items.RemoveAt(selectedIndex);

        this.SelectListboxIndexIfExists(input, selectedIndex);
    }

    protected void SelectListboxIndexIfExists(ListBox input, int selectedIndex)
    {
        if (input.Items.Count > selectedIndex)
            input.SelectedIndex = selectedIndex;
        else if (input.Items.Count > 0)
            input.SelectedIndex = 0;
    }
    #endregion

    #region Error Handling
    protected void Error(Exception ex)
    {
        Debug.Error(ex);
    }
    #endregion
}