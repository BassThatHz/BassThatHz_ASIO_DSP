
namespace BassThatHz_ASIO_DSP_Processor.GUI.Tabs
{
    partial class ctl_GeneralConfigPage
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
            groupBox2 = new System.Windows.Forms.GroupBox();
            chkBackgroundThread = new System.Windows.Forms.CheckBox();
            chkThreading = new System.Windows.Forms.CheckBox();
            label22 = new System.Windows.Forms.Label();
            lstProcesAffinty = new System.Windows.Forms.ListBox();
            label21 = new System.Windows.Forms.Label();
            label20 = new System.Windows.Forms.Label();
            label19 = new System.Windows.Forms.Label();
            cboProcessPriority = new System.Windows.Forms.ComboBox();
            btnLoadConfig = new System.Windows.Forms.Button();
            btnSaveConfig = new System.Windows.Forms.Button();
            saveFileDialog1 = new System.Windows.Forms.SaveFileDialog();
            openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            chkAutoStartDSP = new System.Windows.Forms.CheckBox();
            label1 = new System.Windows.Forms.Label();
            maskStartUpDelay = new System.Windows.Forms.MaskedTextBox();
            groupBox1 = new System.Windows.Forms.GroupBox();
            label5 = new System.Windows.Forms.Label();
            label4 = new System.Windows.Forms.Label();
            label3 = new System.Windows.Forms.Label();
            label2 = new System.Windows.Forms.Label();
            txt_NetworkConfigAPI_Host = new System.Windows.Forms.TextBox();
            chkNetworkConfigAPI = new System.Windows.Forms.CheckBox();
            maskNetworkConfig_Port = new System.Windows.Forms.MaskedTextBox();
            groupBox2.SuspendLayout();
            groupBox1.SuspendLayout();
            SuspendLayout();
            // 
            // groupBox2
            // 
            groupBox2.Controls.Add(chkBackgroundThread);
            groupBox2.Controls.Add(chkThreading);
            groupBox2.Controls.Add(label22);
            groupBox2.Controls.Add(lstProcesAffinty);
            groupBox2.Controls.Add(label21);
            groupBox2.Controls.Add(label20);
            groupBox2.Controls.Add(label19);
            groupBox2.Controls.Add(cboProcessPriority);
            groupBox2.Location = new System.Drawing.Point(3, 64);
            groupBox2.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            groupBox2.Name = "groupBox2";
            groupBox2.Padding = new System.Windows.Forms.Padding(3, 2, 3, 2);
            groupBox2.Size = new System.Drawing.Size(564, 161);
            groupBox2.TabIndex = 79;
            groupBox2.TabStop = false;
            groupBox2.Text = "CPU";
            // 
            // chkBackgroundThread
            // 
            chkBackgroundThread.AutoSize = true;
            chkBackgroundThread.Checked = true;
            chkBackgroundThread.CheckState = System.Windows.Forms.CheckState.Checked;
            chkBackgroundThread.Location = new System.Drawing.Point(300, 96);
            chkBackgroundThread.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            chkBackgroundThread.Name = "chkBackgroundThread";
            chkBackgroundThread.Size = new System.Drawing.Size(173, 19);
            chkBackgroundThread.TabIndex = 14;
            chkBackgroundThread.Text = "Run in a background thread";
            chkBackgroundThread.UseVisualStyleBackColor = true;
            chkBackgroundThread.CheckedChanged += chkBackgroundThread_CheckedChanged;
            // 
            // chkThreading
            // 
            chkThreading.AutoSize = true;
            chkThreading.Checked = true;
            chkThreading.CheckState = System.Windows.Forms.CheckState.Checked;
            chkThreading.Location = new System.Drawing.Point(301, 71);
            chkThreading.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            chkThreading.Name = "chkThreading";
            chkThreading.Size = new System.Drawing.Size(150, 19);
            chkThreading.TabIndex = 12;
            chkThreading.Text = "Enable Multi-Threading";
            chkThreading.UseVisualStyleBackColor = true;
            chkThreading.CheckedChanged += chkThreading_CheckedChanged;
            // 
            // label22
            // 
            label22.AutoSize = true;
            label22.Location = new System.Drawing.Point(298, 47);
            label22.Name = "label22";
            label22.Size = new System.Drawing.Size(98, 15);
            label22.TabIndex = 5;
            label22.Text = "Default: All Cores";
            // 
            // lstProcesAffinty
            // 
            lstProcesAffinty.Enabled = false;
            lstProcesAffinty.FormattingEnabled = true;
            lstProcesAffinty.ItemHeight = 15;
            lstProcesAffinty.Items.AddRange(new object[] { "All Cores (Best performance)", "1 Core (Worst performance)" });
            lstProcesAffinty.Location = new System.Drawing.Point(106, 47);
            lstProcesAffinty.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            lstProcesAffinty.Name = "lstProcesAffinty";
            lstProcesAffinty.Size = new System.Drawing.Size(178, 109);
            lstProcesAffinty.TabIndex = 4;
            // 
            // label21
            // 
            label21.AutoSize = true;
            label21.Location = new System.Drawing.Point(8, 47);
            label21.Name = "label21";
            label21.Size = new System.Drawing.Size(92, 15);
            label21.TabIndex = 3;
            label21.Text = "Process Affinity:";
            // 
            // label20
            // 
            label20.AutoSize = true;
            label20.Location = new System.Drawing.Point(298, 20);
            label20.Name = "label20";
            label20.Size = new System.Drawing.Size(77, 15);
            label20.TabIndex = 2;
            label20.Text = "Default: High";
            // 
            // label19
            // 
            label19.AutoSize = true;
            label19.Location = new System.Drawing.Point(8, 20);
            label19.Name = "label19";
            label19.Size = new System.Drawing.Size(91, 15);
            label19.TabIndex = 1;
            label19.Text = "Process Priority:";
            // 
            // cboProcessPriority
            // 
            cboProcessPriority.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cboProcessPriority.FormattingEnabled = true;
            cboProcessPriority.Location = new System.Drawing.Point(106, 20);
            cboProcessPriority.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            cboProcessPriority.Name = "cboProcessPriority";
            cboProcessPriority.Size = new System.Drawing.Size(178, 23);
            cboProcessPriority.TabIndex = 0;
            cboProcessPriority.SelectedIndexChanged += cboProcessPriority_SelectedIndexChanged;
            // 
            // btnLoadConfig
            // 
            btnLoadConfig.Location = new System.Drawing.Point(96, 35);
            btnLoadConfig.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            btnLoadConfig.Name = "btnLoadConfig";
            btnLoadConfig.Size = new System.Drawing.Size(88, 24);
            btnLoadConfig.TabIndex = 81;
            btnLoadConfig.Text = "Load Config";
            btnLoadConfig.UseVisualStyleBackColor = true;
            btnLoadConfig.Click += btnLoadConfig_Click;
            // 
            // btnSaveConfig
            // 
            btnSaveConfig.Location = new System.Drawing.Point(3, 35);
            btnSaveConfig.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            btnSaveConfig.Name = "btnSaveConfig";
            btnSaveConfig.Size = new System.Drawing.Size(88, 24);
            btnSaveConfig.TabIndex = 80;
            btnSaveConfig.Text = "Save Config";
            btnSaveConfig.UseVisualStyleBackColor = true;
            btnSaveConfig.Click += btnSaveConfig_Click;
            // 
            // saveFileDialog1
            // 
            saveFileDialog1.DefaultExt = "xml";
            saveFileDialog1.FileName = "DSP";
            saveFileDialog1.Filter = "|*.xml";
            saveFileDialog1.Title = "Save Config";
            // 
            // openFileDialog1
            // 
            openFileDialog1.FileName = "DSP";
            openFileDialog1.Filter = "|*.xml";
            openFileDialog1.Title = "Load Config";
            // 
            // chkAutoStartDSP
            // 
            chkAutoStartDSP.AutoSize = true;
            chkAutoStartDSP.Location = new System.Drawing.Point(13, 12);
            chkAutoStartDSP.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            chkAutoStartDSP.Name = "chkAutoStartDSP";
            chkAutoStartDSP.Size = new System.Drawing.Size(103, 19);
            chkAutoStartDSP.TabIndex = 82;
            chkAutoStartDSP.Text = "Auto Start DSP";
            chkAutoStartDSP.UseVisualStyleBackColor = true;
            chkAutoStartDSP.CheckedChanged += chkAutoStartDSP_CheckedChanged;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(138, 14);
            label1.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(111, 15);
            label1.TabIndex = 83;
            label1.Text = "Start Up Delay (ms):";
            // 
            // maskStartUpDelay
            // 
            maskStartUpDelay.Location = new System.Drawing.Point(253, 13);
            maskStartUpDelay.Margin = new System.Windows.Forms.Padding(2, 1, 2, 1);
            maskStartUpDelay.Mask = "#######";
            maskStartUpDelay.Name = "maskStartUpDelay";
            maskStartUpDelay.Size = new System.Drawing.Size(45, 23);
            maskStartUpDelay.TabIndex = 84;
            maskStartUpDelay.Text = "0";
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(maskNetworkConfig_Port);
            groupBox1.Controls.Add(label5);
            groupBox1.Controls.Add(label4);
            groupBox1.Controls.Add(label3);
            groupBox1.Controls.Add(label2);
            groupBox1.Controls.Add(txt_NetworkConfigAPI_Host);
            groupBox1.Controls.Add(chkNetworkConfigAPI);
            groupBox1.Location = new System.Drawing.Point(3, 226);
            groupBox1.Margin = new System.Windows.Forms.Padding(2, 1, 2, 1);
            groupBox1.Name = "groupBox1";
            groupBox1.Padding = new System.Windows.Forms.Padding(2, 1, 2, 1);
            groupBox1.Size = new System.Drawing.Size(564, 76);
            groupBox1.TabIndex = 85;
            groupBox1.TabStop = false;
            groupBox1.Text = "Config API";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new System.Drawing.Point(252, 45);
            label5.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            label5.Name = "label5";
            label5.Size = new System.Drawing.Size(83, 15);
            label5.TabIndex = 6;
            label5.Text = "Default is 8080";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new System.Drawing.Point(253, 19);
            label4.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            label4.Name = "label4";
            label4.Size = new System.Drawing.Size(276, 15);
            label4.TabIndex = 5;
            label4.Text = "Can be IP Address or DNS Alias, Default is localhost";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new System.Drawing.Point(94, 44);
            label3.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            label3.Name = "label3";
            label3.Size = new System.Drawing.Size(32, 15);
            label3.TabIndex = 4;
            label3.Text = "Port:";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new System.Drawing.Point(94, 21);
            label2.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(35, 15);
            label2.TabIndex = 3;
            label2.Text = "Host:";
            // 
            // txt_NetworkConfigAPI_Host
            // 
            txt_NetworkConfigAPI_Host.Location = new System.Drawing.Point(134, 18);
            txt_NetworkConfigAPI_Host.Margin = new System.Windows.Forms.Padding(2, 1, 2, 1);
            txt_NetworkConfigAPI_Host.Name = "txt_NetworkConfigAPI_Host";
            txt_NetworkConfigAPI_Host.Size = new System.Drawing.Size(110, 23);
            txt_NetworkConfigAPI_Host.TabIndex = 1;
            txt_NetworkConfigAPI_Host.Text = "localhost";
            // 
            // chkNetworkConfigAPI
            // 
            chkNetworkConfigAPI.AutoSize = true;
            chkNetworkConfigAPI.Location = new System.Drawing.Point(11, 19);
            chkNetworkConfigAPI.Margin = new System.Windows.Forms.Padding(2, 1, 2, 1);
            chkNetworkConfigAPI.Name = "chkNetworkConfigAPI";
            chkNetworkConfigAPI.Size = new System.Drawing.Size(68, 19);
            chkNetworkConfigAPI.TabIndex = 0;
            chkNetworkConfigAPI.Text = "Enabled";
            chkNetworkConfigAPI.UseVisualStyleBackColor = true;
            chkNetworkConfigAPI.CheckedChanged += chkNetworkConfigAPI_CheckedChanged;
            // 
            // maskNetworkConfig_Port
            // 
            maskNetworkConfig_Port.Location = new System.Drawing.Point(134, 44);
            maskNetworkConfig_Port.Margin = new System.Windows.Forms.Padding(2, 1, 2, 1);
            maskNetworkConfig_Port.Mask = "#####";
            maskNetworkConfig_Port.Name = "maskNetworkConfig_Port";
            maskNetworkConfig_Port.Size = new System.Drawing.Size(58, 23);
            maskNetworkConfig_Port.TabIndex = 86;
            maskNetworkConfig_Port.Text = "0";
            // 
            // ctl_GeneralConfigPage
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(groupBox1);
            Controls.Add(maskStartUpDelay);
            Controls.Add(label1);
            Controls.Add(chkAutoStartDSP);
            Controls.Add(btnLoadConfig);
            Controls.Add(btnSaveConfig);
            Controls.Add(groupBox2);
            Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            Name = "ctl_GeneralConfigPage";
            Size = new System.Drawing.Size(582, 313);
            Load += ctl_GeneralConfigPage_Load;
            groupBox2.ResumeLayout(false);
            groupBox2.PerformLayout();
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }
        #endregion

        protected System.Windows.Forms.GroupBox groupBox2;
        protected System.Windows.Forms.Label label22;
        protected System.Windows.Forms.ListBox lstProcesAffinty;
        protected System.Windows.Forms.Label label21;
        protected System.Windows.Forms.Label label20;
        protected System.Windows.Forms.Label label19;
        protected System.Windows.Forms.ComboBox cboProcessPriority;
        protected System.Windows.Forms.Button btnLoadConfig;
        protected System.Windows.Forms.Button btnSaveConfig;
        protected System.Windows.Forms.CheckBox chkThreading;
        protected System.Windows.Forms.CheckBox chkBackgroundThread;
        protected System.Windows.Forms.SaveFileDialog saveFileDialog1;
        protected System.Windows.Forms.OpenFileDialog openFileDialog1;
        protected System.Windows.Forms.CheckBox chkAutoStartDSP;
        protected System.Windows.Forms.Label label1;
        protected System.Windows.Forms.MaskedTextBox maskStartUpDelay;
        protected System.Windows.Forms.GroupBox groupBox1;
        protected System.Windows.Forms.Label label3;
        protected System.Windows.Forms.Label label2;
        protected System.Windows.Forms.TextBox txt_NetworkConfigAPI_Host;
        protected System.Windows.Forms.CheckBox chkNetworkConfigAPI;
        protected System.Windows.Forms.Label label5;
        protected System.Windows.Forms.Label label4;
        protected System.Windows.Forms.MaskedTextBox maskNetworkConfig_Port;
    }
}
