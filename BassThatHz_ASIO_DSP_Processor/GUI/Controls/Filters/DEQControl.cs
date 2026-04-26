#nullable enable

namespace BassThatHz_ASIO_DSP_Processor.GUI.Controls;

#region Usings
using System;
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
public partial class DEQControl : UserControl, IFilterControl
{
    // Cache enum item arrays to avoid reallocating them for every control instance.
    private static readonly object[] s_deqTypeItems;
    private static readonly object[] s_biquadTypeItems;
    private static readonly object[] s_thresholdTypeItems;

    static DEQControl()
    {
        // Build object arrays once to reduce allocations per instance.
        s_deqTypeItems = Enum.GetValues(typeof(DEQ.DEQType)).Cast<object>().ToArray();
        s_biquadTypeItems = Enum.GetValues(typeof(DEQ.BiquadType)).Cast<object>().ToArray();
        s_thresholdTypeItems = Enum.GetValues(typeof(DEQ.ThresholdType)).Cast<object>().ToArray();
    }
    #region Variables
    protected DEQ Filter = new();
    #endregion

    #region Constructor and MapEventHandlers
    public DEQControl()
    {
        InitializeComponent();

        // Reuse cached enum arrays to reduce allocations.
        this.cboDEQType.Items.AddRange(s_deqTypeItems);
        this.cboDEQType.SelectedItem = DEQ.DEQType.BoostBelow;

        this.cboBiquadType.Items.AddRange(s_biquadTypeItems);
        this.cboBiquadType.SelectedItem = DEQ.BiquadType.PEQ;

        this.cboThresholdType.Items.AddRange(s_thresholdTypeItems);
        this.cboThresholdType.SelectedItem = DEQ.ThresholdType.Peak;

        this.DynamicsApplied.ReadOnly = true;
        this.MapEventHandlers();
    }

    public void MapEventHandlers()
    {
        // Subscribe to global sample-rate notifications. Make sure to unsubscribe on dispose to
        // avoid leaking this control via the static event delegate.
        SampleRateChangeNotifier.SampleRateChanged += SampleRateChangeNotifier_SampleRateChanged;
    }
    #endregion

    #region Event Handlers

    protected void RefreshTimer_Tick(object sender, EventArgs e)
    {
        try
        {
            // Read into a local to avoid repeated property access and potential side-effects.
            var gain = this.Filter.GainApplied;
            this.DynamicsApplied.VolumedB = gain;
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
            this.ApplySettings();
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }
    protected void SampleRateChangeNotifier_SampleRateChanged(int newSampleRate)
    {
        try
        {
            this.Filter.ResetSampleRate(newSampleRate);
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }

    #endregion

    #region Interfaces
    public IFilter GetFilter =>
        this.Filter;

    public void ApplySettings()
    {
        if (this.cboDEQType.SelectedItem is DEQ.DEQType deqType)
            this.Filter.DEQ_Type = deqType;
        if (this.cboBiquadType.SelectedItem is DEQ.BiquadType biquadType)
            this.Filter.Biquad_Type = biquadType;
        if (this.cboThresholdType.SelectedItem is DEQ.ThresholdType thresholdType)
            this.Filter.Threshold_Type = thresholdType;

        if (double.TryParse(this.txtF.Text, out var tf))
            this.Filter.TargetFrequency = tf;
        if (double.TryParse(this.txtG.Text, out var tg))
            this.Filter.TargetGain_dB = tg;
        if (double.TryParse(this.txtQ.Text, out var tq))
            this.Filter.TargetQ = tq;
        if (double.TryParse(this.txtS.Text, out var ts))
            this.Filter.TargetSlope = ts;

        this.Filter.Threshold_dB = this.Threshold.VolumedB;

        if (!double.TryParse(this.mask_Attack.Text, out var tempAttack))
            tempAttack = this.Filter.AttackTime_ms;
        if (tempAttack < 1)
        {
            tempAttack = 1;
            this.mask_Attack.Text = "1";
        }
        this.Filter.AttackTime_ms = tempAttack;

        if (!double.TryParse(this.mask_Release.Text, out var tempRelease))
            tempRelease = this.Filter.ReleaseTime_ms;
        if (tempRelease < 1)
        {
            tempRelease = 1;
            this.mask_Release.Text = "1";
        }
        this.Filter.ReleaseTime_ms = tempRelease;

        if (!double.TryParse(this.msb_CompressionRatio.Text, out var tempRatio))
            tempRatio = this.Filter.Ratio;
        if (tempRatio < 11)
        {
            tempRatio = 11;
            this.msb_CompressionRatio.Text = "11";
        }
        this.Filter.Ratio = tempRatio;

        if (!double.TryParse(this.msb_KneeWidth_db.Text, out var tempKnee))
            tempKnee = this.Filter.KneeWidth_dB;
        if (tempKnee < 1)
        {
            tempKnee = 1;
            this.msb_KneeWidth_db.Text = "1";
        }
        this.Filter.KneeWidth_dB = tempKnee;

        this.Filter.UseSoftKnee = this.chkSoftKnee.Checked;

        if (Program.ASIO != null)
            this.Filter.ResetSampleRate(Program.ASIO.SampleRate_Current);

        this.Filter.ApplySettings();
    }

    public void SetDeepClonedFilter(IFilter input)
    {
        if (input is DEQ deq)
        {
            this.Filter = deq;

            this.cboDEQType.SelectedIndex = this.cboDEQType.Items.IndexOf(deq.DEQ_Type);
            this.cboBiquadType.SelectedIndex = this.cboBiquadType.Items.IndexOf(deq.Biquad_Type);
            this.cboThresholdType.SelectedIndex = this.cboThresholdType.Items.IndexOf(deq.Threshold_Type);

            this.txtF.Text = this.Filter.TargetFrequency.ToString();
            this.txtG.Text = this.Filter.TargetGain_dB.ToString();
            this.txtQ.Text = this.Filter.TargetQ.ToString();
            this.txtS.Text = this.Filter.TargetSlope.ToString();

            this.Threshold.VolumedB = this.Filter.Threshold_dB;
            this.mask_Attack.Text = this.Filter.AttackTime_ms < 1 ? "1" : this.Filter.AttackTime_ms.ToString();
            this.mask_Release.Text = this.Filter.ReleaseTime_ms < 1 ? "1" : this.Filter.ReleaseTime_ms.ToString();
            this.msb_CompressionRatio.Text = this.Filter.Ratio < 11 ? "11" : this.Filter.Ratio.ToString();
            this.msb_KneeWidth_db.Text = this.Filter.KneeWidth_dB < 1 ? "1" : this.Filter.KneeWidth_dB.ToString();
            this.chkSoftKnee.Checked = this.Filter.UseSoftKnee;

            if (Program.ASIO != null)
                this.Filter.ResetSampleRate(Program.ASIO.SampleRate_Current);

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