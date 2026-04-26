#nullable enable

namespace BassThatHz_ASIO_DSP_Processor.GUI.Controls;

#region Usings
using System;
using System.Collections.Generic;
using System.Globalization;
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
        // Perform parsing/assignment synchronously and with minimal allocations
        var previousEnabled = this.Filter.FilterEnabled;
        this.Filter.FilterEnabled = false;

        try
        {
            var inSampleRate = Program.DSP_Info.InSampleRate;
            var tapsSampleRateIndex = this.comboTapsSampleRate.SelectedIndex;

            switch (tapsSampleRateIndex)
            {
                case 0:
                    this.Filter.TapsSampleRate = inSampleRate / 10;
                    break;
                case 1:
                    this.Filter.TapsSampleRate = inSampleRate / 100;
                    break;
                case 2:
                    this.Filter.TapsSampleRate = inSampleRate / 1000;
                    break;
                case 3:
                    if (!int.TryParse(this.txtTapsSampleRate.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var customRate))
                        customRate = this.Filter.TapsSampleRate;
                    this.Filter.TapsSampleRate = customRate;
                    break;
                default:
                    // keep existing
                    break;
            }

            this.txtTapsSampleRate.Text = this.Filter.TapsSampleRate.ToString(CultureInfo.InvariantCulture);

            if (!int.TryParse(this.txtFFTSize.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var fftSize))
                fftSize = this.Filter.FFTSize;
            this.Filter.FFTSize = fftSize;

            var tapsText = this.txtTaps.Text;
            if (!string.IsNullOrWhiteSpace(tapsText))
            {
                // Parse lines with ReadOnlySpan to reduce temporary string allocations
                var span = tapsText.AsSpan();
                var taps = new List<double>();

                int pos = 0;
                while (pos < span.Length)
                {
                    var slice = span[pos..];
                    int nl = slice.IndexOf('\n');
                    ReadOnlySpan<char> lineSpan;
                    if (nl == -1)
                    {
                        lineSpan = slice.Trim();
                        pos = span.Length; // done
                    }
                    else
                    {
                        lineSpan = slice.Slice(0, nl).Trim();
                        pos += nl + 1;
                    }

                    if (lineSpan.Length == 0)
                        continue;

                    if (double.TryParse(lineSpan, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var value))
                        taps.Add(value);
                    else
                        // ignore malformed lines rather than throwing; could log if needed
                        continue;
                }

                if (taps.Count > 0)
                    this.Filter.SetTaps(taps.ToArray());
            }
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
        finally
        {
            // Always restore previous enabled state
            this.Filter.FilterEnabled = previousEnabled;
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