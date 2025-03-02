#nullable enable

namespace BassThatHz_ASIO_DSP_Processor.GUI.Tabs;

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
            if (Program.DSP_Info.Buses.Count > 0)
                foreach (var item in Program.DSP_Info.Buses)
                    this.SimpleBus_LSB.Items.Add(item);
            if (Program.DSP_Info.AbstractBuses.Count > 0)
                foreach (var item in Program.DSP_Info.AbstractBuses)
                    this.AbstractBus_LSB.Items.Add(item);

            this.RefreshAbstractBusComboBoxes();
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
            var TempBus = new DSP_Bus();
            TempBus.Name = this.SimpleBusName_TXT.Text;

            Program.DSP_Info.Buses.Add(TempBus);
            this.SimpleBus_LSB.Items.Add(TempBus);

            this.SelectListboxIndexIfExists(this.SimpleBus_LSB, this.SimpleBus_LSB.Items.Count - 1);
            this.RefreshAbstractBusComboBoxes();
            this.ResetAll_TabPage_Text();
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
            var Buses = Program.DSP_Info.Buses;
            int SelectedIndex = this.SimpleBus_LSB.SelectedIndex;
            if (SelectedIndex < 0 || SelectedIndex >= Buses.Count || SelectedIndex >= SimpleBus_LSB.Items.Count)
                return;

            foreach (var Stream in Program.DSP_Info.Streams)
            {
                if (Stream.InputSource.StreamType == StreamType.Bus && Stream.InputSource.Index == SelectedIndex ||
                    Stream.OutputDestination.StreamType == StreamType.Bus && Stream.OutputDestination.Index == SelectedIndex)
                {
                    MessageBox.Show("Bus in use. It must be unassigned before it can be changed.");
                    return;
                }
            }

            var TempBus = Buses[SelectedIndex];
            TempBus.Name = this.SimpleBusName_TXT.Text;

            this.SimpleBus_LSB.Items.RemoveAt(SelectedIndex);
            this.SimpleBus_LSB.Items.Insert(SelectedIndex, TempBus);
            this.SimpleBus_LSB.SelectedIndex = SelectedIndex;

            this.RefreshAbstractBusComboBoxes();
            this.ResetAll_TabPage_Text();
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
            int SelectedIndex = this.SimpleBus_LSB.SelectedIndex;
            if (SelectedIndex < 0 || SelectedIndex >= Program.DSP_Info.Buses.Count)
                return;

            foreach (var Stream in Program.DSP_Info.Streams)
            {
                if (Stream.InputSource.StreamType == StreamType.Bus && Stream.InputSource.Index == SelectedIndex ||
                    Stream.OutputDestination.StreamType == StreamType.Bus && Stream.OutputDestination.Index == SelectedIndex)
                {
                    MessageBox.Show("Bus in use. It must be unassigned before it can be deleted.");
                    return;
                }
            }

            Program.DSP_Info.Buses.RemoveAt(SelectedIndex);
            this.RemoveSelectedListboxItem(this.SimpleBus_LSB, SelectedIndex);
            this.RefreshAbstractBusComboBoxes();
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
            var AbstractBusSource_SelectedItem = this.AbstractBusSource_CBO?.SelectedItem;
            var AbstractBusDestination_SelectedItem = this.AbstractBusDestination_CBO?.SelectedItem;

            if (AbstractBusSource_SelectedItem is not StreamItem Source)
                return;

            if (AbstractBusDestination_SelectedItem is not StreamItem Destination)
                return;

            //Add new item
            var TempAbstractBus = new DSP_AbstractBus();
            TempAbstractBus.IsBypassed = this.AbstractBusBypass_CHK.Checked;
            TempAbstractBus.Name = this.AbstractBusName_TXT.Text;
            TempAbstractBus.InputSource = Source;
            TempAbstractBus.OutputDestination = Destination;

            Program.DSP_Info.AbstractBuses.Add(TempAbstractBus);
            this.AbstractBus_LSB.Items.Add(TempAbstractBus);

            this.SelectListboxIndexIfExists(this.AbstractBus_LSB, this.AbstractBus_LSB.Items.Count - 1);
            this.RefreshAbstractBusComboBoxes();
            this.ResetAll_TabPage_Text();
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
            var AbstractBusSource_SelectedItem = this.AbstractBusSource_CBO?.SelectedItem;
            var AbstractBusDestination_SelectedItem = this.AbstractBusDestination_CBO?.SelectedItem;

            if (AbstractBusSource_SelectedItem is not StreamItem Source)
                return;

            if (AbstractBusDestination_SelectedItem is not StreamItem Destination)
                return;

            //Change existing item
            var AbstractBuses = Program.DSP_Info.AbstractBuses;
            int SelectedIndex = this.AbstractBus_LSB.SelectedIndex;
            if (SelectedIndex < 0 || SelectedIndex >= AbstractBuses.Count || SelectedIndex >= this.AbstractBus_LSB.Items.Count)
                return;

            foreach (var Stream in Program.DSP_Info.Streams)
            {
                if (Stream.InputSource.StreamType == StreamType.AbstractBus && Stream.InputSource.Index == SelectedIndex ||
                    Stream.OutputDestination.StreamType == StreamType.AbstractBus && Stream.OutputDestination.Index == SelectedIndex)
                {
                    MessageBox.Show("AbstractBus in use. It must be unassigned before it can be changed.");
                    return;
                }
            }

            var TempAbstractBus = AbstractBuses[SelectedIndex];
            TempAbstractBus.Name = this.AbstractBusName_TXT.Text;
            TempAbstractBus.IsBypassed = this.AbstractBusBypass_CHK.Checked;
            TempAbstractBus.InputSource = Source;
            TempAbstractBus.OutputDestination = Destination;

            this.AbstractBus_LSB.Items.RemoveAt(SelectedIndex);
            this.AbstractBus_LSB.Items.Insert(SelectedIndex, TempAbstractBus);
            this.AbstractBus_LSB.SelectedIndex = SelectedIndex;

            this.RefreshAbstractBusComboBoxes();
            this.ResetAll_TabPage_Text();
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
            int SelectedIndex = this.AbstractBus_LSB.SelectedIndex;
            if (SelectedIndex < 0 || SelectedIndex >= Program.DSP_Info.AbstractBuses.Count)
                return;

            foreach (var Stream in Program.DSP_Info.Streams)
            {
                if (Stream.InputSource.StreamType == StreamType.AbstractBus && Stream.InputSource.Index == SelectedIndex ||
                    Stream.OutputDestination.StreamType == StreamType.AbstractBus && Stream.OutputDestination.Index == SelectedIndex)
                {
                    MessageBox.Show("AbstractBus in use. It must be unassigned before it can be deleted.");
                    return;
                }
            }

            Program.DSP_Info.AbstractBuses.RemoveAt(SelectedIndex);
            this.RemoveSelectedListboxItem(this.AbstractBus_LSB, SelectedIndex);
            this.RefreshAbstractBusComboBoxes();
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
                this.AbstractBusSource_CBO.SelectedIndex = TempAbstractBus.InputSource.Index;
                this.AbstractBusDestination_CBO.SelectedIndex = TempAbstractBus.OutputDestination.Index;
                this.AbstractBusBypass_CHK.Checked = TempAbstractBus.IsBypassed;
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

    protected void ResetAll_TabPage_Text()
    {
        var DSPConfigTab = Program.Form_Main?.Get_DSPConfigPage1;
        if (DSPConfigTab != null)
            DSPConfigTab.ResetAll_TabPage_Text();
    }
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

    protected void RefreshAbstractBusComboBoxes()
    {
        this.AbstractBusSource_CBO.Items.Clear();
        this.AbstractBusDestination_CBO.Items.Clear();
        CommonFunctions.Set_DropDownChannelLists(this.AbstractBusSource_CBO, this.AbstractBusDestination_CBO);

        if (this.AbstractBusSource_CBO.Items.Count > 0)
            this.AbstractBusSource_CBO.SelectedIndex = 0;

        if (this.AbstractBusDestination_CBO.Items.Count > 0)
            this.AbstractBusDestination_CBO.SelectedIndex = 0;
    }
    #endregion

    #region Error Handling
    protected void Error(Exception ex)
    {
        Debug.Error(ex);
    }
    #endregion
}