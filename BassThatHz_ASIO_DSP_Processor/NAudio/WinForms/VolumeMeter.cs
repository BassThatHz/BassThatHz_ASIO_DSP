#nullable enable

namespace NAudio.Gui
{
    using System;
    using System.ComponentModel;
    using System.Drawing;
    using System.Windows.Forms;

    /// <summary>
    /// Implements a rudimentary volume meter
    /// </summary>
    public partial class VolumeMeter : Control
    {
        private System.Drawing.SolidBrush? foregroundBrush;

        /// <summary>
        /// Basic volume meter
        /// </summary>
        public VolumeMeter()
        {
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                ControlStyles.OptimizedDoubleBuffer, true);
            MinDb = -60;
            MaxDb = 18;
            Amplitude = 0;
            Orientation = Orientation.Vertical;
            InitializeComponent();
            this.Disposed += VolumeMeter_Disposed;
            OnForeColorChanged(EventArgs.Empty);
        }

        private void VolumeMeter_Disposed(object? sender, EventArgs e)
        {
            foregroundBrush?.Dispose();
            foregroundBrush = null;
        }

        /// <summary>
        /// On Fore Color Changed
        /// </summary>
        protected override void OnForeColorChanged(EventArgs e)
        {
            // Create the new brush first then dispose the old one to avoid leaving the control
            // without a brush if construction throws. Reuse SolidBrush type so we can dispose it.
            var newBrush = new System.Drawing.SolidBrush(ForeColor);
            var oldBrush = foregroundBrush;
            foregroundBrush = newBrush;
            oldBrush?.Dispose();

            base.OnForeColorChanged(e);
        }


        protected double amplitude;

        /// <summary>
        /// Current Value
        /// </summary>
        [DefaultValue(-3.0)]
        public double Amplitude
        {
            get { return amplitude; }
            set
            {
                amplitude = value;
                this.Invalidate();
            }
        }

        /// <summary>
        /// Minimum decibels
        /// </summary>
        [DefaultValue(-60.0)]
        public double MinDb { get; set; }

        /// <summary>
        /// Maximum decibels
        /// </summary>
        [DefaultValue(18.0)]
        public double MaxDb { get; set; }

        /// <summary>
        /// Meter orientation
        /// </summary>
        [DefaultValue(Orientation.Vertical)]
        public Orientation Orientation { get; set; }

        /// <summary>
        /// Paints the volume meter
        /// </summary>
        protected override void OnPaint(PaintEventArgs pe)
        {
            // draw border
            var g = pe.Graphics;
            int w = this.Width;
            int h = this.Height;
            g.DrawRectangle(Pens.Black, 0, 0, w - 1, h - 1);

            // compute dB robustly (avoid Log10(0) -> -Infinity)
            double db;
            double amp = this.Amplitude;
            if (amp <= 0.0 || double.IsNaN(amp) || double.IsInfinity(amp))
            {
                db = MinDb;
            }
            else
            {
                db = 20.0 * Math.Log10(amp);
            }

            if (db < MinDb) db = MinDb;
            if (db > MaxDb) db = MaxDb;

            double range = MaxDb - MinDb;
            double percent = range > 0.0 ? (db - MinDb) / range : 0.0;
            if (double.IsNaN(percent) || percent <= 0.0)
            {
                percent = 0.0;
            }
            else if (percent > 1.0)
            {
                percent = 1.0;
            }

            int innerWidth = w - 2;
            int innerHeight = h - 2;

            var brush = foregroundBrush;
            if (Orientation == Orientation.Horizontal)
            {
                int fillWidth = (int)(innerWidth * percent);
                if (fillWidth > 0 && brush != null)
                {
                    g.FillRectangle(brush, 1, 1, fillWidth, innerHeight);
                }
            }
            else
            {
                int fillHeight = (int)(innerHeight * percent);
                if (fillHeight > 0 && brush != null)
                {
                    g.FillRectangle(brush, 1, h - 1 - fillHeight, innerWidth, fillHeight);
                }
            }
        }
    }
}
