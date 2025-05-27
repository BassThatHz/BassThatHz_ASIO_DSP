using BassThatHz_ASIO_DSP_Processor;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace Test_Project_1;

[TestClass]
public class Test_FilterProcessingTypes
{
    [TestMethod]
    public void FilterProcessingTypes_Contains_AllExpectedValues()
    {
        var expected = new[] { "WholeBlock" };
        var actual = Enum.GetNames(typeof(FilterProcessingTypes));
        CollectionAssert.AreEquivalent(expected, actual);
    }

    [TestMethod]
    public void FilterProcessingTypes_EnumValues_AreUnique()
    {
        var values = Enum.GetValues(typeof(FilterProcessingTypes));
        var set = new HashSet<int>();
        foreach (var v in values)
        {
            int intVal = (int)v;
            Assert.IsFalse(set.Contains(intVal), $"Duplicate enum value: {intVal}");
            set.Add(intVal);
        }
    }
}