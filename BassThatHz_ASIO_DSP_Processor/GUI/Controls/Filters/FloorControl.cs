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
public partial class FloorControl : UserControl, IFilterControl
{
    #region Variables
    protected Floor Filter = new();
    #endregion

    #region Constructor and MapEventHandlers
    public FloorControl()
    {
        InitializeComponent();
        this.MapEventHandlers();
    }

    public void MapEventHandlers()
    {
        this.Threshold.VolumeChanged += this.ThresholdChanged;

        this.txtHoldInMS.KeyPress += TxtHoldInMS_KeyPress;
        this.txtHoldInMS.MaxLength = 5;

        this.txtRatio.KeyPress += txtRatio_KeyPress;
        this.txtRatio.MaxLength = 3;
    }
    #endregion

    #region Event Handlers

    #region InputValidation
    protected void TxtHoldInMS_KeyPress(object? sender, KeyPressEventArgs e)
    {
        InputValidator.Validate_IsNumeric_NonNegative(e);
        this.txtHoldInMS.Text = InputValidator.LimitTo_ReasonableSizedNumber(this.txtHoldInMS.Text);
    }

    protected void txtRatio_KeyPress(object? sender, KeyPressEventArgs e)
    {
        InputValidator.Validate_IsNumeric_NonNegative(e);
        this.txtRatio.Text = InputValidator.LimitTo_ReasonableSizedNumber(this.txtRatio.Text);
    }
    #endregion

    protected void ThresholdChanged(object? sender, System.EventArgs e)
    {
        try
        {
            this.Filter.MinValue = this.Threshold.Volume;
        }
        catch (Exception ex)
        {
            this.Error(ex);
        }
    }

    protected void btnApply_Click(object? sender, System.EventArgs e)
    {
        try
        {
            // Parse HoldInMS safely; fallback to existing value on failure
            if (!double.TryParse(this.txtHoldInMS.Text, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var holdMs))
                holdMs = this.Filter.HoldInMS.TotalMilliseconds;
            this.Filter.HoldInMS = TimeSpan.FromMilliseconds(holdMs);

            // Parse ratio safely and clamp to >= 1
            double ratio;
            if (!double.TryParse(this.txtRatio.Text, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out ratio))
                ratio = this.Filter.Ratio;

            if (ratio <= 1.0)
            {
                ratio = 1.0;
                // Use invariant culture to avoid culture-specific allocations
                this.txtRatio.Text = ratio.ToString(CultureInfo.InvariantCulture);
            }

            this.Filter.Ratio = ratio;
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
        this.btnApply_Click(this, EventArgs.Empty);
        this.Filter.ApplySettings();
    }

    public void SetDeepClonedFilter(IFilter input)
    {
        if (input is Floor floor)
        {
            this.Filter = floor;

            this.txtHoldInMS.Text = floor.HoldInMS.TotalMilliseconds.ToString(CultureInfo.InvariantCulture);
            this.txtRatio.Text = floor.Ratio.ToString(CultureInfo.InvariantCulture);
            this.Threshold.Volume = floor.MinValue;
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