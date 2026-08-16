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
        private RichTextBox _tapsTextBox;
        private Button _applyButton;

        [TestInitialize]
        public void InitializeTest()
        {
            _control = new TestableFIRControl();
            _fftSizeTextBox = new TextBox();
            _tapsTextBox = new RichTextBox();
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
            _tapsTextBox.Lines = new[] { "1.0", "2.0", "3.0" };
            // Filter out any empty/whitespace lines (RichTextBox may append an empty line)
            _tapsTextBox.Lines = Array.FindAll(_tapsTextBox.Lines, line => !string.IsNullOrWhiteSpace(line));

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

            // Assert - Exception expected, so no further checks
        }

        [TestMethod]
        public void TestApplySettings_HandlesInvalidTaps()
        {
            // Arrange
            _tapsTextBox.Text = "1.0\ninvalid\n3.0";

            // Act
            _control.ApplySettings();
        }

        [TestMethod]
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

        // ---------------------------------------------------------------------------------
        // Regression coverage for the allocation pass: the taps split used to allocate a fresh
        // new[] { '\r', '\n' } separator array on every btnApply_Click. It now uses a static
        // readonly array. These tests pin that CR, LF and CRLF line endings (and blank lines)
        // still split identically, and that repeated applies are stable.
        // ---------------------------------------------------------------------------------

        [TestMethod]
        public void TestApplySettings_TapsSplit_HandlesCrLfLfAndCrLineEndings()
        {
            var filter = GetPrivateField<FIR>(_control, "Filter");

            _tapsTextBox.Text = "1.0\r\n2.0\n3.0\r4.0";
            _control.ApplySettings();

            Assert.IsNotNull(filter.Taps);
            Assert.AreEqual(4, filter.Taps.Length);
            Assert.AreEqual(1.0, filter.Taps[0]);
            Assert.AreEqual(2.0, filter.Taps[1]);
            Assert.AreEqual(3.0, filter.Taps[2]);
            Assert.AreEqual(4.0, filter.Taps[3]);
        }

        [TestMethod]
        public void TestApplySettings_TapsSplit_SkipsBlankLines()
        {
            var filter = GetPrivateField<FIR>(_control, "Filter");

            _tapsTextBox.Text = "\r\n1.0\r\n\r\n\r\n2.0\r\n";
            _control.ApplySettings();

            Assert.IsNotNull(filter.Taps);
            Assert.AreEqual(2, filter.Taps.Length);
            Assert.AreEqual(1.0, filter.Taps[0]);
            Assert.AreEqual(2.0, filter.Taps[1]);
        }

        [TestMethod]
        public void TestApplySettings_TapsSplit_RepeatedCallsAreStable()
        {
            // The separator array is now shared static state; repeated splits must not disturb it.
            var filter = GetPrivateField<FIR>(_control, "Filter");
            _tapsTextBox.Text = "1.5\r\n2.5\r\n3.5";

            _control.ApplySettings();
            _control.ApplySettings();
            _control.ApplySettings();

            Assert.IsNotNull(filter.Taps);
            Assert.AreEqual(3, filter.Taps.Length);
            Assert.AreEqual(1.5, filter.Taps[0]);
            Assert.AreEqual(2.5, filter.Taps[1]);
            Assert.AreEqual(3.5, filter.Taps[2]);
        }

        [TestMethod]
        public void TestApplySettings_TapsSplit_SharedSeparatorArrayIsUnmodified()
        {
            // Two independent control instances must both split correctly off the same
            // static separator array.
            var second = new TestableFIRControl();
            var secondTaps = new RichTextBox();
            SetPrivateField(second, "txtTaps", secondTaps);
            SetPrivateField(second, "txtFFTSize", new TextBox());

            _tapsTextBox.Text = "9.0\r\n8.0";
            _control.ApplySettings();

            secondTaps.Text = "1.0\r\n2.0\r\n3.0";
            second.ApplySettings();

            var firstFilter = GetPrivateField<FIR>(_control, "Filter");
            var secondFilter = GetPrivateField<FIR>(second, "Filter");

            Assert.IsNotNull(firstFilter.Taps);
            Assert.IsNotNull(secondFilter.Taps);
            Assert.AreEqual(2, firstFilter.Taps.Length);
            Assert.AreEqual(3, secondFilter.Taps.Length);
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