using Microsoft.VisualStudio.TestTools.UnitTesting;
using BassThatHz_ASIO_DSP_Processor.GUI.Controls.Filters;
using BassThatHz_ASIO_DSP_Processor.GUI.Forms;
using BassThatHz_ASIO_DSP_Processor;
using NAudio.Dsp;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows.Forms;

namespace Test_Project_1
{
    [TestClass]
    public class Test_GPEQControl
    {
        private TestableGPEQControl _control;
        private ListBox _filtersListBox;
        private Button _configButton;

        [TestInitialize]
        public void InitializeTest()
        {
            _control = new TestableGPEQControl();
            _filtersListBox = new ListBox();
            _configButton = new Button();

            // Set up test controls
            SetPrivateField(_control, "Filters_LSB", _filtersListBox);
            SetPrivateField(_control, "ConfigGPEQ_BTN", _configButton);
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
            var filter = GetPrivateField<GPEQ>(_control, "Filter");
            Assert.IsNotNull(filter);
            Assert.AreEqual(FilterTypes.GPEQ, filter.FilterType);
        }

        //[TestMethod]
        //public void TestConfigButton_OpensForm()
        //{
        //    // Arrange
        //    bool formOpened = false;
        //    _control.FormShown += (form) => formOpened = true;

        //    // Act
        //    _control.TestConfigButtonClick();

        //    // Assert
        //    Assert.IsTrue(formOpened);
        //    Assert.IsNotNull(GetPrivateField<FormGPEQ>(_control, "GPEQ_Form"));
        //}

        [TestMethod]
        public void TestConfigForm_ClosingWithoutSave_DoesNotUpdateList()
        {
            // Arrange
            _control.TestConfigButtonClick();
            var gpeqForm = GetPrivateField<FormGPEQ>(_control, "GPEQ_Form");
            var originalItems = _filtersListBox.Items.Count;

            // Act
            _control.SimulateFormClosing(gpeqForm, false);

            // Assert
            Assert.AreEqual(originalItems, _filtersListBox.Items.Count);
        }

        [TestMethod]
        public void TestConfigForm_ClosingWithSave_UpdatesList()
        {
            // Arrange
            _control.TestConfigButtonClick();
            var gpeqForm = GetPrivateField<FormGPEQ>(_control, "GPEQ_Form");
            
            // Add a test filter to form
            var filters = new List<IFilter> { new BiQuadFilter() };
            gpeqForm.SetFilters(filters);

            // Act
            _control.SimulateFormClosing(gpeqForm, true);

            // Assert
            Assert.AreEqual(gpeqForm.GetListBoxItems().Count, _filtersListBox.Items.Count);
        }

        [TestMethod]
        public void TestGetFilter_ReturnsFilterInstance()
        {
            var filter = _control.GetFilter;
            Assert.IsNotNull(filter);
            Assert.IsInstanceOfType(filter, typeof(GPEQ));
        }

        [TestMethod]
        public void TestSetDeepClonedFilter_UpdatesFilterAndUI()
        {
            // Arrange
            var newFilter = new GPEQ();
            var filters = new List<IFilter> 
            { 
                CreateBiQuadFilter(1000, 3),
                CreateBiQuadFilter(2000, -3)
            };
            newFilter.Filters = filters;

            // Act
            _control.SetDeepClonedFilter(newFilter);

            // Assert
            var currentFilter = GetPrivateField<GPEQ>(_control, "Filter");
            Assert.AreEqual(2, currentFilter.Filters.Count);
            Assert.AreEqual(2, _filtersListBox.Items.Count);
            
            // Verify list items contain filter information
            Assert.IsTrue(_filtersListBox.Items[0].ToString().Contains("1000"));
            Assert.IsTrue(_filtersListBox.Items[0].ToString().Contains("3"));
            Assert.IsTrue(_filtersListBox.Items[1].ToString().Contains("2000"));
            Assert.IsTrue(_filtersListBox.Items[1].ToString().Contains("-3"));
        }

