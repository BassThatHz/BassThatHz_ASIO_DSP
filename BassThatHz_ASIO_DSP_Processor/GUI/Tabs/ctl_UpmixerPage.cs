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

            var Local_Layout = new ctl_3DLayout();
            try
            {
                this.elementHost3D.Child = Local_Layout;
                _layout = Local_Layout;
            }
            catch
            {
                //DEFECT FIX: the field used to be assigned BEFORE the host assignment, so a failure
                //here abandoned a fully constructed WPF control without detaching or disposing it.
                //Build into a local, publish only on success, and clean the local up on failure.
                _layout = null;
                if (Local_Layout is IDisposable Local_Disposable)
                {
                    try
                    {
                        Local_Disposable.Dispose();
                    }
                    catch (Exception ex)
                    {
                        BassThatHz_ASIO_DSP_Processor.Debug.ReportSwallowed(ex);
                    }
                }
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
            catch (Exception ex)
            {
                // Best effort detach; recorded rather than silently discarded.
                BassThatHz_ASIO_DSP_Processor.Debug.ReportSwallowed(ex);
            }

            //NOTE: ctl_3DLayout is a WPF UserControl and does not implement IDisposable today, so
            //this branch is defensive only - kept in case the layout gains unmanaged resources.
            if (_layout is IDisposable d)
            {
                try
                {
                    d.Dispose();
                }
                catch (Exception ex)
                {
                    //DEFECT FIX: was 'catch { }' - a failed Dispose hid a real resource leak.
                    BassThatHz_ASIO_DSP_Processor.Debug.ReportSwallowed(ex);
                }
            }

            _layout = null;
        }
    }
}
