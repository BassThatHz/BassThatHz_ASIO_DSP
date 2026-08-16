using Microsoft.VisualStudio.TestTools.UnitTesting;
using BassThatHz_ASIO_DSP_Processor.GUI.Controls;
using BassThatHz_ASIO_DSP_Processor.GUI.Controls.Filters;
using BassThatHz_ASIO_DSP_Processor.GUI.Forms;
using BassThatHz_ASIO_DSP_Processor;
using NAudio.Wave.Asio;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows.Forms;

namespace Test_Project_1
{
    // NOTE ON THIS FIXTURE
    // MixerControl no longer owns element-list management; that responsibility moved to FormMixer.
    // The old tests reflected for AddMixerElement / AddRangeOfMixerElements / ClearElements on
    // MixerControl, got null back from GetMethod, and their `method?.Invoke(...)` silently did
    // nothing - so every assertion ran against an untouched control. They also read
    // `listBox1.Controls.Count`, which is always 0 (a ListBox holds Items, not child Controls).
    // The element-level tests below now drive FormMixer's own members directly, and the
    // control-level tests exercise the FormMixer -> MixerControl callbacks in AttachMixerFormCallbacks.
    [TestClass]
    public class Test_MixerControl
    {
        private TestableMixerControl _control;
        private ListBox _mixerListBox;
        private Button _configButton;

        [TestInitialize]
        public void InitializeTest()
        {
            _control = new TestableMixerControl();
            _configButton = new Button();

            // Ensure InitializeComponent is called to initialize the control's fields
            var initMethod = _control.GetType().GetMethod("InitializeComponent", BindingFlags.NonPublic | BindingFlags.Instance);
            initMethod?.Invoke(_control, null);

            // The ListBox MixerControl uses to display the enabled mixer inputs.
            _mixerListBox = (ListBox)GetFieldIncludingBaseTypes(_control, "listBox1")?.GetValue(_control);
            SetPrivateField(_control, "btnConfigMixer", _configButton);

            // Set up ASIO mock
            var mockAsio = new ASIO_Engine();
            typeof(ASIO_Engine).GetProperty("SampleRate_Current")?.SetValue(mockAsio, 44100);
        }

        private static AsioChannelInfo Channel(int index) =>
            new AsioChannelInfo { channel = index, name = $"Ch{index}", isInput = true, isActive = true };

        private static FieldInfo GetFieldIncludingBaseTypes(object obj, string fieldName)
        {
            var type = obj.GetType();
            while (type != null)
            {
                var field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                if (field != null)
                    return field;
                type = type.BaseType;
            }
            return null;
        }

        private void SetPrivateField(object obj, string fieldName, object value)
        {
            var field = GetFieldIncludingBaseTypes(obj, fieldName);
            field?.SetValue(obj, value);
        }

        private T GetPrivateField<T>(object obj, string fieldName)
        {
            var field = GetFieldIncludingBaseTypes(obj, fieldName);
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
            using var form = new TestableFormMixer();
            int initialCount = form.GetPanel1().Controls.Count;

            // Act
            var element = form.InvokeCreateMixerElement(Channel(0), 0);

            // Assert
            Assert.IsNotNull(element);
            Assert.AreEqual(initialCount + 1, form.GetPanel1().Controls.Count);
            Assert.IsInstanceOfType(form.GetPanel1().Controls[initialCount], typeof(MixerElement));
            Assert.AreSame(element, form.GetPanel1().Controls[initialCount]);
            Assert.AreEqual("(0) Ch0", element.Get_chkChannel.Text);
        }