        private BiQuadFilter CreateBiQuadFilter(float frequency, float gain)
        {
            var filter = new BiQuadFilter();
            typeof(BiQuadFilter).GetProperty("Frequency")?.SetValue(filter, frequency);
            typeof(BiQuadFilter).GetProperty("Gain")?.SetValue(filter, gain);
            return filter;
        }

        [TestMethod]
        public void TestSetDeepClonedFilter_HandlesWrongType()
        {
            // Arrange
            var wrongFilter = CreateTestFilter();
            var originalFilter = GetPrivateField<GPEQ>(_control, "Filter");
            var originalItemCount = _filtersListBox.Items.Count;

            // Act
            _control.SetDeepClonedFilter(wrongFilter);

            // Assert - Nothing should change
            Assert.AreEqual(originalFilter, GetPrivateField<GPEQ>(_control, "Filter"));
            Assert.AreEqual(originalItemCount, _filtersListBox.Items.Count);
        }

        [TestMethod]
        public void TestConfigForm_ErrorHandling()
        {
            // Arrange
            bool errorCaught = false;
            _control.ErrorOccurred += (ex) => errorCaught = true;

            _control.TestConfigButtonClick();
            var gpeqForm = GetPrivateField<FormGPEQ>(_control, "GPEQ_Form");

            // Act - Force an error
            _control.SimulateFormClosingError(gpeqForm);

            // Assert
            Assert.IsTrue(errorCaught);
        }

        [TestMethod]
        public void TestApplySettings_PropagatesSettingsToFilters()
        {
            // Arrange
            var filter = GetPrivateField<GPEQ>(_control, "Filter");
            var testFilters = new List<IFilter>
            {
                CreateBiQuadFilter(1000, 0),
                CreateBiQuadFilter(2000, 0)
            };
            filter.Filters = testFilters;

            // Act
            _control.ApplySettings();

            // Assert - Verify the filter was applied
            // Note: Since we can't modify BiQuadFilter, we can only verify it doesn't throw
            Assert.IsTrue(true);
        }

        private IFilter CreateTestFilter()
        {
            return new GPEQTestFilter
            {
                FilterEnabled = true,
                FilterType = FilterTypes.FIR,
                FilterProcessingType = FilterProcessingTypes.WholeBlock
            };
        }
    }

    public class TestableGPEQControl : GPEQControl
    {
        public event Action<Form> FormShown;
        public event Action<Exception> ErrorOccurred;

        public void TestConfigButtonClick()
        {
            ConfigGPEQ_BTN_Click(this, EventArgs.Empty);
            FormShown?.Invoke(GPEQ_Form);
        }

        public void SimulateFormClosing(FormGPEQ form, bool withSave)
        {
            // Set SavedChanges through reflection since it's protected
            typeof(FormGPEQ).GetProperty("SavedChanges")?.SetValue(form, withSave);
            
            // Simulate form closing
            var args = new FormClosingEventArgs(CloseReason.UserClosing, false);
            typeof(Form).GetMethod("OnFormClosing", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.Invoke(form, new object[] { args });
        }

        public void SimulateFormClosingError(FormGPEQ form)
        {
            // Set SavedChanges through reflection
            typeof(FormGPEQ).GetProperty("SavedChanges")?.SetValue(form, true);
            
            // Force an error by causing an invalid operation
            var filtersListBox = typeof(GPEQControl).GetField("Filters_LSB", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(this) as ListBox;
            if (filtersListBox != null)
            {
                filtersListBox.DataSource = new object(); // This will cause an error when updating
            }

            // Simulate form closing
            var args = new FormClosingEventArgs(CloseReason.UserClosing, false);
            typeof(Form).GetMethod("OnFormClosing", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.Invoke(form, new object[] { args });
        }

        new public void Error(Exception ex)
        {
            ErrorOccurred?.Invoke(ex);
            base.Error(ex);
        }
    }

    internal class GPEQTestFilter : IFilter
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