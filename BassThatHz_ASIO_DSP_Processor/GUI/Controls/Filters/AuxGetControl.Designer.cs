namespace BassThatHz_ASIO_DSP_Processor.GUI.Controls
{
    partial class AuxGetControl
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
            chk_MuteBefore = new System.Windows.Forms.CheckBox();
            cbo_AuxToGet = new System.Windows.Forms.ComboBox();
            label1 = new System.Windows.Forms.Label();
            txtStreamAttenuation = new System.Windows.Forms.TextBox();
            label2 = new System.Windows.Forms.Label();
            txtAuxAttenuation = new System.Windows.Forms.TextBox();
            label3 = new System.Windows.Forms.Label();
            btnApply = new System.Windows.Forms.Button();
            SuspendLayout();
            // 
            // chk_MuteBefore
            // 
            chk_MuteBefore.AutoSize = true;
            chk_MuteBefore.Location = new System.Drawing.Point(258, 18);
            chk_MuteBefore.Name = "chk_MuteBefore";
            chk_MuteBefore.Size = new System.Drawing.Size(91, 19);
            chk_MuteBefore.TabIndex = 0;
            chk_MuteBefore.Text = "Mute Before";
            chk_MuteBefore.UseVisualStyleBackColor = true;
            // 
            // cbo_AuxToGet
            // 
            cbo_AuxToGet.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cbo_AuxToGet.FormattingEnabled = true;
            cbo_AuxToGet.Location = new System.Drawing.Point(119, 16);
            cbo_AuxToGet.Name = "cbo_AuxToGet";
            cbo_AuxToGet.Size = new System.Drawing.Size(121, 23);
            cbo_AuxToGet.TabIndex = 1;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(15, 19);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(100, 15);
            label1.TabIndex = 2;
            label1.Text = "Select Aux to Get:";
            // 
            // txtStreamAttenuation
            // 
            txtStreamAttenuation.Location = new System.Drawing.Point(739, 13);
            txtStreamAttenuation.Margin = new System.Windows.Forms.Padding(1);
            txtStreamAttenuation.Name = "txtStreamAttenuation";
            txtStreamAttenuation.Size = new System.Drawing.Size(60, 23);
            txtStreamAttenuation.TabIndex = 9;
            txtStreamAttenuation.Text = "-6.0";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new System.Drawing.Point(622, 16);
            label2.Margin = new System.Windows.Forms.Padding(1, 0, 1, 0);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(113, 15);
            label2.TabIndex = 8;
            label2.Text = "Stream Attenuation:";
            // 
            // txtAuxAttenuation
            // 
            txtAuxAttenuation.Location = new System.Drawing.Point(540, 14);
            txtAuxAttenuation.Margin = new System.Windows.Forms.Padding(1);
            txtAuxAttenuation.Name = "txtAuxAttenuation";
            txtAuxAttenuation.Size = new System.Drawing.Size(60, 23);
            txtAuxAttenuation.TabIndex = 7;
            txtAuxAttenuation.Text = "-6.051";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new System.Drawing.Point(437, 19);
            label3.Margin = new System.Windows.Forms.Padding(1, 0, 1, 0);
            label3.Name = "label3";
            label3.Size = new System.Drawing.Size(97, 15);
            label3.TabIndex = 6;
            label3.Text = "Aux Attenuation:";
            // 
            // btnApply
            // 
            btnApply.Location = new System.Drawing.Point(1013, 2);
            btnApply.Margin = new System.Windows.Forms.Padding(2);
            btnApply.Name = "btnApply";
            btnApply.Size = new System.Drawing.Size(76, 23);
            btnApply.TabIndex = 10;
            btnApply.Text = "Apply";
            btnApply.UseVisualStyleBackColor = true;
            btnApply.Click += btnApply_Click;
            // 
            // AuxGetControl
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(btnApply);
            Controls.Add(txtStreamAttenuation);
            Controls.Add(label2);
            Controls.Add(txtAuxAttenuation);
            Controls.Add(label3);
            Controls.Add(label1);
            Controls.Add(cbo_AuxToGet);
            Controls.Add(chk_MuteBefore);
            Name = "AuxGetControl";
            Size = new System.Drawing.Size(1091, 52);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        protected System.Windows.Forms.CheckBox chk_MuteBefore;
        protected System.Windows.Forms.ComboBox cbo_AuxToGet;
        protected System.Windows.Forms.Label label1;
        protected System.Windows.Forms.TextBox txtStreamAttenuation;
        protected System.Windows.Forms.Label label2;
        protected System.Windows.Forms.TextBox txtAuxAttenuation;
        protected System.Windows.Forms.Label label3;
        protected System.Windows.Forms.Button btnApply;
    }
}
