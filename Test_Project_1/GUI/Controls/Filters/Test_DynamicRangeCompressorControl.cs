using BassThatHz_ASIO_DSP_Processor;
using BassThatHz_ASIO_DSP_Processor.GUI.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.ComponentModel;
using System.Reflection;
using System.Windows.Forms;

namespace Test_Project_1
{
    [TestClass]
    public class Test_DynamicRangeCompressorControl
    {
        private TestableDynamicRangeCompressorControl _control;
        private TestVolumeControl _thresholdControl;
        private TextBox _attackTimeTextBox;
        private TextBox _releaseTimeTextBox;
        private TextBox _compressionRatioTextBox;
        private TextBox _kneeWidthTextBox;
        private CheckBox _softKneeCheckBox;
        private TestVolumeControl _compressionApplied;

        [TestInitialize]
        public void InitializeTest()
        {
            _control = new TestableDynamicRangeCompressorControl();
            _thresholdControl = new TestVolumeControl();
            _attackTimeTextBox = new TextBox();
            _releaseTimeTextBox = new TextBox();
            _compressionRatioTextBox = new TextBox();
            _kneeWidthTextBox = new TextBox();
            _softKneeCheckBox = new CheckBox();
            _compressionApplied = new TestVolumeControl();

            // Set up test controls
            SetPrivateField(_control, "Threshold", _thresholdControl);
            SetPrivateField(_control, "msb_AttackTime_ms", _attackTimeTextBox);
            SetPrivateField(_control, "msb_ReleaseTime_ms", _releaseTimeTextBox);
            SetPrivateField(_control, "msb_CompressionRatio", _compressionRatioTextBox);
            SetPrivateField(_control, "msb_KneeWidth_db", _kneeWidthTextBox);
            SetPrivateField(_control, "chkSoftKnee", _softKneeCheckBox);
            SetPrivateField(_control, "CompressionApplied", _compressionApplied);

            // Set up mock ASIO
            var asioField = typeof(Program).GetField("ASIO", BindingFlags.Static | BindingFlags.Public);
            var mockAsio = new ASIO_Engine();
            typeof(ASIO_Engine).GetProperty("SampleRate_Current")?.SetValue(mockAsio, 44100);
            asioField?.SetValue(null, mockAsio);

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
            var filter = GetPrivateField<DynamicRangeCompressor>(_control, "Filter");
            Assert.IsNotNull(filter);
            Assert.AreEqual(FilterTypes.DynamicRangeCompressor, filter.FilterType);
        }

        //[TestMethod]
        //public void TestThresholdChange_UpdatesFilter()
        //{
        //    // Arrange
        //    var filter = GetPrivateField<DynamicRangeCompressor>(_control, "Filter");
        //    double newThreshold = -20.0;

        //    // Act
        //    _thresholdControl.SetVolumeDb(newThreshold);
        //    _thresholdControl.RaiseVolumeChanged();

        //    // Assert
        //    Assert.AreEqual(newThreshold, filter.Threshold_dB);
        //}

        [TestMethod]
        public void TestApplySettings_UpdatesAllSettings()
        {
            // Arrange
            var filter = GetPrivateField<DynamicRangeCompressor>(_control, "Filter");
            _thresholdControl.SetVolumeDb(-20.0);
            _attackTimeTextBox.Text = "10";
            _releaseTimeTextBox.Text = "100";
            _compressionRatioTextBox.Text = "20";
            _kneeWidthTextBox.Text = "3";
            _softKneeCheckBox.Checked = true;

            // Act
            _control.ApplySettings();

            // Assert
            Assert.AreEqual(-20.0, filter.Threshold_dB);
            Assert.AreEqual(10.0, filter.AttackTime_ms);
            Assert.AreEqual(100.0, filter.ReleaseTime_ms);
            Assert.AreEqual(20.0, filter.Ratio);
            Assert.AreEqual(3.0, filter.KneeWidth_dB);
            Assert.IsTrue(filter.UseSoftKnee);
        }

        [TestMethod]
        public void TestApplySettings_ClampsValuesBelow1()
        {
            // Arrange
            _attackTimeTextBox.Text = "0.5";
            _releaseTimeTextBox.Text = "0.1";
            _kneeWidthTextBox.Text = "0.8";
            _compressionRatioTextBox.Text = "5";

            // Act
            _control.ApplySettings();

            // Assert
            var filter = GetPrivateField<DynamicRangeCompressor>(_control, "Filter");
            Assert.AreEqual(1.0, filter.AttackTime_ms);
            Assert.AreEqual(1.0, filter.ReleaseTime_ms);
            Assert.AreEqual(1.0, filter.KneeWidth_dB);
            Assert.AreEqual(11.0, filter.Ratio);  // Minimum ratio is 11
            Assert.AreEqual("1", _attackTimeTextBox.Text);
            Assert.AreEqual("1", _releaseTimeTextBox.Text);
            Assert.AreEqual("1", _kneeWidthTextBox.Text);
            Assert.AreEqual("11", _compressionRatioTextBox.Text);
        }

