using Microsoft.VisualStudio.TestTools.UnitTesting;
using BassThatHz_ASIO_DSP_Processor.GUI.Controls;
using BassThatHz_ASIO_DSP_Processor;
using System;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Test_Project_1
{
    [TestClass]
    public class Test_ULF_FIRControl
    {
        private TestableULF_FIRControl _control;
        private TextBox _fftSizeTextBox;
        private TextBox _tapsTextBox;
        private TextBox _tapsSampleRateTextBox;
        private ComboBox _tapsSampleRateComboBox;
        private Button _applyButton;

        [TestInitialize]
        public void InitializeTest()
        {
            // Set up DSP info mock
            var dspInfo = new DSP_Info { InSampleRate = 48000 };
            typeof(Program).GetField("DSP_Info", BindingFlags.Static | BindingFlags.Public)?.SetValue(null, dspInfo);

            _control = new TestableULF_FIRControl();
            _fftSizeTextBox = new TextBox();
            _tapsTextBox = new TextBox();
            _tapsSampleRateTextBox = new TextBox();
            _tapsSampleRateComboBox = new ComboBox();
            _applyButton = new Button();

            // Set up test controls
            SetPrivateField(_control, "txtFFTSize", _fftSizeTextBox);
            SetPrivateField(_control, "txtTaps", _tapsTextBox);
            SetPrivateField(_control, "txtTapsSampleRate", _tapsSampleRateTextBox);
            SetPrivateField(_control, "comboTapsSampleRate", _tapsSampleRateComboBox);
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
            var filter = GetPrivateField<ULF_FIR>(_control, "Filter");
            Assert.IsNotNull(filter);
            Assert.AreEqual(FilterTypes.ULF_FIR, filter.FilterType);
        }

        [TestMethod]
        public void TestInitialize_SetsSampleRateDefaults()
        {
            // Default sample rate is 48000 from test setup
            Assert.AreEqual(1, _tapsSampleRateComboBox.SelectedIndex);
            Assert.AreEqual("480", _tapsSampleRateTextBox.Text); // 48000/100
        }

        [TestMethod]
        public void TestSampleRateChange_UpdatesFilter()
        {
            // Arrange
            var filter = GetPrivateField<ULF_FIR>(_control, "Filter");
            _tapsSampleRateComboBox.SelectedIndex = 1; // /100 mode
            _fftSizeTextBox.Text = "8192";

            // Act
            Program.DSP_Info.InSampleRate = 96000;
            _control.TestSampleRateChanged(96000);

            // Assert
            Assert.AreEqual(960, filter.TapsSampleRate); // 96000/100
            Assert.AreEqual(8192, filter.FFTSize);
        }

        [TestMethod]
        public void TestApplySettings_UpdatesFFTSize()
        {
            // Arrange
            var filter = GetPrivateField<ULF_FIR>(_control, "Filter");
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
            var filter = GetPrivateField<ULF_FIR>(_control, "Filter");
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
        public void TestTapsSampleRate_Divider10()
        {
            // Arrange
            var filter = GetPrivateField<ULF_FIR>(_control, "Filter");
            _tapsSampleRateComboBox.SelectedIndex = 0; // /10 mode

            // Act
            _control.ApplySettings();

            // Assert
            Assert.AreEqual(4800, filter.TapsSampleRate); // 48000/10
            Assert.AreEqual("4800", _tapsSampleRateTextBox.Text);
        }

        [TestMethod]
        public void TestTapsSampleRate_Divider100()
        {
            // Arrange
            var filter = GetPrivateField<ULF_FIR>(_control, "Filter");
            _tapsSampleRateComboBox.SelectedIndex = 1; // /100 mode

            // Act
            _control.ApplySettings();

            // Assert
            Assert.AreEqual(480, filter.TapsSampleRate); // 48000/100
            Assert.AreEqual("480", _tapsSampleRateTextBox.Text);
        }

        [TestMethod]
        public void TestTapsSampleRate_Divider1000()
        {
            // Arrange
            var filter = GetPrivateField<ULF_FIR>(_control, "Filter");
            _tapsSampleRateComboBox.SelectedIndex = 2; // /1000 mode

            // Act
            _control.ApplySettings();

            // Assert
            Assert.AreEqual(48, filter.TapsSampleRate); // 48000/1000
            Assert.AreEqual("48", _tapsSampleRateTextBox.Text);
        }

        [TestMethod]
        public void TestTapsSampleRate_Custom()
        {
            // Arrange
            var filter = GetPrivateField<ULF_FIR>(_control, "Filter");
            _tapsSampleRateComboBox.SelectedIndex = 3; // Custom mode
            _tapsSampleRateTextBox.Text = "200";

            // Act
            _control.ApplySettings();

            // Assert
            Assert.AreEqual(200, filter.TapsSampleRate);
            Assert.AreEqual("200", _tapsSampleRateTextBox.Text);
        }

        [TestMethod]
        public void TestSetDeepClonedFilter_UpdatesFilterAndUI()
        {
            // Arrange
            var newFilter = new ULF_FIR
            {
                FFTSize = 4096,
                TapsSampleRateIndex = 2,
                TapsSampleRate = 48
            };
            var taps = new double[] { 1.0, 2.0, 3.0 };
            newFilter.SetTaps(taps);

            // Act
            _control.SetDeepClonedFilter(newFilter);

            // Assert
            var currentFilter = GetPrivateField<ULF_FIR>(_control, "Filter");
            Assert.AreEqual(4096, currentFilter.FFTSize);
            Assert.AreEqual(2, currentFilter.TapsSampleRateIndex);
            Assert.AreEqual(48, currentFilter.TapsSampleRate);
            Assert.IsNotNull(currentFilter.Taps);
            Assert.AreEqual(3, currentFilter.Taps.Length);

            Assert.AreEqual("4096", _fftSizeTextBox.Text);
            Assert.AreEqual(2, _tapsSampleRateComboBox.SelectedIndex);
            Assert.AreEqual("48", _tapsSampleRateTextBox.Text);
            Assert.AreEqual("1\n2\n3", _tapsTextBox.Text.Trim());
        }

        [TestMethod]
        public void TestSetDeepClonedFilter_HandlesWrongType()
        {
            // Arrange
            var wrongFilter = new ULF_FIRTestFilter();
            var originalFilter = GetPrivateField<ULF_FIR>(_control, "Filter");
            var originalFFTSize = _fftSizeTextBox.Text;

            // Act
            _control.SetDeepClonedFilter(wrongFilter);

            // Assert
            Assert.AreEqual(originalFilter, GetPrivateField<ULF_FIR>(_control, "Filter"));
            Assert.AreEqual(originalFFTSize, _fftSizeTextBox.Text);
        }

        [TestMethod]
        public void TestFilterEnabled_PreservedDuringUpdate()
        {
            // Arrange
            var filter = GetPrivateField<ULF_FIR>(_control, "Filter");
            filter.FilterEnabled = true;
            _fftSizeTextBox.Text = "4096";

            // Act
            _control.ApplySettings();

            // Assert
            Assert.IsTrue(filter.FilterEnabled);
        }
    }

    public class TestableULF_FIRControl : ULF_FIRControl
    {
        public void TestSampleRateChanged(int sampleRate) => SampleRateChangeNotifier_SampleRateChanged(sampleRate);
    }

    internal class ULF_FIRTestFilter : IFilter
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