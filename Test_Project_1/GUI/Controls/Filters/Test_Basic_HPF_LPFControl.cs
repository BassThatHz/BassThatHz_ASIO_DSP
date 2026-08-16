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
    //        Assert.ThrowsExactly<FormatException>(() => control.ApplySettings());
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
    //    Assert.ThrowsExactly<ArgumentException>(() => control.SetDeepClonedFilter(invalidFilter));
    //}

    // ---------------------------------------------------------------------------------
    // Regression coverage for the allocation pass: ShowBiQuads used to allocate a fresh
    // 1024-char StringBuilder on every call. It now reuses one per-control buffer and
    // Clear()s it first. If the Clear() were ever dropped the text would accumulate, so
    // these tests pin that repeated calls produce IDENTICAL, non-growing output.
    // ---------------------------------------------------------------------------------

    private static TextBoxBase GetBiQuadsTextBox(Basic_HPF_LPFControl input)
    {
        var Local_Field = typeof(Basic_HPF_LPFControl).GetField(
            "txtBiQuads",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.IsNotNull(Local_Field, "txtBiQuads field not found on Basic_HPF_LPFControl");
        var Local_Value = Local_Field.GetValue(input) as TextBoxBase;
        Assert.IsNotNull(Local_Value, "txtBiQuads was not a TextBoxBase");
        return Local_Value;
    }

    [TestMethod]
    public void ShowBiQuads_RepeatedApply_ProducesIdenticalNonGrowingText()
    {
        var Local_TextBox = GetBiQuadsTextBox(control);

        control.ApplySettings();
        var Local_First = Local_TextBox.Text;

        control.ApplySettings();
        var Local_Second = Local_TextBox.Text;

        control.ApplySettings();
        var Local_Third = Local_TextBox.Text;

        Assert.IsFalse(string.IsNullOrEmpty(Local_First));
        Assert.AreEqual(Local_First, Local_Second);
        Assert.AreEqual(Local_First, Local_Third);
        Assert.AreEqual(Local_First.Length, Local_Third.Length);
    }

    [TestMethod]
    public void ShowBiQuads_EmitsAllEightBiQuadBlocks()
    {
        var Local_TextBox = GetBiQuadsTextBox(control);
        control.ApplySettings();
        var Local_Text = Local_TextBox.Text;

        for (int Local_i = 1; Local_i <= 8; Local_i++)
            StringAssert.Contains(Local_Text, "biquad" + Local_i.ToString());

        // Exactly 8 blocks - not 16 from a buffer that was never cleared.
        int Local_Count = 0;
        int Local_Index = Local_Text.IndexOf("biquad1", StringComparison.Ordinal);
        while (Local_Index >= 0)
        {
            Local_Count++;
            Local_Index = Local_Text.IndexOf("biquad1", Local_Index + 1, StringComparison.Ordinal);
        }
        Assert.AreEqual(1, Local_Count, "biquad1 should appear exactly once per render");
    }

    #region StackOverflow regression (nyquist == 0 infinite TextChanged recursion)
    // These pin the fix for a PRE-EXISTING defect: with Program.DSP_Info.InSampleRate == 0 the
    // nyquist clamp in TxtHPFFreq_TextChanged / TxtLPFFreq_TextChanged clamped any entered value
    // to "0", which the same handler rewrote to "0.01", which was again > nyquist(0) - infinite
    // recursion ending in an UNCATCHABLE StackOverflowException that kills the process (and,
    // previously, the whole test host). The handlers now only apply the clamp when nyquist > 0.

    [TestMethod]
    public void TxtHPFFreq_WithNoSampleRateConfigured_DoesNotRecurseInfinitely()
    {
        int Local_Original = Program.DSP_Info.InSampleRate;
        try
        {
            Program.DSP_Info.InSampleRate = 0; //no ASIO device configured yet
            this.control.Get_txtHPFFreq.Text = "5";
            //Reaching this line at all is the assertion: before the fix this stack-overflowed.
            Assert.AreEqual("5", this.control.Get_txtHPFFreq.Text,
                "With no sample rate configured the value must be left alone, not clamped to 0.");
        }
        finally
        {
            Program.DSP_Info.InSampleRate = Local_Original;
        }
    }

    [TestMethod]
    public void TxtLPFFreq_WithNoSampleRateConfigured_DoesNotRecurseInfinitely()
    {
        int Local_Original = Program.DSP_Info.InSampleRate;
        try
        {
            Program.DSP_Info.InSampleRate = 0;
            this.control.Get_txtLPFFreq.Text = "5";
            Assert.AreEqual("5", this.control.Get_txtLPFFreq.Text);
        }
        finally
        {
            Program.DSP_Info.InSampleRate = Local_Original;
        }
    }

    [TestMethod]
    public void TxtFreq_WithNoSampleRateConfigured_ZeroStillBecomesMinimum()
    {
        int Local_Original = Program.DSP_Info.InSampleRate;
        try
        {
            Program.DSP_Info.InSampleRate = 0;
            this.control.Get_txtHPFFreq.Text = "0";
            //The zero-guard must still run and must itself settle rather than ping-pong.
            Assert.AreEqual("0.01", this.control.Get_txtHPFFreq.Text);
        }
        finally
        {
            Program.DSP_Info.InSampleRate = Local_Original;
        }
    }

    [TestMethod]
    public void TxtFreq_WithValidSampleRate_StillClampsToNyquist_AndSettles()
    {
        int Local_Original = Program.DSP_Info.InSampleRate;
        try
        {
            Program.DSP_Info.InSampleRate = 48000; //nyquist = 24000
            this.control.Get_txtLPFFreq.Text = "30000";
            Assert.AreEqual("24000", this.control.Get_txtLPFFreq.Text,
                "Existing clamp behaviour must be unchanged when a sample rate IS configured.");

            this.control.Get_txtHPFFreq.Text = "100";
            Assert.AreEqual("100", this.control.Get_txtHPFFreq.Text,
                "A value below nyquist must pass through untouched.");
        }
        finally
        {
            Program.DSP_Info.InSampleRate = Local_Original;
        }
    }
    #endregion

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