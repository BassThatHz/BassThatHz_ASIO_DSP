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
public partial class AuxGetControl : UserControl, IFilterControl
{
    #region Variables
    protected AuxGet Filter = new();
    #endregion

    #region Constructor
    public AuxGetControl()
    {
        InitializeComponent();

        var NumberOfAuxBuffers = DSP_Stream.NumberOfAuxBuffers;
        // Use BeginUpdate/EndUpdate to avoid repeated UI redraws when adding many items
        this.cbo_AuxToGet.BeginUpdate();
        try
        {
            for (var i = 1; i <= NumberOfAuxBuffers; i++)
            {
                this.cbo_AuxToGet.Items.Add(i.ToString());
            }

            if (this.cbo_AuxToGet.Items.Count > 0)
                this.cbo_AuxToGet.SelectedIndex = 0;
        }
        finally
        {
            this.cbo_AuxToGet.EndUpdate();
        }
    }
    #endregion

    #region Event Handlers

    protected void btnApply_Click(object sender, EventArgs e)
    {
        // Use TryParse to avoid throwing exceptions on invalid input and to reduce exception costs
        if (!double.TryParse(this.txtStreamAttenuation.Text, out var streamAtt))
        {
            Error(new FormatException("Invalid Stream Attenuation value."));
            return;
        }

        if (!double.TryParse(this.txtAuxAttenuation.Text, out var auxAtt))
        {
            Error(new FormatException("Invalid Aux Attenuation value."));
            return;
        }

        this.Filter.StreamAttenuation = streamAtt;
        this.Filter.AuxAttenuation = auxAtt;
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
            // Only set SelectedIndex if it's within the valid range to avoid exceptions
            var idx = this.Filter.AuxGetIndex;
            if (idx >= 0 && idx < this.cbo_AuxToGet.Items.Count)
            {
                this.cbo_AuxToGet.SelectedIndex = idx;
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