        [TestMethod]
        public void TestGetFilter_ReturnsFilterInstance()
        {
            var filter = _control.GetFilter;
            Assert.IsNotNull(filter);
            Assert.IsInstanceOfType(filter, typeof(DynamicRangeCompressor));
        }

        [TestMethod]
        public void TestSetDeepClonedFilter_UpdatesFilterAndUI()
        {
            // Arrange
            var newFilter = new DynamicRangeCompressor
            {
                Threshold_dB = -12.0,
                AttackTime_ms = 15.0,
                ReleaseTime_ms = 150.0,
                Ratio = 20.0,
                KneeWidth_dB = 6.0,
                UseSoftKnee = true
            };

            // Act
            _control.SetDeepClonedFilter(newFilter);

            // Assert
            var currentFilter = GetPrivateField<DynamicRangeCompressor>(_control, "Filter");
            Assert.AreEqual(-12.0, currentFilter.Threshold_dB);
            Assert.AreEqual(15.0, currentFilter.AttackTime_ms);
            Assert.AreEqual(150.0, currentFilter.ReleaseTime_ms);
            Assert.AreEqual(20.0, currentFilter.Ratio);
            Assert.AreEqual(6.0, currentFilter.KneeWidth_dB);
            Assert.IsTrue(currentFilter.UseSoftKnee);
            
            Assert.AreEqual(-12.0, _thresholdControl.GetVolumeDb());
            Assert.AreEqual("15", _attackTimeTextBox.Text);
            Assert.AreEqual("150", _releaseTimeTextBox.Text);
            Assert.AreEqual("20", _compressionRatioTextBox.Text);
            Assert.AreEqual("6", _kneeWidthTextBox.Text);
            Assert.IsTrue(_softKneeCheckBox.Checked);
        }

        [TestMethod]
        public void TestSetDeepClonedFilter_HandlesWrongType()
        {
            // Arrange
            var wrongFilter = new TestFilter();
            var originalFilter = GetPrivateField<DynamicRangeCompressor>(_control, "Filter");
            var originalThreshold = _thresholdControl.GetVolumeDb();

            // Act
            _control.SetDeepClonedFilter(wrongFilter);

            // Assert - Nothing should change
            Assert.AreEqual(originalFilter, GetPrivateField<DynamicRangeCompressor>(_control, "Filter"));
            Assert.AreEqual(originalThreshold, _thresholdControl.GetVolumeDb());
        }

        [TestMethod]
        public void TestSoftKneeChange_UpdatesFilter()
        {
            // Arrange
            var filter = GetPrivateField<DynamicRangeCompressor>(_control, "Filter");
            
            // Act
            _softKneeCheckBox.Checked = true;
            _control.TestSoftKneeChanged();

            // Assert
            Assert.IsTrue(filter.UseSoftKnee);
        }

        [TestMethod]
        public void TestRefreshTimer_UpdatesCompressionDisplay()
        {
            // Arrange
            var filter = GetPrivateField<DynamicRangeCompressor>(_control, "Filter");
            double compressionValue = 0.5;
            typeof(DynamicRangeCompressor).GetProperty("CompressionApplied")?.SetValue(filter, compressionValue);

            // Act
            _control.TestRefreshTimer();

            // Assert
            Assert.AreEqual(compressionValue, _compressionApplied.GetVolume());
        }

        [TestMethod]
        public void TestSampleRateChange_UpdatesFilter()
        {
            // Arrange
            int newSampleRate = 96000;

            // Act
            _control.RaiseSampleRateChanged(newSampleRate);

            // Assert
            // The filter should have received the new sample rate for coefficient calculation
            // We can verify this through the state of the filter if it exposes this information,
            // or through the effects of the calculation
        }
    }

    public class TestableDynamicRangeCompressorControl : DynamicRangeCompressorControl
    {
        public new void MapEventHandlers() => base.MapEventHandlers();
        public void RaiseSampleRateChanged(int sampleRate) => SampleRateChangeNotifier_SampleRateChanged(sampleRate);
        public void TestSoftKneeChanged() => chkSoftKnee_CheckedChanged(chkSoftKnee, EventArgs.Empty);
        public void TestRefreshTimer() => RefreshTimer_Tick(null, EventArgs.Empty);
    }

    public class TestVolumeControl : UserControl
    {
        private double volume;
        private double volumeDb;
        public event EventHandler VolumeChanged;

        public double GetVolume() => volume;
        public double GetVolumeDb() => volumeDb;
        public void SetVolume(double value) => volume = value;
        public void SetVolumeDb(double value) => volumeDb = value;

        public void RaiseVolumeChanged()
        {
            VolumeChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public class TestFilter : IFilter
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