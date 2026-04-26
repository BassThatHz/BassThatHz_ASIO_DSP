#nullable enable

namespace BassThatHz_ASIO_DSP_Processor.GUI.Controls;

#region Usings
using DSP.Filters;
using System;
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
public partial class AuxSetControl : UserControl, IFilterControl
{
    #region Variables
    protected AuxSet Filter = new();
    #endregion

    #region Constructor
    public AuxSetControl()
    {
        InitializeComponent();

        var numberOfAuxBuffers = DSP_Stream.NumberOfAuxBuffers;

        // Avoid repeated layout updates and per-item UI allocations by building
        // the string array once and adding all items in a single operation.
        if (numberOfAuxBuffers > 0)
        {
            this.cbo_AuxToSet.BeginUpdate();
            try
            {
                var items = new string[numberOfAuxBuffers];
                for (var i = 0; i < numberOfAuxBuffers; i++)
                    items[i] = (i + 1).ToString(System.Globalization.CultureInfo.InvariantCulture);

                this.cbo_AuxToSet.Items.AddRange(items);

                // Safe to set to first entry because we know there is at least one.
                this.cbo_AuxToSet.SelectedIndex = 0;
            }
            finally
            {
                this.cbo_AuxToSet.EndUpdate();
            }
        }
        else
        {
            // If there are no aux buffers, leave the control empty and disabled to avoid
            // unnecessary UI operations elsewhere.
            this.cbo_AuxToSet.Items.Clear();
            this.cbo_AuxToSet.Enabled = false;
        }
    }
    #endregion

    #region Event Handlers
    protected void chk_MuteAfter_CheckedChanged(object sender, EventArgs e)
    {
        this.Filter.MuteAfter = this.chk_MuteAfter.Checked;
    }

    protected void cbo_AuxToSet_SelectedIndexChanged(object sender, EventArgs e)
    {
        var idx = this.cbo_AuxToSet.SelectedIndex;
        if (idx >= 0 && idx < this.cbo_AuxToSet.Items.Count)
            this.Filter.AuxSetIndex = idx;
    }
    #endregion

    #region Interfaces
    public IFilter GetFilter =>
        this.Filter;

    public void ApplySettings()
    {
        this.Filter.ApplySettings();
    }

    public void SetDeepClonedFilter(IFilter input)
    {
        if (input is AuxSet auxSet)
        {
            this.Filter = auxSet;
            this.chk_MuteAfter.Checked = this.Filter.MuteAfter;

            // Only set the selected index if it's valid for the current combo box items.
            var idx = this.Filter.AuxSetIndex;
            if (idx >= 0 && idx < this.cbo_AuxToSet.Items.Count)
            {
                this.cbo_AuxToSet.SelectedIndex = idx;
            }
            else if (this.cbo_AuxToSet.Items.Count > 0)
            {
                // Clamp to a safe default to avoid throwing and to keep UI consistent.
                this.cbo_AuxToSet.SelectedIndex = 0;
                this.Filter.AuxSetIndex = 0;
            }
        }
    }
    #endregion

    #region Error Handling
    protected void Error(Exception ex)
    {
        Debug.Error(ex);
    }
    #endregion        
}