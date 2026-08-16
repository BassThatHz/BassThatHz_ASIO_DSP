using System;
using ExtendedXmlSerialization;

namespace Test_Project_1.ExtendedXMLSerializer
{
    [TestClass]
    public class Test_PrimitiveValueTools
    {
        [TestMethod]
        public void SetPrimitiveValue_Boolean_ReturnsExpectedString()
        {
            Assert.AreEqual("True", PrimitiveValueTools.SetPrimitiveValue(true, typeof(bool)));
        }

        [TestMethod]
        public void RoundTrip_Boolean_Works()
        {
            var s = PrimitiveValueTools.SetPrimitiveValue(true, typeof(bool));
            var v = PrimitiveValueTools.GetPrimitiveValue(s, typeof(bool), "n");
            Assert.AreEqual(true, v);
        }

        [TestMethod]
        public void RoundTrip_Char_Works()
        {
            var s = PrimitiveValueTools.SetPrimitiveValue('Z', typeof(char));
            var v = PrimitiveValueTools.GetPrimitiveValue(s, typeof(char), "n");
            Assert.AreEqual('Z', v);
        }

        [TestMethod]
        public void RoundTrip_SByte_Works()
        {
            sbyte val = -12;
            var s = PrimitiveValueTools.SetPrimitiveValue(val, typeof(sbyte));
            var v = PrimitiveValueTools.GetPrimitiveValue(s, typeof(sbyte), "n");
            Assert.AreEqual(val, v);
        }

        [TestMethod]
        public void RoundTrip_Byte_Works()
        {
            byte val = 200;
            var s = PrimitiveValueTools.SetPrimitiveValue(val, typeof(byte));
            var v = PrimitiveValueTools.GetPrimitiveValue(s, typeof(byte), "n");
            Assert.AreEqual(val, v);
        }

        [TestMethod]
        public void RoundTrip_Int16_Works()
        {
            short val = -1234;
            var s = PrimitiveValueTools.SetPrimitiveValue(val, typeof(short));
            var v = PrimitiveValueTools.GetPrimitiveValue(s, typeof(short), "n");
            Assert.AreEqual(val, v);
        }

        [TestMethod]
        public void RoundTrip_UInt16_Works()
        {
            ushort val = 60000;
            var s = PrimitiveValueTools.SetPrimitiveValue(val, typeof(ushort));
            var v = PrimitiveValueTools.GetPrimitiveValue(s, typeof(ushort), "n");
            Assert.AreEqual(val, v);
        }

        [TestMethod]
        public void RoundTrip_Int32_Works()
        {
            int val = -123456;
            var s = PrimitiveValueTools.SetPrimitiveValue(val, typeof(int));
            var v = PrimitiveValueTools.GetPrimitiveValue(s, typeof(int), "n");
            Assert.AreEqual(val, v);
        }

        [TestMethod]
        public void RoundTrip_UInt32_Works()
        {
            uint val = 4000000000;
            var s = PrimitiveValueTools.SetPrimitiveValue(val, typeof(uint));
            var v = PrimitiveValueTools.GetPrimitiveValue(s, typeof(uint), "n");
            Assert.AreEqual(val, v);
        }

        [TestMethod]
        public void RoundTrip_Int64_Works()
        {
            long val = -123456789012345;
            var s = PrimitiveValueTools.SetPrimitiveValue(val, typeof(long));
            var v = PrimitiveValueTools.GetPrimitiveValue(s, typeof(long), "n");
            Assert.AreEqual(val, v);
        }

        [TestMethod]
        public void RoundTrip_UInt64_Works()
        {
            ulong val = 18000000000000000000;
            var s = PrimitiveValueTools.SetPrimitiveValue(val, typeof(ulong));
            var v = PrimitiveValueTools.GetPrimitiveValue(s, typeof(ulong), "n");
            Assert.AreEqual(val, v);
        }

        [TestMethod]
        public void RoundTrip_Single_Works()
        {
            float val = 3.14159f;
            var s = PrimitiveValueTools.SetPrimitiveValue(val, typeof(float));
            var v = PrimitiveValueTools.GetPrimitiveValue(s, typeof(float), "n");
            Assert.AreEqual(val, v);
        }

        [TestMethod]
        public void RoundTrip_Double_Works()
        {
            double val = -987654.321;
            var s = PrimitiveValueTools.SetPrimitiveValue(val, typeof(double));
            var v = PrimitiveValueTools.GetPrimitiveValue(s, typeof(double), "n");
            Assert.AreEqual(val, v);
        }

        [TestMethod]
        public void RoundTrip_Decimal_Works()
        {
            decimal val = 123456.789m;
            var s = PrimitiveValueTools.SetPrimitiveValue(val, typeof(decimal));
            var v = PrimitiveValueTools.GetPrimitiveValue(s, typeof(decimal), "n");
            Assert.AreEqual(val, v);
        }

        [TestMethod]
        public void RoundTrip_DateTime_Works()
        {
            var val = new DateTime(2024, 3, 15, 10, 30, 0, DateTimeKind.Utc);
            var s = PrimitiveValueTools.SetPrimitiveValue(val, typeof(DateTime));
            var v = PrimitiveValueTools.GetPrimitiveValue(s, typeof(DateTime), "n");
            Assert.AreEqual(val, v);
        }

