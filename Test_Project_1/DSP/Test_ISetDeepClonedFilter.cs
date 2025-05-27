using BassThatHz_ASIO_DSP_Processor;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Test_Project_1;

[TestClass]
public class Test_ISetDeepClonedFilter
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

    private class DummySetDeepClonedFilter : ISetDeepClonedFilter
    {
        public IFilter? LastSetFilter { get; private set; }
        public void SetDeepClonedFilter(IFilter input) => LastSetFilter = input;
    }

    [TestMethod]
    public void SetDeepClonedFilter_SetsFilter()
    {
        var dummy = new DummySetDeepClonedFilter();
        var filter = new DummyFilter();
        dummy.SetDeepClonedFilter(filter);
        Assert.AreEqual(filter, dummy.LastSetFilter);
    }
}