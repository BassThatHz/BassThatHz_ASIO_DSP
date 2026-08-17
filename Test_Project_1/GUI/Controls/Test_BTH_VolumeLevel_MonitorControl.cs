using BassThatHz_ASIO_DSP_Processor.GUI.Controls;
using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace Test_Project_1;

/// <summary>
/// Covers the Monitor-screen gradient meter. The paint tests render the control at the size it
/// is actually given at runtime (404 x 36) and assert the visual contract: a coloured fill up to
/// the level, black past it, a red peak bar at the peak, and a tick ruler underneath.
/// </summary>
[TestClass]
public class Test_BTH_VolumeLevel_MonitorControl
{
    #region Constants
    //The runtime size BTH_VolumeLevelControl gives each meter once AutoScaleMode.Font is applied.
    protected const int RuntimeWidth = 404;
    protected const int RuntimeHeight = 36;

    //Row 6 sits in the middle of the bar, clear of the 1px inset shading at rows 1-2 and 9-10.
    protected const int BarMidRow = 6;
    #endregion

    #region Properties
    [TestMethod]
    public void DefaultPropertyValues_AreCorrect()
    {
        using var control = new BTH_VolumeLevel_MonitorControl();
        Assert.AreEqual(-100.0, control.MinDb);
        Assert.AreEqual(0.0, control.MaxDb);
        Assert.AreEqual(double.NegativeInfinity, control.DB_Level);
        Assert.AreEqual(double.NegativeInfinity, control.DB_Peak);
    }

    [TestMethod]
    public void PropertySetters_WorkCorrectly()
    {
        using var control = new BTH_VolumeLevel_MonitorControl
        {
            MinDb = -80,
            MaxDb = 6,
            DB_Level = -40,
            DB_Peak = -30,
        };

        Assert.AreEqual(-80.0, control.MinDb);
        Assert.AreEqual(6.0, control.MaxDb);
        Assert.AreEqual(-40.0, control.DB_Level);
        Assert.AreEqual(-30.0, control.DB_Peak);
    }

    [TestMethod]
    public void PropertySetters_IgnoreNaN()
    {
        using var control = new BTH_VolumeLevel_MonitorControl
        {
            MinDb = -90,
            MaxDb = 3,
            DB_Level = -40,
            DB_Peak = -30,
        };

        control.MinDb = double.NaN;
        control.MaxDb = double.NaN;
        control.DB_Level = double.NaN;
        control.DB_Peak = double.NaN;

        Assert.AreEqual(-90.0, control.MinDb);
        Assert.AreEqual(3.0, control.MaxDb);
        Assert.AreEqual(-40.0, control.DB_Level);
        Assert.AreEqual(-30.0, control.DB_Peak);
    }
    #endregion

    #region ColorAtDb
    [TestMethod]
    public void ColorAtDb_ReturnsStopColours_AtTheAnchors()
    {
        Assert.AreEqual(Color.FromArgb(0, 0, 255), BTH_VolumeLevel_MonitorControl.ColorAtDb(-80));
        Assert.AreEqual(Color.FromArgb(0, 255, 0), BTH_VolumeLevel_MonitorControl.ColorAtDb(-30));
        Assert.AreEqual(Color.FromArgb(255, 0, 0), BTH_VolumeLevel_MonitorControl.ColorAtDb(-6));
    }

    [TestMethod]
    public void ColorAtDb_ClampsOutsideTheStopTable()
    {
        Assert.AreEqual(Color.FromArgb(0, 0, 255), BTH_VolumeLevel_MonitorControl.ColorAtDb(-500));
        Assert.AreEqual(Color.FromArgb(255, 0, 0), BTH_VolumeLevel_MonitorControl.ColorAtDb(24));
        Assert.AreEqual(Color.FromArgb(0, 0, 255), BTH_VolumeLevel_MonitorControl.ColorAtDb(double.NaN));
    }

    [TestMethod]
    public void ColorAtDb_InterpolatesBetweenStops()
    {
        //Halfway from blue (-80) to green (-30).
        var middle = BTH_VolumeLevel_MonitorControl.ColorAtDb(-55);
        Assert.AreEqual(0, middle.R);
        Assert.IsTrue(middle.G > 100 && middle.G < 155, "G was " + middle.G);
        Assert.IsTrue(middle.B > 100 && middle.B < 155, "B was " + middle.B);
    }
    #endregion

