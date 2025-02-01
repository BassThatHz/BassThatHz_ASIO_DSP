#nullable enable

namespace BassThatHz_ASIO_DSP_Processor.GUI.Controls;

#region Usings
using System;
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
public partial class ClassicLimiterControl : UserControl, IFilterControl
{
    #region Variables
    protected ClassicLimiter Filter = new();
    #endregion

    #region Constructor and MapEventHandlers
    public ClassicLimiterControl()
    {
        InitializeComponent();

        this.MapEventHandlers();
    }

    public void MapEventHandlers()
    {
        SampleRateChangeNotifier.SampleRateChanged += SampleRateChangeNotifier_SampleRateChanged;
        this.Threshold.VolumeChanged += Threshold_VolumeChanged;
    }

    protected void Threshold_VolumeChanged(object? sender, EventArgs e)
    {
        this.Filter.Threshold_dB = this.Threshold.VolumedB;
        if (Program.ASIO != null)
            this.Filter.CalculateCoeffs(Program.ASIO.SampleRate_Current);
    }
    #endregion

    #region Event Handlers
    protected void RefreshTimer_Tick(object? sender, EventArgs e)
    {
        try
        {
            this.CompressionApplied.Volume = this.Filter.CompressionApplied;
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }

    protected void btnApply_Click(object sender, EventArgs e)
    {
        this.ApplySettings();
    }

    protected void chkSoftKnee_CheckedChanged(object sender, EventArgs e)
    {
        try
        {
            this.Filter.UseSoftKnee = this.chkSoftKnee.Checked;
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }

    protected void SampleRateChangeNotifier_SampleRateChanged(int newSampleRate)
    {
        this.Filter.CalculateCoeffs(newSampleRate);
    }

    #endregion

    #region Interfaces
    public IFilter GetFilter =>
        this.Filter;

    public void ApplySettings()
    {
        this.Filter.Threshold_dB = this.Threshold.VolumedB;
        var TempAttackTime_ms = double.Parse(this.msb_AttackTime_ms.Text);
        if (TempAttackTime_ms < 1)
        {
            TempAttackTime_ms = 1;
            this.msb_AttackTime_ms.Text = "1";
        }
        this.Filter.AttackTime_ms = TempAttackTime_ms;

        var TempReleaseTime_ms = double.Parse(this.msb_ReleaseTime_ms.Text);
        if (TempReleaseTime_ms < 1)
        {
            TempReleaseTime_ms = 1;
            this.msb_ReleaseTime_ms.Text = "1";
        }
        this.Filter.ReleaseTime_ms = TempReleaseTime_ms;

        var TempKneeWidth_dB = double.Parse(this.msb_KneeWidth_db.Text);
        if (TempKneeWidth_dB < 1)
        {
            TempKneeWidth_dB = 1;
            this.msb_KneeWidth_db.Text = "1";
        }
        this.Filter.KneeWidth_dB = TempKneeWidth_dB;


        this.Filter.UseSoftKnee = this.chkSoftKnee.Checked;

        if (Program.ASIO != null)
            this.Filter.CalculateCoeffs(Program.ASIO.SampleRate_Current);

        this.Filter.ApplySettings();
    }

    public void SetDeepClonedFilter(IFilter input)
    {
        if (input is ClassicLimiter drc)
        {
            this.Filter = drc;
            this.Threshold.VolumedB = this.Filter.Threshold_dB;
            this.msb_AttackTime_ms.Text = this.Filter.AttackTime_ms < 1 ? "1" : this.Filter.AttackTime_ms.ToString();
            this.msb_ReleaseTime_ms.Text = this.Filter.ReleaseTime_ms < 1 ? "1" : this.Filter.ReleaseTime_ms.ToString();
            this.msb_KneeWidth_db.Text = this.Filter.KneeWidth_dB < 1 ? "1" : this.Filter.KneeWidth_dB.ToString();
            this.chkSoftKnee.Checked = this.Filter.UseSoftKnee;

            if (Program.ASIO != null)
                this.Filter.CalculateCoeffs(Program.ASIO.SampleRate_Current);
            this.Filter.ApplySettings();
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