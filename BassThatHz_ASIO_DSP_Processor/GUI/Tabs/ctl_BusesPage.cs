#nullable enable

namespace BassThatHz_ASIO_DSP_Processor.GUI.Tabs;

#region Usings
using NAudio.Wave.Asio;
using System;
using System.IO;
using System.Linq;
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
public partial class ctl_BusesPage : UserControl, IHasFocus
{
    #region Variables

    #endregion

    #region Constructor
    public ctl_BusesPage()
    {
        InitializeComponent();

        this.Load += Control_Load;
    }

    #endregion

    #region Public Functions
    public void HasFocus()
    {
        this.RefreshAbstractBusComboBoxes();
    }
    #endregion

    #region LoadConfigRefresh
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public void LoadConfigRefresh()
    {
        try
        {
            this.SimpleBus_LSB.Items.Clear();
            this.AbstractBuses_LSB.Items.Clear();
            this.AbstractBuses_SubList_LSB.Items.Clear();
            var DSP_Info = Program.DSP_Info;
            if (DSP_Info.Buses.Count > 0)
                foreach (var Bus in DSP_Info.Buses)
                    this.SimpleBus_LSB.Items.Add(Bus);
            if (DSP_Info.AbstractBuses.Count > 0)
                foreach (var AbstractBus in DSP_Info.AbstractBuses)
                {
                    this.AbstractBuses_LSB.Items.Add(AbstractBus);
                    this.AbstractBuses_SubList_LSB.Items.Clear();
                    foreach (var Mapping in AbstractBus.Mappings)
                        this.AbstractBuses_SubList_LSB.Items.Add(Mapping);
                }

            this.SelectListboxIndexIfExists(this.SimpleBus_LSB, 0);
            this.SelectListboxIndexIfExists(this.AbstractBuses_LSB, 0);
            this.SelectListboxIndexIfExists(this.AbstractBuses_SubList_LSB, 0);
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }
    #endregion

    #region Event Handlers

    #region Load
    protected void Control_Load(object? sender, EventArgs e)
    {
        this.RefreshAbstractBusComboBoxes();
        this.SelectListboxIndexIfExists(this.AbstractBuses_LSB, 0);
    }
    #endregion

    #region Simple Bus
    protected void AddBus_BTN_Click(object sender, EventArgs e)
    {
        try
        {
            var TempBus = new DSP_Bus();
            TempBus.Name = this.SimpleBusName_TXT.Text;
            TempBus.IsBypassed = this.Bus_Bypass_CHK.Checked;

            var Buses = Program.DSP_Info.Buses;
            var AbstractBuses = Program.DSP_Info.AbstractBuses;
            //Check for duplicates without creating it or changing the direct memory ref
            if (Buses.Any(m => m.Equals(TempBus))
                ||
                AbstractBuses.Any(m => m.Name == TempBus.Name)
                )
            {
                MessageBox.Show("Already exists. Cannot create duplicates.");
                return;
            }


            Program.DSP_Info.Buses.Add(TempBus);
            this.SimpleBus_LSB.Items.Add(TempBus);

            this.SelectListboxIndexIfExists(this.SimpleBus_LSB, this.SimpleBus_LSB.Items.Count - 1);
            this.RefreshAbstractBusComboBoxes();
            this.ResetAll_TabPage_Text();
            this.ResetAll_StreamDropDownLists();
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

            var TempBus = Buses[SelectedIndex];
            TempBus.IsBypassed = this.Bus_Bypass_CHK.Checked;
            var NewName = this.SimpleBusName_TXT.Text;

            var AbstractBuses = Program.DSP_Info.AbstractBuses;
            //Check for duplicates without creating it or changing the direct memory ref
            if (TempBus.Name != NewName
               && !AbstractBuses.Any(m => m.Name == NewName)
               && !Buses.Any(m => m.Name == NewName)
               )
            {
                foreach (var Stream in Program.DSP_Info.Streams)
                {
                    if (Stream.InputSource.StreamType == StreamType.Bus && Stream.InputSource.Index == SelectedIndex ||
                        Stream.OutputDestination.StreamType == StreamType.Bus && Stream.OutputDestination.Index == SelectedIndex)
                    {
                        MessageBox.Show("Bus in use. It must be unassigned before it can be changed.");
                        return;
                    }
                }

                TempBus.Name = NewName;
            }

            this.SimpleBus_LSB.Items.RemoveAt(SelectedIndex);
            this.SimpleBus_LSB.Items.Insert(SelectedIndex, TempBus);
            this.SimpleBus_LSB.SelectedIndex = SelectedIndex;

            this.RefreshAbstractBusComboBoxes();
            this.ResetAll_TabPage_Text();
            this.ResetAll_StreamDropDownLists();
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
                this.Bus_Bypass_CHK.Checked = TempBus.IsBypassed;
            }
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }
    #endregion

    #region Abstract Bus

    #region Abstract Buses
    protected void AddAbstractBus_BTN_Click(object sender, EventArgs e)
    {
        try
        {
            //Add new item
            var TempAbstractBus = new DSP_AbstractBus();
            TempAbstractBus.IsBypassed = this.AbstractBus_Bypass_CHK.Checked;
            TempAbstractBus.Name = this.AbstractBusName_TXT.Text;

            var Buses = Program.DSP_Info.Buses;
            var AbstractBuses = Program.DSP_Info.AbstractBuses;
            //Check for duplicates without creating it or changing the direct memory ref
            if (AbstractBuses.Any(m => m.Equals(TempAbstractBus))
                ||
                Buses.Any(m => m.Name == TempAbstractBus.Name)
                )
            {
                MessageBox.Show("Already exists. Cannot create duplicates.");
                return;
            }

            AbstractBuses.Add(TempAbstractBus);
            int index = this.AbstractBuses_LSB.Items.Add(TempAbstractBus);
            this.AbstractBuses_LSB.SelectedIndex = index;

            this.ResetAll_TabPage_Text();
            this.ResetAll_StreamDropDownLists();
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
            var Buses = Program.DSP_Info.Buses;
            var AbstractBuses = Program.DSP_Info.AbstractBuses;
            int SelectedIndex = this.AbstractBuses_LSB.SelectedIndex;
            if (this.AbstractBusName_TXT.Text.Length < 1
                || SelectedIndex < 0 || SelectedIndex >= AbstractBuses.Count
                || SelectedIndex >= this.AbstractBuses_LSB.Items.Count
               )
            {
                return;
            }

            //Change existing item
            var TempAbstractBus = AbstractBuses[SelectedIndex];
            TempAbstractBus.IsBypassed = this.AbstractBus_Bypass_CHK.Checked;
            var NewName = this.AbstractBusName_TXT.Text;

            //Check for duplicates without creating it or changing the direct memory ref
            var TempAB = new DSP_AbstractBus()
            {
                Name = NewName,
                IsBypassed = this.AbstractBus_Bypass_CHK.Checked
            };
            if (TempAbstractBus.Name != NewName
                && !AbstractBuses.Any(m => m.Name == NewName)
                && !Buses.Any(m => m.Name == NewName)
                )
            {
                foreach (var Stream in Program.DSP_Info.Streams)
                {
                    if (Stream.InputSource.StreamType == StreamType.AbstractBus && Stream.InputSource.Index == SelectedIndex ||
                        Stream.OutputDestination.StreamType == StreamType.AbstractBus && Stream.OutputDestination.Index == SelectedIndex)
                    {
                        MessageBox.Show("AbstractBus in use. It must be unassigned before it can be changed.");
                        return;
                    }
                }

                TempAbstractBus.Name = NewName;
            }

            this.AbstractBuses_LSB.Items.RemoveAt(SelectedIndex);
            this.AbstractBuses_LSB.Items.Insert(SelectedIndex, TempAbstractBus);
            this.AbstractBuses_LSB.SelectedIndex = SelectedIndex;

            this.ResetAll_TabPage_Text();
            this.ResetAll_StreamDropDownLists();
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
            int SelectedIndex = this.AbstractBuses_LSB.SelectedIndex;
            if (SelectedIndex < 0 || SelectedIndex >= Program.DSP_Info.AbstractBuses.Count || SelectedIndex >= this.AbstractBuses_LSB.Items.Count)
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
            this.RemoveSelectedListboxItem(this.AbstractBuses_LSB, SelectedIndex);
            this.AbstractBuses_SubList_LSB.Items.Clear();
            this.SelectListboxIndexIfExists(this.AbstractBuses_SubList_LSB, 0);

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
            if (this.AbstractBuses_LSB.SelectedItem is DSP_AbstractBus AbstractBus)
            {
                this.AbstractBusName_TXT.Text = AbstractBus.Name;
                this.AbstractBus_Bypass_CHK.Checked = AbstractBus.IsBypassed;

                this.AbstractBuses_SubList_LSB.Items.Clear();
                foreach (var Mapping in AbstractBus.Mappings)
                    this.AbstractBuses_SubList_LSB.Items.Add(Mapping);

                this.SelectListboxIndexIfExists(this.AbstractBuses_SubList_LSB, 0);
            }
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }
    #endregion

    #region Mappings

    protected void AbstractBus_SubList_Add_BTN_Click(object sender, EventArgs e)
    {
        try
        {
            var AbstractBus_SelectedItem = this.AbstractBuses_LSB.SelectedItem;
            var AbstractBusSource_SelectedItem = this.AbstractBusSource_CBO.SelectedItem;
            var AbstractBusDestination_SelectedItem = this.AbstractBusDestination_CBO.SelectedItem;

            if (AbstractBus_SelectedItem is not DSP_AbstractBus AbstractBus)
                return;

            if (AbstractBusSource_SelectedItem is not StreamItem Source)
                return;

            if (AbstractBusDestination_SelectedItem is not StreamItem Destination)
                return;

            var TempAbstractBusMapping = new DSP_AbstractBusMappings()
            {
                InputSource = Source
                ,
                OutputDestination = Destination
                ,
                IsBypassed = this.AbstractBus_SubItem_Bypass_CHK.Checked
            };

            if (AbstractBus.Mappings.Any(m => m.Equals(TempAbstractBusMapping)))
            {
                MessageBox.Show("Mapping already exists. Cannot create duplicates.");
                return;
            }

            if (Source.StreamType == StreamType.AbstractBus || Destination.StreamType == StreamType.AbstractBus)
            {
                MessageBox.Show("AbstractBus to AbstractBus is not supported at this time.");
                return;
            }

            AbstractBus.Mappings.Add(TempAbstractBusMapping);
            int index = this.AbstractBuses_SubList_LSB.Items.Add(TempAbstractBusMapping);

            this.RefreshAbstractBusComboBoxes();
            this.AbstractBuses_SubList_LSB.SelectedIndex = index;
            this.ResetAll_TabPage_Text();
            this.ResetAll_StreamDropDownLists();
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }

    protected void AbstractBus_SubList_Change_BTN_Click(object sender, EventArgs e)
    {
        try
        {
            var AbstractBus_SelectedItem = this.AbstractBuses_LSB.SelectedItem;
            var AbstractBusMapping_SelectedItem = this.AbstractBuses_SubList_LSB.SelectedItem;
            var AbstractBusSource_SelectedItem = this.AbstractBusSource_CBO.SelectedItem;
            var AbstractBusDestination_SelectedItem = this.AbstractBusDestination_CBO.SelectedItem;

            if (AbstractBus_SelectedItem is not DSP_AbstractBus AbstractBus)
                return;

            if (AbstractBusMapping_SelectedItem is not DSP_AbstractBusMappings AbstractBusMapping)
                return;

            if (AbstractBusSource_SelectedItem is not StreamItem Source)
                return;

            if (AbstractBusDestination_SelectedItem is not StreamItem Destination)
                return;

            if (Source.StreamType == StreamType.AbstractBus || Destination.StreamType == StreamType.AbstractBus)
            {
                MessageBox.Show("AbstractBus to AbstractBus is not supported at this time.");
                return;
            }

            var AbstractBuses = Program.DSP_Info.AbstractBuses;
            int AbstractBuses_SelectedIndex = this.AbstractBuses_LSB.SelectedIndex;
            if (AbstractBuses_SelectedIndex < 0 || AbstractBuses_SelectedIndex >= AbstractBuses.Count
                || AbstractBuses_SelectedIndex >= this.AbstractBuses_LSB.Items.Count)
                return;

            int AbstractBusMapping_SelectedIndex = this.AbstractBuses_SubList_LSB.SelectedIndex;
            if (AbstractBusMapping_SelectedIndex < 0 || AbstractBusMapping_SelectedIndex >= this.AbstractBuses_SubList_LSB.Items.Count
                || AbstractBusMapping_SelectedIndex >= AbstractBus.Mappings.Count)
                return;

            //Change existing item
            var TempAbstractBusMapping = AbstractBus.Mappings[AbstractBusMapping_SelectedIndex];
            TempAbstractBusMapping.IsBypassed = this.AbstractBus_SubItem_Bypass_CHK.Checked;

            this.AbstractBuses_SubList_LSB.Items.RemoveAt(AbstractBusMapping_SelectedIndex);
            this.AbstractBuses_SubList_LSB.Items.Insert(AbstractBusMapping_SelectedIndex, TempAbstractBusMapping);

            //Check for duplicates without creating it or changing the direct memory ref
            var TempMapping = new DSP_AbstractBusMappings()
            {
                InputSource = Source,
                OutputDestination = Destination,
                IsBypassed = this.AbstractBus_SubItem_Bypass_CHK.Checked
            };
            if ((TempAbstractBusMapping.InputSource != Source || TempAbstractBusMapping.OutputDestination != Destination)
                && !AbstractBus.Mappings.Any(m => m.Equals(TempMapping)))
            {
                foreach (var Stream in Program.DSP_Info.Streams)
                {
                    if (Stream.InputSource.StreamType == StreamType.AbstractBus && Stream.InputSource.Index == AbstractBuses_SelectedIndex ||
                        Stream.OutputDestination.StreamType == StreamType.AbstractBus && Stream.OutputDestination.Index == AbstractBuses_SelectedIndex)
                    {
                        MessageBox.Show("AbstractBus Mapping in use. It must be unassigned before it can be changed.");
                        return;
                    }
                }

                TempAbstractBusMapping.InputSource = Source;
                TempAbstractBusMapping.OutputDestination = Destination;
            }

            this.RefreshAbstractBusComboBoxes();
            this.AbstractBuses_SubList_LSB.SelectedIndex = AbstractBusMapping_SelectedIndex;
            this.ResetAll_TabPage_Text();
            this.ResetAll_StreamDropDownLists();
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }

    protected void AbstractBus_SubList_Delete_BTN_Click(object sender, EventArgs e)
    {
        try
        {
            var AbstractBus_SelectedItem = this.AbstractBuses_LSB.SelectedItem;
            var AbstractBusMapping_SelectedItem = this.AbstractBuses_SubList_LSB.SelectedItem;

            if (AbstractBus_SelectedItem is not DSP_AbstractBus AbstractBus)
                return;

            if (AbstractBusMapping_SelectedItem is not DSP_AbstractBusMappings AbstractBusMapping)
                return;

            int AbstractBusMapping_SelectedIndex = this.AbstractBuses_SubList_LSB.SelectedIndex;
            if (AbstractBusMapping_SelectedIndex < 0 || AbstractBusMapping_SelectedIndex >= AbstractBus.Mappings.Count)
                return;

            var AbstractBuses = Program.DSP_Info.AbstractBuses;
            int AbstractBuses_SelectedIndex = this.AbstractBuses_LSB.SelectedIndex;
            if (AbstractBuses_SelectedIndex < 0 || AbstractBuses_SelectedIndex >= AbstractBuses.Count
                || AbstractBuses_SelectedIndex >= this.AbstractBuses_LSB.Items.Count)
                return;

            foreach (var Stream in Program.DSP_Info.Streams)
            {
                if (Stream.InputSource.StreamType == StreamType.AbstractBus && Stream.InputSource.Index == AbstractBuses_SelectedIndex ||
                    Stream.OutputDestination.StreamType == StreamType.AbstractBus && Stream.OutputDestination.Index == AbstractBuses_SelectedIndex)
                {
                    MessageBox.Show("AbstractBus Mapping in use. It must be unassigned before it can be deleted.");
                    return;
                }
            }

            AbstractBus.Mappings.RemoveAt(AbstractBusMapping_SelectedIndex);

            this.RefreshAbstractBusComboBoxes();
            this.RemoveSelectedListboxItem(this.AbstractBuses_SubList_LSB, AbstractBusMapping_SelectedIndex);
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }

    protected void AbstractBuses_SubList_LSB_SelectedIndexChanged(object sender, EventArgs e)
    {
        try
        {
            var AbstractBusMapping_SelectedItem = this.AbstractBuses_SubList_LSB.SelectedItem;
            var AbstractBusMappingSource_SelectedItem = this.AbstractBusSource_CBO.SelectedItem;
            var AbstractBusMappingDestination_SelectedItem = this.AbstractBusDestination_CBO.SelectedItem;

            if (AbstractBusMapping_SelectedItem is not DSP_AbstractBusMappings AbstractBusMapping)
                return;

            this.AbstractBus_SubItem_Bypass_CHK.Checked = AbstractBusMapping.IsBypassed;
            this.AbstractBusSource_CBO.SelectedIndex = this.AbstractBusSource_CBO.Items.IndexOf(AbstractBusMapping.InputSource);
            this.AbstractBusDestination_CBO.SelectedIndex = this.AbstractBusDestination_CBO.Items.IndexOf(AbstractBusMapping.OutputDestination);
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }

    #endregion

    #endregion

    #endregion

    #region Protected Functions

    protected void ResetAll_TabPage_Text()
    {
        var DSPConfigTab = Program.Form_Main?.Get_DSPConfigPage1;
        if (DSPConfigTab != null)
            DSPConfigTab.ResetAll_TabPage_Text();
    }
    protected void ResetAll_StreamDropDownLists()
    {
        var DSPConfigTab = Program.Form_Main?.Get_DSPConfigPage1;
        if (DSPConfigTab != null)
            DSPConfigTab.ResetAll_StreamDropDownLists();
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
        else
            input.SelectedIndex = -1;
    }

    protected void RefreshAbstractBusComboBoxes()
    {
        this.AbstractBusSource_CBO.Items.Clear();
        this.AbstractBusDestination_CBO.Items.Clear();
        CommonFunctions.Set_DropDownTargetLists(this.AbstractBusSource_CBO, this.AbstractBusDestination_CBO, true);

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