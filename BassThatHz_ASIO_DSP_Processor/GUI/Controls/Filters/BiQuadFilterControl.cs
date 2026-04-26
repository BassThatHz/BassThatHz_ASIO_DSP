#nullable enable

namespace BassThatHz_ASIO_DSP_Processor.GUI.Controls;

#region Usings
using NAudio.Dsp;
using System;
using System.Globalization;
using System.ComponentModel;
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
public partial class BiQuadFilterControl : UserControl, IFilterControl
{
    #region Variables
    protected BiQuadFilter BiQuad = new();

    // Suppresses TextChanged/KeyPress handlers when we update textboxes programmatically
    private bool _suspendTextChangedEvents;
    #endregion

    #region Public Properties
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public TextBox Get_txtF => this.txtF;
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public TextBox Get_txtG => this.txtG;
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public TextBox Get_txtQ => this.txtQ;
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public TextBox Get_txtS => this.txtS;
    #endregion

    #region Constructora and MapEventHandlers
    public BiQuadFilterControl()
    {
        InitializeComponent();

        this.MapEventHandlers();
    }

    public void MapEventHandlers()
    {
        this.txtF.KeyPress += TxtF_KeyPress;
        this.txtF.TextChanged += TxtF_TextChanged;
        InputValidator.Set_TextBox_MaxLength(this.txtF);

        this.txtG.KeyPress += TxtG_KeyPress;
        this.txtG.TextChanged += TxtG_TextChanged;
        InputValidator.Set_TextBox_MaxLength(this.txtG);

        this.txtQ.KeyPress += TxtQ_KeyPress;
        this.txtQ.TextChanged += TxtQ_TextChanged;
        InputValidator.Set_TextBox_MaxLength(this.txtQ);

        this.txtS.KeyPress += TxtS_KeyPress;
        this.txtS.TextChanged += TxtS_TextChanged;
        InputValidator.Set_TextBox_MaxLength(this.txtS);

        this.txta0.KeyPress += Txta0_KeyPress;
        this.txta1.KeyPress += Txta1_KeyPress;
        this.txta2.KeyPress += Txta2_KeyPress;

        this.txtb0.KeyPress += Txtb0_KeyPress;
        this.txtb1.KeyPress += Txtb1_KeyPress;
        this.txtb2.KeyPress += Txtb2_KeyPress;

        SampleRateChangeNotifier.SampleRateChanged += this.SampleRateChangeNotifier_SampleRateChanged;
    }
    #endregion

    #region Event Handlers
    protected void SampleRateChangeNotifier_SampleRateChanged(int sampleRate)
    {
        this.BiQuad.ResetSampleRate(sampleRate);
        this.ApplySettings();
    }

