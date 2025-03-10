namespace BassThatHz_ASIO_DSP_Processor.GUI.Tabs
{
    using System.Linq;
    using System;
    using System.Windows.Forms;

    public partial class UserControl1: UserControl
    {
        public UserControl1()
        {
            InitializeComponent();
        }

        protected void RefreshAbstractBusComboBoxes()
        {
            //this.AbstractBusSource_CBO.Items.Clear();
            //this.AbstractBusDestination_CBO.Items.Clear();
            //CommonFunctions.Set_DropDownTargetLists(this.AbstractBusSource_CBO, this.AbstractBusDestination_CBO, true);

            //if (this.AbstractBusSource_CBO.Items.Count > 0)
            //    this.AbstractBusSource_CBO.SelectedIndex = 0;

            //if (this.AbstractBusDestination_CBO.Items.Count > 0)
            //    this.AbstractBusDestination_CBO.SelectedIndex = 0;
        }

        #region Mappings

        protected void AbstractBus_SubList_Add_BTN_Click(object sender, EventArgs e)
        {
            //try
            //{
            //    var AbstractBus_SelectedItem = this.AbstractBuses_LSB.SelectedItem;
            //    var AbstractBusSource_SelectedItem = this.AbstractBusSource_CBO.SelectedItem;
            //    var AbstractBusDestination_SelectedItem = this.AbstractBusDestination_CBO.SelectedItem;

            //    if (AbstractBus_SelectedItem is not DSP_AbstractBus AbstractBus)
            //        return;

            //    if (AbstractBusSource_SelectedItem is not StreamItem Source)
            //        return;

            //    if (AbstractBusDestination_SelectedItem is not StreamItem Destination)
            //        return;

            //    var TempAbstractBusMapping = new DSP_AbstractBusMappings()
            //    {
            //        InputSource = Source
            //        ,
            //        OutputDestination = Destination
            //        ,
            //        IsBypassed = this.AbstractBus_SubItem_Bypass_CHK.Checked
            //    };

            //    if (AbstractBus.Mappings.Any(m => m.Equals(TempAbstractBusMapping)))
            //    {
            //        MessageBox.Show("Mapping already exists. Cannot create duplicates.");
            //        return;
            //    }

            //    if (Source.StreamType == StreamType.AbstractBus || Destination.StreamType == StreamType.AbstractBus)
            //    {
            //        MessageBox.Show("AbstractBus to AbstractBus is not supported at this time.");
            //        return;
            //    }

            //    AbstractBus.Mappings.Add(TempAbstractBusMapping);
            //    int index = this.AbstractBuses_SubList_LSB.Items.Add(TempAbstractBusMapping);

            //    this.RefreshAbstractBusComboBoxes();
            //    this.AbstractBuses_SubList_LSB.SelectedIndex = index;
            //    this.ResetAll_TabPage_Text();
            //    this.ResetAll_StreamDropDownLists();
            //}
            //catch (Exception ex)
            //{
            //    this.Error(ex);
            //}
        }

        protected void AbstractBus_SubList_Change_BTN_Click(object sender, EventArgs e)
        {
            //try
            //{
            //    var AbstractBus_SelectedItem = this.AbstractBuses_LSB.SelectedItem;
            //    var AbstractBusMapping_SelectedItem = this.AbstractBuses_SubList_LSB.SelectedItem;
            //    var AbstractBusSource_SelectedItem = this.AbstractBusSource_CBO.SelectedItem;
            //    var AbstractBusDestination_SelectedItem = this.AbstractBusDestination_CBO.SelectedItem;

            //    if (AbstractBus_SelectedItem is not DSP_AbstractBus AbstractBus)
            //        return;

            //    if (AbstractBusMapping_SelectedItem is not DSP_AbstractBusMappings AbstractBusMapping)
            //        return;

            //    if (AbstractBusSource_SelectedItem is not StreamItem Source)
            //        return;

            //    if (AbstractBusDestination_SelectedItem is not StreamItem Destination)
            //        return;

            //    if (Source.StreamType == StreamType.AbstractBus || Destination.StreamType == StreamType.AbstractBus)
            //    {
            //        MessageBox.Show("AbstractBus to AbstractBus is not supported at this time.");
            //        return;
            //    }

            //    var AbstractBuses = Program.DSP_Info.AbstractBuses;
            //    int AbstractBuses_SelectedIndex = this.AbstractBuses_LSB.SelectedIndex;
            //    if (AbstractBuses_SelectedIndex < 0 || AbstractBuses_SelectedIndex >= AbstractBuses.Count
            //        || AbstractBuses_SelectedIndex >= this.AbstractBuses_LSB.Items.Count)
            //        return;

            //    int AbstractBusMapping_SelectedIndex = this.AbstractBuses_SubList_LSB.SelectedIndex;
            //    if (AbstractBusMapping_SelectedIndex < 0 || AbstractBusMapping_SelectedIndex >= this.AbstractBuses_SubList_LSB.Items.Count
            //        || AbstractBusMapping_SelectedIndex >= AbstractBus.Mappings.Count)
            //        return;

            //    //Change existing item
            //    var TempAbstractBusMapping = AbstractBus.Mappings[AbstractBusMapping_SelectedIndex];
            //    TempAbstractBusMapping.IsBypassed = this.AbstractBus_SubItem_Bypass_CHK.Checked;

            //    this.AbstractBuses_SubList_LSB.Items.RemoveAt(AbstractBusMapping_SelectedIndex);
            //    this.AbstractBuses_SubList_LSB.Items.Insert(AbstractBusMapping_SelectedIndex, TempAbstractBusMapping);

            //    //Check for duplicates without creating it or changing the direct memory ref
            //    var TempMapping = new DSP_AbstractBusMappings()
            //    {
            //        InputSource = Source,
            //        OutputDestination = Destination,
            //        IsBypassed = this.AbstractBus_SubItem_Bypass_CHK.Checked
            //    };
            //    if ((TempAbstractBusMapping.InputSource != Source || TempAbstractBusMapping.OutputDestination != Destination)
            //        && !AbstractBus.Mappings.Any(m => m.Equals(TempMapping)))
            //    {
            //        foreach (var Stream in Program.DSP_Info.Streams)
            //        {
            //            if (Stream.InputSource.StreamType == StreamType.AbstractBus && Stream.InputSource.Index == AbstractBuses_SelectedIndex ||
            //                Stream.OutputDestination.StreamType == StreamType.AbstractBus && Stream.OutputDestination.Index == AbstractBuses_SelectedIndex)
            //            {
            //                MessageBox.Show("AbstractBus Mapping in use. It must be unassigned before it can be changed.");
            //                return;
            //            }
            //        }

            //        TempAbstractBusMapping.InputSource = Source;
            //        TempAbstractBusMapping.OutputDestination = Destination;
            //    }

            //    this.RefreshAbstractBusComboBoxes();
            //    this.AbstractBuses_SubList_LSB.SelectedIndex = AbstractBusMapping_SelectedIndex;
            //    this.ResetAll_TabPage_Text();
            //    this.ResetAll_StreamDropDownLists();
            //}
            //catch (Exception ex)
            //{
            //    this.Error(ex);
            //}
        }

        protected void AbstractBus_SubList_Delete_BTN_Click(object sender, EventArgs e)
        {
            //try
            //{
            //    var AbstractBus_SelectedItem = this.AbstractBuses_LSB.SelectedItem;
            //    var AbstractBusMapping_SelectedItem = this.AbstractBuses_SubList_LSB.SelectedItem;

            //    if (AbstractBus_SelectedItem is not DSP_AbstractBus AbstractBus)
            //        return;

            //    if (AbstractBusMapping_SelectedItem is not DSP_AbstractBusMappings AbstractBusMapping)
            //        return;

            //    int AbstractBusMapping_SelectedIndex = this.AbstractBuses_SubList_LSB.SelectedIndex;
            //    if (AbstractBusMapping_SelectedIndex < 0 || AbstractBusMapping_SelectedIndex >= AbstractBus.Mappings.Count)
            //        return;

            //    var AbstractBuses = Program.DSP_Info.AbstractBuses;
            //    int AbstractBuses_SelectedIndex = this.AbstractBuses_LSB.SelectedIndex;
            //    if (AbstractBuses_SelectedIndex < 0 || AbstractBuses_SelectedIndex >= AbstractBuses.Count
            //        || AbstractBuses_SelectedIndex >= this.AbstractBuses_LSB.Items.Count)
            //        return;

            //    foreach (var Stream in Program.DSP_Info.Streams)
            //    {
            //        if (Stream.InputSource.StreamType == StreamType.AbstractBus && Stream.InputSource.Index == AbstractBuses_SelectedIndex ||
            //            Stream.OutputDestination.StreamType == StreamType.AbstractBus && Stream.OutputDestination.Index == AbstractBuses_SelectedIndex)
            //        {
            //            MessageBox.Show("AbstractBus Mapping in use. It must be unassigned before it can be deleted.");
            //            return;
            //        }
            //    }

            //    AbstractBus.Mappings.RemoveAt(AbstractBusMapping_SelectedIndex);

            //    this.RefreshAbstractBusComboBoxes();
            //    this.RemoveSelectedListboxItem(this.AbstractBuses_SubList_LSB, AbstractBusMapping_SelectedIndex);
            //}
            //catch (Exception ex)
            //{
            //    this.Error(ex);
            //}
        }

        protected void AbstractBuses_SubList_LSB_SelectedIndexChanged(object sender, EventArgs e)
        {
            //try
            //{
            //    var AbstractBusMapping_SelectedItem = this.AbstractBuses_SubList_LSB.SelectedItem;
            //    var AbstractBusMappingSource_SelectedItem = this.AbstractBusSource_CBO.SelectedItem;
            //    var AbstractBusMappingDestination_SelectedItem = this.AbstractBusDestination_CBO.SelectedItem;

            //    if (AbstractBusMapping_SelectedItem is not DSP_AbstractBusMappings AbstractBusMapping)
            //        return;

            //    this.AbstractBus_SubItem_Bypass_CHK.Checked = AbstractBusMapping.IsBypassed;
            //    this.AbstractBusSource_CBO.SelectedIndex = this.AbstractBusSource_CBO.Items.IndexOf(AbstractBusMapping.InputSource);
            //    this.AbstractBusDestination_CBO.SelectedIndex = this.AbstractBusDestination_CBO.Items.IndexOf(AbstractBusMapping.OutputDestination);
            //}
            //catch (Exception ex)
            //{
            //    this.Error(ex);
            //}
        }

        #endregion

    }
}
