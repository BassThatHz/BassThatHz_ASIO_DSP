namespace BassThatHz_ASIO_DSP_Processor.GUI.Tabs
{
    using System;
    using System.Windows.Forms;

    public partial class ctl_UpmixerPage : UserControl
    {
        // Lazily-created heavy WPF layout. Created only when the control becomes visible/usable
        // Use default! to avoid nullable warnings in projects without nullable enabled.
        private ctl_3DLayout _layout = default!;

        public ctl_UpmixerPage()
        {
            InitializeComponent();
            // Defer creation of the WPF child until the control is actually shown to reduce
            // startup memory and initialization cost. Release when hidden/disposed.
            this.HandleCreated += (s, e) => { if (this.Visible) Ensure3DLayout(); };
            this.VisibleChanged += OnVisibleChanged;
            this.Disposed += (s, e) => Release3DLayout();
        }

        private void OnVisibleChanged(object? sender, EventArgs e)
        {
            if (this.Visible)
            {
                Ensure3DLayout();
            }
            else
            {
                // Release resources when not visible to free memory for other parts of app
                Release3DLayout();
            }
        }

        private void Ensure3DLayout()
        {
            if (_layout != null)
                return;

            _layout = new ctl_3DLayout();
            try
            {
                this.elementHost3D.Child = _layout;
            }
            catch
            {
                // If assignment fails, ensure we don't keep a reference to the partially-initialized layout
                _layout = null;
                throw;
            }
        }

        private void Release3DLayout()
        {
            if (_layout == null)
                return;

            // Detach the WPF child from the ElementHost so it can be garbage collected
            try
            {
                this.elementHost3D.Child = null;
            }
            catch
            {
                // ignore - best effort detach
            }

            if (_layout is IDisposable d)
            {
                try { d.Dispose(); } catch { }
            }

            _layout = null;
        }
    }
}
