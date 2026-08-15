using BassThatHz_ASIO_DSP_Processor;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test_Project_1;

[TestClass]
public class Test_StreamItem
{
    [TestMethod]
    public void StreamItem_DefaultValues_AreCorrect()
    {
        var item = new StreamItem();
        Assert.AreEqual(string.Empty, item.Name);
        Assert.AreEqual(string.Empty, item.DisplayMember);
        Assert.AreEqual(-1, item.Index);
        Assert.AreEqual(StreamType.Channel, item.StreamType);
    }

    [TestMethod]
    public void StreamItem_PropertySetters_WorkCorrectly()
    {
        var item = new StreamItem
        {
            Name = "Ch1",
            DisplayMember = "Channel 1",
            Index = 3,
            StreamType = StreamType.Bus
        };

        Assert.AreEqual("Ch1", item.Name);
        Assert.AreEqual("Channel 1", item.DisplayMember);
        Assert.AreEqual(3, item.Index);
        Assert.AreEqual(StreamType.Bus, item.StreamType);
    }

    [TestMethod]
    public void StreamItem_Name_NullSetsEmptyString()
    {
        var item = new StreamItem { Name = null! };
        Assert.AreEqual(string.Empty, item.Name);
    }

    [TestMethod]
    public void StreamItem_Name_EmptySetsEmptyString()
    {
        var item = new StreamItem { Name = string.Empty };
        Assert.AreEqual(string.Empty, item.Name);
    }

    [TestMethod]
    public void StreamItem_DisplayMember_NullSetsEmptyString()
    {
        var item = new StreamItem { DisplayMember = null! };
        Assert.AreEqual(string.Empty, item.DisplayMember);
    }

    [TestMethod]
    public void StreamItem_DisplayMember_EmptySetsEmptyString()
    {
        var item = new StreamItem { DisplayMember = string.Empty };
        Assert.AreEqual(string.Empty, item.DisplayMember);
    }

    [TestMethod]
    public void StreamItem_Name_IsInterned()
    {
        // The setter interns non-empty strings; verify the interned reference is used.
        var value = new string("InternMe".ToCharArray());
        var item = new StreamItem { Name = value };
        Assert.AreSame(string.Intern(value), item.Name);
    }

    [TestMethod]
    public void StreamItem_DeepClone_ReturnsEquivalentButDistinctInstance()
    {
        var original = new StreamItem
        {
            Name = "Original",
            DisplayMember = "Original Display",
            Index = 5,
            StreamType = StreamType.Stream
        };

        var clone = original.DeepClone();

        Assert.IsNotNull(clone);
        Assert.AreNotSame(original, clone);
        Assert.IsInstanceOfType(clone, typeof(StreamItem));
        Assert.AreEqual(original.Name, clone.Name);
        Assert.AreEqual(original.DisplayMember, clone.DisplayMember);
        Assert.AreEqual(original.Index, clone.Index);
        Assert.AreEqual(original.StreamType, clone.StreamType);
    }

    [TestMethod]
    public void StreamItem_DeepClone_MutatingCloneDoesNotAffectOriginal()
    {
        var original = new StreamItem { Name = "Orig", Index = 1 };
        var clone = (StreamItem)original.DeepClone();

        clone.Name = "Changed";
        clone.Index = 99;

        Assert.AreEqual("Orig", original.Name);
        Assert.AreEqual(1, original.Index);
    }

    [TestMethod]
    public void StreamItem_Equals_SameReference_ReturnsTrue()
    {
        var item = new StreamItem();
        Assert.IsTrue(item.Equals(item));
    }

    [TestMethod]
    public void StreamItem_Equals_Null_ReturnsFalse()
    {
        var item = new StreamItem();
        Assert.IsFalse(item.Equals(null));
    }

    [TestMethod]
    public void StreamItem_Equals_DifferentType_ReturnsFalse()
    {
        var item = new StreamItem();
        Assert.IsFalse(item.Equals("not a stream item"));
    }

    [TestMethod]
    public void StreamItem_Equals_SameStreamTypeAndIndex_ReturnsTrue_RegardlessOfName()
    {
        var item1 = new StreamItem { Name = "A", Index = 2, StreamType = StreamType.Channel };
        var item2 = new StreamItem { Name = "B", Index = 2, StreamType = StreamType.Channel };

        // Equality is documented to be based only on StreamType + Index, not Name.
        Assert.IsTrue(item1.Equals(item2));
    }

    [TestMethod]
    public void StreamItem_Equals_DifferentIndex_ReturnsFalse()
    {
        var item1 = new StreamItem { Index = 1, StreamType = StreamType.Channel };
        var item2 = new StreamItem { Index = 2, StreamType = StreamType.Channel };
        Assert.IsFalse(item1.Equals(item2));
    }

    [TestMethod]
    public void StreamItem_Equals_DifferentStreamType_ReturnsFalse()
    {
        var item1 = new StreamItem { Index = 1, StreamType = StreamType.Channel };
        var item2 = new StreamItem { Index = 1, StreamType = StreamType.Bus };
        Assert.IsFalse(item1.Equals(item2));
    }

    [TestMethod]
    public void StreamItem_GetHashCode_EqualObjects_HaveSameHashCode()
    {
        var item1 = new StreamItem { Index = 4, StreamType = StreamType.AbstractBus };
        var item2 = new StreamItem { Index = 4, StreamType = StreamType.AbstractBus };
        Assert.AreEqual(item1.GetHashCode(), item2.GetHashCode());
    }

    [TestMethod]
    public void StreamItem_GetHashCode_DifferentObjects_TendToDiffer()
    {
        var item1 = new StreamItem { Index = 4, StreamType = StreamType.AbstractBus };
        var item2 = new StreamItem { Index = 5, StreamType = StreamType.AbstractBus };
        Assert.AreNotEqual(item1.GetHashCode(), item2.GetHashCode());
    }

    [TestMethod]
    public void StreamItem_ImplementsIStreamItem()
    {
        IStreamItem item = new StreamItem();
        Assert.IsInstanceOfType(item, typeof(StreamItem));
    }

    [TestMethod]
    public void StreamType_Enum_HasExpectedValues()
    {
        Assert.AreEqual(0, (byte)StreamType.Channel);
        Assert.AreEqual(1, (byte)StreamType.Stream);
        Assert.AreEqual(2, (byte)StreamType.Bus);
        Assert.AreEqual(3, (byte)StreamType.AbstractBus);
    }
}
