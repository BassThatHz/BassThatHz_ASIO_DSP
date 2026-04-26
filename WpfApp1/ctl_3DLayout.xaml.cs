namespace BassThatHz_ASIO_DSP_Processor.GUI.Tabs;

#region Usings
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
#endregion

public partial class ctl_3DLayout : UserControl
{
    private bool _dragging;
    private Point _last;

    public ctl_3DLayout()
    {
        InitializeComponent();

        // Build a box centered at origin (size: width x height x depth)
        BuildWireBox(width: 4.0, height: 2.0, depth: 3.0, thickness: 0.03, color: Colors.Lime);
    }

    private void BuildWireBox(double width, double height, double depth, double thickness, Color color)
    {
        WireframeGroup.Children.Clear();

        double hx = width / 2.0;
        double hy = height / 2.0;
        double hz = depth / 2.0;

        var mat = new DiffuseMaterial(new SolidColorBrush(color));

        // 4 edges along X (length = width)
        AddEdgeX(+hy, +hz);
        AddEdgeX(+hy, -hz);
        AddEdgeX(-hy, +hz);
        AddEdgeX(-hy, -hz);

        // 4 edges along Y (length = height)
        AddEdgeY(+hx, +hz);
        AddEdgeY(+hx, -hz);
        AddEdgeY(-hx, +hz);
        AddEdgeY(-hx, -hz);

        // 4 edges along Z (length = depth)
        AddEdgeZ(+hx, +hy);
        AddEdgeZ(+hx, -hy);
        AddEdgeZ(-hx, +hy);
        AddEdgeZ(-hx, -hy);

        void AddEdgeX(double y, double z)
        {
            var m = CreateBoxModel(width, thickness, thickness, mat);
            m.Transform = new TranslateTransform3D(0, y, z);
            WireframeGroup.Children.Add(m);
        }

        void AddEdgeY(double x, double z)
        {
            var m = CreateBoxModel(thickness, height, thickness, mat);
            m.Transform = new TranslateTransform3D(x, 0, z);
            WireframeGroup.Children.Add(m);
        }

        void AddEdgeZ(double x, double y)
        {
            var m = CreateBoxModel(thickness, thickness, depth, mat);
            m.Transform = new TranslateTransform3D(x, y, 0);
            WireframeGroup.Children.Add(m);
        }
    }

    private static GeometryModel3D CreateBoxModel(double sx, double sy, double sz, Material material)
    {
        var mesh = CreateBoxMesh(sx, sy, sz);
        return new GeometryModel3D(mesh, material) { BackMaterial = material };
    }

    // Axis-aligned box centered at origin
    private static MeshGeometry3D CreateBoxMesh(double sx, double sy, double sz)
    {
        double hx = sx / 2.0;
        double hy = sy / 2.0;
        double hz = sz / 2.0;

        // 8 corners
        Point3D p000 = new(-hx, -hy, -hz);
        Point3D p001 = new(-hx, -hy, +hz);
        Point3D p010 = new(-hx, +hy, -hz);
        Point3D p011 = new(-hx, +hy, +hz);
        Point3D p100 = new(+hx, -hy, -hz);
        Point3D p101 = new(+hx, -hy, +hz);
        Point3D p110 = new(+hx, +hy, -hz);
        Point3D p111 = new(+hx, +hy, +hz);

        var mesh = new MeshGeometry3D();

        // 6 faces (each as a quad -> 2 triangles)
        AddQuad(mesh, p000, p100, p110, p010); // -Z
        AddQuad(mesh, p101, p001, p011, p111); // +Z
        AddQuad(mesh, p001, p000, p010, p011); // -X
        AddQuad(mesh, p100, p101, p111, p110); // +X
        AddQuad(mesh, p010, p110, p111, p011); // +Y
        AddQuad(mesh, p001, p101, p100, p000); // -Y

        return mesh;
    }

    private static void AddQuad(MeshGeometry3D mesh, Point3D p0, Point3D p1, Point3D p2, Point3D p3)
    {
        int i = mesh.Positions.Count;
        mesh.Positions.Add(p0);
        mesh.Positions.Add(p1);
        mesh.Positions.Add(p2);
        mesh.Positions.Add(p3);

        // Two triangles: (0,1,2) (0,2,3)
        mesh.TriangleIndices.Add(i + 0);
        mesh.TriangleIndices.Add(i + 1);
        mesh.TriangleIndices.Add(i + 2);

        mesh.TriangleIndices.Add(i + 0);
        mesh.TriangleIndices.Add(i + 2);
        mesh.TriangleIndices.Add(i + 3);
    }

    // Simple mouse rotate (right-drag)
    private void Viewport_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        _dragging = true;
        _last = e.GetPosition(this);
        Mouse.Capture((IInputElement)sender);
    }

    private void Viewport_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        _dragging = false;
        Mouse.Capture(null);
    }

    private void Viewport_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_dragging) return;

        var p = e.GetPosition(this);
        var dx = p.X - _last.X;
        var dy = p.Y - _last.Y;
        _last = p;

        rotY.Angle += dx * 0.4;
        rotX.Angle -= dy * 0.4;
    }
}