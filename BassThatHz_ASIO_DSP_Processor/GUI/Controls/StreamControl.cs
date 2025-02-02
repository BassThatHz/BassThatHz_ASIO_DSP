#nullable enable

namespace BassThatHz_ASIO_DSP_Processor.GUI.Controls;

#region Usings
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using REW_API;
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
public partial class StreamControl : UserControl
{
    #region Variables
    public List<FilterControl> FilterControls = new();

    public event EventHandler<FilterControl>? FilterAdded;
    public event EventHandler<FilterControl>? FilterDeleted;
    public event EventHandler<FilterDirection>? FilterMovedUp;
    public event EventHandler<FilterDirection>? FilterMovedDown;
    public class FilterDirection
    {
        public FilterControl? Filter;
        public int OldIndex;
        public int NewIndex;
    }

    #region Object Refs
    protected REW_API REW_API = new REW_API();
    #endregion

    #endregion

    #region Public Properties
    public BTH_VolumeSlider Get_Out_Volume => this.Out_Volume;
    public BTH_VolumeSlider Get_In_Volume => this.In_Volume;
    public ComboBox Get_cboInputStream => this.cboInputStream;
    public ComboBox Get_cboOutputStream => this.cboOutputStream;
    public Button Get_btnDelete => this.btnDelete;
    #endregion

    #region Constructor
    public StreamControl()
    {
        InitializeComponent();
        this.panel1.Controls.Clear();
    }
    #endregion

    #region Event Handlers

