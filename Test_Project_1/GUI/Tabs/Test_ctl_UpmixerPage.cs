namespace Test_Project_1;

using BassThatHz_ASIO_DSP_Processor.GUI.Tabs;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;
using System.Windows.Forms.Integration;
using Test_Project_1.TestHelpers;

// ctl_UpmixerPage is a WinForms UserControl that hosts a WPF ctl_3DLayout via an
// System.Windows.Forms.Integration.ElementHost. Because it lazily constructs a WPF
// (Media3D/Freezable) control on VisibleChanged/HandleCreated, all interaction with it must
// happen on an STA thread with a Dispatcher, matching the existing project convention used for
// other WPF-hosting controls/forms (see Test_FormMain.cs and TestHelpers.StaTestRunner).
[TestClass]
public class Test_ctl_UpmixerPage
{
    [TestMethod]
    public void CanInstantiate_ctl_UpmixerPage()
    {
        StaTestRunner.Run(() =>
        {
            var control = new ctl_UpmixerPage();
            Assert.IsNotNull(control);
        });
    }

    [TestMethod]
    public void Constructor_DoesNotEagerlyCreate3DLayout_WhenNotVisible()
    {
        StaTestRunner.Run(() =>
        {
            var control = new ctl_UpmixerPage();
            var layoutField = typeof(ctl_UpmixerPage).GetField("_layout", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(layoutField);

            // A freshly constructed, unparented control is not Visible, so the lazy 3D layout
            // should not have been created yet.
            // Note: WinForms UserControl.Visible defaults to true for a freshly constructed,
            // unparented control (it reflects the "would be visible" flag, not actual on-screen
            // visibility) - HandleCreated is what actually triggers the lazy Ensure3DLayout call,
            // and a bare `new ctl_UpmixerPage()` does not force handle creation. So the
            // meaningful assertion here is simply that the lazy layout was not created eagerly.
            var layoutValue = layoutField!.GetValue(control);
            Assert.IsNull(layoutValue);
        });
    }

    [TestMethod]
    public void Ensure3DLayout_CreatesLayoutAndAssignsElementHostChild()
    {
        StaTestRunner.Run(() =>
        {
            var control = new ctl_UpmixerPage();
            var ensureMethod = typeof(ctl_UpmixerPage).GetMethod("Ensure3DLayout", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(ensureMethod);

            ensureMethod!.Invoke(control, null);

            var layoutField = typeof(ctl_UpmixerPage).GetField("_layout", BindingFlags.NonPublic | BindingFlags.Instance);
            var layoutValue = layoutField!.GetValue(control);
            Assert.IsNotNull(layoutValue);
            Assert.IsInstanceOfType(layoutValue, typeof(ctl_3DLayout));

            var elementHostField = typeof(ctl_UpmixerPage).GetField("elementHost3D", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(elementHostField);
            var elementHost = (ElementHost)elementHostField!.GetValue(control)!;
            Assert.AreSame(layoutValue, elementHost.Child);
        });
    }

    [TestMethod]
    public void Ensure3DLayout_CalledTwice_DoesNotReplaceExistingLayout()
    {
        StaTestRunner.Run(() =>
        {
            var control = new ctl_UpmixerPage();
            var ensureMethod = typeof(ctl_UpmixerPage).GetMethod("Ensure3DLayout", BindingFlags.NonPublic | BindingFlags.Instance);
            var layoutField = typeof(ctl_UpmixerPage).GetField("_layout", BindingFlags.NonPublic | BindingFlags.Instance);

            ensureMethod!.Invoke(control, null);
            var first = layoutField!.GetValue(control);

            ensureMethod.Invoke(control, null);
            var second = layoutField.GetValue(control);

            Assert.AreSame(first, second);
        });
    }

    [TestMethod]
    public void Release3DLayout_ClearsElementHostChildAndDisposesLayout()
    {
        StaTestRunner.Run(() =>
        {
            var control = new ctl_UpmixerPage();
            var ensureMethod = typeof(ctl_UpmixerPage).GetMethod("Ensure3DLayout", BindingFlags.NonPublic | BindingFlags.Instance);
            var releaseMethod = typeof(ctl_UpmixerPage).GetMethod("Release3DLayout", BindingFlags.NonPublic | BindingFlags.Instance);
            var layoutField = typeof(ctl_UpmixerPage).GetField("_layout", BindingFlags.NonPublic | BindingFlags.Instance);
            var elementHostField = typeof(ctl_UpmixerPage).GetField("elementHost3D", BindingFlags.NonPublic | BindingFlags.Instance);

            ensureMethod!.Invoke(control, null);
            Assert.IsNotNull(layoutField!.GetValue(control));

            releaseMethod!.Invoke(control, null);

            Assert.IsNull(layoutField.GetValue(control));
            var elementHost = (ElementHost)elementHostField!.GetValue(control)!;
            Assert.IsNull(elementHost.Child);
        });
    }

    [TestMethod]
    public void Release3DLayout_WhenLayoutAlreadyNull_DoesNotThrow()
    {
        StaTestRunner.Run(() =>
        {
            var control = new ctl_UpmixerPage();
            var releaseMethod = typeof(ctl_UpmixerPage).GetMethod("Release3DLayout", BindingFlags.NonPublic | BindingFlags.Instance);

            // _layout starts null; calling release should be a safe no-op.
            releaseMethod!.Invoke(control, null);

            var layoutField = typeof(ctl_UpmixerPage).GetField("_layout", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNull(layoutField!.GetValue(control));
        });
    }

    [TestMethod]
    public void OnVisibleChanged_WhenVisibleFalse_ReleasesLayout()
    {
        StaTestRunner.Run(() =>
        {
            var control = new ctl_UpmixerPage();
            var ensureMethod = typeof(ctl_UpmixerPage).GetMethod("Ensure3DLayout", BindingFlags.NonPublic | BindingFlags.Instance);
            // Specify the exact (object, EventArgs) overload explicitly: Control also declares a
            // protected OnVisibleChanged(EventArgs) overload, which makes a name-only lookup
            // across the type hierarchy ambiguous.
            var onVisibleChangedMethod = typeof(ctl_UpmixerPage).GetMethod(
                "OnVisibleChanged",
                BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                new[] { typeof(object), typeof(System.EventArgs) },
                null);
            var layoutField = typeof(ctl_UpmixerPage).GetField("_layout", BindingFlags.NonPublic | BindingFlags.Instance);

            ensureMethod!.Invoke(control, null);
            Assert.IsNotNull(layoutField!.GetValue(control));

            // Note: a freshly constructed, unparented UserControl's Visible getter is true by
            // default (it reflects the "would be visible" flag, not actual on-screen state), so
            // we explicitly set Visible=false to exercise the "else" (release) branch of
            // OnVisibleChanged, matching how the control behaves when actually hidden at runtime.
            control.Visible = false;
            onVisibleChangedMethod!.Invoke(control, new object?[] { control, System.EventArgs.Empty });

            Assert.IsNull(layoutField.GetValue(control));
        });
    }

    [TestMethod]
    public void OnVisibleChanged_WhenVisibleTrue_EnsuresLayoutCreated()
    {
        StaTestRunner.Run(() =>
        {
            var control = new ctl_UpmixerPage();
            var onVisibleChangedMethod = typeof(ctl_UpmixerPage).GetMethod(
                "OnVisibleChanged",
                BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                new[] { typeof(object), typeof(System.EventArgs) },
                null);
            var layoutField = typeof(ctl_UpmixerPage).GetField("_layout", BindingFlags.NonPublic | BindingFlags.Instance);

            Assert.IsTrue(control.Visible);
            Assert.IsNull(layoutField!.GetValue(control));

            onVisibleChangedMethod!.Invoke(control, new object?[] { control, System.EventArgs.Empty });

            Assert.IsNotNull(layoutField.GetValue(control));
        });
    }

    [TestMethod]
    public void Disposed_ReleasesLayout()
    {
        StaTestRunner.Run(() =>
        {
            var control = new ctl_UpmixerPage();
            var ensureMethod = typeof(ctl_UpmixerPage).GetMethod("Ensure3DLayout", BindingFlags.NonPublic | BindingFlags.Instance);
            var layoutField = typeof(ctl_UpmixerPage).GetField("_layout", BindingFlags.NonPublic | BindingFlags.Instance);

            ensureMethod!.Invoke(control, null);
            Assert.IsNotNull(layoutField!.GetValue(control));

            control.Dispose();

            Assert.IsNull(layoutField.GetValue(control));
        });
    }
}
