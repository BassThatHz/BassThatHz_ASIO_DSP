namespace BassThatHz_ASIO_DSP_Processor.GUI.Controls
{

    partial class SmartGainControl
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
            this.components = new System.ComponentModel.Container();
            this.txtGain = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.btnApply = new System.Windows.Forms.Button();
            this.chkPeakHold = new System.Windows.Forms.CheckBox();
            this.txtDuration = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.lblHeadroom = new System.Windows.Forms.Label();
            this.lblAppliedGain = new System.Windows.Forms.Label();
            this.lblPeakLevel = new System.Windows.Forms.Label();
            this.RefreshStats_Timer = new System.Windows.Forms.Timer(this.components);
            this.chkPeak = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // txtGain
            // 
            this.txtGain.Location = new System.Drawing.Point(138, 13);
            this.txtGain.Margin = new System.Windows.Forms.Padding(2);
            this.txtGain.MaxLength = 9;
            this.txtGain.Name = "txtGain";
            this.txtGain.Size = new System.Drawing.Size(58, 23);
            this.txtGain.TabIndex = 173;
            this.txtGain.Text = "0";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 17);
            this.label1.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(117, 15);
            this.label1.TabIndex = 172;
            this.label1.Text = "Requested Gain (dB):";
            // 
            // btnApply
            // 
            this.btnApply.Location = new System.Drawing.Point(1013, 2);
            this.btnApply.Margin = new System.Windows.Forms.Padding(2);
            this.btnApply.Name = "btnApply";
            this.btnApply.Size = new System.Drawing.Size(76, 23);
            this.btnApply.TabIndex = 174;
            this.btnApply.Text = "Apply";
            this.btnApply.UseVisualStyleBackColor = true;
            this.btnApply.Click += new System.EventHandler(this.btnApply_Click);
            // 
            // chkPeakHold
            // 
            this.chkPeakHold.AutoSize = true;
            this.chkPeakHold.Location = new System.Drawing.Point(913, 15);
            this.chkPeakHold.Margin = new System.Windows.Forms.Padding(2);
            this.chkPeakHold.Name = "chkPeakHold";
            this.chkPeakHold.Size = new System.Drawing.Size(80, 19);
            this.chkPeakHold.TabIndex = 183;
            this.chkPeakHold.Text = "Peak Hold";
            this.chkPeakHold.UseVisualStyleBackColor = true;
            this.chkPeakHold.CheckedChanged += new System.EventHandler(this.chkPeakHold_CheckedChanged);
            // 
            // txtDuration
            // 
            this.txtDuration.Location = new System.Drawing.Point(775, 13);
            this.txtDuration.Margin = new System.Windows.Forms.Padding(2);
            this.txtDuration.MaxLength = 5;
            this.txtDuration.Name = "txtDuration";
            this.txtDuration.Size = new System.Drawing.Size(69, 23);
            this.txtDuration.TabIndex = 182;
            this.txtDuration.Text = "1";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(684, 17);
            this.label3.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(83, 15);
            this.label3.TabIndex = 181;
            this.label3.Text = "Duration (ms):";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(356, 17);
            this.label2.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(103, 15);
            this.label2.TabIndex = 184;
            this.label2.Text = "Applied Gain (dB):";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(223, 17);
            this.label4.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(92, 15);
            this.label4.TabIndex = 185;
            this.label4.Text = "Headroom (dB):";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(502, 17);
            this.label5.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(90, 15);
            this.label5.TabIndex = 186;
            this.label5.Text = "Peak Level (dB):";
            // 
            // lblHeadroom
            // 
            this.lblHeadroom.AutoSize = true;
            this.lblHeadroom.Location = new System.Drawing.Point(316, 17);
            this.lblHeadroom.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lblHeadroom.Name = "lblHeadroom";
            this.lblHeadroom.Size = new System.Drawing.Size(34, 15);
            this.lblHeadroom.TabIndex = 187;
            this.lblHeadroom.Text = "000.0";
            // 
            // lblAppliedGain
            // 
            this.lblAppliedGain.AutoSize = true;
            this.lblAppliedGain.Location = new System.Drawing.Point(460, 17);
            this.lblAppliedGain.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lblAppliedGain.Name = "lblAppliedGain";
            this.lblAppliedGain.Size = new System.Drawing.Size(34, 15);
            this.lblAppliedGain.TabIndex = 188;
            this.lblAppliedGain.Text = "000.0";
            // 
            // lblPeakLevel
            // 
            this.lblPeakLevel.AutoSize = true;
            this.lblPeakLevel.Location = new System.Drawing.Point(600, 17);
            this.lblPeakLevel.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lblPeakLevel.Name = "lblPeakLevel";
            this.lblPeakLevel.Size = new System.Drawing.Size(34, 15);
            this.lblPeakLevel.TabIndex = 189;
            this.lblPeakLevel.Text = "000.0";
            // 
            // RefreshStats_Timer
            // 
            this.RefreshStats_Timer.Enabled = true;
            this.RefreshStats_Timer.Interval = 500;
            this.RefreshStats_Timer.Tick += new System.EventHandler(this.RefreshStats_Timer_Tick);
            // 
            // chkPeak
            // 
            this.chkPeak.AutoSize = true;
            this.chkPeak.Checked = true;
            this.chkPeak.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkPeak.Location = new System.Drawing.Point(849, 15);
            this.chkPeak.Margin = new System.Windows.Forms.Padding(2);
            this.chkPeak.Name = "chkPeak";
            this.chkPeak.Size = new System.Drawing.Size(51, 19);
            this.chkPeak.TabIndex = 190;
            this.chkPeak.Text = "Peak";
            this.chkPeak.UseVisualStyleBackColor = true;
            this.chkPeak.CheckedChanged += new System.EventHandler(this.chkPeak_CheckedChanged);
            // 
            // SmartGainControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.chkPeak);
            this.Controls.Add(this.lblPeakLevel);
            this.Controls.Add(this.lblAppliedGain);
            this.Controls.Add(this.lblHeadroom);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.chkPeakHold);
            this.Controls.Add(this.txtDuration);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.btnApply);
            this.Controls.Add(this.txtGain);
            this.Controls.Add(this.label1);
            this.Margin = new System.Windows.Forms.Padding(2);
            this.Name = "SmartGainControl";
            this.Size = new System.Drawing.Size(1091, 52);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        protected System.Windows.Forms.TextBox txtGain;
        protected System.Windows.Forms.Label label1;
        protected System.Windows.Forms.Button btnApply;
        protected System.Windows.Forms.CheckBox chkPeakHold;
        protected System.Windows.Forms.TextBox txtDuration;
        protected System.Windows.Forms.Label label3;
        protected System.Windows.Forms.Label label2;
        protected System.Windows.Forms.Label label4;
        protected System.Windows.Forms.Label label5;
        protected System.Windows.Forms.Label lblHeadroom;
        protected System.Windows.Forms.Label lblAppliedGain;
        protected System.Windows.Forms.Label lblPeakLevel;
        protected System.Windows.Forms.Timer RefreshStats_Timer;
        protected System.Windows.Forms.CheckBox chkPeak;
    }
}
