using Microsoft.VisualStudio.TestTools.UnitTesting;
using BassThatHz_ASIO_DSP_Processor.GUI.Controls;
using BassThatHz_ASIO_DSP_Processor.GUI.Controls.Filters;
using BassThatHz_ASIO_DSP_Processor.GUI.Forms;
using BassThatHz_ASIO_DSP_Processor;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows.Forms;

namespace Test_Project_1
{
    [TestClass]
    public class Test_MixerControl
    {
        private TestableMixerControl _control;
        private FlowLayoutPanel _mixerElementsPanel;
        private Button _configButton;

        [TestInitialize]
        public void InitializeTest()
        {
            _control = new TestableMixerControl();
            _mixerElementsPanel = new FlowLayoutPanel();
            _configButton = new Button();

            // Set up test controls
            SetPrivateField(_control, "flp_MixerElements", _mixerElementsPanel);
            SetPrivateField(_control, "btnConfig", _configButton);

            // Set up ASIO mock
            var mockAsio = new ASIO_Engine();
            typeof(ASIO_Engine).GetProperty("SampleRate_Current")?.SetValue(mockAsio, 44100);
            var asioField = typeof(Program).GetField("ASIO", BindingFlags.Static | BindingFlags.Public);
            asioField?.SetValue(null, mockAsio);
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
            var filter = GetPrivateField<Mixer>(_control, "Filter");
            Assert.IsNotNull(filter);
            Assert.AreEqual(FilterTypes.Mixer, filter.FilterType);
        }

        //[TestMethod]
        //public void TestConfigButton_OpensForm()
        //{
        //    // Arrange
        //    bool formOpened = false;
        //    _control.FormShown += (form) => formOpened = true;

        //    // Act
        //    _control.InvokeConfigButtonClick();

        //    // Assert
        //    Assert.IsTrue(formOpened);
        //    Assert.IsNotNull(_control.GetMixerForm());
        //}

        [TestMethod]
        public void TestAddMixerElement_CreatesElement()
        {
            // Arrange
            int initialCount = _mixerElementsPanel.Controls.Count;

            // Act
            _control.InvokeAddMixerElement();

            // Assert
            Assert.AreEqual(initialCount + 1, _mixerElementsPanel.Controls.Count);
            Assert.IsInstanceOfType(_mixerElementsPanel.Controls[initialCount], typeof(MixerElement));
        }

        [TestMethod]
        public void TestAddMixerElements_AddsMultipleElements()
        {
            // Arrange
            var mixerInputs = new List<MixerInput>
            {
                new MixerInput { Attenuation = -3, StreamAttenuation = -6, Enabled = true, ChannelIndex = 0 },
                new MixerInput { Attenuation = -2, StreamAttenuation = -4, Enabled = false, ChannelIndex = 1 }
            };

            // Act
            _control.InvokeAddMixerElements(mixerInputs);

            // Assert
            Assert.AreEqual(2, _mixerElementsPanel.Controls.Count);
            var element1 = _mixerElementsPanel.Controls[0] as MixerElement;
            var element2 = _mixerElementsPanel.Controls[1] as MixerElement;

            Assert.AreEqual("-3", element1.Get_txtChAttenuation.Text);
            Assert.AreEqual("-6", element1.Get_txtStreamAttenuation.Text);
            Assert.IsTrue(element1.Get_chkChannel.Checked);

            Assert.AreEqual("-2", element2.Get_txtChAttenuation.Text);
            Assert.AreEqual("-4", element2.Get_txtStreamAttenuation.Text);
            Assert.IsFalse(element2.Get_chkChannel.Checked);
        }

        [TestMethod]
        public void TestClearElements_RemovesAllElements()
        {
            // Arrange
            _control.InvokeAddMixerElement();
            _control.InvokeAddMixerElement();
            Assert.AreNotEqual(0, _mixerElementsPanel.Controls.Count);

            // Act
            _control.InvokeClearElements();

            // Assert
            Assert.AreEqual(0, _mixerElementsPanel.Controls.Count);
        }

