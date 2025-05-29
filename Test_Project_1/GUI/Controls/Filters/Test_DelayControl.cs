using BassThatHz_ASIO_DSP_Processor;
using BassThatHz_ASIO_DSP_Processor.GUI.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Reflection;
using System.Windows.Forms;

namespace Test_Project_1
{
    [TestClass]
    public class Test_DelayControl
    {
        private TestableDelayControl _control;
        private TextBox _delayTextBox;
        private Button _applyButton;

        [TestInitialize]
        public void InitializeTest()
        {
            _control = new TestableDelayControl();
            _delayTextBox = new TextBox();
            _applyButton = new Button();

            // Set up test controls
            SetPrivateField(_control, "txtDelay", _delayTextBox);
            SetPrivateField(_control, "btnApply", _applyButton);

            // Set up mock ASIO
            var asioField = typeof(Program).GetField("ASIO", BindingFlags.Static | BindingFlags.Public);
            var mockAsio = new ASIO_Engine();
            typeof(ASIO_Engine).GetProperty("SamplesPerChannel")?.SetValue(mockAsio, 512);
            asioField?.SetValue(null, mockAsio);

            // Set up DSP Info
            var dspInfoField = typeof(Program).GetField("DSP_Info", BindingFlags.Static | BindingFlags.Public);
            var dspInfo = new DSP_Info { InSampleRate = 44100 };
            dspInfoField?.SetValue(null, dspInfo);

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
        public void TestInitialize_CreatesNewFilter()
        {
            var filter = GetPrivateField<Delay>(_control, "Filter");
            Assert.IsNotNull(filter);
            Assert.AreEqual(FilterTypes.Delay, filter.FilterType);
        }

        [TestMethod]
        public void TestMapEventHandlers_ConnectsEvents()
        {
            // Verify KeyPress event is connected
            var e = new KeyPressEventArgs('a');
            _control.TestKeyPress(e);
            Assert.IsTrue(e.Handled, "KeyPress event should be handled for non-numeric input");

            // Verify MaxLength is set
            Assert.AreEqual(5, _delayTextBox.MaxLength);

            // Verify sample rate change handler is connected
            _control.RaiseSampleRateChanged(48000);
            var filter = GetPrivateField<Delay>(_control, "Filter");
            Assert.AreEqual(48000, GetPrivateField<int>(filter, "SampleRate"));
        }

        [TestMethod]
        public void TestApplySettings_UpdatesFilter()
        {
            // Arrange
            _delayTextBox.Text = "10";

            // Act
            _control.ApplySettings();

            // Assert
            var filter = GetPrivateField<Delay>(_control, "Filter");
            Assert.AreEqual(10m, filter.DelayInMS);
        }

        [TestMethod]
        public void TestValidateKeyPress()
        {
            var validChars = new[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '\b' };
            var invalidChars = new[] { 'a', 'b', 'c', '.', '-', ' ' };

            foreach (var c in validChars)
            {
                var e = new KeyPressEventArgs(c);
                _control.TestKeyPress(e);
                Assert.IsFalse(e.Handled, $"Should accept character: {c}");
            }

            foreach (var c in invalidChars)
            {
                var e = new KeyPressEventArgs(c);
                _control.TestKeyPress(e);
                Assert.IsTrue(e.Handled, $"Should reject character: {c}");
            }
        }

        [TestMethod]
        public void TestGetFilter_ReturnsFilterInstance()
        {
            var filter = _control.GetFilter;
            Assert.IsNotNull(filter);
            Assert.IsInstanceOfType(filter, typeof(Delay));
        }

        [TestMethod]
        public void TestSetDeepClonedFilter_UpdatesFilterAndUI()
        {
            // Arrange
            var newFilter = new Delay { DelayInMS = 20m };

            // Act
            _control.SetDeepClonedFilter(newFilter);

            // Assert
            var currentFilter = GetPrivateField<Delay>(_control, "Filter");
            Assert.AreEqual(20m, currentFilter.DelayInMS);
            Assert.AreEqual("20", _delayTextBox.Text);
        }

        [TestMethod]
        public void TestSetDeepClonedFilter_HandlesWrongType()
        {
            // Arrange
            var wrongFilter = new MockFilter();
            var originalFilter = GetPrivateField<Delay>(_control, "Filter");
            var originalText = _delayTextBox.Text;

            // Act
            _control.SetDeepClonedFilter(wrongFilter);

            // Assert - Nothing should change
            Assert.AreEqual(originalFilter, GetPrivateField<Delay>(_control, "Filter"));
            Assert.AreEqual(originalText, _delayTextBox.Text);
        }

        [TestMethod]
        public void TestApplyButton_HandlesInvalidInput()
        {
            // Arrange
            _delayTextBox.Text = "invalid";
            var originalFilter = GetPrivateField<Delay>(_control, "Filter").DelayInMS;

            // Act
            _applyButton.PerformClick();

            // Assert - Delay should not change
            var filter = GetPrivateField<Delay>(_control, "Filter");
            Assert.AreEqual(originalFilter, filter.DelayInMS);
        }

        [TestMethod]
        public void TestFilter_Implements_IFilterInterface()
        {
            var filter = _control.GetFilter;
            Assert.IsInstanceOfType(_control, typeof(IFilterControl));
            Assert.IsNotNull(filter);
            Assert.AreEqual(FilterTypes.Delay, filter.FilterType);
            Assert.AreEqual(FilterProcessingTypes.WholeBlock, filter.FilterProcessingType);
        }

        [TestMethod]
        public void TestSampleRateChange_UpdatesFilterAndReappliesSettings()
        {
            // Arrange
            _delayTextBox.Text = "10";
            _control.ApplySettings();
            var filter = GetPrivateField<Delay>(_control, "Filter");

            // Act
            _control.RaiseSampleRateChanged(96000);

            // Assert
            Assert.AreEqual(96000, GetPrivateField<int>(filter, "SampleRate"));
            Assert.AreEqual(10m, filter.DelayInMS); // Settings should be preserved
        }
    }

    public class TestableDelayControl : DelayControl
    {
        public new void MapEventHandlers() => base.MapEventHandlers();
        public void RaiseSampleRateChanged(int sampleRate) => SampleRateChangeNotifier_SampleRateChanged(sampleRate);
        public void TestKeyPress(KeyPressEventArgs e) => TxtDelay_KeyPress(null, e);
    }

    public class MockFilter : IFilter
    {
        public bool FilterEnabled { get; set; }
        public FilterTypes FilterType { get; }
        public FilterProcessingTypes FilterProcessingType { get; }
        public IFilter GetFilter => this;
        public void ApplySettings() { }
        public IFilter DeepClone() => this;
        public double[] Transform(double[] input, DSP_Stream currentStream) => input;
        public void ResetSampleRate(int sampleRate) { }
    }
}