namespace BassThatHz_ASIO_DSP_Processor
{
    partial class FormMonitoring
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        protected void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.pnl_Main = new System.Windows.Forms.Panel();
            this.timer_Refresh = new System.Windows.Forms.Timer(this.components);
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.msb_RefreshInterval = new System.Windows.Forms.MaskedTextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.btn_ResetClip = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.SuspendLayout();
            // 
            // pnl_Main
            // 
            this.pnl_Main.AutoScroll = true;
            this.pnl_Main.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnl_Main.Location = new System.Drawing.Point(0, 0);
            this.pnl_Main.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.pnl_Main.Name = "pnl_Main";
            this.pnl_Main.Size = new System.Drawing.Size(700, 386);
            this.pnl_Main.TabIndex = 1;
            // 
            // timer_Refresh
            // 
            this.timer_Refresh.Tick += new System.EventHandler(this.timer_Refresh_Tick);
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.splitContainer1.IsSplitterFixed = true;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.msb_RefreshInterval);
            this.splitContainer1.Panel1.Controls.Add(this.label1);
            this.splitContainer1.Panel1.Controls.Add(this.btn_ResetClip);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.pnl_Main);
            this.splitContainer1.Size = new System.Drawing.Size(700, 422);
            this.splitContainer1.SplitterDistance = 32;
            this.splitContainer1.TabIndex = 2;
            // 
            // msb_RefreshInterval
            // 
            this.msb_RefreshInterval.Location = new System.Drawing.Point(342, 5);
            this.msb_RefreshInterval.Margin = new System.Windows.Forms.Padding(4);
            this.msb_RefreshInterval.Mask = "00000";
            this.msb_RefreshInterval.Name = "msb_RefreshInterval";
            this.msb_RefreshInterval.PromptChar = '#';
            this.msb_RefreshInterval.Size = new System.Drawing.Size(50, 23);
            this.msb_RefreshInterval.TabIndex = 2;
            this.msb_RefreshInterval.Text = "100";
            this.msb_RefreshInterval.TextMaskFormat = System.Windows.Forms.MaskFormat.ExcludePromptAndLiterals;
            this.msb_RefreshInterval.ValidatingType = typeof(int);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(206, 9);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(131, 15);
            this.label1.TabIndex = 1;
            this.label1.Text = "Refresh Interval (in ms):";
            // 
            // btn_ResetClip
            // 
            this.btn_ResetClip.Location = new System.Drawing.Point(4, 5);
            this.btn_ResetClip.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.btn_ResetClip.Name = "btn_ResetClip";
            this.btn_ResetClip.Size = new System.Drawing.Size(193, 22);
            this.btn_ResetClip.TabIndex = 0;
            this.btn_ResetClip.Text = "Reset Peak and Clip Indicators";
            this.btn_ResetClip.UseVisualStyleBackColor = true;
            this.btn_ResetClip.Click += new System.EventHandler(this.btn_ResetClip_Click);
            // 
            // FormMonitoring
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(700, 422);
            this.Controls.Add(this.splitContainer1);
            this.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.Name = "FormMonitoring";
            this.ShowIcon = false;
            this.Text = "BassThatHz_ASIO_DSP_Processor Monitor";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel1.PerformLayout();
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        protected System.Windows.Forms.Panel pnl_Main;
        protected System.Windows.Forms.Timer timer_Refresh;
        protected System.Windows.Forms.SplitContainer splitContainer1;
        protected System.Windows.Forms.Button btn_ResetClip;
        protected System.Windows.Forms.MaskedTextBox msb_RefreshInterval;
        protected System.Windows.Forms.Label label1;
    }
}