        [TestMethod]
        public void TestGetFilter_ReturnsFilterInstance()
        {
            var filter = _control.GetFilter;
            Assert.IsNotNull(filter);
            Assert.IsInstanceOfType(filter, typeof(Mixer));
        }

        [TestMethod]
        public void TestSetDeepClonedFilter_UpdatesFilterAndUI()
        {
            // Arrange
            var newFilter = new Mixer();
            var mixerInputs = new List<MixerInput>
            {
                new MixerInput { Attenuation = -3, StreamAttenuation = -6, Enabled = true, ChannelIndex = 0 },
                new MixerInput { Attenuation = -2, StreamAttenuation = -4, Enabled = false, ChannelIndex = 1 }
            };
            newFilter.MixerInputs = mixerInputs;

            // Act
            _control.SetDeepClonedFilter(newFilter);

            // Assert
            var currentFilter = GetPrivateField<Mixer>(_control, "Filter");
            Assert.AreEqual(2, currentFilter.MixerInputs.Count);
            Assert.AreEqual(2, _mixerElementsPanel.Controls.Count);

            var element1 = _mixerElementsPanel.Controls[0] as MixerElement;
            Assert.AreEqual("-3", element1.Get_txtChAttenuation.Text);
            Assert.AreEqual("-6", element1.Get_txtStreamAttenuation.Text);
            Assert.IsTrue(element1.Get_chkChannel.Checked);

            var element2 = _mixerElementsPanel.Controls[1] as MixerElement;
            Assert.AreEqual("-2", element2.Get_txtChAttenuation.Text);
            Assert.AreEqual("-4", element2.Get_txtStreamAttenuation.Text);
            Assert.IsFalse(element2.Get_chkChannel.Checked);
        }

        [TestMethod]
        public void TestSetDeepClonedFilter_HandlesWrongType()
        {
            // Arrange
            var wrongFilter = new MixerTestFilter();
            var originalFilter = GetPrivateField<Mixer>(_control, "Filter");
            var originalElementCount = _mixerElementsPanel.Controls.Count;

            // Act
            _control.SetDeepClonedFilter(wrongFilter);

            // Assert
            Assert.AreEqual(originalFilter, GetPrivateField<Mixer>(_control, "Filter"));
            Assert.AreEqual(originalElementCount, _mixerElementsPanel.Controls.Count);
        }

        [TestMethod]
        public void TestApplySettings_UpdatesFilter()
        {
            // Arrange
            var mixerInputs = new List<MixerInput>
            {
                new MixerInput { Attenuation = -3, StreamAttenuation = -6, Enabled = true, ChannelIndex = 0 }
            };
            _control.InvokeAddMixerElements(mixerInputs);

            // Act
            _control.ApplySettings();

            // Assert
            var filter = GetPrivateField<Mixer>(_control, "Filter");
            Assert.IsTrue(filter.FilterEnabled);
            // Additional assertions based on what ApplySettings does
        }
    }

    public class TestableMixerControl : MixerControl
    {
        public event Action<Form> FormShown;

        public Form GetMixerForm() => GetPrivateField<Form>(this, "MixerForm");
        
        public void InvokeConfigButtonClick() 
        {
            var method = GetType().GetMethod("btnConfig_Click", BindingFlags.NonPublic | BindingFlags.Instance);
            method?.Invoke(this, new object[] { this, EventArgs.Empty });
        }
        
        public void InvokeAddMixerElement()
        {
            var method = GetType().GetMethod("AddMixerElement", BindingFlags.NonPublic | BindingFlags.Instance);
            method?.Invoke(this, null);
        }
        
        public void InvokeAddMixerElements(List<MixerInput> inputs)
        {
            var method = GetType().GetMethod("AddRangeOfMixerElements", BindingFlags.NonPublic | BindingFlags.Instance);
            method?.Invoke(this, new object[] { inputs });
        }
        
        public void InvokeClearElements()
        {
            var method = GetType().GetMethod("ClearElements", BindingFlags.NonPublic | BindingFlags.Instance);
            method?.Invoke(this, null);
        }

        private T GetPrivateField<T>(object obj, string fieldName)
        {
            var field = obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            return (T)field?.GetValue(obj);
        }
    }

    internal class MixerTestFilter : IFilter
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