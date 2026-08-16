namespace Test_Project_1;

using BassThatHz_ASIO_DSP_Processor;
using BassThatHz_ASIO_DSP_Processor.GUI.Tabs;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Threading;

/// <summary>
/// Regression coverage for the config-load hang.
/// <para>
/// <c>ctl_DSPConfigPage.LoadConfigRefresh</c> iterated <c>DSP_Stream.Filters</c> LIVE while
/// <c>StreamControl.btnAdd_Click</c> was inserting into that very same list (via
/// FilterCreated -> StreamControl.FilterAdded -> Filters.Insert), so <c>Count</c> grew on every
/// iteration and the loop never terminated - allocating a WinForms UserControl each pass. It threw
/// nothing, so the application simply hung on startup with memory climbing into the GBs.
/// </para>
/// <para>
/// The pre-existing <c>LoadConfigRefresh_DoesNotThrow</c> test could never have caught this: it
/// used a stream with ZERO filters, so the loop body never ran. Every test here therefore uses a
/// stream that actually HAS filters.
/// </para>
/// </summary>
[TestClass]
public class Test_ctl_DSPConfigPage_LoadRegression
{
    #region Helpers
    /// <summary>Builds a DSP_Info holding one stream with the supplied filters.</summary>
    private static List<IFilter> ArrangeStreamWithFilters()
    {
        Program.DSP_Info = new DSP_Info();
        var Local_Stream = new DSP_Stream();

        var Local_Saved = new List<IFilter>
        {
            new Polarity { FilterEnabled = true },
            new Delay { FilterEnabled = true },
            new Limiter { FilterEnabled = true },
        };

        for (int i = 0; i < Local_Saved.Count; i++)
            Local_Stream.Filters.Add(Local_Saved[i]);

        Program.DSP_Info.Streams.Add(Local_Stream);
        return Local_Saved;
    }

    /// <summary>
    /// Runs LoadConfigRefresh on a dedicated STA thread with a hard timeout, so that a regression
    /// fails the test instead of hanging (and endlessly leaking inside) the test host.
    /// </summary>
    private static void RunLoadConfigRefreshBounded(out Exception? error)
    {
        Exception? Local_Error = null;
        var Local_Thread = new Thread(() =>
        {
            try
            {
                var Local_Page = new ctl_DSPConfigPage();
                Local_Page.LoadConfigRefresh();
            }
            catch (Exception ex)
            {
                Local_Error = ex;
            }
        })
        {
            IsBackground = true,
        };
        Local_Thread.SetApartmentState(ApartmentState.STA);
        Local_Thread.Start();

        bool Local_Finished = Local_Thread.Join(TimeSpan.FromSeconds(20));
        Assert.IsTrue(Local_Finished,
            "LoadConfigRefresh did not terminate within 20s - the config-load loop is unbounded again. "
            + "This is the startup hang / unbounded memory growth regression.");

        error = Local_Error;
    }
    #endregion

    [TestMethod]
    public void LoadConfigRefresh_WithSavedFilters_Terminates()
    {
        _ = ArrangeStreamWithFilters();

        RunLoadConfigRefreshBounded(out var Local_Error);

        Assert.IsNull(Local_Error, "LoadConfigRefresh threw: " + Local_Error);
    }

    [TestMethod]
    public void LoadConfigRefresh_WithSavedFilters_DoesNotGrowTheFilterList()
    {
        var Local_Saved = ArrangeStreamWithFilters();
        int Local_ExpectedCount = Local_Saved.Count;

        RunLoadConfigRefreshBounded(out var Local_Error);
        Assert.IsNull(Local_Error, "LoadConfigRefresh threw: " + Local_Error);

        Assert.AreEqual(Local_ExpectedCount, Program.DSP_Info.Streams[0].Filters.Count,
            "The stream's filter list must contain exactly the filters that were saved - no duplicates "
            + "from the controls' auto-created default filters.");
    }

