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

        // ---------------------------------------------------------------------------------
        // Regression coverage for the allocation pass on PropertieDefinition.SetValue.
        // The read-only-collection merge path used to call GetType().GetMethod("Add") on every
        // invocation and allocate a fresh object[1] per item. It now caches the Add MethodInfo
        // per target type and reuses one argument buffer. These tests pin the OBSERVABLE
        // behaviour (which items land in which collection) so the cache cannot go stale or
        // leak items across different target types.
        // ---------------------------------------------------------------------------------

        [TestMethod]
        public void SetValue_GetOnlyCollectionProperty_MergesIntoExistingInstance()
        {
            var propInfo = typeof(ReadOnlyCollectionHolder).GetProperty(nameof(ReadOnlyCollectionHolder.Numbers));
            Assert.IsNotNull(propInfo);
            var def = new PropertieDefinition(typeof(ReadOnlyCollectionHolder), propInfo);

            var holder = new ReadOnlyCollectionHolder();
            var original = holder.Numbers;

            def.SetValue(holder, new System.Collections.Generic.List<int> { 1, 2, 3 });

            // Merged into the SAME instance the getter already exposed - not replaced.
            Assert.AreSame(original, holder.Numbers);
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, holder.Numbers);
        }

        [TestMethod]
        public void SetValue_GetOnlyCollectionProperty_RepeatedCalls_AreStableAcrossCachedAddMethod()
        {
            var propInfo = typeof(ReadOnlyCollectionHolder).GetProperty(nameof(ReadOnlyCollectionHolder.Numbers));
            Assert.IsNotNull(propInfo);
            var def = new PropertieDefinition(typeof(ReadOnlyCollectionHolder), propInfo);

            // First call populates the cached Add MethodInfo; subsequent calls must reuse it and
            // still append correctly (and to the right target instance).
            var first = new ReadOnlyCollectionHolder();
            def.SetValue(first, new System.Collections.Generic.List<int> { 1, 2 });

            var second = new ReadOnlyCollectionHolder();
            def.SetValue(second, new System.Collections.Generic.List<int> { 7, 8, 9 });

            def.SetValue(first, new System.Collections.Generic.List<int> { 3 });

            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, first.Numbers);
            CollectionAssert.AreEqual(new[] { 7, 8, 9 }, second.Numbers);
        }

        [TestMethod]
        public void SetValue_GetOnlyCollectionProperty_NonIListSource_StillMerges()
        {
            // The merge path has an IList fast path (indexed, no boxed enumerator) plus a generic
            // IEnumerable fallback. This exercises the fallback with a lazy iterator source.
            var propInfo = typeof(ReadOnlyCollectionHolder).GetProperty(nameof(ReadOnlyCollectionHolder.Numbers));
            Assert.IsNotNull(propInfo);
            var def = new PropertieDefinition(typeof(ReadOnlyCollectionHolder), propInfo);

            var holder = new ReadOnlyCollectionHolder();
            def.SetValue(holder, YieldNumbers());

            CollectionAssert.AreEqual(new[] { 4, 5, 6 }, holder.Numbers);
        }

        private static System.Collections.Generic.IEnumerable<int> YieldNumbers()
        {
            yield return 4;
            yield return 5;
            yield return 6;
        }

        [TestMethod]
        public void SetValue_GetOnlyCollectionProperty_NullValue_IsIgnored()
        {
            var propInfo = typeof(ReadOnlyCollectionHolder).GetProperty(nameof(ReadOnlyCollectionHolder.Numbers));
            Assert.IsNotNull(propInfo);
            var def = new PropertieDefinition(typeof(ReadOnlyCollectionHolder), propInfo);

            var holder = new ReadOnlyCollectionHolder();
            holder.Numbers.Add(42);

            def.SetValue(holder, null!);

            CollectionAssert.AreEqual(new[] { 42 }, holder.Numbers);
        }

        public class FieldHolder
        {
            public int MyField;
        }

        public class ReadOnlyCollectionHolder
        {
            // Get-only collection property - exactly the DSP_Stream.Filters shape that forces
            // PropertieDefinition.SetValue down its reflection merge path.
            public System.Collections.Generic.List<int> Numbers { get; } = new System.Collections.Generic.List<int>();
        }
    }
}
