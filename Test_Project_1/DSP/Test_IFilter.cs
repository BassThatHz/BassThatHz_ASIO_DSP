using BassThatHz_ASIO_DSP_Processor;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Test_Project_1;

[TestClass]
public class Test_IFilter
{
    private class DummyFilter : IFilter
    {
        public bool FilterEnabled { get; set; }
        public IFilter GetFilter => this;
        public FilterTypes FilterType => FilterTypes.FIR;
        public FilterProcessingTypes FilterProcessingType => FilterProcessingTypes.WholeBlock;
        public IFilter DeepClone() => this;
        public void ApplySettings() { }
        public void ResetSampleRate(int sampleRate) { }
        public double[] Transform(double[] input, DSP_Stream currentStream) => input;
    }

    [TestMethod]
    public void IFilter_Implements_AllMembers()
    {
        var filter = new DummyFilter();
        Assert.IsFalse(filter.FilterEnabled);
        Assert.AreEqual(filter, filter.GetFilter);
        Assert.AreEqual(FilterTypes.FIR, filter.FilterType);
        Assert.AreEqual(FilterProcessingTypes.WholeBlock, filter.FilterProcessingType);
        Assert.AreEqual(filter, filter.DeepClone());
        filter.ApplySettings(); // Should not throw
        filter.ResetSampleRate(48000); // Should not throw
        var input = new double[] { 1, 2, 3 };
        var output = filter.Transform(input, new DSP_Stream());
        Assert.AreEqual(input, output);
    }
}