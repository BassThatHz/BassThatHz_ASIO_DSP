#nullable enable

namespace BassThatHz_ASIO_DSP_Processor.GUI.Controls
{
    using DSP.Filters;
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
    public partial class AuxSetControl : UserControl, IFilterControl
    {
        #region Variables
        protected AuxSet Filter = new();
        #endregion

        #region Constructor
        public AuxSetControl()
        {
            InitializeComponent();

            var NumberOfAuxBuffers = new DSP_Stream().NumberOfAuxBuffers;
            for (var i = 1; i < NumberOfAuxBuffers + 1; i++)
                this.cbo_AuxToSet.Items.Add(i.ToString());
            this.cbo_AuxToSet.SelectedIndex = 0;
        }
        #endregion

        #region Event Handlers
        protected void chk_MuteAfter_CheckedChanged(object sender, EventArgs e)
        {
            this.Filter.MuteAfter = this.chk_MuteAfter.Checked;
        }

        protected void cbo_AuxToSet_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.Filter.AuxSetIndex = this.cbo_AuxToSet.SelectedIndex;
        }
        #endregion

        #region Interfaces
        public IFilter GetFilter =>
            this.Filter;

        public void ApplySettings()
        {
            this.Filter.ApplySettings();
        }

        public void SetDeepClonedFilter(IFilter input)
        {
            if (input is AuxSet auxSet)
            {
                this.Filter = auxSet;
                this.chk_MuteAfter.Checked = this.Filter.MuteAfter;

                try
                {
                    this.cbo_AuxToSet.SelectedIndex = this.Filter.AuxSetIndex;
                }
                catch(Exception)
                {
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
}