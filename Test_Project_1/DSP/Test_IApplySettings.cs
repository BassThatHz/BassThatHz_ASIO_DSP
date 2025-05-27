using BassThatHz_ASIO_DSP_Processor;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Test_Project_1;

[TestClass]
public class Test_IApplySettings
{
    private class DummyApplySettings : IApplySettings
    {
        public bool WasCalled { get; private set; }
        public void ApplySettings() => WasCalled = true;
    }

    [TestMethod]
    public void ApplySettings_CanBeCalled()
    {
        var dummy = new DummyApplySettings();
        Assert.IsFalse(dummy.WasCalled);
        dummy.ApplySettings();
        Assert.IsTrue(dummy.WasCalled);
    }
}