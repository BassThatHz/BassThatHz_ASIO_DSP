using Microsoft.VisualStudio.TestTools.UnitTesting;
using BassThatHz_ASIO_DSP_Processor.GUI.Controls;
using BassThatHz_ASIO_DSP_Processor;
using System;
using System.ComponentModel;
using System.Reflection;
using System.Windows.Forms;

namespace Test_Project_1
{
    [TestClass]
    public class Test_FloorControl
    {
        private TestableFloorControl _control;
        private FloorMockVolume _thresholdControl;
        private TextBox _holdInMSTextBox;
        private TextBox _ratioTextBox;
        private Button _applyButton;

        [TestInitialize]
        public void InitializeTest()
        {
            _control = new TestableFloorControl();
            _thresholdControl = new FloorMockVolume();
            _holdInMSTextBox = new TextBox();
            _ratioTextBox = new TextBox();
            _applyButton = new Button();

            // Set up test controls
            SetPrivateField(_control, "Threshold", _thresholdControl);
            SetPrivateField(_control, "txtHoldInMS", _holdInMSTextBox);
            SetPrivateField(_control, "txtRatio", _ratioTextBox);
            SetPrivateField(_control, "btnApply", _applyButton);

            // Initialize event handlers
            _control.MapEventHandlers();
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
        public void FloorControl_Constructor_Initializes()
        {
            var control = new FloorControl();
            Assert.IsNotNull(control);
            Assert.IsInstanceOfType(control, typeof(UserControl));
        }

        [TestMethod]
        public void FloorControl_DefaultProperties_AreValid()
        {
            var control = new FloorControl();
            Assert.IsTrue(control.Enabled);
        }

        [TestMethod]
        public void TestInitialize_CreatesNewFilter()
        {
            var filter = GetPrivateField<Floor>(_control, "Filter");
            Assert.IsNotNull(filter);
            Assert.AreEqual(FilterTypes.Floor, filter.FilterType);
        }

        //[TestMethod]
        //public void TestThresholdChange_UpdatesFilter()
        //{
        //    // Arrange
        //    var filter = GetPrivateField<Floor>(_control, "Filter");
        //    double newValue = 0.5;

        //    // Act
        //    _thresholdControl.SetVolume(newValue);
        //    _thresholdControl.RaiseVolumeChanged();

        //    // Assert
        //    Assert.AreEqual(newValue, filter.MinValue);
        //}

        [TestMethod]
        public void TestApplySettings_UpdatesAllSettings()
        {
            // Arrange
            var filter = GetPrivateField<Floor>(_control, "Filter");
            _holdInMSTextBox.Text = "100";
            _ratioTextBox.Text = "2";
            _thresholdControl.SetVolume(0.5);

            // Act
            _control.ApplySettings();

            // Assert
            Assert.AreEqual(TimeSpan.FromMilliseconds(100), filter.HoldInMS);
            Assert.AreEqual(2.0, filter.Ratio);
            Assert.AreEqual(0.5, filter.MinValue);
        }

        [TestMethod]
        public void TestApplySettings_ClampsRatioBelow1()
        {
            // Arrange
            var filter = GetPrivateField<Floor>(_control, "Filter");
            _ratioTextBox.Text = "0.5";

            // Act
            _control.ApplySettings();

            // Assert
            Assert.AreEqual(1.0, filter.Ratio);
            Assert.AreEqual("1", _ratioTextBox.Text);
        }

        [TestMethod]
        public void TestInputValidation_HoldInMS()
        {
            var validChars = new[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '\b' };
            var invalidChars = new[] { 'a', 'b', 'c', '.', '-', ' ' };

            foreach (var c in validChars)
            {
                var e = new KeyPressEventArgs(c);
                _control.TestHoldInMSKeyPress(e);
                Assert.IsFalse(e.Handled, $"Should accept character: {c}");
            }

            foreach (var c in invalidChars)
            {
                var e = new KeyPressEventArgs(c);
                _control.TestHoldInMSKeyPress(e);
                Assert.IsTrue(e.Handled, $"Should reject character: {c}");
            }
        }

        [TestMethod]
        public void TestInputValidation_Ratio()
        {
            var validChars = new[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '\b' };
            var invalidChars = new[] { 'a', 'b', 'c', '.', '-', ' ' };

            foreach (var c in validChars)
            {
                var e = new KeyPressEventArgs(c);
                _control.TestRatioKeyPress(e);
                Assert.IsFalse(e.Handled, $"Should accept character: {c}");
            }

            foreach (var c in invalidChars)
            {
                var e = new KeyPressEventArgs(c);
                _control.TestRatioKeyPress(e);
                Assert.IsTrue(e.Handled, $"Should reject character: {c}");
            }
        }

        [TestMethod]
        public void TestInputMaxLength()
        {
            Assert.AreEqual(5, _holdInMSTextBox.MaxLength, "Hold in MS text box should have max length of 5");
            Assert.AreEqual(3, _ratioTextBox.MaxLength, "Ratio text box should have max length of 3");
        }

        [TestMethod]
        public void TestGetFilter_ReturnsFilterInstance()
        {
            var filter = _control.GetFilter;
            Assert.IsNotNull(filter);
            Assert.IsInstanceOfType(filter, typeof(Floor));
        }

        [TestMethod]
        public void TestSetDeepClonedFilter_UpdatesFilterAndUI()
        {
            // Arrange
            var newFilter = new Floor
            {
                MinValue = 0.75,
                HoldInMS = TimeSpan.FromMilliseconds(200),
                Ratio = 3.0
            };

            // Act
            _control.SetDeepClonedFilter(newFilter);

            // Assert
            var currentFilter = GetPrivateField<Floor>(_control, "Filter");
            Assert.AreEqual(0.75, currentFilter.MinValue);
            Assert.AreEqual(TimeSpan.FromMilliseconds(200), currentFilter.HoldInMS);
            Assert.AreEqual(3.0, currentFilter.Ratio);

            Assert.AreEqual(0.75, _thresholdControl.GetVolume());
            Assert.AreEqual("200", _holdInMSTextBox.Text);
            Assert.AreEqual("3", _ratioTextBox.Text);
        }

        [TestMethod]
        public void TestSetDeepClonedFilter_HandlesWrongType()
        {
            // Arrange
            var wrongFilter = CreateMockFilter();
            var originalFilter = GetPrivateField<Floor>(_control, "Filter");
            var originalHoldInMS = _holdInMSTextBox.Text;

            // Act
            _control.SetDeepClonedFilter(wrongFilter);

            // Assert - Nothing should change
            Assert.AreEqual(originalFilter, GetPrivateField<Floor>(_control, "Filter"));
            Assert.AreEqual(originalHoldInMS, _holdInMSTextBox.Text);
        }

        private IFilter CreateMockFilter()
        {
            return new FloorMockFilter
            {
                FilterEnabled = true,
                FilterType = FilterTypes.FIR,
                FilterProcessingType = FilterProcessingTypes.WholeBlock
            };
        }
    }

    public class TestableFloorControl : FloorControl
    {
        public new void MapEventHandlers() => base.MapEventHandlers();
        public void TestHoldInMSKeyPress(KeyPressEventArgs e) => TxtHoldInMS_KeyPress(null, e);
        public void TestRatioKeyPress(KeyPressEventArgs e) => txtRatio_KeyPress(null, e);
    }

    [DesignerCategory("Code")]
    public class FloorMockVolume : UserControl
    {
        private double _volumeValue;
        public event EventHandler VolumeChanged;

        public void SetVolume(double value) => _volumeValue = value;
        public double GetVolume() => _volumeValue;

        public void RaiseVolumeChanged()
        {
            VolumeChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    internal class FloorMockFilter : IFilter
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