    [TestMethod]
    public void LoadConfigRefresh_KeepsTheSavedFilterInstances_InOrder()
    {
        var Local_Saved = ArrangeStreamWithFilters();

        RunLoadConfigRefreshBounded(out var Local_Error);
        Assert.IsNull(Local_Error, "LoadConfigRefresh threw: " + Local_Error);

        var Local_After = Program.DSP_Info.Streams[0].Filters;
        Assert.AreEqual(Local_Saved.Count, Local_After.Count, "Filter count changed.");

        for (int i = 0; i < Local_Saved.Count; i++)
        {
            //The control adopts the saved instance via ISetDeepClonedFilter, so type + order are
            //the contract here; asserting the concrete type catches the list being repopulated
            //with the controls' DEFAULT filters instead of the configured ones.
            Assert.AreEqual(Local_Saved[i].FilterType, Local_After[i].FilterType,
                "Filter at index " + i + " is not the saved filter type - the loaded config was "
                + "replaced by a default filter.");
        }
    }

    /// <summary>
    /// End-to-end proof against the USER'S REAL config file: deserialize the golden DSP.xml exactly
    /// the way FormMain_Shown does, then run the config page load. This is the scenario that hung
    /// the application on startup with unbounded memory growth.
    /// </summary>
    [TestMethod]
    public void LoadConfigRefresh_WithTheRealGoldenConfig_Terminates_AndKeepsEveryFilter()
    {
        var Local_Path = System.IO.Path.Combine(AppContext.BaseDirectory, "TestAssets", "DSP.xml");
        Assert.IsTrue(System.IO.File.Exists(Local_Path), "Golden DSP.xml test asset missing: " + Local_Path);

        //Exactly the app's startup pipeline (FormMain_Shown).
        var Local_Xml = System.IO.File.ReadAllText(Local_Path);
        Local_Xml = CommonFunctions.RemoveDeprecatedXMLInputTags(Local_Xml);
        var Local_Info = new ExtendedXmlSerialization.ExtendedXmlSerializer().Deserialize<DSP_Info>(Local_Xml);
        Assert.IsNotNull(Local_Info, "Golden config failed to deserialize.");

        Program.DSP_Info = Local_Info;

        //Record what the file actually contains so the assertions below cannot be vacuous.
        var Local_ExpectedPerStream = new List<int>();
        int Local_TotalFilters = 0;
        for (int s = 0; s < Local_Info.Streams.Count; s++)
        {
            Local_ExpectedPerStream.Add(Local_Info.Streams[s].Filters.Count);
            Local_TotalFilters += Local_Info.Streams[s].Filters.Count;
        }

        Assert.IsTrue(Local_Info.Streams.Count > 0, "Golden config has no streams - test would be vacuous.");
        Assert.IsTrue(Local_TotalFilters > 0, "Golden config has no filters - this test would not exercise the loop.");

        RunLoadConfigRefreshBounded(out var Local_Error);
        Assert.IsNull(Local_Error, "Loading the real golden config threw: " + Local_Error);

        for (int s = 0; s < Local_ExpectedPerStream.Count; s++)
        {
            Assert.AreEqual(Local_ExpectedPerStream[s], Program.DSP_Info.Streams[s].Filters.Count,
                "Stream " + s + " lost or gained filters while loading the real config.");
        }
    }

    [TestMethod]
    public void LoadConfigRefresh_IsIdempotent_AcrossRepeatedLoads()
    {
        var Local_Saved = ArrangeStreamWithFilters();

        //Loading twice must not accumulate filters - this is what happens on a config reload.
        RunLoadConfigRefreshBounded(out var Local_Error1);
        Assert.IsNull(Local_Error1, "First LoadConfigRefresh threw: " + Local_Error1);

        RunLoadConfigRefreshBounded(out var Local_Error2);
        Assert.IsNull(Local_Error2, "Second LoadConfigRefresh threw: " + Local_Error2);

        Assert.AreEqual(Local_Saved.Count, Program.DSP_Info.Streams[0].Filters.Count,
            "Reloading the config accumulated filters.");
    }
}
