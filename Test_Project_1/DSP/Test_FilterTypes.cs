using BassThatHz_ASIO_DSP_Processor;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace Test_Project_1;

[TestClass]
public class Test_FilterTypes
{
    [TestMethod]
    public void FilterTypes_Contains_AllExpectedValues()
    {
        var expected = new[]
        {
            "PEQ", "Basic_HPF_LPF", "Low_Shelf", "High_Shelf", "Notch", "Band_Pass", "All_Pass", "Adv_High_Pass", "Adv_Low_Pass", "Polarity", "Delay", "Floor", "Limiter", "SmartGain", "FIR", "Anti_DC", "Mixer", "DynamicRangeCompressor", "ClassicLimiter", "DEQ", "AuxSet", "AuxGet", "ULF_FIR", "GPEQ"
        };
        var actual = Enum.GetNames(typeof(FilterTypes));
        CollectionAssert.AreEquivalent(expected, actual);
    }

    [TestMethod]
    public void FilterTypes_EnumValues_AreUnique()
    {
        var values = Enum.GetValues(typeof(FilterTypes));
        var set = new HashSet<int>();
        foreach (var v in values)
        {
            int intVal = (int)v;
            Assert.IsFalse(set.Contains(intVal), $"Duplicate enum value: {intVal}");
            set.Add(intVal);
        }
    }
}