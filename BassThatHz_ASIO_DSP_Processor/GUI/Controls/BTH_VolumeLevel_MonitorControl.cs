#nullable enable

namespace BassThatHz_ASIO_DSP_Processor.GUI.Controls;

#region Usings
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
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
/// <remarks>
/// Gradient level meter with a red peak bar and a dB ruler underneath.
/// <para>
/// This control exists ONLY for the Monitor screen (FormMonitoring, via
/// <see cref="BTH_VolumeLevelControl"/>). The plain meters used elsewhere in the app keep using
/// <see cref="BTH_VolumeLevel_SimpleControl"/> and must NOT be switched over to this one.
/// </para>
/// <para>
/// PERF: the Monitor screen refresh interval is user-settable down to 1 ms per meter, so the
/// ruler (up to ~100 tick marks plus its labels) is rendered once into <see cref="StaticCache"/>
/// and only re-rendered when the size, range or font changes. A repaint then costs one cached
/// blit, two rectangle fills and four shading lines.
/// </para>
/// </remarks>
public partial class BTH_VolumeLevel_MonitorControl : UserControl
{
    #region Constants
    /// <summary>Colour stops of the meter gradient, keyed by dB so the colours stay meaningful when the range changes.</summary>
    protected static readonly double[] GradientStop_Db = { -100, -80, -30, -18, -12, -10, -6, 0 };

    /// <summary>Colour for each entry of <see cref="GradientStop_Db"/>.</summary>
    protected static readonly Color[] GradientStop_Color =
    {
        Color.FromArgb(0, 0, 255),      //blue   (floor)
        Color.FromArgb(0, 0, 255),      //blue
        Color.FromArgb(0, 255, 0),      //green
        Color.FromArgb(0, 255, 0),      //green
        Color.FromArgb(255, 200, 0),    //amber
        Color.FromArgb(255, 200, 0),    //amber
        Color.FromArgb(255, 0, 0),      //red
        Color.FromArgb(255, 0, 0),      //red    (clip)
    };

    protected static readonly Color UnlitTrackColor = Color.Black;
    protected static readonly Color PeakBarColor = Color.Red;
    protected static readonly Color MinorTickColor = Color.FromArgb(176, 176, 176);
    protected static readonly Color MidTickColor = Color.FromArgb(216, 216, 216);
    protected static readonly Color MajorTickColor = Color.FromArgb(255, 255, 255);
    protected static readonly Color ScaleTextColor = Color.FromArgb(211, 211, 211);

    /// <summary>Outer 1px inset shadow drawn over the top and bottom row of the bar.</summary>
    protected static readonly Color BarEdgeShadeColor = Color.FromArgb(198, 0, 0, 0);

    /// <summary>Inner 1px inset shadow, one row in from <see cref="BarEdgeShadeColor"/>.</summary>
    protected static readonly Color BarInnerShadeColor = Color.FromArgb(56, 0, 0, 0);

    /// <summary>Bar height as a fraction of the control height.</summary>
    protected const double BarHeightRatio = 0.28;

    /// <summary>Ruler font em size as a fraction of the control height, in pixels.</summary>
    protected const double ScaleFontRatio = 0.27;

    protected const float MinScaleFontEmPixels = 7f;
    protected const float MaxScaleFontEmPixels = 22f;

    /// <summary>Candidate dB steps between labelled (major) ticks, smallest first.</summary>
    protected static readonly int[] LabelStepCandidates_Db = { 10, 20, 50, 100 };

    /// <summary>Minimum pixels per dB before the 1 dB ticks are dropped.</summary>
    protected const double MinPixelsPerMinorTick = 3.0;

    /// <summary>Minimum pixels per 5 dB before the 5 dB ticks are dropped.</summary>
    protected const double MinPixelsPerMidTick = 6.0;

    /// <summary>Track pixels per pixel of peak bar width.</summary>
    protected const double PeakBarWidthDivisor = 130.0;

    /// <summary>Typographic measuring format; avoids the side bearings GDI+ adds by default.</summary>
    protected static readonly StringFormat MeasureFormat = new(StringFormat.GenericTypographic)
    {
        FormatFlags = StringFormatFlags.NoWrap | StringFormatFlags.MeasureTrailingSpaces,
    };

