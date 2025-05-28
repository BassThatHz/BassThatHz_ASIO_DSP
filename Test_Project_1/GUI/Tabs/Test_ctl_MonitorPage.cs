namespace Test_Project_1;

using BassThatHz_ASIO_DSP_Processor.GUI.Tabs;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Reflection;

[TestClass]
public class Test_ctl_MonitorPage
{
    [TestMethod]
    public void CanInstantiate_ctl_MonitorPage()
    {
        var control = new ctl_MonitorPage();
        Assert.IsNotNull(control);
    }

    [TestMethod]
    public void LoadConfigRefresh_DoesNotThrow()
    {
        var control = new ctl_MonitorPage();
        control.LoadConfigRefresh();
        Assert.IsTrue(true);
    }
}