    #region Stream
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public void btnAdd_Click(object? sender, EventArgs e)
    {
        try
        {
            this.AddFilter();
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    protected void btnEnableAll_Click(object? sender, EventArgs e)
    {
        try
        {
            foreach (var item in this.FilterControls)
                if (item != null && item.Get_chkEnabled.Enabled)
                    item.Get_chkEnabled.Checked = true;
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    protected void btnDisableAll_Click(object? sender, EventArgs e)
    {
        try
        {
            foreach (var item in this.FilterControls)
                if (item != null && item.Get_chkEnabled.Enabled)
                    item.Get_chkEnabled.Checked = false;
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    protected void btnApplyAll_Click(object? sender, EventArgs e)
    {
        try
        {
            foreach (var item in this.FilterControls)
                item?.CurrentFilterControl?.ApplySettings();
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }
    #endregion

    #region REW API
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    protected async void btnImportFromREW_API_Click(object sender, EventArgs e)
    {
        try
        {
            await this.ImportFromREW_API();
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    protected async void btnExportFromREW_API_Click(object sender, EventArgs e)
    {
        try
        {
            await this.ExportToREW_API();
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }
    #endregion

    #endregion

    #region Protected Functions

    #region Stream
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    protected void AddFilter()
    {
        var TempFilterControl = new FilterControl();
        this.CreateFilter(TempFilterControl);
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    protected void CreateFilter(FilterControl inputFilterControl)
    {
        inputFilterControl.BackColor = SystemColors.Control;
        this.FilterControls.Add(inputFilterControl);
        this.panel1.Controls.Add(inputFilterControl);
        this.RefreshFilterCountLabel();
        this.RedrawPanelItems();

        inputFilterControl.FilterDiscarded += (s1, e1) =>
            this.FilterDeleted?.Invoke(this, inputFilterControl);


        inputFilterControl.FilterCreated += (s1, e1) =>
            this.FilterAdded?.Invoke(this, inputFilterControl);

        #region Delete
        inputFilterControl.Get_btnDelete.Click += (s1, e1) =>
        {
            try
            {
                var CurrentVerticalScrollValue = this.panel1.VerticalScroll.Value;

                this.FilterDeleted?.Invoke(this, inputFilterControl);
                this.panel1.Controls.Remove(inputFilterControl);
                _ = this.FilterControls.Remove(inputFilterControl);

                this.RedrawPanelItems();
            }
            catch (Exception ex)
            {
                this.Error(ex);
            }
        };
        #endregion

        #region Sorting
        inputFilterControl.Get_btnUp.Click += (s1, e1) =>
        {
            try
            {
                var OldIndex = this.FilterControls.IndexOf(inputFilterControl);
                var NewIndex = OldIndex - 1;
                if (NewIndex > -1)
                {
                    var FilterDirection = new FilterDirection()
                    {
                        Filter = inputFilterControl,
                        OldIndex = OldIndex,
                        NewIndex = NewIndex
                    };
                    this.FilterMovedUp?.Invoke(this, FilterDirection);
                    this.FilterControls.RemoveAt(OldIndex);
                    this.FilterControls.Insert(NewIndex, inputFilterControl);

                    this.RedrawPanelItems();
                }
            }
            catch (Exception ex)
            {
                this.Error(ex);
            }
        };

        inputFilterControl.Get_btnDown.Click += (s1, e1) =>
        {
            try
            {
                var OldIndex = this.FilterControls.IndexOf(inputFilterControl);
                var NewIndex = OldIndex + 1;
                if (NewIndex < this.FilterControls.Count)
                {
                    var FilterDirection = new FilterDirection()
                    {
                        Filter = inputFilterControl,
                        OldIndex = OldIndex,
                        NewIndex = NewIndex
                    };
                    this.FilterMovedDown?.Invoke(this, FilterDirection);
                    this.FilterControls.RemoveAt(OldIndex);
                    this.FilterControls.Insert(NewIndex, inputFilterControl);
                    this.RedrawPanelItems();
                }
            }
            catch (Exception ex)
            {
                this.Error(ex);
            }
        };
        #endregion
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    protected void RedrawPanelItems()
    {
        int i = 0;
        foreach (var item in this.FilterControls)
        {
            item.Location = new Point(-this.panel1.HorizontalScroll.Value,
                                      -this.panel1.VerticalScroll.Value + i * item.Height + 3);
            i++;
        }
        this.RefreshFilterCountLabel();
    }

    protected void RefreshFilterCountLabel()
    {
        this.lblFilterCount.Text = this.FilterControls.Count.ToString();
    }
    #endregion

    #region REW API

    protected async Task ExportToREW_API()
    {
        try
        {
            REW_API.REW_TargetSettings REW_TargetSettings = new();
            List<REW_API.REW_Filter > REW_Filters = new();

            #region Compose REW Filters and TargetSettings
            int i = 0;
            foreach (var item in this.FilterControls)
            {
                if (item != null)
                {
                    if (item.CurrentFilterControl is BiQuadFilterControl BiQuadFilterControl && BiQuadFilterControl != null)
                    {
                        i++;
                        var Temp_REW_Filter = new REW_API.REW_Filter()
                        {
                            gaindB = double.Parse(BiQuadFilterControl.Get_txtG.Text),
                            frequency = double.Parse(BiQuadFilterControl.Get_txtF.Text),
                            enabled = item.Get_chkEnabled.Checked,
                            isAuto = false,
                            type = this.REW_API.FilterTypeToREW(BiQuadFilterControl.GetFilter.FilterType),
                            index = i,
                            q = double.Parse(BiQuadFilterControl.Get_txtQ.Text)
                        };

                        if (
                            BiQuadFilterControl.GetFilter.FilterType is FilterTypes.All_Pass
                            ||
                            BiQuadFilterControl.GetFilter.FilterType is FilterTypes.Adv_High_Pass
                            ||
                            BiQuadFilterControl.GetFilter.FilterType is FilterTypes.Adv_Low_Pass
                            ||
                            BiQuadFilterControl.GetFilter.FilterType is FilterTypes.Notch
                           )
                        {
                            Temp_REW_Filter.gaindB = null;
                        }

                        REW_Filters.Add(Temp_REW_Filter);
                    }
                    else if (item.CurrentFilterControl is Basic_HPF_LPFControl Basic_HPF_LPFControl && Basic_HPF_LPFControl != null)
                    {
                        REW_TargetSettings.highPassCutoffHz = int.Parse(Basic_HPF_LPFControl.Get_txtHPFFreq.Text);
                        REW_TargetSettings.lowPassCutoffHz = int.Parse(Basic_HPF_LPFControl.Get_txtLPFFreq.Text);

                        REW_TargetSettings.lowPassCrossoverType = this.REW_API.FilterOrderToREW(Basic_HPF_LPFControl.Get_cboLPF.SelectedItem as Basic_HPF_LPF.FilterOrder?);
                        REW_TargetSettings.highPassCrossoverType = this.REW_API.FilterOrderToREW(Basic_HPF_LPFControl.Get_cboHPF.SelectedItem as Basic_HPF_LPF.FilterOrder?);
                        REW_TargetSettings.shape = item.Get_chkEnabled.Checked ? "Driver" : "None";
                    }
                }
            }
            #endregion

            await this.REW_API.PostToREW_API(this.txt_REW_ID.Text, REW_TargetSettings, REW_Filters);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Error exporting to REW: " + ex.Message, "REW API error");
            _ = ex;
        }
    }

    protected async Task ImportFromREW_API()
    {
        REW_API.REW_TargetSettings? REW_TargetSettings = null;
        List<REW_API.REW_Filter>? REW_Filters = null;

        bool HadError = false;
        try
        {
            string REW_ID = this.txt_REW_ID.Text;
            REW_TargetSettings = await this.REW_API.GetTargetSettingsFromREW_API(REW_ID);
            REW_Filters = await this.REW_API.GetFiltersFromREW_API(REW_ID);
        }
        catch (Exception ex)
        {
            HadError = true;
            MessageBox.Show("Error Fetching from REW: " + ex.Message, "REW API error");
        }

        if (!HadError)
        {
            //Create the Fetched Filters if any
            if (REW_Filters != null && REW_Filters.Count > 0)
                foreach (var REW_Filter in REW_Filters)
                    if (REW_Filter != null && REW_Filter.type != string.Empty && REW_Filter.type != "None")
                        this.Create_BiquadFilter_FromREW(REW_Filter);

            //Create the Fetched Crossovers if any
            if (REW_TargetSettings != null)
                this.Create_XO_FromREW(REW_TargetSettings);
        }
    }

    protected void Create_BiquadFilter_FromREW(REW_API.REW_Filter filter_REW)
    {
        FilterControl TempFilterControl = new();
        this.CreateFilter(TempFilterControl);
        var FilterType = this.REW_API.REW_To_FilterType(filter_REW.type);
        TempFilterControl.Get_cboFilterType.SelectedIndex = TempFilterControl.Get_cboFilterType.Items.IndexOf(FilterType);
        TempFilterControl.Get_chkEnabled.Checked = filter_REW.enabled;

        if (TempFilterControl.CurrentFilterControl is BiQuadFilterControl BiQuadFilterControl && BiQuadFilterControl != null)
        {
            var Filter = BiQuadFilterControl.GetFilter;
            Filter.FilterEnabled = filter_REW.enabled;

            BiQuadFilterControl.Get_txtF.Text = filter_REW.frequency.ToString();
            if (filter_REW.q.HasValue)
                BiQuadFilterControl.Get_txtQ.Text = Math.Max(0.001d, filter_REW.q.Value).ToString();
            BiQuadFilterControl.Get_txtG.Text = filter_REW.gaindB.ToString();
            BiQuadFilterControl.ApplySettings();
        }
    }

    protected void Create_XO_FromREW(REW_API.REW_TargetSettings targetSettings_REW)
    {
        FilterControl TempFilterControl = new();
        this.CreateFilter(TempFilterControl);
        var FilterType = FilterTypes.Basic_HPF_LPF;
        TempFilterControl.Get_cboFilterType.SelectedIndex = TempFilterControl.Get_cboFilterType.Items.IndexOf(FilterType);
        TempFilterControl.Get_chkEnabled.Checked = targetSettings_REW.shape != "None";

        if (TempFilterControl.CurrentFilterControl is Basic_HPF_LPFControl Basic_HPF_LPFControl && Basic_HPF_LPFControl != null)
        {
            Basic_HPF_LPFControl.Get_txtHPFFreq.Text = targetSettings_REW.highPassCutoffHz.ToString();
            var HPF_FilterType = this.REW_API.REW_To_FilterOrder(targetSettings_REW.highPassCrossoverType);
            Basic_HPF_LPFControl.Get_cboHPF.SelectedIndex = Basic_HPF_LPFControl.Get_cboHPF.Items.IndexOf(HPF_FilterType);

            Basic_HPF_LPFControl.Get_txtLPFFreq.Text = targetSettings_REW.lowPassCutoffHz.ToString();
            var LPF_FilterType = this.REW_API.REW_To_FilterOrder(targetSettings_REW.lowPassCrossoverType);
            Basic_HPF_LPFControl.Get_cboLPF.SelectedIndex = Basic_HPF_LPFControl.Get_cboLPF.Items.IndexOf(LPF_FilterType);

            Basic_HPF_LPFControl.ApplySettings();
        }
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