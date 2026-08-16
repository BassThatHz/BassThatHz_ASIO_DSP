#nullable enable

namespace BassThatHz_ASIO_DSP_Processor;

#region Usings
using System;
using System.IO;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Windows.Forms;
#endregion

/// <summary>
///  BassThatHz ASIO DSP Processor Engine
///  Copyright (c) 2026 BassThatHz
///
/// Permission is hereby granted to use this software
/// and associated documentation files (the "Software"),
/// for educational purposess, scientific purposess or private purposess
/// or as part of an open-source community project,
/// (and NOT for commerical use or resale in substaintial part or whole without prior authorization)
/// and all copies of the Software subject to the following conditions:
///
/// The copyright notice and this permission notice shall be included in all
/// copies or substantial portions of the Software.
//
/// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
/// IMPLIED, INCLUDING BUT NOT LIMITED TO FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
/// IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
/// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
/// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
/// SOFTWARE. ENFORCEABLE PORTIONS SHALL REMAIN IF NOT FOUND CONTRARY UNDER LAW.
/// </summary>
public static class Debug
{
    #region Interactive Dialog Suppression

    /// <summary>
    /// True when this process was detected, once at type-initialization time, as a
    /// non-interactive host (no interactive desktop, or a unit-test runner such as
    /// testhost.exe / vstest.console.exe). Probed exactly once; never re-evaluated.
    /// </summary>
    public static readonly bool IsNonInteractiveContext = DetectNonInteractiveContext();

    private static bool SuppressInteractiveDialogsField = IsNonInteractiveContext;

    /// <summary>
    /// When true, no modal dialog is ever displayed, no error-report file is written and
    /// <see cref="Error(Exception)"/> never rethrows. Defaults to
    /// <see cref="IsNonInteractiveContext"/> so that automated runs can never block, and is
    /// settable so tests (or the app) can force the behaviour deterministically.
    /// </summary>
    public static bool SuppressInteractiveDialogs
    {
        get => SuppressInteractiveDialogsField;
        set => SuppressInteractiveDialogsField = value;
    }

    /// <summary>
    /// The most recent exception passed to <see cref="Error(Exception)"/>. Recorded even when
    /// dialogs are suppressed so that the error is observable rather than silently lost.
    /// </summary>
    public static Exception? LastError { get; set; }

    /// <summary>
    /// Raised for every exception passed to <see cref="Error(Exception)"/>, whether or not
    /// dialogs are suppressed. Subscriber exceptions are swallowed so a faulty listener can
    /// never mask the original error.
    /// </summary>
    public static event Action<Exception>? ErrorReported;

    /// <summary>
    /// The most recent exception passed to <see cref="ReportSwallowed(Exception)"/>.
    /// </summary>
    public static Exception? LastSwallowedError { get; set; }

    /// <summary>
    /// Raised for every exception passed to <see cref="ReportSwallowed(Exception)"/>.
    /// </summary>
    public static event Action<Exception>? SwallowedErrorReported;

    /// <summary>
    /// Records a deliberately-swallowed, non-fatal exception. Never shows UI and never rethrows,
    /// so it is safe from finalizers, Dispose paths and real-time threads - but unlike a bare
    /// <c>_ = ex;</c> the error remains observable to tests and to a debugger breakpoint.
    /// </summary>
    /// <param name="ex">The swallowed exception.</param>
    public static void ReportSwallowed(Exception ex)
    {
        LastSwallowedError = ex;
        try
        {
            SwallowedErrorReported?.Invoke(ex);
        }
        catch (Exception ex2)
        {
            _ = ex2; //A faulty subscriber must never turn a swallowed error into a real one.
        }
    }

