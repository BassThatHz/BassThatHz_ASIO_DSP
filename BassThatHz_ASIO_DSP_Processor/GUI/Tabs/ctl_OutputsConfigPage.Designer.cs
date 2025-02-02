
namespace BassThatHz_ASIO_DSP_Processor.GUI.Tabs
{
    partial class ctl_OutputsConfigPage
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
            this.btnEditDevices = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.txtCustomDeviceName = new System.Windows.Forms.TextBox();
            this.btnEditChannels = new System.Windows.Forms.Button();
            this.label5 = new System.Windows.Forms.Label();
            this.txtCustomChannelName = new System.Windows.Forms.TextBox();
            this.lstChannels = new System.Windows.Forms.ListBox();
            this.lstDevices = new System.Windows.Forms.ListBox();
            this.cboDevices = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.volMaster = new BassThatHz_ASIO_DSP_Processor.BTH_VolumeSlider();
            this.label10 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.cboBufferSize = new System.Windows.Forms.ComboBox();
            this.btnASIOControlPanel = new System.Windows.Forms.Button();
            this.label4 = new System.Windows.Forms.Label();
            this.cboSampleRate = new System.Windows.Forms.ComboBox();
            this.SuspendLayout();
            // 
            // btnEditDevices
            // 
            this.btnEditDevices.Enabled = false;
            this.btnEditDevices.Location = new System.Drawing.Point(947, 405);
            this.btnEditDevices.Margin = new System.Windows.Forms.Padding(6, 5, 6, 5);
            this.btnEditDevices.Name = "btnEditDevices";
            this.btnEditDevices.Size = new System.Drawing.Size(236, 67);
            this.btnEditDevices.TabIndex = 142;
            this.btnEditDevices.Text = "Edit";
            this.btnEditDevices.UseVisualStyleBackColor = true;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(13, 384);
            this.label3.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(262, 37);
            this.label3.TabIndex = 141;
            this.label3.Text = "Customizable Name:";
            // 
            // txtCustomDeviceName
            // 
            this.txtCustomDeviceName.Enabled = false;
            this.txtCustomDeviceName.Location = new System.Drawing.Point(15, 423);
            this.txtCustomDeviceName.Margin = new System.Windows.Forms.Padding(6, 5, 6, 5);
            this.txtCustomDeviceName.MaxLength = 100;
            this.txtCustomDeviceName.Name = "txtCustomDeviceName";
            this.txtCustomDeviceName.Size = new System.Drawing.Size(919, 43);
            this.txtCustomDeviceName.TabIndex = 140;
            // 
            // btnEditChannels
            // 
            this.btnEditChannels.Enabled = false;
            this.btnEditChannels.Location = new System.Drawing.Point(2310, 162);
            this.btnEditChannels.Margin = new System.Windows.Forms.Padding(6, 5, 6, 5);
            this.btnEditChannels.Name = "btnEditChannels";
            this.btnEditChannels.Size = new System.Drawing.Size(236, 67);
            this.btnEditChannels.TabIndex = 138;
            this.btnEditChannels.Text = "Edit";
            this.btnEditChannels.UseVisualStyleBackColor = true;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(1423, 125);
            this.label5.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(262, 37);
            this.label5.TabIndex = 137;
            this.label5.Text = "Customizable Name:";
            // 
            // txtCustomChannelName
            // 
            this.txtCustomChannelName.Enabled = false;
            this.txtCustomChannelName.Location = new System.Drawing.Point(1423, 171);
            this.txtCustomChannelName.Margin = new System.Windows.Forms.Padding(6, 5, 6, 5);
            this.txtCustomChannelName.MaxLength = 100;
            this.txtCustomChannelName.Name = "txtCustomChannelName";
            this.txtCustomChannelName.Size = new System.Drawing.Size(874, 43);
            this.txtCustomChannelName.TabIndex = 136;
            // 
            // lstChannels
            // 
            this.lstChannels.FormattingEnabled = true;
            this.lstChannels.ItemHeight = 37;
            this.lstChannels.Items.AddRange(new object[] {
            "(Channel 01) Left",
            "(Channel 02) Right"});
            this.lstChannels.Location = new System.Drawing.Point(1431, 231);
            this.lstChannels.Margin = new System.Windows.Forms.Padding(6, 5, 6, 5);
            this.lstChannels.Name = "lstChannels";
            this.lstChannels.Size = new System.Drawing.Size(1367, 1336);
            this.lstChannels.TabIndex = 135;
            // 
            // lstDevices
            // 
            this.lstDevices.Enabled = false;
            this.lstDevices.FormattingEnabled = true;
            this.lstDevices.ItemHeight = 37;
            this.lstDevices.Items.AddRange(new object[] {
            "(ASIO4ALL v2) ASIO4ALL v2",
            "(Motu Pro Audio) Motu Pro Audio"});
            this.lstDevices.Location = new System.Drawing.Point(8, 490);
            this.lstDevices.Margin = new System.Windows.Forms.Padding(6, 5, 6, 5);
            this.lstDevices.Name = "lstDevices";
            this.lstDevices.Size = new System.Drawing.Size(1412, 1077);
            this.lstDevices.TabIndex = 132;
            // 
            // cboDevices
            // 
            this.cboDevices.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboDevices.FormattingEnabled = true;
            this.cboDevices.Location = new System.Drawing.Point(15, 58);
            this.cboDevices.Margin = new System.Windows.Forms.Padding(8, 9, 8, 9);
            this.cboDevices.Name = "cboDevices";
            this.cboDevices.Size = new System.Drawing.Size(912, 45);
            this.cboDevices.TabIndex = 125;
            this.cboDevices.SelectedIndexChanged += new System.EventHandler(this.cboDevices_SelectedIndexChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(1423, 12);
            this.label1.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(378, 37);
            this.label1.TabIndex = 126;
            this.label1.Text = "Output Device Master Volume:";
            // 
            // volMaster
            // 
            this.volMaster.Location = new System.Drawing.Point(1431, 53);
            this.volMaster.Margin = new System.Windows.Forms.Padding(6, 5, 6, 5);
            this.volMaster.MaxDb = 0D;
            this.volMaster.MinDb = -384D;
            this.volMaster.Name = "volMaster";
            this.volMaster.Size = new System.Drawing.Size(1372, 51);
            this.volMaster.TabIndex = 127;
            this.volMaster.Volume = 0.10000000149011611D;
            this.volMaster.VolumedB = -19.999999870570161D;
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(8, 12);
            this.label10.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(258, 37);
            this.label10.TabIndex = 128;
            this.label10.Text = "ASIO Output Device:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(13, 250);
            this.label2.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(146, 37);
            this.label2.TabIndex = 131;
            this.label2.Text = "Buffer Size:";
            // 
            // cboBufferSize
            // 
            this.cboBufferSize.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboBufferSize.Enabled = false;
            this.cboBufferSize.FormattingEnabled = true;
            this.cboBufferSize.Items.AddRange(new object[] {
            "Hardware Min (x ms)",
            "Hardware Recommended (x ms)",
            "Hardware Max (x ms)"});
            this.cboBufferSize.Location = new System.Drawing.Point(21, 298);
            this.cboBufferSize.Margin = new System.Windows.Forms.Padding(6, 5, 6, 5);
            this.cboBufferSize.Name = "cboBufferSize";
            this.cboBufferSize.Size = new System.Drawing.Size(906, 45);
            this.cboBufferSize.TabIndex = 130;
            this.cboBufferSize.SelectedIndexChanged += new System.EventHandler(this.cboBufferSize_SelectedIndexChanged);
            // 
            // btnASIOControlPanel
            // 
            this.btnASIOControlPanel.Enabled = false;
            this.btnASIOControlPanel.Location = new System.Drawing.Point(938, 53);
            this.btnASIOControlPanel.Margin = new System.Windows.Forms.Padding(6, 5, 6, 5);
            this.btnASIOControlPanel.Name = "btnASIOControlPanel";
            this.btnASIOControlPanel.Size = new System.Drawing.Size(472, 67);
            this.btnASIOControlPanel.TabIndex = 129;
            this.btnASIOControlPanel.Text = "ASIO Control Panel";
            this.btnASIOControlPanel.UseVisualStyleBackColor = true;
            this.btnASIOControlPanel.Click += new System.EventHandler(this.btnASIOControlPanel_Click);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(13, 125);
            this.label4.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(171, 37);
            this.label4.TabIndex = 151;
            this.label4.Text = "Sample Rate:";
            // 
            // cboSampleRate
            // 
            this.cboSampleRate.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboSampleRate.Enabled = false;
            this.cboSampleRate.FormattingEnabled = true;
            this.cboSampleRate.Items.AddRange(new object[] {
            "Hardware Min (x ms)",
            "Hardware Recommended (x ms)",
            "Hardware Max (x ms)"});
            this.cboSampleRate.Location = new System.Drawing.Point(21, 173);
            this.cboSampleRate.Margin = new System.Windows.Forms.Padding(6, 5, 6, 5);
            this.cboSampleRate.Name = "cboSampleRate";
            this.cboSampleRate.Size = new System.Drawing.Size(913, 45);
            this.cboSampleRate.TabIndex = 150;
            // 
            // ctl_OutputsConfigPage
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(15F, 37F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.label4);
            this.Controls.Add(this.cboSampleRate);
            this.Controls.Add(this.btnEditDevices);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.txtCustomDeviceName);
            this.Controls.Add(this.btnEditChannels);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.txtCustomChannelName);
            this.Controls.Add(this.lstChannels);
            this.Controls.Add(this.lstDevices);
            this.Controls.Add(this.cboDevices);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.volMaster);
            this.Controls.Add(this.label10);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.cboBufferSize);
            this.Controls.Add(this.btnASIOControlPanel);
            this.Margin = new System.Windows.Forms.Padding(6, 5, 6, 5);
            this.Name = "ctl_OutputsConfigPage";
            this.Size = new System.Drawing.Size(2818, 1610);
            this.Load += new System.EventHandler(this.ctl_OutputsConfigPage_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        protected System.Windows.Forms.Button btnEditDevices;
        protected System.Windows.Forms.Label label3;
        protected System.Windows.Forms.TextBox txtCustomDeviceName;
        protected System.Windows.Forms.Button btnEditChannels;
        protected System.Windows.Forms.Label label5;
        protected System.Windows.Forms.TextBox txtCustomChannelName;
        protected System.Windows.Forms.ListBox lstChannels;
        protected System.Windows.Forms.ListBox lstDevices;
        protected System.Windows.Forms.ComboBox cboDevices;
        protected System.Windows.Forms.Label label1;
        protected BTH_VolumeSlider volMaster;
        protected System.Windows.Forms.Label label10;
        protected System.Windows.Forms.Label label2;
        protected System.Windows.Forms.ComboBox cboBufferSize;
        protected System.Windows.Forms.Button btnASIOControlPanel;
        protected System.Windows.Forms.Label label4;
        protected System.Windows.Forms.ComboBox cboSampleRate;
    }
}
