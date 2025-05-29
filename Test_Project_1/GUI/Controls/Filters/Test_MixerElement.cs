using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Windows.Forms;
using BassThatHz_ASIO_DSP_Processor.GUI.Controls.Filters;

namespace Test_Project_1;

[TestClass]
public class Test_MixerElement
{
    [TestMethod]
    public void Constructor_InitializesControls()
    {
        var element = new MixerElement();
        Assert.IsNotNull(element.Get_txtChAttenuation);
        Assert.IsNotNull(element.Get_txtStreamAttenuation);
        Assert.IsNotNull(element.Get_chkChannel);
    }

    [TestMethod]
    public void ChAttenuation_NegativeSignAlwaysAtStart()
    {
        var element = new MixerElement();
        var box = element.Get_txtChAttenuation;
        box.Text = "12-3";
        // Simulate TextChanged event
        box.Text = box.Text; // triggers handler
        Assert.IsTrue(box.Text.StartsWith("-"));
        Assert.IsFalse(box.Text.Substring(1).Contains("-"));
    }

    [TestMethod]
    public void ChAttenuation_PositiveValueResetsToZero()
    {
        var element = new MixerElement();
        var box = element.Get_txtChAttenuation;
        box.Text = "5";
        // Simulate TextChanged event
        box.Text = box.Text; // triggers handler
        Assert.AreEqual("0", box.Text);
    }

    [TestMethod]
    public void StreamAttenuation_NegativeSignAlwaysAtStart()
    {
        var element = new MixerElement();
        var box = element.Get_txtStreamAttenuation;
        box.Text = "45-6";
        // Simulate TextChanged event
        box.Text = box.Text; // triggers handler
        Assert.IsTrue(box.Text.StartsWith("-"));
        Assert.IsFalse(box.Text.Substring(1).Contains("-"));
    }

    [TestMethod]
    public void StreamAttenuation_PositiveValueResetsToZero()
    {
        var element = new MixerElement();
        var box = element.Get_txtStreamAttenuation;
        box.Text = "7.2";
        // Simulate TextChanged event
        box.Text = box.Text; // triggers handler
        Assert.AreEqual("0", box.Text);
    }

    [TestMethod]
    public void Checkbox_DefaultState()
    {
        var element = new MixerElement();
        var chk = element.Get_chkChannel;
        Assert.IsFalse(chk.Checked); // Default unchecked
        chk.Checked = true;
        Assert.IsTrue(chk.Checked);
    }
}