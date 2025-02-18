namespace BassThatHz_ASIO_DSP_Processor.GUI.Controls
{
    partial class DEQControl
    {

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
            Threshold = new BTH_VolumeSlider();
            btnApply = new System.Windows.Forms.Button();
            label1 = new System.Windows.Forms.Label();
            txtQ = new System.Windows.Forms.TextBox();
            txtG = new System.Windows.Forms.TextBox();
            txtF = new System.Windows.Forms.TextBox();
            label3 = new System.Windows.Forms.Label();
            label4 = new System.Windows.Forms.Label();
            label5 = new System.Windows.Forms.Label();
            mask_Attack = new System.Windows.Forms.MaskedTextBox();
            chk_PeakHoldAttack = new System.Windows.Forms.CheckBox();
            mask_Release = new System.Windows.Forms.MaskedTextBox();
            chk_PeakHoldRelease = new System.Windows.Forms.CheckBox();
            lblCompressionRatio2 = new System.Windows.Forms.Label();
            msb_CompressionRatio = new System.Windows.Forms.MaskedTextBox();
            lblCompressionRatio = new System.Windows.Forms.Label();
            msb_KneeWidth_db = new System.Windows.Forms.MaskedTextBox();
            lbl_KneeWidth_db = new System.Windows.Forms.Label();
            chkSoftKnee = new System.Windows.Forms.CheckBox();
            label2 = new System.Windows.Forms.Label();
            DynamicsApplied = new BTH_VolumeSlider();
            RefreshTimer = new System.Windows.Forms.Timer(components);
            cboDEQType = new System.Windows.Forms.ComboBox();
            cboBiquadType = new System.Windows.Forms.ComboBox();
            txtS = new System.Windows.Forms.TextBox();
            label6 = new System.Windows.Forms.Label();
            cboThresholdType = new System.Windows.Forms.ComboBox();
            SuspendLayout();
            // 
            // Threshold
            // 
            Threshold.Location = new System.Drawing.Point(196, 29);
            Threshold.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
            Threshold.Name = "Threshold";
            Threshold.Size = new System.Drawing.Size(272, 20);
            Threshold.TabIndex = 179;
            Threshold.Volume = 0.010000000000000004D;
            // 
            // btnApply
            // 
            btnApply.Location = new System.Drawing.Point(1013, 2);
            btnApply.Margin = new System.Windows.Forms.Padding(2);
            btnApply.Name = "btnApply";
            btnApply.Size = new System.Drawing.Size(76, 23);
            btnApply.TabIndex = 177;
            btnApply.Text = "Apply";
            btnApply.UseVisualStyleBackColor = true;
            btnApply.Click += btnApply_Click;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(131, 32);
            label1.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(62, 15);
            label1.TabIndex = 175;
            label1.Text = "Threshold:";
            // 
            // txtQ
            // 
            txtQ.Location = new System.Drawing.Point(347, 3);
            txtQ.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            txtQ.Name = "txtQ";
            txtQ.Size = new System.Drawing.Size(44, 23);
            txtQ.TabIndex = 185;
            txtQ.Text = "1";
            // 
            // txtG
            // 
            txtG.Location = new System.Drawing.Point(292, 4);
            txtG.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            txtG.Name = "txtG";
            txtG.Size = new System.Drawing.Size(32, 23);
            txtG.TabIndex = 184;
            txtG.Text = "0";
            // 
            // txtF
            // 
            txtF.Location = new System.Drawing.Point(196, 3);
            txtF.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            txtF.Name = "txtF";
            txtF.Size = new System.Drawing.Size(56, 23);
            txtF.TabIndex = 183;
            txtF.Text = "1000";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new System.Drawing.Point(328, 6);
            label3.Name = "label3";
            label3.Size = new System.Drawing.Size(19, 15);
            label3.TabIndex = 182;
            label3.Text = "Q:";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new System.Drawing.Point(255, 6);
            label4.Name = "label4";
            label4.Size = new System.Drawing.Size(34, 15);
            label4.TabIndex = 181;
            label4.Text = "Gain:";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new System.Drawing.Point(131, 7);
            label5.Name = "label5";
            label5.Size = new System.Drawing.Size(65, 15);
            label5.TabIndex = 180;
            label5.Text = "Frequency:";
            // 
            // mask_Attack
            // 
            mask_Attack.Location = new System.Drawing.Point(959, 2);
            mask_Attack.Mask = "#####";
            mask_Attack.Name = "mask_Attack";
            mask_Attack.Size = new System.Drawing.Size(49, 23);
            mask_Attack.TabIndex = 189;
            mask_Attack.Text = "1";
            mask_Attack.TextMaskFormat = System.Windows.Forms.MaskFormat.ExcludePromptAndLiterals;
            // 
            // chk_PeakHoldAttack
            // 
            chk_PeakHoldAttack.AutoSize = true;
            chk_PeakHoldAttack.Checked = true;
            chk_PeakHoldAttack.CheckState = System.Windows.Forms.CheckState.Checked;
            chk_PeakHoldAttack.Location = new System.Drawing.Point(893, 5);
            chk_PeakHoldAttack.Name = "chk_PeakHoldAttack";
            chk_PeakHoldAttack.Size = new System.Drawing.Size(60, 19);
            chk_PeakHoldAttack.TabIndex = 188;
            chk_PeakHoldAttack.Text = "Attack";
            chk_PeakHoldAttack.UseVisualStyleBackColor = true;
            // 
            // mask_Release
            // 
            mask_Release.Location = new System.Drawing.Point(959, 28);
            mask_Release.Mask = "#####";
            mask_Release.Name = "mask_Release";
            mask_Release.Size = new System.Drawing.Size(49, 23);
            mask_Release.TabIndex = 187;
            mask_Release.Text = "1";
            mask_Release.TextMaskFormat = System.Windows.Forms.MaskFormat.ExcludePromptAndLiterals;
            // 
            // chk_PeakHoldRelease
            // 
            chk_PeakHoldRelease.AutoSize = true;
            chk_PeakHoldRelease.Checked = true;
            chk_PeakHoldRelease.CheckState = System.Windows.Forms.CheckState.Checked;
            chk_PeakHoldRelease.Location = new System.Drawing.Point(893, 29);
            chk_PeakHoldRelease.Name = "chk_PeakHoldRelease";
            chk_PeakHoldRelease.Size = new System.Drawing.Size(65, 19);
            chk_PeakHoldRelease.TabIndex = 186;
            chk_PeakHoldRelease.Text = "Release";
            chk_PeakHoldRelease.UseVisualStyleBackColor = true;
            // 
            // lblCompressionRatio2
            // 
            lblCompressionRatio2.AutoSize = true;
            lblCompressionRatio2.Location = new System.Drawing.Point(861, 6);
            lblCompressionRatio2.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            lblCompressionRatio2.Name = "lblCompressionRatio2";
            lblCompressionRatio2.Size = new System.Drawing.Size(22, 15);
            lblCompressionRatio2.TabIndex = 192;
            lblCompressionRatio2.Text = ":10";
            // 
            // msb_CompressionRatio
            // 
            msb_CompressionRatio.Location = new System.Drawing.Point(819, 3);
            msb_CompressionRatio.Mask = "####";
            msb_CompressionRatio.Name = "msb_CompressionRatio";
            msb_CompressionRatio.Size = new System.Drawing.Size(41, 23);
            msb_CompressionRatio.TabIndex = 191;
            msb_CompressionRatio.Text = "240";
            // 
            // lblCompressionRatio
            // 
            lblCompressionRatio.AutoSize = true;
            lblCompressionRatio.Location = new System.Drawing.Point(704, 7);
            lblCompressionRatio.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            lblCompressionRatio.Name = "lblCompressionRatio";
            lblCompressionRatio.Size = new System.Drawing.Size(110, 15);
            lblCompressionRatio.TabIndex = 190;
            lblCompressionRatio.Text = "Compression Ratio:";
            // 
            // msb_KneeWidth_db
            // 
            msb_KneeWidth_db.Location = new System.Drawing.Point(647, 4);
            msb_KneeWidth_db.Mask = "##";
            msb_KneeWidth_db.Name = "msb_KneeWidth_db";
            msb_KneeWidth_db.Size = new System.Drawing.Size(30, 23);
            msb_KneeWidth_db.TabIndex = 196;
            msb_KneeWidth_db.Text = "24";
            msb_KneeWidth_db.TextMaskFormat = System.Windows.Forms.MaskFormat.ExcludePromptAndLiterals;
            // 
            // lbl_KneeWidth_db
            // 
            lbl_KneeWidth_db.AutoSize = true;
            lbl_KneeWidth_db.Location = new System.Drawing.Point(554, 8);
            lbl_KneeWidth_db.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            lbl_KneeWidth_db.Name = "lbl_KneeWidth_db";
            lbl_KneeWidth_db.Size = new System.Drawing.Size(93, 15);
            lbl_KneeWidth_db.TabIndex = 195;
            lbl_KneeWidth_db.Text = "KneeWidth (dB):";
            // 
            // chkSoftKnee
            // 
            chkSoftKnee.AutoSize = true;
            chkSoftKnee.Checked = true;
            chkSoftKnee.CheckState = System.Windows.Forms.CheckState.Checked;
            chkSoftKnee.Location = new System.Drawing.Point(475, 6);
            chkSoftKnee.Name = "chkSoftKnee";
            chkSoftKnee.Size = new System.Drawing.Size(76, 19);
            chkSoftKnee.TabIndex = 194;
            chkSoftKnee.Text = "Soft Knee";
            chkSoftKnee.UseVisualStyleBackColor = true;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new System.Drawing.Point(536, 32);
            label2.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(106, 15);
            label2.TabIndex = 198;
            label2.Text = "Dynamics Applied:";
            // 
            // DynamicsApplied
            // 
            DynamicsApplied.Location = new System.Drawing.Point(646, 30);
            DynamicsApplied.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
            DynamicsApplied.MaxDb = 100D;
            DynamicsApplied.MinDb = -100D;
            DynamicsApplied.Name = "DynamicsApplied";
            DynamicsApplied.RestPosition = 0.5D;
            DynamicsApplied.Size = new System.Drawing.Size(241, 20);
            DynamicsApplied.TabIndex = 197;
            // 
            // RefreshTimer
            // 
            RefreshTimer.Enabled = true;
            RefreshTimer.Interval = 500;
            RefreshTimer.Tick += RefreshTimer_Tick;
            // 
            // cboDEQType
            // 
            cboDEQType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cboDEQType.FormattingEnabled = true;
            cboDEQType.Location = new System.Drawing.Point(8, 3);
            cboDEQType.Margin = new System.Windows.Forms.Padding(2, 1, 2, 1);
            cboDEQType.Name = "cboDEQType";
            cboDEQType.Size = new System.Drawing.Size(120, 23);
            cboDEQType.TabIndex = 200;
            // 
            // cboBiquadType
            // 
            cboBiquadType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cboBiquadType.FormattingEnabled = true;
            cboBiquadType.Location = new System.Drawing.Point(8, 29);
            cboBiquadType.Margin = new System.Windows.Forms.Padding(2, 1, 2, 1);
            cboBiquadType.Name = "cboBiquadType";
            cboBiquadType.Size = new System.Drawing.Size(120, 23);
            cboBiquadType.TabIndex = 201;
            // 
            // txtS
            // 
            txtS.Location = new System.Drawing.Point(435, 4);
            txtS.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            txtS.Name = "txtS";
            txtS.Size = new System.Drawing.Size(33, 23);
            txtS.TabIndex = 203;
            txtS.Text = "1";
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new System.Drawing.Point(395, 7);
            label6.Name = "label6";
            label6.Size = new System.Drawing.Size(39, 15);
            label6.TabIndex = 202;
            label6.Text = "Slope:";
            // 
            // cboThresholdType
            // 
            cboThresholdType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cboThresholdType.FormattingEnabled = true;
            cboThresholdType.Location = new System.Drawing.Point(472, 27);
            cboThresholdType.Margin = new System.Windows.Forms.Padding(2, 1, 2, 1);
            cboThresholdType.Name = "cboThresholdType";
            cboThresholdType.Size = new System.Drawing.Size(58, 23);
            cboThresholdType.TabIndex = 205;
            // 
            // DEQControl
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(cboThresholdType);
            Controls.Add(txtS);
            Controls.Add(label6);
            Controls.Add(cboBiquadType);
            Controls.Add(cboDEQType);
            Controls.Add(label2);
            Controls.Add(DynamicsApplied);
            Controls.Add(msb_KneeWidth_db);
            Controls.Add(lbl_KneeWidth_db);
            Controls.Add(chkSoftKnee);
            Controls.Add(lblCompressionRatio2);
            Controls.Add(msb_CompressionRatio);
            Controls.Add(lblCompressionRatio);
            Controls.Add(mask_Attack);
            Controls.Add(chk_PeakHoldAttack);
            Controls.Add(mask_Release);
            Controls.Add(chk_PeakHoldRelease);
            Controls.Add(txtQ);
            Controls.Add(txtG);
            Controls.Add(txtF);
            Controls.Add(label3);
            Controls.Add(label4);
            Controls.Add(label5);
            Controls.Add(Threshold);
            Controls.Add(btnApply);
            Controls.Add(label1);
            Margin = new System.Windows.Forms.Padding(2, 1, 2, 1);
            Name = "DEQControl";
            Size = new System.Drawing.Size(1090, 52);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        protected BTH_VolumeSlider Threshold;
        protected System.Windows.Forms.Button btnApply;
        protected System.Windows.Forms.Label label1;
        protected System.Windows.Forms.TextBox txtQ;
        protected System.Windows.Forms.TextBox txtG;
        protected System.Windows.Forms.TextBox txtF;
        protected System.Windows.Forms.Label label3;
        protected System.Windows.Forms.Label label4;
        protected System.Windows.Forms.Label label5;
        protected System.Windows.Forms.MaskedTextBox mask_Attack;
        protected System.Windows.Forms.CheckBox chk_PeakHoldAttack;
        protected System.Windows.Forms.MaskedTextBox mask_Release;
        protected System.Windows.Forms.CheckBox chk_PeakHoldRelease;
        protected System.Windows.Forms.Label lblCompressionRatio2;
        protected System.Windows.Forms.MaskedTextBox msb_CompressionRatio;
        protected System.Windows.Forms.Label lblCompressionRatio;
        protected System.Windows.Forms.MaskedTextBox msb_KneeWidth_db;
        protected System.Windows.Forms.Label lbl_KneeWidth_db;
        protected System.Windows.Forms.CheckBox chkSoftKnee;
        protected System.Windows.Forms.Label label2;
        protected System.Windows.Forms.Timer RefreshTimer;
        protected BTH_VolumeSlider DynamicsApplied;
        protected System.Windows.Forms.ComboBox cboDEQType;
        protected System.Windows.Forms.ComboBox cboBiquadType;
        protected System.Windows.Forms.TextBox txtS;
        protected System.Windows.Forms.Label label6;
        protected System.Windows.Forms.ComboBox cboThresholdType;
        private System.ComponentModel.IContainer components;
    }
}
