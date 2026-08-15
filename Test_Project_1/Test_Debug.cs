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
    public void GlobalError_UnhandledExceptionEventArgs_WithNonExceptionObject_Throws_InvalidCastException()
    {
        // ExceptionObject can technically be any object per the CLR contract, and the
        // implementation casts directly to Exception without a safe check.
        var args = new UnhandledExceptionEventArgs("not an exception", false);

        Assert.ThrowsExactly<InvalidCastException>(() => Debug.GlobalError(null, args));
    }

    [TestMethod]
    public void GlobalError_MultipleCalls_DoNotThrow()
    {
        for (int i = 0; i < 5; i++)
        {
            Debug.GlobalError(new Exception($"error {i}"));
        }
    }

    // Note: Debug.Error(Exception) is intentionally NOT tested here, because it calls
    // System.Windows.Forms.MessageBox.Show(...) which requires interactive UI/message-pump
    // and would hang or fail in an automated, non-interactive MSTest run.
}
