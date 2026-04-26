#nullable enable

namespace BassThatHz_ASIO_DSP_Processor.GUI.Controls;

#region Usings
using System;
using System.Globalization;
using System.Collections.Generic;
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
public partial class FIRControl : UserControl, IFilterControl
{
    #region Variables
    protected FIR Filter = new();
    #endregion

    #region Constructor
    public FIRControl()
    {
        InitializeComponent();
    }
    #endregion

    #region Event Handlers
    protected void btnApply_Click(object? sender, System.EventArgs e)
    {
        try
        {
            // Preserve previous enabled state and disable while applying settings
            bool filterState = this.Filter.FilterEnabled;
            this.Filter.FilterEnabled = false;

            // Parse FFT size safely to avoid exceptions and unnecessary allocations
            if (!int.TryParse(this.txtFFTSize.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var fftSize))
                fftSize = this.Filter.FFTSize; // fallback to existing value on parse failure
            this.Filter.FFTSize = fftSize;

            // Parse taps with minimal allocations and robust handling of empty/whitespace lines
            var tapsText = this.txtTaps.Text;
            if (!string.IsNullOrWhiteSpace(tapsText))
            {
                var parts = tapsText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                var list = new List<double>(parts.Length);
                foreach (var p in parts)
                {
                    // TryParse avoids exceptions for malformed lines and uses invariant culture
                    if (double.TryParse(p.Trim(), NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var d))
                        list.Add(d);
                }

                if (list.Count > 0)
                    this.Filter.SetTaps(list.ToArray());
            }

            this.Filter.FilterEnabled = filterState;
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
        if (input is FIR fir)
        {
            this.Filter = fir;

            // Use invariant culture when converting numeric values to string to avoid culture-dependent allocations
            this.txtFFTSize.Text = fir.FFTSize.ToString(CultureInfo.InvariantCulture);

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