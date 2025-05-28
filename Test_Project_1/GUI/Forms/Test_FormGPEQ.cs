using Microsoft.VisualStudio.TestTools.UnitTesting;
using BassThatHz_ASIO_DSP_Processor.GUI.Forms;
using BassThatHz_ASIO_DSP_Processor;
using NAudio.Dsp;
using System.Collections.Generic;

namespace Test_Project_1;

[TestClass]
public class Test_FormGPEQ
{
    [TestMethod]
    public void CanInstantiate_FormGPEQ()
    {
        var form = new FormGPEQ();
        Assert.IsNotNull(form);
    }

    [TestMethod]
    public void SavedChanges_Default_IsFalse()
    {
        var form = new FormGPEQ();
        Assert.IsFalse(form.SavedChanges);
    }

    [TestMethod]
    public void GetListBoxItems_ReturnsListBoxItems()
    {
        var form = new FormGPEQ();
        var items = form.GetListBoxItems();
        Assert.IsNotNull(items);
    }

    [TestMethod]
    public void SetFilters_Null_DoesNotThrow()
    {
        var form = new FormGPEQ();
        form.SetFilters(null);
        Assert.IsNotNull(form.GetListBoxItems());
    }

    [TestMethod]
    public void SetFilters_WithBiQuadFilter_AddsFilter()
    {
        var form = new FormGPEQ();
        var filter = new BiQuadFilter();
        var filters = new List<IFilter> { filter };
        form.SetFilters(filters);
        var items = form.GetListBoxItems();
        Assert.IsNotNull(items);
    }

    [TestMethod]
    public void GetListText_BiQuadFilter_ReturnsString()
    {
        var form = new FormGPEQ();
        var filter = new BiQuadFilter();
        var text = form.GetListText(filter);
        Assert.IsTrue(text.Contains("G:"));
    }
}