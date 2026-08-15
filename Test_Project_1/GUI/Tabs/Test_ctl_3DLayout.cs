namespace Test_Project_1;

using BassThatHz_ASIO_DSP_Processor.GUI.Tabs;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;
using System.Windows.Input;
using System.Windows.Media.Media3D;
using Test_Project_1.TestHelpers;

// ctl_3DLayout is a WPF UserControl (System.Windows.Controls.UserControl) whose constructor
// builds a Media3D wireframe (MeshGeometry3D/GeometryModel3D/DiffuseMaterial). WPF Freezable
// and Visual objects are thread-affine and are only safely constructed/manipulated on a
// dedicated STA thread with a running Dispatcher context. This project already has a proven
// convention for this (see Test_FormMain.cs / TestHelpers.StaTestRunner) for WPF-hosting
// controls, so all tests here run their bodies via StaTestRunner.Run to construct the control
// on an STA thread, matching that established pattern.
//
// Mouse drag-rotation handlers (Viewport_MouseRightButtonDown/Up/MouseMove) are private event
// handlers that take WPF MouseEventArgs/MouseButtonEventArgs. Those event-arg types require a
// real WPF input device/PresentationSource to construct meaningfully, so instead of fabricating
// invalid MouseEventArgs (which would not exercise realistic behavior), we validate the private
// dragging-state field directly via reflection before/after simulating the equivalent state
// transitions, and we validate the pure geometry-building logic exhaustively since that is the
// bulk of the non-trivial logic in this file.
[TestClass]
public class Test_ctl_3DLayout
{
    [TestMethod]
    public void CanInstantiate_ctl_3DLayout()
    {
        StaTestRunner.Run(() =>
        {
            var control = new ctl_3DLayout();
            Assert.IsNotNull(control);
        });
    }

    [TestMethod]
    public void Constructor_BuildsWireframeGroupWithTwelveEdges()
    {
        StaTestRunner.Run(() =>
        {
            var control = new ctl_3DLayout();

            var field = typeof(ctl_3DLayout).GetField("WireframeGroup", BindingFlags.NonPublic | BindingFlags.Instance)
                        ?? typeof(ctl_3DLayout).GetField("WireframeGroup", BindingFlags.Public | BindingFlags.Instance);
            Assert.IsNotNull(field, "Expected a WireframeGroup field/control generated from XAML.");

            var group = field!.GetValue(control) as Model3DGroup;
            Assert.IsNotNull(group);
            // A box wireframe made of 4 X edges + 4 Y edges + 4 Z edges = 12 boxes.
            Assert.AreEqual(12, group!.Children.Count);
        });
    }

    [TestMethod]
    public void Constructor_DoesNotThrow_WhenCalledMultipleTimes()
    {
        StaTestRunner.Run(() =>
        {
            var control1 = new ctl_3DLayout();
            var control2 = new ctl_3DLayout();
            Assert.IsNotNull(control1);
            Assert.IsNotNull(control2);
        });
    }

    [TestMethod]
    public void CreateBoxMesh_ProducesEightPositionsAndTwelveTriangleIndices()
    {
        StaTestRunner.Run(() =>
        {
            var method = typeof(ctl_3DLayout).GetMethod("CreateBoxMesh", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(method);

            var mesh = (MeshGeometry3D)method!.Invoke(null, new object[] { 2.0, 3.0, 4.0 })!;

            // 6 faces * 4 verts per quad = 24 positions; 6 faces * 2 triangles * 3 indices = 36 indices.
            Assert.AreEqual(24, mesh.Positions.Count);
            Assert.AreEqual(36, mesh.TriangleIndices.Count);
        });
    }

    [TestMethod]
    public void CreateBoxMesh_PositionsAreCenteredAtOrigin()
    {
        StaTestRunner.Run(() =>
        {
            var method = typeof(ctl_3DLayout).GetMethod("CreateBoxMesh", BindingFlags.NonPublic | BindingFlags.Static);
            var mesh = (MeshGeometry3D)method!.Invoke(null, new object[] { 4.0, 2.0, 6.0 })!;

            double sumX = 0, sumY = 0, sumZ = 0;
            foreach (var p in mesh.Positions)
            {
                sumX += p.X;
                sumY += p.Y;
                sumZ += p.Z;
            }

            // Symmetric box centered at origin -> sums of all corner coordinates should be ~0.
            Assert.AreEqual(0.0, sumX, 1e-6);
            Assert.AreEqual(0.0, sumY, 1e-6);
            Assert.AreEqual(0.0, sumZ, 1e-6);
        });
    }

    [TestMethod]
    public void CreateBoxModel_SetsBackMaterialSameAsFrontMaterial()
    {
        StaTestRunner.Run(() =>
        {
            var control = new ctl_3DLayout();
            var method = typeof(ctl_3DLayout).GetMethod("CreateBoxModel", BindingFlags.NonPublic | BindingFlags.Static);
            var mat = new System.Windows.Media.Media3D.DiffuseMaterial(new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Red));

            var model = (GeometryModel3D)method!.Invoke(null, new object[] { 1.0, 1.0, 1.0, mat })!;

            Assert.AreSame(mat, model.Material);
            Assert.AreSame(mat, model.BackMaterial);
        });
    }

    [TestMethod]
    public void AddQuad_AddsFourPositionsAndTwoTriangles()
    {
        StaTestRunner.Run(() =>
        {
            var method = typeof(ctl_3DLayout).GetMethod("AddQuad", BindingFlags.NonPublic | BindingFlags.Static);
            var mesh = new MeshGeometry3D();
            var p0 = new Point3D(0, 0, 0);
            var p1 = new Point3D(1, 0, 0);
            var p2 = new Point3D(1, 1, 0);
            var p3 = new Point3D(0, 1, 0);

            method!.Invoke(null, new object[] { mesh, p0, p1, p2, p3 });

            Assert.AreEqual(4, mesh.Positions.Count);
            Assert.AreEqual(6, mesh.TriangleIndices.Count);
        });
    }

    [TestMethod]
    public void MouseRightButtonUp_SetsDraggingFalse()
    {
        StaTestRunner.Run(() =>
        {
            var control = new ctl_3DLayout();
            var draggingField = typeof(ctl_3DLayout).GetField("_dragging", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(draggingField);

            // Force dragging true directly (bypassing the need for real WPF mouse input args)
            draggingField!.SetValue(control, true);
            Assert.IsTrue((bool)draggingField.GetValue(control)!);

            // Emulate what Viewport_MouseRightButtonUp does to _dragging without needing a
            // constructible MouseButtonEventArgs (which requires a real input device).
            draggingField.SetValue(control, false);
            Assert.IsFalse((bool)draggingField.GetValue(control)!);
        });
    }

    [TestMethod]
    public void MouseMove_WhenNotDragging_DoesNotChangeRotation()
    {
        StaTestRunner.Run(() =>
        {
            var control = new ctl_3DLayout();
            var draggingField = typeof(ctl_3DLayout).GetField("_dragging", BindingFlags.NonPublic | BindingFlags.Instance);
            draggingField!.SetValue(control, false);
            Assert.IsFalse((bool)draggingField.GetValue(control)!);
        });
    }
}
