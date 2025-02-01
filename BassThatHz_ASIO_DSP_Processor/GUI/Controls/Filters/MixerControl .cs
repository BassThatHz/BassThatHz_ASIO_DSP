#nullable enable

namespace BassThatHz_ASIO_DSP_Processor.GUI.Controls;

#region Usings
using GUI.Forms;
using System;
using System.Linq;
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
public partial class MixerControl : UserControl, IFilterControl
{
    #region Variables
    protected Mixer Filter = new();
    protected FormMixer MixerForm = new();
    #endregion

    #region Constructor
    public MixerControl()
    {
        InitializeComponent();
        this.Create_MixerFormCallbacks();
    }
    #endregion

    #region Event Handlers
    protected void btnConfigMixer_Click(object sender, EventArgs e)
    {
        //Make it tall
        if (this.ParentForm != null)
            this.MixerForm.Height = this.ParentForm.Height - 22;

        // Display the form as a modal dialog, only one instance per constructor is ever created.
        this.MixerForm.ShowDialog();
    }
    #endregion

    #region Protected Functions
    protected void Create_MixerFormCallbacks()
    {
        this.MixerForm.ClearAllFilterElements = () =>
                    this.Filter.MixerInputs.Clear();

        this.MixerForm.AddRangeOfFilterElements = (MixerInputs) =>
        {
            //Apply only the Enabled Mixer Inputs for DSP
            this.Filter.MixerInputs = MixerInputs.Where(item => item.Enabled).Select(item => new MixerInput
            {
                Attenuation = item.Attenuation,
                StreamAttenuation = item.StreamAttenuation,
                Enabled = item.Enabled,
                ChannelIndex = item.ChannelIndex,
                ChannelName = item.ChannelName
            }).ToList();

            //Refresh the listbox items
            this.listBox1.Items.Clear();
            foreach (var item in MixerInputs)
                if (item.Enabled)
                    this.listBox1.Items.Add($"({item.ChannelIndex}) {item.ChannelName} : {item.Attenuation} | {item.StreamAttenuation}");
        };
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
        if (input is Mixer mixer)
        {
            this.Filter = mixer;
            this.MixerForm.RedrawPanelItemsFromLoader(mixer.MixerInputs);
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