    #region Painting
    [TestMethod]
    public void Paint_FillsUpToTheLevel_AndLeavesTheRestBlack()
    {
        using var bmp = Paint(RuntimeWidth, RuntimeHeight, -100, 0, level: -50, peak: double.NegativeInfinity);

        //-50 dB is the middle of a -100..0 track, so the left half is lit and the right half is not.
        var lit = bmp.GetPixel(100, BarMidRow);
        var unlit = bmp.GetPixel(350, BarMidRow);

        Assert.AreNotEqual(Color.Black.ToArgb(), lit.ToArgb(), "the lit half should be coloured");
        Assert.IsTrue(lit.B > lit.G, "at roughly -77 dB the gradient should still be blue-dominant");
        Assert.AreEqual(Color.Black.ToArgb(), unlit.ToArgb(), "past the level the track should be black");
    }

    [TestMethod]
    public void Paint_Silence_LeavesTheWholeTrackBlack()
    {
        using var bmp = Paint(RuntimeWidth, RuntimeHeight, -100, 0, double.NegativeInfinity, double.NegativeInfinity);

        Assert.AreEqual(Color.Black.ToArgb(), bmp.GetPixel(13, BarMidRow).ToArgb());
        Assert.AreEqual(Color.Black.ToArgb(), bmp.GetPixel(200, BarMidRow).ToArgb());
        Assert.AreEqual(Color.Black.ToArgb(), bmp.GetPixel(390, BarMidRow).ToArgb());
    }

    [TestMethod]
    public void Paint_FullScale_LightsTheWholeTrack_EndingInRed()
    {
        using var bmp = Paint(RuntimeWidth, RuntimeHeight, -100, 0, level: 0, peak: double.NegativeInfinity);

        var start = bmp.GetPixel(13, BarMidRow);
        var end = bmp.GetPixel(396, BarMidRow);

        Assert.IsTrue(start.B > 200 && start.R < 40, "the floor of the meter should be blue, was " + start);
        Assert.IsTrue(end.R > 200 && end.G < 60, "the top of the meter should be red, was " + end);
    }

    [TestMethod]
    public void Paint_DrawsTheRedPeakBar_AtThePeak()
    {
        //-25 dB of a -100..0 track lands three quarters along, well clear of the -50 dB fill.
        using var bmp = Paint(RuntimeWidth, RuntimeHeight, -100, 0, level: -50, peak: -25);

        int expectedX = 12 + (int)Math.Round(0.75 * 386);
        Assert.AreEqual(Color.Red.ToArgb(), bmp.GetPixel(expectedX, BarMidRow).ToArgb(),
            "no red peak bar at x=" + expectedX);

        //It must be a bar, not a smear: a little to the left of it is still unlit track.
        Assert.AreEqual(Color.Black.ToArgb(), bmp.GetPixel(expectedX - 12, BarMidRow).ToArgb());
    }

    [TestMethod]
    public void Paint_PeakBar_StaysInsideTheTrack_WhenPinned()
    {
        using var bmp = Paint(RuntimeWidth, RuntimeHeight, -100, 0, level: 0, peak: 40);

        //Clamped against the right edge of the track rather than spilling into the margin.
        Assert.AreEqual(Color.Red.ToArgb(), bmp.GetPixel(397, BarMidRow).ToArgb());
        Assert.AreNotEqual(Color.Red.ToArgb(), bmp.GetPixel(402, BarMidRow).ToArgb());
    }

    [TestMethod]
    public void Paint_DrawsARulerBelowTheBar()
    {
        using var bmp = Paint(RuntimeWidth, RuntimeHeight, -100, 0, double.NegativeInfinity, double.NegativeInfinity);

        //The -100 and 0 dB major ticks sit at the two ends of the track.
        Assert.IsTrue(IsLight(bmp.GetPixel(12, 15)), "no tick at the -100 dB end");
        Assert.IsTrue(IsLight(bmp.GetPixel(398, 15)), "no tick at the 0 dB end");

        //And the labels are painted under the ticks.
        bool anyLabelPixel = false;
        for (int y = 24; y < RuntimeHeight && !anyLabelPixel; y++)
            for (int x = 0; x < 30; x++)
                if (IsLight(bmp.GetPixel(x, y)))
                {
                    anyLabelPixel = true;
                    break;
                }

        Assert.IsTrue(anyLabelPixel, "the -100 label was not drawn");
    }

