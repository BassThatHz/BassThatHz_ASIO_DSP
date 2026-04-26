#nullable enable

namespace BassThatHz_ASIO_DSP_Processor.GUI.Controls;

#region Usings
using GUI.Forms;
using NAudio.Utils;
using System;
using System.ComponentModel;
using System.Runtime.Versioning;
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
public partial class BTH_VolumeLevelControl : UserControl
{
    #region Variables
    public DSP_Stream? Stream;
    protected double Input_StreamVolume = 0;
    protected IStreamItem? InputChannel;
    protected IStreamItem? OutputChannel;

    protected double ClipLevel = 1;

    protected double Input_Peak = 0;
    protected double Input_RMS = 0;
    protected double Input_DB_Peak = 0;
    protected double Input_DB = 0;

    protected double Output_Peak = 0;
    protected double Output_RMS = 0;
    protected double Output_DB_Peak = 0;
    protected double Output_DB = 0;
    // Last rendered values to avoid unnecessary UI updates
    protected double Prev_Input_DB = double.NaN;
    protected double Prev_Input_DB_Peak = double.NaN;
    protected double Prev_Output_DB = double.NaN;
    protected double Prev_Output_DB_Peak = double.NaN;
    #endregion

    #region Public Properties
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Button Get_btn_View => this.btn_View;
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Timer Get_timer_Refresh => this.timer_Refresh;
    #endregion

    #region Constructor and MapEventHandlers
    public BTH_VolumeLevelControl()
    {
        InitializeComponent();
        this.MapEventHandlers();
    }

    public void MapEventHandlers()
    {
        this.pnl_InputClip.Click += Pnl_InputClip_Click;
        this.pnl_OutputClip.Click += Pnl_OutputClip_Click;
    }
    #endregion

