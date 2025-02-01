#nullable enable

namespace BassThatHz_ASIO_DSP_Processor.GUI.Controls;

#region Usings
using GUI.Forms;
using NAudio.Utils;
using System;
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
public partial class BTH_VolumeLevel : UserControl
{
    #region Variables
    public DSP_Stream? Stream;
    protected double Input_StreamVolume = 0;
    protected int InputChannelIndex = -1;
    protected int OutputChannelIndex = -1;

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

    #region Constructor and MapEventHandlers
    public BTH_VolumeLevel()
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
            if (this.Stream != null)
            {
                var x = new RTA();
                x.Text += "  " + this.Stream.InputChannelName + "-> " + this.Stream.OutputChannelName;
                x.Init_Channels(this.Stream.InputChannelIndex, this.Stream.OutputChannelIndex);
                x.Show();
            }
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
        if (Stream != null)
        {
            this.InputChannelIndex = Stream.InputChannelIndex;
            this.lbl_InputSource.Text = Stream.InputChannelName;
            this.OutputChannelIndex = Stream.OutputChannelIndex;
            this.lbl_OutputSource.Text = Stream.OutputChannelName;
        }
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

    protected void CalculateInputLevels()
    {
        if (this.InputChannelIndex > -1)
        {
            var InputData = Program.ASIO.GetInputAudioData(this.InputChannelIndex);
            if (InputData != null)
            {
                if (this.Stream != null)
                    this.Input_StreamVolume = this.Stream.InputVolume;

                double Length = InputData.Length;
                double SquareSum = 0;

                foreach (var Sample in InputData)
                {
                    var Level = Sample * this.Input_StreamVolume;
                    var Abs_Level = Math.Abs(Level);
                    if (Abs_Level > this.Input_Peak)
                        this.Input_Peak = Abs_Level;

                    //Calculate RMS Value
                    SquareSum += Math.Pow(Level, 2);
                }

                //Calculate RMS Value
                if (Length > 0)
                    this.Input_RMS = Math.Sqrt(SquareSum / Length);

                this.Input_DB = Decibels.LinearToDecibels(this.Input_RMS);
                this.Input_DB_Peak = Decibels.LinearToDecibels(this.Input_Peak);
            }
        }
    }

    protected void CalculateOutputLevels()
    {
        if (this.OutputChannelIndex > -1)
        {
            var OutputData = Program.ASIO.GetOutputAudioData(this.OutputChannelIndex);
            if (OutputData != null)
            {
                double Length = OutputData.Length;
                double SquareSum = 0;

                foreach (var Sample in OutputData)
                {
                    var Abs_Level = Math.Abs(Sample);
                    if (Abs_Level > this.Output_Peak)
                        this.Output_Peak = Abs_Level;

                    //Calculate RMS Value
                    SquareSum += Math.Pow(Sample, 2);
                }

                //Calculate RMS Value
                if (Length > 0)
                    this.Output_RMS = Math.Sqrt(SquareSum / Length);

                this.Output_DB = Decibels.LinearToDecibels(this.Output_RMS);
                this.Output_DB_Peak = Decibels.LinearToDecibels(this.Output_Peak);
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