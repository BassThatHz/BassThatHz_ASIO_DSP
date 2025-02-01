#nullable enable

namespace BassThatHz_ASIO_DSP_Processor.GUI.Controls;

#region Usings
using DSP.Filters;
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
public partial class AuxGetControl : UserControl, IFilterControl
{
    #region Variables
    protected AuxGet Filter = new();
    #endregion

    #region Constructor
    public AuxGetControl()
    {
        InitializeComponent();

        var NumberOfAuxBuffers = new DSP_Stream().NumberOfAuxBuffers;
        for (var i = 1; i < NumberOfAuxBuffers + 1; i++)
            this.cbo_AuxToGet.Items.Add(i.ToString());
        this.cbo_AuxToGet.SelectedIndex = 0;
    }
    #endregion

    #region Event Handlers

    protected void btnApply_Click(object sender, EventArgs e)
    {
        this.Filter.StreamAttenuation = double.Parse(this.txtStreamAttenuation.Text); 
        this.Filter.AuxAttenuation = double.Parse(this.txtAuxAttenuation.Text);
        this.Filter.AuxGetIndex = this.cbo_AuxToGet.SelectedIndex;
        this.Filter.MuteBefore = this.chk_MuteBefore.Checked;
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
        if (input is AuxGet auxGet)
        {
            this.Filter = auxGet;

            this.chk_MuteBefore.Checked = this.Filter.MuteBefore;
            this.txtStreamAttenuation.Text = this.Filter.StreamAttenuation.ToString();
            this.txtAuxAttenuation.Text = this.Filter.AuxAttenuation.ToString();
            try
            {
                this.cbo_AuxToGet.SelectedIndex = this.Filter.AuxGetIndex;
            }
            catch (Exception)
            {
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