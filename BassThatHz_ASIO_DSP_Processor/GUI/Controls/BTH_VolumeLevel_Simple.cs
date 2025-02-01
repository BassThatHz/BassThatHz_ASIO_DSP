#nullable enable

namespace BassThatHz_ASIO_DSP_Processor.GUI.Controls;

#region Usings
using System.ComponentModel;
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
public partial class BTH_VolumeLevel_Simple : UserControl
{
    #region Public Properties
    [DefaultValue(-60f)]
    public double MinDb { get; set; } = -60;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public double DB_Level { get; set; } = double.MinValue;
    #endregion

    #region Constructor
    public BTH_VolumeLevel_Simple()
    {
        InitializeComponent();

        this.DoubleBuffered = true;
        this.MapEventHandlers();
    }

    public void MapEventHandlers()
    {
        this.Paint += Simple_Paint;
    }
    #endregion

    #region Event Handlers
    protected void Simple_Paint(object? sender, PaintEventArgs e)
    {
        double percent = 1 - this.DB_Level / this.MinDb;
        //Draw Rect
        e.Graphics.DrawRectangle(Pens.Black, 0, 0, this.Width - 1, this.Height - 1);
        e.Graphics.FillRectangle(Brushes.LightGreen, 1, 1, (int)((this.Width - 1.99) * percent), this.Height - 2);
    }
    #endregion
}