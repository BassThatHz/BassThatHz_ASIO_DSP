namespace BassThatHz_ASIO_DSP_Processor.GUI.Controls
{
    partial class BTH_VolumeLevel_MonitorControl
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        protected System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                //The cached ruler bitmap, gradient brush and ruler font are all GDI+ handles.
                this.ReleaseCachedResources();

                if (components != null)
                    components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        protected void InitializeComponent()
        {
            this.SuspendLayout();
            //
            // BTH_VolumeLevel_Monitor
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(64, 64, 64);
            this.MaxDb = 0D;
            this.MinDb = -100D;
            this.Name = "BTH_VolumeLevel_Monitor";
            this.Size = new System.Drawing.Size(404, 36);
            this.ResumeLayout(false);

        }

        #endregion
    }
}