    [TestMethod]
    public void Paint_HonoursAnAlternateRange()
    {
        //A range whose floor sits above several gradient stops exercises the clamping in the blend.
        using var bmp = Paint(RuntimeWidth, RuntimeHeight, -60, 18, level: 18, peak: double.NegativeInfinity);

        var start = bmp.GetPixel(RuntimeWidth / 2, BarMidRow);
        Assert.AreNotEqual(Color.Black.ToArgb(), start.ToArgb(), "a full meter should be lit across its width");
    }

    [TestMethod]
    public void Paint_DoesNotThrow_AtDegenerateSizes()
    {
        var sizes = new[]
        {
            new Size(1, 1), new Size(4, 3), new Size(83, 9), new Size(40, 36),
            new Size(900, 36), new Size(404, 120),
        };

        foreach (var size in sizes)
        {
            using var bmp = Paint(size.Width, size.Height, -100, 0, -20, -12);
            Assert.IsNotNull(bmp, "failed at " + size);
        }
    }

    [TestMethod]
    public void Paint_DoesNotThrow_WhenTheRangeIsInverted()
    {
        using var bmp = Paint(RuntimeWidth, RuntimeHeight, 0, -100, -20, -12);
        Assert.IsNotNull(bmp);
    }
    #endregion

    #region Resources
    [TestMethod]
    public void Resize_RebuildsTheCache()
    {
        using var control = Build(RuntimeWidth, RuntimeHeight, -100, 0, -20, -12);
        PaintInto(control);
        Assert.IsNotNull(GetCache(control), "the cache should exist after a paint");

        control.Size = new Size(300, 30);
        Assert.IsNull(GetCache(control), "resizing should drop the stale cache");

        PaintInto(control);
        var rebuilt = GetCache(control);
        Assert.IsNotNull(rebuilt);
        Assert.AreEqual(300, rebuilt!.Width);
        Assert.AreEqual(30, rebuilt.Height);
    }

    [TestMethod]
    public void ChangingTheRange_RebuildsTheCache()
    {
        using var control = Build(RuntimeWidth, RuntimeHeight, -100, 0, -20, -12);
        PaintInto(control);
        Assert.IsNotNull(GetCache(control));

        control.MinDb = -60;
        Assert.IsNull(GetCache(control), "changing MinDb should drop the stale cache");
    }

    [TestMethod]
    public void Dispose_ReleasesTheCachedResources()
    {
        var control = Build(RuntimeWidth, RuntimeHeight, -100, 0, -20, -12);
        PaintInto(control);
        Assert.IsNotNull(GetCache(control));

        control.Dispose();
        Assert.IsNull(GetCache(control), "Dispose should release the cached ruler bitmap");
    }
    #endregion

    #region Helpers
    protected static BTH_VolumeLevel_MonitorControl Build(int width, int height, double min, double max, double level, double peak)
    {
        var control = new BTH_VolumeLevel_MonitorControl { Size = new Size(width, height) };
        control.MinDb = min;
        control.MaxDb = max;
        control.DB_Level = level;
        control.DB_Peak = peak;
        return control;
    }

    /// <summary>Renders the control without needing a window handle.</summary>
    protected static Bitmap Paint(int width, int height, double min, double max, double level, double peak)
    {
        using var control = Build(width, height, min, max, level, peak);
        return PaintInto(control);
    }

    protected static Bitmap PaintInto(BTH_VolumeLevel_MonitorControl control)
    {
        var bmp = new Bitmap(Math.Max(1, control.Width), Math.Max(1, control.Height));
        using var g = Graphics.FromImage(bmp);
        using var args = new PaintEventArgs(g, new Rectangle(0, 0, bmp.Width, bmp.Height));

        var onPaint = typeof(BTH_VolumeLevel_MonitorControl)
            .GetMethod("OnPaint", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(onPaint, "OnPaint not found");
        onPaint!.Invoke(control, new object[] { args });

        return bmp;
    }

    protected static Bitmap? GetCache(BTH_VolumeLevel_MonitorControl control)
    {
        return typeof(BTH_VolumeLevel_MonitorControl)
            .GetField("StaticCache", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(control) as Bitmap;
    }

    protected static bool IsLight(Color color)
    {
        //The background is #404040 and the ticks/labels are 176..255 grey.
        return color.R > 120 && color.G > 120 && color.B > 120;
    }
    #endregion
}
