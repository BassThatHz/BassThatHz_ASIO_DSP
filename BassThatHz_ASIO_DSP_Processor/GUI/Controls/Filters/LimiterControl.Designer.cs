namespace BassThatHz_ASIO_DSP_Processor.GUI.Controls
{

    partial class LimiterControl
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
            Limit = new BTH_VolumeSlider();
            label1 = new System.Windows.Forms.Label();
            Threshold = new BTH_VolumeSlider();
            label2 = new System.Windows.Forms.Label();
            CompressionApplied = new BTH_VolumeSlider();
            label3 = new System.Windows.Forms.Label();
            RefreshTimer = new System.Windows.Forms.Timer(components);
            chk_PeakHoldRelease = new System.Windows.Forms.CheckBox();
            mask_Release = new System.Windows.Forms.MaskedTextBox();
            mask_Attack = new System.Windows.Forms.MaskedTextBox();
            chk_PeakHoldAttack = new System.Windows.Forms.CheckBox();
            SuspendLayout();
            // 
            // btnApply
            // 
            btnApply.Location = new System.Drawing.Point(1013, 2);
            btnApply.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            btnApply.Name = "btnApply";
            btnApply.Size = new System.Drawing.Size(76, 23);
            btnApply.TabIndex = 172;
            btnApply.Text = "Apply";
            btnApply.UseVisualStyleBackColor = true;
            btnApply.Click += btnApply_Click;
            // 
            // Limit
            // 
            Limit.Location = new System.Drawing.Point(78, 26);
            Limit.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
            Limit.Name = "Limit";
            Limit.ReadOnly = false;
            Limit.Size = new System.Drawing.Size(410, 20);
            Limit.TabIndex = 165;
            Limit.TextColor = System.Drawing.Color.Black;
            Limit.Volume = 0.98855309465693886D;
            Limit.VolumedB = -0.099999999999999839D;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(9, 6);
            label1.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(62, 15);
            label1.TabIndex = 164;
            label1.Text = "Threshold:";
            // 
            // Threshold
            // 
            Threshold.Location = new System.Drawing.Point(78, 3);
            Threshold.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
            Threshold.Name = "Threshold";
            Threshold.ReadOnly = false;
            Threshold.Size = new System.Drawing.Size(410, 20);
            Threshold.TabIndex = 174;
            Threshold.TextColor = System.Drawing.Color.Black;
            Threshold.Volume = 0.10000000000000002D;
            Threshold.VolumedB = -20D;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new System.Drawing.Point(36, 29);
            label2.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(37, 15);
            label2.TabIndex = 173;
            label2.Text = "Limit:";
            // 
            // CompressionApplied
            // 
            CompressionApplied.BackColor = System.Drawing.Color.Black;
            CompressionApplied.Location = new System.Drawing.Point(501, 26);
            CompressionApplied.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
            CompressionApplied.Name = "CompressionApplied";
            CompressionApplied.ReadOnly = false;
            CompressionApplied.Size = new System.Drawing.Size(356, 20);
            CompressionApplied.TabIndex = 177;
            CompressionApplied.TextColor = System.Drawing.Color.Black;
            CompressionApplied.VolumedB = 0D;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new System.Drawing.Point(618, 6);
            label3.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            label3.Name = "label3";
            label3.Size = new System.Drawing.Size(124, 15);
            label3.TabIndex = 178;
            label3.Text = "Compression Applied:";
            // 
            // RefreshTimer
            // 
            RefreshTimer.Enabled = true;
            RefreshTimer.Interval = 500;
            RefreshTimer.Tick += RefreshTimer_Tick;
            // 
            // chk_PeakHoldRelease
            // 
            chk_PeakHoldRelease.AutoSize = true;
            chk_PeakHoldRelease.Checked = true;
            chk_PeakHoldRelease.CheckState = System.Windows.Forms.CheckState.Checked;
            chk_PeakHoldRelease.Location = new System.Drawing.Point(873, 29);
            chk_PeakHoldRelease.Name = "chk_PeakHoldRelease";
            chk_PeakHoldRelease.Size = new System.Drawing.Size(65, 19);
            chk_PeakHoldRelease.TabIndex = 179;
            chk_PeakHoldRelease.Text = "Release";
            chk_PeakHoldRelease.UseVisualStyleBackColor = true;
            chk_PeakHoldRelease.CheckedChanged += chk_PeakHoldRelease_CheckedChanged;
            // 
            // mask_Release
            // 
            mask_Release.Location = new System.Drawing.Point(939, 28);
            mask_Release.Mask = "#####";
            mask_Release.Name = "mask_Release";
            mask_Release.Size = new System.Drawing.Size(49, 23);
            mask_Release.TabIndex = 180;
            mask_Release.Text = "5";
            mask_Release.TextMaskFormat = System.Windows.Forms.MaskFormat.ExcludePromptAndLiterals;
            // 
            // mask_Attack
            // 
            mask_Attack.Location = new System.Drawing.Point(939, 3);
            mask_Attack.Mask = "#####";
            mask_Attack.Name = "mask_Attack";
            mask_Attack.Size = new System.Drawing.Size(49, 23);
            mask_Attack.TabIndex = 182;
            mask_Attack.Text = "1";
            mask_Attack.TextMaskFormat = System.Windows.Forms.MaskFormat.ExcludePromptAndLiterals;
            // 
            // chk_PeakHoldAttack
            // 
            chk_PeakHoldAttack.AutoSize = true;
            chk_PeakHoldAttack.Checked = true;
            chk_PeakHoldAttack.CheckState = System.Windows.Forms.CheckState.Checked;
            chk_PeakHoldAttack.Location = new System.Drawing.Point(873, 7);
            chk_PeakHoldAttack.Name = "chk_PeakHoldAttack";
            chk_PeakHoldAttack.Size = new System.Drawing.Size(60, 19);
            chk_PeakHoldAttack.TabIndex = 181;
            chk_PeakHoldAttack.Text = "Attack";
            chk_PeakHoldAttack.UseVisualStyleBackColor = true;
            chk_PeakHoldAttack.CheckedChanged += chk_PeakHoldAttack_CheckedChanged;
            // 
            // LimiterControl
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(mask_Attack);
            Controls.Add(chk_PeakHoldAttack);
            Controls.Add(mask_Release);
            Controls.Add(chk_PeakHoldRelease);
            Controls.Add(label3);
            Controls.Add(CompressionApplied);
            Controls.Add(Threshold);
            Controls.Add(label2);
            Controls.Add(btnApply);
            Controls.Add(Limit);
            Controls.Add(label1);
            Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            Name = "LimiterControl";
            Size = new System.Drawing.Size(1090, 52);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Button btnApply;
        public BTH_VolumeSlider Limit;
        private System.Windows.Forms.Label label1;
        public BTH_VolumeSlider Threshold;
        private System.Windows.Forms.Label label2;
        public BTH_VolumeSlider CompressionApplied;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Timer RefreshTimer;
        private System.Windows.Forms.CheckBox chk_PeakHoldRelease;
        private System.Windows.Forms.MaskedTextBox mask_Release;
        private System.Windows.Forms.MaskedTextBox mask_Attack;
        private System.Windows.Forms.CheckBox chk_PeakHoldAttack;
    }
}
