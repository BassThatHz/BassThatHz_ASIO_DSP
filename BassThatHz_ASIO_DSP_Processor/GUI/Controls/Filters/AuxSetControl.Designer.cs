namespace BassThatHz_ASIO_DSP_Processor.GUI.Controls
{
    partial class AuxSetControl
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
            chk_MuteAfter = new System.Windows.Forms.CheckBox();
            cbo_AuxToSet = new System.Windows.Forms.ComboBox();
            label1 = new System.Windows.Forms.Label();
            SuspendLayout();
            // 
            // chk_MuteAfter
            // 
            chk_MuteAfter.AutoSize = true;
            chk_MuteAfter.Location = new System.Drawing.Point(258, 18);
            chk_MuteAfter.Name = "chk_MuteAfter";
            chk_MuteAfter.Size = new System.Drawing.Size(83, 19);
            chk_MuteAfter.TabIndex = 0;
            chk_MuteAfter.Text = "Mute After";
            chk_MuteAfter.UseVisualStyleBackColor = true;
            chk_MuteAfter.CheckedChanged += chk_MuteAfter_CheckedChanged;
            // 
            // cbo_AuxToSet
            // 
            cbo_AuxToSet.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cbo_AuxToSet.FormattingEnabled = true;
            cbo_AuxToSet.Location = new System.Drawing.Point(119, 16);
            cbo_AuxToSet.Name = "cbo_AuxToSet";
            cbo_AuxToSet.Size = new System.Drawing.Size(121, 23);
            cbo_AuxToSet.TabIndex = 1;
            cbo_AuxToSet.SelectedIndexChanged += cbo_AuxToSet_SelectedIndexChanged;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(15, 19);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(98, 15);
            label1.TabIndex = 2;
            label1.Text = "Select Aux to Set:";
            // 
            // AuxSetControl
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(label1);
            Controls.Add(cbo_AuxToSet);
            Controls.Add(chk_MuteAfter);
            Name = "AuxSetControl";
            Size = new System.Drawing.Size(1091, 52);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        protected System.Windows.Forms.CheckBox chk_MuteAfter;
        protected System.Windows.Forms.ComboBox cbo_AuxToSet;
        protected System.Windows.Forms.Label label1;
    }
}
