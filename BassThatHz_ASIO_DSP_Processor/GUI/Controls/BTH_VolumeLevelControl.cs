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
        this.SafeInvoke(() =>
        {
            this.CalculateInputLevels();
            this.CalculateOutputLevels();

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

        foreach (var sample in audioData)
        {
            // Multiply sample by volume if input
            double level = sample * volume;
            double absLevel = Math.Abs(level);

            if (absLevel > peak)
                peak = absLevel;

            squareSum += Math.Pow(level, 2);
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
        this.lbl_Input_DB_Peak.Text = Math.Round(this.Input_DB_Peak, 0) + "dB";
        this.lbl_Input_DB.Text = Math.Round(this.Input_DB, 0) + "dB";

        this.lbl_Output_DB_Peak.Text = Math.Round(this.Output_DB_Peak, 0) + "dB";
        this.lbl_Output_DB.Text = Math.Round(this.Output_DB, 0) + "dB";
    }

    protected void Set_VolAndClipIndicators()
    {
        this.vol_In.DB_Level = this.Input_DB;
        this.vol_In.Refresh();

        if (this.Input_DB >= this.ClipLevel || this.Input_DB_Peak >= this.ClipLevel)
            this.pnl_InputClip.BackColor = System.Drawing.Color.Red;

        this.vol_Out.DB_Level = this.Output_DB;
        this.vol_Out.Refresh();

        if (this.Output_DB >= this.ClipLevel || this.Output_DB_Peak >= this.ClipLevel)
            this.pnl_OutputClip.BackColor = System.Drawing.Color.Red;
    }
    #endregion

    #region Error Handling
    protected void Error(Exception ex)
    {
        Debug.Error(ex);
    }
    #endregion
}