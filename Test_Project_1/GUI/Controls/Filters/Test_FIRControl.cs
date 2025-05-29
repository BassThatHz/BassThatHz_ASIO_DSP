using BassThatHz_ASIO_DSP_Processor;
using BassThatHz_ASIO_DSP_Processor.GUI.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Reflection;
using System.Windows.Forms;

namespace Test_Project_1
{
    [TestClass]
    public class Test_FIRControl
    {
        private TestableFIRControl _control;
        private TextBox _fftSizeTextBox;
        private TextBox _tapsTextBox;
        private Button _applyButton;

        [TestInitialize]
        public void InitializeTest()
        {
            _control = new TestableFIRControl();
            _fftSizeTextBox = new TextBox();
            _tapsTextBox = new TextBox();
            _applyButton = new Button();

            // Set up test controls
            SetPrivateField(_control, "txtFFTSize", _fftSizeTextBox);
            SetPrivateField(_control, "txtTaps", _tapsTextBox);
            SetPrivateField(_control, "btnApply", _applyButton);
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
            var filter = GetPrivateField<FIR>(_control, "Filter");
            Assert.IsNotNull(filter);
            Assert.AreEqual(FilterTypes.FIR, filter.FilterType);
        }

        [TestMethod]
        public void TestApplySettings_UpdatesFFTSize()
        {
            // Arrange
            var filter = GetPrivateField<FIR>(_control, "Filter");
            _fftSizeTextBox.Text = "4096";

            // Act
            _control.ApplySettings();

            // Assert
            Assert.AreEqual(4096, filter.FFTSize);
        }

        [TestMethod]
        public void TestApplySettings_UpdatesTaps()
        {
            // Arrange
            var filter = GetPrivateField<FIR>(_control, "Filter");
            _tapsTextBox.Text = "1.0\n2.0\n3.0";

            // Act
            _control.ApplySettings();

            // Assert
            Assert.IsNotNull(filter.Taps);
            Assert.AreEqual(3, filter.Taps.Length);
            Assert.AreEqual(1.0, filter.Taps[0]);
            Assert.AreEqual(2.0, filter.Taps[1]);
            Assert.AreEqual(3.0, filter.Taps[2]);
        }

        [TestMethod]
        public void TestApplySettings_HandlesEmptyTaps()
        {
            // Arrange
            var filter = GetPrivateField<FIR>(_control, "Filter");
            filter.SetTaps(new double[] { 1.0, 2.0 });
            _tapsTextBox.Text = "";

            // Act
            _control.ApplySettings();

            // Assert - Original taps should remain
            Assert.IsNotNull(filter.Taps);
            Assert.AreEqual(2, filter.Taps.Length);
        }

        [TestMethod]
        [ExpectedException(typeof(FormatException))]
        public void TestApplySettings_HandlesInvalidTaps()
        {
            // Arrange
            _tapsTextBox.Text = "1.0\ninvalid\n3.0";

            // Act
            _control.ApplySettings();
        }

        [TestMethod]
        [ExpectedException(typeof(FormatException))]
        public void TestApplySettings_HandlesInvalidFFTSize()
        {
            // Arrange
            _fftSizeTextBox.Text = "invalid";

            // Act
            _control.ApplySettings();
        }

        [TestMethod]
        public void TestGetFilter_ReturnsFilterInstance()
        {
            var filter = _control.GetFilter;
            Assert.IsNotNull(filter);
            Assert.IsInstanceOfType(filter, typeof(FIR));
        }

        [TestMethod]
        public void TestSetDeepClonedFilter_UpdatesFilterAndUI()
        {
            // Arrange
            var newFilter = new FIR
            {
                FFTSize = 8192,
            };
            newFilter.SetTaps(new double[] { 1.0, 2.0, 3.0 });

            // Act
            _control.SetDeepClonedFilter(newFilter);

            // Assert
            var currentFilter = GetPrivateField<FIR>(_control, "Filter");
            Assert.AreEqual(8192, currentFilter.FFTSize);
            Assert.IsNotNull(currentFilter.Taps);
            Assert.AreEqual(3, currentFilter.Taps.Length);
            Assert.AreEqual(1.0, currentFilter.Taps[0]);
            Assert.AreEqual(2.0, currentFilter.Taps[1]);
            Assert.AreEqual(3.0, currentFilter.Taps[2]);

            Assert.AreEqual("8192", _fftSizeTextBox.Text);
            Assert.AreEqual("1\n2\n3", _tapsTextBox.Text.Trim());
        }

        [TestMethod]
        public void TestSetDeepClonedFilter_HandlesWrongType()
        {
            // Arrange
            var wrongFilter = CreateMockFilter();
            var originalFilter = GetPrivateField<FIR>(_control, "Filter");
            var originalFFTSize = _fftSizeTextBox.Text;

            // Act
            _control.SetDeepClonedFilter(wrongFilter);

            // Assert - Nothing should change
            Assert.AreEqual(originalFilter, GetPrivateField<FIR>(_control, "Filter"));
            Assert.AreEqual(originalFFTSize, _fftSizeTextBox.Text);
        }

        [TestMethod]
        public void TestFilterEnabled_PreservedDuringUpdate()
        {
            // Arrange
            var filter = GetPrivateField<FIR>(_control, "Filter");
            filter.FilterEnabled = true;
            _fftSizeTextBox.Text = "4096";

            // Act
            _control.ApplySettings();

            // Assert - FilterEnabled should be restored
            Assert.IsTrue(filter.FilterEnabled);
        }

        private IFilter CreateMockFilter()
        {
            var filter = new Mock_TestFilter();
            return filter;
        }
    }

    public class TestableFIRControl : FIRControl
    {
        public new void Error(Exception ex) => base.Error(ex);
    }

    internal class Mock_TestFilter : IFilter
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