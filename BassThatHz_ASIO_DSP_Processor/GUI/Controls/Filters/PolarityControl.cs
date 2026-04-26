#nullable enable

namespace BassThatHz_ASIO_DSP_Processor.GUI.Controls;

#region Usings
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
public partial class PolarityControl : UserControl, IFilterControl
{
    #region Variables
    // Keep the active filter instance private to allow internal changes
    // without exposing the concrete implementation to subclasses.
    private Polarity Filter = new();
    #endregion

    #region Constructor
    public PolarityControl()
    {
        InitializeComponent();
    }
    #endregion

    #region Event Handlers
    // Event handler kept non-virtual (private) to avoid virtual dispatch overhead
    // and avoid try/catch in the hot path. Only apply changes when the value
    // actually differs to prevent unnecessary work.
    private void cboInverted_CheckedChanged(object? sender, System.EventArgs e)
    {
        if (this.cboInverted == null)
            return;

        bool positive = !this.cboInverted.Checked;
        if (this.Filter.Positive == positive)
            return;

        this.Filter.Positive = positive;
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
        if (input is not Polarity polarity)
            return;

        // Prevent triggering the CheckedChanged event while we update the control
        // and avoid unnecessary assignments.
        bool desiredChecked = !polarity.Positive;
        bool unsubscribed = false;
        try
        {
            this.cboInverted.CheckedChanged -= cboInverted_CheckedChanged;
            unsubscribed = true;

            this.Filter = polarity;

            if (this.cboInverted.Checked != desiredChecked)
                this.cboInverted.Checked = desiredChecked;
        }
        finally
        {
            if (unsubscribed)
                this.cboInverted.CheckedChanged += cboInverted_CheckedChanged;
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