namespace BassThatHz_ASIO_DSP_Processor
{
    partial class FormMonitoring
    {

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
        protected void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            pnl_Main = new System.Windows.Forms.Panel();
            timer_Refresh = new System.Windows.Forms.Timer(components);
            splitContainer1 = new System.Windows.Forms.SplitContainer();
            Pause_CHK = new System.Windows.Forms.CheckBox();
            msb_RefreshInterval = new System.Windows.Forms.MaskedTextBox();
            label1 = new System.Windows.Forms.Label();
            btn_ResetClip = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            SuspendLayout();
            // 
            // pnl_Main
            // 
            pnl_Main.AutoScroll = true;
            pnl_Main.Dock = System.Windows.Forms.DockStyle.Fill;
            pnl_Main.Location = new System.Drawing.Point(0, 0);
            pnl_Main.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            pnl_Main.Name = "pnl_Main";
            pnl_Main.Size = new System.Drawing.Size(700, 386);
            pnl_Main.TabIndex = 1;
            // 
            // timer_Refresh
            // 
            timer_Refresh.Enabled = true;
            timer_Refresh.Tick += Refresh_Timer_Tick;
            // 
            // splitContainer1
            // 
            splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            splitContainer1.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            splitContainer1.IsSplitterFixed = true;
            splitContainer1.Location = new System.Drawing.Point(0, 0);
            splitContainer1.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            splitContainer1.Name = "splitContainer1";
            splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.Controls.Add(Pause_CHK);
            splitContainer1.Panel1.Controls.Add(msb_RefreshInterval);
            splitContainer1.Panel1.Controls.Add(label1);
            splitContainer1.Panel1.Controls.Add(btn_ResetClip);
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(pnl_Main);
            splitContainer1.Size = new System.Drawing.Size(700, 422);
            splitContainer1.SplitterDistance = 32;
            splitContainer1.TabIndex = 2;
            // 
            // Pause_CHK
            // 
            Pause_CHK.AutoSize = true;
            Pause_CHK.Location = new System.Drawing.Point(399, 7);
            Pause_CHK.Name = "Pause_CHK";
            Pause_CHK.Size = new System.Drawing.Size(57, 19);
            Pause_CHK.TabIndex = 3;
            Pause_CHK.Text = "Pause";
            Pause_CHK.UseVisualStyleBackColor = true;
            Pause_CHK.CheckedChanged += Pause_CHK_CheckedChanged;
            // 
            // msb_RefreshInterval
            // 
            msb_RefreshInterval.Location = new System.Drawing.Point(342, 5);
            msb_RefreshInterval.Margin = new System.Windows.Forms.Padding(4);
            msb_RefreshInterval.Mask = "00000";
            msb_RefreshInterval.Name = "msb_RefreshInterval";
            msb_RefreshInterval.PromptChar = '#';
            msb_RefreshInterval.Size = new System.Drawing.Size(50, 23);
            msb_RefreshInterval.TabIndex = 2;
            msb_RefreshInterval.Text = "100";
            msb_RefreshInterval.TextMaskFormat = System.Windows.Forms.MaskFormat.ExcludePromptAndLiterals;
            msb_RefreshInterval.ValidatingType = typeof(int);
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(206, 9);
            label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(131, 15);
            label1.TabIndex = 1;
            label1.Text = "Refresh Interval (in ms):";
            // 
            // btn_ResetClip
            // 
            btn_ResetClip.Location = new System.Drawing.Point(4, 5);
            btn_ResetClip.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            btn_ResetClip.Name = "btn_ResetClip";
            btn_ResetClip.Size = new System.Drawing.Size(193, 22);
            btn_ResetClip.TabIndex = 0;
            btn_ResetClip.Text = "Reset Peak and Clip Indicators";
            btn_ResetClip.UseVisualStyleBackColor = true;
            btn_ResetClip.Click += ResetClipBTN_Click;
            // 
            // FormMonitoring
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(700, 422);
            Controls.Add(splitContainer1);
            Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            Name = "FormMonitoring";
            ShowIcon = false;
            Text = "BassThatHz_ASIO_DSP_Processor Monitor";
            WindowState = System.Windows.Forms.FormWindowState.Maximized;
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel1.PerformLayout();
            splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        protected System.Windows.Forms.Panel pnl_Main;
        protected System.Windows.Forms.Timer timer_Refresh;
        protected System.Windows.Forms.SplitContainer splitContainer1;
        protected System.Windows.Forms.Button btn_ResetClip;
        protected System.Windows.Forms.MaskedTextBox msb_RefreshInterval;
        protected System.Windows.Forms.Label label1;
        private System.Windows.Forms.CheckBox Pause_CHK;
        private System.ComponentModel.IContainer components;
    }
}