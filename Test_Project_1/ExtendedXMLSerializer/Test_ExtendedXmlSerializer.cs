using System;
using System.Collections.Generic;
using System.Xml;
using ExtendedXmlSerialization;

namespace Test_Project_1.ExtendedXMLSerializer
{
    [TestClass]
    public class Test_ExtendedXmlSerializer
    {
        [TestMethod]
        public void Serialize_SimplePoco_ProducesXmlWithTypeAttribute()
        {
            var serializer = new ExtendedXmlSerializer();
            var poco = new SimplePoco { IntValue = 5, StringValue = "hi" };

            var xml = serializer.Serialize(poco);

            Assert.IsFalse(string.IsNullOrEmpty(xml));
            StringAssert.Contains(xml, "IntValue");
            StringAssert.Contains(xml, "StringValue");
        }

        [TestMethod]
        public void RoundTrip_SimplePoco_AllPrimitiveFieldsPreserved()
        {
            var serializer = new ExtendedXmlSerializer();
            var original = new SimplePoco
            {
                IntValue = 123,
                StringValue = "Hello",
                DoubleValue = 3.14,
                BoolValue = true,
                DateValue = new DateTime(2023, 5, 1, 12, 0, 0, DateTimeKind.Utc),
                GuidValue = Guid.NewGuid(),
                TimeSpanValue = TimeSpan.FromHours(2),
                NullableInt = 7
            };

            var xml = serializer.Serialize(original);
            var result = serializer.Deserialize<SimplePoco>(xml);

            Assert.AreEqual(original.IntValue, result.IntValue);
            Assert.AreEqual(original.StringValue, result.StringValue);
            Assert.AreEqual(original.DoubleValue, result.DoubleValue);
            Assert.AreEqual(original.BoolValue, result.BoolValue);
            Assert.AreEqual(original.DateValue, result.DateValue);
            Assert.AreEqual(original.GuidValue, result.GuidValue);
            Assert.AreEqual(original.TimeSpanValue, result.TimeSpanValue);
            Assert.AreEqual(original.NullableInt, result.NullableInt);
        }

        [TestMethod]
        public void RoundTrip_NestedPoco_PreservesChildObject()
        {
            var serializer = new ExtendedXmlSerializer();
            var original = new NestedPoco
            {
                Name = "Parent",
                Child = new SimplePoco { IntValue = 42, StringValue = "Child" }
            };

            var xml = serializer.Serialize(original);
            var result = serializer.Deserialize<NestedPoco>(xml);

            Assert.AreEqual("Parent", result.Name);
            Assert.IsNotNull(result.Child);
            Assert.AreEqual(42, result.Child.IntValue);
            Assert.AreEqual("Child", result.Child.StringValue);
        }

        [TestMethod]
        public void RoundTrip_NullNestedObject_StaysNull()
        {
            var serializer = new ExtendedXmlSerializer();
            var original = new NestedPoco { Name = "Parent", Child = null };

            var xml = serializer.Serialize(original);
            var result = serializer.Deserialize<NestedPoco>(xml);

            Assert.AreEqual("Parent", result.Name);
            Assert.IsNull(result.Child);
        }

        [TestMethod]
        public void RoundTrip_ListOfPrimitives_Preserved()
        {
            var serializer = new ExtendedXmlSerializer();
            var original = new ListPoco();
            original.Numbers.AddRange(new[] { 1, 2, 3, 4, 5 });

            var xml = serializer.Serialize(original);
            var result = serializer.Deserialize<ListPoco>(xml);

            CollectionAssert.AreEqual(original.Numbers, result.Numbers);
        }

