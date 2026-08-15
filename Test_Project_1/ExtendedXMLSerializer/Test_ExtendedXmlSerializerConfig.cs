using System.Xml;
using System.Xml.Linq;
using ExtendedXmlSerialization;

namespace Test_Project_1.ExtendedXMLSerializer
{
    [TestClass]
    public class Test_ExtendedXmlSerializerConfig
    {
        [TestMethod]
        public void Constructor_DefaultVersionIsZero()
        {
            var config = new ExtendedXmlSerializerConfig<SimplePoco>();
            Assert.AreEqual(0, config.Version);
            Assert.AreEqual(typeof(SimplePoco), config.Type);
        }

        [TestMethod]
        public void AddMigration_IncrementsVersion()
        {
            var config = new ExtendedXmlSerializerConfig<SimplePoco>();
            config.AddMigration(e => { });
            Assert.AreEqual(1, config.Version);
            config.AddMigration(e => { });
            Assert.AreEqual(2, config.Version);
        }

        [TestMethod]
        public void ObjectReference_SetsIsObjectReferenceFlag()
        {
            var config = new ExtendedXmlSerializerConfig<ReferencedItem>();
            config.ObjectReference(x => x.Id);
            IExtendedXmlSerializerConfig iface = config;
            Assert.IsTrue(iface.IsObjectReference);
        }

        [TestMethod]
        public void GetObjectId_ReturnsStringOfConfiguredId()
        {
            var config = new ExtendedXmlSerializerConfig<ReferencedItem>();
            config.ObjectReference(x => x.Id);
            IExtendedXmlSerializerConfig iface = config;
            var id = iface.GetObjectId(new ReferencedItem { Id = 42 });
            Assert.AreEqual("42", id);
        }

        [TestMethod]
        public void CustomSerializer_SetsIsCustomSerializerFlag_AndInvokesDelegates()
        {
            var config = new ExtendedXmlSerializerConfig<SimplePoco>();
            config.CustomSerializer(
                (writer, obj) => writer.WriteElementString("Custom", obj.StringValue),
                element => new SimplePoco { StringValue = element.Element("Custom")?.Value });

            IExtendedXmlSerializerConfig iface = config;
            Assert.IsTrue(iface.IsCustomSerializer);

            using var sw = new System.IO.StringWriter();
            using (var xw = XmlWriter.Create(sw))
            {
                xw.WriteStartElement("Root");
                iface.WriteObject(xw, new SimplePoco { StringValue = "hi" });
                xw.WriteEndElement();
            }
            var xml = sw.ToString();
            StringAssert.Contains(xml, "hi");

            var doc = XDocument.Parse(xml);
            var result = (SimplePoco)iface.ReadObject(doc.Root);
            Assert.AreEqual("hi", result.StringValue);
        }

        [TestMethod]
        public void Map_NoSerializeVersionAttribute_NoMigrationsRun()
        {
            var config = new ExtendedXmlSerializerConfig<SimplePoco>();
            var element = new XElement("Root");
            // Should not throw, no migrations registered, version is 0.
            IExtendedXmlSerializerConfig iface = config;
            iface.Map(typeof(SimplePoco), element);
        }

        [TestMethod]
        public void Map_CurrentVersionHigherThanConfig_ThrowsXmlException()
        {
            var config = new ExtendedXmlSerializerConfig<SimplePoco>();
            var element = new XElement("Root", new XAttribute("serializeVersion", "5"));
            IExtendedXmlSerializerConfig iface = config;
            Assert.ThrowsExactly<XmlException>(() => iface.Map(typeof(SimplePoco), element));
        }

        [TestMethod]
        public void Map_RunsRegisteredMigrationsInOrder()
        {
            var config = new ExtendedXmlSerializerConfig<SimplePoco>();
            var callOrder = new System.Collections.Generic.List<int>();
            config.AddMigration(e => callOrder.Add(0));
            config.AddMigration(e => callOrder.Add(1));

            var element = new XElement("Root", new XAttribute("serializeVersion", "0"));
            IExtendedXmlSerializerConfig iface = config;
            iface.Map(typeof(SimplePoco), element);

            CollectionAssert.AreEqual(new[] { 0, 1 }, callOrder);
        }

        [TestMethod]
        public void Map_AlreadyAtCurrentVersion_RunsNoMigrations()
        {
            var config = new ExtendedXmlSerializerConfig<SimplePoco>();
            var callOrder = new System.Collections.Generic.List<int>();
            config.AddMigration(e => callOrder.Add(0));

            var element = new XElement("Root", new XAttribute("serializeVersion", "1"));
            IExtendedXmlSerializerConfig iface = config;
            iface.Map(typeof(SimplePoco), element);

            Assert.AreEqual(0, callOrder.Count);
        }

        [TestMethod]
        public void ExtractedListName_DefaultsToNull()
        {
            var config = new ExtendedXmlSerializerConfig<SimplePoco>();
            IExtendedXmlSerializerConfig iface = config;
            Assert.IsNull(iface.ExtractedListName);
        }
    }
}
