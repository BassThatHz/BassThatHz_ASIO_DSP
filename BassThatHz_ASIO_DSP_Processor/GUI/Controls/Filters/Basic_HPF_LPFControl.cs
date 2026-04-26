#nullable enable

namespace BassThatHz_ASIO_DSP_Processor.GUI.Controls;

#region Usings
using System;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
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
public partial class Basic_HPF_LPFControl : UserControl, IFilterControl
{
    #region Variables
    protected Basic_HPF_LPF Filter = new();
    #endregion

    #region Public Properties
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public TextBox Get_txtLPFFreq => this.txtLPFFreq;
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public TextBox Get_txtHPFFreq => this.txtHPFFreq;
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public ComboBox Get_cboLPF => this.cboLPF;
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public ComboBox Get_cboHPF => this.cboHPF;
    #endregion

    #region Constructor and MapEventHandlers
    public Basic_HPF_LPFControl()
    {
        InitializeComponent();

        // Create separate data sources for each ComboBox so their selection/currency
        // managers are independent. If both controls share the same data source
        // changing one would change the other.
        var orders = Enum.GetValues(typeof(Basic_HPF_LPF.FilterOrder)).Cast<Basic_HPF_LPF.FilterOrder>().ToArray();
        this.cboHPF.DataSource = orders;
        this.cboHPF.SelectedIndex = 0;
        // Use a separate copy of the array for the second ComboBox
        this.cboLPF.DataSource = (Basic_HPF_LPF.FilterOrder[])orders.Clone();
        this.cboLPF.SelectedIndex = 0;

        this.txtHPFFreq.MaxLength = 9;
        this.txtLPFFreq.MaxLength = 9;

        this.MapEventHandlers();
        this.ApplySettings();
    }

