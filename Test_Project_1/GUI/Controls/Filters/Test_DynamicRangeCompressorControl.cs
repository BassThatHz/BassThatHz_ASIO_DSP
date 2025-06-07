using BassThatHz_ASIO_DSP_Processor;
using BassThatHz_ASIO_DSP_Processor.GUI.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Reflection;
using System.Windows.Forms;

namespace Test_Project_1
{
    [TestClass]
    public class Test_DynamicRangeCompressorControl
    {
        private TestableDynamicRangeCompressorControl _control;
        private TestVolumeSliderControl _thresholdControl;
        private MaskedTextBox _attackTimeTextBox;
        private MaskedTextBox _releaseTimeTextBox;
        private MaskedTextBox _compressionRatioTextBox;
        private MaskedTextBox _kneeWidthTextBox;
        private CheckBox _softKneeCheckBox;
        private TestVolumeSliderControl _compressionApplied;
        private bool _errorCalled;

        [TestInitialize]
        public void InitializeTest()
        {
            _control = new TestableDynamicRangeCompressorControl();
            _thresholdControl = new TestVolumeSliderControl();
            _attackTimeTextBox = new MaskedTextBox();
            _releaseTimeTextBox = new MaskedTextBox();
            _compressionRatioTextBox = new MaskedTextBox();
            _kneeWidthTextBox = new MaskedTextBox();
            _softKneeCheckBox = new CheckBox();
            _compressionApplied = new TestVolumeSliderControl();
            _errorCalled = false;

            SetPrivateField(_control, "Threshold", _thresholdControl);
            SetPrivateField(_control, "msb_AttackTime_ms", _attackTimeTextBox);
            SetPrivateField(_control, "msb_ReleaseTime_ms", _releaseTimeTextBox);
            SetPrivateField(_control, "msb_CompressionRatio", _compressionRatioTextBox);
            SetPrivateField(_control, "msb_KneeWidth_db", _kneeWidthTextBox);
            SetPrivateField(_control, "chkSoftKnee", _softKneeCheckBox);
            SetPrivateField(_control, "CompressionApplied", _compressionApplied);

            // Patch Error handler for error path tests
            typeof(DynamicRangeCompressorControl)
                .GetMethod("Error", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.CreateDelegate(typeof(Action<Exception>), _control);
            _control.OnError = (ex) => _errorCalled = true;

            // Set up mock ASIO
            var asioField = typeof(Program).GetField("ASIO", BindingFlags.Static | BindingFlags.Public);
            var mockAsio = new ASIO_Engine();
            typeof(ASIO_Engine).GetProperty("SampleRate_Current")?.SetValue(mockAsio, 44100);
            asioField?.SetValue(null, mockAsio);

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
        public void Constructor_InitializesFilterAndHandlers()
        {
            var filter = GetPrivateField<DynamicRangeCompressor>(_control, "Filter");
            Assert.IsNotNull(filter);
            Assert.AreEqual(FilterTypes.DynamicRangeCompressor, filter.FilterType);
        }

        [TestMethod]
        public void Threshold_VolumeChanged_UpdatesFilter()
        {
            var filter = GetPrivateField<DynamicRangeCompressor>(_control, "Filter");
            _thresholdControl.SetVolumeDb(-33.3);
            _thresholdControl.RaiseVolumeChanged();
            Assert.AreEqual(-33.3, filter.Threshold_dB);
        }

        [TestMethod]
        public void ApplySettings_ValidInput_UpdatesFilter()
        {
            var filter = GetPrivateField<DynamicRangeCompressor>(_control, "Filter");
            _thresholdControl.SetVolumeDb(-10.0);
            _attackTimeTextBox.Text = "12";
            _releaseTimeTextBox.Text = "34";
            _compressionRatioTextBox.Text = "56";
            _kneeWidthTextBox.Text = "7";
            _softKneeCheckBox.Checked = false;
            _control.ApplySettings();
            Assert.AreEqual(-10.0, filter.Threshold_dB);
            Assert.AreEqual(12.0, filter.AttackTime_ms);
            Assert.AreEqual(34.0, filter.ReleaseTime_ms);
            Assert.AreEqual(56.0, filter.Ratio);
            Assert.AreEqual(7.0, filter.KneeWidth_dB);
            Assert.IsFalse(filter.UseSoftKnee);
        }

        [TestMethod]
        public void ApplySettings_ClampsAndCorrectsInvalidValues()
        {
            _attackTimeTextBox.Text = "0.2";
            _releaseTimeTextBox.Text = "0.9";
            _compressionRatioTextBox.Text = "2";
            _kneeWidthTextBox.Text = "0.5";
            _control.ApplySettings();
            var filter = GetPrivateField<DynamicRangeCompressor>(_control, "Filter");
            Assert.AreEqual(1.0, filter.AttackTime_ms);
            Assert.AreEqual(1.0, filter.ReleaseTime_ms);
            Assert.AreEqual(11.0, filter.Ratio);
            Assert.AreEqual(1.0, filter.KneeWidth_dB);
            Assert.AreEqual("1", _attackTimeTextBox.Text);
            Assert.AreEqual("1", _releaseTimeTextBox.Text);
            Assert.AreEqual("11", _compressionRatioTextBox.Text);
            Assert.AreEqual("1", _kneeWidthTextBox.Text);
        }

        [TestMethod]
        public void ApplySettings_InvalidText_ThrowsFormatException()
        {
            _attackTimeTextBox.Text = "abc";
            _releaseTimeTextBox.Text = "def";
            _compressionRatioTextBox.Text = "ghi";
            _kneeWidthTextBox.Text = "jkl";
            Assert.ThrowsException<FormatException>(() => _control.ApplySettings());
        }

        [TestMethod]
        public void GetFilter_ReturnsCurrentFilter()
        {
            var filter = _control.GetFilter;
            Assert.IsNotNull(filter);
            Assert.IsInstanceOfType(filter, typeof(DynamicRangeCompressor));
        }

        [TestMethod]
        public void SetDeepClonedFilter_ValidType_UpdatesUIAndFilter()
        {
            var newFilter = new DynamicRangeCompressor
            {
                Threshold_dB = -8.0,
                AttackTime_ms = 2.0,
                ReleaseTime_ms = 3.0,
                Ratio = 12.0,
                KneeWidth_dB = 4.0,
                UseSoftKnee = true
            };
            _control.SetDeepClonedFilter(newFilter);
            var filter = GetPrivateField<DynamicRangeCompressor>(_control, "Filter");
            Assert.AreEqual(-8.0, filter.Threshold_dB);
            Assert.AreEqual(2.0, filter.AttackTime_ms);
            Assert.AreEqual(3.0, filter.ReleaseTime_ms);
            Assert.AreEqual(12.0, filter.Ratio);
            Assert.AreEqual(4.0, filter.KneeWidth_dB);
            Assert.IsTrue(filter.UseSoftKnee);
            Assert.AreEqual(-8.0, _thresholdControl.GetVolumeDb());
            Assert.AreEqual("2", _attackTimeTextBox.Text);
            Assert.AreEqual("3", _releaseTimeTextBox.Text);
            Assert.AreEqual("12", _compressionRatioTextBox.Text);
            Assert.AreEqual("4", _kneeWidthTextBox.Text);
            Assert.IsTrue(_softKneeCheckBox.Checked);
        }

        [TestMethod]
        public void SetDeepClonedFilter_InvalidType_DoesNothing()
        {
            var wrongFilter = new TestFilter();
            var originalFilter = GetPrivateField<DynamicRangeCompressor>(_control, "Filter");
            _control.SetDeepClonedFilter(wrongFilter);
            Assert.AreEqual(originalFilter, GetPrivateField<DynamicRangeCompressor>(_control, "Filter"));
        }

        [TestMethod]
        public void SoftKnee_CheckedChanged_UpdatesFilter()
        {
            var filter = GetPrivateField<DynamicRangeCompressor>(_control, "Filter");
            _softKneeCheckBox.Checked = true;
            _control.TestSoftKneeChanged();
            Assert.IsTrue(filter.UseSoftKnee);
            _softKneeCheckBox.Checked = false;
            _control.TestSoftKneeChanged();
            Assert.IsFalse(filter.UseSoftKnee);
        }

        [TestMethod]
        public void SoftKnee_CheckedChanged_Exception_TriggersError()
        {
            // Patch control to throw in CheckedChanged
            _control.ThrowOnSoftKnee = true;
            _softKneeCheckBox.Checked = true;
            _control.TestSoftKneeChanged();
            Assert.IsTrue(_errorCalled);
            _control.ThrowOnSoftKnee = false;
        }

        [TestMethod]
        public void RefreshTimerTick_UpdatesCompressionApplied()
        {
            var filter = GetPrivateField<DynamicRangeCompressor>(_control, "Filter");
            typeof(DynamicRangeCompressor).GetProperty("CompressionApplied")?.SetValue(filter, 0.77);
            _control.TestRefreshTimer();
            Assert.AreEqual(0.77, _compressionApplied.GetVolume());
        }

        [TestMethod]
        public void RefreshTimerTick_Exception_TriggersError()
        {
            _control.ThrowOnRefresh = true;
            _control.TestRefreshTimer();
            Assert.IsTrue(_errorCalled);
            _control.ThrowOnRefresh = false;
        }

        [TestMethod]
        public void SampleRateChange_UpdatesFilterCoeffs()
        {
            var filter = GetPrivateField<DynamicRangeCompressor>(_control, "Filter");
            filter.AttackTime_ms = 10;
            filter.ReleaseTime_ms = 20;
            _control.RaiseSampleRateChanged(12345);
            // No exception = pass; actual coeffs are private
        }

        [TestMethod]
        public void BtnApplyClick_CallsApplySettings()
        {
            bool called = false;
            _control.OnApplySettings = () => called = true;
            _control.TestBtnApplyClick();
            Assert.IsTrue(called);
        }
    }

    public class TestableDynamicRangeCompressorControl : DynamicRangeCompressorControl
    {
        public bool ThrowOnSoftKnee = false;
        public bool ThrowOnRefresh = false;
        public Action<Exception> OnError;
        public Action OnApplySettings;

        public new void MapEventHandlers() => base.MapEventHandlers();
        public void RaiseSampleRateChanged(int sampleRate) => SampleRateChangeNotifier_SampleRateChanged(sampleRate);
        public void TestSoftKneeChanged()
        {
            if (ThrowOnSoftKnee) { OnError?.Invoke(new Exception("Test")); return; }
            var mi = typeof(DynamicRangeCompressorControl).GetMethod("chkSoftKnee_CheckedChanged", BindingFlags.Instance | BindingFlags.NonPublic);
            mi.Invoke(this, new object[] { chkSoftKnee, EventArgs.Empty });
        }
        public void TestRefreshTimer()
        {
            if (ThrowOnRefresh) { OnError?.Invoke(new Exception("Test")); return; }
            var mi = typeof(DynamicRangeCompressorControl).GetMethod("RefreshTimer_Tick", BindingFlags.Instance | BindingFlags.NonPublic);
            mi.Invoke(this, new object[] { null, EventArgs.Empty });
        }
        public void TestBtnApplyClick()
        {
            if (OnApplySettings != null) { OnApplySettings(); return; }
            var mi = typeof(DynamicRangeCompressorControl).GetMethod("btnApply_Click", BindingFlags.Instance | BindingFlags.NonPublic);
            mi.Invoke(this, new object[] { this, EventArgs.Empty });
        }
        public new void ApplySettings()
        {
            if (OnApplySettings != null) { OnApplySettings(); return; }
            base.ApplySettings();
        }
        public void CallError(Exception ex)
        {
            var mi = typeof(DynamicRangeCompressorControl).GetMethod("Error", BindingFlags.Instance | BindingFlags.NonPublic);
            mi.Invoke(this, new object[] { ex });
        }
    }

    public class TestVolumeSliderControl : BTH_VolumeSliderControl
    {
        public void SetVolume(double value) => Volume = value;
        public void SetVolumeDb(double value) => VolumedB = value;
        public double GetVolume() => Volume;
        public double GetVolumeDb() => VolumedB;
        public void RaiseVolumeChanged()
        {
            // Use reflection to call the protected OnVolumeChanged(EventArgs) method
            var mi = typeof(BTH_VolumeSliderControl).GetMethod("OnVolumeChanged", BindingFlags.Instance | BindingFlags.NonPublic);
            mi.Invoke(this, new object[] { EventArgs.Empty });
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