    /// <summary>Ruler label format; labels are centred on their tick mark.</summary>
    protected static readonly StringFormat LabelFormat = new(StringFormat.GenericTypographic)
    {
        Alignment = StringAlignment.Center,
        LineAlignment = StringAlignment.Near,
        FormatFlags = StringFormatFlags.NoWrap | StringFormatFlags.NoClip,
    };
    #endregion

    #region Variables
    protected double _minDb = -100.0;
    protected double _maxDb = 0.0;
    protected double _dbLevel = double.NegativeInfinity;
    protected double _dbPeak = double.NegativeInfinity;

    /// <summary>Background, unlit track, tick marks and ruler labels; everything that does not change per refresh.</summary>
    protected Bitmap? StaticCache;

    /// <summary>Gradient spanning the WHOLE track, so a partial fill keeps the colour that belongs to its position.</summary>
    protected LinearGradientBrush? TrackBrush;

    protected Font? ScaleFont;

    /// <summary>Cached geometry; only meaningful while <see cref="StaticCache"/> is non-null.</summary>
    protected MeterLayout Geometry;
    #endregion

    #region MeterLayout
    /// <summary>Pixel geometry derived from the control size, the dB range and the ruler font.</summary>
    protected struct MeterLayout
    {
        public int TrackX;
        public int TrackWidth;
        public int BarTop;
        public int BarHeight;
        public int PeakBarWidth;
        public int TickTop;
        public int MajorTickLength;
        public int MidTickLength;
        public int MinorTickLength;
        public int LabelTop;
        public int LabelStep_Db;
        public bool ShowMinorTicks;
        public bool ShowMidTicks;
        public bool ShowLabels;
    }
    #endregion

    #region Public Properties
    /// <summary>Bottom of the displayed range, in dB.</summary>
    [DefaultValue(-100.0)]
    public double MinDb
    {
        get => this._minDb;
        set
        {
            if (double.IsNaN(value) || value.Equals(this._minDb))
                return;

            this._minDb = value;
            this.InvalidateCache();
        }
    }

    /// <summary>Top of the displayed range, in dB.</summary>
    [DefaultValue(0.0)]
    public double MaxDb
    {
        get => this._maxDb;
        set
        {
            if (double.IsNaN(value) || value.Equals(this._maxDb))
                return;

            this._maxDb = value;
            this.InvalidateCache();
        }
    }

    /// <summary>Current level (RMS), in dB; drives the length of the gradient fill.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public double DB_Level
    {
        get => this._dbLevel;
        set
        {
            if (double.IsNaN(value) || value.Equals(this._dbLevel))
                return;

            this._dbLevel = value;
            this.InvalidateBar();
        }
    }

    /// <summary>Current peak, in dB; drives the position of the red peak bar.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public double DB_Peak
    {
        get => this._dbPeak;
        set
        {
            if (double.IsNaN(value) || value.Equals(this._dbPeak))
                return;

            this._dbPeak = value;
            this.InvalidateBar();
        }
    }
    #endregion

    #region Constructor
    public BTH_VolumeLevel_MonitorControl()
    {
        InitializeComponent();

        this.SetStyle(ControlStyles.AllPaintingInWmPaint
            | ControlStyles.UserPaint
            | ControlStyles.OptimizedDoubleBuffer
            | ControlStyles.ResizeRedraw, true);
        this.DoubleBuffered = true;
    }
    #endregion

    #region Public Functions
    /// <summary>
    /// Converts a dB value to the pixel column of the track that represents it.
    /// </summary>
    /// <param name="db">The value to place.</param>
    /// <returns>An X coordinate clamped to the track.</returns>
    public int DbToX(double db)
    {
        var layout = this.Geometry;
        if (layout.TrackWidth < 2)
            return layout.TrackX;

        return layout.TrackX + (int)Math.Round(this.DbToFraction(db) * (layout.TrackWidth - 1));
    }

