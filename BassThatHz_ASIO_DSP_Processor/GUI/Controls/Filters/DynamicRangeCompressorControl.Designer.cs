namespace BassThatHz_ASIO_DSP_Processor.GUI.Controls
{

    partial class DynamicRangeCompressorControl
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
        protected void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            btnApply = new System.Windows.Forms.Button();
            lblThreshold = new System.Windows.Forms.Label();
            chkSoftKnee = new System.Windows.Forms.CheckBox();
            msb_CompressionRatio = new System.Windows.Forms.MaskedTextBox();
            lblCompressionRatio = new System.Windows.Forms.Label();
            lblCompressionRatio2 = new System.Windows.Forms.Label();
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
            lblThreshold.Location = new System.Drawing.Point(560, 28);
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
            // msb_CompressionRatio
            // 
            msb_CompressionRatio.Location = new System.Drawing.Point(124, 21);
            msb_CompressionRatio.Mask = "####";
            msb_CompressionRatio.Name = "msb_CompressionRatio";
            msb_CompressionRatio.Size = new System.Drawing.Size(41, 23);
            msb_CompressionRatio.TabIndex = 186;
            msb_CompressionRatio.Text = "240";
            // 
            // lblCompressionRatio
            // 
            lblCompressionRatio.AutoSize = true;
            lblCompressionRatio.Location = new System.Drawing.Point(6, 24);
            lblCompressionRatio.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            lblCompressionRatio.Name = "lblCompressionRatio";
            lblCompressionRatio.Size = new System.Drawing.Size(110, 15);
            lblCompressionRatio.TabIndex = 185;
            lblCompressionRatio.Text = "Compression Ratio:";
            // 
            // lblCompressionRatio2
            // 
            lblCompressionRatio2.AutoSize = true;
            lblCompressionRatio2.Location = new System.Drawing.Point(168, 26);
            lblCompressionRatio2.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            lblCompressionRatio2.Name = "lblCompressionRatio2";
            lblCompressionRatio2.Size = new System.Drawing.Size(22, 15);
            lblCompressionRatio2.TabIndex = 187;
            lblCompressionRatio2.Text = ":10";
            // 
            // msb_AttackTime_ms
            // 
            msb_AttackTime_ms.Location = new System.Drawing.Point(305, 2);
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
            lbl_AttackTime_ms.Location = new System.Drawing.Point(193, 8);
            lbl_AttackTime_ms.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            lbl_AttackTime_ms.Name = "lbl_AttackTime_ms";
            lbl_AttackTime_ms.Size = new System.Drawing.Size(97, 15);
            lbl_AttackTime_ms.TabIndex = 188;
            lbl_AttackTime_ms.Text = "AttackTime (ms):";
            // 
            // msb_ReleaseTime_ms
            // 
            msb_ReleaseTime_ms.Location = new System.Drawing.Point(305, 27);
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
            lbl_ReleaseTime_ms.Location = new System.Drawing.Point(193, 28);
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
            Threshold.Location = new System.Drawing.Point(631, 27);
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
            label3.Location = new System.Drawing.Point(494, 6);
            label3.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            label3.Name = "label3";
            label3.Size = new System.Drawing.Size(124, 15);
            label3.TabIndex = 196;
            label3.Text = "Compression Applied:";
            // 
            // CompressionApplied
            // 
            CompressionApplied.Enabled = false;
            CompressionApplied.Location = new System.Drawing.Point(631, 2);
            CompressionApplied.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
            CompressionApplied.Name = "CompressionApplied";
            CompressionApplied.ReadOnly = false;
            CompressionApplied.Size = new System.Drawing.Size(360, 20);
            CompressionApplied.TabIndex = 195;
            CompressionApplied.TextColor = System.Drawing.Color.Black;
            CompressionApplied.VolumedB = 0D;
            // 
            // RefreshTimer
            // 
            RefreshTimer.Enabled = true;
            RefreshTimer.Interval = 500;
            RefreshTimer.Tick += RefreshTimer_Tick;
            // 
            // DynamicRangeCompressorControl
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
            Controls.Add(lblCompressionRatio2);
            Controls.Add(msb_CompressionRatio);
            Controls.Add(lblCompressionRatio);
            Controls.Add(chkSoftKnee);
            Controls.Add(btnApply);
            Controls.Add(lblThreshold);
            Name = "DynamicRangeCompressorControl";
            Size = new System.Drawing.Size(1091, 52);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        protected System.Windows.Forms.Button btnApply;
        protected System.Windows.Forms.Label lblThreshold;
        protected System.Windows.Forms.CheckBox chkSoftKnee;
        protected System.Windows.Forms.MaskedTextBox msb_CompressionRatio;
        protected System.Windows.Forms.Label lblCompressionRatio;
        protected System.Windows.Forms.Label lblCompressionRatio2;
        protected System.Windows.Forms.MaskedTextBox msb_AttackTime_ms;
        protected System.Windows.Forms.Label lbl_AttackTime_ms;
        protected System.Windows.Forms.MaskedTextBox msb_ReleaseTime_ms;
        protected System.Windows.Forms.Label lbl_ReleaseTime_ms;
        protected System.Windows.Forms.MaskedTextBox msb_KneeWidth_db;
        protected System.Windows.Forms.Label lbl_KneeWidth_db;
        protected BTH_VolumeSlider Threshold;
        protected System.Windows.Forms.Label label3;
        protected BTH_VolumeSlider CompressionApplied;
        protected System.Windows.Forms.Timer RefreshTimer;
    }
}
