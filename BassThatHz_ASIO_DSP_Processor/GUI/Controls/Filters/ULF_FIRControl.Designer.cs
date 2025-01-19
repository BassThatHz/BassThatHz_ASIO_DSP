namespace BassThatHz_ASIO_DSP_Processor.GUI.Controls
{

    partial class ULF_FIRControl
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
            txtTaps = new System.Windows.Forms.RichTextBox();
            btnApply = new System.Windows.Forms.Button();
            label1 = new System.Windows.Forms.Label();
            label2 = new System.Windows.Forms.Label();
            txtFFTSize = new System.Windows.Forms.TextBox();
            comboTapsSampleRate = new System.Windows.Forms.ComboBox();
            label3 = new System.Windows.Forms.Label();
            txtTapsSampleRate = new System.Windows.Forms.TextBox();
            label4 = new System.Windows.Forms.Label();
            SuspendLayout();
            // 
            // txtTaps
            // 
            txtTaps.Location = new System.Drawing.Point(129, 2);
            txtTaps.Name = "txtTaps";
            txtTaps.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.Vertical;
            txtTaps.Size = new System.Drawing.Size(460, 46);
            txtTaps.TabIndex = 0;
            txtTaps.Text = "";
            // 
            // btnApply
            // 
            btnApply.Location = new System.Drawing.Point(1013, 2);
            btnApply.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            btnApply.Name = "btnApply";
            btnApply.Size = new System.Drawing.Size(76, 23);
            btnApply.TabIndex = 3;
            btnApply.Text = "Apply";
            btnApply.UseVisualStyleBackColor = true;
            btnApply.Click += btnApply_Click;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(7, 11);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(105, 15);
            label1.TabIndex = 5;
            label1.Text = "64Bit Floats (lines):";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new System.Drawing.Point(601, 6);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(186, 15);
            label2.TabIndex = 6;
            label2.Text = "FFT Size (Power of 2 > buffer size):";
            // 
            // txtFFTSize
            // 
            txtFFTSize.Location = new System.Drawing.Point(793, 1);
            txtFFTSize.MaxLength = 7;
            txtFFTSize.Name = "txtFFTSize";
            txtFFTSize.Size = new System.Drawing.Size(154, 23);
            txtFFTSize.TabIndex = 7;
            txtFFTSize.Text = "8192";
            // 
            // comboTapsSampleRate
            // 
            comboTapsSampleRate.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            comboTapsSampleRate.FormattingEnabled = true;
            comboTapsSampleRate.Items.AddRange(new object[] { "1/10th", "1/100th", "1/1000th", "Manual" });
            comboTapsSampleRate.Location = new System.Drawing.Point(737, 27);
            comboTapsSampleRate.Name = "comboTapsSampleRate";
            comboTapsSampleRate.Size = new System.Drawing.Size(121, 23);
            comboTapsSampleRate.TabIndex = 8;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new System.Drawing.Point(601, 33);
            label3.Name = "label3";
            label3.Size = new System.Drawing.Size(130, 15);
            label3.TabIndex = 9;
            label3.Text = "Auto Taps Sample Rate:";
            // 
            // txtTapsSampleRate
            // 
            txtTapsSampleRate.Location = new System.Drawing.Point(864, 27);
            txtTapsSampleRate.MaxLength = 7;
            txtTapsSampleRate.Name = "txtTapsSampleRate";
            txtTapsSampleRate.Size = new System.Drawing.Size(83, 23);
            txtTapsSampleRate.TabIndex = 10;
            txtTapsSampleRate.Text = "960";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new System.Drawing.Point(951, 34);
            label4.Name = "label4";
            label4.Size = new System.Drawing.Size(21, 15);
            label4.TabIndex = 11;
            label4.Text = "Hz";
            // 
            // ULF_FIRControl
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(label4);
            Controls.Add(txtTapsSampleRate);
            Controls.Add(label3);
            Controls.Add(comboTapsSampleRate);
            Controls.Add(txtFFTSize);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(btnApply);
            Controls.Add(txtTaps);
            Name = "ULF_FIRControl";
            Size = new System.Drawing.Size(1091, 52);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.RichTextBox txtTaps;
        private System.Windows.Forms.Button btnApply;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox txtFFTSize;
        private System.Windows.Forms.ComboBox comboTapsSampleRate;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox txtTapsSampleRate;
        private System.Windows.Forms.Label label4;
    }
}
