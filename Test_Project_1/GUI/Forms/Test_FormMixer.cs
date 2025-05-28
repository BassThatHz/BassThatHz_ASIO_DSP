using Microsoft.VisualStudio.TestTools.UnitTesting;
using BassThatHz_ASIO_DSP_Processor.GUI.Forms;
using System.Collections.Generic;
using BassThatHz_ASIO_DSP_Processor; // For MixerInput

namespace Test_Project_1;

[TestClass]
public class Test_FormMixer
{
    [TestMethod]
    public void CanInstantiate_FormMixer()
    {
        var form = new FormMixer();
        Assert.IsNotNull(form);
    }

    [TestMethod]
    public void CanSetAndInvoke_ClearAllFilterElements()
    {
        var form = new FormMixer();
        bool called = false;
        form.ClearAllFilterElements = () => called = true;
        form.ClearAllFilterElements?.Invoke();
        Assert.IsTrue(called);
    }

    [TestMethod]
    public void CanSetAndInvoke_AddRangeOfFilterElements()
    {
        var form = new FormMixer();
        List<MixerInput> received = null;
        form.AddRangeOfFilterElements = (inputs) => received = inputs;
        var testInputs = new List<MixerInput> { new MixerInput() };
        form.AddRangeOfFilterElements?.Invoke(testInputs);
        Assert.IsNotNull(received);
        Assert.AreEqual(1, received.Count);
    }

    [TestMethod]
    public void CanCall_RedrawPanelItemsFromLoader()
    {
        var form = new FormMixer();
        var testInputs = new List<MixerInput> { new MixerInput() };
        // Should not throw
        form.RedrawPanelItemsFromLoader(testInputs);
    }
}