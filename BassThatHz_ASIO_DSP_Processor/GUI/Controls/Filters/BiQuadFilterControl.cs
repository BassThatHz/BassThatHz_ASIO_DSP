#nullable enable

namespace BassThatHz_ASIO_DSP_Processor.GUI.Controls
{
    using NAudio.Dsp;
    using System;
    using System.Windows.Forms;

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
    public partial class BiQuadFilterControl : UserControl, IFilterControl
    {
        #region Variables
        protected BiQuadFilter BiQuad = new();
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
        private void TxtF_KeyPress(object? sender, KeyPressEventArgs e)
        {
            InputValidator.Validate_IsNumeric_NonNegative(e);
            this.txtF.Text = InputValidator.LimitTo_ReasonableSizedNumber(this.txtF.Text);
        }
        private void TxtF_TextChanged(object? sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(this.txtF.Text))
                this.txtF.Text = "1";

            if (double.TryParse(this.txtF.Text, out double result))
            {
                if (result > Program.DSP_Info.InSampleRate * 0.5)
                    this.txtF.Text = (Program.DSP_Info.InSampleRate * 0.5).ToString();
                else if (result == 0 || result <= double.Epsilon)
                    this.txtF.Text = "0.01";
            }
        }
        private void TxtG_TextChanged(object? sender, EventArgs e)
        {
            //this.txtG.Text = InputValidator.LimitTo_ReasonableSizedNumber(this.txtG.Text, true);

            if (string.IsNullOrEmpty(this.txtG.Text))
                this.txtG.Text = "0";

            this.txtG.Text = this.txtG.Text.Trim();

            if (double.TryParse(this.txtG.Text, out double result))
            {
                if (result > 999999999) //Limit to 999 million
                    this.txtG.Text = "999999999";
                else if (result < -999999999) //Limit to -999 million
                    this.txtG.Text = "-999999999";
            }
        }
        private void TxtG_KeyPress(object? sender, KeyPressEventArgs e)
        {
            InputValidator.Validate_IsNumeric_Negative(e);
        }
        private void TxtQ_TextChanged(object? sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(this.txtQ.Text))
                this.txtQ.Text = "1";
        }
        private void TxtQ_KeyPress(object? sender, KeyPressEventArgs e)
        {
            InputValidator.Validate_IsNumeric_NonNegative(e);
            this.txtQ.Text = InputValidator.LimitTo_ReasonableSizedNumber(this.txtQ.Text);
        }
        private void TxtS_TextChanged(object? sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(this.txtS.Text))
                this.txtS.Text = "1";
        }
        private void TxtS_KeyPress(object? sender, KeyPressEventArgs e)
        {
            InputValidator.Validate_IsNumeric_NonNegative(e);
            this.txtS.Text = InputValidator.LimitTo_ReasonableSizedNumber(this.txtS.Text);
        }

        private void Txta0_KeyPress(object? sender, KeyPressEventArgs e)
        {
            InputValidator.Validate_IsNumeric_Negative(e);
        }
        private void Txta1_KeyPress(object? sender, KeyPressEventArgs e)
        {
            InputValidator.Validate_IsNumeric_Negative(e);
        }
        private void Txta2_KeyPress(object? sender, KeyPressEventArgs e)
        {
            InputValidator.Validate_IsNumeric_Negative(e);
        }
        private void Txtb0_KeyPress(object? sender, KeyPressEventArgs e)
        {
            InputValidator.Validate_IsNumeric_Negative(e);
        }
        private void Txtb1_KeyPress(object? sender, KeyPressEventArgs e)
        {
            InputValidator.Validate_IsNumeric_Negative(e);
        }
        private void Txtb2_KeyPress(object? sender, KeyPressEventArgs e)
        {
            InputValidator.Validate_IsNumeric_Negative(e);
        }

        #endregion

        protected void btnApplyCo_Click(object? sender, EventArgs e)
        {
            try
            {
                var a0 = double.Parse(this.txta0.Text);
                var a1 = double.Parse(this.txta1.Text);
                var a2 = double.Parse(this.txta2.Text);
                var b0 = double.Parse(this.txtb0.Text);
                var b1 = double.Parse(this.txtb1.Text);
                var b2 = double.Parse(this.txtb2.Text);
                this.BiQuad.SetCoefficients(a0, a1, a2, b0, b1, b2);
            }
            catch (Exception ex)
            {
                this.Error(ex);
            }
        }

        protected void btnApply_Click(object? sender, System.EventArgs e)
        {
            try
            {
                var r = (double)Program.DSP_Info.InSampleRate;
                var f = double.Parse(this.txtF.Text);
                var s = double.Parse(this.txtS.Text);
                var q = double.Parse(this.txtQ.Text);
                var g = double.Parse(this.txtG.Text);
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
                        throw new InvalidOperationException("FilterType not defined");
                }

                this.txta0.Text = this.BiQuad.aa0.ToString();
                this.txta1.Text = this.BiQuad.aa1.ToString();
                this.txta2.Text = this.BiQuad.aa2.ToString();
                this.txtb0.Text = this.BiQuad.b0.ToString();
                this.txtb1.Text = this.BiQuad.b1.ToString();
                this.txtb2.Text = this.BiQuad.b2.ToString();
            }
            catch (Exception ex)
            {
                this.Error(ex);
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

                this.txta0.Text = biQuad.aa0.ToString();
                this.txta1.Text = biQuad.aa1.ToString();
                this.txta2.Text = biQuad.aa2.ToString();
                this.txtb0.Text = biQuad.b0.ToString();
                this.txtb1.Text = biQuad.b1.ToString();
                this.txtb2.Text = biQuad.b2.ToString();

                this.txtF.Text = biQuad.Frequency.ToString();
                this.txtS.Text = biQuad.Slope.ToString();
                this.txtQ.Text = biQuad.Q.ToString();
                this.txtG.Text = biQuad.Gain.ToString();
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
}