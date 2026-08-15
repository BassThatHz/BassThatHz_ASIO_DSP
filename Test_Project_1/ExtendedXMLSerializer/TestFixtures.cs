using System;
using System.Collections.Generic;

namespace Test_Project_1.ExtendedXMLSerializer
{
    // Shared model classes used by the ExtendedXMLSerializer test suite.
    // Not a test class itself - just fixtures referenced by Test_*.cs files in this folder.

    public class SimplePoco
    {
        public int IntValue { get; set; }
        public string StringValue { get; set; }
        public double DoubleValue { get; set; }
        public bool BoolValue { get; set; }
        public DateTime DateValue { get; set; }
        public Guid GuidValue { get; set; }
        public TimeSpan TimeSpanValue { get; set; }
        public int? NullableInt { get; set; }
    }

    public class NestedPoco
    {
        public string Name { get; set; }
        public SimplePoco Child { get; set; }
    }

    public class ListPoco
    {
        public List<int> Numbers { get; set; } = new List<int>();
        public List<SimplePoco> Items { get; set; } = new List<SimplePoco>();
    }

    public class ArrayPoco
    {
        public int[] Numbers { get; set; }
        public string[] Names { get; set; }
    }

    public enum SampleEnum
    {
        First,
        Second,
        Third
    }

    public class EnumPoco
    {
        public SampleEnum EnumValue { get; set; }
    }

    public abstract class BaseAnimal
    {
        public string Name { get; set; }
    }

    public class Dog : BaseAnimal
    {
        public string Breed { get; set; }
    }

    public class Cat : BaseAnimal
    {
        public bool Indoor { get; set; }
    }

    public class PolymorphicPoco
    {
        public BaseAnimal Animal { get; set; }
    }

    public class SpecialCharsPoco
    {
        public string Text { get; set; }
    }

    public class StringOnlyPoco
    {
        public string Text { get; set; }
    }

    public class ReferencedItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class ReferenceHolderPoco
    {
        public List<ReferencedItem> Items { get; set; } = new List<ReferencedItem>();
    }
}
