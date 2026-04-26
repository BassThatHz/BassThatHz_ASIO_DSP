#nullable enable

namespace BassThatHz_ASIO_DSP_Processor.GUI.Controls.Filters;


using System;
using System.ComponentModel;

#region Usings
using System.Windows.Forms;
using System.Globalization;
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
public partial class MixerElement : UserControl
{
    // Prevent re-entrant TextChanged handling when we programmatically update TextBox.Text
    private bool _suppressTextChanged;
    #region Public Properties
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public TextBox Get_txtChAttenuation => this.txtChAttenuation;
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public TextBox Get_txtStreamAttenuation => this.txtStreamAttenuation;
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public CheckBox Get_chkChannel => this.chkChannel;
    #endregion

    #region Constructor and Init
    public MixerElement()
    {
        InitializeComponent();
        this.MapEventHandlers();
    }

    protected void MapEventHandlers()
    {
        this.txtChAttenuation.KeyPress += txtChAttenuation_KeyPress;
        this.txtChAttenuation.TextChanged += txtChAttenuation_TextChanged;
        this.txtChAttenuation.MaxLength = 6;

        this.txtStreamAttenuation.KeyPress += txtStreamAttenuation_KeyPress;
        this.txtStreamAttenuation.TextChanged += txtStreamAttenuation_TextChanged;
        this.txtStreamAttenuation.MaxLength = 6;
    }
    #endregion

    #region Event Handlers

    #region InputValidation
    protected void txtChAttenuation_KeyPress(object? sender, KeyPressEventArgs e)
    {
        InputValidator.Validate_IsNumeric_Negative(e);
    }

    protected void txtChAttenuation_TextChanged(object? sender, System.EventArgs e)
    {
        if (this._suppressTextChanged)
            return;

        var text = this.txtChAttenuation.Text;
        // Empty or lone negative sign are allowed while typing
        if (string.IsNullOrEmpty(text) || text == "-")
            return;

        // If positive numeric value entered, coerce to "0" (only update when necessary)
        if (double.TryParse(text, NumberStyles.Float | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out double result) && result > 0)
        {
            if (text != "0")
            {
                try { this._suppressTextChanged = true; }
                finally { this.txtChAttenuation.Text = "0"; this.txtChAttenuation.SelectionStart = this.txtChAttenuation.Text.Length; this._suppressTextChanged = false; }
            }

            return;
        }

        // Negative symbol must be at the start of the string
        if (text.IndexOf('-') >= 0 && !text.StartsWith("-"))
        {
            var cleaned = text.Replace("-", "");
            var newText = "-" + cleaned;
            if (newText != text)
            {
                var sel = this.txtChAttenuation.SelectionStart;
                try { this._suppressTextChanged = true; }
                finally { this.txtChAttenuation.Text = newText; this.txtChAttenuation.SelectionStart = Math.Min(newText.Length, Math.Max(0, sel)); this._suppressTextChanged = false; }
            }
        }
    }

    protected void txtStreamAttenuation_KeyPress(object? sender, KeyPressEventArgs e)
    {
        InputValidator.Validate_IsNumeric_Negative(e);
    }

    protected void txtStreamAttenuation_TextChanged(object? sender, System.EventArgs e)
    {
        if (this._suppressTextChanged)
            return;

        var text = this.txtStreamAttenuation.Text;
        // Empty or lone negative sign are allowed while typing
        if (string.IsNullOrEmpty(text) || text == "-")
            return;

        if (double.TryParse(text, NumberStyles.Float | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out double result) && result > 0)
        {
            if (text != "0")
            {
                try { this._suppressTextChanged = true; }
                finally { this.txtStreamAttenuation.Text = "0"; this.txtStreamAttenuation.SelectionStart = this.txtStreamAttenuation.Text.Length; this._suppressTextChanged = false; }
            }

            return;
        }

        // Negative symbol must be at the start of the string
        if (text.IndexOf('-') >= 0 && !text.StartsWith("-"))
        {
            var cleaned = text.Replace("-", "");
            var newText = "-" + cleaned;
            if (newText != text)
            {
                var sel = this.txtStreamAttenuation.SelectionStart;
                try { this._suppressTextChanged = true; }
                finally { this.txtStreamAttenuation.Text = newText; this.txtStreamAttenuation.SelectionStart = Math.Min(newText.Length, Math.Max(0, sel)); this._suppressTextChanged = false; }
            }
        }
    }
    #endregion

    #endregion
}