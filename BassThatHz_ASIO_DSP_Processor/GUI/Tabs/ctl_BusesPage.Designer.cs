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
            ChangeBus_BTN = new System.Windows.Forms.Button();
            DeleteBus_BTN = new System.Windows.Forms.Button();
            SimpleBus_LSB = new System.Windows.Forms.ListBox();
            AddBus_BTN = new System.Windows.Forms.Button();
            label1 = new System.Windows.Forms.Label();
            SimpleBusName_TXT = new System.Windows.Forms.TextBox();
            groupBox2 = new System.Windows.Forms.GroupBox();
            AbstractBusBypass_CHK = new System.Windows.Forms.CheckBox();
            ChangeAbstractBus_BTN = new System.Windows.Forms.Button();
            DeleteAbstractBus_BTN = new System.Windows.Forms.Button();
            label4 = new System.Windows.Forms.Label();
            label3 = new System.Windows.Forms.Label();
            AbstractBusDestination_CBO = new System.Windows.Forms.ComboBox();
            AbstractBusSource_CBO = new System.Windows.Forms.ComboBox();
            AbstractBus_LSB = new System.Windows.Forms.ListBox();
            AddAbstractBus_BTN = new System.Windows.Forms.Button();
            label2 = new System.Windows.Forms.Label();
            AbstractBusName_TXT = new System.Windows.Forms.TextBox();
            groupBox1.SuspendLayout();
            groupBox2.SuspendLayout();
            SuspendLayout();
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(ChangeBus_BTN);
            groupBox1.Controls.Add(DeleteBus_BTN);
            groupBox1.Controls.Add(SimpleBus_LSB);
            groupBox1.Controls.Add(AddBus_BTN);
            groupBox1.Controls.Add(label1);
            groupBox1.Controls.Add(SimpleBusName_TXT);
            groupBox1.Location = new System.Drawing.Point(7, 12);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new System.Drawing.Size(353, 477);
            groupBox1.TabIndex = 3;
            groupBox1.TabStop = false;
            groupBox1.Text = "Simple Bus:";
            // 
            // ChangeBus_BTN
            // 
            ChangeBus_BTN.Location = new System.Drawing.Point(263, 45);
            ChangeBus_BTN.Name = "ChangeBus_BTN";
            ChangeBus_BTN.Size = new System.Drawing.Size(75, 23);
            ChangeBus_BTN.TabIndex = 8;
            ChangeBus_BTN.Text = "Change";
            ChangeBus_BTN.UseVisualStyleBackColor = true;
            ChangeBus_BTN.Click += ChangeBus_BTN_Click;
            // 
            // DeleteBus_BTN
            // 
            DeleteBus_BTN.Location = new System.Drawing.Point(263, 75);
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
            SimpleBus_LSB.Location = new System.Drawing.Point(6, 105);
            SimpleBus_LSB.Name = "SimpleBus_LSB";
            SimpleBus_LSB.ScrollAlwaysVisible = true;
            SimpleBus_LSB.Size = new System.Drawing.Size(251, 364);
            SimpleBus_LSB.TabIndex = 6;
            SimpleBus_LSB.SelectedIndexChanged += SimpleBus_LSB_SelectedIndexChanged;
            // 
            // AddBus_BTN
            // 
            AddBus_BTN.Location = new System.Drawing.Point(182, 45);
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
            // groupBox2
            // 
            groupBox2.Controls.Add(AbstractBusBypass_CHK);
            groupBox2.Controls.Add(ChangeAbstractBus_BTN);
            groupBox2.Controls.Add(DeleteAbstractBus_BTN);
            groupBox2.Controls.Add(label4);
            groupBox2.Controls.Add(label3);
            groupBox2.Controls.Add(AbstractBusDestination_CBO);
            groupBox2.Controls.Add(AbstractBusSource_CBO);
            groupBox2.Controls.Add(AbstractBus_LSB);
            groupBox2.Controls.Add(AddAbstractBus_BTN);
            groupBox2.Controls.Add(label2);
            groupBox2.Controls.Add(AbstractBusName_TXT);
            groupBox2.Location = new System.Drawing.Point(375, 12);
            groupBox2.Name = "groupBox2";
            groupBox2.Size = new System.Drawing.Size(714, 477);
            groupBox2.TabIndex = 4;
            groupBox2.TabStop = false;
            groupBox2.Text = "Abstract Bus:";
            // 
            // AbstractBusBypass_CHK
            // 
            AbstractBusBypass_CHK.AutoSize = true;
            AbstractBusBypass_CHK.Location = new System.Drawing.Point(8, 80);
            AbstractBusBypass_CHK.Name = "AbstractBusBypass_CHK";
            AbstractBusBypass_CHK.Size = new System.Drawing.Size(62, 19);
            AbstractBusBypass_CHK.TabIndex = 17;
            AbstractBusBypass_CHK.Text = "Bypass";
            AbstractBusBypass_CHK.UseVisualStyleBackColor = true;
            // 
            // ChangeAbstractBus_BTN
            // 
            ChangeAbstractBus_BTN.Location = new System.Drawing.Point(626, 46);
            ChangeAbstractBus_BTN.Name = "ChangeAbstractBus_BTN";
            ChangeAbstractBus_BTN.Size = new System.Drawing.Size(75, 23);
            ChangeAbstractBus_BTN.TabIndex = 16;
            ChangeAbstractBus_BTN.Text = "Change";
            ChangeAbstractBus_BTN.UseVisualStyleBackColor = true;
            ChangeAbstractBus_BTN.Click += ChangeAbstractBus_BTN_Click;
            // 
            // DeleteAbstractBus_BTN
            // 
            DeleteAbstractBus_BTN.Location = new System.Drawing.Point(626, 75);
            DeleteAbstractBus_BTN.Name = "DeleteAbstractBus_BTN";
            DeleteAbstractBus_BTN.Size = new System.Drawing.Size(75, 23);
            DeleteAbstractBus_BTN.TabIndex = 15;
            DeleteAbstractBus_BTN.Text = "Delete";
            DeleteAbstractBus_BTN.UseVisualStyleBackColor = true;
            DeleteAbstractBus_BTN.Click += DeleteAbstractBus_BTN_Click;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new System.Drawing.Point(355, 28);
            label4.Name = "label4";
            label4.Size = new System.Drawing.Size(70, 15);
            label4.TabIndex = 14;
            label4.Text = "Destination:";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new System.Drawing.Point(171, 28);
            label3.Name = "label3";
            label3.Size = new System.Drawing.Size(46, 15);
            label3.TabIndex = 13;
            label3.Text = "Source:";
            // 
            // AbstractBusDestination_CBO
            // 
            AbstractBusDestination_CBO.DisplayMember = "DisplayMember";
            AbstractBusDestination_CBO.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            AbstractBusDestination_CBO.FormattingEnabled = true;
            AbstractBusDestination_CBO.Location = new System.Drawing.Point(355, 46);
            AbstractBusDestination_CBO.Name = "AbstractBusDestination_CBO";
            AbstractBusDestination_CBO.Size = new System.Drawing.Size(175, 23);
            AbstractBusDestination_CBO.TabIndex = 12;
            // 
            // AbstractBusSource_CBO
            // 
            AbstractBusSource_CBO.DisplayMember = "DisplayMember";
            AbstractBusSource_CBO.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            AbstractBusSource_CBO.FormattingEnabled = true;
            AbstractBusSource_CBO.Location = new System.Drawing.Point(171, 46);
            AbstractBusSource_CBO.Name = "AbstractBusSource_CBO";
            AbstractBusSource_CBO.Size = new System.Drawing.Size(175, 23);
            AbstractBusSource_CBO.TabIndex = 11;
            // 
            // AbstractBus_LSB
            // 
            AbstractBus_LSB.DisplayMember = "DisplayMember";
            AbstractBus_LSB.FormattingEnabled = true;
            AbstractBus_LSB.Location = new System.Drawing.Point(6, 105);
            AbstractBus_LSB.Name = "AbstractBus_LSB";
            AbstractBus_LSB.ScrollAlwaysVisible = true;
            AbstractBus_LSB.Size = new System.Drawing.Size(614, 364);
            AbstractBus_LSB.TabIndex = 10;
            AbstractBus_LSB.SelectedIndexChanged += AbstractBus_LSB_SelectedIndexChanged;
            // 
            // AddAbstractBus_BTN
            // 
            AddAbstractBus_BTN.Location = new System.Drawing.Point(545, 46);
            AddAbstractBus_BTN.Name = "AddAbstractBus_BTN";
            AddAbstractBus_BTN.Size = new System.Drawing.Size(75, 23);
            AddAbstractBus_BTN.TabIndex = 9;
            AddAbstractBus_BTN.Text = "Add";
            AddAbstractBus_BTN.UseVisualStyleBackColor = true;
            AddAbstractBus_BTN.Click += AddAbstractBus_BTN_Click;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new System.Drawing.Point(6, 28);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(64, 15);
            label2.TabIndex = 8;
            label2.Text = "Bus Name:";
            // 
            // AbstractBusName_TXT
            // 
            AbstractBusName_TXT.Location = new System.Drawing.Point(6, 46);
            AbstractBusName_TXT.Name = "AbstractBusName_TXT";
            AbstractBusName_TXT.Size = new System.Drawing.Size(154, 23);
            AbstractBusName_TXT.TabIndex = 7;
            // 
            // ctl_BusesPage
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            AutoScroll = true;
            Controls.Add(groupBox2);
            Controls.Add(groupBox1);
            Name = "ctl_BusesPage";
            Size = new System.Drawing.Size(1120, 515);
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
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ComboBox AbstractBusDestination_CBO;
        private System.Windows.Forms.ComboBox AbstractBusSource_CBO;
        private System.Windows.Forms.ListBox AbstractBus_LSB;
        private System.Windows.Forms.Button AddAbstractBus_BTN;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox AbstractBusName_TXT;
        private System.Windows.Forms.Button DeleteBus_BTN;
        private System.Windows.Forms.Button AddBus_BTN;
        private System.Windows.Forms.Button DeleteAbstractBus_BTN;
        private System.Windows.Forms.Button ChangeBus_BTN;
        private System.Windows.Forms.Button ChangeAbstractBus_BTN;
        private System.Windows.Forms.CheckBox AbstractBusBypass_CHK;
    }
}
