using System.Collections.Generic;
using ExtendedXmlSerialization.Cache;

namespace Test_Project_1.ExtendedXMLSerializer
{
    [TestClass]
    public class Test_ObjectAccessors
    {
        [TestMethod]
        public void CreateObjectActivator_ForClassWithDefaultCtor_CreatesInstance()
        {
            var activator = ObjectAccessors.CreateObjectActivator(typeof(SimplePoco), false);
            Assert.IsNotNull(activator);
            var instance = activator();
            Assert.IsInstanceOfType<SimplePoco>(instance);
        }

        [TestMethod]
        public void CreateObjectActivator_ForAbstractClass_ReturnsNull()
        {
            var activator = ObjectAccessors.CreateObjectActivator(typeof(BaseAnimal), false);
            Assert.IsNull(activator);
        }

        [TestMethod]
        public void CreateObjectActivator_ForPrimitiveFlagTrue_ReturnsNull()
        {
            var activator = ObjectAccessors.CreateObjectActivator(typeof(int), true);
            Assert.IsNull(activator);
        }

        [TestMethod]
        public void CreatePropertyGetter_ReturnsCorrectValue()
        {
            var getter = ObjectAccessors.CreatePropertyGetter(typeof(SimplePoco), nameof(SimplePoco.IntValue));
            var poco = new SimplePoco { IntValue = 99 };
            Assert.AreEqual(99, getter(poco));
        }

        [TestMethod]
        public void CreatePropertySetter_SetsCorrectValue()
        {
            var setter = ObjectAccessors.CreatePropertySetter(typeof(SimplePoco), nameof(SimplePoco.StringValue));
            var poco = new SimplePoco();
            setter(poco, "hello");
            Assert.AreEqual("hello", poco.StringValue);
        }

        [TestMethod]
        public void CreateMethodAdd_ForList_AddsItem()
        {
            var addMethod = ObjectAccessors.CreateMethodAdd(typeof(List<int>));
            var list = new List<int>();
            addMethod(list, 5);
            CollectionAssert.AreEqual(new[] { 5 }, list);
        }

        [TestMethod]
        public void CreatePropertyGetter_And_Setter_RoundTrip()
        {
            var getter = ObjectAccessors.CreatePropertyGetter(typeof(SimplePoco), nameof(SimplePoco.DoubleValue));
            var setter = ObjectAccessors.CreatePropertySetter(typeof(SimplePoco), nameof(SimplePoco.DoubleValue));
            var poco = new SimplePoco();
            setter(poco, 3.5);
            Assert.AreEqual(3.5, getter(poco));
        }
    }
}