    /// <summary>
    /// Returns the gradient colour that belongs to a dB value.
    /// </summary>
    /// <param name="db">The value to look up.</param>
    /// <returns>The interpolated stop colour.</returns>
    public static Color ColorAtDb(double db)
    {
        int last = GradientStop_Db.Length - 1;

        if (double.IsNaN(db) || db <= GradientStop_Db[0])
            return GradientStop_Color[0];
        if (db >= GradientStop_Db[last])
            return GradientStop_Color[last];

        for (int i = 1; i <= last; i++)
        {
            double upper = GradientStop_Db[i];
            if (db > upper)
                continue;

            double lower = GradientStop_Db[i - 1];
            double span = upper - lower;
            double t = span <= 0 ? 0 : (db - lower) / span;

            var a = GradientStop_Color[i - 1];
            var b = GradientStop_Color[i];
            return Color.FromArgb(Lerp(a.R, b.R, t), Lerp(a.G, b.G, t), Lerp(a.B, b.B, t));
        }

        return GradientStop_Color[last];
    }
    #endregion

    #region Overrides
    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        var cache = this.EnsureCache(g);
        if (cache == null)
        {
            //Too small to hold a meter; UserPaint means nobody else fills the background.
            g.Clear(this.BackColor);
            return;
        }

        g.SmoothingMode = SmoothingMode.None;
        g.PixelOffsetMode = PixelOffsetMode.Half;
        g.DrawImageUnscaled(cache, 0, 0);

        var layout = this.Geometry;

        //Lit portion: one slice of the full-track gradient, so the colour at a given position
        //is the same whether the meter is pinned or barely moving.
        int litWidth = this.GetLitWidth();
        var brush = this.TrackBrush;
        if (litWidth > 0 && brush != null)
            g.FillRectangle(brush, layout.TrackX, layout.BarTop, litWidth, layout.BarHeight);

