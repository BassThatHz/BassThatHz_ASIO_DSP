namespace BassThatHz_ASIO_DSP_Processor.GUI.Controls.Filters
{
    partial class MixerElement
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
            chkChannel = new System.Windows.Forms.CheckBox();
            label1 = new System.Windows.Forms.Label();
            txtChAttenuation = new System.Windows.Forms.TextBox();
            txtStreamAttenuation = new System.Windows.Forms.TextBox();
            label2 = new System.Windows.Forms.Label();
            SuspendLayout();
            // 
            // chkChannel
            // 
            chkChannel.AutoSize = true;
            chkChannel.Location = new System.Drawing.Point(7, 0);
            chkChannel.Margin = new System.Windows.Forms.Padding(1, 1, 1, 1);
            chkChannel.Name = "chkChannel";
            chkChannel.Size = new System.Drawing.Size(122, 19);
            chkChannel.TabIndex = 0;
            chkChannel.Text = "(0) Channel Name";
            chkChannel.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(7, 22);
            label1.Margin = new System.Windows.Forms.Padding(1, 0, 1, 0);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(91, 15);
            label1.TabIndex = 2;
            label1.Text = "Ch Attenuation:";
            // 
            // txtChAttenuation
            // 
            txtChAttenuation.Location = new System.Drawing.Point(110, 17);
            txtChAttenuation.Margin = new System.Windows.Forms.Padding(1, 1, 1, 1);
            txtChAttenuation.Name = "txtChAttenuation";
            txtChAttenuation.Size = new System.Drawing.Size(60, 23);
            txtChAttenuation.TabIndex = 3;
            txtChAttenuation.Text = "-6.051";
            // 
            // txtStreamAttenuation
            // 
            txtStreamAttenuation.Location = new System.Drawing.Point(309, 16);
            txtStreamAttenuation.Margin = new System.Windows.Forms.Padding(1);
            txtStreamAttenuation.Name = "txtStreamAttenuation";
            txtStreamAttenuation.Size = new System.Drawing.Size(60, 23);
            txtStreamAttenuation.TabIndex = 5;
            txtStreamAttenuation.Text = "-6.0";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new System.Drawing.Point(192, 19);
            label2.Margin = new System.Windows.Forms.Padding(1, 0, 1, 0);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(113, 15);
            label2.TabIndex = 4;
            label2.Text = "Stream Attenuation:";
            // 
            // MixerElement
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(txtStreamAttenuation);
            Controls.Add(label2);
            Controls.Add(txtChAttenuation);
            Controls.Add(label1);
            Controls.Add(chkChannel);
            Margin = new System.Windows.Forms.Padding(1, 1, 1, 1);
            Name = "MixerElement";
            Size = new System.Drawing.Size(375, 41);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        public System.Windows.Forms.CheckBox chkChannel;
        public System.Windows.Forms.Label label1;
        public System.Windows.Forms.TextBox txtChAttenuation;
        public System.Windows.Forms.TextBox txtStreamAttenuation;
        public System.Windows.Forms.Label label2;
    }
}
