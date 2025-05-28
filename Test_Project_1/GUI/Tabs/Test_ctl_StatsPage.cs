namespace Test_Project_1;

using BassThatHz_ASIO_DSP_Processor.GUI.Tabs;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Reflection;

[TestClass]
public class Test_ctl_StatsPage
{
    [TestMethod]
    public void CanInstantiate_ctl_StatsPage()
    {
        var control = new ctl_StatsPage();
        Assert.IsNotNull(control);
    }

    [TestMethod]
    public void LoadConfigRefresh_DoesNotThrow()
    {
        var control = new ctl_StatsPage();
        control.LoadConfigRefresh();
        Assert.IsTrue(true); // If no exception, pass
    }
}