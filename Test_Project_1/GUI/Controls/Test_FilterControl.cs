using BassThatHz_ASIO_DSP_Processor.GUI.Controls;
using BassThatHz_ASIO_DSP_Processor; // For IFilter, FilterTypes, FilterProcessingTypes, DSP_Stream
using System;
using System.Reflection;
using System.Windows.Forms;
using System.ComponentModel;

namespace Test_Project_1;

[TestClass]
public class Test_FilterControl
{
    //[TestMethod]
    //public void DefaultProperties_AreAccessible()
    //{
    //    var control = new FilterControl();
    //    Assert.IsNotNull(control.Get_btnDown);
    //    Assert.IsNotNull(control.Get_btnUp);
    //    Assert.IsNotNull(control.Get_chkEnabled);
    //    Assert.IsNotNull(control.Get_btnDelete);
    //    Assert.IsNotNull(control.Get_cboFilterType);
    //}

    [TestMethod]
    public void Constructor_PopulatesFilterTypes()
    {
        var control = new FilterControl();
        Assert.IsTrue(control.Get_cboFilterType.Items.Count > 0);
    }

    [TestMethod]
    public void LoadConfigRefresh_SetsSelectedIndex()
    {
        var control = new FilterControl();
        var dummyFilter = new DummyFilter();
        control.LoadConfigRefresh(dummyFilter);
        Assert.AreEqual(control.Get_cboFilterType.SelectedItem, dummyFilter.FilterType);
    }

    [TestMethod]
    public void chkEnabled_CheckedChanged_SetsFilterEnabled()
    {
        var control = new FilterControl();
        var dummyFilterControl = new DummyFilterControl();
        control.GetType().GetProperty("CurrentFilterControl", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!.SetValue(control, dummyFilterControl);
        control.Get_chkEnabled.Checked = true;
        control.chkEnabled_CheckedChanged(control, EventArgs.Empty);
        Assert.IsTrue(dummyFilterControl.GetFilter.FilterEnabled);
    }

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
        public IFilter GetFilter { get; } = new DummyFilter();
        public void ApplySettings() { }
        public void SetDeepClonedFilter(IFilter input) { }
    }
}