        [TestMethod]
        public void RoundTrip_String_Works()
        {
            var val = "Hello World";
            var s = PrimitiveValueTools.SetPrimitiveValue(val, typeof(string));
            var v = PrimitiveValueTools.GetPrimitiveValue(s, typeof(string), "n");
            Assert.AreEqual(val, v);
        }

        [TestMethod]
        public void RoundTrip_Guid_Works()
        {
            var val = Guid.NewGuid();
            var s = PrimitiveValueTools.SetPrimitiveValue(val, typeof(Guid));
            var v = PrimitiveValueTools.GetPrimitiveValue(s, typeof(Guid), "n");
            Assert.AreEqual(val, v);
        }

        [TestMethod]
        public void RoundTrip_TimeSpan_Works()
        {
            var val = TimeSpan.FromMinutes(90);
            var s = PrimitiveValueTools.SetPrimitiveValue(val, typeof(TimeSpan));
            var v = PrimitiveValueTools.GetPrimitiveValue(s, typeof(TimeSpan), "n");
            Assert.AreEqual(val, v);
        }

        [TestMethod]
        public void RoundTrip_NullableInt_WithValue_Works()
        {
            int? val = 42;
            var s = PrimitiveValueTools.SetPrimitiveValue(val.Value, typeof(int));
            var v = PrimitiveValueTools.GetPrimitiveValue(s, typeof(int?), "n");
            Assert.AreEqual(42, v);
        }

        [TestMethod]
        public void GetPrimitiveValue_Enum_ParsesCorrectly()
        {
            var v = PrimitiveValueTools.GetPrimitiveValue("Second", typeof(SampleEnum), "n");
            Assert.AreEqual(SampleEnum.Second, v);
        }

        [TestMethod]
        public void GetPrimitiveValue_DoubleWithComma_UsesDecimalSeparatorFix()
        {
            var v = PrimitiveValueTools.GetPrimitiveValue("3,14", typeof(double), "n");
            Assert.AreEqual(3.14, v);
        }

        [TestMethod]
        public void GetPrimitiveValue_InvalidNumber_ThrowsInvalidOperationException()
        {
            Assert.ThrowsExactly<InvalidOperationException>(() =>
                PrimitiveValueTools.GetPrimitiveValue("not-a-number", typeof(int), "MyNode"));
        }

        [TestMethod]
        public void GetPrimitiveValue_InvalidGuid_ThrowsInvalidOperationException()
        {
            Assert.ThrowsExactly<InvalidOperationException>(() =>
                PrimitiveValueTools.GetPrimitiveValue("not-a-guid", typeof(Guid), "MyNode"));
        }

        [TestMethod]
        public void GetPrimitiveValue_UnsupportedType_ThrowsInvalidOperationExceptionWrappingNotSupported()
        {
            var ex = Assert.ThrowsExactly<InvalidOperationException>(() =>
                PrimitiveValueTools.GetPrimitiveValue("x", typeof(object), "MyNode"));
            Assert.IsInstanceOfType<NotSupportedException>(ex.InnerException);
        }

        [TestMethod]
        public void DecimalSeparator_NullOrEmpty_ReturnsInputUnchanged()
        {
            Assert.IsNull(PrimitiveValueTools.DecimalSeparator(null));
            Assert.AreEqual(string.Empty, PrimitiveValueTools.DecimalSeparator(string.Empty));
        }

        [TestMethod]
        public void DecimalSeparator_ReplacesCommaWithDot()
        {
            Assert.AreEqual("1.23", PrimitiveValueTools.DecimalSeparator("1,23"));
        }

        // ---------------------------------------------------------------------------------
        // Regression coverage for the allocation pass: DecimalSeparator now probes with
        // IndexOf(',') and returns the ORIGINAL string instance when there is no comma,
        // instead of relying on string.Replace's internal no-match shortcut. This runs for
        // every Single/Double/Decimal property on every deserialize (and therefore on every
        // CommonFunctions.DeepClone), so it must stay behaviourally identical.
        // ---------------------------------------------------------------------------------

        [TestMethod]
        public void DecimalSeparator_NoComma_ReturnsSameInstance_NoAllocation()
        {
            var Local_Input = string.Concat("12", ".", "5");
            var Local_Result = PrimitiveValueTools.DecimalSeparator(Local_Input);
            Assert.AreEqual("12.5", Local_Result);
            Assert.AreSame(Local_Input, Local_Result);
        }

        [TestMethod]
        public void DecimalSeparator_MultipleCommas_AllReplaced()
        {
            Assert.AreEqual("1.234.567", PrimitiveValueTools.DecimalSeparator("1,234,567"));
        }

        [TestMethod]
        public void DecimalSeparator_CommaOnly_IsReplaced()
        {
            Assert.AreEqual(".", PrimitiveValueTools.DecimalSeparator(","));
        }

        [TestMethod]
        public void GetPrimitiveValue_NegativeDoubleWithoutComma_StillParses()
        {
            var v = PrimitiveValueTools.GetPrimitiveValue("-3.5", typeof(double), "n");
            Assert.AreEqual(-3.5d, (double)v, 1e-12);
        }
    }
}
