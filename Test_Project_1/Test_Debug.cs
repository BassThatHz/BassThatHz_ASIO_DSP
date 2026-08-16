namespace Test_Project_1;

using BassThatHz_ASIO_DSP_Processor;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Runtime.ExceptionServices;

[TestClass]
public class Test_Debug
{
    [TestMethod]
    public void GlobalError_Exception_SwallowsAsioStopException_AndDoesNotThrow()
    {
        // Program.ASIO defaults to a real ASIO_Engine with no driver connected.
        // Debug.GlobalError should swallow any exception thrown by ASIO.Stop() internally.
        var ex = new InvalidOperationException("test error");

        // Should not throw, even though there is no real ASIO driver connected.
        Debug.GlobalError(ex);
    }

    [TestMethod]
    public void GlobalError_Exception_WithNullMessage_DoesNotThrow()
    {
        var ex = new Exception();
        Debug.GlobalError(ex);
    }

    [TestMethod]
    public void GlobalError_FirstChanceExceptionEventArgs_DoesNotThrow_AndDoesNotRethrow()
    {
        // GlobalError(object, FirstChanceExceptionEventArgs) currently just reads e.Exception
        // and does NOT call the Exception overload (the call is commented out in source).
        var ex = new InvalidOperationException("first chance");
        var args = new FirstChanceExceptionEventArgs(ex);

        Debug.GlobalError(null, args);
    }

    [TestMethod]
    public void GlobalError_UnhandledExceptionEventArgs_DelegatesToExceptionOverload()
    {
        var ex = new InvalidOperationException("unhandled");
        var args = new UnhandledExceptionEventArgs(ex, false);

        // This should route to GlobalError(Exception) which swallows the ASIO.Stop() exception
        // and should not throw.
        Debug.GlobalError(null, args);
    }

    [TestMethod]
    public void GlobalError_UnhandledExceptionEventArgs_WithNonExceptionObject_DoesNotThrow()
    {
        // ExceptionObject can technically be any object per the CLR contract. The implementation
        // must NOT hard-cast to Exception (that used to throw InvalidCastException from inside the
        // last-chance handler, replacing the real fault with a bogus one). Non-Exception payloads
        // are now wrapped instead.
        var args = new UnhandledExceptionEventArgs("not an exception", false);

        Debug.GlobalError(null, args);
    }

    [TestMethod]
    public void GlobalError_UnhandledExceptionEventArgs_WithNullExceptionObject_DoesNotThrow()
    {
        var args = new UnhandledExceptionEventArgs(null!, false);

        Debug.GlobalError(null, args);
    }

    [TestMethod]
    public void GlobalError_MultipleCalls_DoNotThrow()
    {
        for (int i = 0; i < 5; i++)
        {
            Debug.GlobalError(new Exception($"error {i}"));
        }
    }

    // Debug.Error(Exception) IS tested now: it auto-detects the non-interactive test host and
    // suppresses every modal dialog, so it can no longer hang an automated run.

    #region Debug.Error suppression

    [TestMethod]
    public void IsNonInteractiveContext_IsTrue_UnderTestHost()
    {
        // Auto-detection must recognise the MSTest host without any configuration.
        Assert.IsTrue(Debug.IsNonInteractiveContext,
            "Non-interactive/test-host auto-detection failed; modal dialogs could block the test run.");
    }

    [TestMethod]
    public void SuppressInteractiveDialogs_DefaultsToAutoDetectedValue()
    {
        Assert.IsTrue(Debug.SuppressInteractiveDialogs);
    }

    [TestMethod]
    public void Error_WhenSuppressed_DoesNotThrow_AndDoesNotBlock()
    {
        var Local_Previous = Debug.SuppressInteractiveDialogs;
        try
        {
            Debug.SuppressInteractiveDialogs = true;
            Debug.LastError = null;

            var Local_Ex = new InvalidOperationException("suppressed error");

            // Must return promptly; a modal dialog would hang here forever.
            var Local_Task = System.Threading.Tasks.Task.Run(() => Debug.Error(Local_Ex));
            Assert.IsTrue(Local_Task.Wait(TimeSpan.FromSeconds(15)),
                "Debug.Error blocked when interactive dialogs were suppressed.");

            Assert.AreSame(Local_Ex, Debug.LastError);
        }
        finally
        {
            Debug.SuppressInteractiveDialogs = Local_Previous;
        }
    }

