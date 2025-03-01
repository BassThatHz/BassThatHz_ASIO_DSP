#nullable enable

namespace BassThatHz_ASIO_DSP_Processor.GUI.Tabs;

#region Usings
using Controls;
using NAudio.Wave.Asio;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
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
public partial class ctl_DSPConfigPage : UserControl
{
    #region Variables
    public ObservableCollection<StreamControl> StreamControls = new();
    #endregion

    #region Constructor
    public ctl_DSPConfigPage()
    {
        InitializeComponent();
        this.ResetTabPagesAndStreamControls();
    }
    #endregion

    #region LoadConfigRefresh
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public void LoadConfigRefresh()
    {
        this.ResetTabPagesAndStreamControls();
        foreach (var DSP_Stream in Program.DSP_Info.Streams)
        {
            if (DSP_Stream != null)
            {
                var StreamControl = this.AddStreamControl(DSP_Stream);
                StreamControl.Get_In_Volume.Volume = DSP_Stream.InputVolume;
                StreamControl.Get_Out_Volume.Volume = DSP_Stream.OutputVolume;

                if (StreamControl.Get_cboInputStream.Items.Count > DSP_Stream.InputChannelIndex)
                    StreamControl.Get_cboInputStream.SelectedIndex = DSP_Stream.InputChannelIndex;

                if (StreamControl.Get_cboOutputStream.Items.Count > DSP_Stream.OutputChannelIndex)
                    StreamControl.Get_cboOutputStream.SelectedIndex = DSP_Stream.OutputChannelIndex;

                foreach (var Filter in DSP_Stream.Filters)
                {
                    if (Filter != null)
                    {
                        StreamControl.btnAdd_Click(this, EventArgs.Empty);
                        var LastIndex = StreamControl.FilterControls.Count - 1;
                        var FilterControl = StreamControl.FilterControls[LastIndex];
                        FilterControl.LoadConfigRefresh(Filter);
                    }
                }
            }
        }
    }
    #endregion

