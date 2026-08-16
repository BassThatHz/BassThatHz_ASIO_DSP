using System.Collections.Generic;
using ExtendedXmlSerialization.Cache;

namespace Test_Project_1.ExtendedXMLSerializer
{
    [TestClass]
    public class Test_TypeDefinition
    {
        [TestMethod]
        public void PrimitiveType_Int_IsMarkedPrimitive()
        {
            var def = new TypeDefinition(typeof(int));
            Assert.IsTrue(def.IsPrimitive);
            Assert.AreEqual("int", def.PrimitiveName);
        }

        [TestMethod]
        public void PrimitiveType_Guid_IsMarkedPrimitive()
        {
            var def = new TypeDefinition(typeof(System.Guid));
            Assert.IsTrue(def.IsPrimitive);
            Assert.AreEqual("guid", def.PrimitiveName);
        }

        [TestMethod]
        public void PrimitiveType_TimeSpan_IsMarkedPrimitive()
        {
            var def = new TypeDefinition(typeof(System.TimeSpan));
            Assert.IsTrue(def.IsPrimitive);
            Assert.AreEqual("TimeSpan", def.PrimitiveName);
        }

        [TestMethod]
        public void ClassType_IsObjectToSerialize_HasProperties()
        {
            var def = new TypeDefinition(typeof(SimplePoco));
            Assert.IsTrue(def.IsObjectToSerialize);
            Assert.IsFalse(def.IsPrimitive);
            Assert.IsNotNull(def.Properties);
            Assert.IsTrue(def.Properties.Count > 0);
        }

        [TestMethod]
        public void ArrayType_IsArray_And_IsEnumerable()
        {
            var def = new TypeDefinition(typeof(int[]));
            Assert.IsTrue(def.IsArray);
            Assert.IsTrue(def.IsEnumerable);
            Assert.AreEqual("ArrayOfInt32", def.Name);
        }

        [TestMethod]
        public void ListType_IsEnumerable_NotArray()
        {
            var def = new TypeDefinition(typeof(List<int>));
            Assert.IsFalse(def.IsArray);
            Assert.IsTrue(def.IsEnumerable);
            Assert.IsNotNull(def.GenericArguments);
            Assert.AreEqual(typeof(int), def.GenericArguments[0]);
            Assert.IsNotNull(def.MethodAddToList);
        }

        [TestMethod]
        public void EnumType_IsEnum_True()
        {
            var def = new TypeDefinition(typeof(SampleEnum));
            Assert.IsTrue(def.IsEnum);
            Assert.IsFalse(def.IsObjectToSerialize);
        }

        [TestMethod]
        public void StringType_IsNotObjectToSerialize_IsNotEnumerable()
        {
            var def = new TypeDefinition(typeof(string));
            Assert.IsTrue(def.IsPrimitive);
            Assert.IsFalse(def.IsObjectToSerialize);
        }

        [TestMethod]
        public void GetProperty_ExistingName_ReturnsDefinition()
        {
            var def = new TypeDefinition(typeof(SimplePoco));
            var prop = def.GetProperty(nameof(SimplePoco.IntValue));
            Assert.IsNotNull(prop);
            Assert.AreEqual("IntValue", prop.Name);
        }

        [TestMethod]
        public void GetProperty_MissingName_ReturnsNull()
        {
            var def = new TypeDefinition(typeof(SimplePoco));
            var prop = def.GetProperty("DoesNotExist");
            Assert.IsNull(prop);
        }

        [TestMethod]
        public void ObjectActivator_ForConcreteClass_CreatesNewInstance()
        {
            var def = new TypeDefinition(typeof(SimplePoco));
            Assert.IsNotNull(def.ObjectActivator);
            var instance = def.ObjectActivator();
            Assert.IsInstanceOfType<SimplePoco>(instance);
        }

        [TestMethod]
        public void ObjectActivator_ForAbstractClass_IsNull()
        {
            var def = new TypeDefinition(typeof(BaseAnimal));
            Assert.IsNull(def.ObjectActivator);
        }

        [TestMethod]
        public void FullName_MatchesTypeFullName()
        {
            var def = new TypeDefinition(typeof(SimplePoco));
            Assert.AreEqual(typeof(SimplePoco).FullName, def.FullName);
        }

        // ---------------------------------------------------------------------------------
        // Regression coverage for the allocation pass: GetProperty was
        // Properties.FirstOrDefault(p => p.Name == name), which allocated a closure AND a
        // delegate on every call (once per XML element per deserialize). It is now an indexed
        // loop. These tests pin the exact "first match, else null" contract, including the
        // ordinal/case-sensitive comparison and the empty-Properties case.
        // ---------------------------------------------------------------------------------

        [TestMethod]
        public void GetProperty_FindsEveryDeclaredProperty()
        {
            var def = new TypeDefinition(typeof(SimplePoco));
            Assert.IsNotNull(def.Properties);

            for (int Local_i = 0; Local_i < def.Properties.Count; Local_i++)
            {
                var Local_Expected = def.Properties[Local_i];
                var Local_Actual = def.GetProperty(Local_Expected.Name);
                Assert.AreSame(Local_Expected, Local_Actual, "GetProperty failed for " + Local_Expected.Name);
            }
        }

        [TestMethod]
        public void GetProperty_ReturnsFirstMatch()
        {
            var def = new TypeDefinition(typeof(SimplePoco));
            Assert.IsNotNull(def.Properties);

            var Local_First = def.Properties[0];
            Assert.AreSame(Local_First, def.GetProperty(Local_First.Name));
        }

        [TestMethod]
        public void GetProperty_IsCaseSensitive()
        {
            var def = new TypeDefinition(typeof(SimplePoco));
            Assert.IsNotNull(def.GetProperty("IntValue"));
            Assert.IsNull(def.GetProperty("intvalue"));
            Assert.IsNull(def.GetProperty("INTVALUE"));
        }

        [TestMethod]
        public void GetProperty_OnTypeWithNoProperties_ReturnsNull()
        {
            // A primitive definition never populates Properties at all, so GetProperty must
            // tolerate a null list rather than throwing.
            var def = new TypeDefinition(typeof(int));
            Assert.IsNull(def.Properties);
            Assert.IsNull(def.GetProperty("Anything"));
        }
    }
}
