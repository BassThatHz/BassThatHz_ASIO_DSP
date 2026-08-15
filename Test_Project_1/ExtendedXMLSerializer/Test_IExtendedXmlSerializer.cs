using System;
using ExtendedXmlSerialization;

namespace Test_Project_1.ExtendedXMLSerializer
{
    [TestClass]
    public class Test_IExtendedXmlSerializer
    {
        private class FakeSerializer : IExtendedXmlSerializer
        {
            public ISerializationToolsFactory SerializationToolsFactory { get; set; }

            public string Serialize(object o) => o == null ? null : $"<obj>{o}</obj>";

            public object Deserialize(string xml, Type type) => xml;

            public T Deserialize<T>(string xml) => default;
        }

        [TestMethod]
        public void Serialize_ReturnsExpectedString_ForValidObject()
        {
            IExtendedXmlSerializer serializer = new FakeSerializer();
            var result = serializer.Serialize(42);
            Assert.AreEqual("<obj>42</obj>", result);
        }

        [TestMethod]
        public void Serialize_NullObject_ReturnsNull()
        {
            IExtendedXmlSerializer serializer = new FakeSerializer();
            Assert.IsNull(serializer.Serialize(null));
        }

        [TestMethod]
        public void Deserialize_WithType_ReturnsXmlString()
        {
            IExtendedXmlSerializer serializer = new FakeSerializer();
            var result = serializer.Deserialize("<xml/>", typeof(string));
            Assert.AreEqual("<xml/>", result);
        }

        [TestMethod]
        public void Deserialize_Generic_ReturnsDefault()
        {
            IExtendedXmlSerializer serializer = new FakeSerializer();
            var result = serializer.Deserialize<int>("<xml/>");
            Assert.AreEqual(0, result);
        }

        [TestMethod]
        public void SerializationToolsFactory_CanBeSetAndRetrieved()
        {
            IExtendedXmlSerializer serializer = new FakeSerializer();
            Assert.IsNull(serializer.SerializationToolsFactory);

            var factory = new FakeFactory();
            serializer.SerializationToolsFactory = factory;
            Assert.AreSame(factory, serializer.SerializationToolsFactory);
        }

        private class FakeFactory : ISerializationToolsFactory
        {
            public IExtendedXmlSerializerConfig GetConfiguration(Type type) => null;
        }
    }
}
