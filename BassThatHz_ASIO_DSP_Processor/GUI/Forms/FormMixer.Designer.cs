namespace BassThatHz_ASIO_DSP_Processor.GUI.Forms
{
    partial class FormMixer
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            splitContainer1 = new System.Windows.Forms.SplitContainer();
            btnApply = new System.Windows.Forms.Button();
            btnRefreshList = new System.Windows.Forms.Button();
            btnInvertSelection = new System.Windows.Forms.Button();
            btnClearSelection = new System.Windows.Forms.Button();
            btn_SelectAll = new System.Windows.Forms.Button();
            panel1 = new System.Windows.Forms.Panel();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            SuspendLayout();
            // 
            // splitContainer1
            // 
            splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            splitContainer1.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            splitContainer1.IsSplitterFixed = true;
            splitContainer1.Location = new System.Drawing.Point(0, 0);
            splitContainer1.Margin = new System.Windows.Forms.Padding(2, 1, 2, 1);
            splitContainer1.Name = "splitContainer1";
            splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.Controls.Add(btnApply);
            splitContainer1.Panel1.Controls.Add(btnRefreshList);
            splitContainer1.Panel1.Controls.Add(btnInvertSelection);
            splitContainer1.Panel1.Controls.Add(btnClearSelection);
            splitContainer1.Panel1.Controls.Add(btn_SelectAll);
            splitContainer1.Panel1MinSize = 20;
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(panel1);
            splitContainer1.Panel2MinSize = 100;
            splitContainer1.Size = new System.Drawing.Size(1005, 492);
            splitContainer1.SplitterWidth = 2;
            splitContainer1.TabIndex = 0;
            // 
            // btnApply
            // 
            btnApply.Location = new System.Drawing.Point(879, 10);
            btnApply.Margin = new System.Windows.Forms.Padding(2, 1, 2, 1);
            btnApply.Name = "btnApply";
            btnApply.Size = new System.Drawing.Size(111, 22);
            btnApply.TabIndex = 4;
            btnApply.Text = "Apply";
            btnApply.UseVisualStyleBackColor = true;
            btnApply.Click += btnApply_Click;
            // 
            // btnRefreshList
            // 
            btnRefreshList.Location = new System.Drawing.Point(561, 10);
            btnRefreshList.Margin = new System.Windows.Forms.Padding(2, 1, 2, 1);
            btnRefreshList.Name = "btnRefreshList";
            btnRefreshList.Size = new System.Drawing.Size(111, 22);
            btnRefreshList.TabIndex = 3;
            btnRefreshList.Text = "Refresh List";
            btnRefreshList.UseVisualStyleBackColor = true;
            btnRefreshList.Click += btnRefreshList_Click;
            // 
            // btnInvertSelection
            // 
            btnInvertSelection.Location = new System.Drawing.Point(254, 10);
            btnInvertSelection.Margin = new System.Windows.Forms.Padding(2, 1, 2, 1);
            btnInvertSelection.Name = "btnInvertSelection";
            btnInvertSelection.Size = new System.Drawing.Size(111, 22);
            btnInvertSelection.TabIndex = 2;
            btnInvertSelection.Text = "Invert Selection";
            btnInvertSelection.UseVisualStyleBackColor = true;
            btnInvertSelection.Click += btnInvertSelection_Click;
            // 
            // btnClearSelection
            // 
            btnClearSelection.Location = new System.Drawing.Point(130, 10);
            btnClearSelection.Margin = new System.Windows.Forms.Padding(2, 1, 2, 1);
            btnClearSelection.Name = "btnClearSelection";
            btnClearSelection.Size = new System.Drawing.Size(113, 22);
            btnClearSelection.TabIndex = 1;
            btnClearSelection.Text = "Clear Selection";
            btnClearSelection.UseVisualStyleBackColor = true;
            btnClearSelection.Click += btnClearSelection_Click;
            // 
            // btn_SelectAll
            // 
            btn_SelectAll.Location = new System.Drawing.Point(6, 10);
            btn_SelectAll.Margin = new System.Windows.Forms.Padding(2, 1, 2, 1);
            btn_SelectAll.Name = "btn_SelectAll";
            btn_SelectAll.Size = new System.Drawing.Size(113, 22);
            btn_SelectAll.TabIndex = 0;
            btn_SelectAll.Text = "Select All";
            btn_SelectAll.UseVisualStyleBackColor = true;
            btn_SelectAll.Click += btn_SelectAll_Click;
            // 
            // panel1
            // 
            panel1.AutoScroll = true;
            panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            panel1.Location = new System.Drawing.Point(0, 0);
            panel1.Name = "panel1";
            panel1.Size = new System.Drawing.Size(1005, 440);
            panel1.TabIndex = 0;
            // 
            // FormMixer
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(1005, 492);
            Controls.Add(splitContainer1);
            Margin = new System.Windows.Forms.Padding(2, 1, 2, 1);
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "FormMixer";
            ShowIcon = false;
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "Mixer";
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.Button btnRefreshList;
        private System.Windows.Forms.Button btnInvertSelection;
        private System.Windows.Forms.Button btnClearSelection;
        private System.Windows.Forms.Button btn_SelectAll;
        private System.Windows.Forms.Button btnApply;
        private System.Windows.Forms.Panel panel1;
    }
}