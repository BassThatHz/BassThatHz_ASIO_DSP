using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Windows.Forms;
using BassThatHz_ASIO_DSP_Processor;
using BassThatHz_ASIO_DSP_Processor.GUI.Controls;

namespace Test_Project_1;

[TestClass]
public class Test_DEQControl
{
    [TestMethod]
    public void Constructor_InitializesControls()
    {
        var control = new DEQControl();
        Assert.IsNotNull(control.Controls);
        Assert.IsNotNull(control.GetFilter);
    }

    [TestMethod]
    public void ApplySettings_UpdatesFilterFromUI()
    {
        var control = new DEQControl();
        // Set UI values
        SetComboBox(control, "cboDEQType", 0);
        SetComboBox(control, "cboBiquadType", 0);
        SetComboBox(control, "cboThresholdType", 0);
        SetTextBox(control, "txtF", "123.4");
        SetTextBox(control, "txtG", "5.6");
        SetTextBox(control, "txtQ", "0.7");
        SetTextBox(control, "txtS", "1.2");
        SetTextBox(control, "mask_Attack", "10");
        SetTextBox(control, "mask_Release", "20");
        SetTextBox(control, "msb_CompressionRatio", "12");
        SetTextBox(control, "msb_KneeWidth_db", "2");
        SetCheckBox(control, "chkSoftKnee", true);
        // Simulate threshold control
        SetThreshold(control, 3.3);
        control.ApplySettings();
        var filter = control.GetFilter as DEQ;
        Assert.IsNotNull(filter);
        Assert.AreEqual(123.4, filter!.TargetFrequency);
        Assert.AreEqual(5.6, filter.TargetGain_dB);
        Assert.AreEqual(0.7, filter.TargetQ);
        Assert.AreEqual(1.2, filter.TargetSlope);
        Assert.AreEqual(3.3, filter.Threshold_dB);
        Assert.AreEqual(10, filter.AttackTime_ms);
        Assert.AreEqual(20, filter.ReleaseTime_ms);
        Assert.AreEqual(12, filter.Ratio);
        Assert.AreEqual(2, filter.KneeWidth_dB);
        Assert.IsTrue(filter.UseSoftKnee);
    }

    //[TestMethod]
    //public void SetDeepClonedFilter_UpdatesUIFromFilter()
    //{
    //    var control = new DEQControl();
    //    var filter = new DEQ
    //    {
    //        TargetFrequency = 111,
    //        TargetGain_dB = 2.2,
    //        TargetQ = 0.5,
    //        TargetSlope = 1.1,
    //        Threshold_dB = 4.4,
    //        AttackTime_ms = 15,
    //        ReleaseTime_ms = 25,
    //        Ratio = 13,
    //        KneeWidth_dB = 3,
    //        UseSoftKnee = true
    //    };
    //    control.SetDeepClonedFilter(filter);
    //    Assert.AreEqual("111", GetTextBox(control, "txtF").Text);
    //    Assert.AreEqual("2.2", GetTextBox(control, "txtG").Text);
    //    Assert.AreEqual("0.5", GetTextBox(control, "txtQ").Text);
    //    Assert.AreEqual("1.1", GetTextBox(control, "txtS").Text);
    //    Assert.AreEqual(4.4, GetThreshold(control));
    //    Assert.AreEqual("15", GetTextBox(control, "mask_Attack").Text);
    //    Assert.AreEqual("25", GetTextBox(control, "mask_Release").Text);
    //    Assert.AreEqual("13", GetTextBox(control, "msb_CompressionRatio").Text);
    //    Assert.AreEqual("3", GetTextBox(control, "msb_KneeWidth_db").Text);
    //    Assert.IsTrue(GetCheckBox(control, "chkSoftKnee").Checked);
    //}

    [TestMethod]
    public void GetFilter_ReturnsDEQInstance()
    {
        var control = new DEQControl();
        Assert.IsInstanceOfType(control.GetFilter, typeof(DEQ));
    }

    // --- Helpers ---
    private static void SetComboBox(DEQControl control, string name, int index)
    {
        var field = typeof(DEQControl).GetField(name, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.IsNotNull(field, $"{name} not found");
        var cb = field.GetValue(control) as ComboBox;
        Assert.IsNotNull(cb, $"{name} is not ComboBox");
        cb.SelectedIndex = index;
    }
    private static void SetTextBox(DEQControl control, string name, string value)
    {
        var field = typeof(DEQControl).GetField(name, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.IsNotNull(field, $"{name} not found");
        var tb = field.GetValue(control) as TextBox;
        Assert.IsNotNull(tb, $"{name} is not TextBox");
        tb.Text = value;
    }
    private static TextBox GetTextBox(DEQControl control, string name)
    {
        var field = typeof(DEQControl).GetField(name, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.IsNotNull(field, $"{name} not found");
        var tb = field.GetValue(control) as TextBox;
        Assert.IsNotNull(tb, $"{name} is not TextBox");
        return tb!;
    }
    private static void SetCheckBox(DEQControl control, string name, bool value)
    {
        var field = typeof(DEQControl).GetField(name, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.IsNotNull(field, $"{name} not found");
        var cb = field.GetValue(control) as CheckBox;
        Assert.IsNotNull(cb, $"{name} is not CheckBox");
        cb.Checked = value;
    }
    private static CheckBox GetCheckBox(DEQControl control, string name)
    {
        var field = typeof(DEQControl).GetField(name, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.IsNotNull(field, $"{name} not found");
        var cb = field.GetValue(control) as CheckBox;
        Assert.IsNotNull(cb, $"{name} is not CheckBox");
        return cb!;
    }
    private static void SetThreshold(DEQControl control, double value)
    {
        var field = typeof(DEQControl).GetField("Threshold", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.IsNotNull(field, "Threshold not found");
        dynamic threshold = field.GetValue(control);
        threshold.VolumedB = value;
    }
    private static double GetThreshold(DEQControl control)
    {
        var field = typeof(DEQControl).GetField("Threshold", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.IsNotNull(field, "Threshold not found");
        dynamic threshold = field.GetValue(control);
        return (double)threshold.VolumedB;
    }
}