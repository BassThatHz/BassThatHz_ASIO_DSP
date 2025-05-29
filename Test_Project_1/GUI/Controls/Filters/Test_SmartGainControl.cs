using Microsoft.VisualStudio.TestTools.UnitTesting;
using BassThatHz_ASIO_DSP_Processor.GUI.Controls;
using BassThatHz_ASIO_DSP_Processor;
using NAudio.Utils;
using System;
using System.Reflection;
using System.Windows.Forms;

namespace Test_Project_1
{
    [TestClass]
    public class Test_SmartGainControl
    {
        private TestableSmartGainControl _control;
        private TextBox _gainTextBox;
        private TextBox _durationTextBox;
        private CheckBox _peakCheckBox;
        private CheckBox _peakHoldCheckBox;
        private Label _appliedGainLabel;
        private Label _peakLevelLabel;
        private Label _headroomLabel;

        [TestInitialize]
        public void InitializeTest()
        {
            _control = new TestableSmartGainControl();
            _gainTextBox = new TextBox();
            _durationTextBox = new TextBox();
            _peakCheckBox = new CheckBox();
            _peakHoldCheckBox = new CheckBox();
            _appliedGainLabel = new Label();
            _peakLevelLabel = new Label();
            _headroomLabel = new Label();

            // Set up test controls
            SetPrivateField(_control, "txtGain", _gainTextBox);
            SetPrivateField(_control, "txtDuration", _durationTextBox);
            SetPrivateField(_control, "chkPeak", _peakCheckBox);
            SetPrivateField(_control, "chkPeakHold", _peakHoldCheckBox);
            SetPrivateField(_control, "lblAppliedGain", _appliedGainLabel);
            SetPrivateField(_control, "lblPeakLevel", _peakLevelLabel);
            SetPrivateField(_control, "lblHeadroom", _headroomLabel);
        }

        private void SetPrivateField(object obj, string fieldName, object value)
        {
            var field = obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            field?.SetValue(obj, value);
        }

        private T GetPrivateField<T>(object obj, string fieldName)
        {
            var field = obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            return (T)field?.GetValue(obj);
        }

        [TestMethod]
        public void TestInitialize_CreatesNewFilter()
        {
            var filter = GetPrivateField<SmartGain>(_control, "Filter");
            Assert.IsNotNull(filter);
            Assert.AreEqual(FilterTypes.SmartGain, filter.FilterType);
        }

        [TestMethod]
        public void TestGainInput_ValidatesInput()
        {
            var validChars = new[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '-', '.', '\b' };
            var invalidChars = new[] { 'a', 'b', 'c', ' ', '+', '*' };

            foreach (var c in validChars)
            {
                var e = new KeyPressEventArgs(c);
                _control.TestGainKeyPress(e);
                Assert.IsFalse(e.Handled, $"Should accept character: {c}");
            }

            foreach (var c in invalidChars)
            {
                var e = new KeyPressEventArgs(c);
                _control.TestGainKeyPress(e);
                Assert.IsTrue(e.Handled, $"Should reject character: {c}");
            }
        }

        [TestMethod]
        public void TestDurationInput_ValidatesInput()
        {
            var validChars = new[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '.', '\b' };
            var invalidChars = new[] { 'a', 'b', 'c', '-', ' ', '+', '*' };

            foreach (var c in validChars)
            {
                var e = new KeyPressEventArgs(c);
                _control.TestDurationKeyPress(e);
                Assert.IsFalse(e.Handled, $"Should accept character: {c}");
            }

            foreach (var c in invalidChars)
            {
                var e = new KeyPressEventArgs(c);
                _control.TestDurationKeyPress(e);
                Assert.IsTrue(e.Handled, $"Should reject character: {c}");
            }
        }

        [TestMethod]
        public void TestDurationMaxLength_IsLimited()
        {
            Assert.AreEqual(5, _durationTextBox.MaxLength);
        }

        [TestMethod]
        public void TestApplySettings_UpdatesFilter()
        {
            // Arrange
            var filter = GetPrivateField<SmartGain>(_control, "Filter");
            _gainTextBox.Text = "-6.0";
            _durationTextBox.Text = "100";

            // Act
            _control.ApplySettings();

            // Assert
            Assert.AreEqual(-6.0, filter.GaindB);
            Assert.AreEqual(TimeSpan.FromMilliseconds(100), filter.Duration);
        }

