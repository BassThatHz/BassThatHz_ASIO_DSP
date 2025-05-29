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
    public class Test_ClassicLimiterControl
    {
        private TestableClassicLimiterControl _control;
        private MockVolumeControl _thresholdControl;
        private TextBox _attackTimeTextBox;
        private TextBox _releaseTimeTextBox;
        private TextBox _kneeWidthTextBox;
        private CheckBox _softKneeCheckBox;
        private MockVolumeControl _compressionApplied;

        [TestInitialize]
        public void InitializeTest()
        {
            _thresholdControl = new MockVolumeControl();
            _attackTimeTextBox = new TextBox();
            _releaseTimeTextBox = new TextBox();
            _kneeWidthTextBox = new TextBox();
            _softKneeCheckBox = new CheckBox();
            _compressionApplied = new MockVolumeControl();

            // Set up the ASIO mock
            var mockAsio = new ASIO_Engine();
            typeof(ASIO_Engine).GetProperty("SampleRate_Current")?.SetValue(mockAsio, 44100);
            var asioField = typeof(Program).GetField("ASIO", BindingFlags.Static | BindingFlags.Public);
            asioField?.SetValue(null, mockAsio);

            _control = new TestableClassicLimiterControl();
            SetPrivateField(_control, "Threshold", _thresholdControl);
            SetPrivateField(_control, "msb_AttackTime_ms", _attackTimeTextBox);
            SetPrivateField(_control, "msb_ReleaseTime_ms", _releaseTimeTextBox);
            SetPrivateField(_control, "msb_KneeWidth_db", _kneeWidthTextBox);
            SetPrivateField(_control, "chkSoftKnee", _softKneeCheckBox);
            SetPrivateField(_control, "CompressionApplied", _compressionApplied);
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
            _thresholdControl.Volume = newVolume;
            _thresholdControl.RaiseVolumeChanged();

            // Assert
            Assert.AreEqual(newVolume, filter.Threshold_dB);
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
            Assert.AreEqual(-6.0, filter.Threshold_dB);
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
    public class MockVolumeControl : UserControl
    {
        private double _volume;
        private double _volumeDb;

        public event EventHandler VolumeChanged;

        [Browsable(true)]
        [Category("Data")]
        [System.ComponentModel.Description("The volume in linear scale")]
        [DefaultValue(0.0)]
        public virtual double Volume 
        { 
            get => _volume;
            set
            {
                if (_volume != value)
                {
                    _volume = value;
                    VolumeChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        [Browsable(true)]
        [Category("Data")]
        [System.ComponentModel.Description("The volume in decibels")]
        [DefaultValue(0.0)]
        public virtual double VolumedB
        {
            get => _volumeDb;
            set
            {
                if (_volumeDb != value)
                {
                    _volumeDb = value;
                    VolumeChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public void RaiseVolumeChanged()
        {
            VolumeChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}