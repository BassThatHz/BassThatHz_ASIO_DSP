using Microsoft.VisualStudio.TestTools.UnitTesting;
using BassThatHz_ASIO_DSP_Processor.GUI.Forms;

namespace Test_Project_1;

[TestClass]
public class Test_RTA
{
    [TestMethod]
    public void CanInstantiate_FormRTA()
    {
        var form = new FormRTA();
        Assert.IsNotNull(form);
    }
}