        [TestMethod]
        public void RoundTrip_ListOfObjects_Preserved()
        {
            var serializer = new ExtendedXmlSerializer();
            var original = new ListPoco();
            original.Items.Add(new SimplePoco { IntValue = 1, StringValue = "one" });
            original.Items.Add(new SimplePoco { IntValue = 2, StringValue = "two" });

            var xml = serializer.Serialize(original);
            var result = serializer.Deserialize<ListPoco>(xml);

            Assert.AreEqual(2, result.Items.Count);
            Assert.AreEqual("one", result.Items[0].StringValue);
            Assert.AreEqual("two", result.Items[1].StringValue);
        }

        [TestMethod]
        public void RoundTrip_EmptyList_ProducesEmptyListNotNull()
        {
            var serializer = new ExtendedXmlSerializer();
            var original = new ListPoco();

            var xml = serializer.Serialize(original);
            var result = serializer.Deserialize<ListPoco>(xml);

            Assert.IsNotNull(result.Numbers);
            Assert.AreEqual(0, result.Numbers.Count);
        }

        [TestMethod]
        public void RoundTrip_Array_Preserved()
        {
            var serializer = new ExtendedXmlSerializer();
            var original = new ArrayPoco
            {
                Numbers = new[] { 10, 20, 30 },
                Names = new[] { "a", "b", "c" }
            };

            var xml = serializer.Serialize(original);
            var result = serializer.Deserialize<ArrayPoco>(xml);

            CollectionAssert.AreEqual(original.Numbers, result.Numbers);
            CollectionAssert.AreEqual(original.Names, result.Names);
        }

        [TestMethod]
        public void RoundTrip_Enum_Preserved()
        {
            var serializer = new ExtendedXmlSerializer();
            var original = new EnumPoco { EnumValue = SampleEnum.Second };

            var xml = serializer.Serialize(original);
            var result = serializer.Deserialize<EnumPoco>(xml);

            Assert.AreEqual(SampleEnum.Second, result.EnumValue);
        }

        [TestMethod]
        public void RoundTrip_PolymorphicProperty_PreservesDerivedTypeAndData()
        {
            var serializer = new ExtendedXmlSerializer();
            var original = new PolymorphicPoco
            {
                Animal = new Dog { Name = "Rex", Breed = "Labrador" }
            };

            var xml = serializer.Serialize(original);
            StringAssert.Contains(xml, "Dog");

            var result = serializer.Deserialize<PolymorphicPoco>(xml);

            Assert.IsInstanceOfType<Dog>(result.Animal);
            Assert.AreEqual("Rex", result.Animal.Name);
            Assert.AreEqual("Labrador", ((Dog)result.Animal).Breed);
        }

        [TestMethod]
        public void RoundTrip_PolymorphicProperty_OtherDerivedType_AlsoWorks()
        {
            var serializer = new ExtendedXmlSerializer();
            var original = new PolymorphicPoco
            {
                Animal = new Cat { Name = "Whiskers", Indoor = true }
            };

            var xml = serializer.Serialize(original);
            var result = serializer.Deserialize<PolymorphicPoco>(xml);

            Assert.IsInstanceOfType<Cat>(result.Animal);
            Assert.AreEqual("Whiskers", result.Animal.Name);
            Assert.IsTrue(((Cat)result.Animal).Indoor);
        }

        [TestMethod]
        public void RoundTrip_SpecialXmlCharacters_AreEscapedAndRestored()
        {
            var serializer = new ExtendedXmlSerializer();
            var original = new SpecialCharsPoco
            {
                Text = "Less<Than & Greater>Than \"Quoted\" 'Apostrophe' \n newline"
            };

            var xml = serializer.Serialize(original);
            var result = serializer.Deserialize<SpecialCharsPoco>(xml);

            Assert.AreEqual(original.Text, result.Text);
        }

