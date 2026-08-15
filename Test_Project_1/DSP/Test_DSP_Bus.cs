using BassThatHz_ASIO_DSP_Processor;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test_Project_1;

[TestClass]
public class Test_DSP_Bus
{
    [TestMethod]
    public void DSP_Bus_DefaultValues_AreCorrect()
    {
        var bus = new DSP_Bus();
        Assert.AreEqual(string.Empty, bus.Name);
        Assert.IsNotNull(bus.Buffer);
        Assert.AreEqual(0, bus.Buffer.Length);
        Assert.IsFalse(bus.IsBypassed);
        Assert.AreEqual(" | False", bus.DisplayMember);
    }

    [TestMethod]
    public void DSP_Bus_PropertySetters_WorkCorrectly()
    {
        var bus = new DSP_Bus
        {
            Name = "MyBus",
            IsBypassed = true
        };

        Assert.AreEqual("MyBus", bus.Name);
        Assert.IsTrue(bus.IsBypassed);
        Assert.AreEqual("MyBus | True", bus.DisplayMember);
    }

    [TestMethod]
    public void DSP_Bus_Buffer_CanBeSetAndRetrieved()
    {
        var bus = new DSP_Bus();
        var data = new double[] { 1.0, 2.0, 3.0 };
        bus.Buffer = data;
        Assert.AreEqual(3, bus.Buffer.Length);
        CollectionAssert.AreEqual(data, bus.Buffer);
    }

    [TestMethod]
    public void DSP_Bus_Buffer_SetNull_DefaultsToEmptyArray()
    {
        var bus = new DSP_Bus();
        bus.Buffer = null!;
        Assert.IsNotNull(bus.Buffer);
        Assert.AreEqual(0, bus.Buffer.Length);
    }

    [TestMethod]
    public void DSP_Bus_ToString_ReturnsDisplayMember()
    {
        var bus = new DSP_Bus { Name = "Sub", IsBypassed = false };
        Assert.AreEqual(bus.DisplayMember, bus.ToString());
        Assert.AreEqual("Sub | False", bus.ToString());
    }

    [TestMethod]
    public void DSP_Bus_ImplementsIBus()
    {
        IBus bus = new DSP_Bus();
        Assert.IsInstanceOfType(bus, typeof(DSP_Bus));
    }

    [TestMethod]
    public void DSP_Bus_Buffer_IsLazilyInitializedAndReusable()
    {
        var bus = new DSP_Bus();
        var first = bus.Buffer;
        var second = bus.Buffer;
        Assert.AreSame(first, second);
    }
}
