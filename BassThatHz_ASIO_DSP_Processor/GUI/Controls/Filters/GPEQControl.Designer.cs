namespace BassThatHz_ASIO_DSP_Processor.GUI.Controls.Filters
{
    partial class GPEQControl
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
            ConfigGPEQ_BTN = new System.Windows.Forms.Button();
            Filters_LSB = new System.Windows.Forms.ListBox();
            SuspendLayout();
            // 
            // ConfigGPEQ_BTN
            // 
            ConfigGPEQ_BTN.Location = new System.Drawing.Point(19, 16);
            ConfigGPEQ_BTN.Margin = new System.Windows.Forms.Padding(2, 1, 2, 1);
            ConfigGPEQ_BTN.Name = "ConfigGPEQ_BTN";
            ConfigGPEQ_BTN.Size = new System.Drawing.Size(115, 22);
            ConfigGPEQ_BTN.TabIndex = 0;
            ConfigGPEQ_BTN.Text = "Config GPEQ";
            ConfigGPEQ_BTN.UseVisualStyleBackColor = true;
            ConfigGPEQ_BTN.Click += ConfigGPEQ_BTN_Click;
            // 
            // Filters_LSB
            // 
            Filters_LSB.FormattingEnabled = true;
            Filters_LSB.Location = new System.Drawing.Point(139, 2);
            Filters_LSB.Name = "Filters_LSB";
            Filters_LSB.Size = new System.Drawing.Size(794, 49);
            Filters_LSB.TabIndex = 3;
            // 
            // GPEQ
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(Filters_LSB);
            Controls.Add(ConfigGPEQ_BTN);
            Margin = new System.Windows.Forms.Padding(2, 1, 2, 1);
            Name = "GPEQ";
            Size = new System.Drawing.Size(1090, 52);
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Button ConfigGPEQ_BTN;
        private System.Windows.Forms.ListBox Filters_LSB;
    }
}