    public void MapEventHandlers()
    {
        this.txtHPFFreq.KeyPress += TxtHPFFreq_KeyPress;
        this.txtLPFFreq.KeyPress += TxtLPFFreq_KeyPress;

        this.txtHPFFreq.TextChanged += TxtHPFFreq_TextChanged;
        this.txtLPFFreq.TextChanged += TxtLPFFreq_TextChanged;
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
    protected void TxtHPFFreq_KeyPress(object? sender, KeyPressEventArgs e)
    {
        // Validate input characters only. Avoid changing Text here to prevent caret jumps and extra allocations.
        InputValidator.Validate_IsNumeric_NonNegative(e);
    }

    protected void TxtLPFFreq_KeyPress(object? sender, KeyPressEventArgs e)
    {
        // Validate input characters only. Avoid changing Text here to prevent caret jumps and extra allocations.
        InputValidator.Validate_IsNumeric_NonNegative(e);
    }

    protected void TxtLPFFreq_TextChanged(object? sender, EventArgs e)
    {
        // Limit textual length/size first
        this.txtLPFFreq.Text = InputValidator.LimitTo_ReasonableSizedNumber(this.txtLPFFreq.Text);

        if (string.IsNullOrWhiteSpace(this.txtLPFFreq.Text))
        {
            this.txtLPFFreq.Text = "1";
            return;
        }

        if (double.TryParse(this.txtLPFFreq.Text, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out double result))
        {
            var nyquist = Program.DSP_Info.InSampleRate * 0.5;
            if (result > nyquist)
                this.txtLPFFreq.Text = nyquist.ToString(CultureInfo.InvariantCulture);
            else if (result == 0 || result <= double.Epsilon)
                this.txtLPFFreq.Text = "0.01";
        }
    }

    protected void TxtHPFFreq_TextChanged(object? sender, EventArgs e)
    {
        // Limit textual length/size first
        this.txtHPFFreq.Text = InputValidator.LimitTo_ReasonableSizedNumber(this.txtHPFFreq.Text);

        if (string.IsNullOrWhiteSpace(this.txtHPFFreq.Text))
        {
            this.txtHPFFreq.Text = "1";
            return;
        }

        if (double.TryParse(this.txtHPFFreq.Text, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out double result))
        {
            var nyquist = Program.DSP_Info.InSampleRate * 0.5;
            if (result > nyquist)
                this.txtHPFFreq.Text = nyquist.ToString(CultureInfo.InvariantCulture);
            else if (result == 0 || result <= double.Epsilon)
                this.txtHPFFreq.Text = "0.01";
        }
    }
    #endregion

    protected void btnApply_Click(object? sender, EventArgs e)
    {
        try
        {
            if (!double.TryParse(this.txtHPFFreq.Text, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out double hpf))
            {
                // Invalid input - bail out gracefully
                return;
            }

            if (!double.TryParse(this.txtLPFFreq.Text, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out double lpf))
            {
                // Invalid input - bail out gracefully
                return;
            }

            this.Filter.HPFFreq = hpf;
            if (this.cboHPF.SelectedItem != null)
                this.Filter.HPFFilter = (Basic_HPF_LPF.FilterOrder)this.cboHPF.SelectedItem!;

            this.Filter.LPFFreq = lpf;
            if (this.cboLPF.SelectedItem != null)
                this.Filter.LPFFilter = (Basic_HPF_LPF.FilterOrder)this.cboLPF.SelectedItem!;

            this.Filter.ApplySettings();
            this.ShowBiQuads();
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }

    protected void cboShowNormalized_CheckedChanged(object? sender, EventArgs e)
    {
        this.ShowBiQuads();
    }
    #endregion

    #region Protected Functions

    protected void ShowBiQuads()
    {
        var sb = new StringBuilder(1024);
        for (int i = 0; i < 8; i++)
        {
            var biquad = this.Filter.BiQuads[i];
            double normalized = (this.cboShowNormalized.Checked && biquad != null) ? biquad.aa0 : 1.0;

            double a0v = biquad != null ? biquad.aa0 / normalized : 0.0;
            double a1v = biquad != null ? biquad.aa1 / normalized : 0.0;
            double a2v = biquad != null ? biquad.aa2 / normalized : 0.0;
            double b0v = biquad != null ? biquad.b0 / normalized : 0.0;
            double b1v = biquad != null ? biquad.b1 / normalized : 0.0;
            double b2v = biquad != null ? biquad.b2 / normalized : 0.0;

            double Q = i < 4 ? this.Filter.Q_Array_HPF[i] : this.Filter.Q_Array_LPF[i - 4];

            sb.Append("biquad");
            sb.Append(i + 1);
            sb.Append(' ');
            sb.Append(biquad?.SampleRate.ToString(CultureInfo.InvariantCulture) ?? "");
            sb.Append(" q");
            sb.Append(Q.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\r\n");

            sb.Append("a0="); sb.Append(a0v.ToString(CultureInfo.InvariantCulture)); sb.Append(",\r\n");
            sb.Append("a1="); sb.Append(a1v.ToString(CultureInfo.InvariantCulture)); sb.Append(",\r\n");
            sb.Append("a2="); sb.Append(a2v.ToString(CultureInfo.InvariantCulture)); sb.Append(",\r\n");
            sb.Append("b0="); sb.Append(b0v.ToString(CultureInfo.InvariantCulture)); sb.Append(",\r\n");
            sb.Append("b1="); sb.Append(b1v.ToString(CultureInfo.InvariantCulture)); sb.Append(",\r\n");
            sb.Append("b2="); sb.Append(b2v.ToString(CultureInfo.InvariantCulture)); sb.Append(",\r\n");
        }

        this.txtBiQuads.Text = sb.ToString();
    }

    #endregion

    #region Interfaces
   
    public IFilter GetFilter =>
        this.Filter;
    
    public void ApplySettings()
    {
        this.btnApply_Click(this, EventArgs.Empty);
    }

    public void SetDeepClonedFilter(IFilter input)
    {
        if (input is Basic_HPF_LPF basic_HPF_LPF)
        {
            this.Filter = basic_HPF_LPF;

            this.cboHPF.SelectedItem = basic_HPF_LPF.HPFFilter;
            this.cboLPF.SelectedItem = basic_HPF_LPF.LPFFilter;
            this.txtHPFFreq.Text = basic_HPF_LPF.HPFFreq.ToString();
            this.txtLPFFreq.Text = basic_HPF_LPF.LPFFreq.ToString();
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