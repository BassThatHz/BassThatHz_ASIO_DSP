namespace BassThatHz_ASIO_DSP_Processor.GUI.Controls
{

    partial class AntiDCControl
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
            btnApply = new System.Windows.Forms.Button();
            label1 = new System.Windows.Forms.Label();
            txtConsecutiveDCSamples = new System.Windows.Forms.TextBox();
            txtEvents = new System.Windows.Forms.TextBox();
            label2 = new System.Windows.Forms.Label();
            label3 = new System.Windows.Forms.Label();
            txtDuration = new System.Windows.Forms.TextBox();
            label4 = new System.Windows.Forms.Label();
            chkOutputMuted = new System.Windows.Forms.CheckBox();
            label5 = new System.Windows.Forms.Label();
            ClipThreshold = new BTH_VolumeSlider();
            DCThreshold = new BTH_VolumeSlider();
            label6 = new System.Windows.Forms.Label();
            SuspendLayout();
            // 
            // btnApply
            // 
            btnApply.Location = new System.Drawing.Point(1013, 2);
            btnApply.Margin = new System.Windows.Forms.Padding(2);
            btnApply.Name = "btnApply";
            btnApply.Size = new System.Drawing.Size(76, 23);
            btnApply.TabIndex = 172;
            btnApply.Text = "Apply";
            btnApply.UseVisualStyleBackColor = true;
            btnApply.Click += btnApply_Click;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(2, 6);
            label1.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(279, 15);
            label1.TabIndex = 173;
            label1.Text = "Number of consecutive DC Samples before muting:";
            // 
            // txtConsecutiveDCSamples
            // 
            txtConsecutiveDCSamples.Location = new System.Drawing.Point(296, 2);
            txtConsecutiveDCSamples.Margin = new System.Windows.Forms.Padding(2);
            txtConsecutiveDCSamples.Name = "txtConsecutiveDCSamples";
            txtConsecutiveDCSamples.Size = new System.Drawing.Size(88, 23);
            txtConsecutiveDCSamples.TabIndex = 174;
            txtConsecutiveDCSamples.Text = "42";
            // 
            // txtEvents
            // 
            txtEvents.Location = new System.Drawing.Point(583, 2);
            txtEvents.Margin = new System.Windows.Forms.Padding(2);
            txtEvents.Name = "txtEvents";
            txtEvents.Size = new System.Drawing.Size(56, 23);
            txtEvents.TabIndex = 176;
            txtEvents.Text = "1";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new System.Drawing.Point(453, 6);
            label2.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(126, 15);
            label2.TabIndex = 175;
            label2.Text = "Number of Clip events";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new System.Drawing.Point(645, 6);
            label3.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            label3.Name = "label3";
            label3.Size = new System.Drawing.Size(65, 15);
            label3.TabIndex = 177;
            label3.Text = "in duration";
            // 
            // txtDuration
            // 
            txtDuration.Location = new System.Drawing.Point(716, 2);
            txtDuration.Margin = new System.Windows.Forms.Padding(2);
            txtDuration.Name = "txtDuration";
            txtDuration.Size = new System.Drawing.Size(48, 23);
            txtDuration.TabIndex = 178;
            txtDuration.Text = "1";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new System.Drawing.Point(768, 6);
            label4.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            label4.Name = "label4";
            label4.Size = new System.Drawing.Size(113, 15);
            label4.TabIndex = 179;
            label4.Text = "(ms) before muting.";
            // 
            // chkOutputMuted
            // 
            chkOutputMuted.AutoSize = true;
            chkOutputMuted.Enabled = false;
            chkOutputMuted.Location = new System.Drawing.Point(590, 29);
            chkOutputMuted.Margin = new System.Windows.Forms.Padding(2);
            chkOutputMuted.Name = "chkOutputMuted";
            chkOutputMuted.Size = new System.Drawing.Size(387, 19);
            chkOutputMuted.TabIndex = 180;
            chkOutputMuted.Text = "Muted with {0} consecutive RMS clipped samples and {1} clip events.";
            chkOutputMuted.UseVisualStyleBackColor = true;
            chkOutputMuted.CheckedChanged += chkOutputMuted_CheckedChanged;
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new System.Drawing.Point(293, 29);
            label5.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            label5.Name = "label5";
            label5.Size = new System.Drawing.Size(114, 15);
            label5.TabIndex = 181;
            label5.Text = "Clip Peak Threshold:";
            // 
            // ClipThreshold
            // 
            ClipThreshold.Location = new System.Drawing.Point(415, 28);
            ClipThreshold.Margin = new System.Windows.Forms.Padding(2);
            ClipThreshold.Name = "ClipThreshold";
            ClipThreshold.ReadOnly = false;
            ClipThreshold.Size = new System.Drawing.Size(170, 21);
            ClipThreshold.TabIndex = 182;
            ClipThreshold.TextColor = System.Drawing.Color.Black;
            ClipThreshold.Volume = 0.999899983408545D;
            ClipThreshold.VolumedB = -0.00086877652211086163D;
            // 
            // DCThreshold
            // 
            DCThreshold.Location = new System.Drawing.Point(118, 28);
            DCThreshold.Margin = new System.Windows.Forms.Padding(2);
            DCThreshold.Name = "DCThreshold";
            DCThreshold.ReadOnly = false;
            DCThreshold.Size = new System.Drawing.Size(170, 21);
            DCThreshold.TabIndex = 184;
            DCThreshold.TextColor = System.Drawing.Color.Black;
            DCThreshold.Volume = 9.9999999999999974E-06D;
            DCThreshold.VolumedB = -100D;
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new System.Drawing.Point(4, 30);
            label6.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            label6.Name = "label6";
            label6.Size = new System.Drawing.Size(109, 15);
            label6.TabIndex = 183;
            label6.Text = "DC Peak Threshold:";
            // 
            // AntiDCControl
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(DCThreshold);
            Controls.Add(label6);
            Controls.Add(ClipThreshold);
            Controls.Add(label5);
            Controls.Add(chkOutputMuted);
            Controls.Add(label4);
            Controls.Add(txtDuration);
            Controls.Add(label3);
            Controls.Add(txtEvents);
            Controls.Add(label2);
            Controls.Add(txtConsecutiveDCSamples);
            Controls.Add(label1);
            Controls.Add(btnApply);
            Margin = new System.Windows.Forms.Padding(2);
            Name = "AntiDCControl";
            Size = new System.Drawing.Size(1091, 52);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Button btnApply;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txtConsecutiveDCSamples;
        private System.Windows.Forms.TextBox txtEvents;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox txtDuration;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.CheckBox chkOutputMuted;
        private System.Windows.Forms.Label label5;
        private BTH_VolumeSlider ClipThreshold;
        private BTH_VolumeSlider DCThreshold;
        private System.Windows.Forms.Label label6;
    }
}
