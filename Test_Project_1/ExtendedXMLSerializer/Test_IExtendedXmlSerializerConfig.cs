using System;
using System.Xml;
using System.Xml.Linq;
using ExtendedXmlSerialization;

namespace Test_Project_1.ExtendedXMLSerializer
{
    [TestClass]
    public class Test_IExtendedXmlSerializerConfig
    {
        private class FakeConfig : IExtendedXmlSerializerConfig
        {
            public Type Type => typeof(string);
            public int Version { get; set; }
            public bool IsCustomSerializer { get; set; }
            public bool IsObjectReference { get; set; }
            public string ExtractedListName { get; set; }

            public int MapCallCount { get; private set; }
            public void Map(Type targetType, XElement currentNode) => MapCallCount++;

            public object ReadObject(XElement element) => element?.Value;

            public int WriteObjectCallCount { get; private set; }
            public void WriteObject(XmlWriter writer, object obj) => WriteObjectCallCount++;

            public string GetObjectId(object obj) => obj?.GetHashCode().ToString();
        }

        [TestMethod]
        public void Type_ReturnsExpectedType()
        {
            IExtendedXmlSerializerConfig config = new FakeConfig();
            Assert.AreEqual(typeof(string), config.Type);
        }

        [TestMethod]
        public void Version_CanBeSetAndRetrieved()
        {
            IExtendedXmlSerializerConfig config = new FakeConfig();
            config.Version = 3;
            Assert.AreEqual(3, config.Version);
        }

        [TestMethod]
        public void Map_InvokesImplementation()
        {
            var config = new FakeConfig();
            config.Map(typeof(int), new XElement("root"));
            Assert.AreEqual(1, config.MapCallCount);
        }

        [TestMethod]
        public void ReadObject_ReturnsElementValue()
        {
            IExtendedXmlSerializerConfig config = new FakeConfig();
            var element = new XElement("node", "hello");
            var result = config.ReadObject(element);
            Assert.AreEqual("hello", result);
        }

        [TestMethod]
        public void ReadObject_NullElement_ReturnsNull()
        {
            IExtendedXmlSerializerConfig config = new FakeConfig();
            Assert.IsNull(config.ReadObject(null));
        }

        [TestMethod]
        public void WriteObject_InvokesImplementation()
        {
            var config = new FakeConfig();
            using var sw = new System.IO.StringWriter();
            using var writer = XmlWriter.Create(sw);
            config.WriteObject(writer, "value");
            Assert.AreEqual(1, config.WriteObjectCallCount);
        }

        [TestMethod]
        public void GetObjectId_ReturnsNonNull_ForNonNullObject()
        {
            IExtendedXmlSerializerConfig config = new FakeConfig();
            var id = config.GetObjectId(new object());
            Assert.IsNotNull(id);
        }

        [TestMethod]
        public void IsCustomSerializer_And_IsObjectReference_DefaultToFalse()
        {
            IExtendedXmlSerializerConfig config = new FakeConfig();
            Assert.IsFalse(config.IsCustomSerializer);
            Assert.IsFalse(config.IsObjectReference);
        }

        [TestMethod]
        public void ExtractedListName_CanBeSetAndRetrieved()
        {
            IExtendedXmlSerializerConfig config = new FakeConfig();
            config.ExtractedListName = "Items";
            Assert.AreEqual("Items", config.ExtractedListName);
        }
    }
}
