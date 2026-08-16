using BassThatHz_ASIO_DSP_Processor.GUI.Controls;
using BassThatHz_ASIO_DSP_Processor;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Windows.Forms;
using System.Linq;
using System.Drawing;
using System.Reflection;
using System.Threading.Tasks;

namespace Test_Project_1;

[TestClass]
public class Test_StreamControl
{
    private StreamControl streamControl;

    [TestInitialize]
    public void Setup()
    {
        streamControl = new StreamControl();
    }

    [TestMethod]
    public void Constructor_InitializesEmptyFilterList()
    {
        Assert.IsNotNull(streamControl.FilterControls);
        Assert.AreEqual(0, streamControl.FilterControls.Count);
    }

    [TestMethod]
    public void Constructor_InitializesControls()
    {
        Assert.IsNotNull(streamControl.Get_Out_Volume);
        Assert.IsNotNull(streamControl.Get_In_Volume);
        Assert.IsNotNull(streamControl.Get_cboInputStream);
        Assert.IsNotNull(streamControl.Get_cboOutputStream);
        Assert.IsNotNull(streamControl.Get_btnDelete);
        Assert.IsNotNull(streamControl.Get_btn_MoveTo);
        Assert.IsNotNull(streamControl.Get_txtMoveToIndex);
    }

    [TestMethod]
    public void AddFilter_CreatesNewFilter()
    {
        bool filterAddedCalled = false;
        streamControl.FilterAdded += (s, e) => filterAddedCalled = true;
        
        // Use reflection to invoke protected method
        var addMethod = typeof(StreamControl).GetMethod("AddFilter", BindingFlags.NonPublic | BindingFlags.Instance);
        addMethod.Invoke(streamControl, null);
        
        Assert.AreEqual(1, streamControl.FilterControls.Count);
        Assert.IsTrue(filterAddedCalled);
        Assert.IsNotNull(streamControl.FilterControls[0]);
    }

    [TestMethod]
    public void DeleteFilter_RemovesFilter()
    {
        bool filterDeletedCalled = false;
        streamControl.FilterDeleted += (s, e) => filterDeletedCalled = true;
        
        // Add a filter first
        var addMethod = typeof(StreamControl).GetMethod("AddFilter", BindingFlags.NonPublic | BindingFlags.Instance);
        addMethod.Invoke(streamControl, null);
        
        var filter = streamControl.FilterControls[0];
        
        // Simulate delete button click
        filter.Get_btnDelete.PerformClick();
        
        Assert.AreEqual(0, streamControl.FilterControls.Count);
        Assert.IsTrue(filterDeletedCalled);
    }

    [TestMethod]
    public void MoveFilterUp_ChangesFilterPosition()
    {
        bool filterMovedUpCalled = false;
        streamControl.FilterMovedUp += (s, e) => filterMovedUpCalled = true;

        // Add two filters
        var addMethod = typeof(StreamControl).GetMethod("AddFilter", BindingFlags.NonPublic | BindingFlags.Instance);
        addMethod.Invoke(streamControl, null);
        addMethod.Invoke(streamControl, null);
        
        var filter2 = streamControl.FilterControls[1];
        
        // Move second filter up
        filter2.Get_btnUp.PerformClick();
        
        Assert.IsTrue(filterMovedUpCalled);
        Assert.AreEqual(filter2, streamControl.FilterControls[0]);
    }

    [TestMethod]
    public void MoveFilterDown_ChangesFilterPosition()
    {
        bool filterMovedDownCalled = false;
        streamControl.FilterMovedDown += (s, e) => filterMovedDownCalled = true;

        // Add two filters
        var addMethod = typeof(StreamControl).GetMethod("AddFilter", BindingFlags.NonPublic | BindingFlags.Instance);
        addMethod.Invoke(streamControl, null);
        addMethod.Invoke(streamControl, null);
        
        var filter1 = streamControl.FilterControls[0];
        
        // Move first filter down
        filter1.Get_btnDown.PerformClick();
        
        Assert.IsTrue(filterMovedDownCalled);
        Assert.AreEqual(filter1, streamControl.FilterControls[1]);
    }

    [TestMethod]
    public void EnableAll_EnablesAllFilters()
    {
        // Add two filters
        var addMethod = typeof(StreamControl).GetMethod("AddFilter", BindingFlags.NonPublic | BindingFlags.Instance);
        addMethod.Invoke(streamControl, null);
        addMethod.Invoke(streamControl, null);

        // Disable them first and ensure they are enabled
        foreach (var filter in streamControl.FilterControls)
        {
            filter.Get_chkEnabled.Checked = false;
            filter.Get_chkEnabled.Enabled = true; // Ensure enabled so btnEnableAll_Click can set Checked
        }

        // Use reflection to invoke protected method
        var enableAllMethod = typeof(StreamControl).GetMethod("btnEnableAll_Click", BindingFlags.NonPublic | BindingFlags.Instance);
        enableAllMethod.Invoke(streamControl, new object[] { null, EventArgs.Empty });

        Assert.IsTrue(streamControl.FilterControls.All(f => f.Get_chkEnabled.Checked));
    }

