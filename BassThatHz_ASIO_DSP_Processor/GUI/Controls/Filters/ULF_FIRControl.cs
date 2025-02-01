#nullable enable

namespace BassThatHz_ASIO_DSP_Processor.GUI.Controls;

#region Usings
using System;
using System.Threading.Tasks;
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
public partial class ULF_FIRControl : UserControl, IFilterControl
{
    #region Variables
    protected ULF_FIR Filter = new();
    #endregion

    #region Constructor
    public ULF_FIRControl()
    {
        InitializeComponent();

        this.comboTapsSampleRate.SelectedIndex = 1;
        this.txtTapsSampleRate.Text = (Program.DSP_Info.InSampleRate / 100).ToString();

        SampleRateChangeNotifier.SampleRateChanged += this.SampleRateChangeNotifier_SampleRateChanged;
    }
    #endregion

    #region Event Handlers
    protected void SampleRateChangeNotifier_SampleRateChanged(int sampleRate)
    {
        this.ApplySettings();
    }

    protected void btnApply_Click(object? sender, System.EventArgs e)
    {
        try
        {
            var FilterState = (bool)this.Filter.FilterEnabled;
            this.Filter.FilterEnabled = false;

            switch (this.comboTapsSampleRate.SelectedIndex)
            {
                case 0:
                    this.Filter.TapsSampleRate = Program.DSP_Info.InSampleRate / 10;
                    break;
                case 1:
                    this.Filter.TapsSampleRate = Program.DSP_Info.InSampleRate / 100;
                    break;
                case 2:
                    this.Filter.TapsSampleRate = Program.DSP_Info.InSampleRate / 1000;
                    break;
                case 3:
                    this.Filter.TapsSampleRate = int.Parse(this.txtTapsSampleRate.Text);
                    break;
            };
            this.txtTapsSampleRate.Text = this.Filter.TapsSampleRate.ToString();

            this.Filter.FFTSize = int.Parse(this.txtFFTSize.Text);
            var TapsString = this.txtTaps.Text.Trim();
            if (!string.IsNullOrEmpty(TapsString))
            {
                var Task_Convert = Task.Run(() => Array.ConvertAll
                                    (
                                        TapsString.Split('\n'),
                                        Double.Parse
                                    ));

                Task_Convert.Wait();
                this.Filter.SetTaps(Task_Convert.Result);
            }
            this.Filter.FilterEnabled = FilterState;
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
        if (input is ULF_FIR fir)
        {
            this.Filter = fir;

            this.txtFFTSize.Text = fir.FFTSize.ToString();
            this.comboTapsSampleRate.SelectedIndex = fir.TapsSampleRateIndex;
            this.txtTapsSampleRate.Text = fir.TapsSampleRate.ToString();

            if (fir.Taps != null)
                this.txtTaps.Text = string.Join("\n", fir.Taps);
            this.ApplySettings();
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