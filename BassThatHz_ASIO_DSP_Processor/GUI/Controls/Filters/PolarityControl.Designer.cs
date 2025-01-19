namespace BassThatHz_ASIO_DSP_Processor.GUI.Controls
{

    partial class PolarityControl
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
            this.cboInverted = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // cboInverted
            // 
            this.cboInverted.AutoSize = true;
            this.cboInverted.Location = new System.Drawing.Point(3, 15);
            this.cboInverted.Name = "cboInverted";
            this.cboInverted.Size = new System.Drawing.Size(65, 21);
            this.cboInverted.TabIndex = 0;
            this.cboInverted.Text = "Invert";
            this.cboInverted.UseVisualStyleBackColor = true;
            this.cboInverted.CheckedChanged += new System.EventHandler(this.cboInverted_CheckedChanged);
            // 
            // PolarityControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.cboInverted);
            this.Name = "PolarityControl";
            this.Size = new System.Drawing.Size(1247, 55);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.CheckBox cboInverted;
    }
}
