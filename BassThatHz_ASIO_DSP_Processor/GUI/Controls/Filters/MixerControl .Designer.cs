namespace BassThatHz_ASIO_DSP_Processor.GUI.Controls
{

    partial class MixerControl
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
            btnConfigMixer = new System.Windows.Forms.Button();
            listBox1 = new System.Windows.Forms.ListBox();
            SuspendLayout();
            // 
            // btnConfigMixer
            // 
            btnConfigMixer.Location = new System.Drawing.Point(16, 14);
            btnConfigMixer.Name = "btnConfigMixer";
            btnConfigMixer.Size = new System.Drawing.Size(99, 23);
            btnConfigMixer.TabIndex = 1;
            btnConfigMixer.Text = "Config Mixer";
            btnConfigMixer.UseVisualStyleBackColor = true;
            btnConfigMixer.Click += btnConfigMixer_Click;
            // 
            // listBox1
            // 
            listBox1.FormattingEnabled = true;
            listBox1.ItemHeight = 15;
            listBox1.Location = new System.Drawing.Point(130, 1);
            listBox1.Name = "listBox1";
            listBox1.Size = new System.Drawing.Size(519, 49);
            listBox1.TabIndex = 2;
            // 
            // MixerControl
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(listBox1);
            Controls.Add(btnConfigMixer);
            Name = "MixerControl";
            Size = new System.Drawing.Size(1091, 51);
            ResumeLayout(false);
        }

        #endregion
        protected System.Windows.Forms.Button btnConfigMixer;
        protected System.Windows.Forms.ListBox listBox1;
    }
}
