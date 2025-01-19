namespace BassThatHz_ASIO_DSP_Processor
{
    partial class BTH_VolumeSlider
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.lblDB = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // lblDB
            // 
            this.lblDB.AutoSize = true;
            this.lblDB.BackColor = System.Drawing.Color.Transparent;
            this.lblDB.Location = new System.Drawing.Point(221, 3);
            this.lblDB.Name = "lblDB";
            this.lblDB.Size = new System.Drawing.Size(57, 17);
            this.lblDB.TabIndex = 0;
            this.lblDB.Text = "0.00 dB";
            this.lblDB.Click += new System.EventHandler(this.lblDB_Click);
            // 
            // VolumeSlider
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.lblDB);
            this.Name = "VolumeSlider";
            this.Size = new System.Drawing.Size(508, 22);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblDB;
    }
}
