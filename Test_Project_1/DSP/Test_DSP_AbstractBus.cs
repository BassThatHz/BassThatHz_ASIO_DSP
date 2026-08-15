using BassThatHz_ASIO_DSP_Processor;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test_Project_1;

[TestClass]
public class Test_DSP_AbstractBus
{
    [TestMethod]
    public void DSP_AbstractBus_DefaultValues_AreCorrect()
    {
        var bus = new DSP_AbstractBus();
        Assert.IsFalse(bus.IsBypassed);
        Assert.AreEqual(string.Empty, bus.Name);
        Assert.AreEqual(" | False", bus.DisplayMember);
        Assert.IsNotNull(bus.Mappings);
        Assert.AreEqual(0, bus.Mappings.Count);
    }

    [TestMethod]
    public void DSP_AbstractBus_PropertySetters_WorkCorrectly()
    {
        var bus = new DSP_AbstractBus
        {
            Name = "MyAbstractBus",
            IsBypassed = true
        };

        Assert.AreEqual("MyAbstractBus", bus.Name);
        Assert.IsTrue(bus.IsBypassed);
        Assert.AreEqual("MyAbstractBus | True", bus.DisplayMember);
    }

    [TestMethod]
    public void DSP_AbstractBus_Mappings_CanBeAddedTo()
    {
        var bus = new DSP_AbstractBus();
        var mapping = new DSP_AbstractBusMappings();
        bus.Mappings.Add(mapping);
        Assert.AreEqual(1, bus.Mappings.Count);
        Assert.AreSame(mapping, bus.Mappings[0]);
    }

    [TestMethod]
    public void DSP_AbstractBus_Mappings_SetNull_DefaultsToEmptyList()
    {
        var bus = new DSP_AbstractBus();
        bus.Mappings = null!;
        Assert.IsNotNull(bus.Mappings);
        Assert.AreEqual(0, bus.Mappings.Count);
    }

    [TestMethod]
    public void DSP_AbstractBus_Mappings_SetExplicitList_IsRetained()
    {
        var bus = new DSP_AbstractBus();
        var list = new System.Collections.Generic.List<IAbstractBusMappings> { new DSP_AbstractBusMappings() };
        bus.Mappings = list;
        Assert.AreSame(list, bus.Mappings);
    }

    [TestMethod]
    public void DSP_AbstractBus_ToString_ReturnsDisplayMember()
    {
        var bus = new DSP_AbstractBus { Name = "Bus1" };
        Assert.AreEqual(bus.DisplayMember, bus.ToString());
    }

    [TestMethod]
    public void DSP_AbstractBus_Equals_SameReference_ReturnsTrue()
    {
        var bus = new DSP_AbstractBus();
        Assert.IsTrue(bus.Equals(bus));
    }

    [TestMethod]
    public void DSP_AbstractBus_Equals_Null_ReturnsFalse()
    {
        var bus = new DSP_AbstractBus();
        Assert.IsFalse(bus.Equals(null));
    }

    [TestMethod]
    public void DSP_AbstractBus_Equals_DifferentType_ReturnsFalse()
    {
        var bus = new DSP_AbstractBus();
        Assert.IsFalse(bus.Equals("not a bus"));
    }

    [TestMethod]
    public void DSP_AbstractBus_Equals_SameName_ReturnsTrue()
    {
        var bus1 = new DSP_AbstractBus { Name = "Same" };
        var bus2 = new DSP_AbstractBus { Name = "Same" };
        Assert.IsTrue(bus1.Equals(bus2));
    }

    [TestMethod]
    public void DSP_AbstractBus_Equals_DifferentName_ReturnsFalse()
    {
        var bus1 = new DSP_AbstractBus { Name = "One" };
        var bus2 = new DSP_AbstractBus { Name = "Two" };
        Assert.IsFalse(bus1.Equals(bus2));
    }

    [TestMethod]
    public void DSP_AbstractBus_GetHashCode_EqualNames_HaveSameHashCode()
    {
        var bus1 = new DSP_AbstractBus { Name = "Same" };
        var bus2 = new DSP_AbstractBus { Name = "Same" };
        Assert.AreEqual(bus1.GetHashCode(), bus2.GetHashCode());
    }

    [TestMethod]
    public void DSP_AbstractBus_ImplementsIAbstractBus()
    {
        IAbstractBus bus = new DSP_AbstractBus();
        Assert.IsInstanceOfType(bus, typeof(DSP_AbstractBus));
    }

    // ---------- DSP_AbstractBusMappings (defined in the same production file) ----------

    [TestMethod]
    public void DSP_AbstractBusMappings_DefaultValues_AreCorrect()
    {
        var mapping = new DSP_AbstractBusMappings();
        Assert.IsFalse(mapping.IsBypassed);
        Assert.IsNotNull(mapping.InputSource);
        Assert.IsNotNull(mapping.OutputDestination);
        Assert.IsNotNull(mapping.Buffer);
        Assert.AreEqual(0, mapping.Buffer.Length);
    }

