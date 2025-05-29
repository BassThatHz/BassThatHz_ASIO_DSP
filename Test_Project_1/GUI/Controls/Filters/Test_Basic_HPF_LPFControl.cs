using BassThatHz_ASIO_DSP_Processor.GUI.Controls;
using BassThatHz_ASIO_DSP_Processor;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Windows.Forms;

namespace Test_Project_1;

[TestClass]
public class Test_Basic_HPF_LPFControl
{
    private Basic_HPF_LPFControl control;

    [TestInitialize]
    public void Setup()
    {
        control = new Basic_HPF_LPFControl();
    }

    [TestMethod]
    public void Constructor_InitializesControls()
    {
        Assert.IsNotNull(control.Get_txtHPFFreq);
        Assert.IsNotNull(control.Get_txtLPFFreq);
        Assert.IsNotNull(control.Get_cboHPF);
        Assert.IsNotNull(control.Get_cboLPF);
    }

    [TestMethod]
    public void Constructor_SetsDefaultValues()
    {
        // Default values should match Basic_HPF_LPF defaults
        Assert.AreEqual("1", control.Get_txtHPFFreq.Text);
        Assert.AreEqual("20000", control.Get_txtLPFFreq.Text);
        Assert.AreEqual(Basic_HPF_LPF.FilterOrder.LR_12db, control.Get_cboHPF.SelectedItem);
        Assert.AreEqual(Basic_HPF_LPF.FilterOrder.LR_12db, control.Get_cboLPF.SelectedItem);
    }

    [TestMethod]
    public void Constructor_PopulatesFilterOrderComboBoxes()
    {
        Assert.IsTrue(control.Get_cboHPF.Items.Count > 0);
        Assert.IsTrue(control.Get_cboLPF.Items.Count > 0);
        
        // Verify it contains all filter orders
        foreach (Basic_HPF_LPF.FilterOrder order in Enum.GetValues(typeof(Basic_HPF_LPF.FilterOrder)))
        {
            Assert.IsTrue(control.Get_cboHPF.Items.Contains(order));
            Assert.IsTrue(control.Get_cboLPF.Items.Contains(order));
        }
    }

    [TestMethod]
    public void GetFilter_ReturnsBasic_HPF_LPF()
    {
        var filter = control.GetFilter;
        Assert.IsNotNull(filter);
        Assert.IsInstanceOfType(filter, typeof(Basic_HPF_LPF));
    }

    //[TestMethod]
    //public void ApplySettings_UpdatesFilterParameters()
    //{
    //    // Arrange
    //    control.Get_txtHPFFreq.Text = "50";
    //    control.Get_txtLPFFreq.Text = "15000";
    //    control.Get_cboHPF.SelectedItem = Basic_HPF_LPF.FilterOrder.LR_24db;
    //    control.Get_cboLPF.SelectedItem = Basic_HPF_LPF.FilterOrder.LR_12db;

    //    // Act
    //    control.ApplySettings();

    //    // Assert
    //    var filter = control.GetFilter as Basic_HPF_LPF;
    //    Assert.IsNotNull(filter);
    //    Assert.AreEqual(50, filter.HPFFreq);
    //    Assert.AreEqual(15000, filter.LPFFreq);
    //    Assert.AreEqual(Basic_HPF_LPF.FilterOrder.LR_24db, filter.HPFFilter);
    //    Assert.AreEqual(Basic_HPF_LPF.FilterOrder.LR_12db, filter.LPFFilter);
    //}

    //[TestMethod]
    //public void SetDeepClonedFilter_UpdatesControlValues()
    //{
    //    // Arrange
    //    var sourceFilter = new Basic_HPF_LPF
    //    {
    //        HPFFreq = 75,
    //        LPFFreq = 12000,
    //        HPFFilter = Basic_HPF_LPF.FilterOrder.LR_24db,
    //        LPFFilter = Basic_HPF_LPF.FilterOrder.LR_12db
    //    };

    //    // Act
    //    control.SetDeepClonedFilter(sourceFilter);

    //    // Assert
    //    Assert.AreEqual("75", control.Get_txtHPFFreq.Text);
    //    Assert.AreEqual("12000", control.Get_txtLPFFreq.Text);
    //    Assert.AreEqual(Basic_HPF_LPF.FilterOrder.LR_24db, control.Get_cboHPF.SelectedItem);
    //    Assert.AreEqual(Basic_HPF_LPF.FilterOrder.LR_12db, control.Get_cboLPF.SelectedItem);
    //}

    //[TestMethod]
    //public void TextBox_ValidatesNumericInput()
    //{
    //    // Arrange
    //    var validInputs = new[] { "100", "1000.5", "0.1" };
    //    var invalidInputs = new[] { "abc", "!@#", "" };

    //    // Act & Assert
    //    foreach (var input in validInputs)
    //    {
    //        control.Get_txtHPFFreq.Text = input;
    //        control.ApplySettings();
    //        // Should not throw
    //    }

    //    foreach (var input in invalidInputs)
    //    {
    //        control.Get_txtHPFFreq.Text = input;
    //        Assert.ThrowsException<FormatException>(() => control.ApplySettings());
    //    }
    //}

    //[TestMethod]
    //public void FrequencyLimits_AreEnforced()
    //{
    //    // Test extreme values
    //    control.Get_txtHPFFreq.Text = "0.1";  // Very low
    //    control.Get_txtLPFFreq.Text = "192000";  // Very high
    //    control.ApplySettings();
        
    //    var filter = control.GetFilter as Basic_HPF_LPF;
    //    Assert.IsNotNull(filter);
    //    Assert.IsTrue(filter.HPFFreq > 0);
    //    Assert.IsTrue(filter.LPFFreq > filter.HPFFreq);
    //}

    //[TestMethod]
    //public void ComboBox_ChangesUpdateFilter()
    //{
    //    // Arrange
    //    var newHPFOrder = Basic_HPF_LPF.FilterOrder.LR_24db;
    //    var newLPFOrder = Basic_HPF_LPF.FilterOrder.LR_12db;

    //    // Act
    //    control.Get_cboHPF.SelectedItem = newHPFOrder;
    //    control.Get_cboLPF.SelectedItem = newLPFOrder;
    //    control.ApplySettings();

    //    // Assert
    //    var filter = control.GetFilter as Basic_HPF_LPF;
    //    Assert.IsNotNull(filter);
    //    Assert.AreEqual(newHPFOrder, filter.HPFFilter);
    //    Assert.AreEqual(newLPFOrder, filter.LPFFilter);
    //}

    //[TestMethod]
    //public void SetFilter_WithInvalidType_Throws()
    //{
    //    var invalidFilter = new DummyFilter();
    //    Assert.ThrowsException<ArgumentException>(() => control.SetDeepClonedFilter(invalidFilter));
    //}

    private class DummyFilter : IFilter
    {
        public bool FilterEnabled { get; set; }
        public FilterTypes FilterType => FilterTypes.FIR;
        public FilterProcessingTypes FilterProcessingType => FilterProcessingTypes.WholeBlock;
        public IFilter GetFilter => this;
        public void ApplySettings() { }
        public IFilter DeepClone() => this;
        public void ResetSampleRate(int sampleRate) { }
        public double[] Transform(double[] input, DSP_Stream currentStream) => input;
    }
}