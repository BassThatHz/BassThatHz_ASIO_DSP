
namespace BassThatHz_ASIO_DSP_Processor.GUI.Tabs
{
    partial class ctl_InputsConfigPage
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
            this.btnEditDevice = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.txtDeviceCustomName = new System.Windows.Forms.TextBox();
            this.lstDevices = new System.Windows.Forms.ListBox();
            this.cboDevices = new System.Windows.Forms.ComboBox();
            this.btnASIOControlPanel = new System.Windows.Forms.Button();
            this.btnEditChannel = new System.Windows.Forms.Button();
            this.label5 = new System.Windows.Forms.Label();
            this.txtChannelCustomName = new System.Windows.Forms.TextBox();
            this.lstChannels = new System.Windows.Forms.ListBox();
            this.label1 = new System.Windows.Forms.Label();
            this.volMaster = new BassThatHz_ASIO_DSP_Processor.BTH_VolumeSlider();
            this.label10 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.cboBufferSize = new System.Windows.Forms.ComboBox();
            this.label4 = new System.Windows.Forms.Label();
            this.cboSampleRate = new System.Windows.Forms.ComboBox();
            this.SuspendLayout();
            // 
            // btnEditDevice
            // 
            this.btnEditDevice.Enabled = false;
            this.btnEditDevice.Location = new System.Drawing.Point(504, 176);
            this.btnEditDevice.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.btnEditDevice.Name = "btnEditDevice";
            this.btnEditDevice.Size = new System.Drawing.Size(126, 29);
            this.btnEditDevice.TabIndex = 147;
            this.btnEditDevice.Text = "Edit";
            this.btnEditDevice.UseVisualStyleBackColor = true;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(7, 166);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(137, 17);
            this.label3.TabIndex = 146;
            this.label3.Text = "Customizable Name:";
            // 
            // txtDeviceCustomName
            // 
            this.txtDeviceCustomName.Enabled = false;
            this.txtDeviceCustomName.Location = new System.Drawing.Point(7, 184);
            this.txtDeviceCustomName.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.txtDeviceCustomName.MaxLength = 100;
            this.txtDeviceCustomName.Name = "txtDeviceCustomName";
            this.txtDeviceCustomName.Size = new System.Drawing.Size(492, 22);
            this.txtDeviceCustomName.TabIndex = 145;
            // 
            // lstDevices
            // 
            this.lstDevices.Enabled = false;
            this.lstDevices.FormattingEnabled = true;
            this.lstDevices.ItemHeight = 16;
            this.lstDevices.Items.AddRange(new object[] {
            "(ASIO4ALL v2) ASIO4ALL v2",
            "(Motu Pro Audio) Motu Pro Audio"});
            this.lstDevices.Location = new System.Drawing.Point(3, 213);
            this.lstDevices.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.lstDevices.Name = "lstDevices";
            this.lstDevices.Size = new System.Drawing.Size(755, 468);
            this.lstDevices.TabIndex = 144;
            // 
            // cboDevices
            // 
            this.cboDevices.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboDevices.FormattingEnabled = true;
            this.cboDevices.Location = new System.Drawing.Point(7, 26);
            this.cboDevices.Margin = new System.Windows.Forms.Padding(4);
            this.cboDevices.Name = "cboDevices";
            this.cboDevices.Size = new System.Drawing.Size(488, 24);
            this.cboDevices.TabIndex = 142;
            this.cboDevices.SelectedIndexChanged += new System.EventHandler(this.cboDevices_SelectedIndexChanged);
            // 
            // btnASIOControlPanel
            // 
            this.btnASIOControlPanel.Location = new System.Drawing.Point(500, 22);
            this.btnASIOControlPanel.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.btnASIOControlPanel.Name = "btnASIOControlPanel";
            this.btnASIOControlPanel.Size = new System.Drawing.Size(252, 29);
            this.btnASIOControlPanel.TabIndex = 143;
            this.btnASIOControlPanel.Text = "ASIO Control Panel";
            this.btnASIOControlPanel.UseVisualStyleBackColor = true;
            this.btnASIOControlPanel.Click += new System.EventHandler(this.btnASIOControlPanel_Click);
            // 
            // btnEditChannel
            // 
            this.btnEditChannel.Enabled = false;
            this.btnEditChannel.Location = new System.Drawing.Point(1236, 70);
            this.btnEditChannel.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.btnEditChannel.Name = "btnEditChannel";
            this.btnEditChannel.Size = new System.Drawing.Size(126, 29);
            this.btnEditChannel.TabIndex = 140;
            this.btnEditChannel.Text = "Edit";
            this.btnEditChannel.UseVisualStyleBackColor = true;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(763, 55);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(137, 17);
            this.label5.TabIndex = 139;
            this.label5.Text = "Customizable Name:";
            // 
            // txtChannelCustomName
            // 
            this.txtChannelCustomName.Enabled = false;
            this.txtChannelCustomName.Location = new System.Drawing.Point(763, 74);
            this.txtChannelCustomName.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.txtChannelCustomName.MaxLength = 100;
            this.txtChannelCustomName.Name = "txtChannelCustomName";
            this.txtChannelCustomName.Size = new System.Drawing.Size(468, 22);
            this.txtChannelCustomName.TabIndex = 138;
            // 
            // lstChannels
            // 
            this.lstChannels.FormattingEnabled = true;
            this.lstChannels.ItemHeight = 16;
            this.lstChannels.Items.AddRange(new object[] {
            "(Channel 01)",
            "(Channel 02)"});
            this.lstChannels.Location = new System.Drawing.Point(762, 101);
            this.lstChannels.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.lstChannels.Name = "lstChannels";
            this.lstChannels.Size = new System.Drawing.Size(731, 580);
            this.lstChannels.TabIndex = 137;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(757, 6);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(188, 17);
            this.label1.TabIndex = 132;
            this.label1.Text = "Input Device Master Volume:";
            // 
            // volMaster
            // 
            this.volMaster.Location = new System.Drawing.Point(761, 26);
            this.volMaster.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.volMaster.Name = "volMaster";
            this.volMaster.Size = new System.Drawing.Size(732, 22);
            this.volMaster.TabIndex = 133;
            this.volMaster.Volume = 0.1F;
            this.volMaster.VolumedB = -20F;
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(7, 6);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(126, 17);
            this.label10.TabIndex = 134;
            this.label10.Text = "ASIO Input Device:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(7, 112);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(81, 17);
            this.label2.TabIndex = 136;
            this.label2.Text = "Buffer Size:";
            // 
            // cboBufferSize
            // 
            this.cboBufferSize.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboBufferSize.FormattingEnabled = true;
            this.cboBufferSize.Items.AddRange(new object[] {
            "Hardware Min (x ms)",
            "Hardware Recommended (x ms)",
            "Hardware Max (x ms)"});
            this.cboBufferSize.Location = new System.Drawing.Point(7, 133);
            this.cboBufferSize.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.cboBufferSize.Name = "cboBufferSize";
            this.cboBufferSize.Size = new System.Drawing.Size(488, 24);
            this.cboBufferSize.TabIndex = 135;
            this.cboBufferSize.SelectedIndexChanged += new System.EventHandler(this.cboBufferSize_SelectedIndexChanged);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(7, 55);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(93, 17);
            this.label4.TabIndex = 149;
            this.label4.Text = "Sample Rate:";
            // 
            // cboSampleRate
            // 
            this.cboSampleRate.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboSampleRate.FormattingEnabled = true;
            this.cboSampleRate.Location = new System.Drawing.Point(7, 76);
            this.cboSampleRate.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.cboSampleRate.Name = "cboSampleRate";
            this.cboSampleRate.Size = new System.Drawing.Size(488, 24);
            this.cboSampleRate.TabIndex = 148;
            this.cboSampleRate.SelectedIndexChanged += new System.EventHandler(this.cboSampleRate_SelectedIndexChanged);
            // 
            // ctl_InputsConfigPage
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.label4);
            this.Controls.Add(this.cboSampleRate);
            this.Controls.Add(this.btnEditDevice);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.txtDeviceCustomName);
            this.Controls.Add(this.lstDevices);
            this.Controls.Add(this.cboDevices);
            this.Controls.Add(this.btnASIOControlPanel);
            this.Controls.Add(this.btnEditChannel);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.txtChannelCustomName);
            this.Controls.Add(this.lstChannels);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.volMaster);
            this.Controls.Add(this.label10);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.cboBufferSize);
            this.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.Name = "ctl_InputsConfigPage";
            this.Size = new System.Drawing.Size(1503, 696);
            this.Load += new System.EventHandler(this.ctl_InputsConfigPage_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        public System.Windows.Forms.Button btnEditDevice;
        public System.Windows.Forms.Label label3;
        public System.Windows.Forms.TextBox txtDeviceCustomName;
        public System.Windows.Forms.ListBox lstDevices;
        public System.Windows.Forms.ComboBox cboDevices;
        public System.Windows.Forms.Button btnASIOControlPanel;
        public System.Windows.Forms.Button btnEditChannel;
        public System.Windows.Forms.Label label5;
        public System.Windows.Forms.TextBox txtChannelCustomName;
        public System.Windows.Forms.ListBox lstChannels;
        public System.Windows.Forms.Label label1;
        public BTH_VolumeSlider volMaster;
        public System.Windows.Forms.Label label10;
        public System.Windows.Forms.Label label2;
        public System.Windows.Forms.ComboBox cboBufferSize;
        public System.Windows.Forms.Label label4;
        public System.Windows.Forms.ComboBox cboSampleRate;
    }
}
