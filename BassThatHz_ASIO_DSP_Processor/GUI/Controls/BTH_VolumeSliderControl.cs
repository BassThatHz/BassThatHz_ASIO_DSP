#nullable enable

namespace BassThatHz_ASIO_DSP_Processor;

#region Usings
using GUI;
using NAudio.Utils;
using System;
using System.ComponentModel;
using System.Drawing;
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
public partial class BTH_VolumeSliderControl : UserControl
{
    #region Variables
    protected TextBox txtDB = new() { Text = "0.00" };
    #endregion

    #region Constructor and MapEventHandlers
    public BTH_VolumeSliderControl()
    {
        InitializeComponent();
        this.DoubleBuffered = true;
        this.Controls.Add(this.txtDB);
        this.txtDB.CausesValidation = false;
        this.txtDB.Visible = false;
        this.txtDB.MaxLength = 7;
        this.Volume = 0;
        this.MapEventHandlers();
    }

    public void MapEventHandlers()
    {
        this.txtDB.LostFocus += TxtDB_LostFocus;

        this.txtDB.KeyDown += TxtDB_KeyDown;

        this.txtDB.KeyPress += TxtDB_KeyPress;

        this.txtDB.TextChanged += TxtDB_TextChanged;
    }

    #endregion

    #region Public Properties
    [DefaultValue(1d)]
    public double RestPosition { get; set; } = 1d;

    [DefaultValue(-384d)]
    public double MinDb { get; set; } = -384d;

    [DefaultValue(0d)]
    public double MaxDb { get; set; } = 0d;

    protected double _volume = 1.0d;
    [DefaultValue(1.0d)]
    public double Volume
    {
        get
        {
            return _volume;
        }
        set
        {
            var valdB = Decibels.LinearToDecibels(value); //Min/Max are in dB
            if (valdB < this.MinDb)
                valdB = this.MinDb;
            if (valdB > this.MaxDb)
                valdB = this.MaxDb;
            var TempVal = Decibels.DecibelsToLinear(valdB); //_volume is linear
            if (this._volume != TempVal)
            {
                this._volume = TempVal;
                this.lblDB.Text = String.Format("{0:F2} dB", this.VolumedB);
                this.txtDB.Text = String.Format("{0:F2}", this.VolumedB);
                this.VolumeChanged?.Invoke(this, EventArgs.Empty);
                this.Invalidate();
            }
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public double VolumedB
    {
        get
        {
            return Decibels.LinearToDecibels(this.Volume);
        }
        set
        {
            this.Volume = Decibels.DecibelsToLinear(value);
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool ReadOnly { get; set; } = false;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color TextColor { get; set; } = Color.Black;

    private Brush _sliderColor = Brushes.LightGreen;
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Brush SliderColor
    {
        get => _sliderColor;
        set
        {
            // Avoid null and avoid unnecessary assignments
            if (value is null)
                return;
            if (!object.ReferenceEquals(_sliderColor, value))
                _sliderColor = value;
        }
    }
    #endregion

    #region Events
    public event EventHandler? VolumeChanged;
    #endregion

    #region Event Handlers

    #region InputValidation
    protected void TxtDB_KeyPress(object? sender, KeyPressEventArgs e)
    {
        if (this.ReadOnly)
            return;
        InputValidator.Validate_IsNumeric_Negative(e);
    }

    protected void TxtDB_TextChanged(object? sender, EventArgs e)
    {
        if (double.TryParse(this.txtDB.Text, out double result))
        {
            if (result > this.MaxDb)
                this.txtDB.Text = this.MaxDb.ToString("F2");
            else if (result < this.MinDb)
                this.txtDB.Text = this.MinDb.ToString("F2");
        }
    }
    #endregion

    protected override void OnPaint(PaintEventArgs pe)
    {
        double db = this.VolumedB;
        double percent = this.RestPosition - db / this.MinDb;

        // Guard against invalid values and clamp to [0,1]
        if (double.IsNaN(percent) || double.IsInfinity(percent))
            percent = 0d;
        percent = Math.Min(1d, Math.Max(0d, percent));

        // Draw Rect
        if (this.lblDB.ForeColor != this.TextColor)
            this.lblDB.ForeColor = this.TextColor;

        pe.Graphics.DrawRectangle(Pens.Black, 0, 0, this.Width - 1, this.Height - 1);

        int fillWidth = (int)((this.Width - 1.99) * percent);
        if (fillWidth > 0)
            pe.Graphics.FillRectangle(this.SliderColor, 1, 1, fillWidth, this.Height - 2);

        this.CenterControl(this.lblDB);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (this.ReadOnly)
            return;

        if (e.Button == MouseButtons.Left)
        {
            this.SetVolumeFromMouse(e.X);
        }
        base.OnMouseMove(e);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (this.ReadOnly)
            return;

        this.SetVolumeFromMouse(e.X);
        base.OnMouseDown(e);
    }

    protected void lblDB_Click(object? sender, EventArgs e)
    {
        if (this.ReadOnly)
            return;

        this.txtDB.Visible = true;
        this.txtDB.Size = this.lblDB.Size;
        this.CenterControl(this.txtDB);

        _ = this.txtDB.Focus();
        this.txtDB.SelectAll();

        this.lblDB.Visible = false;
    }

    protected void TxtDB_LostFocus(object? sender, EventArgs e)
    {
        if (this.ReadOnly)
            return;

        ProcessUserInput();
    }

    protected void TxtDB_KeyDown(object? sender, KeyEventArgs e)
    {
        if (this.ReadOnly)
            return;

        if (e.KeyCode == Keys.Enter)
        {
            ProcessUserInput();
        }
        else if (e.KeyCode == Keys.Escape)
        {
            this.txtDB.Text = String.Format("{0:F2}", this.VolumedB);
            this.txtDB.Visible = false;
            this.lblDB.Visible = true;
        }
    }
    #endregion

    #region Protected Functions
    protected void ProcessUserInput()
    {
        if (double.TryParse(this.txtDB.Text, out double result))
        {
            if (result > this.MaxDb)
            {
                result = this.MaxDb;
                this.txtDB.Text = this.MaxDb.ToString("F2");
            }
            else if (result < this.MinDb)
            {
                result = this.MinDb;
                this.txtDB.Text = this.MinDb.ToString("F2");
            }
            this.VolumedB = result;
        }
        else
        {
            this.txtDB.Text = String.Format("{0:F2}", this.VolumedB);
        }
        this.lblDB.Visible = true;
        this.txtDB.Visible = false;
    }
    protected void SetVolumeFromMouse(int x)
    {
        // If click is at or past leftmost edge, set linear volume to 0
        if (x <= 0)
        {
            this.Volume = 0;
            return;
        }

        // Compute dB based on mouse position and set via VolumedB to avoid extra conversions
        double dbVolume = (1 - (double)x / this.Width) * this.MinDb;
        this.VolumedB = dbVolume;
    }

    protected void CenterControl(Control input)
    {
        var LabelX = (int)(this.Width * 0.5 - input.Width * 0.5);
        var LabelY = (int)(this.Height * 0.5 - input.Height * 0.5);
        input.Location = new Point(LabelX, LabelY);
    }

    #endregion 
}