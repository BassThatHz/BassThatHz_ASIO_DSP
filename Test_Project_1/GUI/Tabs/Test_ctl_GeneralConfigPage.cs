namespace Test_Project_1;

using BassThatHz_ASIO_DSP_Processor.GUI.Tabs;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Reflection;

[TestClass]
public class Test_ctl_GeneralConfigPage
{
    [TestMethod]
    public void CanInstantiate_ctl_GeneralConfigPage()
    {
        var control = new ctl_GeneralConfigPage();
        Assert.IsNotNull(control);
    }

    [TestMethod]
    public void LoadConfigRefresh_DoesNotThrow()
    {
        var control = new ctl_GeneralConfigPage();
        control.LoadConfigRefresh();
        Assert.IsTrue(true);
    }
}