        [TestMethod]
        public void TestAddMixerElements_AddsMultipleElements()
        {
            // Arrange
            using var form = new TestableFormMixer();
            var mixerInputs = new List<MixerInput>
            {
                new MixerInput { Attenuation = -3, StreamAttenuation = -6, Enabled = true, ChannelIndex = 0 },
                new MixerInput { Attenuation = -2, StreamAttenuation = -4, Enabled = false, ChannelIndex = 1 }
            };

            // Act
            for (int i = 0; i < mixerInputs.Count; i++)
            {
                var element = form.InvokeCreateMixerElement(Channel(mixerInputs[i].ChannelIndex), i);
                form.InvokeCreateMixerElementEventHandlers(mixerInputs[i], element);
            }

            // Assert
            Assert.AreEqual(2, form.GetPanel1().Controls.Count);
            var element1 = form.GetPanel1().Controls[0] as MixerElement;
            var element2 = form.GetPanel1().Controls[1] as MixerElement;

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
            using var form = new TestableFormMixer();
            _ = form.InvokeCreateMixerElement(Channel(0), 0);
            _ = form.InvokeCreateMixerElement(Channel(1), 1);
            Assert.AreEqual(2, form.GetPanel1().Controls.Count);

            // Act
            form.InvokeClearGUI();

            // Assert
            Assert.AreEqual(0, form.GetPanel1().Controls.Count);
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
                new MixerInput { Attenuation = -3, StreamAttenuation = -6, Enabled = true, ChannelIndex = 0, ChannelName = "Ch0" },
                new MixerInput { Attenuation = -2, StreamAttenuation = -4, Enabled = false, ChannelIndex = 1, ChannelName = "Ch1" }
            };
            newFilter.MixerInputs = mixerInputs;

            // Act
            _control.SetDeepClonedFilter(newFilter);

            // Assert - the control adopts the supplied filter instance.
            var currentFilterAfterLoad = GetPrivateField<Mixer>(_control, "Filter");
            Assert.AreSame(newFilter, currentFilterAfterLoad);

            // SetDeepClonedFilter then hands the list to FormMixer.RedrawPanelItemsFromLoader, which
            // rebuilds the element rows from the LIVE ASIO channel list and pushes the result back
            // through the AddRangeOfFilterElements callback. On a host with no ASIO device there are
            // no channels to rebuild from.
            //
            // DEFECT FIX: this assertion used to read `Assert.AreEqual(0, ...)` and described the
            // empty result as legitimate. It was not - it was pinning a data-loss bug. ApplyChanges
            // calls ClearAllFilterElements (clearing Filter.MixerInputs in place) and then re-added
            // only the rebuilt rows, so opening a config on a machine without the configured device
            // silently destroyed the saved routing, permanently once the user saved.
            // Saved entries with no live channel are now preserved; the enabled one survives, and the
            // DISABLED one is still dropped, which is the pre-existing deliberate rule that
            // AddRangeOfFilterElements only persists Enabled == true entries.
            Assert.AreEqual(1, currentFilterAfterLoad.MixerInputs.Count,
                "Saved routing must survive loading on a host with no ASIO device.");
            Assert.AreEqual(0, currentFilterAfterLoad.MixerInputs[0].ChannelIndex);
            Assert.AreEqual(-3, currentFilterAfterLoad.MixerInputs[0].Attenuation);
            Assert.AreEqual(-6, currentFilterAfterLoad.MixerInputs[0].StreamAttenuation);

            // Drive the callback directly with a fresh list to exercise the same production code path
            // (AttachMixerFormCallbacks) deterministically.

            var reloadedInputs = new List<MixerInput>
            {
                new MixerInput { Attenuation = -3, StreamAttenuation = -6, Enabled = true, ChannelIndex = 0, ChannelName = "Ch0" },
                new MixerInput { Attenuation = -2, StreamAttenuation = -4, Enabled = false, ChannelIndex = 1, ChannelName = "Ch1" }
            };
            var form = _control.GetMixerForm();
            Assert.IsNotNull(form, "SetDeepClonedFilter must have created the FormMixer.");
            form.AddRangeOfFilterElements(reloadedInputs);

            // Only ENABLED inputs survive into the filter and the list box.
            var currentFilter = GetPrivateField<Mixer>(_control, "Filter");
            Assert.AreEqual(1, currentFilter.MixerInputs.Count);
            Assert.AreEqual(0, currentFilter.MixerInputs[0].ChannelIndex);
            Assert.AreEqual(-3, currentFilter.MixerInputs[0].Attenuation);
            Assert.AreEqual(-6, currentFilter.MixerInputs[0].StreamAttenuation);
            Assert.IsTrue(currentFilter.MixerInputs[0].Enabled);

