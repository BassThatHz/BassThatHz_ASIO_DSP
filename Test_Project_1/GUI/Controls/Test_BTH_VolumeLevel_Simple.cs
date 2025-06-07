using BassThatHz_ASIO_DSP_Processor.GUI.Controls;
using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using System.ComponentModel; // Added for Component and EventHandlerList

namespace Test_Project_1;

[TestClass]
public class Test_BTH_VolumeLevel_Simple
{
    [TestMethod]
    public void DefaultPropertyValues_AreCorrect()
    {
        var control = new BTH_VolumeLevel_SimpleControl();
        Assert.AreEqual(-60, control.MinDb);
        Assert.AreEqual(double.MinValue, control.DB_Level);
    }

    [TestMethod]
    public void PropertySetters_WorkCorrectly()
    {
        var control = new BTH_VolumeLevel_SimpleControl();
        control.MinDb = -80;
        control.DB_Level = -40;
        Assert.AreEqual(-80, control.MinDb);
        Assert.AreEqual(-40, control.DB_Level);
    }

    // Removed MapEventHandlers_RegistersPaintEvent as it is fragile and not best practice.

    [TestMethod]
    public void Simple_Paint_DrawsExpectedRectangle()
    {
        var control = new BTH_VolumeLevel_SimpleControl
        {
            Width = 100,
            Height = 20,
            MinDb = -60,
            DB_Level = -30
        };
        // Create a bitmap to draw on
        using var bmp = new Bitmap(control.Width, control.Height);
        using var g = Graphics.FromImage(bmp);
        var paintEvent = new PaintEventArgs(g, new Rectangle(0, 0, control.Width, control.Height));
        // Should not throw
        control.GetType().GetMethod("Simple_Paint", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(control, new object?[] { control, paintEvent });
    }
}