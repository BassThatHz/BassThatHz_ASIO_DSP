#nullable enable

namespace BassThatHz_ASIO_DSP_Processor.GUI.Controls
{
    using System;
    using System.Data;
    using System.Linq;
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
    public partial class Basic_HPF_LPFControl : UserControl, IFilterControl
    {
        #region Variables
        protected Basic_HPF_LPF Filter = new();
        #endregion

        #region Constructor and MapEventHandlers
        public Basic_HPF_LPFControl()
        {
            InitializeComponent();

            this.cboHPF.Items.Clear();
            this.cboLPF.Items.Clear();
            var EnumArray = Enum.GetValues(typeof(Basic_HPF_LPF.FilterOrder)).Cast<object>().ToArray();
            this.cboHPF.Items.AddRange(EnumArray);
            this.cboHPF.SelectedIndex = 0;
            this.cboLPF.Items.AddRange(EnumArray);
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
        private void TxtHPFFreq_KeyPress(object? sender, KeyPressEventArgs e)
        {
            InputValidator.Validate_IsNumeric_NonNegative(e);
            this.txtHPFFreq.Text = InputValidator.LimitTo_ReasonableSizedNumber(this.txtHPFFreq.Text);
        }

        private void TxtLPFFreq_KeyPress(object? sender, KeyPressEventArgs e)
        {
            InputValidator.Validate_IsNumeric_NonNegative(e);
            this.txtLPFFreq.Text = InputValidator.LimitTo_ReasonableSizedNumber(this.txtLPFFreq.Text);
        }

        private void TxtLPFFreq_TextChanged(object? sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(this.txtLPFFreq.Text))
                this.txtLPFFreq.Text = "1";

            if (double.TryParse(this.txtLPFFreq.Text, out double result))
            {
                if (result > Program.DSP_Info.InSampleRate * 0.5)
                    this.txtLPFFreq.Text = (Program.DSP_Info.InSampleRate * 0.5).ToString();
                else if (result == 0 || result <= double.Epsilon)
                    this.txtLPFFreq.Text = "0.01";
            }
        }

        private void TxtHPFFreq_TextChanged(object? sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(this.txtHPFFreq.Text))
                this.txtHPFFreq.Text = "1";

            if (double.TryParse(this.txtHPFFreq.Text, out double result))
            {
                if (result > Program.DSP_Info.InSampleRate * 0.5)
                    this.txtHPFFreq.Text = (Program.DSP_Info.InSampleRate * 0.5).ToString();
                else if (result == 0|| result <= double.Epsilon)
                    this.txtHPFFreq.Text = "0.01";
            }
        }
        #endregion

        protected void btnApply_Click(object? sender, EventArgs e)
        {
            try
            {
                this.Filter.HPFFreq = double.Parse(this.txtHPFFreq.Text);
                if (this.cboHPF.SelectedItem != null)
                    this.Filter.HPFFilter = (Basic_HPF_LPF.FilterOrder)this.cboHPF.SelectedItem;

                this.Filter.LPFFreq = double.Parse(this.txtLPFFreq.Text);
                if (this.cboLPF.SelectedItem != null)
                    this.Filter.LPFFilter = (Basic_HPF_LPF.FilterOrder)this.cboLPF.SelectedItem;

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
            this.txtBiQuads.Text = string.Empty;
            for (int i = 0; i < 8; i++)
            {
                var BiQuad = this.Filter.BiQuads[i];

                double Normalized = this.cboShowNormalized.Checked && BiQuad != null? BiQuad.aa0 : 1;

                var a0 = "a0=" + (BiQuad?.aa0 / Normalized).ToString() + ",\r\n";
                var a1 = "a1=" + (BiQuad?.aa1 / Normalized).ToString() + ",\r\n";
                var a2 = "a2=" + (BiQuad?.aa2 / Normalized).ToString() + ",\r\n";
                var b0 = "b0=" + (BiQuad?.b0 / Normalized).ToString() + ",\r\n";
                var b1 = "b1=" + (BiQuad?.b1 / Normalized).ToString() + ",\r\n";
                var b2 = "b2=" + (BiQuad?.b2 / Normalized).ToString() + ",\r\n";

                double Q = i < 4 ? this.Filter.Q_Array_HPF[i] : this.Filter.Q_Array_LPF[i - 4];
                this.txtBiQuads.Text += "biquad"+ (i + 1) + " "  + BiQuad?.SampleRate + " q" + Q + ",\r\n";
                this.txtBiQuads.Text += a0;
                this.txtBiQuads.Text += a1;
                this.txtBiQuads.Text += a2;
                this.txtBiQuads.Text += b0;
                this.txtBiQuads.Text += b1;
                this.txtBiQuads.Text += b2;
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
}