namespace BassThatHz_ASIO_DSP_Processor.GUI.Controls
{
    partial class ClassicLimiterControl
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
            components = new System.ComponentModel.Container();
            btnApply = new System.Windows.Forms.Button();
            lblThreshold = new System.Windows.Forms.Label();
            chkSoftKnee = new System.Windows.Forms.CheckBox();
            msb_AttackTime_ms = new System.Windows.Forms.MaskedTextBox();
            lbl_AttackTime_ms = new System.Windows.Forms.Label();
            msb_ReleaseTime_ms = new System.Windows.Forms.MaskedTextBox();
            lbl_ReleaseTime_ms = new System.Windows.Forms.Label();
            msb_KneeWidth_db = new System.Windows.Forms.MaskedTextBox();
            lbl_KneeWidth_db = new System.Windows.Forms.Label();
            Threshold = new BTH_VolumeSlider();
            label3 = new System.Windows.Forms.Label();
            CompressionApplied = new BTH_VolumeSlider();
            RefreshTimer = new System.Windows.Forms.Timer(components);
            SuspendLayout();
            // 
            // btnApply
            // 
            btnApply.Location = new System.Drawing.Point(1013, 2);
            btnApply.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            btnApply.Name = "btnApply";
            btnApply.Size = new System.Drawing.Size(76, 23);
            btnApply.TabIndex = 182;
            btnApply.Text = "Apply";
            btnApply.UseVisualStyleBackColor = true;
            btnApply.Click += btnApply_Click;
            // 
            // lblThreshold
            // 
            lblThreshold.AutoSize = true;
            lblThreshold.Location = new System.Drawing.Point(559, 27);
            lblThreshold.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            lblThreshold.Name = "lblThreshold";
            lblThreshold.Size = new System.Drawing.Size(62, 15);
            lblThreshold.TabIndex = 181;
            lblThreshold.Text = "Threshold:";
            // 
            // chkSoftKnee
            // 
            chkSoftKnee.AutoSize = true;
            chkSoftKnee.Checked = true;
            chkSoftKnee.CheckState = System.Windows.Forms.CheckState.Checked;
            chkSoftKnee.Location = new System.Drawing.Point(352, 8);
            chkSoftKnee.Name = "chkSoftKnee";
            chkSoftKnee.Size = new System.Drawing.Size(76, 19);
            chkSoftKnee.TabIndex = 184;
            chkSoftKnee.Text = "Soft Knee";
            chkSoftKnee.UseVisualStyleBackColor = true;
            chkSoftKnee.CheckedChanged += chkSoftKnee_CheckedChanged;
            // 
            // msb_AttackTime_ms
            // 
            msb_AttackTime_ms.Location = new System.Drawing.Point(305, 3);
            msb_AttackTime_ms.Mask = "####";
            msb_AttackTime_ms.Name = "msb_AttackTime_ms";
            msb_AttackTime_ms.Size = new System.Drawing.Size(37, 23);
            msb_AttackTime_ms.TabIndex = 189;
            msb_AttackTime_ms.Text = "99";
            msb_AttackTime_ms.TextMaskFormat = System.Windows.Forms.MaskFormat.ExcludePromptAndLiterals;
            // 
            // lbl_AttackTime_ms
            // 
            lbl_AttackTime_ms.AutoSize = true;
            lbl_AttackTime_ms.Location = new System.Drawing.Point(183, 7);
            lbl_AttackTime_ms.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            lbl_AttackTime_ms.Name = "lbl_AttackTime_ms";
            lbl_AttackTime_ms.Size = new System.Drawing.Size(97, 15);
            lbl_AttackTime_ms.TabIndex = 188;
            lbl_AttackTime_ms.Text = "AttackTime (ms):";
            // 
            // msb_ReleaseTime_ms
            // 
            msb_ReleaseTime_ms.Location = new System.Drawing.Point(305, 28);
            msb_ReleaseTime_ms.Mask = "####";
            msb_ReleaseTime_ms.Name = "msb_ReleaseTime_ms";
            msb_ReleaseTime_ms.Size = new System.Drawing.Size(37, 23);
            msb_ReleaseTime_ms.TabIndex = 191;
            msb_ReleaseTime_ms.Text = "1";
            msb_ReleaseTime_ms.TextMaskFormat = System.Windows.Forms.MaskFormat.ExcludePromptAndLiterals;
            // 
            // lbl_ReleaseTime_ms
            // 
            lbl_ReleaseTime_ms.AutoSize = true;
            lbl_ReleaseTime_ms.Location = new System.Drawing.Point(183, 29);
            lbl_ReleaseTime_ms.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            lbl_ReleaseTime_ms.Name = "lbl_ReleaseTime_ms";
            lbl_ReleaseTime_ms.Size = new System.Drawing.Size(102, 15);
            lbl_ReleaseTime_ms.TabIndex = 190;
            lbl_ReleaseTime_ms.Text = "ReleaseTime (ms):";
            // 
            // msb_KneeWidth_db
            // 
            msb_KneeWidth_db.Location = new System.Drawing.Point(453, 25);
            msb_KneeWidth_db.Mask = "##";
            msb_KneeWidth_db.Name = "msb_KneeWidth_db";
            msb_KneeWidth_db.Size = new System.Drawing.Size(30, 23);
            msb_KneeWidth_db.TabIndex = 193;
            msb_KneeWidth_db.Text = "24";
            msb_KneeWidth_db.TextMaskFormat = System.Windows.Forms.MaskFormat.ExcludePromptAndLiterals;
            // 
            // lbl_KneeWidth_db
            // 
            lbl_KneeWidth_db.AutoSize = true;
            lbl_KneeWidth_db.Location = new System.Drawing.Point(349, 26);
            lbl_KneeWidth_db.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            lbl_KneeWidth_db.Name = "lbl_KneeWidth_db";
            lbl_KneeWidth_db.Size = new System.Drawing.Size(93, 15);
            lbl_KneeWidth_db.TabIndex = 192;
            lbl_KneeWidth_db.Text = "KneeWidth (dB):";
            // 
            // Threshold
            // 
            Threshold.Location = new System.Drawing.Point(636, 26);
            Threshold.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
            Threshold.Name = "Threshold";
            Threshold.ReadOnly = false;
            Threshold.Size = new System.Drawing.Size(360, 20);
            Threshold.TabIndex = 194;
            Threshold.TextColor = System.Drawing.Color.Black;
            Threshold.Volume = 0.10000000000000002D;
            Threshold.VolumedB = -20D;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new System.Drawing.Point(500, 7);
            label3.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            label3.Name = "label3";
            label3.Size = new System.Drawing.Size(124, 15);
            label3.TabIndex = 198;
            label3.Text = "Compression Applied:";
            // 
            // CompressionApplied
            // 
            CompressionApplied.Enabled = false;
            CompressionApplied.Location = new System.Drawing.Point(636, 2);
            CompressionApplied.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
            CompressionApplied.Name = "CompressionApplied";
            CompressionApplied.ReadOnly = false;
            CompressionApplied.Size = new System.Drawing.Size(360, 20);
            CompressionApplied.TabIndex = 197;
            CompressionApplied.TextColor = System.Drawing.Color.Black;
            CompressionApplied.VolumedB = 0D;
            // 
            // RefreshTimer
            // 
            RefreshTimer.Enabled = true;
            RefreshTimer.Interval = 500;
            RefreshTimer.Tick += RefreshTimer_Tick;
            // 
            // ClassicLimiterControl
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(label3);
            Controls.Add(CompressionApplied);
            Controls.Add(Threshold);
            Controls.Add(msb_KneeWidth_db);
            Controls.Add(lbl_KneeWidth_db);
            Controls.Add(msb_ReleaseTime_ms);
            Controls.Add(lbl_ReleaseTime_ms);
            Controls.Add(msb_AttackTime_ms);
            Controls.Add(lbl_AttackTime_ms);
            Controls.Add(chkSoftKnee);
            Controls.Add(btnApply);
            Controls.Add(lblThreshold);
            Name = "ClassicLimiterControl";
            Size = new System.Drawing.Size(1091, 52);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private System.Windows.Forms.Button btnApply;
        private System.Windows.Forms.Label lblThreshold;
        private System.Windows.Forms.CheckBox chkSoftKnee;
        private System.Windows.Forms.MaskedTextBox msb_AttackTime_ms;
        private System.Windows.Forms.Label lbl_AttackTime_ms;
        private System.Windows.Forms.MaskedTextBox msb_ReleaseTime_ms;
        private System.Windows.Forms.Label lbl_ReleaseTime_ms;
        private System.Windows.Forms.MaskedTextBox msb_KneeWidth_db;
        private System.Windows.Forms.Label lbl_KneeWidth_db;
        public BTH_VolumeSlider Threshold;
        private System.Windows.Forms.Label label3;
        public BTH_VolumeSlider CompressionApplied;
        private System.Windows.Forms.Timer RefreshTimer;
    }
}