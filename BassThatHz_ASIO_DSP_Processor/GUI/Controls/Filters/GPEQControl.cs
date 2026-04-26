#nullable enable

namespace BassThatHz_ASIO_DSP_Processor.GUI.Controls.Filters;

#region Usings
using GUI.Forms;
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

public partial class GPEQControl : UserControl, IFilterControl
{
    #region Variables
    protected GPEQ Filter = new();

    protected FormGPEQ? GPEQ_Form;
    #endregion

    #region Constructor
    public GPEQControl()
    {
        InitializeComponent();
    }        
    #endregion

    #region Event Handlers
    protected void ConfigGPEQ_BTN_Click(object sender, System.EventArgs e)
    {
        // Create a local form instance and assign to the field so callers
        // can still access it if needed. Capturing the local variable
        // in the closure avoids accidental reference to 'this' and
        // reduces unexpected lifetime extensions.
        var form = new FormGPEQ();
        this.GPEQ_Form = form;
        form.SetFilters(this.Filter.Filters);

        form.FormClosing += (s, e) =>
        {
            try
            {
                if (form.SavedChanges)
                {
                    // Update the listbox data source in one operation.
                    // BeginUpdate/EndUpdate are not required for DataSource,
                    // but keeping UI update concise avoids extra redraws.
                    this.Filters_LSB.BeginUpdate();
                    try
                    {
                        this.Filters_LSB.DataSource = form.GetListBoxItems();
                    }
                    finally
                    {
                        this.Filters_LSB.EndUpdate();
                    }
                }
            }
            catch (Exception ex)
            {
                this.Error(ex);
            }
        };

        form.Show(this);
    }
    #endregion

    #region Interfaces
    public void ApplySettings()
    {
        this.Filter.ApplySettings();
    }

    public IFilter GetFilter =>
         this.Filter;

    public void SetDeepClonedFilter(IFilter input)
    {
        if (input is GPEQ filter)
        {
            this.Filter = filter;

            // Use a temporary FormGPEQ only for formatting list text
            // and make sure it's disposed promptly to free resources.
            using var tempForm = new FormGPEQ();

            // Update the ListBox in a single batch to avoid repeated
            // UI updates and reduce allocations.
            this.Filters_LSB.BeginUpdate();
            try
            {
                this.Filters_LSB.Items.Clear();
                int i = 0;
                if (this.Filter?.Filters != null)
                {
                    foreach (var f in this.Filter.Filters)
                    {
                        i++;
                        if (f == null)
                            continue;

                        // ApplySettings may be required per-filter; keep it
                        // but avoid re-creating objects inside the loop.
                        f.ApplySettings();

                        // Use string interpolation which is efficient and
                        // clear about intent.
                        this.Filters_LSB.Items.Add(i + " " + tempForm.GetListText(f));
                    }
                }
            }
            finally
            {
                this.Filters_LSB.EndUpdate();
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