        [TestMethod]
        public void RoundTrip_EmptyStringProperty_PreservesEmptyString()
        {
            // REGRESSION COVERAGE for a FIXED defect (DefectPin Defect 3a): ReadXml used to do
            // "if (string.IsNullOrEmpty(value)) continue;" for primitive element text, so a string
            // property serialized as an empty element was skipped on deserialize and silently left
            // at its default instead of "" - a save/load data-loss bug. ReadXml now distinguishes
            // "element present but empty" (restore "") from a non-string primitive that genuinely
            // cannot be parsed from empty text.
            var serializer = new ExtendedXmlSerializer();
            var original = new StringOnlyPoco { Text = string.Empty };

            var xml = serializer.Serialize(original);
            var result = serializer.Deserialize<StringOnlyPoco>(xml);

            Assert.IsNotNull(result.Text, "An empty string must not come back as null.");
            Assert.AreEqual(string.Empty, result.Text);
        }

        [TestMethod]
        public void RoundTrip_NullStringProperty_StaysNull()
        {
            var serializer = new ExtendedXmlSerializer();
            var original = new StringOnlyPoco { Text = null };

            var xml = serializer.Serialize(original);
            var result = serializer.Deserialize<StringOnlyPoco>(xml);

            Assert.IsNull(result.Text);
        }

        [TestMethod]
        public void Serialize_NullObject_ThrowsNullReferenceException()
        {
            var serializer = new ExtendedXmlSerializer();
            Assert.ThrowsExactly<NullReferenceException>(() => serializer.Serialize(null));
        }

        [TestMethod]
        public void Deserialize_MalformedXml_ThrowsXmlException()
        {
            var serializer = new ExtendedXmlSerializer();
            Assert.ThrowsExactly<XmlException>(() => serializer.Deserialize<SimplePoco>("<Not<Valid.Xml"));
        }

        [TestMethod]
        public void Deserialize_XmlReferencingUnknownType_Throws()
        {
            var serializer = new ExtendedXmlSerializer();
            var xml = "<SimplePoco type=\"Totally.Bogus.Type, NoSuchAssembly\"></SimplePoco>";
            Assert.ThrowsExactly<Exception>(() => serializer.Deserialize<SimplePoco>(xml));
        }

        [TestMethod]
        public void Deserialize_XmlWithUnknownProperty_ThrowsInvalidOperationException()
        {
            var serializer = new ExtendedXmlSerializer();
            var xml = "<SimplePoco type=\"" + typeof(SimplePoco).FullName + "\"><NoSuchProperty>x</NoSuchProperty></SimplePoco>";
            Assert.ThrowsExactly<InvalidOperationException>(() => serializer.Deserialize<SimplePoco>(xml));
        }

        [TestMethod]
        public void Deserialize_TypeMismatchOnPrimitive_ThrowsInvalidOperationException()
        {
            var serializer = new ExtendedXmlSerializer();
            var xml = "<SimplePoco type=\"" + typeof(SimplePoco).FullName + "\"><IntValue>not-an-int</IntValue></SimplePoco>";
            Assert.ThrowsExactly<InvalidOperationException>(() => serializer.Deserialize<SimplePoco>(xml));
        }

        [TestMethod]
        public void RoundTrip_ObjectReferenceConfiguration_ReusesSameInstanceOnDeserialize()
        {
            var config = new ExtendedXmlSerializerConfig<ReferencedItem>();
            config.ObjectReference(x => x.Id);
            var factory = new SimpleSerializationToolsFactory();
            factory.Configurations.Add(config);
            var serializer = new ExtendedXmlSerializer(factory);

            var shared = new ReferencedItem { Id = 1, Name = "Shared" };
            var holder = new ReferenceHolderPoco();
            holder.Items.Add(shared);
            holder.Items.Add(shared); // same instance referenced twice

            var xml = serializer.Serialize(holder);
            StringAssert.Contains(xml, "ref=");

            var result = serializer.Deserialize<ReferenceHolderPoco>(xml);

            Assert.AreEqual(2, result.Items.Count);
            Assert.AreEqual("Shared", result.Items[0].Name);
            Assert.AreEqual("Shared", result.Items[1].Name);
            Assert.AreSame(result.Items[0], result.Items[1]);
        }