    #region Event Handlers
    protected void Pnl_OutputClip_Click(object? sender, EventArgs e)
    {
        try
        {
            this.pnl_OutputClip.BackColor = System.Drawing.Color.Black;
            this.Output_Peak = 0;
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }

    protected void Pnl_InputClip_Click(object? sender, EventArgs e)
    {
        try
        {
            this.pnl_InputClip.BackColor = System.Drawing.Color.Black;
            this.Input_Peak = 0;
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }

    protected void timer_Refresh_Tick(object? sender, EventArgs e)
    {
        try
        {
            this.Set_DB_Lables();
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }

    [SupportedOSPlatform("windows")]
    protected void Btn_View_Click(object? sender, EventArgs e)
    {
        try
        {
            if (this.Stream == null || this.Stream.InputSource == null || this.Stream.OutputDestination == null)
                return;

            var x = new FormRTA();
            x.Text += "  " + this.Stream.InputSource.Name + "-> " + this.Stream.OutputDestination.Name;
            x.Init_Channels(this.Stream.InputSource, this.Stream.OutputDestination);
            x.Show();
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }
    #endregion

    #region Public Functions
    public void Set_StreamInfo(DSP_Stream? input)
    {
        this.Stream = input;
        if (Stream == null || Stream.InputSource == null || Stream.OutputDestination == null)
            return;

        this.InputChannel = Stream.InputSource;
        this.lbl_InputSource.Text = Stream.InputSource.DisplayMember;
        this.OutputChannel = Stream.OutputDestination;
        this.lbl_OutputSource.Text = Stream.OutputDestination.DisplayMember;
    }

    public void Reset_ClipIndicator()
    {
        this.SafeInvoke(() =>
        {
            this.Pnl_InputClip_Click(this, EventArgs.Empty);
            this.Pnl_OutputClip_Click(this, EventArgs.Empty);
        });
    }

    public void ComputeLevels()
    {
        if (this.Disposing || this.IsDisposed)
            return;
        this.SafeInvoke(() =>
        {
            if (this.Disposing || this.IsDisposed)
                return;
            this.CalculateInputLevels();
            if (this.Disposing || this.IsDisposed)
                return;
            this.CalculateOutputLevels();
            if (this.Disposing || this.IsDisposed)
                return;
            this.Set_VolAndClipIndicators();
        });
    }
    #endregion

    #region Protected Functions

    // Helper method that calculates RMS, peak, and decibel values.
    private void CalculateLevels(double[] audioData, bool isInput)
    {
        if (audioData == null || audioData.Length == 0)
            return;

        double squareSum = 0;
        double peak = 0;
        // For input channels, apply the stream volume if available.
        double volume = isInput && this.Stream != null ? this.Stream.InputVolume : 1.0;

        // Use indexed loop to avoid enumerator allocation and use multiplication
        // instead of Math.Pow for square to reduce CPU overhead.
        int len = audioData.Length;
        for (int i = 0; i < len; i++)
        {
            double level = audioData[i] * volume;
            double absLevel = Math.Abs(level);

            if (absLevel > peak)
                peak = absLevel;

            squareSum += level * level;
        }

        double rms = Math.Sqrt(squareSum / audioData.Length);
        double db = Decibels.LinearToDecibels(rms);
        double dbPeak = Decibels.LinearToDecibels(peak);

        if (isInput)
        {
            this.Input_RMS = rms;
            this.Input_DB = db;
            this.Input_DB_Peak = dbPeak;
            this.Input_Peak = peak;
        }
        else
        {
            this.Output_RMS = rms;
            this.Output_DB = db;
            this.Output_DB_Peak = dbPeak;
            this.Output_Peak = peak;
        }
    }

    protected void CalculateInputLevels()
    {
        if (this.InputChannel != null && this.InputChannel.Index > -1)
        {
            double[] inputData = CommonFunctions.GetStreamInputDataByStreamItem(this.InputChannel);
            // Only calculate if inputData is not null.
            if (inputData != null)
            {
                // For input, pass 'true' to apply stream volume.
                CalculateLevels(inputData, true);
            }
        }
    }

    protected void CalculateOutputLevels()
    {
        if (this.OutputChannel != null && this.OutputChannel.Index > -1)
        {
            double[] outputData = CommonFunctions.GetStreamOutputDataByStreamItem(this.OutputChannel);
            // Only calculate if outputData is not null.
            if (outputData != null)
            {
                // For output, pass 'false' so that no volume multiplier is applied.
                CalculateLevels(outputData, false);
            }
        }
    }

    protected void Set_DB_Lables()
    {
        // Prepare strings once and only update UI when the text actually changes
        string inPeak = Math.Round(this.Input_DB_Peak, 0).ToString(System.Globalization.CultureInfo.InvariantCulture) + "dB";
        if (this.lbl_Input_DB_Peak.Text != inPeak)
            this.lbl_Input_DB_Peak.Text = inPeak;

        string inDb = Math.Round(this.Input_DB, 0).ToString(System.Globalization.CultureInfo.InvariantCulture) + "dB";
        if (this.lbl_Input_DB.Text != inDb)
            this.lbl_Input_DB.Text = inDb;

        string outPeak = Math.Round(this.Output_DB_Peak, 0).ToString(System.Globalization.CultureInfo.InvariantCulture) + "dB";
        if (this.lbl_Output_DB_Peak.Text != outPeak)
            this.lbl_Output_DB_Peak.Text = outPeak;

        string outDb = Math.Round(this.Output_DB, 0).ToString(System.Globalization.CultureInfo.InvariantCulture) + "dB";
        if (this.lbl_Output_DB.Text != outDb)
            this.lbl_Output_DB.Text = outDb;
    }

    protected void Set_VolAndClipIndicators()
    {
        // Local refs to reduce repeated property access
        var volIn = this.vol_In;
        var volOut = this.vol_Out;
        var pnlIn = this.pnl_InputClip;
        var pnlOut = this.pnl_OutputClip;

        // Only update DB level if changed beyond a small threshold to avoid frequent redraws
        const double threshold = 0.1; // dB
        if (double.IsNaN(this.Prev_Input_DB) || Math.Abs(this.Input_DB - this.Prev_Input_DB) > threshold)
        {
            volIn.DB_Level = this.Input_DB;
            volIn.Invalidate();
            this.Prev_Input_DB = this.Input_DB;
        }

        if (double.IsNaN(this.Prev_Input_DB_Peak) || Math.Abs(this.Input_DB_Peak - this.Prev_Input_DB_Peak) > threshold)
        {
            // If clip threshold reached, set to red. Only change color when different to avoid repaint churn.
            if ((this.Input_DB >= this.ClipLevel || this.Input_DB_Peak >= this.ClipLevel) && pnlIn.BackColor != System.Drawing.Color.Red)
                pnlIn.BackColor = System.Drawing.Color.Red;
            else if (this.Input_DB < this.ClipLevel && this.Input_DB_Peak < this.ClipLevel && pnlIn.BackColor != System.Drawing.Color.Black)
                pnlIn.BackColor = System.Drawing.Color.Black;

            this.Prev_Input_DB_Peak = this.Input_DB_Peak;
        }

        if (double.IsNaN(this.Prev_Output_DB) || Math.Abs(this.Output_DB - this.Prev_Output_DB) > threshold)
        {
            volOut.DB_Level = this.Output_DB;
            volOut.Invalidate();
            this.Prev_Output_DB = this.Output_DB;
        }

        if (double.IsNaN(this.Prev_Output_DB_Peak) || Math.Abs(this.Output_DB_Peak - this.Prev_Output_DB_Peak) > threshold)
        {
            if ((this.Output_DB >= this.ClipLevel || this.Output_DB_Peak >= this.ClipLevel) && pnlOut.BackColor != System.Drawing.Color.Red)
                pnlOut.BackColor = System.Drawing.Color.Red;
            else if (this.Output_DB < this.ClipLevel && this.Output_DB_Peak < this.ClipLevel && pnlOut.BackColor != System.Drawing.Color.Black)
                pnlOut.BackColor = System.Drawing.Color.Black;

            this.Prev_Output_DB_Peak = this.Output_DB_Peak;
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