    [TestMethod]
    public void DSP_AbstractBusMappings_InputSource_SetNull_DefaultsToNewStreamItem()
    {
        var mapping = new DSP_AbstractBusMappings();
        mapping.InputSource = null!;
        Assert.IsNotNull(mapping.InputSource);
    }

    [TestMethod]
    public void DSP_AbstractBusMappings_OutputDestination_SetNull_DefaultsToNewStreamItem()
    {
        var mapping = new DSP_AbstractBusMappings();
        mapping.OutputDestination = null!;
        Assert.IsNotNull(mapping.OutputDestination);
    }

    [TestMethod]
    public void DSP_AbstractBusMappings_DisplayMember_ReflectsSourcesAndBypass()
    {
        var mapping = new DSP_AbstractBusMappings
        {
            InputSource = new StreamItem { Name = "In", DisplayMember = "InDisp" },
            OutputDestination = new StreamItem { Name = "Out", DisplayMember = "OutDisp" },
            IsBypassed = true
        };

        var expected = string.Concat("InDisp", " | ", "OutDisp", " | ", "True");
        Assert.AreEqual(expected, mapping.DisplayMember);
    }

    [TestMethod]
    public void DSP_AbstractBusMappings_ToString_ReturnsDisplayMember()
    {
        var mapping = new DSP_AbstractBusMappings();
        Assert.AreEqual(mapping.DisplayMember, mapping.ToString());
    }

    [TestMethod]
    public void DSP_AbstractBusMappings_Buffer_CanBeSetAndRetrieved()
    {
        var mapping = new DSP_AbstractBusMappings();
        var data = new double[] { 1.0, 2.0 };
        mapping.Buffer = data;
        CollectionAssert.AreEqual(data, mapping.Buffer);
    }

    [TestMethod]
    public void DSP_AbstractBusMappings_Equals_SameReference_ReturnsTrue()
    {
        var mapping = new DSP_AbstractBusMappings();
        Assert.IsTrue(mapping.Equals(mapping));
    }

    [TestMethod]
    public void DSP_AbstractBusMappings_Equals_Null_ReturnsFalse()
    {
        var mapping = new DSP_AbstractBusMappings();
        Assert.IsFalse(mapping.Equals(null));
    }

    [TestMethod]
    public void DSP_AbstractBusMappings_Equals_DifferentType_ReturnsFalse()
    {
        var mapping = new DSP_AbstractBusMappings();
        Assert.IsFalse(mapping.Equals("not a mapping"));
    }

    [TestMethod]
    public void DSP_AbstractBusMappings_Equals_SameSourcesAndDestinations_ReturnsTrue()
    {
        var mapping1 = new DSP_AbstractBusMappings
        {
            InputSource = new StreamItem { Index = 1, StreamType = StreamType.Channel },
            OutputDestination = new StreamItem { Index = 2, StreamType = StreamType.Bus }
        };
        var mapping2 = new DSP_AbstractBusMappings
        {
            InputSource = new StreamItem { Index = 1, StreamType = StreamType.Channel },
            OutputDestination = new StreamItem { Index = 2, StreamType = StreamType.Bus }
        };
        Assert.IsTrue(mapping1.Equals(mapping2));
    }

    [TestMethod]
    public void DSP_AbstractBusMappings_Equals_DifferentInputSource_ReturnsFalse()
    {
        var mapping1 = new DSP_AbstractBusMappings
        {
            InputSource = new StreamItem { Index = 1, StreamType = StreamType.Channel },
            OutputDestination = new StreamItem { Index = 2, StreamType = StreamType.Bus }
        };
        var mapping2 = new DSP_AbstractBusMappings
        {
            InputSource = new StreamItem { Index = 99, StreamType = StreamType.Channel },
            OutputDestination = new StreamItem { Index = 2, StreamType = StreamType.Bus }
        };
        Assert.IsFalse(mapping1.Equals(mapping2));
    }

    [TestMethod]
    public void DSP_AbstractBusMappings_GetHashCode_EqualObjects_HaveSameHashCode()
    {
        var mapping1 = new DSP_AbstractBusMappings
        {
            InputSource = new StreamItem { Index = 1, StreamType = StreamType.Channel },
            OutputDestination = new StreamItem { Index = 2, StreamType = StreamType.Bus }
        };
        var mapping2 = new DSP_AbstractBusMappings
        {
            InputSource = new StreamItem { Index = 1, StreamType = StreamType.Channel },
            OutputDestination = new StreamItem { Index = 2, StreamType = StreamType.Bus }
        };
        Assert.AreEqual(mapping1.GetHashCode(), mapping2.GetHashCode());
    }

    [TestMethod]
    public void DSP_AbstractBusMappings_ImplementsIAbstractBusMappings()
    {
        IAbstractBusMappings mapping = new DSP_AbstractBusMappings();
        Assert.IsInstanceOfType(mapping, typeof(DSP_AbstractBusMappings));
    }
}
