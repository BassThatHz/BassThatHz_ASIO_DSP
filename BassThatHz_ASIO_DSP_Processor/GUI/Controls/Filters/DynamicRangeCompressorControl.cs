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
public partial class DynamicRangeCompressorControl : UserControl, IFilterControl
{
    #region Variables
    protected DynamicRangeCompressor Filter = new();
    // Guard to avoid mapping event handlers multiple times (which would cause duplicates)
    private bool _handlersMapped = false;
    private const double ThresholdEpsilon = 1e-6;
    #endregion

    #region Constructor and MapEventHandlers
    public DynamicRangeCompressorControl()
    {
        InitializeComponent();

        this.MapEventHandlers();
    }

    public void MapEventHandlers()
    {
        if (_handlersMapped)
            return;

        SampleRateChangeNotifier.SampleRateChanged += SampleRateChangeNotifier_SampleRateChanged;
        this.Threshold.VolumeChanged += Threshold_VolumeChanged;

        _handlersMapped = true;
    }
    #endregion

    #region Event Handlers
    protected void Threshold_VolumeChanged(object? sender, EventArgs e)
    {
        try
        {
            var newThreshold = this.Threshold.VolumedB;
            // Avoid unnecessary updates if value did not change meaningfully
            if (Math.Abs(this.Filter.Threshold_dB - newThreshold) <= ThresholdEpsilon)
                return;

            this.Filter.Threshold_dB = newThreshold;
            var asio = Program.ASIO;
            if (asio != null)
                this.Filter.CalculateCoeffs(asio.SampleRate_Current);
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
            // read once to avoid any potential race with filter state
            var applied = this.Filter.CompressionApplied;
            this.CompressionApplied.Volume = applied;
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
        // Apply UI values to filter with robust parsing and minimal UI updates
        this.Filter.Threshold_dB = this.Threshold.VolumedB;

        // Local copies to avoid repeated control access
        var attackText = this.msb_AttackTime_ms.Text;
        var releaseText = this.msb_ReleaseTime_ms.Text;
        var ratioText = this.msb_CompressionRatio.Text;
        var kneeText = this.msb_KneeWidth_db.Text;

        // Parse using invariant culture to avoid locale issues
        if (!double.TryParse(attackText, NumberStyles.Float, CultureInfo.InvariantCulture, out var attack) || double.IsNaN(attack))
            attack = this.Filter.AttackTime_ms;
        if (attack < 1)
            attack = 1;

        if (!double.TryParse(releaseText, NumberStyles.Float, CultureInfo.InvariantCulture, out var release) || double.IsNaN(release))
            release = this.Filter.ReleaseTime_ms;
        if (release < 1)
            release = 1;

        if (!double.TryParse(ratioText, NumberStyles.Float, CultureInfo.InvariantCulture, out var ratio) || double.IsNaN(ratio))
            ratio = this.Filter.Ratio;
        if (ratio < 11)
            ratio = 11;

        if (!double.TryParse(kneeText, NumberStyles.Float, CultureInfo.InvariantCulture, out var knee) || double.IsNaN(knee))
            knee = this.Filter.KneeWidth_dB;
        if (knee < 1)
            knee = 1;

        // Only update UI text if we adjusted values (reduces UI invalidation)
        var newAttackText = attack.ToString(CultureInfo.InvariantCulture);
        if (!string.Equals(newAttackText, attackText, StringComparison.Ordinal))
            this.msb_AttackTime_ms.Text = newAttackText;

        var newReleaseText = release.ToString(CultureInfo.InvariantCulture);
        if (!string.Equals(newReleaseText, releaseText, StringComparison.Ordinal))
            this.msb_ReleaseTime_ms.Text = newReleaseText;

        var newRatioText = ratio.ToString(CultureInfo.InvariantCulture);
        if (!string.Equals(newRatioText, ratioText, StringComparison.Ordinal))
            this.msb_CompressionRatio.Text = newRatioText;

        var newKneeText = knee.ToString(CultureInfo.InvariantCulture);
        if (!string.Equals(newKneeText, kneeText, StringComparison.Ordinal))
            this.msb_KneeWidth_db.Text = newKneeText;

        this.Filter.AttackTime_ms = attack;
        this.Filter.ReleaseTime_ms = release;
        this.Filter.Ratio = ratio;
        this.Filter.KneeWidth_dB = knee;

        this.Filter.UseSoftKnee = this.chkSoftKnee.Checked;

        var asio2 = Program.ASIO;
        if (asio2 != null)
            this.Filter.CalculateCoeffs(asio2.SampleRate_Current);

        this.Filter.ApplySettings();
    }

    public void SetDeepClonedFilter(IFilter input)
    {
        if (input is DynamicRangeCompressor drc)
        {
            this.Filter = drc;
            // Update UI from filter using invariant culture formatting
            this.Threshold.VolumedB = this.Filter.Threshold_dB;
            this.msb_AttackTime_ms.Text = (this.Filter.AttackTime_ms < 1 ? 1 : this.Filter.AttackTime_ms).ToString(CultureInfo.InvariantCulture);
            this.msb_ReleaseTime_ms.Text = (this.Filter.ReleaseTime_ms < 1 ? 1 : this.Filter.ReleaseTime_ms).ToString(CultureInfo.InvariantCulture);
            this.msb_CompressionRatio.Text = (this.Filter.Ratio < 11 ? 11 : this.Filter.Ratio).ToString(CultureInfo.InvariantCulture);
            this.msb_KneeWidth_db.Text = (this.Filter.KneeWidth_dB < 1 ? 1 : this.Filter.KneeWidth_dB).ToString(CultureInfo.InvariantCulture);
            this.chkSoftKnee.Checked = this.Filter.UseSoftKnee;

            var asio = Program.ASIO;
            if (asio != null)
                this.Filter.CalculateCoeffs(asio.SampleRate_Current);
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