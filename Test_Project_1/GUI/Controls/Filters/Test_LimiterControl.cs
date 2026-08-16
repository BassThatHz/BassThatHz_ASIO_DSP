using Microsoft.VisualStudio.TestTools.UnitTesting;
using BassThatHz_ASIO_DSP_Processor.GUI.Controls;
using BassThatHz_ASIO_DSP_Processor;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace Test_Project_1
{
    [TestClass]
    public class Test_LimiterControl
    {
        private TestableLimiterControl _control;
        private LimiterMockVolumeControl _limitControl;
        private LimiterMockVolumeControl _thresholdControl;
        private LimiterMockVolumeControl _compressionAppliedControl;
        private MaskedTextBox _attackTextBox;
        private MaskedTextBox _releaseTextBox;
        private CheckBox _peakHoldAttackCheckBox;
        private CheckBox _peakHoldReleaseCheckBox;

        [TestInitialize]
        public void InitializeTest()
        {
            _control = new TestableLimiterControl();
            _limitControl = new LimiterMockVolumeControl();
            _thresholdControl = new LimiterMockVolumeControl();
            _compressionAppliedControl = new LimiterMockVolumeControl();
            _attackTextBox = new MaskedTextBox();
            _releaseTextBox = new MaskedTextBox();
            _peakHoldAttackCheckBox = new CheckBox();
            _peakHoldReleaseCheckBox = new CheckBox();

            // Set up test controls
            SetPrivateField(_control, "Limit", _limitControl);
            SetPrivateField(_control, "Threshold", _thresholdControl);
            SetPrivateField(_control, "CompressionApplied", _compressionAppliedControl);
            SetPrivateField(_control, "mask_Attack", _attackTextBox);
            SetPrivateField(_control, "mask_Release", _releaseTextBox);
            SetPrivateField(_control, "chk_PeakHoldAttack", _peakHoldAttackCheckBox);
            SetPrivateField(_control, "chk_PeakHoldRelease", _peakHoldReleaseCheckBox);

            // Set up ASIO mock
            var mockAsio = new ASIO_Engine();
            typeof(ASIO_Engine).GetProperty("SampleRate_Current")?.SetValue(mockAsio, 44100);
            var asioField = typeof(Program).GetField("ASIO", BindingFlags.Static | BindingFlags.Public);
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
            var filter = GetPrivateField<Limiter>(_control, "Filter");
            Assert.IsNotNull(filter);
            Assert.AreEqual(FilterTypes.Limiter, filter.FilterType);
        }

        [TestMethod]
        public void TestLimitChanged_UpdatesFilter()
        {
            // Arrange
            var filter = GetPrivateField<Limiter>(_control, "Filter");
            double newValue = 0.8;

            // Act
            _limitControl.SetVolume(newValue);
            _limitControl.OnVolumeChanged();

            // Assert
            Assert.AreEqual(newValue, filter.MaxValue);
        }

        [TestMethod]
        public void TestLimitChanged_ClampsThreshold()
        {
            // Arrange
            // BTH_VolumeSliderControl's constructor sets Volume = 0, which clamps to MinDb (-384),
            // so a freshly constructed Limit slider sits at -384dB, not 0dB. Without seeding a sane
            // Limit first, the Threshold handler immediately drags Threshold down to -384 and this
            // test never reaches the behaviour it is named for.
            _limitControl.SetVolumeDb(0);
            _thresholdControl.SetVolumeDb(-0.5); // Above minimum -1dB
            _limitControl.SetVolumeDb(-5);

            // Act
            _limitControl.OnVolumeChanged();

            // Assert
            // Delta: every value round-trips dB -> linear -> dB through the shared slider control.
            Assert.AreEqual(-5, _thresholdControl.GetVolumeDb(), 1e-9);
        }

        [TestMethod]
        public void TestThresholdChanged_ClampsToLimitAndMinimum()
        {
            // LimiterControl.Threshold_VolumeChanged applies TWO clamps, in this order:
            //   1. if (Threshold.Volume >= Limit.Volume) Threshold.VolumedB = Limit.VolumedB;
            //   2. if (Threshold.VolumedB > -1d)         Threshold.VolumedB = -1d;
            // Whichever is more restrictive therefore wins. Both are exercised below; the old
            // version only asserted the -1dB floor while configuring a Limit that overrode it.
            var filter = GetPrivateField<Limiter>(_control, "Filter");

            // Case 1: Limit (-6dB) is more restrictive than the -1dB floor, so the Limit wins.
            _limitControl.SetVolumeDb(-6);
            _thresholdControl.SetVolumeDb(-0.5);

            _thresholdControl.OnVolumeChanged();

            Assert.AreEqual(-6.0, _thresholdControl.GetVolumeDb(), 1e-9);
            Assert.AreEqual(Math.Pow(10, -6.0 / 20), filter.Threshold, 1e-9);

            // Case 2: Limit (0dB) is less restrictive, so the -1dB minimum floor wins.
            _limitControl.SetVolumeDb(0);
            _thresholdControl.SetVolumeDb(-0.5);

            _thresholdControl.OnVolumeChanged();

            Assert.AreEqual(-1.0, _thresholdControl.GetVolumeDb(), 1e-9);
            Assert.AreEqual(Math.Pow(10, -1.0 / 20), filter.Threshold, 1e-9);
        }

        [TestMethod]
        public void TestPeakHoldAttack_UpdatesFilter()
        {
            // Arrange
            var filter = GetPrivateField<Limiter>(_control, "Filter");
            _attackTextBox.Text = "100";

            // Act
            _control.TestAttackTextChanged();

            // Assert
            Assert.AreEqual(100, filter.PeakHoldAttack);
        }

        [TestMethod]
        public void TestPeakHoldRelease_UpdatesFilter()
        {
            // Arrange
            var filter = GetPrivateField<Limiter>(_control, "Filter");
            _releaseTextBox.Text = "200";

            // Act
            _control.TestReleaseTextChanged();

            // Assert
            Assert.AreEqual(200, filter.PeakHoldRelease);
        }

        [TestMethod]
        public void TestPeakHoldAttackEnabled_UpdatesFilter()
        {
            // Arrange
            var filter = GetPrivateField<Limiter>(_control, "Filter");
            _peakHoldAttackCheckBox.Checked = true;

            // Act
            _control.TestPeakHoldAttackCheckedChanged();

            // Assert
            Assert.IsTrue(filter.PeakHoldAttackEnabled);
        }

        [TestMethod]
        public void TestPeakHoldReleaseEnabled_UpdatesFilter()
        {
            // Arrange
            var filter = GetPrivateField<Limiter>(_control, "Filter");
            _peakHoldReleaseCheckBox.Checked = true;

            // Act
            _control.TestPeakHoldReleaseCheckedChanged();

            // Assert
            Assert.IsTrue(filter.PeakHoldReleaseEnabled);
        }

        [TestMethod]
        public void TestRefreshTimer_UpdatesCompressionDisplay()
        {
            // Arrange
            var filter = GetPrivateField<Limiter>(_control, "Filter");
            // CompressionApplied and IsBrickwall are public FIELDS on Limiter, so GetProperty
            // returned null and the ?.SetValue calls silently did nothing.
            typeof(Limiter).GetField("CompressionApplied")?.SetValue(filter, 0.5);
            typeof(Limiter).GetField("IsBrickwall")?.SetValue(filter, true);

            // Act
            _control.TestRefreshTimer();

            // Assert
            Assert.AreEqual(0.5, _compressionAppliedControl.GetVolume());
            Assert.AreEqual(Color.White, _compressionAppliedControl.GetTextColor());
        }

        [TestMethod]
        public void TestSetDeepClonedFilter_UpdatesFilterAndUI()
        {
            // Arrange
            var newFilter = new Limiter
            {
                MaxValue = 0.8,
                Threshold = 0.5,
                PeakHoldAttack = 100,
                PeakHoldRelease = 200,
                PeakHoldAttackEnabled = true,
                PeakHoldReleaseEnabled = true
            };

            // Act
            _control.SetDeepClonedFilter(newFilter);

            // Assert
            var currentFilter = GetPrivateField<Limiter>(_control, "Filter");
            Assert.AreEqual(0.8, currentFilter.MaxValue);
            Assert.AreEqual(0.5, currentFilter.Threshold);
            Assert.AreEqual(100, currentFilter.PeakHoldAttack);
            Assert.AreEqual(200, currentFilter.PeakHoldRelease);
            Assert.IsTrue(currentFilter.PeakHoldAttackEnabled);
            Assert.IsTrue(currentFilter.PeakHoldReleaseEnabled);

            Assert.AreEqual(0.8, _limitControl.GetVolume());
            Assert.AreEqual(0.5, _thresholdControl.GetVolume());
            Assert.AreEqual("100", _attackTextBox.Text);
            Assert.AreEqual("200", _releaseTextBox.Text);
            Assert.IsTrue(_peakHoldAttackCheckBox.Checked);
            Assert.IsTrue(_peakHoldReleaseCheckBox.Checked);
        }

        [TestMethod]
        public void TestSetDeepClonedFilter_HandlesWrongType()
        {
            // Arrange
            var wrongFilter = new LimiterTestFilter();
            var originalFilter = GetPrivateField<Limiter>(_control, "Filter");
            var originalLimit = _limitControl.GetVolume();

            // Act
            _control.SetDeepClonedFilter(wrongFilter);

            // Assert
            Assert.AreEqual(originalFilter, GetPrivateField<Limiter>(_control, "Filter"));
            Assert.AreEqual(originalLimit, _limitControl.GetVolume());
        }
    }

    public class TestableLimiterControl : LimiterControl
    {
        public new void MapEventHandlers() => base.MapEventHandlers();
        public void TestAttackTextChanged() => Mask_Decay_TextChanged(null, EventArgs.Empty);
        public void TestReleaseTextChanged() => Mask_Release_TextChanged(null, EventArgs.Empty);
        public void TestPeakHoldAttackCheckedChanged() => chk_PeakHoldAttack_CheckedChanged(null, EventArgs.Empty);
        public void TestPeakHoldReleaseCheckedChanged() => chk_PeakHoldRelease_CheckedChanged(null, EventArgs.Empty);
        public void TestRefreshTimer() => RefreshTimer_Tick(null, EventArgs.Empty);
    }

    [DesignerCategory("Code")]
    public class LimiterMockVolumeControl : BTH_VolumeSliderControl
    {
        public void SetVolume(double value) => this.Volume = value;
        public void SetVolumeDb(double value) => this.VolumedB = value;
        public double GetVolume() => this.Volume;
        public double GetVolumeDb() => this.VolumedB;
        public Color GetTextColor() => this.TextColor;
        public Brush GetSliderBrush() => this.SliderColor;

        public void OnVolumeChanged()
        {
            var field = typeof(BTH_VolumeSliderControl).GetField("VolumeChanged", BindingFlags.Instance | BindingFlags.NonPublic);
            var handler = field?.GetValue(this) as EventHandler;
            handler?.Invoke(this, EventArgs.Empty);
        }
    }

    internal class LimiterTestFilter : IFilter
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