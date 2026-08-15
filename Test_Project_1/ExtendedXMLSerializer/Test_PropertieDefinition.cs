using System.Reflection;
using ExtendedXmlSerialization.Cache;

namespace Test_Project_1.ExtendedXMLSerializer
{
    [TestClass]
    public class Test_PropertieDefinition
    {
        [TestMethod]
        public void Constructor_FromPropertyInfo_SetsNameAndType()
        {
            var propInfo = typeof(SimplePoco).GetProperty(nameof(SimplePoco.IntValue));
            var def = new PropertieDefinition(typeof(SimplePoco), propInfo);
            Assert.AreEqual("IntValue", def.Name);
            Assert.AreEqual(typeof(int), def.Type);
        }

        [TestMethod]
        public void GetValue_ReturnsPropertyValue()
        {
            var propInfo = typeof(SimplePoco).GetProperty(nameof(SimplePoco.StringValue));
            var def = new PropertieDefinition(typeof(SimplePoco), propInfo);
            var poco = new SimplePoco { StringValue = "abc" };
            Assert.AreEqual("abc", def.GetValue(poco));
        }

        [TestMethod]
        public void SetValue_WritablePropertyAssignsValue()
        {
            var propInfo = typeof(SimplePoco).GetProperty(nameof(SimplePoco.StringValue));
            var def = new PropertieDefinition(typeof(SimplePoco), propInfo);
            var poco = new SimplePoco();
            def.SetValue(poco, "xyz");
            Assert.AreEqual("xyz", poco.StringValue);
        }

        [TestMethod]
        public void SetValue_ReadOnlyCollectionProperty_MergesItemsIntoExistingInstance()
        {
            var propInfo = typeof(ListPoco).GetProperty(nameof(ListPoco.Numbers));
            var def = new PropertieDefinition(typeof(ListPoco), propInfo);
            var poco = new ListPoco();
            def.SetValue(poco, new System.Collections.Generic.List<int> { 1, 2, 3 });
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, poco.Numbers);
        }

        [TestMethod]
        public void SetValue_NullValue_DoesNotThrow()
        {
            var propInfo = typeof(SimplePoco).GetProperty(nameof(SimplePoco.StringValue));
            var def = new PropertieDefinition(typeof(SimplePoco), propInfo);
            var poco = new SimplePoco { StringValue = "keep" };
            def.SetValue(poco, null);
            // writable setter path: assigning null overwrites via the compiled setter
            Assert.IsNull(poco.StringValue);
        }

        [TestMethod]
        public void Constructor_FromFieldInfo_SetsNameAndType()
        {
            var fieldInfo = typeof(FieldHolder).GetField(nameof(FieldHolder.MyField));
            var def = new PropertieDefinition(typeof(FieldHolder), fieldInfo);
            Assert.AreEqual("MyField", def.Name);
            Assert.AreEqual(typeof(int), def.Type);

            var holder = new FieldHolder();
            def.SetValue(holder, 77);
            Assert.AreEqual(77, holder.MyField);
            Assert.AreEqual(77, def.GetValue(holder));
        }

        public class FieldHolder
        {
            public int MyField;
        }
    }
}
