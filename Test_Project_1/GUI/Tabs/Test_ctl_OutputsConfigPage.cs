namespace Test_Project_1;

using BassThatHz_ASIO_DSP_Processor.GUI.Tabs;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Reflection;

[TestClass]
public class Test_ctl_OutputsConfigPage
{
    [TestMethod]
    public void CanInstantiate_ctl_OutputsConfigPage()
    {
        var control = new ctl_OutputsConfigPage();
        Assert.IsNotNull(control);
    }

    [TestMethod]
    public void LoadConfigRefresh_DoesNotThrow()
    {
        var control = new ctl_OutputsConfigPage();
        control.LoadConfigRefresh();
        Assert.IsTrue(true);
    }
}