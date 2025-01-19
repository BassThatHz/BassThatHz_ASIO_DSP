﻿
namespace BassThatHz_ASIO_DSP_Processor.GUI.Tabs
{
    partial class ctl_DSPConfigPage
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
            btnAddStream = new System.Windows.Forms.Button();
            lblStreamCount = new System.Windows.Forms.Label();
            tabControl1 = new System.Windows.Forms.TabControl();
            tabPage1 = new System.Windows.Forms.TabPage();
            stream1 = new Controls.StreamControl();
            hScrollBar1 = new System.Windows.Forms.HScrollBar();
            tabControl1.SuspendLayout();
            tabPage1.SuspendLayout();
            SuspendLayout();
            // 
            // btnAddStream
            // 
            btnAddStream.Location = new System.Drawing.Point(14, 961);
            btnAddStream.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            btnAddStream.Name = "btnAddStream";
            btnAddStream.Size = new System.Drawing.Size(70, 25);
            btnAddStream.TabIndex = 1;
            btnAddStream.Text = "Add";
            btnAddStream.UseVisualStyleBackColor = true;
            btnAddStream.Click += btnAddStream_Click;
            // 
            // lblStreamCount
            // 
            lblStreamCount.AutoSize = true;
            lblStreamCount.Location = new System.Drawing.Point(90, 968);
            lblStreamCount.Name = "lblStreamCount";
            lblStreamCount.Size = new System.Drawing.Size(13, 15);
            lblStreamCount.TabIndex = 3;
            lblStreamCount.Text = "0";
            // 
            // tabControl1
            // 
            tabControl1.Controls.Add(tabPage1);
            tabControl1.Location = new System.Drawing.Point(10, 2);
            tabControl1.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            tabControl1.Name = "tabControl1";
            tabControl1.SelectedIndex = 0;
            tabControl1.Size = new System.Drawing.Size(1581, 935);
            tabControl1.TabIndex = 4;
            // 
            // tabPage1
            // 
            tabPage1.Controls.Add(stream1);
            tabPage1.Location = new System.Drawing.Point(4, 24);
            tabPage1.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            tabPage1.Name = "tabPage1";
            tabPage1.Size = new System.Drawing.Size(1573, 907);
            tabPage1.TabIndex = 0;
            tabPage1.Text = "null -> null";
            tabPage1.UseVisualStyleBackColor = true;
            // 
            // stream1
            // 
            stream1.BackColor = System.Drawing.SystemColors.ControlDark;
            stream1.Dock = System.Windows.Forms.DockStyle.Fill;
            stream1.Location = new System.Drawing.Point(0, 0);
            stream1.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            stream1.Name = "stream1";
            stream1.Size = new System.Drawing.Size(1573, 907);
            stream1.TabIndex = 0;
            // 
            // hScrollBar1
            // 
            hScrollBar1.LargeChange = 1;
            hScrollBar1.Location = new System.Drawing.Point(14, 939);
            hScrollBar1.Maximum = 0;
            hScrollBar1.Name = "hScrollBar1";
            hScrollBar1.Size = new System.Drawing.Size(1541, 20);
            hScrollBar1.TabIndex = 5;
            hScrollBar1.Scroll += hScrollBar1_Scroll;
            // 
            // ctl_DSPConfigPage
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            AutoScroll = true;
            Controls.Add(hScrollBar1);
            Controls.Add(tabControl1);
            Controls.Add(lblStreamCount);
            Controls.Add(btnAddStream);
            Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            Name = "ctl_DSPConfigPage";
            Size = new System.Drawing.Size(1611, 993);
            tabControl1.ResumeLayout(false);
            tabPage1.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        public System.Windows.Forms.Button btnAddStream;
        public System.Windows.Forms.Label lblStreamCount;
        public System.Windows.Forms.TabControl tabControl1;
        public System.Windows.Forms.HScrollBar hScrollBar1;
        public System.Windows.Forms.TabPage tabPage1;
        public Controls.StreamControl stream1;
    }
}
