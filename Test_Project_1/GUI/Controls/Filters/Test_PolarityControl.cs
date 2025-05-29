using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Windows.Forms;
using BassThatHz_ASIO_DSP_Processor;
using BassThatHz_ASIO_DSP_Processor.GUI.Controls;

namespace Test_Project_1;

[TestClass]
public class Test_PolarityControl
{
    [TestMethod]
    public void Constructor_InitializesWithPositivePolarity()
    {
        var control = new PolarityControl();
        var filter = control.GetFilter as Polarity;
        Assert.IsNotNull(filter);
        Assert.IsTrue(filter!.Positive);
        Assert.IsFalse(control.Controls.Count == 0); // Should have at least the checkbox
    }

    [TestMethod]
    public void CboInverted_Checked_ChangesPolarity()
    {
        var control = new PolarityControl();
        var filter = control.GetFilter as Polarity;
        Assert.IsNotNull(filter);
        // Simulate checking the checkbox (invert)
        var cbo = GetCboInverted(control);
        cbo.Checked = true;
        Assert.IsFalse(filter!.Positive);
        // Simulate unchecking the checkbox (not inverted)
        cbo.Checked = false;
        Assert.IsTrue(filter.Positive);
    }

    [TestMethod]
    public void SetDeepClonedFilter_SetsFilterAndCheckbox()
    {
        var control = new PolarityControl();
        var newFilter = new Polarity { Positive = false };
        control.SetDeepClonedFilter(newFilter);
        var filter = control.GetFilter as Polarity;
        Assert.IsNotNull(filter);
        Assert.AreEqual(newFilter, filter);
        var cbo = GetCboInverted(control);
        Assert.IsTrue(cbo.Checked); // Positive=false => Checked=true
    }

    [TestMethod]
    public void ApplySettings_DoesNotThrow()
    {
        var control = new PolarityControl();
        control.ApplySettings(); // Should not throw
    }

    [TestMethod]
    public void GetFilter_ReturnsPolarityInstance()
    {
        var control = new PolarityControl();
        var filter = control.GetFilter;
        Assert.IsInstanceOfType(filter, typeof(Polarity));
    }

    private static CheckBox GetCboInverted(PolarityControl control)
    {
        // Access the protected field via reflection
        var field = typeof(PolarityControl).GetField("cboInverted", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.FlattenHierarchy);
        Assert.IsNotNull(field, "cboInverted field not found");
        var value = field.GetValue(control) as CheckBox;
        Assert.IsNotNull(value, "cboInverted is not a CheckBox");
        return value!;
    }
}