        this.DrawPeakBar(g, layout);
        this.DrawBarShading(g, layout);
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        this.InvalidateCache();
    }

    protected override void OnFontChanged(EventArgs e)
    {
        base.OnFontChanged(e);
        this.InvalidateCache();
    }

    protected override void OnBackColorChanged(EventArgs e)
    {
        base.OnBackColorChanged(e);
        this.InvalidateCache();
    }
    #endregion

    #region Protected Functions

    #region Cache Management
    /// <summary>
    /// Drops the cached bitmap, gradient and font so the next paint rebuilds them, then repaints.
    /// </summary>
    protected void InvalidateCache()
    {
        this.ReleaseCachedResources();
        if (!this.IsDisposed && !this.Disposing)
            this.Invalidate();
    }

    /// <summary>
    /// Repaints just the bar strip; the ruler underneath cannot have changed.
    /// </summary>
    protected void InvalidateBar()
    {
        if (this.IsDisposed || this.Disposing)
            return;

        var layout = this.Geometry;
        if (this.StaticCache == null || layout.BarHeight < 1)
        {
            this.Invalidate();
            return;
        }

        this.Invalidate(new Rectangle(0, layout.BarTop, this.Width, layout.BarHeight));
    }

    /// <summary>
    /// Disposes the cached GDI+ objects. Called from Dispose and whenever the cache goes stale.
    /// </summary>
    protected void ReleaseCachedResources()
    {
        this.StaticCache?.Dispose();
        this.StaticCache = null;

        this.TrackBrush?.Dispose();
        this.TrackBrush = null;

        this.ScaleFont?.Dispose();
        this.ScaleFont = null;
    }

    /// <summary>
    /// Returns the cached ruler bitmap, rebuilding it (and the layout, font and gradient) when stale.
    /// </summary>
    /// <param name="reference">A graphics context, used only for font metrics during a rebuild.</param>
    /// <returns>The cache, or null when the control is too small to draw anything.</returns>
    protected Bitmap? EnsureCache(Graphics reference)
    {
        var cache = this.StaticCache;
        if (cache != null && cache.Width == this.Width && cache.Height == this.Height)
            return cache;

        this.ReleaseCachedResources();

        int width = this.Width;
        int height = this.Height;
        if (width < 4 || height < 3)
            return null;

        this.ScaleFont = this.CreateScaleFont(height);
        this.Geometry = this.ComputeLayout(reference, width, height);
        this.TrackBrush = this.CreateTrackBrush(this.Geometry);

        cache = new Bitmap(width, height, PixelFormat.Format32bppPArgb);
        using (var g = Graphics.FromImage(cache))
            this.RenderStatic(g, this.Geometry, width, height);

        this.StaticCache = cache;
        return cache;
    }
    #endregion

    #region Layout
    /// <summary>
    /// Builds a font whose em size tracks the control height, so the ruler stays in proportion at
    /// both the (font-scaled) design size and the runtime size.
    /// </summary>
    /// <param name="height">The control height in pixels.</param>
    /// <returns>A font owned by this control.</returns>
    protected Font CreateScaleFont(int height)
    {
        float em = (float)(height * ScaleFontRatio);
        if (em < MinScaleFontEmPixels)
            em = MinScaleFontEmPixels;
        if (em > MaxScaleFontEmPixels)
            em = MaxScaleFontEmPixels;

        try
        {
            return new Font(this.Font.FontFamily, em, FontStyle.Regular, GraphicsUnit.Pixel);
        }
        catch (ArgumentException)
        {
            //A family that cannot render the regular style is possible with a custom ambient font.
            return new Font(FontFamily.GenericSansSerif, em, FontStyle.Regular, GraphicsUnit.Pixel);
        }
    }

    /// <summary>
    /// Derives every pixel coordinate the meter needs from the control size and the ruler font.
    /// </summary>
    /// <param name="g">Graphics used for text measurement.</param>
    /// <param name="width">The control width.</param>
    /// <param name="height">The control height.</param>
    /// <returns>The geometry for this size.</returns>
    protected MeterLayout ComputeLayout(Graphics g, int width, int height)
    {
        var layout = default(MeterLayout);

        layout.BarTop = 1;
        layout.BarHeight = Math.Max(3, (int)Math.Round(height * BarHeightRatio));
        if (layout.BarTop + layout.BarHeight > height)
            layout.BarHeight = Math.Max(1, height - layout.BarTop);

        layout.TickTop = Math.Min(height, layout.BarTop + layout.BarHeight + 2);

        int labelHeight = (int)Math.Ceiling(this.ScaleFont?.GetHeight(g) ?? 0f);
        int rulerRoom = height - layout.TickTop;

        layout.ShowLabels = rulerRoom >= labelHeight + 4;
        layout.MajorTickLength = layout.ShowLabels
            ? Math.Max(3, rulerRoom - labelHeight - 1)
            : Math.Max(0, rulerRoom - 1);
        layout.MidTickLength = Math.Max(2, (int)Math.Round(layout.MajorTickLength * 0.78));
        layout.MinorTickLength = Math.Max(1, (int)Math.Round(layout.MajorTickLength * 0.55));
        layout.LabelTop = layout.TickTop + layout.MajorTickLength + 1;

        //The ruler is centred on its ticks, so the outermost labels each need half their own
        //width of margin to stay inside the control.
        int leftInset = 1;
        int rightInset = 1;
        if (layout.ShowLabels)
        {
            leftInset = HalfTextWidth(g, this.FormatDb(FirstLabelDb(this._minDb, 10)), this.ScaleFont) + 2;
            rightInset = HalfTextWidth(g, this.FormatDb(LastLabelDb(this._maxDb, 10)), this.ScaleFont) + 2;
        }

        if (leftInset + rightInset >= width)
        {
            leftInset = 0;
            rightInset = 0;
        }

        layout.TrackX = leftInset;
        layout.TrackWidth = Math.Max(1, width - leftInset - rightInset);

        double range = this._maxDb - this._minDb;
        double pixelsPerDb = range > 0 ? (layout.TrackWidth - 1) / range : 0;
        layout.ShowMinorTicks = pixelsPerDb >= MinPixelsPerMinorTick;
        layout.ShowMidTicks = pixelsPerDb * 5 >= MinPixelsPerMidTick;
        layout.LabelStep_Db = this.PickLabelStep(g, pixelsPerDb);
        layout.PeakBarWidth = Math.Max(2, (int)Math.Round(layout.TrackWidth / PeakBarWidthDivisor));

        return layout;
    }

    /// <summary>
    /// Picks the finest step that still keeps neighbouring labels from touching.
    /// </summary>
    /// <param name="g">Graphics used for text measurement.</param>
    /// <param name="pixelsPerDb">Track pixels per dB.</param>
    /// <returns>The dB distance between labelled ticks.</returns>
    protected int PickLabelStep(Graphics g, double pixelsPerDb)
    {
        //The floor label ("-100" by default) is the widest one on the ruler.
        float widest = MeasureText(g, this.FormatDb(FirstLabelDb(this._minDb, 10)), this.ScaleFont);

        for (int i = 0; i < LabelStepCandidates_Db.Length; i++)
        {
            int step = LabelStepCandidates_Db[i];
            if (widest + 4 <= pixelsPerDb * step)
                return step;
        }

        return LabelStepCandidates_Db[LabelStepCandidates_Db.Length - 1];
    }
    #endregion

    #region Rendering
    /// <summary>
    /// Draws everything that does not change per refresh: background, unlit track and ruler.
    /// </summary>
    /// <param name="g">The cache bitmap's graphics.</param>
    /// <param name="layout">The geometry to draw with.</param>
    /// <param name="width">The control width.</param>
    /// <param name="height">The control height.</param>
    protected void RenderStatic(Graphics g, MeterLayout layout, int width, int height)
    {
        g.SmoothingMode = SmoothingMode.None;
        g.PixelOffsetMode = PixelOffsetMode.Half;

        using (var back = new SolidBrush(this.BackColor))
            g.FillRectangle(back, 0, 0, width, height);

        using (var unlit = new SolidBrush(UnlitTrackColor))
            g.FillRectangle(unlit, layout.TrackX, layout.BarTop, layout.TrackWidth, layout.BarHeight);

        this.DrawRuler(g, layout);
    }

    /// <summary>
    /// Draws the tick marks and their dB labels.
    /// </summary>
    /// <param name="g">The cache bitmap's graphics.</param>
    /// <param name="layout">The geometry to draw with.</param>
    protected void DrawRuler(Graphics g, MeterLayout layout)
    {
        if (layout.MajorTickLength < 1 || layout.TrackWidth < 2)
            return;

        int firstDb = (int)Math.Ceiling(this._minDb);
        int lastDb = (int)Math.Floor(this._maxDb);
        if (lastDb < firstDb)
            return;

        int labelStep = Math.Max(1, layout.LabelStep_Db);
        var font = this.ScaleFont;

        using var minor = new SolidBrush(MinorTickColor);
        using var mid = new SolidBrush(MidTickColor);
        using var major = new SolidBrush(MajorTickColor);
        using var text = new SolidBrush(ScaleTextColor);

        g.TextRenderingHint = TextRenderingHint.AntiAlias;

        for (int db = firstDb; db <= lastDb; db++)
        {
            bool isMajor = db % labelStep == 0;
            bool isMid = !isMajor && db % 5 == 0;

            if (isMid && !layout.ShowMidTicks)
                continue;
            if (!isMajor && !isMid && !layout.ShowMinorTicks)
                continue;

            int x = this.DbToX(db);
            int length = isMajor ? layout.MajorTickLength : isMid ? layout.MidTickLength : layout.MinorTickLength;
            var brush = isMajor ? major : isMid ? mid : minor;

            g.FillRectangle(brush, x, layout.TickTop, 1, length);

            if (isMajor && layout.ShowLabels && font != null)
                g.DrawString(this.FormatDb(db), font, text, x, layout.LabelTop, LabelFormat);
        }
    }

    /// <summary>
    /// Draws the red peak bar at the peak position, clamped so it always sits inside the track.
    /// </summary>
    /// <param name="g">The target graphics.</param>
    /// <param name="layout">The geometry to draw with.</param>
    protected void DrawPeakBar(Graphics g, MeterLayout layout)
    {
        double peak = this._dbPeak;
        if (double.IsNaN(peak) || peak <= this._minDb || layout.TrackWidth < layout.PeakBarWidth)
            return;

        int x = this.DbToX(peak) - layout.PeakBarWidth / 2;
        int leftLimit = layout.TrackX;
        int rightLimit = layout.TrackX + layout.TrackWidth - layout.PeakBarWidth;

        if (x < leftLimit)
            x = leftLimit;
        if (x > rightLimit)
            x = rightLimit;

        using var peakBrush = new SolidBrush(PeakBarColor);
        g.FillRectangle(peakBrush, x, layout.BarTop, layout.PeakBarWidth, layout.BarHeight);
    }

    /// <summary>
    /// Overlays the 1px inset shadow that gives the bar its recessed look.
    /// </summary>
    /// <param name="g">The target graphics.</param>
    /// <param name="layout">The geometry to draw with.</param>
    protected void DrawBarShading(Graphics g, MeterLayout layout)
    {
        if (layout.BarHeight < 2 || layout.TrackWidth < 1)
            return;

        int top = layout.BarTop;
        int bottom = layout.BarTop + layout.BarHeight - 1;

        using (var edge = new SolidBrush(BarEdgeShadeColor))
        {
            g.FillRectangle(edge, layout.TrackX, top, layout.TrackWidth, 1);
            g.FillRectangle(edge, layout.TrackX, bottom, layout.TrackWidth, 1);
        }

        if (layout.BarHeight < 6)
            return;

        using var inner = new SolidBrush(BarInnerShadeColor);
        g.FillRectangle(inner, layout.TrackX, top + 1, layout.TrackWidth, 1);
        g.FillRectangle(inner, layout.TrackX, bottom - 1, layout.TrackWidth, 1);
    }

    /// <summary>
    /// Builds the gradient that spans the whole track.
    /// </summary>
    /// <param name="layout">The geometry the gradient must cover.</param>
    /// <returns>A brush owned by this control, or null when there is no track to fill.</returns>
    protected LinearGradientBrush? CreateTrackBrush(MeterLayout layout)
    {
        if (layout.TrackWidth < 1 || layout.BarHeight < 1)
            return null;

        double min = this._minDb;
        double max = this._maxDb;
        if (max <= min)
            return null;

        //One extra pixel each side keeps GDI+ from wrapping the gradient at the edges.
        var bounds = new Rectangle(layout.TrackX - 1, layout.BarTop, layout.TrackWidth + 2, layout.BarHeight);
        var brush = new LinearGradientBrush(bounds, ColorAtDb(min), ColorAtDb(max), LinearGradientMode.Horizontal);

        var positions = new List<float>(GradientStop_Db.Length + 2);
        var colors = new List<Color>(GradientStop_Db.Length + 2);

        AddBlendStop(positions, colors, 0f, ColorAtDb(min));
        for (int i = 0; i < GradientStop_Db.Length; i++)
        {
            double db = GradientStop_Db[i];
            if (db <= min || db >= max)
                continue;

            AddBlendStop(positions, colors, (float)((db - min) / (max - min)), GradientStop_Color[i]);
        }
        AddBlendStop(positions, colors, 1f, ColorAtDb(max));

        if (positions.Count >= 2)
        {
            brush.InterpolationColors = new ColorBlend(positions.Count)
            {
                Positions = positions.ToArray(),
                Colors = colors.ToArray(),
            };
        }

        return brush;
    }
    #endregion

    #region Helpers
    /// <summary>
    /// Appends a gradient stop, collapsing any that is not strictly to the right of the previous
    /// one (GDI+ rejects a <see cref="ColorBlend"/> whose positions do not increase).
    /// </summary>
    /// <param name="positions">The positions collected so far.</param>
    /// <param name="colors">The colours collected so far.</param>
    /// <param name="position">The position to add, in 0..1.</param>
    /// <param name="color">The colour at that position.</param>
    protected static void AddBlendStop(List<float> positions, List<Color> colors, float position, Color color)
    {
        if (position < 0f)
            position = 0f;
        if (position > 1f)
            position = 1f;

        int count = positions.Count;
        if (count > 0 && position <= positions[count - 1])
        {
            //Collapsed onto the previous stop: keep the later colour so the clamped end of the
            //ramp still shows the hue that belongs there.
            colors[count - 1] = color;
            return;
        }

        positions.Add(position);
        colors.Add(color);
    }

    /// <summary>
    /// Maps a dB value onto 0..1 across the displayed range.
    /// </summary>
    /// <param name="db">The value to map.</param>
    /// <returns>A clamped fraction of the track.</returns>
    protected double DbToFraction(double db)
    {
        double min = this._minDb;
        double max = this._maxDb;
        if (max <= min || double.IsNaN(db))
            return 0.0;

        if (db <= min)
            return 0.0;
        if (db >= max)
            return 1.0;

        return (db - min) / (max - min);
    }

    /// <summary>
    /// Returns how many pixels of the track the current level lights up.
    /// </summary>
    /// <returns>A width in pixels; 0 when the level is at or below <see cref="MinDb"/>.</returns>
    protected int GetLitWidth()
    {
        var layout = this.Geometry;
        double level = this._dbLevel;
        if (double.IsNaN(level) || level <= this._minDb || layout.TrackWidth < 1)
            return 0;

        int width = this.DbToX(level) - layout.TrackX + 1;
        if (width < 1)
            return 0;
        if (width > layout.TrackWidth)
            width = layout.TrackWidth;

        return width;
    }

    /// <summary>
    /// Formats a ruler label: whole dB, no unit, so the numbers stay as narrow as possible.
    /// </summary>
    /// <param name="db">The value to format.</param>
    /// <returns>The label text.</returns>
    protected string FormatDb(double db)
    {
        return ((int)Math.Round(db)).ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>Lowest labelled dB at or above <paramref name="minDb"/>.</summary>
    /// <param name="minDb">The bottom of the range.</param>
    /// <param name="step">The dB distance between labels.</param>
    /// <returns>The first label value.</returns>
    protected static int FirstLabelDb(double minDb, int step)
    {
        if (step < 1)
            step = 1;
        return (int)Math.Ceiling(minDb / step) * step;
    }

    /// <summary>Highest labelled dB at or below <paramref name="maxDb"/>.</summary>
    /// <param name="maxDb">The top of the range.</param>
    /// <param name="step">The dB distance between labels.</param>
    /// <returns>The last label value.</returns>
    protected static int LastLabelDb(double maxDb, int step)
    {
        if (step < 1)
            step = 1;
        return (int)Math.Floor(maxDb / step) * step;
    }

    /// <summary>Measures a ruler label in pixels.</summary>
    /// <param name="g">Graphics used for measurement.</param>
    /// <param name="value">The text to measure.</param>
    /// <param name="font">The ruler font.</param>
    /// <returns>The text width, or 0 when there is nothing to measure.</returns>
    protected static float MeasureText(Graphics g, string value, Font? font)
    {
        if (font == null || string.IsNullOrEmpty(value))
            return 0f;

        return g.MeasureString(value, font, PointF.Empty, MeasureFormat).Width;
    }

    /// <summary>Half of <see cref="MeasureText"/>, rounded up.</summary>
    /// <param name="g">Graphics used for measurement.</param>
    /// <param name="value">The text to measure.</param>
    /// <param name="font">The ruler font.</param>
    /// <returns>Half the text width in whole pixels.</returns>
    protected static int HalfTextWidth(Graphics g, string value, Font? font)
    {
        return (int)Math.Ceiling(MeasureText(g, value, font) / 2f);
    }

    /// <summary>Interpolates one colour channel and clamps it to a byte.</summary>
    /// <param name="from">The channel at t=0.</param>
    /// <param name="to">The channel at t=1.</param>
    /// <param name="t">The interpolation factor.</param>
    /// <returns>The interpolated channel value.</returns>
    protected static int Lerp(int from, int to, double t)
    {
        int value = (int)Math.Round(from + (to - from) * t);
        if (value < 0)
            return 0;
        if (value > 255)
            return 255;
        return value;
    }
    #endregion

    #endregion
}
