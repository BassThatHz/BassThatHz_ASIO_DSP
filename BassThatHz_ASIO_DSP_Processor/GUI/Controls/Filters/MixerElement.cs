#nullable enable

namespace BassThatHz_ASIO_DSP_Processor.GUI.Controls.Filters
{
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
    public partial class MixerElement : UserControl
    {
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

        #region Event Handlers

        #region InputValidation
        protected void txtChAttenuation_KeyPress(object? sender, KeyPressEventArgs e)
        {
            InputValidator.Validate_IsNumeric_Negative(e);
        }

        protected void txtChAttenuation_TextChanged(object? sender, System.EventArgs e)
        {
            if (double.TryParse(this.txtChAttenuation.Text, out double result) && result > 0)
                this.txtChAttenuation.Text = "0";

            //Negative symbol must be at the start of the string
            if (this.txtChAttenuation.Text.Contains("-") && !this.txtChAttenuation.Text.StartsWith("-"))
                this.txtChAttenuation.Text = "-" + this.txtChAttenuation.Text.Replace("-", "");
        }

        protected void txtStreamAttenuation_KeyPress(object? sender, KeyPressEventArgs e)
        {
            InputValidator.Validate_IsNumeric_Negative(e);
        }

        protected void txtStreamAttenuation_TextChanged(object? sender, System.EventArgs e)
        {
            if (double.TryParse(this.txtStreamAttenuation.Text, out double result) && result > 0)
                this.txtStreamAttenuation.Text = "0";

            //Negative symbol must be at the start of the string
            if (this.txtStreamAttenuation.Text.Contains("-") && !this.txtStreamAttenuation.Text.StartsWith("-"))
                this.txtStreamAttenuation.Text = "-" + this.txtStreamAttenuation.Text.Replace("-", "");
        }
        #endregion

        #endregion
    }
}