    #region InputValidation
    protected void TxtF_KeyPress(object? sender, KeyPressEventArgs e)
    {
        InputValidator.Validate_IsNumeric_NonNegative(e);
        // Limit size but avoid re-entrant TextChanged events
        var limited = InputValidator.LimitTo_ReasonableSizedNumber(this.txtF.Text);
        if (this.txtF.Text != limited)
        {
            try { this._suspendTextChangedEvents = true; this.txtF.Text = limited; }
            finally { this._suspendTextChangedEvents = false; }
        }
    }
    protected void TxtF_TextChanged(object? sender, EventArgs e)
    {
        if (this._suspendTextChangedEvents)
            return;

        try
        {
            this._suspendTextChangedEvents = true;

            if (string.IsNullOrEmpty(this.txtF.Text))
            {
                this.txtF.Text = "1";
                return;
            }

            if (double.TryParse(this.txtF.Text, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out double result))
            {
                var max = Program.DSP_Info.InSampleRate * 0.5;
                if (result > max)
                    this.txtF.Text = max.ToString(CultureInfo.InvariantCulture);
                else if (result == 0 || result <= double.Epsilon)
                    this.txtF.Text = "0.01";
            }
        }
        finally
        {
            this._suspendTextChangedEvents = false;
        }
    }
    protected void TxtG_TextChanged(object? sender, EventArgs e)
    {
        if (this._suspendTextChangedEvents)
            return;

        try
        {
            this._suspendTextChangedEvents = true;

            if (string.IsNullOrEmpty(this.txtG.Text))
            {
                this.txtG.Text = "0";
                return;
            }

            var trimmed = this.txtG.Text.Trim();
            if (this.txtG.Text != trimmed)
                this.txtG.Text = trimmed;

            if (double.TryParse(this.txtG.Text, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out double result))
            {
                const double limit = 999_999_999;
                if (result > limit) //Limit to 999 million
                    this.txtG.Text = limit.ToString(CultureInfo.InvariantCulture);
                else if (result < -limit) //Limit to -999 million
                    this.txtG.Text = (-limit).ToString(CultureInfo.InvariantCulture);
            }
        }
        finally
        {
            this._suspendTextChangedEvents = false;
        }
    }
    protected void TxtG_KeyPress(object? sender, KeyPressEventArgs e)
    {
        InputValidator.Validate_IsNumeric_Negative(e);
    }
    protected void TxtQ_TextChanged(object? sender, EventArgs e)
    {
        if (this._suspendTextChangedEvents)
            return;

        try
        {
            this._suspendTextChangedEvents = true;

            if (string.IsNullOrEmpty(this.txtQ.Text))
                this.txtQ.Text = "1";
        }
        finally
        {
            this._suspendTextChangedEvents = false;
        }
    }
    protected void TxtQ_KeyPress(object? sender, KeyPressEventArgs e)
    {
        InputValidator.Validate_IsNumeric_NonNegative(e);
        var limited = InputValidator.LimitTo_ReasonableSizedNumber(this.txtQ.Text);
        if (this.txtQ.Text != limited)
        {
            try { this._suspendTextChangedEvents = true; this.txtQ.Text = limited; }
            finally { this._suspendTextChangedEvents = false; }
        }
    }
    protected void TxtS_TextChanged(object? sender, EventArgs e)
    {
        if (this._suspendTextChangedEvents)
            return;

        try
        {
            this._suspendTextChangedEvents = true;

            if (string.IsNullOrEmpty(this.txtS.Text))
                this.txtS.Text = "1";
        }
        finally
        {
            this._suspendTextChangedEvents = false;
        }
    }
    protected void TxtS_KeyPress(object? sender, KeyPressEventArgs e)
    {
        InputValidator.Validate_IsNumeric_NonNegative(e);
        var limited = InputValidator.LimitTo_ReasonableSizedNumber(this.txtS.Text);
        if (this.txtS.Text != limited)
        {
            try { this._suspendTextChangedEvents = true; this.txtS.Text = limited; }
            finally { this._suspendTextChangedEvents = false; }
        }
    }

    protected void Txta0_KeyPress(object? sender, KeyPressEventArgs e)
    {
        InputValidator.Validate_IsNumeric_Negative(e);
    }
    protected void Txta1_KeyPress(object? sender, KeyPressEventArgs e)
    {
        InputValidator.Validate_IsNumeric_Negative(e);
    }
    protected void Txta2_KeyPress(object? sender, KeyPressEventArgs e)
    {
        InputValidator.Validate_IsNumeric_Negative(e);
    }
    protected void Txtb0_KeyPress(object? sender, KeyPressEventArgs e)
    {
        InputValidator.Validate_IsNumeric_Negative(e);
    }
    protected void Txtb1_KeyPress(object? sender, KeyPressEventArgs e)
    {
        InputValidator.Validate_IsNumeric_Negative(e);
    }
    protected void Txtb2_KeyPress(object? sender, KeyPressEventArgs e)
    {
        InputValidator.Validate_IsNumeric_Negative(e);
    }

    #endregion

    protected void btnApplyCo_Click(object? sender, EventArgs e)
    {
        // Use TryParse to avoid exception-based control flow for invalid input
        if (!double.TryParse(this.txta0.Text, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var a0)
            || !double.TryParse(this.txta1.Text, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var a1)
            || !double.TryParse(this.txta2.Text, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var a2)
            || !double.TryParse(this.txtb0.Text, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var b0)
            || !double.TryParse(this.txtb1.Text, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var b1)
            || !double.TryParse(this.txtb2.Text, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var b2))
        {
            this.Error(new FormatException("One or more coefficient fields contain invalid numbers."));
            return;
        }

        this.BiQuad.SetCoefficients(a0, a1, a2, b0, b1, b2);
    }

