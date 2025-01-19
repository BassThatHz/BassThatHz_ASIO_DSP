
namespace BassThatHz_ASIO_DSP_Processor.GUI.Tabs
{
    partial class ctl_MonitorPage
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
            this.btn_Monitor = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // btn_Monitor
            // 
            this.btn_Monitor.Location = new System.Drawing.Point(3, 3);
            this.btn_Monitor.Name = "btn_Monitor";
            this.btn_Monitor.Size = new System.Drawing.Size(190, 23);
            this.btn_Monitor.TabIndex = 0;
            this.btn_Monitor.Text = "Open Monitoring Window";
            this.btn_Monitor.UseVisualStyleBackColor = true;
            this.btn_Monitor.Click += new System.EventHandler(this.btn_Monitor_Click);
            // 
            // ctl_MonitorPage
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoScroll = true;
            this.Controls.Add(this.btn_Monitor);
            this.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.Name = "ctl_MonitorPage";
            this.Size = new System.Drawing.Size(623, 305);
            this.Load += new System.EventHandler(this.ctl_MonitorPage_Load);
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Button btn_Monitor;
    }
}
