using Microsoft.VisualStudio.TestTools.UnitTesting;
using BassThatHz_ASIO_DSP_Processor;
using System;

namespace Test_Project_1;

[TestClass]
public class Test_IFilterControl
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

    private class DummyFilterControl : IFilterControl
    {
        public IFilter? LastSetFilter { get; private set; }
        public bool ApplySettingsCalled { get; private set; }
        public IFilter GetFilter => _filter;
        private IFilter _filter = new DummyFilter();
        public void ApplySettings() => ApplySettingsCalled = true;
        public void SetDeepClonedFilter(IFilter input)
        {
            LastSetFilter = input;
            _filter = input;
        }
    }

    [TestMethod]
    public void IFilterControl_Implements_AllMembers()
    {
        var control = new DummyFilterControl();
        Assert.IsInstanceOfType(control, typeof(IGetFilter));
        Assert.IsInstanceOfType(control, typeof(IApplySettings));
        Assert.IsInstanceOfType(control, typeof(ISetDeepClonedFilter));
        Assert.IsInstanceOfType(control, typeof(IFilterControl));
    }

    [TestMethod]
    public void GetFilter_ReturnsFilter()
    {
        var control = new DummyFilterControl();
        Assert.IsNotNull(control.GetFilter);
        Assert.IsInstanceOfType(control.GetFilter, typeof(IFilter));
    }

    [TestMethod]
    public void ApplySettings_CallsFlag()
    {
        var control = new DummyFilterControl();
        Assert.IsFalse(control.ApplySettingsCalled);
        control.ApplySettings();
        Assert.IsTrue(control.ApplySettingsCalled);
    }

    [TestMethod]
    public void SetDeepClonedFilter_SetsFilter()
    {
        var control = new DummyFilterControl();
        var filter = new DummyFilter();
        control.SetDeepClonedFilter(filter);
        Assert.AreEqual(filter, control.LastSetFilter);
        Assert.AreEqual(filter, control.GetFilter);
    }
}