    [TestMethod]
    public void DisableAll_DisablesAllFilters()
    {
        // Add two filters
        var addMethod = typeof(StreamControl).GetMethod("AddFilter", BindingFlags.NonPublic | BindingFlags.Instance);
        addMethod.Invoke(streamControl, null);
        addMethod.Invoke(streamControl, null);

        // Enable them first and ensure they are enabled
        foreach (var filter in streamControl.FilterControls)
        {
            filter.Get_chkEnabled.Checked = true;
            filter.Get_chkEnabled.Enabled = true; // Ensure enabled so btnDisableAll_Click can set Checked
        }

        // Use reflection to invoke protected method
        var disableAllMethod = typeof(StreamControl).GetMethod("btnDisableAll_Click", BindingFlags.NonPublic | BindingFlags.Instance);
        disableAllMethod.Invoke(streamControl, new object[] { null, EventArgs.Empty });

        Assert.IsTrue(streamControl.FilterControls.All(f => !f.Get_chkEnabled.Checked));
    }

    [TestMethod]
    public void RedrawPanelItems_UpdatesFilterPositions()
    {
        // Add two filters
        var addMethod = typeof(StreamControl).GetMethod("AddFilter", BindingFlags.NonPublic | BindingFlags.Instance);
        addMethod.Invoke(streamControl, null);
        addMethod.Invoke(streamControl, null);

        var filter1 = streamControl.FilterControls[0];
        var filter2 = streamControl.FilterControls[1];

        // Set known sizes
        filter1.Size = new Size(100, 50);
        filter2.Size = new Size(100, 50);

        // Use reflection to trigger redraw
        var redrawMethod = typeof(StreamControl).GetMethod("RedrawPanelItems", BindingFlags.NonPublic | BindingFlags.Instance);
        redrawMethod.Invoke(streamControl, null);

        // Since panel1 is protected, we can only verify the relative positions
        var heightDiff = filter2.Location.Y - filter1.Location.Y;
        Assert.AreEqual(50, heightDiff); // Should be the height of filter1 (50)
        Assert.AreEqual(3, filter1.Location.Y); // First filter starts at y=3
    }

    #region REW API Tests

    //[TestMethod]
    //public async Task ImportFromREW_API_CreatesFiltersAndCrossovers()
    //{
    //    // Use reflection to invoke protected method
    //    var importMethod = typeof(StreamControl).GetMethod("ImportFromREW_API", BindingFlags.NonPublic | BindingFlags.Instance);
    //    await (Task)importMethod.Invoke(streamControl, null);
        
    //    // Note: Full testing would require mocking the REW_API responses
    //    // For now we just verify it doesn't throw
    //}

    //[TestMethod]
    //public async Task ExportToREW_API_SendsFilterData()
    //{
    //    // Add a test filter
    //    var addMethod = typeof(StreamControl).GetMethod("AddFilter", BindingFlags.NonPublic | BindingFlags.Instance);
    //    addMethod.Invoke(streamControl, null);
        
    //    // Use reflection to invoke protected method
    //    var exportMethod = typeof(StreamControl).GetMethod("ExportToREW_API", BindingFlags.NonPublic | BindingFlags.Instance);
    //    await (Task)exportMethod.Invoke(streamControl, null);
        
    //    // Note: Full testing would require verifying the REW_API calls
    //    // For now we just verify it doesn't throw
    //}

    #endregion

    #region Error Handling Tests

    [TestMethod]
    public void AddFilter_HandlesExceptions()
    {
        // Regression coverage for the CreateFilter rollback: if any step after the
        // FilterControls.Add() throws, the half-added control must not be left behind.
        // Nulling lblFilterCount makes RefreshFilterCountLabel (reached via RedrawPanelItems)
        // throw a NullReferenceException *after* the control has been added to both
        // FilterControls and panel1, which is exactly the partial-failure case.
        int initialCount = streamControl.FilterControls.Count;

        var labelField = typeof(StreamControl).GetField("lblFilterCount", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(labelField, "lblFilterCount field not found - test needs updating.");
        var originalLabel = labelField!.GetValue(streamControl);
        var panelField = typeof(StreamControl).GetField("panel1", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(panelField, "panel1 field not found - test needs updating.");
        var panel = (System.Windows.Forms.Panel)panelField!.GetValue(streamControl)!;
        int initialPanelCount = panel.Controls.Count;

        labelField.SetValue(streamControl, null);
        try
        {
            var addMethod = typeof(StreamControl).GetMethod("AddFilter", BindingFlags.NonPublic | BindingFlags.Instance);
            var ex = Assert.ThrowsExactly<TargetInvocationException>(() => addMethod!.Invoke(streamControl, null));
            Assert.IsInstanceOfType<NullReferenceException>(ex.InnerException);
        }
        finally
        {
            labelField.SetValue(streamControl, originalLabel);
        }

        Assert.AreEqual(initialCount, streamControl.FilterControls.Count, "FilterControls must be rolled back on failure.");
        Assert.AreEqual(initialPanelCount, panel.Controls.Count, "panel1 must be rolled back on failure.");
    }

    [TestMethod]
    public void EnableAll_HandlesExceptions()
    {
        // Add a filter and create invalid state
        var addMethod = typeof(StreamControl).GetMethod("AddFilter", BindingFlags.NonPublic | BindingFlags.Instance);
        addMethod.Invoke(streamControl, null);
        var filter = streamControl.FilterControls[0];
        filter.Get_chkEnabled.Enabled = false;

        // Use reflection to invoke protected method - this should not throw
        var enableAllMethod = typeof(StreamControl).GetMethod("btnEnableAll_Click", BindingFlags.NonPublic | BindingFlags.Instance);
        enableAllMethod.Invoke(streamControl, new object[] { null, EventArgs.Empty });
    }

    #endregion
}