#nullable enable

namespace BassThatHz_ASIO_DSP_Processor.GUI.Controls;

#region Usings
using GUI.Controls.Filters;
using System;
using System.ComponentModel;
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
public partial class FilterControl : UserControl
{
    #region Protected Variables
    protected bool Handle_cboFilterType_SelectedIndexChanged = true;
    #endregion

    #region Events
    public event EventHandler<FilterControl>? FilterDiscarded;
    public event EventHandler<FilterControl>? FilterCreated;
    #endregion

    #region Properties
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public IFilterControl? CurrentFilterControl { get; protected set; }
    #endregion

    #region Constructor
    public FilterControl()
    {
        InitializeComponent();

        var EnumArray = Enum.GetValues(typeof(FilterTypes)).Cast<object>().ToArray();
        this.cboFilterType.Items.AddRange(EnumArray);
    }
    #endregion

    #region Event Handlers
    public void chkEnabled_CheckedChanged(object? sender, EventArgs e)
    {
        try
        {
            if (this.CurrentFilterControl?.GetFilter != null)
                this.CurrentFilterControl.GetFilter.FilterEnabled = this.chkEnabled.Checked;
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public void cboFilterType_SelectedIndexChanged(object? sender, EventArgs e)
    {
        try
        {
            if (this.Handle_cboFilterType_SelectedIndexChanged)
                this.CreateNewFilter();
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }
    #endregion

    #region Public Functions
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public void LoadConfigRefresh(IFilter input)
    {
        if (input == null)
            return;

        this.Handle_cboFilterType_SelectedIndexChanged = false;
        this.cboFilterType.SelectedIndex = this.cboFilterType.Items.IndexOf(input.FilterType);
        this.CreateFilterFromConfig(input);
        this.Handle_cboFilterType_SelectedIndexChanged = true;
    }
    #endregion

    #region Protected Functions
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    protected void CreateNewFilter()
    {
        try
        {
            if (this.cboFilterType.SelectedItem == null)
                return;

            this.FilterDiscarded?.Invoke(this, this);

            this.CreateFilterType(false, null);

            this.FilterCreated?.Invoke(this, this);
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    protected void CreateFilterFromConfig(IFilter input)
    {
        try
        {
            if (input == null)
                return;

            this.CreateFilterType(true, input);

            this.chkEnabled.Checked = input.FilterEnabled;
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }

    protected void CreateFilterType(bool setDeepClone, IFilter? input)
    {
        this.panel1.Controls.Clear();
        if (this.cboFilterType.SelectedItem == null)
            return;

        dynamic tempControl;
        switch ((FilterTypes)this.cboFilterType.SelectedItem)
        {
            case FilterTypes.PEQ:
            case FilterTypes.Adv_High_Pass:
            case FilterTypes.Adv_Low_Pass:
            case FilterTypes.Low_Shelf:
            case FilterTypes.High_Shelf:
            case FilterTypes.Notch:
            case FilterTypes.Band_Pass:
            case FilterTypes.All_Pass:
                tempControl = new BiQuadFilterControl();
                tempControl.GetFilter.FilterType = (FilterTypes)this.cboFilterType.SelectedItem;
                break;
            case FilterTypes.Polarity:
                tempControl = new PolarityControl();
                break;
            case FilterTypes.Delay:
                tempControl = new DelayControl();
                break;
            case FilterTypes.Floor:
                tempControl = new FloorControl();
                break;
            case FilterTypes.Limiter:
                tempControl = new LimiterControl();
                break;
            case FilterTypes.Anti_DC:
                tempControl = new AntiDCControl();
                break;
            case FilterTypes.SmartGain:
                tempControl = new SmartGainControl();
                break;
            case FilterTypes.FIR:
                tempControl = new FIRControl();
                break;
            case FilterTypes.Basic_HPF_LPF:
                tempControl = new Basic_HPF_LPFControl();
                break;
            case FilterTypes.Mixer:
                tempControl = new MixerControl();
                break;
            case FilterTypes.DynamicRangeCompressor:
                tempControl = new DynamicRangeCompressorControl();
                break;
            case FilterTypes.ClassicLimiter:
                tempControl = new ClassicLimiterControl();
                break;
            case FilterTypes.DEQ:
                tempControl = new DEQControl();
                break;
            case FilterTypes.AuxSet:
                tempControl = new AuxSetControl();
                break;
            case FilterTypes.AuxGet:
                tempControl = new AuxGetControl();
                break;
            case FilterTypes.ULF_FIR:
                tempControl = new ULF_FIRControl();
                break;
            case FilterTypes.GPEQ:
                tempControl = new GPEQControl();
                break;

            default:
                throw new InvalidOperationException("FilterType not defined");
        }

        if (setDeepClone && input != null)
            ((ISetDeepClonedFilter)tempControl).SetDeepClonedFilter(input);

        this.CurrentFilterControl = (IFilterControl)tempControl;
        this.panel1.Controls.Add(tempControl);

        this.chkEnabled.Enabled = true;
        this.chkEnabled.Checked = this.CurrentFilterControl.GetFilter.FilterEnabled;
    }
    #endregion

    #region Error Handling
    protected void Error(Exception ex)
    {
        Debug.Error(ex);
    }
    #endregion
}