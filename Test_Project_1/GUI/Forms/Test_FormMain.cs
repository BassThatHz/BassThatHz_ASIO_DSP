using Microsoft.VisualStudio.TestTools.UnitTesting;
using BassThatHz_ASIO_DSP_Processor;
using Test_Project_1.TestHelpers;

namespace Test_Project_1;

[TestClass]
public class Test_FormMain
{
    [TestMethod]
    public void CanInstantiate_FormMain()
    {
        StaTestRunner.Run(() =>
        {
            var form = new BassThatHz_ASIO_DSP_Processor.FormMain();
            Assert.IsNotNull(form);
        });
    }
}