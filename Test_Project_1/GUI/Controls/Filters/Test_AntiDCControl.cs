using Microsoft.VisualStudio.TestTools.UnitTesting;
using BassThatHz_ASIO_DSP_Processor.GUI.Controls;
using System.Windows.Forms;

namespace Test_Project_1;

[TestClass]
public class Test_AntiDCControl
{
    [TestMethod]
    public void AntiDCControl_Constructor_Initializes()
    {
        var control = new AntiDCControl();
        Assert.IsNotNull(control);
        Assert.IsInstanceOfType(control, typeof(UserControl));
    }

    [TestMethod]
    public void AntiDCControl_DefaultProperties_AreValid()
    {
        var control = new AntiDCControl();
        Assert.IsTrue(control.Enabled);
        // DesignMode is protected, so we cannot test it directly
    }
}