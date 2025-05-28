using Microsoft.VisualStudio.TestTools.UnitTesting;
using BassThatHz_ASIO_DSP_Processor;

namespace Test_Project_1;

[TestClass]
public class Test_FormMonitoring
{
    [TestMethod]
    public void CanInstantiate_FormMonitoring()
    {
        var form = new BassThatHz_ASIO_DSP_Processor.FormMonitoring();
        Assert.IsNotNull(form);
    }
}