        [TestMethod]
        public void RoundTrip_CustomSerializerConfiguration_UsesCustomLogic()
        {
            var config = new ExtendedXmlSerializerConfig<SimplePoco>();
            config.CustomSerializer(
                (writer, obj) => writer.WriteElementString("CustomValue", obj.IntValue.ToString()),
                element =>
                {
                    var text = element.Element("CustomValue")?.Value;
                    return new SimplePoco { IntValue = int.Parse(text) };
                });
            var factory = new SimpleSerializationToolsFactory();
            factory.Configurations.Add(config);
            var serializer = new ExtendedXmlSerializer(factory);

            var original = new SimplePoco { IntValue = 99, StringValue = "ignored-by-custom-serializer" };

            var xml = serializer.Serialize(original);
            StringAssert.Contains(xml, "CustomValue");

            var result = serializer.Deserialize<SimplePoco>(xml);
            Assert.AreEqual(99, result.IntValue);
        }

        [TestMethod]
        public void RoundTrip_MigrationConfiguration_UpgradesOldVersionXml()
        {
            var config = new ExtendedXmlSerializerConfig<SimplePoco>();
            config.AddMigration(element =>
            {
                // Simulate a migration that renames/adds data for an old xml document.
                if (element.Element("IntValue") == null)
                {
                    element.Add(new System.Xml.Linq.XElement("IntValue", "0"));
                }
            });
            var factory = new SimpleSerializationToolsFactory();
            factory.Configurations.Add(config);
            var serializer = new ExtendedXmlSerializer(factory);

            var oldXml = "<SimplePoco type=\"" + typeof(SimplePoco).FullName + "\"><StringValue>legacy</StringValue></SimplePoco>";

            var result = serializer.Deserialize<SimplePoco>(oldXml);

            Assert.AreEqual("legacy", result.StringValue);
            Assert.AreEqual(0, result.IntValue);
        }

        [TestMethod]
        public void Deserialize_VersionHigherThanConfigSupports_ThrowsXmlException()
        {
            var config = new ExtendedXmlSerializerConfig<SimplePoco>();
            config.AddMigration(element => { });
            var factory = new SimpleSerializationToolsFactory();
            factory.Configurations.Add(config);
            var serializer = new ExtendedXmlSerializer(factory);

            var xml = "<SimplePoco type=\"" + typeof(SimplePoco).FullName + "\" serializeVersion=\"99\"></SimplePoco>";

            Assert.ThrowsExactly<XmlException>(() => serializer.Deserialize<SimplePoco>(xml));
        }

        [TestMethod]
        public void Deserialize_GenericType_WorksViaGenericMethod()
        {
            var serializer = new ExtendedXmlSerializer();
            var original = new SimplePoco { IntValue = 5 };
            var xml = serializer.Serialize(original);

            var resultViaType = (SimplePoco)serializer.Deserialize(xml, typeof(SimplePoco));
            var resultViaGeneric = serializer.Deserialize<SimplePoco>(xml);

            Assert.AreEqual(5, resultViaType.IntValue);
            Assert.AreEqual(5, resultViaGeneric.IntValue);
        }

        [TestMethod]
        public void SerializationToolsFactory_Property_GetSet_Works()
        {
            var serializer = new ExtendedXmlSerializer();
            var factory = new SimpleSerializationToolsFactory();
            serializer.SerializationToolsFactory = factory;
            Assert.AreSame(factory, serializer.SerializationToolsFactory);
        }

        [TestMethod]
        public void WriteXml_PropagatesExceptions_InsteadOfProducingIncompleteXml()
        {
            // REGRESSION COVERAGE for a FIXED defect (DefectPin Defect 3b): WriteXml used to wrap
            // its whole body in `catch (Exception ex) { _ = ex; }`, silently swallowing any failure
            // (e.g. a throwing property getter) and handing the caller TRUNCATED XML that looked
            // like a successful config save. The swallow is gone, so the failure now surfaces.
            var serializer = new ExtendedXmlSerializer();
            var original = new ThrowingPoco();

            var ex = Assert.ThrowsExactly<InvalidOperationException>(() => serializer.Serialize(original));
            Assert.AreEqual("boom", ex.Message);
        }

