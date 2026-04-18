namespace BassThatHz_ASIO_DSP_Processor.GUI.Tabs
{
    using System.Windows.Forms.Integration;

    partial class ctl_UpmixerPage
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
            elementHost3D = new ElementHost();
            listBox1 = new System.Windows.Forms.ListBox();
            label1 = new System.Windows.Forms.Label();
            button1 = new System.Windows.Forms.Button();
            listBox2 = new System.Windows.Forms.ListBox();
            groupBox1 = new System.Windows.Forms.GroupBox();
            button8 = new System.Windows.Forms.Button();
            button9 = new System.Windows.Forms.Button();
            button6 = new System.Windows.Forms.Button();
            button7 = new System.Windows.Forms.Button();
            button4 = new System.Windows.Forms.Button();
            button5 = new System.Windows.Forms.Button();
            button3 = new System.Windows.Forms.Button();
            button2 = new System.Windows.Forms.Button();
            groupBox2 = new System.Windows.Forms.GroupBox();
            button12 = new System.Windows.Forms.Button();
            button13 = new System.Windows.Forms.Button();
            button14 = new System.Windows.Forms.Button();
            button15 = new System.Windows.Forms.Button();
            button16 = new System.Windows.Forms.Button();
            button17 = new System.Windows.Forms.Button();
            groupBox3 = new System.Windows.Forms.GroupBox();
            label11 = new System.Windows.Forms.Label();
            label12 = new System.Windows.Forms.Label();
            label13 = new System.Windows.Forms.Label();
            label8 = new System.Windows.Forms.Label();
            label9 = new System.Windows.Forms.Label();
            label10 = new System.Windows.Forms.Label();
            label5 = new System.Windows.Forms.Label();
            label6 = new System.Windows.Forms.Label();
            label7 = new System.Windows.Forms.Label();
            label4 = new System.Windows.Forms.Label();
            label3 = new System.Windows.Forms.Label();
            label2 = new System.Windows.Forms.Label();
            panel1 = new System.Windows.Forms.Panel();
            groupBox1.SuspendLayout();
            groupBox2.SuspendLayout();
            groupBox3.SuspendLayout();
            panel1.SuspendLayout();
            SuspendLayout();
            // 
            // elementHost3D
            // 
            elementHost3D.Dock = System.Windows.Forms.DockStyle.Fill;
            elementHost3D.Location = new System.Drawing.Point(0, 0);
            elementHost3D.Name = "elementHost3D";
            elementHost3D.Size = new System.Drawing.Size(1002, 759);
            elementHost3D.TabIndex = 16;
            elementHost3D.Text = "elementHost3D";
            // 
            // listBox1
            // 
            listBox1.FormattingEnabled = true;
            listBox1.Items.AddRange(new object[] { "5.1               (6 channel)", "5.1.2            (8 channel)", "5.1.4            (10 channel)", "7.1               (8 channel)", "7.1.2            (10 channel)", "7.1.4            (12 channel)", "7.2.4            (13 channel)", "7.1.6            (14 channel)", "7.2.6            (15 channel)", "7.4.6            (17 channel)", "9.1.2            (12 channel)", "9.2.2            (13 channel)", "9.1.4            (14 channel)", "9.2.4            (15 channel)", "9.1.6            (16 channel)", "9.2.6            (17 channel)", "9.4.6            (19 channel)", "11.1.2          (14 channel)", "11.2.2          (15 channel)", "11.1.4          (16 channel)", "11.2.4          (17 channel)", "16.2.14         (32 channel)", "16.4.12         (32 channel)", "24.4.10\t  (38 channel)", "32.8.24        (64 channels)", "44.42.42      (128 channels)" });
            listBox1.Location = new System.Drawing.Point(3, 33);
            listBox1.Name = "listBox1";
            listBox1.Size = new System.Drawing.Size(186, 874);
            listBox1.TabIndex = 0;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(3, 11);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(97, 15);
            label1.TabIndex = 1;
            label1.Text = "Select 3D Layout:";
            // 
            // button1
            // 
            button1.Location = new System.Drawing.Point(208, 7);
            button1.Name = "button1";
            button1.Size = new System.Drawing.Size(143, 23);
            button1.TabIndex = 2;
            button1.Text = "Generate Layout Buses";
            button1.UseVisualStyleBackColor = true;
            // 
            // listBox2
            // 
            listBox2.FormattingEnabled = true;
            listBox2.Items.AddRange(new object[] { "3D Left", "3D Right", "3D Center", "3D Wide Left", "3D Wide Right", "3D Side Left", "3D Side Right", "3D Rear Left", "3D Rear Right", "3D Rear Left2", "3D Rear Center", "3D Rear Right2", "3D LFE 1", "3D LFE 2", "3D LFE 3", "3D LFE 4", "3D Height 1", "3D Height 2", "3D Height 3", "3D Height 4", "3D Height 5", "3D Height 6", "3D Height 7", "3D Height 8", "3D Height 9", "3D Height 10", "3D Height 11", "3D Height 12", "3D Height 13", "3D Height 14", "3D VOG", "3D Screen Left", "3D Screen Right", "3D Screen Top", "3D Screen Bottom", "3D Top Front Left", "3D Top Front Right", "3D Top Wide Left", "3D Top Wide Right", "3D Top Side Left", "3D Top Side Right", "3D Top Rear Left", "3D Top Rear Left2", "3D Top Rear Center", "3D Top Rear Right", "3D Top Rear Right2", "3D Bottom Front Left", "3D Bottom Front Right", "3D Bottom Wide Left", "3D Bottom Wide Right", "3D Bottom Side Left", "3D Bottom Side Right", "3D Bottom Rear Left", "3D Bottom Rear Left1", "3D Bottom Rear Center", "3D Bottom Rear Right", "3D Bottom Rear Right2" });
            listBox2.Location = new System.Drawing.Point(208, 33);
            listBox2.Name = "listBox2";
            listBox2.Size = new System.Drawing.Size(143, 874);
            listBox2.TabIndex = 3;
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(button8);
            groupBox1.Controls.Add(button9);
            groupBox1.Controls.Add(button6);
            groupBox1.Controls.Add(button7);
            groupBox1.Controls.Add(button4);
            groupBox1.Controls.Add(button5);
            groupBox1.Controls.Add(button3);
            groupBox1.Controls.Add(button2);
            groupBox1.Location = new System.Drawing.Point(358, 773);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new System.Drawing.Size(356, 134);
            groupBox1.TabIndex = 13;
            groupBox1.TabStop = false;
            groupBox1.Text = "Room";
            // 
            // button8
            // 
            button8.Location = new System.Drawing.Point(249, 51);
            button8.Name = "button8";
            button8.Size = new System.Drawing.Size(75, 23);
            button8.TabIndex = 20;
            button8.Text = "Zoom -";
            button8.UseVisualStyleBackColor = true;
            // 
            // button9
            // 
            button9.Location = new System.Drawing.Point(249, 22);
            button9.Name = "button9";
            button9.Size = new System.Drawing.Size(75, 23);
            button9.TabIndex = 19;
            button9.Text = "Zoom +";
            button9.UseVisualStyleBackColor = true;
            // 
            // button6
            // 
            button6.Location = new System.Drawing.Point(168, 51);
            button6.Name = "button6";
            button6.Size = new System.Drawing.Size(75, 23);
            button6.TabIndex = 18;
            button6.Text = "Roll -";
            button6.UseVisualStyleBackColor = true;
            // 
            // button7
            // 
            button7.Location = new System.Drawing.Point(168, 22);
            button7.Name = "button7";
            button7.Size = new System.Drawing.Size(75, 23);
            button7.TabIndex = 17;
            button7.Text = "Roll +";
            button7.UseVisualStyleBackColor = true;
            // 
            // button4
            // 
            button4.Location = new System.Drawing.Point(6, 51);
            button4.Name = "button4";
            button4.Size = new System.Drawing.Size(75, 23);
            button4.TabIndex = 16;
            button4.Text = "Pitch -";
            button4.UseVisualStyleBackColor = true;
            // 
            // button5
            // 
            button5.Location = new System.Drawing.Point(6, 22);
            button5.Name = "button5";
            button5.Size = new System.Drawing.Size(75, 23);
            button5.TabIndex = 15;
            button5.Text = "Pitch +";
            button5.UseVisualStyleBackColor = true;
            // 
            // button3
            // 
            button3.Location = new System.Drawing.Point(87, 51);
            button3.Name = "button3";
            button3.Size = new System.Drawing.Size(75, 23);
            button3.TabIndex = 14;
            button3.Text = "Yaw -";
            button3.UseVisualStyleBackColor = true;
            // 
            // button2
            // 
            button2.Location = new System.Drawing.Point(87, 22);
            button2.Name = "button2";
            button2.Size = new System.Drawing.Size(75, 23);
            button2.TabIndex = 13;
            button2.Text = "Yaw +";
            button2.UseVisualStyleBackColor = true;
            // 
            // groupBox2
            // 
            groupBox2.Controls.Add(button12);
            groupBox2.Controls.Add(button13);
            groupBox2.Controls.Add(button14);
            groupBox2.Controls.Add(button15);
            groupBox2.Controls.Add(button16);
            groupBox2.Controls.Add(button17);
            groupBox2.Location = new System.Drawing.Point(720, 773);
            groupBox2.Name = "groupBox2";
            groupBox2.Size = new System.Drawing.Size(255, 137);
            groupBox2.TabIndex = 14;
            groupBox2.TabStop = false;
            groupBox2.Text = "Speaker";
            // 
            // button12
            // 
            button12.Location = new System.Drawing.Point(168, 51);
            button12.Name = "button12";
            button12.Size = new System.Drawing.Size(75, 23);
            button12.TabIndex = 18;
            button12.Text = "Roll -";
            button12.UseVisualStyleBackColor = true;
            // 
            // button13
            // 
            button13.Location = new System.Drawing.Point(168, 22);
            button13.Name = "button13";
            button13.Size = new System.Drawing.Size(75, 23);
            button13.TabIndex = 17;
            button13.Text = "Roll +";
            button13.UseVisualStyleBackColor = true;
            // 
            // button14
            // 
            button14.Location = new System.Drawing.Point(6, 51);
            button14.Name = "button14";
            button14.Size = new System.Drawing.Size(75, 23);
            button14.TabIndex = 16;
            button14.Text = "Pitch -";
            button14.UseVisualStyleBackColor = true;
            // 
            // button15
            // 
            button15.Location = new System.Drawing.Point(6, 22);
            button15.Name = "button15";
            button15.Size = new System.Drawing.Size(75, 23);
            button15.TabIndex = 15;
            button15.Text = "Pitch +";
            button15.UseVisualStyleBackColor = true;
            // 
            // button16
            // 
            button16.Location = new System.Drawing.Point(87, 51);
            button16.Name = "button16";
            button16.Size = new System.Drawing.Size(75, 23);
            button16.TabIndex = 14;
            button16.Text = "Yaw -";
            button16.UseVisualStyleBackColor = true;
            // 
            // button17
            // 
            button17.Location = new System.Drawing.Point(87, 22);
            button17.Name = "button17";
            button17.Size = new System.Drawing.Size(75, 23);
            button17.TabIndex = 13;
            button17.Text = "Yaw +";
            button17.UseVisualStyleBackColor = true;
            // 
            // groupBox3
            // 
            groupBox3.Controls.Add(label11);
            groupBox3.Controls.Add(label12);
            groupBox3.Controls.Add(label13);
            groupBox3.Controls.Add(label8);
            groupBox3.Controls.Add(label9);
            groupBox3.Controls.Add(label10);
            groupBox3.Controls.Add(label5);
            groupBox3.Controls.Add(label6);
            groupBox3.Controls.Add(label7);
            groupBox3.Controls.Add(label4);
            groupBox3.Controls.Add(label3);
            groupBox3.Controls.Add(label2);
            groupBox3.Location = new System.Drawing.Point(983, 773);
            groupBox3.Name = "groupBox3";
            groupBox3.Size = new System.Drawing.Size(377, 137);
            groupBox3.TabIndex = 15;
            groupBox3.TabStop = false;
            groupBox3.Text = "Position";
            // 
            // label11
            // 
            label11.AutoSize = true;
            label11.Location = new System.Drawing.Point(223, 62);
            label11.Name = "label11";
            label11.Size = new System.Drawing.Size(13, 15);
            label11.TabIndex = 11;
            label11.Text = "0";
            // 
            // label12
            // 
            label12.AutoSize = true;
            label12.Location = new System.Drawing.Point(223, 43);
            label12.Name = "label12";
            label12.Size = new System.Drawing.Size(13, 15);
            label12.TabIndex = 10;
            label12.Text = "0";
            // 
            // label13
            // 
            label13.AutoSize = true;
            label13.Location = new System.Drawing.Point(223, 23);
            label13.Name = "label13";
            label13.Size = new System.Drawing.Size(13, 15);
            label13.TabIndex = 9;
            label13.Text = "0";
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Location = new System.Drawing.Point(40, 68);
            label8.Name = "label8";
            label8.Size = new System.Drawing.Size(13, 15);
            label8.TabIndex = 8;
            label8.Text = "0";
            // 
            // label9
            // 
            label9.AutoSize = true;
            label9.Location = new System.Drawing.Point(40, 49);
            label9.Name = "label9";
            label9.Size = new System.Drawing.Size(13, 15);
            label9.TabIndex = 7;
            label9.Text = "0";
            // 
            // label10
            // 
            label10.AutoSize = true;
            label10.Location = new System.Drawing.Point(40, 29);
            label10.Name = "label10";
            label10.Size = new System.Drawing.Size(13, 15);
            label10.TabIndex = 6;
            label10.Text = "0";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new System.Drawing.Point(187, 62);
            label5.Name = "label5";
            label5.Size = new System.Drawing.Size(30, 15);
            label5.TabIndex = 5;
            label5.Text = "Roll:";
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new System.Drawing.Point(186, 43);
            label6.Name = "label6";
            label6.Size = new System.Drawing.Size(31, 15);
            label6.TabIndex = 4;
            label6.Text = "Yaw:";
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new System.Drawing.Point(180, 23);
            label7.Name = "label7";
            label7.Size = new System.Drawing.Size(37, 15);
            label7.TabIndex = 3;
            label7.Text = "Pitch:";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new System.Drawing.Point(17, 68);
            label4.Name = "label4";
            label4.Size = new System.Drawing.Size(17, 15);
            label4.TabIndex = 2;
            label4.Text = "Z:";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new System.Drawing.Point(17, 49);
            label3.Name = "label3";
            label3.Size = new System.Drawing.Size(17, 15);
            label3.TabIndex = 1;
            label3.Text = "Y:";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new System.Drawing.Point(17, 29);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(17, 15);
            label2.TabIndex = 0;
            label2.Text = "X:";
            // 
            // panel1
            // 
            panel1.Controls.Add(elementHost3D);
            panel1.Location = new System.Drawing.Point(358, 8);
            panel1.Name = "panel1";
            panel1.Size = new System.Drawing.Size(1002, 759);
            panel1.TabIndex = 17;
            // 
            // ctl_UpmixerPage
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(panel1);
            Controls.Add(groupBox3);
            Controls.Add(groupBox2);
            Controls.Add(groupBox1);
            Controls.Add(listBox2);
            Controls.Add(button1);
            Controls.Add(label1);
            Controls.Add(listBox1);
            Name = "ctl_UpmixerPage";
            Size = new System.Drawing.Size(1390, 913);
            groupBox1.ResumeLayout(false);
            groupBox2.ResumeLayout(false);
            groupBox3.ResumeLayout(false);
            groupBox3.PerformLayout();
            panel1.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.ListBox listBox1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.ListBox listBox2;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button button8;
        private System.Windows.Forms.Button button9;
        private System.Windows.Forms.Button button6;
        private System.Windows.Forms.Button button7;
        private System.Windows.Forms.Button button4;
        private System.Windows.Forms.Button button5;
        private System.Windows.Forms.Button button3;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Button button12;
        private System.Windows.Forms.Button button13;
        private System.Windows.Forms.Button button14;
        private System.Windows.Forms.Button button15;
        private System.Windows.Forms.Button button16;
        private System.Windows.Forms.Button button17;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label label10;
        private ctl_3DLayout ctl_3DLayout1;
        private System.Windows.Forms.Integration.ElementHost elementHost3D;
        private System.Windows.Forms.Panel panel1;
    }
}
