namespace Test_Project_1;

using BassThatHz_ASIO_DSP_Processor.GUI.Tabs;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Reflection;

[TestClass]
public class Test_ctl_DSPConfigPage
{
    [TestMethod]
    public void CanInstantiate_ctl_DSPConfigPage()
    {
        var control = new ctl_DSPConfigPage();
        Assert.IsNotNull(control);
    }

    [TestMethod]
    public void LoadConfigRefresh_DoesNotThrow()
    {
        var control = new ctl_DSPConfigPage();
        control.LoadConfigRefresh();
        Assert.IsTrue(true);
    }

    [TestMethod]
    public void ResetAll_TabPage_Text_DoesNotThrow()
    {
        var control = new ctl_DSPConfigPage();
        control.ResetAll_TabPage_Text();
        Assert.IsTrue(true);
    }

    [TestMethod]
    public void ResetAll_StreamDropDownLists_DoesNotThrow()
    {
        var control = new ctl_DSPConfigPage();
        control.ResetAll_StreamDropDownLists();
        Assert.IsTrue(true);
    }
}