        // ---------------------------------------------------------------------------------
        // Regression coverage for the allocation pass on ExtendedXmlSerializer:
        //  * XmlWriterSettings is now a single shared static instance instead of a fresh object
        //    per Serialize() call (Serialize backs CommonFunctions.DeepClone<T>).
        //  * WriteXmlArray no longer builds its reserved-object List<string> unconditionally.
        //  * ReadXmlArray enumerates currentNode.Elements() ONCE instead of twice
        //    (Count() + ToArray()).
        // All three must be byte-for-byte output-identical and behaviour-identical.
        // ---------------------------------------------------------------------------------

        [TestMethod]
        public void Serialize_CalledRepeatedly_ProducesIdenticalXml_WithSharedWriterSettings()
        {
            var serializer = new ExtendedXmlSerializer();
            var original = new SimplePoco { IntValue = 7, StringValue = "shared-settings", DoubleValue = 1.5 };

            var first = serializer.Serialize(original);
            var second = serializer.Serialize(original);
            var third = new ExtendedXmlSerializer().Serialize(original);

            // Reusing one XmlWriterSettings instance must not change indentation, newlines or
            // encoding on any subsequent call, nor across serializer instances.
            Assert.AreEqual(first, second);
            Assert.AreEqual(first, third);
            StringAssert.Contains(first, "  <IntValue>");
            StringAssert.Contains(first, System.Environment.NewLine);
        }

        [TestMethod]
        public void RoundTrip_EmptyList_StillRoundTrips()
        {
            // ReadXmlArray with zero child elements - the "materialise once, take Length"
            // rewrite must handle the empty case identically.
            var serializer = new ExtendedXmlSerializer();
            var original = new ListPoco();

            var result = serializer.Deserialize<ListPoco>(serializer.Serialize(original));

            Assert.IsNotNull(result.Numbers);
            Assert.AreEqual(0, result.Numbers.Count);
            Assert.IsNotNull(result.Items);
            Assert.AreEqual(0, result.Items.Count);
        }

        [TestMethod]
        public void RoundTrip_ListAndArray_PreserveOrderAndCount()
        {
            var serializer = new ExtendedXmlSerializer();
            var listOriginal = new ListPoco();
            listOriginal.Numbers.AddRange(new[] { 5, 4, 3, 2, 1 });
            listOriginal.Items.Add(new SimplePoco { IntValue = 1 });
            listOriginal.Items.Add(new SimplePoco { IntValue = 2 });

            var listResult = serializer.Deserialize<ListPoco>(serializer.Serialize(listOriginal));
            CollectionAssert.AreEqual(new[] { 5, 4, 3, 2, 1 }, listResult.Numbers);
            Assert.AreEqual(2, listResult.Items.Count);
            Assert.AreEqual(1, listResult.Items[0].IntValue);
            Assert.AreEqual(2, listResult.Items[1].IntValue);

            var arrayOriginal = new ArrayPoco { Numbers = new[] { 9, 8, 7 }, Names = new[] { "a", "b" } };
            var arrayResult = serializer.Deserialize<ArrayPoco>(serializer.Serialize(arrayOriginal));
            CollectionAssert.AreEqual(new[] { 9, 8, 7 }, arrayResult.Numbers);
            CollectionAssert.AreEqual(new[] { "a", "b" }, arrayResult.Names);
        }

        public class ThrowingPoco
        {
            public string Safe { get; set; } = "ok";
            public int Bad
            {
                get => throw new InvalidOperationException("boom");
                set { }
            }
        }
    }
}