    protected void btnApply_Click(object? sender, System.EventArgs e)
    {
        // Prefer TryParse to avoid exceptions, and use invariant culture for consistency
        var r = (double)Program.DSP_Info.InSampleRate;
        if (!double.TryParse(this.txtF.Text, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var f)
            || !double.TryParse(this.txtS.Text, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var s)
            || !double.TryParse(this.txtQ.Text, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var q)
            || !double.TryParse(this.txtG.Text, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var g))
        {
            this.Error(new FormatException("One or more filter parameter fields contain invalid numbers."));
            return;
        }

        switch (this.BiQuad.FilterType)
        {
            case FilterTypes.PEQ:
                this.BiQuad.PeakingEQ(r, f, q, g);
                break;
            case FilterTypes.Adv_High_Pass:
                this.BiQuad.HighPassFilter(r, f, q);
                break;
            case FilterTypes.Adv_Low_Pass:
                this.BiQuad.LowPassFilter(r, f, q);
                break;
            case FilterTypes.Low_Shelf:
                this.BiQuad.LowShelf(r, f, s, g);
                break;
            case FilterTypes.High_Shelf:
                this.BiQuad.HighShelf(r, f, s, g);
                break;
            case FilterTypes.Notch:
                this.BiQuad.NotchFilter(r, f, q);
                break;
            case FilterTypes.Band_Pass:
                this.BiQuad.BandPassFilterConstantPeakGain(r, f, q);
                break;
            case FilterTypes.All_Pass:
                this.BiQuad.AllPassFilter(r, f, q);
                break;
            default:
                this.Error(new InvalidOperationException("FilterType not defined"));
                return;
        }

        // Update coefficient textboxes without triggering change handlers
        try
        {
            this._suspendTextChangedEvents = true;
            this.txta0.Text = this.BiQuad.aa0.ToString(CultureInfo.InvariantCulture);
            this.txta1.Text = this.BiQuad.aa1.ToString(CultureInfo.InvariantCulture);
            this.txta2.Text = this.BiQuad.aa2.ToString(CultureInfo.InvariantCulture);
            this.txtb0.Text = this.BiQuad.b0.ToString(CultureInfo.InvariantCulture);
            this.txtb1.Text = this.BiQuad.b1.ToString(CultureInfo.InvariantCulture);
            this.txtb2.Text = this.BiQuad.b2.ToString(CultureInfo.InvariantCulture);
        }
        finally
        {
            this._suspendTextChangedEvents = false;
        }
    }

    #endregion

    #region Interfaces

    public IFilter GetFilter => 
        this.BiQuad;

    public void ApplySettings()
    {
        this.btnApply_Click(this, EventArgs.Empty);
        this.btnApplyCo_Click(this, EventArgs.Empty);
        this.BiQuad.ApplySettings();
    }

    public void SetDeepClonedFilter(IFilter input)
    {
        if (input is BiQuadFilter biQuad)
        {
            this.BiQuad = biQuad;

            // Bulk update UI fields while suppressing TextChanged events to avoid extra parsing and allocations
            try
            {
                this._suspendTextChangedEvents = true;

                this.txta0.Text = biQuad.aa0.ToString(CultureInfo.InvariantCulture);
                this.txta1.Text = biQuad.aa1.ToString(CultureInfo.InvariantCulture);
                this.txta2.Text = biQuad.aa2.ToString(CultureInfo.InvariantCulture);
                this.txtb0.Text = biQuad.b0.ToString(CultureInfo.InvariantCulture);
                this.txtb1.Text = biQuad.b1.ToString(CultureInfo.InvariantCulture);
                this.txtb2.Text = biQuad.b2.ToString(CultureInfo.InvariantCulture);

                this.txtF.Text = biQuad.Frequency.ToString(CultureInfo.InvariantCulture);
                this.txtS.Text = biQuad.Slope.ToString(CultureInfo.InvariantCulture);
                this.txtQ.Text = biQuad.Q.ToString(CultureInfo.InvariantCulture);
                this.txtG.Text = biQuad.Gain.ToString(CultureInfo.InvariantCulture);
            }
            finally
            {
                this._suspendTextChangedEvents = false;
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