    /// <summary>
    /// Probes the hosting environment once to decide whether interactive dialogs are safe.
    /// Combines: the interactive-desktop flag, the process name, the entry assembly name and
    /// the presence of a loaded unit-test framework assembly.
    /// </summary>
    private static bool DetectNonInteractiveContext()
    {
        try
        {
            //No interactive desktop (service, CI agent, session 0) -> a modal dialog would hang forever.
            if (!Environment.UserInteractive)
                return true;

            string? Local_ProcessName = null;
            try
            {
                using var Local_Process = System.Diagnostics.Process.GetCurrentProcess();
                Local_ProcessName = Local_Process.ProcessName;
            }
            catch (Exception ex)
            {
                _ = ex; //Process name is only a hint; fall through to the other probes.
            }

            if (IsTestHostName(Local_ProcessName))
                return true;

            if (IsTestHostName(Assembly.GetEntryAssembly()?.GetName().Name))
                return true;

            //A loaded test framework is the most reliable signal that we are inside a test run.
            var Local_Assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < Local_Assemblies.Length; i++)
            {
                var Local_Name = Local_Assemblies[i].GetName().Name;
                if (Local_Name is null)
                    continue;

                if (Local_Name.StartsWith("Microsoft.VisualStudio.TestPlatform", StringComparison.OrdinalIgnoreCase)
                    || Local_Name.StartsWith("Microsoft.VisualStudio.TestTools", StringComparison.OrdinalIgnoreCase)
                    || Local_Name.StartsWith("Microsoft.TestPlatform", StringComparison.OrdinalIgnoreCase)
                    || Local_Name.StartsWith("MSTest", StringComparison.OrdinalIgnoreCase)
                    || Local_Name.StartsWith("nunit.framework", StringComparison.OrdinalIgnoreCase)
                    || Local_Name.StartsWith("xunit.", StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _ = ex;
            //Detection must never be the thing that breaks the app; assume interactive.
            return false;
        }
    }

    /// <summary>
    /// Returns true when the supplied process/assembly name looks like a unit-test runner host.
    /// </summary>
    private static bool IsTestHostName(string? name)
    {
        if (string.IsNullOrEmpty(name))
            return false;

        return name!.StartsWith("testhost", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("vstest.console", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("te.processhost", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("datacollector", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("ReSharperTestRunner", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("nCrunch", StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Shared Dialog Helpers

    /// <summary>
    /// Suppression-aware replacement for MessageBox.Show(text).
    /// Returns <paramref name="suppressedResult"/> without displaying anything when
    /// <see cref="SuppressInteractiveDialogs"/> is set.
    /// </summary>
    public static DialogResult ShowMessage(string text, DialogResult suppressedResult = DialogResult.OK)
    {
        if (SuppressInteractiveDialogs)
            return suppressedResult;

        return MessageBox.Show(text);
    }

    /// <summary>
    /// Suppression-aware replacement for MessageBox.Show(text, caption).
    /// </summary>
    public static DialogResult ShowMessage(string text, string caption, DialogResult suppressedResult = DialogResult.OK)
    {
        if (SuppressInteractiveDialogs)
            return suppressedResult;

        return MessageBox.Show(text, caption);
    }

    /// <summary>
    /// Suppression-aware replacement for MessageBox.Show(text, caption, buttons).
    /// <paramref name="suppressedResult"/> must be the conservative "do nothing" answer so the
    /// caller takes the safe branch when running non-interactively.
    /// </summary>
    public static DialogResult ShowMessage(string text, string caption, MessageBoxButtons buttons,
                                           DialogResult suppressedResult)
    {
        if (SuppressInteractiveDialogs)
            return suppressedResult;

        return MessageBox.Show(text, caption, buttons);
    }

    /// <summary>
    /// Suppression-aware replacement for MessageBox.Show(text, caption, buttons, icon).
    /// </summary>
    public static DialogResult ShowMessage(string text, string caption, MessageBoxButtons buttons,
                                           MessageBoxIcon icon, DialogResult suppressedResult)
    {
        if (SuppressInteractiveDialogs)
            return suppressedResult;

        return MessageBox.Show(text, caption, buttons, icon);
    }

    /// <summary>
    /// Suppression-aware replacement for Form.ShowDialog(). Does nothing and returns
    /// <paramref name="suppressedResult"/> when dialogs are suppressed.
    /// </summary>
    public static DialogResult ShowDialogSafe(Form? form, DialogResult suppressedResult = DialogResult.Cancel)
    {
        if (form is null)
            return suppressedResult;

        if (SuppressInteractiveDialogs)
            return suppressedResult;

        return form.ShowDialog();
    }

    /// <summary>
    /// Suppression-aware replacement for CommonDialog.ShowDialog(owner) (file dialogs etc.).
    /// </summary>
    public static DialogResult ShowDialogSafe(CommonDialog? dialog, IWin32Window? owner,
                                              DialogResult suppressedResult = DialogResult.Cancel)
    {
        if (dialog is null)
            return suppressedResult;

        if (SuppressInteractiveDialogs)
            return suppressedResult;

        return dialog.ShowDialog(owner);
    }

    /// <summary>
    /// Suppression-aware replacement for Form.Show(). Does nothing when dialogs are suppressed.
    /// </summary>
    public static void ShowFormSafe(Form? form)
    {
        if (form is null || SuppressInteractiveDialogs)
            return;

        form.Show();
    }

    /// <summary>
    /// Suppression-aware replacement for Form.Show(owner) (modeless, owned).
    /// </summary>
    public static void ShowFormSafe(Form? form, IWin32Window? owner)
    {
        if (form is null || SuppressInteractiveDialogs)
            return;

        form.Show(owner);
    }

    #endregion

    #region Error Reporting

    public static void GlobalError(Exception ex)
    {
        try
        {
            Program.ASIO.Stop(); //Try to stop ASIO (just in case it is running to prevent audio buffer underruns.)
        }
        catch (Exception ex2)
        {
            _ = ex2; //Swallow ASIO Error, we don't care at this point
        }

        _ = ex; //Set global debug breakpoint here
    }

    public static void Error(Exception ex)
    {
        //Always record first so the error is observable even when dialogs are suppressed.
        LastError = ex;
        try
        {
            ErrorReported?.Invoke(ex);
        }
        catch (Exception ex2)
        {
            _ = ex2; //A faulty subscriber must never mask or replace the original error.
        }

        //Non-interactive/test host: never show a modal dialog, never write a report file,
        //never rethrow. Blocking here would hang an automated run indefinitely.
        if (SuppressInteractiveDialogs)
            return;

        _ = MessageBox.Show(ex.Message + ex.StackTrace, "A fatal error has occured");

        var dialogResult = MessageBox.Show("Save detailed error report to file before closing app?",
                                    "A fatal error has occured", MessageBoxButtons.YesNo);
        if (dialogResult == DialogResult.Yes)
        {
            var UpTime = (DateTime.Now - Program.App_StartTime).ToString("c");
            var UTCDateTime = "UTC " + DateTime.UtcNow.ToString();
            var UTCFileName = UTCDateTime.Replace("/", "_").Replace(":", "_").Replace(" ", "_");
            var FileName = "ASIO_ErrorReport_" + UTCFileName + ".txt";
            var FilePath = AppDomain.CurrentDomain.BaseDirectory + @"\" + FileName;

            var ErrorMessage = "UpTime: " + UpTime + " " + UTCDateTime + " : " + ex.Message
                                + "\r\n" + ex.StackTrace + "\r\n" + ex.InnerException?.Message;

            //DEFECT FIX: writing next to the executable throws UnauthorizedAccessException under
            //Program Files, which used to replace the real fault with a bogus one from inside the
            //fatal-error handler itself.
            try
            {
                File.WriteAllText(FilePath, ErrorMessage);
                _ = MessageBox.Show(FilePath, "Error report saved to file:");
            }
            catch (Exception ex2)
            {
                ReportSwallowed(ex2);
                _ = MessageBox.Show("Could not write the error report: " + ex2.Message,
                                    "Error report NOT saved");
            }
        }

        var dialogResult2 = MessageBox.Show("Press Yes to abort the app (recommended), " +
                                        "or No to ignore the error and attempt to continue running in an errored state.",
                                        "A fatal error has occured", MessageBoxButtons.YesNo);
        if (dialogResult2 == DialogResult.Yes)
        {
            //DEFECT FIX: 'throw ex;' reset the stack trace of every fatal error in the app, so the
            //crash report pointed at Debug.Error instead of the real fault site.
            ExceptionDispatchInfo.Capture(ex).Throw();
        }
    }

    public static void GlobalError(object? sender, FirstChanceExceptionEventArgs e)
    {
        _ = e.Exception;
        //GlobalError(e.Exception);
    }

    public static void GlobalError(object? sender, UnhandledExceptionEventArgs e)
    {
        //ExceptionObject is typed as object per the CLR contract and is not guaranteed to be an
        //Exception. A hard cast here would throw InvalidCastException from inside the last-chance
        //handler, replacing the real fault with a bogus one; wrap non-Exception payloads instead.
        if (e.ExceptionObject is Exception Local_Exception)
            GlobalError(Local_Exception);
        else
            GlobalError(new ApplicationException("Unhandled non-Exception object: "
                                                 + (e.ExceptionObject?.ToString() ?? "null")));
    }

    #endregion
}
