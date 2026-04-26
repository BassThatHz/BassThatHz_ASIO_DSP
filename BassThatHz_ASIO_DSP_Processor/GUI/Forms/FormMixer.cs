#nullable enable

namespace BassThatHz_ASIO_DSP_Processor.GUI.Forms;

#region Usings
using Controls.Filters;
using NAudio.Wave.Asio;
using System;
using System.Collections.Generic;
using System.Drawing;
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
public partial class FormMixer : Form
{
    #region Public Callbacks
    public Action? ClearAllFilterElements;
    public Action<List<MixerInput>>? AddRangeOfFilterElements;
    #endregion

    #region Variables
    // Lightweight snapshot of UI element state to avoid keeping extra control instances in memory
    protected readonly struct MixerElementSnapshot
    {
        public readonly string ChAttenuationText;
        public readonly string StreamAttenuationText;
        public readonly bool Checked;

        public MixerElementSnapshot(string ch, string stream, bool @checked)
        {
            ChAttenuationText = ch;
            StreamAttenuationText = stream;
            Checked = @checked;
        }
    }

    protected List<MixerElementSnapshot> OriginalMixerElements = new();
    protected List<MixerInput> OriginalMixerInputs = new();

    protected List<MixerElement> MixerElements = new();
    protected List<MixerInput> MixerInputs = new();
    protected bool HasChangesBeenSaved = true;
    #endregion

