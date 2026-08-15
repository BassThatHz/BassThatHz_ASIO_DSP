using System;
using System.Xml;
using System.Xml.Linq;
using ExtendedXmlSerialization;

namespace Test_Project_1.ExtendedXMLSerializer
{
    [TestClass]
    public class Test_ISerializationToolsFactory
    {
        private class FakeConfig : IExtendedXmlSerializerConfig
        {
            public Type Type => typeof(object);
            public int Version { get; set; }
            public bool IsCustomSerializer { get; set; }
            public bool IsObjectReference { get; set; }
            public string ExtractedListName { get; set; }
            public void Map(Type targetType, XElement currentNode) { }
            public object ReadObject(XElement element) => null;
            public void WriteObject(XmlWriter writer, object obj) { }
            public string GetObjectId(object obj) => "id";
        }

        private class FakeFactory : ISerializationToolsFactory
        {
            public IExtendedXmlSerializerConfig ConfigToReturn { get; set; }
            public Type LastRequestedType { get; private set; }

            public IExtendedXmlSerializerConfig GetConfiguration(Type type)
            {
                LastRequestedType = type;
                return ConfigToReturn;
            }
        }

        [TestMethod]
        public void GetConfiguration_ReturnsProvidedConfig_ForRequestedType()
        {
            var expectedConfig = new FakeConfig();
            var factory = new FakeFactory { ConfigToReturn = expectedConfig };

            var result = factory.GetConfiguration(typeof(string));

            Assert.AreSame(expectedConfig, result);
            Assert.AreEqual(typeof(string), factory.LastRequestedType);
        }

        [TestMethod]
        public void GetConfiguration_ReturnsNull_WhenNoConfigConfigured()
        {
            var factory = new FakeFactory();

            var result = factory.GetConfiguration(typeof(int));

            Assert.IsNull(result);
        }

        [TestMethod]
        public void GetConfiguration_NullType_DoesNotThrow_AndTracksNull()
        {
            var factory = new FakeFactory();

            var result = factory.GetConfiguration(null);

            Assert.IsNull(result);
            Assert.IsNull(factory.LastRequestedType);
        }

        [TestMethod]
        public void Interface_DefinesExpectedMethodSignature()
        {
            var method = typeof(ISerializationToolsFactory).GetMethod("GetConfiguration");

            Assert.IsNotNull(method);
            Assert.AreEqual(typeof(IExtendedXmlSerializerConfig), method.ReturnType);
            var parameters = method.GetParameters();
            Assert.AreEqual(1, parameters.Length);
            Assert.AreEqual(typeof(Type), parameters[0].ParameterType);
        }
    }
}
