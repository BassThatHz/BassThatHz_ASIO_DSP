using Microsoft.VisualStudio.TestTools.UnitTesting;
using BassThatHz_ASIO_DSP_Processor.GUI.Controls;
using System.Windows.Forms;

namespace Test_Project_1;

[TestClass]
public class Test_FloorControl
{
    [TestMethod]
    public void FloorControl_Constructor_Initializes()
    {
        var control = new FloorControl();
        Assert.IsNotNull(control);
        Assert.IsInstanceOfType(control, typeof(UserControl));
    }

    [TestMethod]
    public void FloorControl_DefaultProperties_AreValid()
    {
        var control = new FloorControl();
        Assert.IsTrue(control.Enabled);
    }
}