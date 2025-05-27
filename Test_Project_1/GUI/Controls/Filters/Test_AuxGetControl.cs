using Microsoft.VisualStudio.TestTools.UnitTesting;
using BassThatHz_ASIO_DSP_Processor.GUI.Controls;
using System.Windows.Forms;

namespace Test_Project_1;

[TestClass]
public class Test_AuxGetControl
{
    [TestMethod]
    public void AuxGetControl_Constructor_Initializes()
    {
        var control = new AuxGetControl();
        Assert.IsNotNull(control);
        Assert.IsInstanceOfType(control, typeof(UserControl));
    }

    [TestMethod]
    public void AuxGetControl_DefaultProperties_AreValid()
    {
        var control = new AuxGetControl();
        Assert.IsTrue(control.Enabled);
    }
}