namespace BassThatHz_ASIO_DSP_Processor.GUI.Tabs
{
    using System.Windows.Forms;

    public partial class ctl_UpmixerPage : UserControl
    {
        public ctl_UpmixerPage()
        {
            InitializeComponent();
            var ctl_3DLayout1 = new ctl_3DLayout();
            this.elementHost3D.Child = ctl_3DLayout1;
        }
    }
}
