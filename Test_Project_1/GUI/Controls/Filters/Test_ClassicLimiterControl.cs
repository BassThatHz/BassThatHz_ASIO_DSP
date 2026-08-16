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
    [DoNotParallelize]
    public class Test_ClassicLimiterControl
    {
        private TestableClassicLimiterControl _control;
        private MockVolumeControl _thresholdControl;
        private MaskedTextBox _attackTimeTextBox;
        private MaskedTextBox _releaseTimeTextBox;
        private MaskedTextBox _kneeWidthTextBox;
        private CheckBox _softKneeCheckBox;
        private MockVolumeControl _compressionApplied;

        [TestInitialize]
        public void InitializeTest()
        {
            _thresholdControl = new MockVolumeControl();
            _attackTimeTextBox = new MaskedTextBox();
            _releaseTimeTextBox = new MaskedTextBox();
            _kneeWidthTextBox = new MaskedTextBox();
            _softKneeCheckBox = new CheckBox();
            _compressionApplied = new MockVolumeControl();

            // Set up the ASIO mock
            var mockAsio = new ASIO_Engine();
            typeof(ASIO_Engine).GetProperty("SampleRate_Current")?.SetValue(mockAsio, 44100);
            Program.SetAsioForTesting(mockAsio);

            _control = new TestableClassicLimiterControl();
            SetPrivateField(_control, "Threshold", _thresholdControl);
            SetPrivateField(_control, "msb_AttackTime_ms", _attackTimeTextBox);
            SetPrivateField(_control, "msb_ReleaseTime_ms", _releaseTimeTextBox);
            SetPrivateField(_control, "msb_KneeWidth_db", _kneeWidthTextBox);
            SetPrivateField(_control, "chkSoftKnee", _softKneeCheckBox);
            SetPrivateField(_control, "CompressionApplied", _compressionApplied);

            // The constructor already ran MapEventHandlers() against the DESIGNER-created Threshold
            // control; the reflection swap above points the field at the mock instead, leaving the
            // mock's VolumeChanged event with no subscriber at all. Re-wire so the production
            // handler actually runs (MapEventHandlers unsubscribes-then-resubscribes, so this is
            // idempotent). Test_LimiterControl and Test_DynamicRangeCompressorControl already do this.
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
            var filter = GetPrivateField<ClassicLimiter>(_control, "Filter");
            Assert.IsNotNull(filter);
        }

        [TestMethod]
        public void TestThresholdVolumeChanged_UpdatesFilter()
        {
            // Arrange
            var filter = GetPrivateField<ClassicLimiter>(_control, "Filter");
            double newVolume = -6.0;

            // Act
            // VolumedB, not Volume: -6.0 is a DECIBEL value; assigning it to the linear Volume
            // property is meaningless (a negative amplitude) and yielded NaN.
            _thresholdControl.VolumedB = newVolume;
            _thresholdControl.RaiseVolumeChanged();

            // Assert
            // Delta: dB -> linear -> dB through the shared slider control is lossy at the 1e-15 level.
            Assert.AreEqual(newVolume, filter.Threshold_dB, 1e-9);
        }

        [TestMethod]
        public void TestApplySettings_UpdatesAllSettings()
        {
            // Arrange
            var filter = GetPrivateField<ClassicLimiter>(_control, "Filter");
            _thresholdControl.VolumedB = -6.0;
            _attackTimeTextBox.Text = "10";
            _releaseTimeTextBox.Text = "100";
            _kneeWidthTextBox.Text = "3";
            _softKneeCheckBox.Checked = true;

            // Act
            _control.ApplySettings();

            // Assert
            // Delta: the value passed through BTH_VolumeSliderControl's dB -> linear -> dB round trip.
            Assert.AreEqual(-6.0, filter.Threshold_dB, 1e-9);
            Assert.AreEqual(10.0, filter.AttackTime_ms);
            Assert.AreEqual(100.0, filter.ReleaseTime_ms);
            Assert.AreEqual(3.0, filter.KneeWidth_dB);
            Assert.IsTrue(filter.UseSoftKnee);
        }

        [TestMethod]
        public void TestApplySettings_ClampsValuesBelow1()
        {
            // Arrange
            var filter = GetPrivateField<ClassicLimiter>(_control, "Filter");
            _attackTimeTextBox.Text = "0.5";
            _releaseTimeTextBox.Text = "0.1";
            _kneeWidthTextBox.Text = "0.8";

            // Act
            _control.ApplySettings();

            // Assert
            Assert.AreEqual(1.0, filter.AttackTime_ms);
            Assert.AreEqual(1.0, filter.ReleaseTime_ms);
            Assert.AreEqual(1.0, filter.KneeWidth_dB);
            Assert.AreEqual("1", _attackTimeTextBox.Text);
            Assert.AreEqual("1", _releaseTimeTextBox.Text);
            Assert.AreEqual("1", _kneeWidthTextBox.Text);
        }

        [TestMethod]
        public void TestGetFilter_ReturnsFilterInstance()
        {
            var filter = _control.GetFilter;
            Assert.IsNotNull(filter);
            Assert.IsInstanceOfType(filter, typeof(ClassicLimiter));
        }

        [TestMethod]
        public void TestSetDeepClonedFilter_UpdatesFilterAndUI()
        {
            // Arrange
            var newFilter = new ClassicLimiter
            {
                Threshold_dB = -12.0,
                AttackTime_ms = 15.0,
                ReleaseTime_ms = 150.0,
                KneeWidth_dB = 6.0,
                UseSoftKnee = true
            };

            // Act
            _control.SetDeepClonedFilter(newFilter);

            // Assert
            var currentFilter = GetPrivateField<ClassicLimiter>(_control, "Filter");
            Assert.AreEqual(-12.0, currentFilter.Threshold_dB);
            Assert.AreEqual(15.0, currentFilter.AttackTime_ms);
            Assert.AreEqual(150.0, currentFilter.ReleaseTime_ms);
            Assert.AreEqual(6.0, currentFilter.KneeWidth_dB);
            Assert.IsTrue(currentFilter.UseSoftKnee);
            
            Assert.AreEqual(-12.0, _thresholdControl.VolumedB);
            Assert.AreEqual("15", _attackTimeTextBox.Text);
            Assert.AreEqual("150", _releaseTimeTextBox.Text);
            Assert.AreEqual("6", _kneeWidthTextBox.Text);
            Assert.IsTrue(_softKneeCheckBox.Checked);
        }
    }

    public class TestableClassicLimiterControl : ClassicLimiterControl
    {
        public new void MapEventHandlers() => base.MapEventHandlers();
    }

    [DesignerCategory("Code")]
    public class MockVolumeControl : BTH_VolumeSliderControl
    {
        public void RaiseVolumeChanged()
        {
            var field = typeof(BTH_VolumeSliderControl).GetField("VolumeChanged", BindingFlags.Instance | BindingFlags.NonPublic);
            var handler = field?.GetValue(this) as EventHandler;
            handler?.Invoke(this, EventArgs.Empty);
        }
    }
}