    [TestMethod]
    public void Error_WhenSuppressed_RaisesErrorReportedEvent()
    {
        var Local_Previous = Debug.SuppressInteractiveDialogs;
        Exception? Local_Observed = null;
        void Local_Handler(Exception ex) => Local_Observed = ex;

        Debug.ErrorReported += Local_Handler;
        try
        {
            Debug.SuppressInteractiveDialogs = true;
            var Local_Ex = new ArgumentOutOfRangeException("param", "observable");

            Debug.Error(Local_Ex);

            Assert.AreSame(Local_Ex, Local_Observed, "The exception was silently lost.");
            Assert.AreSame(Local_Ex, Debug.LastError);
        }
        finally
        {
            Debug.ErrorReported -= Local_Handler;
            Debug.SuppressInteractiveDialogs = Local_Previous;
        }
    }

    [TestMethod]
    public void Error_WhenSuppressed_FaultySubscriber_DoesNotMaskOriginalError()
    {
        var Local_Previous = Debug.SuppressInteractiveDialogs;
        void Local_Handler(Exception ex) => throw new NotSupportedException("bad subscriber");

        Debug.ErrorReported += Local_Handler;
        try
        {
            Debug.SuppressInteractiveDialogs = true;
            var Local_Ex = new InvalidOperationException("original");

            Debug.Error(Local_Ex);

            Assert.AreSame(Local_Ex, Debug.LastError);
        }
        finally
        {
            Debug.ErrorReported -= Local_Handler;
            Debug.SuppressInteractiveDialogs = Local_Previous;
        }
    }

    [TestMethod]
    public void Error_WhenSuppressed_WritesNoErrorReportFile()
    {
        var Local_Previous = Debug.SuppressInteractiveDialogs;
        try
        {
            Debug.SuppressInteractiveDialogs = true;

            var Local_Dir = AppDomain.CurrentDomain.BaseDirectory;
            var Local_Before = System.IO.Directory.GetFiles(Local_Dir, "ASIO_ErrorReport_*.txt").Length;

            Debug.Error(new Exception("no report file please"));

            var Local_After = System.IO.Directory.GetFiles(Local_Dir, "ASIO_ErrorReport_*.txt").Length;
            Assert.AreEqual(Local_Before, Local_After);
        }
        finally
        {
            Debug.SuppressInteractiveDialogs = Local_Previous;
        }
    }

    [TestMethod]
    public void ShowMessage_WhenSuppressed_ReturnsSuppressedResult_WithoutDisplaying()
    {
        var Local_Previous = Debug.SuppressInteractiveDialogs;
        try
        {
            Debug.SuppressInteractiveDialogs = true;

            Assert.AreEqual(System.Windows.Forms.DialogResult.OK, Debug.ShowMessage("text"));
            Assert.AreEqual(System.Windows.Forms.DialogResult.OK, Debug.ShowMessage("text", "caption"));
            Assert.AreEqual(System.Windows.Forms.DialogResult.No,
                Debug.ShowMessage("text", "caption", System.Windows.Forms.MessageBoxButtons.YesNo,
                                  System.Windows.Forms.DialogResult.No));
            Assert.AreEqual(System.Windows.Forms.DialogResult.Cancel, Debug.ShowDialogSafe((System.Windows.Forms.Form?)null));
            Debug.ShowFormSafe(null); //must not throw
        }
        finally
        {
            Debug.SuppressInteractiveDialogs = Local_Previous;
        }
    }

    #endregion

    #region Debug.ReportSwallowed (observable sink for deliberate swallows)

    [TestMethod]
    public void ReportSwallowed_RecordsLastSwallowedError()
    {
        var Local_Ex = new InvalidOperationException("swallowed");
        Debug.ReportSwallowed(Local_Ex);

        Assert.AreSame(Local_Ex, Debug.LastSwallowedError);
    }

    [TestMethod]
    public void ReportSwallowed_RaisesSwallowedErrorReported()
    {
        Exception? Local_Observed = null;
        void Local_Handler(Exception ex) => Local_Observed = ex;

        Debug.SwallowedErrorReported += Local_Handler;
        try
        {
            var Local_Ex = new NotSupportedException("swallowed and observed");
            Debug.ReportSwallowed(Local_Ex);

            Assert.AreSame(Local_Ex, Local_Observed);
        }
        finally
        {
            Debug.SwallowedErrorReported -= Local_Handler;
        }
    }

    [TestMethod]
    public void ReportSwallowed_FaultySubscriber_DoesNotThrowToCaller()
    {
        void Local_Handler(Exception ex) => throw new InvalidOperationException("bad subscriber");

        Debug.SwallowedErrorReported += Local_Handler;
        try
        {
            // Must not throw: callers are Dispose paths and finalizers.
            Debug.ReportSwallowed(new Exception("original"));
        }
        finally
        {
            Debug.SwallowedErrorReported -= Local_Handler;
        }
    }

    #endregion
}
