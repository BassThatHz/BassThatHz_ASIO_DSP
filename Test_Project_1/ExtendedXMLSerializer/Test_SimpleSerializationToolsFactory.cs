using ExtendedXmlSerialization;

namespace Test_Project_1.ExtendedXMLSerializer
{
    [TestClass]
    public class Test_SimpleSerializationToolsFactory
    {
        [TestMethod]
        public void Constructor_InitializesEmptyConfigurationsList()
        {
            var factory = new SimpleSerializationToolsFactory();
            Assert.IsNotNull(factory.Configurations);
            Assert.AreEqual(0, factory.Configurations.Count);
        }

        [TestMethod]
        public void GetConfiguration_NoMatchingType_ReturnsNull()
        {
            var factory = new SimpleSerializationToolsFactory();
            var config = new ExtendedXmlSerializerConfig<SimplePoco>();
            factory.Configurations.Add(config);

            var result = factory.GetConfiguration(typeof(NestedPoco));
            Assert.IsNull(result);
        }

        [TestMethod]
        public void GetConfiguration_MatchingType_ReturnsConfig()
        {
            var factory = new SimpleSerializationToolsFactory();
            var config = new ExtendedXmlSerializerConfig<SimplePoco>();
            factory.Configurations.Add(config);

            var result = factory.GetConfiguration(typeof(SimplePoco));
            Assert.IsNotNull(result);
            Assert.AreSame(config, result);
        }

        [TestMethod]
        public void GetConfiguration_MultipleConfigurations_ReturnsCorrectOne()
        {
            var factory = new SimpleSerializationToolsFactory();
            var config1 = new ExtendedXmlSerializerConfig<SimplePoco>();
            var config2 = new ExtendedXmlSerializerConfig<NestedPoco>();
            factory.Configurations.Add(config1);
            factory.Configurations.Add(config2);

            Assert.AreSame(config2, factory.GetConfiguration(typeof(NestedPoco)));
            Assert.AreSame(config1, factory.GetConfiguration(typeof(SimplePoco)));
        }
    }
}