            Assert.AreEqual(1, _mixerListBox.Items.Count);
            Assert.AreEqual("(0) Ch0 : -3 | -6", _mixerListBox.Items[0].ToString());
        }

        [TestMethod]
        public void TestSetDeepClonedFilter_HandlesWrongType()
        {
            // Arrange
            var wrongFilter = new MixerTestFilter();
            var originalFilter = GetPrivateField<Mixer>(_control, "Filter");
            var originalItemCount = _mixerListBox.Items.Count;

            // Act
            _control.SetDeepClonedFilter(wrongFilter);

            // Assert
            Assert.AreEqual(originalFilter, GetPrivateField<Mixer>(_control, "Filter"));
            Assert.AreEqual(originalItemCount, _mixerListBox.Items.Count);
        }

        [TestMethod]
        public void TestApplySettings_UpdatesFilter()
        {
            // MixerControl.ApplySettings() delegates straight to Mixer.ApplySettings(), which is
            // deliberately a no-op ("Non-Applicable"): a Mixer has no derived coefficients to
            // recompute, its inputs are pushed in live by the FormMixer callbacks. The old
            // assertion (FilterEnabled == true) never had anything to make it true.
            // Arrange
            var filter = GetPrivateField<Mixer>(_control, "Filter");
            var mixerInputs = new List<MixerInput>
            {
                new MixerInput { Attenuation = -3, StreamAttenuation = -6, Enabled = true, ChannelIndex = 0, ChannelName = "Ch0" }
            };
            filter.MixerInputs = mixerInputs;
            filter.FilterEnabled = true;

            // Act
            _control.ApplySettings();

            // Assert - state is preserved, nothing is reset or dropped.
            var afterFilter = GetPrivateField<Mixer>(_control, "Filter");
            Assert.AreSame(filter, afterFilter);
            Assert.IsTrue(afterFilter.FilterEnabled);
            Assert.AreEqual(1, afterFilter.MixerInputs.Count);
            Assert.AreEqual(-3, afterFilter.MixerInputs[0].Attenuation);
            Assert.AreEqual(-6, afterFilter.MixerInputs[0].StreamAttenuation);
        }
    }

    public class TestableMixerControl : MixerControl
    {
        public event Action<Form> FormShown;

        /// <summary>
        /// The lazily created FormMixer, or null if nothing has caused it to be created yet.
        /// MixerControl stores it in the private backing field `_mixerForm`; the `MixerForm`
        /// property is a lazy getter, so reading the FIELD (as this does) must not be changed to
        /// read the property or it would construct a form as a side effect of inspecting one.
        /// </summary>
        public FormMixer GetMixerForm() => GetPrivateField<FormMixer>(this, "_mixerForm");

        public void InvokeConfigButtonClick()
        {
            var method = GetType().GetMethod("btnConfigMixer_Click", BindingFlags.NonPublic | BindingFlags.Instance);
            method?.Invoke(this, new object[] { this, EventArgs.Empty });
        }

        private T GetPrivateField<T>(object obj, string fieldName)
        {
            var type = obj.GetType();
            while (type != null)
            {
                var field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null)
                    return (T)field.GetValue(obj);
                type = type.BaseType;
            }
            return default;
        }
    }

    /// <summary>
    /// Exposes the protected FormMixer members that own mixer-element list management, which is
    /// where that responsibility now lives (it used to be on MixerControl).
    /// </summary>
    public class TestableFormMixer : FormMixer
    {
        public Panel GetPanel1() => this.panel1;

        public MixerElement InvokeCreateMixerElement(NAudio.Wave.Asio.AsioChannelInfo info, int controlIndex) =>
            this.CreateMixerElement(info, controlIndex);

        public void InvokeCreateMixerElementEventHandlers(MixerInput mixerInput, MixerElement mixerElement) =>
            this.CreateMixerElementEventHandlers(mixerInput, mixerElement);

        public void InvokeClearGUI() => this.ClearGUI();
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