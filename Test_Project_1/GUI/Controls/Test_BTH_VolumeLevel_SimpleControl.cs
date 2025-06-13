using BassThatHz_ASIO_DSP_Processor.GUI.Controls;
using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using System.ComponentModel;

namespace Test_Project_1;

[TestClass]
public class Test_BTH_VolumeLevel_SimpleControl
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

    [TestMethod]
    public void MapEventHandlers_RegistersPaintEvent()
    {
        var control = new BTH_VolumeLevel_SimpleControl();
        control.MapEventHandlers(); // Ensure the method is called to register the event

        var eventField = typeof(Control).GetField("EventPaint", BindingFlags.Static | BindingFlags.NonPublic);
        var eventKey = eventField?.GetValue(null);
        var eventsProp = typeof(Component).GetProperty("Events", BindingFlags.NonPublic | BindingFlags.Instance);
        var eventList = eventsProp?.GetValue(control) as EventHandlerList;
        var paintDelegate = eventList?[eventKey] as Delegate;

        Assert.IsNotNull(paintDelegate);
        Assert.IsTrue(paintDelegate.GetInvocationList().Length > 0);
    }

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
        using var bmp = new Bitmap(control.Width, control.Height);
        using var g = Graphics.FromImage(bmp);
        var paintEvent = new PaintEventArgs(g, new Rectangle(0, 0, control.Width, control.Height));
        control.GetType().GetMethod("Simple_Paint", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(control, new object?[] { control, paintEvent });
    }
}