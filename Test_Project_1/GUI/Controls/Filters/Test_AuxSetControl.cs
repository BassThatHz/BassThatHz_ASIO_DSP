using Microsoft.VisualStudio.TestTools.UnitTesting;
using BassThatHz_ASIO_DSP_Processor.GUI.Controls;
using System.Windows.Forms;

namespace Test_Project_1;

[TestClass]
public class Test_AuxSetControl
{
    [TestMethod]
    public void AuxSetControl_Constructor_Initializes()
    {
        var control = new AuxSetControl();
        Assert.IsNotNull(control);
        Assert.IsInstanceOfType(control, typeof(UserControl));
    }

    [TestMethod]
    public void AuxSetControl_DefaultProperties_AreValid()
    {
        var control = new AuxSetControl();
        Assert.IsTrue(control.Enabled);
    }
}