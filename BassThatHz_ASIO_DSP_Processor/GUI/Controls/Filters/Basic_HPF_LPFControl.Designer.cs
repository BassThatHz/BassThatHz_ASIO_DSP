namespace BassThatHz_ASIO_DSP_Processor.GUI.Controls
{

    partial class Basic_HPF_LPFControl
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
            txtLPFFreq = new System.Windows.Forms.TextBox();
            label4 = new System.Windows.Forms.Label();
            btnApply = new System.Windows.Forms.Button();
            cboHPF = new System.Windows.Forms.ComboBox();
            txtHPFFreq = new System.Windows.Forms.TextBox();
            label59 = new System.Windows.Forms.Label();
            cboLPF = new System.Windows.Forms.ComboBox();
            label1 = new System.Windows.Forms.Label();
            label2 = new System.Windows.Forms.Label();
            txtBiQuads = new System.Windows.Forms.TextBox();
            cboShowNormalized = new System.Windows.Forms.CheckBox();
            SuspendLayout();
            // 
            // txtLPFFreq
            // 
            txtLPFFreq.Location = new System.Drawing.Point(306, 25);
            txtLPFFreq.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            txtLPFFreq.Name = "txtLPFFreq";
            txtLPFFreq.Size = new System.Drawing.Size(125, 23);
            txtLPFFreq.TabIndex = 141;
            txtLPFFreq.Text = "20000";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new System.Drawing.Point(231, 30);
            label4.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            label4.Name = "label4";
            label4.Size = new System.Drawing.Size(65, 15);
            label4.TabIndex = 140;
            label4.Text = "Frequency:";
            // 
            // btnApply
            // 
            btnApply.Location = new System.Drawing.Point(1013, 2);
            btnApply.Name = "btnApply";
            btnApply.Size = new System.Drawing.Size(76, 23);
            btnApply.TabIndex = 145;
            btnApply.Text = "Apply";
            btnApply.UseVisualStyleBackColor = true;
            btnApply.Click += btnApply_Click;
            // 
            // cboHPF
            // 
            cboHPF.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cboHPF.FormattingEnabled = true;
            cboHPF.Items.AddRange(new object[] { "LR 12db/oct", "LR 24db/oct", "LR 48db/oct", "BW 6db/oct", "BW 12db/oct", "BW 18db/oct", "BW 24db/oct", "BW 30db/oct", "BW 36db/oct", "BW 42db/oct", "BW 48db/oct" });
            cboHPF.Location = new System.Drawing.Point(85, 1);
            cboHPF.Name = "cboHPF";
            cboHPF.Size = new System.Drawing.Size(125, 23);
            cboHPF.TabIndex = 204;
            // 
            // txtHPFFreq
            // 
            txtHPFFreq.Location = new System.Drawing.Point(85, 27);
            txtHPFFreq.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            txtHPFFreq.Name = "txtHPFFreq";
            txtHPFFreq.Size = new System.Drawing.Size(125, 23);
            txtHPFFreq.TabIndex = 203;
            txtHPFFreq.Text = "1";
            // 
            // label59
            // 
            label59.AutoSize = true;
            label59.Location = new System.Drawing.Point(6, 31);
            label59.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            label59.Name = "label59";
            label59.Size = new System.Drawing.Size(65, 15);
            label59.TabIndex = 202;
            label59.Text = "Frequency:";
            // 
            // cboLPF
            // 
            cboLPF.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cboLPF.FormattingEnabled = true;
            cboLPF.Items.AddRange(new object[] { "LR 12db/oct", "LR 24db/oct", "LR 48db/oct", "BW 6db/oct", "BW 12db/oct", "BW 18db/oct", "BW 24db/oct", "BW 30db/oct", "BW 36db/oct", "BW 42db/oct", "BW 48db/oct" });
            cboLPF.Location = new System.Drawing.Point(306, 0);
            cboLPF.Name = "cboLPF";
            cboLPF.Size = new System.Drawing.Size(125, 23);
            cboLPF.TabIndex = 258;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(40, 6);
            label1.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(32, 15);
            label1.TabIndex = 259;
            label1.Text = "HPF:";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new System.Drawing.Point(263, 5);
            label2.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(29, 15);
            label2.TabIndex = 260;
            label2.Text = "LPF:";
            // 
            // txtBiQuads
            // 
            txtBiQuads.Location = new System.Drawing.Point(569, 5);
            txtBiQuads.Multiline = true;
            txtBiQuads.Name = "txtBiQuads";
            txtBiQuads.ReadOnly = true;
            txtBiQuads.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            txtBiQuads.Size = new System.Drawing.Size(423, 40);
            txtBiQuads.TabIndex = 261;
            // 
            // cboShowNormalized
            // 
            cboShowNormalized.AutoSize = true;
            cboShowNormalized.Location = new System.Drawing.Point(440, 5);
            cboShowNormalized.Name = "cboShowNormalized";
            cboShowNormalized.Size = new System.Drawing.Size(119, 19);
            cboShowNormalized.TabIndex = 262;
            cboShowNormalized.Text = "Show Normalized";
            cboShowNormalized.UseVisualStyleBackColor = true;
            cboShowNormalized.CheckedChanged += cboShowNormalized_CheckedChanged;
            // 
            // Basic_HPF_LPFControl
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(cboShowNormalized);
            Controls.Add(txtBiQuads);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(cboLPF);
            Controls.Add(cboHPF);
            Controls.Add(txtHPFFreq);
            Controls.Add(label59);
            Controls.Add(btnApply);
            Controls.Add(txtLPFFreq);
            Controls.Add(label4);
            Name = "Basic_HPF_LPFControl";
            Size = new System.Drawing.Size(1091, 52);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        protected System.Windows.Forms.TextBox txtLPFFreq;
        protected System.Windows.Forms.Label label4;
        protected System.Windows.Forms.Button btnApply;
        protected System.Windows.Forms.ComboBox cboHPF;
        protected System.Windows.Forms.TextBox txtHPFFreq;
        protected System.Windows.Forms.Label label59;
        protected System.Windows.Forms.ComboBox cboLPF;
        protected System.Windows.Forms.Label label1;
        protected System.Windows.Forms.Label label2;
        protected System.Windows.Forms.TextBox txtBiQuads;
        protected System.Windows.Forms.CheckBox cboShowNormalized;
    }
}
