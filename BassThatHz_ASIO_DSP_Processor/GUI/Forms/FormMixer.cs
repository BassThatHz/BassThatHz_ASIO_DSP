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
public partial class FormMixer : Form
{
    #region Public Callbacks
    public Action? ClearAllFilterElements;
    public Action<List<MixerInput>>? AddRangeOfFilterElements;
    #endregion

    #region Variables
    protected List<MixerElement> OriginalMixerElements = new();
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
        this.OriginalMixerElements = this.MixerElements.Select(item =>
        {
            var newElement = new MixerElement();
            newElement.Get_txtChAttenuation.Text = item.Get_txtChAttenuation.Text;
            newElement.Get_txtStreamAttenuation.Text = item.Get_txtStreamAttenuation.Text;
            newElement.Get_chkChannel.Checked = item.Get_chkChannel.Checked;
            return newElement;
        }).ToList();

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
        foreach (var MixerInput1 in this.MixerInputs)
        {
            foreach (var MixerInput2 in this.OriginalMixerInputs)
            {
                if (MixerInput1.ChannelIndex == MixerInput2.ChannelIndex)
                {
                    MixerInput1.Attenuation = MixerInput2.Attenuation;
                    MixerInput1.StreamAttenuation = MixerInput2.StreamAttenuation;
                    MixerInput1.Enabled = MixerInput2.Enabled;

                    var MixerIndex = this.MixerInputs.IndexOf(MixerInput1);
                    var MixerElement = this.MixerElements[MixerIndex]; //1:1 mapping
                    var OriginalMixerElement = this.OriginalMixerElements[MixerIndex]; //1:1 mapping
                    MixerElement.Get_txtChAttenuation.Text = OriginalMixerElement.Get_txtChAttenuation.Text;
                    MixerElement.Get_txtStreamAttenuation.Text = OriginalMixerElement.Get_txtStreamAttenuation.Text;
                    MixerElement.Get_chkChannel.Checked = OriginalMixerElement.Get_chkChannel.Checked;

                    break;
                }
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

        foreach (var MixerInput1 in this.MixerInputs)
        {
            foreach (var MixerInput2 in input)
            {
                if (MixerInput1.ChannelIndex == MixerInput2.ChannelIndex)
                {
                    MixerInput1.Attenuation = MixerInput2.Attenuation;
                    MixerInput1.StreamAttenuation = MixerInput2.StreamAttenuation;
                    MixerInput1.Enabled = MixerInput2.Enabled;

                    var MixerIndex = this.MixerInputs.IndexOf(MixerInput1);
                    var MixerElement = this.MixerElements[MixerIndex]; //1:1 mapping
                    MixerElement.Get_txtChAttenuation.Text = MixerInput2.Attenuation.ToString();
                    MixerElement.Get_txtStreamAttenuation.Text = MixerInput2.StreamAttenuation.ToString();
                    MixerElement.Get_chkChannel.Checked = MixerInput2.Enabled;

                    break;
                }
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
            try
            {
                mixerInput.Attenuation = -Math.Abs(double.Parse(mixerElement.Get_txtChAttenuation.Text));
            }
            catch { }

            try
            {
                mixerInput.StreamAttenuation = -Math.Abs(double.Parse(mixerElement.Get_txtStreamAttenuation.Text));
            }
            catch { }
        };

        mixerElement.Get_txtChAttenuation.TextChanged += (s, e) =>
        {
            this.HasChangesBeenSaved = false;
            try
            {
                mixerInput.Attenuation = -Math.Abs(double.Parse(mixerElement.Get_txtChAttenuation.Text));
            }
            catch { }
        };

        mixerElement.Get_txtStreamAttenuation.TextChanged += (s, e) =>
        {
            this.HasChangesBeenSaved = false;
            try
            {
                mixerInput.StreamAttenuation = -Math.Abs(double.Parse(mixerElement.Get_txtStreamAttenuation.Text));
            }
            catch { }
        };

        mixerElement.Get_txtChAttenuation.Text = Math.Round(mixerInput.Attenuation, 4).ToString();
        mixerElement.Get_txtStreamAttenuation.Text = Math.Round(mixerInput.StreamAttenuation, 4).ToString();
        mixerElement.Get_chkChannel.Checked = mixerInput.Enabled;
    }

    protected void ClearGUI()
    {
        this.MixerElements.Clear();
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