namespace BassThatHz_ASIO_DSP_Processor.GUI.Controls
{

    partial class FloorControl
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
            this.txtHoldInMS = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.txtRatio = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.Threshold = new BassThatHz_ASIO_DSP_Processor.BTH_VolumeSlider();
            this.label1 = new System.Windows.Forms.Label();
            this.btnApply = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // txtHoldInMS
            // 
            this.txtHoldInMS.Location = new System.Drawing.Point(822, 13);
            this.txtHoldInMS.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.txtHoldInMS.Name = "txtHoldInMS";
            this.txtHoldInMS.Size = new System.Drawing.Size(31, 20);
            this.txtHoldInMS.TabIndex = 162;
            this.txtHoldInMS.Text = "1000";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(755, 15);
            this.label4.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(65, 13);
            this.label4.TabIndex = 161;
            this.label4.Text = "Hold (in ms):";
            // 
            // txtRatio
            // 
            this.txtRatio.Location = new System.Drawing.Point(701, 13);
            this.txtRatio.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.txtRatio.Name = "txtRatio";
            this.txtRatio.Size = new System.Drawing.Size(31, 20);
            this.txtRatio.TabIndex = 160;
            this.txtRatio.Text = "1.1";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(568, 15);
            this.label3.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(135, 13);
            this.label3.TabIndex = 159;
            this.label3.Text = "Attack/Release Ratio (X:1)";
            // 
            // Threshold
            // 
            this.Threshold.Location = new System.Drawing.Point(64, 11);
            this.Threshold.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.Threshold.Name = "Threshold";
            this.Threshold.Size = new System.Drawing.Size(500, 23);
            this.Threshold.TabIndex = 156;
            this.Threshold.Volume = 6.309573E-20F;
            this.Threshold.VolumedB = -384F;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(5, 15);
            this.label1.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(57, 13);
            this.label1.TabIndex = 155;
            this.label1.Text = "Threshold:";
            // 
            // btnApply
            // 
            this.btnApply.Location = new System.Drawing.Point(868, 2);
            this.btnApply.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.btnApply.Name = "btnApply";
            this.btnApply.Size = new System.Drawing.Size(65, 20);
            this.btnApply.TabIndex = 163;
            this.btnApply.Text = "Apply";
            this.btnApply.UseVisualStyleBackColor = true;
            this.btnApply.Click += new System.EventHandler(this.btnApply_Click);
            // 
            // FloorControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.btnApply);
            this.Controls.Add(this.txtHoldInMS);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.txtRatio);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.Threshold);
            this.Controls.Add(this.label1);
            this.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.Name = "FloorControl";
            this.Size = new System.Drawing.Size(935, 45);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox txtHoldInMS;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox txtRatio;
        private System.Windows.Forms.Label label3;
        public BTH_VolumeSlider Threshold;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btnApply;
    }
}
