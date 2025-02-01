#nullable enable

namespace BassThatHz_ASIO_DSP_Processor.GUI.Controls.Filters;

#region Usings
using GUI.Forms;
using System;
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
        this.GPEQ_Form = new();
        this.GPEQ_Form.SetFilters(this.Filter.Filters);

        this.GPEQ_Form.FormClosing += (s, e) =>
        {
            try
            {
                if (this.GPEQ_Form.SavedChanges)
                {
                    this.Filters_LSB.DataSource = this.GPEQ_Form.GetListBoxItems();
                }
            }
            catch (Exception ex)
            {
                this.Error(ex);
            }
        };

        this.GPEQ_Form.Show(this);
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
            var TempGPEQForm = new FormGPEQ();
            int i = 0;
            foreach (var Filter in this.Filter.Filters)
            {
                i++;
                if (Filter != null)
                {
                    Filter.ApplySettings();
                    this.Filters_LSB.Items.Add(i + " " + TempGPEQForm.GetListText(Filter));
                }
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