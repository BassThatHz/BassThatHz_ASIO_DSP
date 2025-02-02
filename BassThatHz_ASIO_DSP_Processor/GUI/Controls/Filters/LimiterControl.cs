#nullable enable

namespace BassThatHz_ASIO_DSP_Processor.GUI.Controls;

#region Usings
using System;
using System.Drawing;
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
public partial class LimiterControl : UserControl, IFilterControl
{
    #region Variables
    protected Limiter Filter = new();
    #endregion

    #region Constructor and MapEventHandlers
    public LimiterControl()
    {
        InitializeComponent();
        this.CompressionApplied.ReadOnly = true;
        this.MapEventHandlers();
    }

    public void MapEventHandlers()
    {
        this.Limit.VolumeChanged += this.LimitChanged;
        this.Threshold.VolumeChanged += Threshold_VolumeChanged;

        this.mask_Attack.TextChanged += Mask_Decay_TextChanged;
        this.mask_Release.TextChanged += Mask_Release_TextChanged;
    }
    #endregion

    #region Event Handlers
    protected void Mask_Release_TextChanged(object? sender, EventArgs e)
    {
        try
        {
            this.Filter.PeakHoldRelease = int.Parse(this.mask_Release.Text);
            this.ApplyCoeffs();
        }
        catch
        {
        }
    }

    protected void Mask_Decay_TextChanged(object? sender, EventArgs e)
    {
        try
        {
            this.Filter.PeakHoldAttack = int.Parse(this.mask_Attack.Text);
            this.ApplyCoeffs();
        }
        catch
        {
        }
    }

    protected void chk_PeakHoldRelease_CheckedChanged(object? sender, EventArgs e)
    {
        try
        {
            this.ApplyCoeffs();
            this.Filter.PeakHoldReleaseEnabled = this.chk_PeakHoldRelease.Checked;
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }

    protected void chk_PeakHoldAttack_CheckedChanged(object? sender, EventArgs e)
    {
        try
        {
            this.ApplyCoeffs();
            this.Filter.PeakHoldAttackEnabled = this.chk_PeakHoldAttack.Checked;
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }

    protected void RefreshTimer_Tick(object? sender, EventArgs e)
    {
        try
        {
            this.CompressionApplied.Volume = this.Filter.CompressionApplied;
            if (this.Filter.IsBrickwall)
            {
                if (this.Filter.CompressionApplied < 0.99885051990500184d)
                {
                    this.CompressionApplied.TextColor = Color.White;
                    this.CompressionApplied.SliderColor = Brushes.Firebrick;
                }
                else
                {
                    this.CompressionApplied.TextColor = Color.Black;
                    this.CompressionApplied.SliderColor = Brushes.LightGreen;
                }
            }
            else
            {
                if (this.Filter.CompressionApplied < 0.99885051990500184d)
                {
                    this.CompressionApplied.TextColor = Color.White;
                    this.CompressionApplied.SliderColor = Brushes.DarkBlue;
                }
                else
                {
                    this.CompressionApplied.TextColor = Color.Black;
                    this.CompressionApplied.SliderColor = Brushes.LightGreen;
                }
            }
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }

    protected void LimitChanged(object? sender, System.EventArgs e)
    {
        try
        {
            if (this.Threshold.Volume >= this.Limit.Volume || Math.Abs(this.Limit.VolumedB - this.Threshold.VolumedB) < 0)
                this.Threshold.VolumedB = this.Limit.VolumedB;

            this.Filter.MaxValue = this.Limit.Volume;
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }

    protected void Threshold_VolumeChanged(object? sender, EventArgs e)
    {
        try
        {
            if (this.Threshold.Volume >= this.Limit.Volume || Math.Abs(this.Limit.VolumedB - this.Threshold.VolumedB) < 0d)
                this.Threshold.VolumedB = this.Limit.VolumedB;
            if (this.Threshold.VolumedB > -1d)
                this.Threshold.VolumedB = -1d;
            this.Filter.Threshold = this.Threshold.Volume;
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }

    protected void btnApply_Click(object? sender, System.EventArgs e)
    {
        this.ApplySettings();
    }
    #endregion

    #region Protected Functions

    protected void ApplyCoeffs()
    {
        if (Program.ASIO != null)
            this.Filter.CalculateCoeffs(Program.ASIO.SampleRate_Current);
    }

    protected void Limit_Threshold_Maximum()
    {
        if (this.Threshold.Volume >= this.Limit.Volume || Math.Abs(this.Limit.VolumedB - this.Threshold.VolumedB) < 20d)
            this.Threshold.VolumedB = this.Limit.VolumedB - 20d;
    }
    #endregion

    #region Interfaces

    public IFilter GetFilter =>
        this.Filter;

    public void ApplySettings()
    {
        this.ApplyCoeffs();
        this.Filter.ApplySettings();
    }

    public void SetDeepClonedFilter(IFilter input)
    {
        if (input is Limiter limiter)
        {
            this.Filter = limiter;
            this.Limit.Volume = (double)limiter.MaxValue;
            this.Threshold.Volume = (double)limiter.Threshold;

            this.chk_PeakHoldAttack.Checked = this.Filter.PeakHoldAttackEnabled;
            this.mask_Attack.Text = this.Filter.PeakHoldAttack.ToString();

            this.chk_PeakHoldRelease.Checked = this.Filter.PeakHoldReleaseEnabled;
            this.mask_Release.Text = this.Filter.PeakHoldRelease.ToString();

            this.ApplySettings();
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