namespace Test_Project_1;

using BassThatHz_ASIO_DSP_Processor;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Reflection;

/// <summary>
/// Pins the error-handling defect fixes in ASIO_Engine:
/// the AggregateException-aware "tolerated transient DSP fault" filter used by the multi-threaded
/// DSP path (Task.WaitAll wraps worker faults, so the old bare type test never matched and the
/// deliberately-tolerated stream-edit race was rethrown, killing the DSP thread).
/// </summary>
[TestClass]
public class Test_ASIO_Engine_ErrorHandling
{
    #region Helpers
    private static bool IsTolerated(Exception ex)
    {
        var Local_Method = typeof(ASIO_Engine).GetMethod("IsToleratedTransientDspFault",
                                BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
        Assert.IsNotNull(Local_Method, "IsToleratedTransientDspFault not found on ASIO_Engine.");

        return (bool)Local_Method!.Invoke(null, new object?[] { ex })!;
    }
    #endregion

    [TestMethod]
    public void IsToleratedTransientDspFault_BareIndexOutOfRange_IsTolerated()
    {
        Assert.IsTrue(IsTolerated(new IndexOutOfRangeException()));
    }

    [TestMethod]
    public void IsToleratedTransientDspFault_BareArgumentOutOfRange_IsTolerated()
    {
        Assert.IsTrue(IsTolerated(new ArgumentOutOfRangeException()));
    }

    [TestMethod]
    public void IsToleratedTransientDspFault_AggregateOfIndexOutOfRange_IsTolerated()
    {
        // This is the case the original code got wrong: Task.WaitAll always wraps.
        var Local_Aggregate = new AggregateException(new IndexOutOfRangeException(),
                                                     new ArgumentOutOfRangeException());
        Assert.IsTrue(IsTolerated(Local_Aggregate));
    }

    [TestMethod]
    public void IsToleratedTransientDspFault_NestedAggregate_IsFlattenedAndTolerated()
    {
        var Local_Inner = new AggregateException(new IndexOutOfRangeException());
        var Local_Outer = new AggregateException(Local_Inner);
        Assert.IsTrue(IsTolerated(Local_Outer));
    }

    [TestMethod]
    public void IsToleratedTransientDspFault_AggregateContainingARealFault_IsNotTolerated()
    {
        var Local_Aggregate = new AggregateException(new IndexOutOfRangeException(),
                                                     new NullReferenceException());
        Assert.IsFalse(IsTolerated(Local_Aggregate), "A real fault must still be rethrown.");
    }

    [TestMethod]
    public void IsToleratedTransientDspFault_EmptyAggregate_IsNotTolerated()
    {
        Assert.IsFalse(IsTolerated(new AggregateException()));
    }

    [TestMethod]
    public void IsToleratedTransientDspFault_UnrelatedException_IsNotTolerated()
    {
        Assert.IsFalse(IsTolerated(new InvalidOperationException()));
        Assert.IsFalse(IsTolerated(new NullReferenceException()));
    }

    #region DSP thread survival / no-UI-on-the-real-time-thread
    /// <summary>
    /// Reflection helper: forces the engine into a state where one DSP pass is guaranteed to throw
    /// a NON-tolerated exception. InputBuffer is left at its default empty jagged array while the
    /// ASIO data claims one input channel, so GetAsJaggedSamples throws InvalidOperationException.
    /// </summary>
    private static void ArmFailingDspPass(ASIO_Engine engine)
    {
        var Local_Field = typeof(ASIO_Engine).GetField("DSP_ASIO_Data",
                                BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(Local_Field, "DSP_ASIO_Data field not found.");

        var Local_Data = new NAudio.Wave.AsioAudioAvailableEventArgs(
                                [IntPtr.Zero], [IntPtr.Zero], 1, NAudio.Wave.Asio.AsioSampleType.Int32LSB);
        Local_Field!.SetValue(engine, Local_Data);
    }

    private static AutoResetEvent GetAre(ASIO_Engine engine, string name)
    {
        var Local_Field = typeof(ASIO_Engine).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(Local_Field, $"{name} field not found.");
        return (AutoResetEvent)Local_Field!.GetValue(engine)!;
    }

    /// <summary>
    /// Pins the deadlock fix: when a DSP pass throws, the completion event MUST still be signalled
    /// and the long-running DSP thread MUST survive to run further passes. Before the fix the
    /// try/catch wrapped the whole while-loop, so one transient fault permanently killed the thread
    /// and the ASIO callback (which waits with no timeout) hung forever.
    /// </summary>
    [TestMethod]
    public void DspThread_PassThrows_StillSignalsCompletion_AndSurvivesForTheNextPass()
    {
        using var Local_Engine = new ASIO_Engine();
        var Local_RunOnce = GetAre(Local_Engine, "DSP_RunOnce_ARE");
        var Local_Completed = GetAre(Local_Engine, "DSP_PassCompleted_ARE");

        ArmFailingDspPass(Local_Engine);

        //Pass 1: throws internally.
        _ = Local_RunOnce.Set();
        Assert.IsTrue(Local_Completed.WaitOne(TimeSpan.FromSeconds(5)),
            "DSP thread did not signal completion after a failing pass - the ASIO callback would hang forever.");

        //Pass 2: proves the thread was not killed by the first fault.
        ArmFailingDspPass(Local_Engine);
        _ = Local_RunOnce.Set();
        Assert.IsTrue(Local_Completed.WaitOne(TimeSpan.FromSeconds(5)),
            "DSP thread died after the first exception instead of surviving for the next pass.");
    }

    /// <summary>
    /// The DSP thread must report faults through the non-blocking, never-rethrowing sink rather
    /// than through Debug.Error (which shows modal dialogs and would deadlock the audio callback,
    /// because a finally only runs after the catch body completes).
    /// </summary>
    [TestMethod]
    public void DspThread_PassThrows_ReportsThroughNonBlockingSink()
    {
        var Local_Reported = new System.Collections.Generic.List<Exception>();
        void Handler(Exception ex)
        {
            lock (Local_Reported)
                Local_Reported.Add(ex);
        }

        Debug.SwallowedErrorReported += Handler;
        try
        {
            using var Local_Engine = new ASIO_Engine();
            var Local_RunOnce = GetAre(Local_Engine, "DSP_RunOnce_ARE");
            var Local_Completed = GetAre(Local_Engine, "DSP_PassCompleted_ARE");

            ArmFailingDspPass(Local_Engine);
            _ = Local_RunOnce.Set();
            Assert.IsTrue(Local_Completed.WaitOne(TimeSpan.FromSeconds(5)), "DSP pass did not complete.");
        }
        finally
        {
            Debug.SwallowedErrorReported -= Handler;
        }

        lock (Local_Reported)
        {
            bool Local_Found = false;
            for (int i = 0; i < Local_Reported.Count; i++)
            {
                if (Local_Reported[i] is InvalidOperationException)
                    Local_Found = true;
            }
            Assert.IsTrue(Local_Found,
                "The DSP pass fault was not routed to Debug.ReportSwallowed - it must not go to Debug.Error, "
                + "which can block a real-time thread on a modal dialog.");
        }
    }
    #endregion

    [TestMethod]
    public void BuildReverseAdjacencyMap_IsRemoved_DeadCode()
    {
        var Local_Method = typeof(ASIO_Engine).GetMethod("BuildReverseAdjacencyMap",
                                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        Assert.IsNull(Local_Method, "BuildReverseAdjacencyMap was unreferenced dead code and should stay removed.");
    }
}
