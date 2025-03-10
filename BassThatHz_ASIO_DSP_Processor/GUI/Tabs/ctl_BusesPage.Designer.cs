namespace BassThatHz_ASIO_DSP_Processor.GUI.Tabs
{
    partial class ctl_BusesPage
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
            groupBox1 = new System.Windows.Forms.GroupBox();
            Bus_Bypass_CHK = new System.Windows.Forms.CheckBox();
            ChangeBus_BTN = new System.Windows.Forms.Button();
            DeleteBus_BTN = new System.Windows.Forms.Button();
            SimpleBus_LSB = new System.Windows.Forms.ListBox();
            AddBus_BTN = new System.Windows.Forms.Button();
            label1 = new System.Windows.Forms.Label();
            SimpleBusName_TXT = new System.Windows.Forms.TextBox();
            AbstractBusName_TXT = new System.Windows.Forms.TextBox();
            label2 = new System.Windows.Forms.Label();
            AddAbstractBus_BTN = new System.Windows.Forms.Button();
            AbstractBuses_LSB = new System.Windows.Forms.ListBox();
            DeleteAbstractBus_BTN = new System.Windows.Forms.Button();
            ChangeAbstractBus_BTN = new System.Windows.Forms.Button();
            groupBox2 = new System.Windows.Forms.GroupBox();
            label6 = new System.Windows.Forms.Label();
            AbstractBus_SubList_Change_BTN = new System.Windows.Forms.Button();
            AbstractBus_SubList_Delete_BTN = new System.Windows.Forms.Button();
            AbstractBus_SubList_Add_BTN = new System.Windows.Forms.Button();
            label4 = new System.Windows.Forms.Label();
            label3 = new System.Windows.Forms.Label();
            AbstractBusDestination_CBO = new System.Windows.Forms.ComboBox();
            AbstractBusSource_CBO = new System.Windows.Forms.ComboBox();
            AbstractBuses_SubList_LSB = new System.Windows.Forms.ListBox();
            AbstractBus_SubItem_Bypass_CHK = new System.Windows.Forms.CheckBox();
            label5 = new System.Windows.Forms.Label();
            AbstractBus_Bypass_CHK = new System.Windows.Forms.CheckBox();
            groupBox1.SuspendLayout();
            groupBox2.SuspendLayout();
            SuspendLayout();
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(Bus_Bypass_CHK);
            groupBox1.Controls.Add(ChangeBus_BTN);
            groupBox1.Controls.Add(DeleteBus_BTN);
            groupBox1.Controls.Add(SimpleBus_LSB);
            groupBox1.Controls.Add(AddBus_BTN);
            groupBox1.Controls.Add(label1);
            groupBox1.Controls.Add(SimpleBusName_TXT);
            groupBox1.Location = new System.Drawing.Point(7, 12);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new System.Drawing.Size(401, 601);
            groupBox1.TabIndex = 3;
            groupBox1.TabStop = false;
            groupBox1.Text = "Simple Bus:";
            // 
            // Bus_Bypass_CHK
            // 
            Bus_Bypass_CHK.AutoSize = true;
            Bus_Bypass_CHK.Location = new System.Drawing.Point(166, 49);
            Bus_Bypass_CHK.Name = "Bus_Bypass_CHK";
            Bus_Bypass_CHK.Size = new System.Drawing.Size(62, 19);
            Bus_Bypass_CHK.TabIndex = 27;
            Bus_Bypass_CHK.Text = "Bypass";
            Bus_Bypass_CHK.UseVisualStyleBackColor = true;
            // 
            // ChangeBus_BTN
            // 
            ChangeBus_BTN.Location = new System.Drawing.Point(313, 46);
            ChangeBus_BTN.Name = "ChangeBus_BTN";
            ChangeBus_BTN.Size = new System.Drawing.Size(75, 23);
            ChangeBus_BTN.TabIndex = 8;
            ChangeBus_BTN.Text = "Change";
            ChangeBus_BTN.UseVisualStyleBackColor = true;
            ChangeBus_BTN.Click += ChangeBus_BTN_Click;
            // 
            // DeleteBus_BTN
            // 
            DeleteBus_BTN.Location = new System.Drawing.Point(313, 76);
            DeleteBus_BTN.Name = "DeleteBus_BTN";
            DeleteBus_BTN.Size = new System.Drawing.Size(75, 23);
            DeleteBus_BTN.TabIndex = 7;
            DeleteBus_BTN.Text = "Delete";
            DeleteBus_BTN.UseVisualStyleBackColor = true;
            DeleteBus_BTN.Click += DeleteBus_BTN_Click;
            // 
            // SimpleBus_LSB
            // 
            SimpleBus_LSB.FormattingEnabled = true;
            SimpleBus_LSB.Location = new System.Drawing.Point(6, 75);
            SimpleBus_LSB.Name = "SimpleBus_LSB";
            SimpleBus_LSB.ScrollAlwaysVisible = true;
            SimpleBus_LSB.Size = new System.Drawing.Size(300, 514);
            SimpleBus_LSB.TabIndex = 6;
            SimpleBus_LSB.SelectedIndexChanged += SimpleBus_LSB_SelectedIndexChanged;
            // 
            // AddBus_BTN
            // 
            AddBus_BTN.Location = new System.Drawing.Point(231, 44);
            AddBus_BTN.Name = "AddBus_BTN";
            AddBus_BTN.Size = new System.Drawing.Size(75, 23);
            AddBus_BTN.TabIndex = 5;
            AddBus_BTN.Text = "Add";
            AddBus_BTN.UseVisualStyleBackColor = true;
            AddBus_BTN.Click += AddBus_BTN_Click;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(6, 28);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(64, 15);
            label1.TabIndex = 4;
            label1.Text = "Bus Name:";
            // 
            // SimpleBusName_TXT
            // 
            SimpleBusName_TXT.Location = new System.Drawing.Point(6, 46);
            SimpleBusName_TXT.Name = "SimpleBusName_TXT";
            SimpleBusName_TXT.Size = new System.Drawing.Size(154, 23);
            SimpleBusName_TXT.TabIndex = 3;
            // 
            // AbstractBusName_TXT
            // 
            AbstractBusName_TXT.Location = new System.Drawing.Point(484, 43);
            AbstractBusName_TXT.Name = "AbstractBusName_TXT";
            AbstractBusName_TXT.Size = new System.Drawing.Size(154, 23);
            AbstractBusName_TXT.TabIndex = 7;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new System.Drawing.Point(484, 25);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(64, 15);
            label2.TabIndex = 8;
            label2.Text = "Bus Name:";
            // 
            // AddAbstractBus_BTN
            // 
            AddAbstractBus_BTN.Location = new System.Drawing.Point(710, 45);
            AddAbstractBus_BTN.Name = "AddAbstractBus_BTN";
            AddAbstractBus_BTN.Size = new System.Drawing.Size(75, 23);
            AddAbstractBus_BTN.TabIndex = 9;
            AddAbstractBus_BTN.Text = "Add";
            AddAbstractBus_BTN.UseVisualStyleBackColor = true;
            AddAbstractBus_BTN.Click += AddAbstractBus_BTN_Click;
            // 
            // AbstractBuses_LSB
            // 
            AbstractBuses_LSB.DisplayMember = "DisplayMember";
            AbstractBuses_LSB.FormattingEnabled = true;
            AbstractBuses_LSB.Location = new System.Drawing.Point(6, 75);
            AbstractBuses_LSB.Name = "AbstractBuses_LSB";
            AbstractBuses_LSB.ScrollAlwaysVisible = true;
            AbstractBuses_LSB.Size = new System.Drawing.Size(783, 214);
            AbstractBuses_LSB.TabIndex = 10;
            AbstractBuses_LSB.SelectedIndexChanged += AbstractBus_LSB_SelectedIndexChanged;
            // 
            // DeleteAbstractBus_BTN
            // 
            DeleteAbstractBus_BTN.Location = new System.Drawing.Point(795, 72);
            DeleteAbstractBus_BTN.Name = "DeleteAbstractBus_BTN";
            DeleteAbstractBus_BTN.Size = new System.Drawing.Size(75, 23);
            DeleteAbstractBus_BTN.TabIndex = 15;
            DeleteAbstractBus_BTN.Text = "Delete";
            DeleteAbstractBus_BTN.UseVisualStyleBackColor = true;
            DeleteAbstractBus_BTN.Click += DeleteAbstractBus_BTN_Click;
            // 
            // ChangeAbstractBus_BTN
            // 
            ChangeAbstractBus_BTN.Location = new System.Drawing.Point(795, 43);
            ChangeAbstractBus_BTN.Name = "ChangeAbstractBus_BTN";
            ChangeAbstractBus_BTN.Size = new System.Drawing.Size(75, 23);
            ChangeAbstractBus_BTN.TabIndex = 16;
            ChangeAbstractBus_BTN.Text = "Change";
            ChangeAbstractBus_BTN.UseVisualStyleBackColor = true;
            ChangeAbstractBus_BTN.Click += ChangeAbstractBus_BTN_Click;
            // 
            // groupBox2
            // 
            groupBox2.Controls.Add(label6);
            groupBox2.Controls.Add(AbstractBus_SubList_Change_BTN);
            groupBox2.Controls.Add(AbstractBus_SubList_Delete_BTN);
            groupBox2.Controls.Add(AbstractBus_SubList_Add_BTN);
            groupBox2.Controls.Add(label4);
            groupBox2.Controls.Add(label3);
            groupBox2.Controls.Add(AbstractBusDestination_CBO);
            groupBox2.Controls.Add(AbstractBusSource_CBO);
            groupBox2.Controls.Add(AbstractBuses_SubList_LSB);
            groupBox2.Controls.Add(AbstractBus_SubItem_Bypass_CHK);
            groupBox2.Controls.Add(label5);
            groupBox2.Controls.Add(AbstractBus_Bypass_CHK);
            groupBox2.Controls.Add(ChangeAbstractBus_BTN);
            groupBox2.Controls.Add(DeleteAbstractBus_BTN);
            groupBox2.Controls.Add(AbstractBuses_LSB);
            groupBox2.Controls.Add(AddAbstractBus_BTN);
            groupBox2.Controls.Add(label2);
            groupBox2.Controls.Add(AbstractBusName_TXT);
            groupBox2.Location = new System.Drawing.Point(426, 12);
            groupBox2.Name = "groupBox2";
            groupBox2.Size = new System.Drawing.Size(888, 601);
            groupBox2.TabIndex = 4;
            groupBox2.TabStop = false;
            groupBox2.Text = "Abstract Bus:";
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new System.Drawing.Point(10, 328);
            label6.Name = "label6";
            label6.Size = new System.Drawing.Size(114, 15);
            label6.TabIndex = 48;
            label6.Text = "Concrete Mappings:";
            // 
            // AbstractBus_SubList_Change_BTN
            // 
            AbstractBus_SubList_Change_BTN.Location = new System.Drawing.Point(795, 317);
            AbstractBus_SubList_Change_BTN.Name = "AbstractBus_SubList_Change_BTN";
            AbstractBus_SubList_Change_BTN.Size = new System.Drawing.Size(75, 23);
            AbstractBus_SubList_Change_BTN.TabIndex = 47;
            AbstractBus_SubList_Change_BTN.Text = "Change";
            AbstractBus_SubList_Change_BTN.UseVisualStyleBackColor = true;
            AbstractBus_SubList_Change_BTN.Click += AbstractBus_SubList_Change_BTN_Click;
            // 
            // AbstractBus_SubList_Delete_BTN
            // 
            AbstractBus_SubList_Delete_BTN.Location = new System.Drawing.Point(795, 346);
            AbstractBus_SubList_Delete_BTN.Name = "AbstractBus_SubList_Delete_BTN";
            AbstractBus_SubList_Delete_BTN.Size = new System.Drawing.Size(75, 23);
            AbstractBus_SubList_Delete_BTN.TabIndex = 46;
            AbstractBus_SubList_Delete_BTN.Text = "Delete";
            AbstractBus_SubList_Delete_BTN.UseVisualStyleBackColor = true;
            AbstractBus_SubList_Delete_BTN.Click += AbstractBus_SubList_Delete_BTN_Click;
            // 
            // AbstractBus_SubList_Add_BTN
            // 
            AbstractBus_SubList_Add_BTN.Location = new System.Drawing.Point(713, 318);
            AbstractBus_SubList_Add_BTN.Name = "AbstractBus_SubList_Add_BTN";
            AbstractBus_SubList_Add_BTN.Size = new System.Drawing.Size(75, 23);
            AbstractBus_SubList_Add_BTN.TabIndex = 45;
            AbstractBus_SubList_Add_BTN.Text = "Add";
            AbstractBus_SubList_Add_BTN.UseVisualStyleBackColor = true;
            AbstractBus_SubList_Add_BTN.Click += AbstractBus_SubList_Add_BTN_Click;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new System.Drawing.Point(390, 301);
            label4.Name = "label4";
            label4.Size = new System.Drawing.Size(70, 15);
            label4.TabIndex = 44;
            label4.Text = "Destination:";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new System.Drawing.Point(129, 301);
            label3.Name = "label3";
            label3.Size = new System.Drawing.Size(46, 15);
            label3.TabIndex = 43;
            label3.Text = "Source:";
            // 
            // AbstractBusDestination_CBO
            // 
            AbstractBusDestination_CBO.DisplayMember = "DisplayMember";
            AbstractBusDestination_CBO.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            AbstractBusDestination_CBO.FormattingEnabled = true;
            AbstractBusDestination_CBO.Location = new System.Drawing.Point(390, 318);
            AbstractBusDestination_CBO.Name = "AbstractBusDestination_CBO";
            AbstractBusDestination_CBO.Size = new System.Drawing.Size(248, 23);
            AbstractBusDestination_CBO.TabIndex = 42;
            // 
            // AbstractBusSource_CBO
            // 
            AbstractBusSource_CBO.DisplayMember = "DisplayMember";
            AbstractBusSource_CBO.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            AbstractBusSource_CBO.FormattingEnabled = true;
            AbstractBusSource_CBO.Location = new System.Drawing.Point(129, 319);
            AbstractBusSource_CBO.Name = "AbstractBusSource_CBO";
            AbstractBusSource_CBO.Size = new System.Drawing.Size(248, 23);
            AbstractBusSource_CBO.TabIndex = 41;
            // 
            // AbstractBuses_SubList_LSB
            // 
            AbstractBuses_SubList_LSB.DisplayMember = "DisplayMember";
            AbstractBuses_SubList_LSB.FormattingEnabled = true;
            AbstractBuses_SubList_LSB.Location = new System.Drawing.Point(6, 350);
            AbstractBuses_SubList_LSB.Name = "AbstractBuses_SubList_LSB";
            AbstractBuses_SubList_LSB.ScrollAlwaysVisible = true;
            AbstractBuses_SubList_LSB.Size = new System.Drawing.Size(783, 229);
            AbstractBuses_SubList_LSB.TabIndex = 40;
            AbstractBuses_SubList_LSB.SelectedIndexChanged += AbstractBuses_SubList_LSB_SelectedIndexChanged;
            // 
            // AbstractBus_SubItem_Bypass_CHK
            // 
            AbstractBus_SubItem_Bypass_CHK.AutoSize = true;
            AbstractBus_SubItem_Bypass_CHK.Enabled = false;
            AbstractBus_SubItem_Bypass_CHK.Location = new System.Drawing.Point(648, 321);
            AbstractBus_SubItem_Bypass_CHK.Name = "AbstractBus_SubItem_Bypass_CHK";
            AbstractBus_SubItem_Bypass_CHK.Size = new System.Drawing.Size(62, 19);
            AbstractBus_SubItem_Bypass_CHK.TabIndex = 39;
            AbstractBus_SubItem_Bypass_CHK.Text = "Bypass";
            AbstractBus_SubItem_Bypass_CHK.UseVisualStyleBackColor = true;
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new System.Drawing.Point(10, 53);
            label5.Name = "label5";
            label5.Size = new System.Drawing.Size(87, 15);
            label5.TabIndex = 27;
            label5.Text = "Abstract Buses:";
            // 
            // AbstractBus_Bypass_CHK
            // 
            AbstractBus_Bypass_CHK.AutoSize = true;
            AbstractBus_Bypass_CHK.Location = new System.Drawing.Point(645, 49);
            AbstractBus_Bypass_CHK.Name = "AbstractBus_Bypass_CHK";
            AbstractBus_Bypass_CHK.Size = new System.Drawing.Size(62, 19);
            AbstractBus_Bypass_CHK.TabIndex = 26;
            AbstractBus_Bypass_CHK.Text = "Bypass";
            AbstractBus_Bypass_CHK.UseVisualStyleBackColor = true;
            // 
            // ctl_BusesPage
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            AutoScroll = true;
            Controls.Add(groupBox2);
            Controls.Add(groupBox1);
            Name = "ctl_BusesPage";
            Size = new System.Drawing.Size(1354, 639);
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            groupBox2.ResumeLayout(false);
            groupBox2.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.ListBox SimpleBus_LSB;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox SimpleBusName_TXT;
        private System.Windows.Forms.Button DeleteBus_BTN;
        private System.Windows.Forms.Button AddBus_BTN;
        private System.Windows.Forms.Button ChangeBus_BTN;
        private System.Windows.Forms.TextBox AbstractBusName_TXT;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button AddAbstractBus_BTN;
        private System.Windows.Forms.ListBox AbstractBuses_LSB;
        private System.Windows.Forms.Button DeleteAbstractBus_BTN;
        private System.Windows.Forms.Button ChangeAbstractBus_BTN;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.CheckBox AbstractBus_Bypass_CHK;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.CheckBox Bus_Bypass_CHK;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Button AbstractBus_SubList_Change_BTN;
        private System.Windows.Forms.Button AbstractBus_SubList_Delete_BTN;
        private System.Windows.Forms.Button AbstractBus_SubList_Add_BTN;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ComboBox AbstractBusDestination_CBO;
        private System.Windows.Forms.ComboBox AbstractBusSource_CBO;
        private System.Windows.Forms.ListBox AbstractBuses_SubList_LSB;
        private System.Windows.Forms.CheckBox AbstractBus_SubItem_Bypass_CHK;
    }
}
