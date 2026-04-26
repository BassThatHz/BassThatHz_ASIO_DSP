#nullable enable

namespace BassThatHz_ASIO_DSP_Processor.GUI.Controls;

#region Usings
using System;
using System.Globalization;
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
public partial class ClassicLimiterControl : UserControl, IFilterControl
{
    #region Variables
    protected ClassicLimiter Filter = new();
    // cache last sample rate to avoid redundant coefficient calculations
    private int _lastSampleRate = -1;
    #endregion

    #region Constructor and MapEventHandlers
    public ClassicLimiterControl()
    {
        InitializeComponent();

        this.MapEventHandlers();
    }

    public void MapEventHandlers()
    {
        // ensure handlers are not registered multiple times
        SampleRateChangeNotifier.SampleRateChanged -= SampleRateChangeNotifier_SampleRateChanged;
        SampleRateChangeNotifier.SampleRateChanged += SampleRateChangeNotifier_SampleRateChanged;

        this.Threshold.VolumeChanged -= Threshold_VolumeChanged;
        this.Threshold.VolumeChanged += Threshold_VolumeChanged;
    }

    protected void Threshold_VolumeChanged(object? sender, EventArgs e)
    {
        // minimize repeated property access and avoid unnecessary coefficient recalculation
        this.Filter.Threshold_dB = this.Threshold.VolumedB;
        var asio = Program.ASIO;
        if (asio != null)
        {
            var sr = asio.SampleRate_Current;
            if (sr != _lastSampleRate)
            {
                this.Filter.CalculateCoeffs(sr);
                _lastSampleRate = sr;
            }
        }
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
        if (newSampleRate == _lastSampleRate) return;
        this.Filter.CalculateCoeffs(newSampleRate);
        _lastSampleRate = newSampleRate;
    }

    #endregion

    #region Interfaces
    public IFilter GetFilter =>
        this.Filter;

    public void ApplySettings()
    {
        this.Filter.Threshold_dB = this.Threshold.VolumedB;
        // parse numeric inputs safely to avoid exceptions and allocations from thrown exceptions
        if (!double.TryParse(this.msb_AttackTime_ms.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var TempAttackTime_ms))
            TempAttackTime_ms = 1;
        TempAttackTime_ms = Math.Max(1.0, TempAttackTime_ms);
        this.msb_AttackTime_ms.Text = TempAttackTime_ms.ToString(CultureInfo.InvariantCulture);
        this.Filter.AttackTime_ms = TempAttackTime_ms;

        if (!double.TryParse(this.msb_ReleaseTime_ms.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var TempReleaseTime_ms))
            TempReleaseTime_ms = 1;
        TempReleaseTime_ms = Math.Max(1.0, TempReleaseTime_ms);
        this.msb_ReleaseTime_ms.Text = TempReleaseTime_ms.ToString(CultureInfo.InvariantCulture);
        this.Filter.ReleaseTime_ms = TempReleaseTime_ms;

        if (!double.TryParse(this.msb_KneeWidth_db.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var TempKneeWidth_dB))
            TempKneeWidth_dB = 1;
        TempKneeWidth_dB = Math.Max(1.0, TempKneeWidth_dB);
        this.msb_KneeWidth_db.Text = TempKneeWidth_dB.ToString(CultureInfo.InvariantCulture);
        this.Filter.KneeWidth_dB = TempKneeWidth_dB;


        this.Filter.UseSoftKnee = this.chkSoftKnee.Checked;

        var asio2 = Program.ASIO;
        if (asio2 != null && asio2.SampleRate_Current != _lastSampleRate)
        {
            this.Filter.CalculateCoeffs(asio2.SampleRate_Current);
            _lastSampleRate = asio2.SampleRate_Current;
        }

        this.Filter.ApplySettings();
    }

    public void SetDeepClonedFilter(IFilter input)
    {
        if (input is ClassicLimiter drc)
        {
            this.Filter = drc;
            this.Threshold.VolumedB = this.Filter.Threshold_dB;
            this.msb_AttackTime_ms.Text = (this.Filter.AttackTime_ms < 1 ? 1.0 : this.Filter.AttackTime_ms).ToString(CultureInfo.InvariantCulture);
            this.msb_ReleaseTime_ms.Text = (this.Filter.ReleaseTime_ms < 1 ? 1.0 : this.Filter.ReleaseTime_ms).ToString(CultureInfo.InvariantCulture);
            this.msb_KneeWidth_db.Text = (this.Filter.KneeWidth_dB < 1 ? 1.0 : this.Filter.KneeWidth_dB).ToString(CultureInfo.InvariantCulture);
            this.chkSoftKnee.Checked = this.Filter.UseSoftKnee;

            var asio3 = Program.ASIO;
            if (asio3 != null && asio3.SampleRate_Current != _lastSampleRate)
            {
                this.Filter.CalculateCoeffs(asio3.SampleRate_Current);
                _lastSampleRate = asio3.SampleRate_Current;
            }
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