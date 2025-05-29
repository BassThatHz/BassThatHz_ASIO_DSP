using BassThatHz_ASIO_DSP_Processor;
using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using System.ComponentModel;

namespace Test_Project_1;

[TestClass]
public class Test_BTH_VolumeSliderControl
{
    //[TestMethod]
    //public void DefaultPropertyValues_AreCorrect()
    //{
    //    var control = new BTH_VolumeSliderControl();
    //    Assert.AreEqual(1d, control.RestPosition);
    //    Assert.AreEqual(-384d, control.MinDb);
    //    Assert.AreEqual(0d, control.MaxDb);
    //    Assert.AreEqual(1d, control.Volume);
    //    Assert.AreEqual(0d, control.VolumedB);
    //    Assert.IsFalse(control.ReadOnly);
    //    Assert.AreEqual(Color.Black, control.TextColor);
    //    Assert.IsNotNull(control.SliderColor);
    //}

    [TestMethod]
    public void PropertySetters_WorkCorrectly()
    {
        var control = new BTH_VolumeSliderControl();
        control.MinDb = -100;
        control.MaxDb = 10;
        control.RestPosition = 0.5;
        control.Volume = 0.25;
        control.VolumedB = -12;
        control.TextColor = Color.Red;
        control.SliderColor = Brushes.Blue;
        Assert.AreEqual(-100, control.MinDb);
        Assert.AreEqual(10, control.MaxDb);
        Assert.AreEqual(0.5, control.RestPosition);
        Assert.AreEqual(Color.Red, control.TextColor);
        Assert.AreEqual(Brushes.Blue, control.SliderColor);
    }

    //[TestMethod]
    //public void VolumeChanged_Event_IsRaised()
    //{
    //    var control = new BTH_VolumeSliderControl();
    //    bool eventRaised = false;
    //    control.VolumeChanged += (s, e) => eventRaised = true;
    //    control.Volume = 0.5;
    //    Assert.IsTrue(eventRaised);
    //}

    [TestMethod]
    public void TextBoxInput_UpdatesVolume()
    {
        var control = new BTH_VolumeSliderControl();
        var txtDB = control.GetType().GetField("txtDB", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(control) as TextBox;
        txtDB.Text = "-6";
        txtDB.Visible = true;
        txtDB.Focus();
        // Simulate lost focus
        control.GetType().GetMethod("TxtDB_LostFocus", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(control, new object?[] { txtDB, EventArgs.Empty });
        Assert.AreEqual(-6, Math.Round(control.VolumedB));
    }

    [TestMethod]
    public void OnPaint_DoesNotThrow()
    {
        var control = new BTH_VolumeSliderControl { Width = 100, Height = 20 };
        using var bmp = new Bitmap(control.Width, control.Height);
        using var g = Graphics.FromImage(bmp);
        var paintEvent = new PaintEventArgs(g, new Rectangle(0, 0, control.Width, control.Height));
        control.GetType().GetMethod("OnPaint", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(control, new object?[] { paintEvent });
    }
}
