#nullable enable

namespace BassThatHz_ASIO_DSP_Processor.GUI.Controls;

#region Usings
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
public partial class DelayControl : UserControl, IFilterControl
{
    #region Variables
    protected Delay Filter = new();
    #endregion

    #region Constructor and MapEventHandlers
    public DelayControl()
    {
        InitializeComponent();

        this.MapEventHandlers();
    }

    public void MapEventHandlers()
    {
        this.txtDelay.KeyPress += TxtDelay_KeyPress;
        this.txtDelay.MaxLength = 5;
        SampleRateChangeNotifier.SampleRateChanged += this.SampleRateChangeNotifier_SampleRateChanged;
    }

    #endregion

    #region Event Handlers
    protected void SampleRateChangeNotifier_SampleRateChanged(int sampleRate)
    {
        this.Filter.ResetSampleRate(sampleRate);
        this.ApplySettings();
    }

    #region InputValidation
    protected void TxtDelay_KeyPress(object? sender, KeyPressEventArgs e)
    {
        InputValidator.Validate_IsNumeric_NonNegative(e);
        this.txtDelay.Text = InputValidator.LimitTo_ReasonableSizedNumber(this.txtDelay.Text);
    }
    #endregion

    protected void btnApply_Click(object? sender, System.EventArgs e)
    {
        try
        {
            if (decimal.TryParse(this.txtDelay.Text, out decimal DelayInMS))
                this.Filter.Initialize(DelayInMS, Program.ASIO.SamplesPerChannel, Program.DSP_Info.InSampleRate);
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }
    #endregion

    #region Interfaces

    public IFilter GetFilter =>
        this.Filter;

    public void ApplySettings()
    {
        this.btnApply_Click(this, EventArgs.Empty);
        this.Filter.ApplySettings();
    }

    public void SetDeepClonedFilter(IFilter input)
    {
        if (input is Delay delay)
        {
            this.Filter = delay;

            this.txtDelay.Text = delay.DelayInMS.ToString();
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