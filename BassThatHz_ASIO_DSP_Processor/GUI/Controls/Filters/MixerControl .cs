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
public partial class MixerControl : UserControl, IFilterControl
{
    #region Variables
    protected Mixer Filter = new();
    // Lazy-create the mixer form to avoid allocating a potentially heavy form
    // until the user actually requests configuration. Keep the field private
    // so we can control its lifecycle and dispose it when the control is disposed.
    private FormMixer? _mixerForm;
    private FormMixer MixerForm => _mixerForm ??= CreateMixerForm();
    #endregion

    #region Helpers
    private FormMixer CreateMixerForm()
    {
        var fm = new FormMixer();
        // Attach callbacks so the form and control interact correctly
        AttachMixerFormCallbacks(fm);
        // When the form is disposed we should release our reference so it can be GC'd
        fm.Disposed += (s, e) => _mixerForm = null;
        return fm;
    }

    #endregion

    #region Constructor
    public MixerControl()
    {
        InitializeComponent();
        // Ensure any lazily created form is disposed when this control is disposed.
        this.Disposed += (s, e) => { _mixerForm?.Dispose(); _mixerForm = null; };
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
        // Call AttachMixerFormCallbacks when the form is created. This method intentionally
        // does not reference the MixerForm to avoid creating it during control construction.
    }

    private void AttachMixerFormCallbacks(FormMixer form)
    {
        // Clear cached filter elements
        form.ClearAllFilterElements = () => this.Filter.MixerInputs.Clear();

        // Add a range of elements. Optimize to minimize allocations and UI updates.
        form.AddRangeOfFilterElements = (MixerInputs) =>
        {
            // Build the enabled list with a single pass and reuse capacity where possible.
            var enabledList = new System.Collections.Generic.List<MixerInput>();
            foreach (var item in MixerInputs)
            {
                if (!item.Enabled)
                    continue;

                enabledList.Add(new MixerInput
                {
                    Attenuation = item.Attenuation,
                    StreamAttenuation = item.StreamAttenuation,
                    Enabled = item.Enabled,
                    ChannelIndex = item.ChannelIndex,
                    ChannelName = item.ChannelName
                });
            }

            // Replace filter inputs with the filtered list
            this.Filter.MixerInputs = enabledList;

            // Refresh the listbox in a single update to avoid repeated UI work
            if (this.listBox1 != null)
            {
                this.listBox1.BeginUpdate();
                try
                {
                    this.listBox1.Items.Clear();
                    if (enabledList.Count > 0)
                    {
                        var display = enabledList.Select(item => $"({item.ChannelIndex}) {item.ChannelName} : {item.Attenuation} | {item.StreamAttenuation}").ToArray();
                        this.listBox1.Items.AddRange(display);
                    }
                }
                finally { this.listBox1.EndUpdate(); }
            }
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