    #region Event Handlers
    protected void hScrollBar1_Scroll(object? sender, ScrollEventArgs e)
    {
        try
        {
            this.tabControl1.SelectedIndex = e.NewValue;
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    protected void btnAddStream_Click(object? sender, EventArgs e)
    {
        try
        {
            //Add a new instance of DSP_Stream (this is used by the ASIO Engine directly)
            var DSP_Stream = new DSP_Stream();
            Program.DSP_Info.Streams.Add(DSP_Stream);

            _ = this.AddStreamControl(DSP_Stream);
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    protected void btn_Add_Stream_At_Index_Click(object sender, EventArgs e)
    {
        try
        {
            //Add a new instance of DSP_Stream (this is used by the ASIO Engine directly)
            var DSP_Stream = new DSP_Stream();
            var Streams = Program.DSP_Info.Streams;
            int StreamCount = Streams.Count;

            int Index = int.Parse(this.txt_AddIndex.Text);
            Index--;

            if (Index < 0)
                Index = 0;

            if (Index < StreamCount)
                Streams.Insert(Index, DSP_Stream);
            else
            {
                Streams.Add(DSP_Stream);
                Index = -1;
            }

            _ = this.AddStreamControl(DSP_Stream, Index);

            this.ResetAll_TabPage_IndexText();
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }
    #endregion

    #region Protected Functions
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    protected StreamControl AddStreamControl(DSP_Stream dsp_Stream, int AddAtIndex = -1)
    {
        var StreamControl = new StreamControl();

        TabPage TempTabPage;
        if (AddAtIndex < 0)
        {
            this.StreamControls.Add(StreamControl);
            TempTabPage = this.CreateTabPage(StreamControl);
        }
        else
        {
            this.StreamControls.Insert(AddAtIndex, StreamControl);
            TempTabPage = this.CreateTabPage(StreamControl, AddAtIndex);
        }

        this.AddStreamControlEventHandlers(StreamControl, TempTabPage, dsp_Stream);
        this.Display_Input_Output_Channels(StreamControl);

        //Uses Task Run's to make it more proficient
        _ = Task.Run(() => this.SafeInvoke(() =>
        {
            var StreamControlsCount = this.StreamControls.Count;
            this.Set_HScrollbar(StreamControlsCount);
            this.RefreshStreamCountLabel(StreamControlsCount);
        }));

        _ = Task.Run(() => this.SafeInvoke(() =>
            Program.Form_Monitoring?.CreateStreamVolumeLevelControl(dsp_Stream)
        ));

        return StreamControl;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    protected TabPage CreateTabPage(StreamControl streamControl, int AddAtIndex = -1)
    {
        var TabPage = new TabPage()
        {
            Controls = { streamControl }
        };
        if (AddAtIndex < 0)
            this.tabControl1.TabPages.Add(TabPage);
        else
            this.tabControl1.TabPages.Insert(AddAtIndex, TabPage);

        this.Set_TabPage_IndexText(TabPage);

        return TabPage;
    }

    protected void Set_TabPage_IndexText(TabPage tabPage)
    {
        var TabIndex = this.tabControl1.TabPages.IndexOf(tabPage);
        var DisplayableTabIndex = $"[{TabIndex + 1}] ";
        tabPage.Text = DisplayableTabIndex + "null -> null";
    }

    protected void ResetAll_TabPage_IndexText()
    {
        for (int TabIndex = 0; TabIndex < this.tabControl1.TabPages.Count; TabIndex++)
        {
            var TabPage = this.tabControl1.TabPages[TabIndex];
            int closingBracketIndex = TabPage.Text.IndexOf("] ");
            string remainingText = TabPage.Text.Substring(closingBracketIndex + 2);

            var DisplayableTabIndex = $"[{TabIndex + 1}] ";
            TabPage.Text = DisplayableTabIndex + remainingText;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    protected void Display_Input_Output_Channels(StreamControl streamControl)
    {
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

        int InputIndex = -1;
        int OutputIndex = -1;
        for (int i = 0; i < Capabilities.Value.InputChannelInfos.Length; i++)
        {
            InputIndex++;
            var InputChannel = Capabilities.Value.InputChannelInfos[i];
            _ = streamControl.Get_cboInputStream.Items.Add("(" + InputChannel.channel + ") " + InputChannel.name);
        }

        for (int i = 0; i < Capabilities.Value.OutputChannelInfos.Length; i++)
        {
            OutputIndex++;
            var OutputChannel = Capabilities.Value.OutputChannelInfos[i];
            _ = streamControl.Get_cboOutputStream.Items.Add("(" + OutputChannel.channel + ") " + OutputChannel.name);
        }

        //for (int i = 0; i < Program.DSP_Info.Buses.Count; i++)
        //{
        //    InputIndex++;
        //    OutputIndex++;
        //    var Bus = Program.DSP_Info.Buses[i];
        //    _ = streamControl.Get_cboInputStream.Items.Add("(" + InputIndex + ") Input " + Bus.Name);            
        //    _ = streamControl.Get_cboOutputStream.Items.Add("(" + OutputIndex + ") Output " + Bus.Name);
        //}

        //for (int i = 0; i < Program.DSP_Info.AbstractBuses.Count; i++)
        //{
        //    InputIndex++;
        //    OutputIndex++;
        //    var AbstractBus = Program.DSP_Info.AbstractBuses[i];
        //    _ = streamControl.Get_cboInputStream.Items.Add("(" + InputIndex + ") Input " + AbstractBus.Name);
        //    _ = streamControl.Get_cboOutputStream.Items.Add("(" + OutputIndex + ") Output " + AbstractBus.Name);
        //}
    }

    protected void Set_HScrollbar(int streamControlCount)
    {
        this.hScrollBar1.Visible = false;
        this.hScrollBar1.Maximum = streamControlCount;
        this.hScrollBar1.Minimum = 0;
        this.hScrollBar1.Value = this.hScrollBar1.Maximum;
        this.hScrollBar1.Visible = true;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    protected void AddStreamControlEventHandlers(StreamControl streamControl, TabPage tabPage, DSP_Stream dsp_Stream)
    {
        #region Event Handlers

        #region Filters
        streamControl.FilterAdded += (s1, filter) =>
        {
            try
            {
                if (filter?.CurrentFilterControl?.GetFilter != null)
                {
                    var Index = streamControl.FilterControls.IndexOf(filter);
                    if (Index >= 0 && Index <= dsp_Stream.Filters.Count)
                        dsp_Stream.Filters.Insert(Index, filter.CurrentFilterControl.GetFilter);
                    else //If not found, add to the end
                        dsp_Stream.Filters.Add(filter.CurrentFilterControl.GetFilter);
                }
            }
            catch (Exception ex)
            {
                this.Error(ex);
            }
        };

        streamControl.FilterDeleted += (s1, filter) =>
        {
            try
            {
                if (filter?.CurrentFilterControl?.GetFilter != null)
                    _ = dsp_Stream.Filters.Remove(filter.CurrentFilterControl.GetFilter);
            }
            catch (Exception ex)
            {
                this.Error(ex);
            }
        };

        streamControl.FilterMovedUp += (s1, filterDirection) =>
        {
            try
            {
                if (filterDirection.Filter?.CurrentFilterControl?.GetFilter != null)
                {
                    var CurrentIndex = dsp_Stream.Filters.FindIndex(x => x == filterDirection.Filter.CurrentFilterControl.GetFilter);
                    if (CurrentIndex > 0)
                    {
                        dsp_Stream.Filters.RemoveAt(CurrentIndex);
                        dsp_Stream.Filters.Insert(CurrentIndex - 1, filterDirection.Filter.CurrentFilterControl.GetFilter);
                    }
                }
            }
            catch (Exception ex)
            {
                this.Error(ex);
            }
        };

        streamControl.FilterMovedDown += (s1, filterDirection) =>
        {
            try
            {
                if (filterDirection.Filter?.CurrentFilterControl?.GetFilter != null)
                {
                    var CurrentIndex = dsp_Stream.Filters.FindIndex(x => x == filterDirection.Filter.CurrentFilterControl.GetFilter);
                    if (CurrentIndex + 1 < dsp_Stream.Filters.Count)
                    {
                        dsp_Stream.Filters.RemoveAt(CurrentIndex);
                        dsp_Stream.Filters.Insert(CurrentIndex + 1, filterDirection.Filter.CurrentFilterControl.GetFilter);
                    }
                }
            }
            catch (Exception ex)
            {
                this.Error(ex);
            }
        };

        #endregion

        #region Volume
        streamControl.Get_In_Volume.VolumeChanged += (s1, e1) =>
        {
            try
            {
                dsp_Stream.InputVolume = streamControl.Get_In_Volume.Volume;
            }
            catch (Exception ex)
            {
                this.Error(ex);
            }
        };

        streamControl.Get_Out_Volume.VolumeChanged += (s1, e1) =>
        {
            try
            {
                dsp_Stream.OutputVolume = streamControl.Get_Out_Volume.Volume;
            }
            catch (Exception ex)
            {
                this.Error(ex);
            }
        };
        #endregion

        #region Stream Deleted
        streamControl.Get_btnDelete.Click += (s1, e1) =>
        {
            try
            {
                this.DeleteStream(streamControl, tabPage, dsp_Stream);
            }
            catch (Exception ex)
            {
                this.Error(ex);
            }
        };
        #endregion

        #region Stream Moved
        streamControl.Get_btn_MoveTo.Click += (s1, e1) =>
        {
            try
            {
                int Index = int.Parse(streamControl.Get_txtMoveToIndex.Text);
                Index--;
                if (Index < 0)
                    Index = 0;

                this.MoveStream(Index, streamControl, tabPage, dsp_Stream);
            }
            catch (Exception ex)
            {
                this.Error(ex);
            }
        };
        #endregion

        #region Stream Channel Changed
        streamControl.Get_cboInputStream.SelectedIndexChanged += (s1, e1) =>
            {
                try
                {
                    this.InputStream_SelectedIndexChanged(streamControl, tabPage, dsp_Stream);
                }
                catch (Exception ex)
                {
                    this.Error(ex);
                }
            };

        streamControl.Get_cboOutputStream.SelectedIndexChanged += (s1, e1) =>
        {
            try
            {
                this.OutputStream_SelectedIndexChanged(streamControl, tabPage, dsp_Stream);
            }
            catch (Exception ex)
            {
                this.Error(ex);
            }
        };
        #endregion

        #endregion
    }

    protected void RefreshStreamCountLabel(int streamControlCount)
    {
        this.lblStreamCount.Text = streamControlCount.ToString();
        this.lblStreamCount.Update();
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    protected void RefreshTabPageText(StreamControl stream, TabPage tabPage)
    {
        var InputText = stream.Get_cboInputStream.SelectedItem != null ?
            stream.Get_cboInputStream.SelectedItem.ToString()
            : "null";
        var OutputText = stream.Get_cboOutputStream.SelectedItem != null ?
            stream.Get_cboOutputStream.SelectedItem.ToString()
            : "null";
        var TabIndex = "[" + (this.tabControl1.TabPages.IndexOf(tabPage) + 1).ToString() + "] ";
        tabPage.Text = TabIndex + InputText + " -> " + OutputText;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    protected void InputStream_SelectedIndexChanged(StreamControl stream, TabPage tabPage, DSP_Stream dsp_Stream)
    {
        String? InputChannelName = stream?.Get_cboInputStream?.SelectedItem?.ToString();
        if (stream != null && !String.IsNullOrEmpty(InputChannelName))
        {
            //Change the DSP Streams InputChannelIndex to that of what the user has selected
            //Default is -1
            dsp_Stream.InputChannelIndex = stream.Get_cboInputStream.SelectedIndex;
            dsp_Stream.InputChannelName = InputChannelName;

            Program.Form_Monitoring?.RefreshStreamInfo(dsp_Stream);

            //Refresh the UI
            this.RefreshTabPageText(stream, tabPage);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    protected void OutputStream_SelectedIndexChanged(StreamControl stream, TabPage tabPage, DSP_Stream dsp_Stream)
    {
        String? OutputChannelName = stream?.Get_cboOutputStream?.SelectedItem?.ToString();
        if (stream != null && !String.IsNullOrEmpty(OutputChannelName))
        {
            //Change the DSP Streams OutputChannelIndex to that of what the user has selected
            //Default is -1

            //Clears\mutes the audio data from the now abandoned output channel
            Program.ASIO.RequestClearedOutputBuffer(dsp_Stream.OutputChannelIndex);
            //Set the new Output Channel Index for this stream
            dsp_Stream.OutputChannelIndex = stream.Get_cboOutputStream.SelectedIndex;
            dsp_Stream.OutputChannelName = OutputChannelName;

            Program.Form_Monitoring?.RefreshStreamInfo(dsp_Stream);

            //Refresh the UI
            this.RefreshTabPageText(stream, tabPage);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    protected void DeleteStream(StreamControl stream, TabPage tabPage, DSP_Stream dsp_Stream)
    {
        var OutputChannelIndex = dsp_Stream.OutputChannelIndex;
        //Remove the stream from the internal control list, UI, and DSP engine
        _ = Program.DSP_Info.Streams.Remove(dsp_Stream);

        //Use Task Run's to speed up the delete

        _ = Task.Run(() => this.SafeInvoke(() =>
           //Clear the abandoned audio stream from the output buffer
           Program.ASIO.RequestClearedOutputBuffer(OutputChannelIndex)
        ));

        _ = Task.Run(() => this.SafeInvoke(() =>
        {
            this.tabControl1.Visible = false;
            //Store the current tab index we'll need this later
            var CurrentTabControl1SelectedIndex = this.tabControl1.SelectedIndex;

            _ = this.StreamControls.Remove(stream);
            this.tabControl1.TabPages.Remove(tabPage);

            this.Set_HScrollbar(this.StreamControls.Count);

            this.ResetAll_TabPage_IndexText();

            //Try to set Tab Index so that it doesn't change or goes to the end of the list
            if (CurrentTabControl1SelectedIndex <= this.tabControl1.TabCount - 1)
                this.tabControl1.SelectedIndex = CurrentTabControl1SelectedIndex;
            else
                this.tabControl1.SelectedIndex = this.tabControl1.TabCount - 1;

            this.RefreshStreamCountLabel(this.StreamControls.Count);
            this.tabControl1.Visible = true;
        }));

        _ = Task.Run(() => this.SafeInvoke(() =>
            Program.Form_Monitoring?.RemoveStreamVolumeLevelControl(dsp_Stream)
        ));
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    protected void MoveStream(int MoveToIndex, StreamControl stream, TabPage tabPage, DSP_Stream dsp_Stream)
    {
        this.SafeInvoke(() =>
        {
            var StreamControlCurrentIndex = this.StreamControls.IndexOf(stream);
            if (StreamControlCurrentIndex >= 0 && 
                MoveToIndex >= 0 && MoveToIndex < this.StreamControls.Count)
            {
                this.StreamControls.Move(StreamControlCurrentIndex, MoveToIndex);
            }

            var TabControlCurrentIndex = this.tabControl1.TabPages.IndexOf(tabPage);
            if (TabControlCurrentIndex >= 0 && 
                MoveToIndex >= 0 && MoveToIndex < this.tabControl1.TabPages.Count)
            {
                var movingTab = this.tabControl1.TabPages[TabControlCurrentIndex];
                this.tabControl1.TabPages.RemoveAt(TabControlCurrentIndex);
                this.tabControl1.TabPages.Insert(MoveToIndex, movingTab);
                this.ResetAll_TabPage_IndexText();
                this.tabControl1.SelectedIndex = MoveToIndex;
            }

            var Streams = Program.DSP_Info.Streams;
            var StreamCurrentIndex = Streams.IndexOf(dsp_Stream);
            if (StreamCurrentIndex >= 0 &&
                MoveToIndex >= 0 && MoveToIndex < Streams.Count)
            {
                Streams.Move(StreamCurrentIndex, MoveToIndex);
            }
        });
    }

    protected void ResetTabPagesAndStreamControls()
    {
        this.StreamControls.Clear();
        this.tabControl1.TabPages.Clear();
    }
    #endregion

    #region Error Handling
    protected void Error(Exception ex)
    {
        Debug.Error(ex);
    }
    #endregion

}