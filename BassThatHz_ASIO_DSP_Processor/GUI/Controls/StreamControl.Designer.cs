
namespace BassThatHz_ASIO_DSP_Processor.GUI.Controls
{
    partial class StreamControl
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
            label2 = new System.Windows.Forms.Label();
            label1 = new System.Windows.Forms.Label();
            cboInputStream = new System.Windows.Forms.ComboBox();
            label3 = new System.Windows.Forms.Label();
            label4 = new System.Windows.Forms.Label();
            cboOutputStream = new System.Windows.Forms.ComboBox();
            groupBox1 = new System.Windows.Forms.GroupBox();
            btnExportFromREW_API = new System.Windows.Forms.Button();
            txt_REW_ID = new System.Windows.Forms.TextBox();
            label5 = new System.Windows.Forms.Label();
            btnImportFromREW_API = new System.Windows.Forms.Button();
            btnApplyAll = new System.Windows.Forms.Button();
            btnDisableAll = new System.Windows.Forms.Button();
            btnEnableAll = new System.Windows.Forms.Button();
            lblFilterCount = new System.Windows.Forms.Label();
            btnAdd = new System.Windows.Forms.Button();
            panel1 = new System.Windows.Forms.Panel();
            filterControl1 = new FilterControl();
            In_Volume = new BTH_VolumeSlider();
            Out_Volume = new BTH_VolumeSlider();
            btnDelete = new System.Windows.Forms.Button();
            groupBox1.SuspendLayout();
            panel1.SuspendLayout();
            SuspendLayout();
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new System.Drawing.Point(540, 58);
            label2.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(70, 15);
            label2.TabIndex = 140;
            label2.Text = "Out Volume";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(540, 16);
            label1.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(63, 15);
            label1.TabIndex = 138;
            label1.Text = "In Volume:";
            // 
            // cboInputStream
            // 
            cboInputStream.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cboInputStream.FormattingEnabled = true;
            cboInputStream.Location = new System.Drawing.Point(5, 36);
            cboInputStream.Margin = new System.Windows.Forms.Padding(2);
            cboInputStream.Name = "cboInputStream";
            cboInputStream.Size = new System.Drawing.Size(530, 23);
            cboInputStream.TabIndex = 142;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new System.Drawing.Point(5, 16);
            label3.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            label3.Name = "label3";
            label3.Size = new System.Drawing.Size(78, 15);
            label3.TabIndex = 143;
            label3.Text = "Input Stream:";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new System.Drawing.Point(5, 59);
            label4.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            label4.Name = "label4";
            label4.Size = new System.Drawing.Size(88, 15);
            label4.TabIndex = 145;
            label4.Text = "Output Stream:";
            // 
            // cboOutputStream
            // 
            cboOutputStream.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cboOutputStream.FormattingEnabled = true;
            cboOutputStream.Location = new System.Drawing.Point(5, 79);
            cboOutputStream.Margin = new System.Windows.Forms.Padding(2);
            cboOutputStream.Name = "cboOutputStream";
            cboOutputStream.Size = new System.Drawing.Size(530, 23);
            cboOutputStream.TabIndex = 144;
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(btnExportFromREW_API);
            groupBox1.Controls.Add(txt_REW_ID);
            groupBox1.Controls.Add(label5);
            groupBox1.Controls.Add(btnImportFromREW_API);
            groupBox1.Controls.Add(btnApplyAll);
            groupBox1.Controls.Add(btnDisableAll);
            groupBox1.Controls.Add(btnEnableAll);
            groupBox1.Controls.Add(lblFilterCount);
            groupBox1.Controls.Add(btnAdd);
            groupBox1.Controls.Add(panel1);
            groupBox1.Controls.Add(label3);
            groupBox1.Controls.Add(label4);
            groupBox1.Controls.Add(In_Volume);
            groupBox1.Controls.Add(cboOutputStream);
            groupBox1.Controls.Add(label1);
            groupBox1.Controls.Add(Out_Volume);
            groupBox1.Controls.Add(cboInputStream);
            groupBox1.Controls.Add(label2);
            groupBox1.Location = new System.Drawing.Point(78, 2);
            groupBox1.Margin = new System.Windows.Forms.Padding(2);
            groupBox1.Name = "groupBox1";
            groupBox1.Padding = new System.Windows.Forms.Padding(2);
            groupBox1.Size = new System.Drawing.Size(1478, 882);
            groupBox1.TabIndex = 146;
            groupBox1.TabStop = false;
            groupBox1.Text = "Stream";
            // 
            // btnExportFromREW_API
            // 
            btnExportFromREW_API.Location = new System.Drawing.Point(10, 850);
            btnExportFromREW_API.Name = "btnExportFromREW_API";
            btnExportFromREW_API.Size = new System.Drawing.Size(137, 23);
            btnExportFromREW_API.TabIndex = 154;
            btnExportFromREW_API.Text = "Export EQ To REW API";
            btnExportFromREW_API.UseVisualStyleBackColor = true;
            btnExportFromREW_API.Click += btnExportFromREW_API_Click;
            // 
            // txt_REW_ID
            // 
            txt_REW_ID.Location = new System.Drawing.Point(451, 849);
            txt_REW_ID.Name = "txt_REW_ID";
            txt_REW_ID.Size = new System.Drawing.Size(43, 23);
            txt_REW_ID.TabIndex = 153;
            txt_REW_ID.Text = "1";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new System.Drawing.Point(321, 853);
            label5.Name = "label5";
            label5.Size = new System.Drawing.Size(124, 15);
            label5.TabIndex = 152;
            label5.Text = "REW Measurement ID:";
            // 
            // btnImportFromREW_API
            // 
            btnImportFromREW_API.Location = new System.Drawing.Point(153, 849);
            btnImportFromREW_API.Name = "btnImportFromREW_API";
            btnImportFromREW_API.Size = new System.Drawing.Size(159, 23);
            btnImportFromREW_API.TabIndex = 151;
            btnImportFromREW_API.Text = "Import EQ from REW API";
            btnImportFromREW_API.UseVisualStyleBackColor = true;
            btnImportFromREW_API.Click += btnImportFromREW_API_Click;
            // 
            // btnApplyAll
            // 
            btnApplyAll.Location = new System.Drawing.Point(1391, 818);
            btnApplyAll.Margin = new System.Windows.Forms.Padding(2);
            btnApplyAll.Name = "btnApplyAll";
            btnApplyAll.Size = new System.Drawing.Size(76, 24);
            btnApplyAll.TabIndex = 150;
            btnApplyAll.Text = "Apply All";
            btnApplyAll.UseVisualStyleBackColor = true;
            btnApplyAll.Click += btnApplyAll_Click;
            // 
            // btnDisableAll
            // 
            btnDisableAll.Location = new System.Drawing.Point(1258, 818);
            btnDisableAll.Margin = new System.Windows.Forms.Padding(2);
            btnDisableAll.Name = "btnDisableAll";
            btnDisableAll.Size = new System.Drawing.Size(76, 24);
            btnDisableAll.TabIndex = 149;
            btnDisableAll.Text = "Disable All";
            btnDisableAll.UseVisualStyleBackColor = true;
            btnDisableAll.Click += btnDisableAll_Click;
            // 
            // btnEnableAll
            // 
            btnEnableAll.Location = new System.Drawing.Point(1178, 818);
            btnEnableAll.Margin = new System.Windows.Forms.Padding(2);
            btnEnableAll.Name = "btnEnableAll";
            btnEnableAll.Size = new System.Drawing.Size(76, 24);
            btnEnableAll.TabIndex = 148;
            btnEnableAll.Text = "Enable All";
            btnEnableAll.UseVisualStyleBackColor = true;
            btnEnableAll.Click += btnEnableAll_Click;
            // 
            // lblFilterCount
            // 
            lblFilterCount.AutoSize = true;
            lblFilterCount.Location = new System.Drawing.Point(89, 825);
            lblFilterCount.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            lblFilterCount.Name = "lblFilterCount";
            lblFilterCount.Size = new System.Drawing.Size(13, 15);
            lblFilterCount.TabIndex = 147;
            lblFilterCount.Text = "0";
            // 
            // btnAdd
            // 
            btnAdd.Location = new System.Drawing.Point(8, 820);
            btnAdd.Margin = new System.Windows.Forms.Padding(2);
            btnAdd.Name = "btnAdd";
            btnAdd.Size = new System.Drawing.Size(76, 24);
            btnAdd.TabIndex = 1;
            btnAdd.Text = "Add";
            btnAdd.UseVisualStyleBackColor = true;
            btnAdd.Click += btnAdd_Click;
            // 
            // panel1
            // 
            panel1.AutoScroll = true;
            panel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            panel1.Controls.Add(filterControl1);
            panel1.Location = new System.Drawing.Point(8, 104);
            panel1.Margin = new System.Windows.Forms.Padding(2);
            panel1.Name = "panel1";
            panel1.Size = new System.Drawing.Size(1459, 710);
            panel1.TabIndex = 146;
            // 
            // filterControl1
            // 
            filterControl1.BackColor = System.Drawing.Color.White;
            filterControl1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            filterControl1.Location = new System.Drawing.Point(1, 6);
            filterControl1.Margin = new System.Windows.Forms.Padding(2);
            filterControl1.Name = "filterControl1";
            filterControl1.Size = new System.Drawing.Size(1424, 63);
            filterControl1.TabIndex = 0;
            // 
            // In_Volume
            // 
            In_Volume.Location = new System.Drawing.Point(542, 36);
            In_Volume.Margin = new System.Windows.Forms.Padding(2);
            In_Volume.Name = "In_Volume";
            In_Volume.ReadOnly = false;
            In_Volume.Size = new System.Drawing.Size(891, 20);
            In_Volume.TabIndex = 139;
            In_Volume.TextColor = System.Drawing.Color.Black;
            In_Volume.VolumedB = 0D;
            // 
            // Out_Volume
            // 
            Out_Volume.Location = new System.Drawing.Point(542, 79);
            Out_Volume.Margin = new System.Windows.Forms.Padding(2);
            Out_Volume.Name = "Out_Volume";
            Out_Volume.ReadOnly = false;
            Out_Volume.Size = new System.Drawing.Size(891, 20);
            Out_Volume.TabIndex = 141;
            Out_Volume.TextColor = System.Drawing.Color.Black;
            Out_Volume.VolumedB = 0D;
            // 
            // btnDelete
            // 
            btnDelete.Location = new System.Drawing.Point(2, 4);
            btnDelete.Margin = new System.Windows.Forms.Padding(2);
            btnDelete.Name = "btnDelete";
            btnDelete.Size = new System.Drawing.Size(71, 24);
            btnDelete.TabIndex = 147;
            btnDelete.Text = "Delete";
            btnDelete.UseVisualStyleBackColor = true;
            // 
            // StreamControl
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            BackColor = System.Drawing.SystemColors.ControlDark;
            Controls.Add(btnDelete);
            Controls.Add(groupBox1);
            Margin = new System.Windows.Forms.Padding(2);
            Name = "StreamControl";
            Size = new System.Drawing.Size(1559, 893);
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            panel1.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        public System.Windows.Forms.Label label2;
        public BTH_VolumeSlider Out_Volume;
        public System.Windows.Forms.Label label1;
        public BTH_VolumeSlider In_Volume;
        public System.Windows.Forms.ComboBox cboInputStream;
        public System.Windows.Forms.Label label3;
        public System.Windows.Forms.Label label4;
        public System.Windows.Forms.ComboBox cboOutputStream;
        public System.Windows.Forms.GroupBox groupBox1;
        public System.Windows.Forms.Panel panel1;
        public System.Windows.Forms.Button btnAdd;
        public System.Windows.Forms.Button btnDelete;
        public System.Windows.Forms.Label lblFilterCount;
        private FilterControl filterControl1;
        public System.Windows.Forms.Button btnApplyAll;
        public System.Windows.Forms.Button btnDisableAll;
        public System.Windows.Forms.Button btnEnableAll;
        private System.Windows.Forms.Button btnExportFromREW_API;
        private System.Windows.Forms.TextBox txt_REW_ID;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Button btnImportFromREW_API;
    }
}