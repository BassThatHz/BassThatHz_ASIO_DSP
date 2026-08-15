using Microsoft.VisualStudio.TestTools.UnitTesting;
using BassThatHz_ASIO_DSP_Processor;
using BassThatHz_ASIO_DSP_Processor.GUI.Controls;
using BassThatHz_ASIO_DSP_Processor.DSP.Filters;
using NAudio.Dsp;
using System.Windows.Forms;

namespace Test_Project_1;

[TestClass]
public class Test_BiQuadFilterControl
{
    [TestMethod]
    public void Constructor_InitializesControls()
    {
        var control = new BiQuadFilterControl();
        Assert.IsNotNull(control.Controls);
        Assert.IsNotNull(control.GetFilter);
    }

    [TestMethod]
    public void GetFilter_ReturnsExpectedType()
    {
        var control = new BiQuadFilterControl();
        var filter = control.GetFilter;
        Assert.IsNotNull(filter);
        Assert.IsInstanceOfType(filter, typeof(BiQuadFilter));
    }

    [TestMethod]
    public void ApplySettings_UpdatesFilterFromUI()
    {
        var control = new BiQuadFilterControl();
        // Setup test values in the actual UI controls (txtF/txtQ/txtG). FilterType
        // defaults to PEQ (FilterTypes.PEQ == 0), which ApplySettings routes to PeakingEQ.
        control.Get_txtF.Text = "1000";
        control.Get_txtQ.Text = "0.707";
        control.Get_txtG.Text = "6.0";

        control.ApplySettings();

        var filter = control.GetFilter as BiQuadFilter;
        Assert.IsNotNull(filter);
        Assert.AreEqual(1000, filter.Frequency);
        Assert.AreEqual(0.707, filter.Q);
        Assert.AreEqual(6.0, filter.Gain);
    }

    //[TestMethod]
    //public void SetDeepClonedFilter_UpdatesUIFromFilter()
    //{
    //    var control = new BiQuadFilterControl();
    //    var sourceFilter = new BiQuadFilter
    //    {
    //        Frequency = 2000,
    //        Q = 1.414,
    //        Gain = -3.0
    //    };

    //    control.SetDeepClonedFilter(sourceFilter);
        
    //    // Verify UI values were updated
    //    Assert.AreEqual(2000, GetNumericValue(control, "numFrequency"));
    //    Assert.AreEqual(1.414, GetNumericValue(control, "numQ"));
    //    Assert.AreEqual(-3.0, GetNumericValue(control, "numGain"));
    //}

    [TestMethod]
    public void BiQuadFilterControl_ImplementsInterfaces()
    {
        var control = new BiQuadFilterControl();
        Assert.IsInstanceOfType(control, typeof(IFilterControl));
        Assert.IsInstanceOfType(control, typeof(IGetFilter));
        Assert.IsInstanceOfType(control, typeof(IApplySettings));
        Assert.IsInstanceOfType(control, typeof(ISetDeepClonedFilter));
    }

}