    #region Constructor
    public FormMixer()
    {
        InitializeComponent();
        try
        {
            this.FormClosing += FormMixer_FormClosing;
            this.SizeChanged += FormMixer_SizeChanged;
            this.RedrawPanelItems();
            this.PersistentDeepClone();
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }
    #endregion

    #region Event Handlers
    protected void FormMixer_SizeChanged(object? sender, EventArgs e)
    {
        try
        {
            this.Width = 1021;
            if (this.Height < 145)
                this.Height = 145;
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }

    protected void FormMixer_FormClosing(object? sender, FormClosingEventArgs e)
    {
        try
        {
            if (!this.HasChangesBeenSaved)
            {
                var result = MessageBox.Show("Would you like to apply the changes? (No will discard the changes)", "Apply Changes?",
                                    MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.Yes)
                {
                    this.ApplyChanges();
                }
                else
                {
                    this.RevertToOrignal();
                    this.HasChangesBeenSaved = true;
                }
            }
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }

    protected void btn_SelectAll_Click(object sender, EventArgs e)
    {
        try
        {
            this.HasChangesBeenSaved = false;
            foreach (var item in this.MixerElements)
                item.Get_chkChannel.Checked = true;
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }

    protected void btnClearSelection_Click(object sender, EventArgs e)
    {
        try
        {
            this.HasChangesBeenSaved = false;
            foreach (var item in this.MixerElements)
                item.Get_chkChannel.Checked = false;
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }

    protected void btnInvertSelection_Click(object sender, EventArgs e)
    {
        try
        {
            this.HasChangesBeenSaved = false;
            foreach (var item in this.MixerElements)
                item.Get_chkChannel.Checked = !item.Get_chkChannel.Checked;
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }

    protected void btnApply_Click(object sender, EventArgs e)
    {
        try
        {
            this.ApplyChanges();
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }

    protected void btnRefreshList_Click(object sender, EventArgs e)
    {
        try
        {
            var result = MessageBox.Show("This discards changes. Do you want to continue?", "Discard Changes?",
                                    MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
            if (result == DialogResult.OK)
            {
                this.HasChangesBeenSaved = false;
                this.RedrawPanelItems();
            }
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }
    #endregion

    #region Protected Functions

    protected void ApplyChanges()
    {
        this.ClearAllFilterElements?.Invoke();
        this.AddRangeOfFilterElements?.Invoke(this.MixerInputs);
        this.PersistentDeepClone();
        this.HasChangesBeenSaved = true;
    }

    protected void PersistentDeepClone()
    {
        // Capture only the minimal state required to restore UI and inputs later.
        this.OriginalMixerElements = this.MixerElements.Select(item =>
            new MixerElementSnapshot(
                item.Get_txtChAttenuation.Text,
                item.Get_txtStreamAttenuation.Text,
                item.Get_chkChannel.Checked
            )).ToList();

        // Deep copy of MixerInputs values (keep simple DTO copies)
        this.OriginalMixerInputs = this.MixerInputs.Select(item => new MixerInput
        {
            Attenuation = item.Attenuation,
            StreamAttenuation = item.StreamAttenuation,
            Enabled = item.Enabled,
            ChannelIndex = item.ChannelIndex,
            ChannelName = item.ChannelName
        }).ToList();
    }

    protected void RevertToOrignal()
    {
        // Build a lookup for original inputs by ChannelIndex for O(1) access
        var originalByChannel = this.OriginalMixerInputs.ToDictionary(mi => mi.ChannelIndex);

        // MixerInputs and MixerElements are expected to be 1:1 mapped by index
        for (int i = 0; i < this.MixerInputs.Count && i < this.MixerElements.Count && i < this.OriginalMixerElements.Count; i++)
        {
            var currentInput = this.MixerInputs[i];
            if (originalByChannel.TryGetValue(currentInput.ChannelIndex, out var originalInput))
            {
                currentInput.Attenuation = originalInput.Attenuation;
                currentInput.StreamAttenuation = originalInput.StreamAttenuation;
                currentInput.Enabled = originalInput.Enabled;

                var element = this.MixerElements[i]; // 1:1 mapping
                var snapshot = this.OriginalMixerElements[i];
                element.Get_txtChAttenuation.Text = snapshot.ChAttenuationText;
                element.Get_txtStreamAttenuation.Text = snapshot.StreamAttenuationText;
                element.Get_chkChannel.Checked = snapshot.Checked;
            }
        }
    }

    protected void RedrawPanelItems()
    {
        this.MixerInputs.Clear();
        this.ClearGUI();

        if (string.IsNullOrEmpty(Program.DSP_Info.ASIO_InputDevice))
            return;

        AsioDriverCapability? Capabilities = null;
        try
        {
            Capabilities = Program.ASIO.GetDriverCapabilities(Program.DSP_Info.ASIO_InputDevice);
        }
        catch (Exception ex)
        {
            _ = ex;
            //throw new InvalidOperationException("Can't fetch Driver Capabilities", ex);
        }
        if (Capabilities == null)
            return;

        int i = 0;
        foreach (var item in Capabilities.Value.InputChannelInfos)
        {
            var tempMixerElement = this.CreateMixerElement(item, i);
            var tempMixerInput = this.CreateMixerInput(item.channel, item.name);
            this.CreateMixerElementEventHandlers(tempMixerInput, tempMixerElement);
            i++;
        }
    }

    public void RedrawPanelItemsFromLoader(List<MixerInput> input)
    {
        this.RedrawPanelItems();

        // Use a lookup to avoid O(n^2) nested loops
        var loaderByChannel = input.ToDictionary(mi => mi.ChannelIndex);
        for (int i = 0; i < this.MixerInputs.Count && i < this.MixerElements.Count; i++)
        {
            var current = this.MixerInputs[i];
            if (loaderByChannel.TryGetValue(current.ChannelIndex, out var loaded))
            {
                current.Attenuation = loaded.Attenuation;
                current.StreamAttenuation = loaded.StreamAttenuation;
                current.Enabled = loaded.Enabled;

                var element = this.MixerElements[i]; //1:1 mapping
                element.Get_txtChAttenuation.Text = loaded.Attenuation.ToString();
                element.Get_txtStreamAttenuation.Text = loaded.StreamAttenuation.ToString();
                element.Get_chkChannel.Checked = loaded.Enabled;
            }
        }

        this.ApplyChanges();
    }

    protected MixerInput CreateMixerInput(int channelIndex, string channelName)
    {
        var ReturnValue = new MixerInput()
        {
            ChannelIndex = channelIndex,
            ChannelName = channelName
        };
        this.MixerInputs.Add(ReturnValue);
        return ReturnValue;
    }

    protected void CreateMixerElementEventHandlers(MixerInput mixerInput, MixerElement mixerElement)
    {
        mixerElement.Get_chkChannel.CheckedChanged += (s, e) =>
        {
            this.HasChangesBeenSaved = false;
            mixerInput.Enabled = mixerElement.Get_chkChannel.Checked;
            if (double.TryParse(mixerElement.Get_txtChAttenuation.Text, out var chVal))
                mixerInput.Attenuation = -Math.Abs(chVal);
            if (double.TryParse(mixerElement.Get_txtStreamAttenuation.Text, out var stVal))
                mixerInput.StreamAttenuation = -Math.Abs(stVal);
        };

        mixerElement.Get_txtChAttenuation.TextChanged += (s, e) =>
        {
            this.HasChangesBeenSaved = false;
            if (double.TryParse(mixerElement.Get_txtChAttenuation.Text, out var chVal))
                mixerInput.Attenuation = -Math.Abs(chVal);
        };

        mixerElement.Get_txtStreamAttenuation.TextChanged += (s, e) =>
        {
            this.HasChangesBeenSaved = false;
            if (double.TryParse(mixerElement.Get_txtStreamAttenuation.Text, out var stVal))
                mixerInput.StreamAttenuation = -Math.Abs(stVal);
        };

        mixerElement.Get_txtChAttenuation.Text = Math.Round(mixerInput.Attenuation, 4).ToString();
        mixerElement.Get_txtStreamAttenuation.Text = Math.Round(mixerInput.StreamAttenuation, 4).ToString();
        mixerElement.Get_chkChannel.Checked = mixerInput.Enabled;
    }

    protected void ClearGUI()
    {
        if (this.MixerElements.Count > 0)
        {
            // Remove controls from panel and dispose them to free resources and event handlers
            foreach (var ctrl in this.MixerElements)
            {
                try
                {
                    this.panel1.Controls.Remove(ctrl);
                    ctrl.Dispose();
                }
                catch { }
            }
            this.MixerElements.Clear();
        }

        // Clear inputs and ensure panel is empty
        this.MixerInputs.Clear();
        this.panel1.Controls.Clear();
    }

    protected MixerElement CreateMixerElement(AsioChannelInfo info, int controlIndex)
    {
        var ReturnValue = new MixerElement();
        this.SetTextFromASIO(ReturnValue.Get_chkChannel, info);
        this.SetLocation(ReturnValue, controlIndex);

        this.panel1.Controls.Add(ReturnValue);
        this.MixerElements.Add(ReturnValue);
        return ReturnValue;
    }

    protected void SetTextFromASIO(Control input, AsioChannelInfo info)
    {
        input.Text = $"({info.channel}) {info.name}";
    }

    protected void SetLocation(Control input, int controlIndex)
    {
        var ElementsPerWidth = 2;
        var ColumnSpacing = 100;
        var LeftMargin = 20;
        var TopMargin = 15;

        var x = input.Width * (controlIndex % ElementsPerWidth) + LeftMargin;
        // If the control is in the second column, add the spacing
        if (controlIndex % ElementsPerWidth == 1)
            x += ColumnSpacing;

        var y = controlIndex / ElementsPerWidth * (input.Height + TopMargin);

        input.Location = new Point(x, y);
    }
    #endregion

    #region Error Handling
    protected void Error(Exception ex)
    {
        Debug.Error(ex);
    }
    #endregion
}