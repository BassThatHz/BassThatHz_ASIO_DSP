namespace BassThatHz_ASIO_DSP_Processor.GUI.Controls
{

    partial class FIRControl
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
            txtTaps = new System.Windows.Forms.RichTextBox();
            btnApply = new System.Windows.Forms.Button();
            label1 = new System.Windows.Forms.Label();
            label2 = new System.Windows.Forms.Label();
            txtFFTSize = new System.Windows.Forms.TextBox();
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
            txtFFTSize.Location = new System.Drawing.Point(793, 4);
            txtFFTSize.MaxLength = 7;
            txtFFTSize.Name = "txtFFTSize";
            txtFFTSize.Size = new System.Drawing.Size(154, 23);
            txtFFTSize.TabIndex = 7;
            txtFFTSize.Text = "8192";
            // 
            // FIRControl
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(txtFFTSize);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(btnApply);
            Controls.Add(txtTaps);
            Name = "FIRControl";
            Size = new System.Drawing.Size(1091, 52);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        protected System.Windows.Forms.RichTextBox txtTaps;
        protected System.Windows.Forms.Button btnApply;
        protected System.Windows.Forms.Label label1;
        protected System.Windows.Forms.Label label2;
        protected System.Windows.Forms.TextBox txtFFTSize;
    }
}
