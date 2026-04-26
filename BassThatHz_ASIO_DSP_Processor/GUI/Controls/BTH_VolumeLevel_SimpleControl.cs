#nullable enable

namespace BassThatHz_ASIO_DSP_Processor.GUI.Controls;

#region Usings
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
public partial class BTH_VolumeLevel_SimpleControl : UserControl
{
    #region Public Properties
    // Backing fields to avoid unnecessary allocations and allow change detection
    private double _minDb = -60.0;
    private double _dbLevel = -60.0; // initialize to _minDb to avoid invalid calculations

    [DefaultValue(-60.0)]
    public double MinDb
    {
        get => _minDb;
        set
        {
            if (double.IsNaN(value) || value.Equals(_minDb))
                return;

            _minDb = value;

            // Ensure DB level remains within sensible bounds relative to the new MinDb
            if (_dbLevel < _minDb)
                _dbLevel = _minDb;

            Invalidate();
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public double DB_Level
    {
        get => _dbLevel;
        set
        {
            if (double.IsNaN(value) || value.Equals(_dbLevel))
                return;

            _dbLevel = value;
            // Only repaint when value actually changes
            Invalidate();
        }
    }
    #endregion

    #region Constructor
    public BTH_VolumeLevel_SimpleControl()
    {
        InitializeComponent();

        this.DoubleBuffered = true;
        this.MapEventHandlers();
    }

    // Make event mapping idempotent to avoid accidental multiple attachments
    // Exposed publicly so unit tests can verify event hookup.
    public void MapEventHandlers()
    {
        // Remove first to ensure we don't attach multiple times
        this.Paint -= Simple_Paint;
        this.Paint += Simple_Paint;
    }
    #endregion

    #region Event Handlers
    protected void Simple_Paint(object? sender, PaintEventArgs e)
    {
        // Use local copies to avoid repeated field access
        double minDb = _minDb;
        double dbLevel = _dbLevel;

        double percent;
        // Handle edge cases and ensure percent in [0,1]
        if (double.IsNaN(dbLevel) || double.IsNaN(minDb))
        {
            percent = 0.0;
        }
        else if (dbLevel <= minDb)
        {
            percent = 0.0;
        }
        else if (dbLevel >= 0.0)
        {
            percent = 1.0;
        }
        else
        {
            // Original logic: percent = 1 - DB_Level / MinDb
            percent = 1.0 - dbLevel / minDb;
        }

        if (percent <= 0.0)
        {
            // Draw only the border
            e.Graphics.DrawRectangle(Pens.Black, 0, 0, Math.Max(0, this.Width - 1), Math.Max(0, this.Height - 1));
            return;
        }

        // Draw border and filled area. Use float overloads to minimize conversions.
        int outerW = Math.Max(0, this.Width - 1);
        int outerH = Math.Max(0, this.Height - 1);
        e.Graphics.DrawRectangle(Pens.Black, 0, 0, outerW, outerH);

        // Inner available area (subtract 2 for the border lines)
        float innerW = Math.Max(0f, outerW - 1f);
        float innerH = Math.Max(0f, outerH - 1f);

        float fillW = (float)Math.Round(innerW * Math.Min(1.0, Math.Max(0.0, percent)));

        if (fillW > 0f && innerH > 0f)
        {
            e.Graphics.FillRectangle(Brushes.LightGreen, 1f, 1f, fillW, innerH);
        }
    }
    #endregion
}