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
        // Setup test values in UI
        SetNumericValue(control, "numFrequency", 1000);
        SetNumericValue(control, "numQ", 0.707);
        SetNumericValue(control, "numGain", 6.0);
        SetComboBoxValue(control, "cboFilterType", 0); // LowPass

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

    #region Helper Methods

    private double GetNumericValue(BiQuadFilterControl control, string controlName)
    {
        var numericUpDown = control.Controls.Find(controlName, true)[0] as NumericUpDown;
        return numericUpDown != null ? (double)numericUpDown.Value : 0;
    }

    private void SetNumericValue(BiQuadFilterControl control, string controlName, double value)
    {
        var numericUpDown = control.Controls.Find(controlName, true)[0] as NumericUpDown;
        if (numericUpDown != null)
        {
            numericUpDown.Value = (decimal)value;
        }
    }

    private void SetComboBoxValue(BiQuadFilterControl control, string controlName, int index)
    {
        var comboBox = control.Controls.Find(controlName, true)[0] as ComboBox;
        if (comboBox != null)
        {
            comboBox.SelectedIndex = index;
        }
    }

    #endregion
}