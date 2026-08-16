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

    /// <summary>
    /// Saved routing that has no live ASIO channel to bind to on this machine - either because the
    /// configured input device is absent entirely, or because the device that IS present does not
    /// expose that ChannelIndex.
    /// <para>
    /// These entries deliberately get NO row in the mixer panel (there is no hardware channel to
    /// show), but they are carried back out through <see cref="ApplyChanges"/> so that merely
    /// opening a config on the "wrong" machine cannot silently delete the user's routing - which,
    /// once saved, would destroy it on disk permanently.
    /// </para>
    /// <para>
    /// Matching is keyed on ChannelIndex only. ChannelName is hardware-derived and is not a
    /// reliable round-trip value, so it is never used to identify an entry.
    /// </para>
    /// </summary>
    protected List<MixerInput> UnbackedMixerInputs = new();

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
                //Suppressed (non-interactive/test) default is No: an unattended close must not
                //silently commit unconfirmed changes to the live DSP config.
                var result = Debug.ShowMessage("Would you like to apply the changes? (No will discard the changes)", "Apply Changes?",
                                    MessageBoxButtons.YesNo, MessageBoxIcon.Question, DialogResult.No);
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
            //Suppressed (non-interactive/test) default is OK: the refresh was explicitly requested.
            var result = Debug.ShowMessage("This discards changes. Do you want to continue?", "Discard Changes?",
                                    MessageBoxButtons.OKCancel, MessageBoxIcon.Question, DialogResult.OK);
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
        this.AddRangeOfFilterElements?.Invoke(this.GetAllMixerInputs());
        this.PersistentDeepClone();
        this.HasChangesBeenSaved = true;
    }

    /// <summary>
    /// The full set of routing to push back into the filter: the rows currently shown in the panel,
    /// followed by any saved-but-unbacked entries being preserved for this session.
    /// </summary>
    /// <remarks>
    /// When nothing is unbacked - the normal case, where the configured device is fully present -
    /// this returns the very same list instance as before, so the device-present path is byte-for-byte
    /// unchanged. Unbacked entries are appended last; their relative order against the live rows is
    /// irrelevant because <see cref="Mixer.Transform"/> skips any ChannelIndex that has no input
    /// buffer, so they contribute no audio while their device is missing.
    /// </remarks>
    /// <returns>The combined list of mixer inputs.</returns>
    protected List<MixerInput> GetAllMixerInputs()
    {
        if (this.UnbackedMixerInputs.Count == 0)
            return this.MixerInputs;

        var Local_Combined = new List<MixerInput>(this.MixerInputs.Count + this.UnbackedMixerInputs.Count);
        Local_Combined.AddRange(this.MixerInputs);
        Local_Combined.AddRange(this.UnbackedMixerInputs);
        return Local_Combined;
    }

    /// <summary>
    /// Drops any preserved entry whose ChannelIndex is now backed by a live panel row, so that a
    /// device coming back (or a Refresh List) can never produce a duplicate for the same channel.
    /// </summary>
    protected void PruneUnbackedMixerInputs()
    {
        if (this.UnbackedMixerInputs.Count == 0)
            return;

        var Local_LiveChannels = new HashSet<int>();
        for (int i = 0; i < this.MixerInputs.Count; i++)
            Local_LiveChannels.Add(this.MixerInputs[i].ChannelIndex);

        this.UnbackedMixerInputs.RemoveAll(mi => Local_LiveChannels.Contains(mi.ChannelIndex));
    }

    /// <remarks>
    /// <see cref="UnbackedMixerInputs"/> is deliberately NOT snapshotted: it has no UI rows, so
    /// nothing the user does in this form can edit it, and <see cref="RevertToOrignal"/> therefore
    /// leaves it intact - which is what preserving it means.
    /// </remarks>
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

    /// <summary>
    /// Fetches the input channels exposed by the ASIO device named in the current config, or null
    /// when no device is configured, the device is not present on this machine, or the driver
    /// refuses to report its capabilities.
    /// Virtual so tests can supply a synthetic channel list without real hardware.
    /// </summary>
    /// <returns>The live input channels, or null when they cannot be determined.</returns>
    protected virtual AsioChannelInfo[]? GetLiveInputChannels()
    {
        if (string.IsNullOrEmpty(Program.DSP_Info.ASIO_InputDevice))
            return null;

        AsioDriverCapability? Local_Capabilities = null;
        try
        {
            Local_Capabilities = Program.ASIO.GetDriverCapabilities(Program.DSP_Info.ASIO_InputDevice);
        }
        catch (Exception ex)
        {
            //A missing / busy / broken driver is an expected condition here, not a fault: the user
            //may simply be editing a config on a machine that does not have the device. Record it
            //rather than discarding it so the cause is still observable.
            Debug.ReportSwallowed(ex);
        }

        return Local_Capabilities?.InputChannelInfos;
    }

    protected void RedrawPanelItems()
    {
        this.MixerInputs.Clear();
        this.ClearGUI();

        var Local_Channels = this.GetLiveInputChannels();
        if (Local_Channels == null)
            return;

        int i = 0;
        foreach (var item in Local_Channels)
        {
            var tempMixerElement = this.CreateMixerElement(item, i);
            var tempMixerInput = this.CreateMixerInput(item.channel, item.name);
            this.CreateMixerElementEventHandlers(tempMixerInput, tempMixerElement);
            i++;
        }

        this.PruneUnbackedMixerInputs();
    }

    /// <summary>
    /// Rebuilds the mixer panel from the live ASIO channel list and overlays the routing loaded from
    /// a config file onto it.
    /// </summary>
    /// <remarks>
    /// DEFECT FIX: this used to overlay onto <see cref="MixerInputs"/> and then hand ONLY that list
    /// to <see cref="ApplyChanges"/>. On a machine where the configured input device is missing,
    /// <see cref="RedrawPanelItems"/> leaves MixerInputs empty, so ApplyChanges cleared the filter and
    /// re-added nothing - the user's saved routing was destroyed in memory just by loading the config,
    /// and permanently on disk the moment they saved. Saved entries that cannot be bound to a live
    /// channel are now retained in <see cref="UnbackedMixerInputs"/> and passed straight back through,
    /// so load/save is lossless on any host.
    /// </remarks>
    /// <param name="input">The routing loaded from the config.</param>
    public void RedrawPanelItemsFromLoader(List<MixerInput> input)
    {
        //A fresh config supersedes anything preserved from a previous load.
        this.UnbackedMixerInputs.Clear();

        this.RedrawPanelItems();

        // Use a lookup to avoid O(n^2) nested loops
        var loaderByChannel = input.ToDictionary(mi => mi.ChannelIndex);
        var Local_LiveChannels = new HashSet<int>();
        for (int i = 0; i < this.MixerInputs.Count && i < this.MixerElements.Count; i++)
        {
            var current = this.MixerInputs[i];
            Local_LiveChannels.Add(current.ChannelIndex);
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

        //Retain, in their saved order, every entry that has no live channel to bind to. Copies are
        //taken because ApplyChanges below clears the caller's list via ClearAllFilterElements.
        foreach (var Local_Saved in input)
        {
            if (Local_LiveChannels.Contains(Local_Saved.ChannelIndex))
                continue;

            this.UnbackedMixerInputs.Add(new MixerInput
            {
                Attenuation = Local_Saved.Attenuation,
                StreamAttenuation = Local_Saved.StreamAttenuation,
                Enabled = Local_Saved.Enabled,
                ChannelIndex = Local_Saved.ChannelIndex,
                ChannelName = Local_Saved.ChannelName
            });
        }

        this.ReportUnbackedMixerInputs();

        this.ApplyChanges();
    }

    /// <summary>
    /// Makes a partial or total channel mismatch observable without blocking the user. This must NOT
    /// use a modal dialog: config loading also runs at startup and under automated tests, where a
    /// dialog would hang the process.
    /// </summary>
    protected void ReportUnbackedMixerInputs()
    {
        if (this.UnbackedMixerInputs.Count == 0)
            return;

        var Local_Channels = string.Join(", ", this.UnbackedMixerInputs.Select(mi => mi.ChannelIndex));
        Debug.ReportSwallowed(new InvalidOperationException(
            $"Mixer: {this.UnbackedMixerInputs.Count} saved input channel(s) ({Local_Channels}) are not present on the " +
            $"current ASIO input device ('{Program.DSP_Info.ASIO_InputDevice}'). Their routing has been PRESERVED and " +
            "will be written back on save, but it is muted until that device is available again."));
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
                //DEFECT FIX: this was a bare 'catch { }'. A failed Remove/Dispose left the control
                //parented to panel1 with its handlers attached while MixerElements.Clear() below
                //dropped the only reference to it - a silent control leak with no trace.
                try
                {
                    this.panel1.Controls.Remove(ctrl);
                    ctrl.Dispose();
                }
                catch (Exception ex)
                {
                    Debug.ReportSwallowed(ex);
                }
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