        [TestMethod]
        public void TestPeakCheckBox_TogglesBehavior()
        {
            // Arrange
            var filter = GetPrivateField<SmartGain>(_control, "Filter");

            // Act - Check Peak
            _peakCheckBox.Checked = true;
            _control.TestPeakCheckedChanged();

            // Assert
            Assert.IsTrue(_peakCheckBox.Checked);
            Assert.IsFalse(_peakHoldCheckBox.Checked);
            Assert.IsFalse(filter.PeakHold);
            Assert.IsTrue(_durationTextBox.Enabled);

            // Act - Check PeakHold
            _peakHoldCheckBox.Checked = true;
            _control.TestPeakHoldCheckedChanged();

            // Assert
            Assert.IsFalse(_peakCheckBox.Checked);
            Assert.IsTrue(_peakHoldCheckBox.Checked);
            Assert.IsTrue(filter.PeakHold);
            Assert.IsFalse(_durationTextBox.Enabled);
        }

        [TestMethod]
        public void TestRefreshStats_UpdatesLabels()
        {
            // Arrange
            var filter = GetPrivateField<SmartGain>(_control, "Filter");
            
            // Set filter values through reflection
            typeof(SmartGain).GetProperty("ActualGaindB")?.SetValue(filter, -3.0);
            typeof(SmartGain).GetProperty("PeakLevelLinear")?.SetValue(filter, 0.5);
            typeof(SmartGain).GetProperty("HeadroomLinear")?.SetValue(filter, 0.707);

            // Act
            _control.TestRefreshStats();

            // Assert
            Assert.AreEqual("-03.0", _appliedGainLabel.Text);
            Assert.AreEqual("-06.0", _peakLevelLabel.Text); // ~-6dB for 0.5 linear
            Assert.AreEqual("-03.0", _headroomLabel.Text);  // ~-3dB for 0.707 linear
        }

        [TestMethod]
        public void TestSetDeepClonedFilter_UpdatesFilterAndUI()
        {
            // Arrange
            var newFilter = new SmartGain
            {
                GaindB = -3.0,
                Duration = TimeSpan.FromMilliseconds(200),
                PeakHold = true
            };

            // Act
            _control.SetDeepClonedFilter(newFilter);

            // Assert
            var currentFilter = GetPrivateField<SmartGain>(_control, "Filter");
            Assert.AreEqual(-3.0, currentFilter.GaindB);
            Assert.AreEqual(TimeSpan.FromMilliseconds(200), currentFilter.Duration);
            Assert.IsTrue(currentFilter.PeakHold);

            Assert.AreEqual("-3", _gainTextBox.Text);
            Assert.AreEqual("200", _durationTextBox.Text);
            Assert.IsTrue(_peakHoldCheckBox.Checked);
            Assert.IsFalse(_peakCheckBox.Checked);
        }

        [TestMethod]
        public void TestSetDeepClonedFilter_HandlesWrongType()
        {
            // Arrange
            var wrongFilter = new SmartGainTestFilter();
            var originalFilter = GetPrivateField<SmartGain>(_control, "Filter");
            var originalGain = _gainTextBox.Text;

            // Act
            _control.SetDeepClonedFilter(wrongFilter);

            // Assert
            Assert.AreEqual(originalFilter, GetPrivateField<SmartGain>(_control, "Filter"));
            Assert.AreEqual(originalGain, _gainTextBox.Text);
        }
    }

    public class TestableSmartGainControl : SmartGainControl
    {
        public void TestGainKeyPress(KeyPressEventArgs e) => TxtGain_KeyPress(null, e);
        public void TestDurationKeyPress(KeyPressEventArgs e) => TxtDuration_KeyPress(null, e);
        public void TestPeakCheckedChanged() => chkPeak_CheckedChanged(null, EventArgs.Empty);
        public void TestPeakHoldCheckedChanged() => chkPeakHold_CheckedChanged(null, EventArgs.Empty);
        public void TestRefreshStats() => RefreshStats_Timer_Tick(null, EventArgs.Empty);
    }

    internal class SmartGainTestFilter : IFilter
    {
        public bool FilterEnabled { get; set; }
        public FilterTypes FilterType { get; set; }
        public FilterProcessingTypes FilterProcessingType { get; set; }
        public IFilter GetFilter => this;
        public void ApplySettings() { }
        public IFilter DeepClone() => this;
        public double[] Transform(double[] input, DSP_Stream currentStream) => input;
        public void ResetSampleRate(int sampleRate) { }
    }
}