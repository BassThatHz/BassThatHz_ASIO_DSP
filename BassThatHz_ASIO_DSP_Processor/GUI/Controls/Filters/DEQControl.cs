#nullable enable

namespace BassThatHz_ASIO_DSP_Processor.GUI.Controls;

#region Usings
using System;
using System.Linq;
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
public partial class DEQControl : UserControl, IFilterControl
{
    #region Variables
    protected DEQ Filter = new();
    #endregion

    #region Constructor and MapEventHandlers
    public DEQControl()
    {
        InitializeComponent();

        var EnumArray = Enum.GetValues(typeof(DEQ.DEQType)).Cast<object>().ToArray();
        this.cboDEQType.Items.AddRange(EnumArray);
        this.cboDEQType.SelectedIndex = this.cboDEQType.Items.IndexOf(DEQ.DEQType.BoostBelow);

        var EnumArray2 = Enum.GetValues(typeof(DEQ.BiquadType)).Cast<object>().ToArray();
        this.cboBiquadType.Items.AddRange(EnumArray2);
        this.cboBiquadType.SelectedIndex = this.cboBiquadType.Items.IndexOf(DEQ.BiquadType.PEQ);

        var EnumArray3 = Enum.GetValues(typeof(DEQ.ThresholdType)).Cast<object>().ToArray();
        this.cboThresholdType.Items.AddRange(EnumArray3);
        this.cboThresholdType.SelectedIndex = this.cboThresholdType.Items.IndexOf(DEQ.ThresholdType.Peak); 

        this.DynamicsApplied.ReadOnly = true;
        this.MapEventHandlers();
    }

    public void MapEventHandlers()
    {
        SampleRateChangeNotifier.SampleRateChanged += SampleRateChangeNotifier_SampleRateChanged;
    }
    #endregion

    #region Event Handlers

    protected void RefreshTimer_Tick(object sender, EventArgs e)
    {
        try
        {
            this.DynamicsApplied.VolumedB = this.Filter.GainApplied;
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
        if (this.cboDEQType.SelectedItem != null)
            this.Filter.DEQ_Type = (DEQ.DEQType)this.cboDEQType.SelectedItem;
        if (this.cboBiquadType.SelectedItem != null)
            this.Filter.Biquad_Type = (DEQ.BiquadType)this.cboBiquadType.SelectedItem;
        if (this.cboThresholdType.SelectedItem != null)
            this.Filter.Threshold_Type = (DEQ.ThresholdType)this.cboThresholdType.SelectedItem;

        this.Filter.TargetFrequency = double.Parse(this.txtF.Text);
        this.Filter.TargetGain_dB = double.Parse(this.txtG.Text);
        this.Filter.TargetQ = double.Parse(this.txtQ.Text);
        this.Filter.TargetSlope = double.Parse(this.txtS.Text);

        this.Filter.Threshold_dB = this.Threshold.VolumedB;

        var TempAttackTime_ms = double.Parse(this.mask_Attack.Text);
        if (TempAttackTime_ms < 1)
        {
            TempAttackTime_ms = 1;
            this.mask_Attack.Text = "1";
        }
        this.Filter.AttackTime_ms = TempAttackTime_ms;

        var TempReleaseTime_ms = double.Parse(this.mask_Release.Text);
        if (TempReleaseTime_ms < 1)
        {
            TempReleaseTime_ms = 1;
            this.mask_Release.Text = "1";
        }
        this.Filter.ReleaseTime_ms = TempReleaseTime_ms;

        var TempRatio = double.Parse(this.msb_CompressionRatio.Text);
        if (TempRatio < 11)
        {
            TempRatio = 11;
            this.msb_CompressionRatio.Text = "11";
        }
        this.Filter.Ratio = TempRatio;

        var TempKneeWidth_dB = double.Parse(this.msb_KneeWidth_db.Text);
        if (TempKneeWidth_dB < 1)
        {
            TempKneeWidth_dB = 1;
            this.msb_KneeWidth_db.Text = "1";
        }
        this.Filter.KneeWidth_dB = TempKneeWidth_dB;


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