using System.Reflection;
using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BassThatHz_ASIO_DSP_Processor;
using Test_Project_1.TestHelpers;

namespace Test_Project_1;

[TestClass]
public class Test_FormMain
{
    #region Helpers
    /// <summary>
    /// Stops HideToSysTray from popping a real Windows balloon notification during a test run.
    /// </summary>
    protected static void SuppressTrayBalloon(FormMain form)
    {
        var Local_Field = typeof(FormMain).GetField("HasShownTrayBalloon",
                                BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(Local_Field);
        Local_Field.SetValue(form, true);
    }

    protected static void SetIsExitingToDesktop(FormMain form, bool value)
    {
        var Local_Field = typeof(FormMain).GetField("IsExitingToDesktop",
                                BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(Local_Field);
        Local_Field.SetValue(form, value);
    }

    protected static void SetField(FormMain form, string name, object value)
    {
        var Local_Field = typeof(FormMain).GetField(name,
                                BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(Local_Field, name);
        Local_Field.SetValue(form, value);
    }

    /// <summary>
    /// Drives the real FormClosing handler and hands back the args so the test can inspect Cancel.
    /// </summary>
    protected static FormClosingEventArgs InvokeFormClosing(FormMain form, CloseReason reason)
    {
        var Local_Args = new FormClosingEventArgs(reason, false);
        var Local_Method = typeof(FormMain).GetMethod("FormMain_FormClosing",
                                BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(Local_Method);
        Local_Method.Invoke(form, new object[] { form, Local_Args });
        return Local_Args;
    }

    protected static void ClickTrayMenuItem(FormMain form, string text)
    {
        foreach (ToolStripItem item in form.Get_SysTrayMenu.Items)
        {
            if (item.Text == text)
            {
                item.PerformClick();
                return;
            }
        }

        Assert.Fail("System tray menu item not found: " + text);
    }
    #endregion

    [TestMethod]
    public void CanInstantiate_FormMain()
    {
        StaTestRunner.Run(() =>
        {
            var form = new BassThatHz_ASIO_DSP_Processor.FormMain();
            Assert.IsNotNull(form);
        });
    }

    [TestMethod]
    public void SysTray_MenuExposes_OpenAndClose()
    {
        StaTestRunner.Run(() =>
        {
            using var form = new FormMain();

            var menu = form.Get_SysTrayMenu;
            Assert.IsNotNull(menu);
            Assert.AreEqual(2, menu.Items.Count);
            Assert.AreEqual("Open", menu.Items[0].Text);
            Assert.AreEqual("Close", menu.Items[1].Text);

            // The right-click menu must be attached to the tray icon itself.
            Assert.AreSame(menu, form.Get_SysTrayIcon.ContextMenuStrip);
        });
    }

    [TestMethod]
    public void SysTray_IconIsNotRegistered_UntilTheAppRuns()
    {
        StaTestRunner.Run(() =>
        {
            using var form = new FormMain();

            // Merely constructing the form must not leave an icon in the notification area.
            Assert.IsFalse(form.Get_SysTrayIcon.Visible);
        });
    }

    [TestMethod]
    public void SysTray_UserClosing_IsCancelledAndHidesToTray()
    {
        StaTestRunner.Run(() =>
        {
            using var form = new FormMain();
            SuppressTrayBalloon(form);

            var args = InvokeFormClosing(form, CloseReason.UserClosing);

            Assert.IsTrue(args.Cancel, "Closing the window must not terminate the DSP engine.");
            Assert.IsTrue(form.Get_SysTrayIcon.Visible);
            Assert.IsFalse(form.Visible);
        });
    }

    [TestMethod]
    public void SysTray_CloseCommand_PerformsARealExit()
    {
        StaTestRunner.Run(() =>
        {
            using var form = new FormMain();
            SuppressTrayBalloon(form);

            // What the tray "Close" item sets before calling Close().
            SetIsExitingToDesktop(form, true);

            var args = InvokeFormClosing(form, CloseReason.UserClosing);

            Assert.IsFalse(args.Cancel);
            Assert.IsFalse(form.Get_SysTrayIcon.Visible, "The tray icon must not outlive the app.");
        });
    }

    [TestMethod]
    public void SysTray_WindowsShutDown_IsNeverCancelled()
    {
        StaTestRunner.Run(() =>
        {
            using var form = new FormMain();
            SuppressTrayBalloon(form);

            var args = InvokeFormClosing(form, CloseReason.WindowsShutDown);

            Assert.IsFalse(args.Cancel);
            Assert.IsFalse(form.Get_SysTrayIcon.Visible);
        });
    }

    [TestMethod]
    public void SysTray_ApplicationExitCall_IsNeverCancelled()
    {
        StaTestRunner.Run(() =>
        {
            using var form = new FormMain();
            SuppressTrayBalloon(form);

            var args = InvokeFormClosing(form, CloseReason.ApplicationExitCall);

            Assert.IsFalse(args.Cancel);
        });
    }

    [TestMethod]
    public void SysTray_CloseMenuItem_SetsTheExitFlag()
    {
        StaTestRunner.Run(() =>
        {
            using var form = new FormMain();
            SuppressTrayBalloon(form);

            ClickTrayMenuItem(form, "Close");

            var Local_Field = typeof(FormMain).GetField("IsExitingToDesktop",
                                    BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(Local_Field);
            Assert.AreEqual(true, Local_Field.GetValue(form));
        });
    }

    [TestMethod]
    public void SysTray_OpenCommand_RestoresThePreviousWindowState()
    {
        StaTestRunner.Run(() =>
        {
            using var form = new FormMain();
            SuppressTrayBalloon(form);

            // Pretend the window was Normal-sized when it was sent to the tray.
            SetField(form, "PreTrayWindowState", FormWindowState.Normal);
            form.WindowState = FormWindowState.Minimized;

            ClickTrayMenuItem(form, "Open");

            Assert.IsTrue(form.Visible);
            Assert.AreEqual(FormWindowState.Normal, form.WindowState);
        });
    }

    [TestMethod]
    public void SysTray_HideToTray_RemembersAMaximizedWindow()
    {
        StaTestRunner.Run(() =>
        {
            using var form = new FormMain();
            SuppressTrayBalloon(form);

            // The form is designed Maximized; hiding then reopening must not shrink it.
            form.WindowState = FormWindowState.Maximized;
            _ = InvokeFormClosing(form, CloseReason.UserClosing);

            var Local_Field = typeof(FormMain).GetField("PreTrayWindowState",
                                    BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(Local_Field);
            Assert.AreEqual(FormWindowState.Maximized, Local_Field.GetValue(form));
        });
    }
}
