namespace Test_Project_1;

using BassThatHz_ASIO_DSP_Processor.GUI.Tabs;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Reflection;

[TestClass]
public class Test_ctl_BusesPage
{
    [TestMethod]
    public void CanInstantiate_ctl_BusesPage()
    {
        var control = new ctl_BusesPage();
        Assert.IsNotNull(control);
    }

    [TestMethod]
    public void HasFocus_DoesNotThrow()
    {
        var control = new ctl_BusesPage();
        control.HasFocus();
        Assert.IsTrue(true); // If no exception, pass
    }

    [TestMethod]
    public void LoadConfigRefresh_DoesNotThrow()
    {
        var control = new ctl_BusesPage();
        control.LoadConfigRefresh();
        Assert.IsTrue(true); // If no exception, pass
    }

    [TestMethod]
    public void Control_Load_DoesNotThrow()
    {
        var control = new ctl_BusesPage();
        // Use reflection to invoke protected event handler
        var method = typeof(ctl_BusesPage).GetMethod("Control_Load", BindingFlags.NonPublic | BindingFlags.Instance);
        method.Invoke(control, new object[] { null, EventArgs.Empty });
        Assert.IsTrue(true);
    }

    [TestMethod]
    public void AddBus_BTN_Click_DoesNotThrow()
    {
        var control = new ctl_BusesPage();
        var method = typeof(ctl_BusesPage).GetMethod("AddBus_BTN_Click", BindingFlags.NonPublic | BindingFlags.Instance);
        method.Invoke(control, new object[] { control, EventArgs.Empty });
        Assert.IsTrue(true);
    }

    [TestMethod]
    public void ChangeBus_BTN_Click_DoesNotThrow()
    {
        var control = new ctl_BusesPage();
        var method = typeof(ctl_BusesPage).GetMethod("ChangeBus_BTN_Click", BindingFlags.NonPublic | BindingFlags.Instance);
        method.Invoke(control, new object[] { control, EventArgs.Empty });
        Assert.IsTrue(true);
    }

    [TestMethod]
    public void DeleteBus_BTN_Click_DoesNotThrow()
    {
        var control = new ctl_BusesPage();
        var method = typeof(ctl_BusesPage).GetMethod("DeleteBus_BTN_Click", BindingFlags.NonPublic | BindingFlags.Instance);
        method.Invoke(control, new object[] { control, EventArgs.Empty });
        Assert.IsTrue(true);
    }
}
