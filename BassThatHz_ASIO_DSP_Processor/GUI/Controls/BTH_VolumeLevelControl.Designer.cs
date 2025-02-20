namespace BassThatHz_ASIO_DSP_Processor.GUI.Controls
{
    partial class BTH_VolumeLevelControl
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
            this.components = new System.ComponentModel.Container();
            this.pnl_InputClip = new System.Windows.Forms.Panel();
            this.pnl_OutputClip = new System.Windows.Forms.Panel();
            this.lbl_Input_DB = new System.Windows.Forms.Label();
            this.lbl_Output_DB = new System.Windows.Forms.Label();
            this.lbl_Input_DB_Peak = new System.Windows.Forms.Label();
            this.lbl_Output_DB_Peak = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.lbl_InputSource = new System.Windows.Forms.Label();
            this.lbl_OutputSource = new System.Windows.Forms.Label();
            this.btn_View = new System.Windows.Forms.Button();
            this.timer_Refresh = new System.Windows.Forms.Timer(this.components);
            this.vol_Out = new BassThatHz_ASIO_DSP_Processor.GUI.Controls.BTH_VolumeLevel_SimpleControl();
            this.vol_In = new BassThatHz_ASIO_DSP_Processor.GUI.Controls.BTH_VolumeLevel_SimpleControl();
            this.SuspendLayout();
            // 
            // pnl_InputClip
            // 
            this.pnl_InputClip.BackColor = System.Drawing.Color.Black;
            this.pnl_InputClip.Location = new System.Drawing.Point(195, 6);
            this.pnl_InputClip.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            this.pnl_InputClip.Name = "pnl_InputClip";
            this.pnl_InputClip.Size = new System.Drawing.Size(20, 23);
            this.pnl_InputClip.TabIndex = 2;
            // 
            // pnl_OutputClip
            // 
            this.pnl_OutputClip.BackColor = System.Drawing.Color.Black;
            this.pnl_OutputClip.Location = new System.Drawing.Point(195, 43);
            this.pnl_OutputClip.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            this.pnl_OutputClip.Name = "pnl_OutputClip";
            this.pnl_OutputClip.Size = new System.Drawing.Size(20, 23);
            this.pnl_OutputClip.TabIndex = 3;
            // 
            // lbl_Input_DB
            // 
            this.lbl_Input_DB.AutoSize = true;
            this.lbl_Input_DB.BackColor = System.Drawing.Color.Transparent;
            this.lbl_Input_DB.Location = new System.Drawing.Point(265, 0);
            this.lbl_Input_DB.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            this.lbl_Input_DB.Name = "lbl_Input_DB";
            this.lbl_Input_DB.Size = new System.Drawing.Size(106, 37);
            this.lbl_Input_DB.TabIndex = 4;
            this.lbl_Input_DB.Text = "0.00 dB";
            // 
            // lbl_Output_DB
            // 
            this.lbl_Output_DB.AutoSize = true;
            this.lbl_Output_DB.BackColor = System.Drawing.Color.Transparent;
            this.lbl_Output_DB.Location = new System.Drawing.Point(265, 34);
            this.lbl_Output_DB.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            this.lbl_Output_DB.Name = "lbl_Output_DB";
            this.lbl_Output_DB.Size = new System.Drawing.Size(106, 37);
            this.lbl_Output_DB.TabIndex = 5;
            this.lbl_Output_DB.Text = "0.00 dB";
            // 
            // lbl_Input_DB_Peak
            // 
            this.lbl_Input_DB_Peak.AutoSize = true;
            this.lbl_Input_DB_Peak.BackColor = System.Drawing.Color.Transparent;
            this.lbl_Input_DB_Peak.Location = new System.Drawing.Point(395, 0);
            this.lbl_Input_DB_Peak.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            this.lbl_Input_DB_Peak.Name = "lbl_Input_DB_Peak";
            this.lbl_Input_DB_Peak.Size = new System.Drawing.Size(106, 37);
            this.lbl_Input_DB_Peak.TabIndex = 6;
            this.lbl_Input_DB_Peak.Text = "0.00 dB";
            // 
            // lbl_Output_DB_Peak
            // 
            this.lbl_Output_DB_Peak.AutoSize = true;
            this.lbl_Output_DB_Peak.BackColor = System.Drawing.Color.Transparent;
            this.lbl_Output_DB_Peak.Location = new System.Drawing.Point(395, 37);
            this.lbl_Output_DB_Peak.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            this.lbl_Output_DB_Peak.Name = "lbl_Output_DB_Peak";
            this.lbl_Output_DB_Peak.Size = new System.Drawing.Size(106, 37);
            this.lbl_Output_DB_Peak.TabIndex = 7;
            this.lbl_Output_DB_Peak.Text = "0.00 dB";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(520, 0);
            this.label4.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(45, 37);
            this.label4.TabIndex = 8;
            this.label4.Text = "In:";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(500, 37);
            this.label5.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(67, 37);
            this.label5.TabIndex = 9;
            this.label5.Text = "Out:";
            // 
            // lbl_InputSource
            // 
            this.lbl_InputSource.AutoSize = true;
            this.lbl_InputSource.Location = new System.Drawing.Point(555, 0);
            this.lbl_InputSource.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            this.lbl_InputSource.Name = "lbl_InputSource";
            this.lbl_InputSource.Size = new System.Drawing.Size(85, 37);
            this.lbl_InputSource.TabIndex = 10;
            this.lbl_InputSource.Text = "Input:";
            // 
            // lbl_OutputSource
            // 
            this.lbl_OutputSource.AutoSize = true;
            this.lbl_OutputSource.Location = new System.Drawing.Point(555, 34);
            this.lbl_OutputSource.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            this.lbl_OutputSource.Name = "lbl_OutputSource";
            this.lbl_OutputSource.Size = new System.Drawing.Size(107, 37);
            this.lbl_OutputSource.TabIndex = 11;
            this.lbl_OutputSource.Text = "Output:";
            // 
            // btn_View
            // 
            this.btn_View.Location = new System.Drawing.Point(1032, 6);
            this.btn_View.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            this.btn_View.Name = "btn_View";
            this.btn_View.Size = new System.Drawing.Size(162, 63);
            this.btn_View.TabIndex = 12;
            this.btn_View.Text = "[100] View";
            this.btn_View.UseVisualStyleBackColor = true;
            this.btn_View.Click += new System.EventHandler(this.Btn_View_Click);
            // 
            // timer_Refresh
            // 
            this.timer_Refresh.Interval = 1000;
            this.timer_Refresh.Tick += new System.EventHandler(this.timer_Refresh_Tick);
            // 
            // vol_Out
            // 
            this.vol_Out.BackColor = System.Drawing.Color.Black;
            this.vol_Out.DB_Level = -1.7976931348623157E+308D;
            this.vol_Out.Location = new System.Drawing.Point(5, 43);
            this.vol_Out.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            this.vol_Out.MinDb = -100D;
            this.vol_Out.Name = "vol_Out";
            this.vol_Out.Size = new System.Drawing.Size(178, 23);
            this.vol_Out.TabIndex = 14;
            // 
            // vol_In
            // 
            this.vol_In.BackColor = System.Drawing.Color.Black;
            this.vol_In.DB_Level = -1.7976931348623157E+308D;
            this.vol_In.Location = new System.Drawing.Point(5, 6);
            this.vol_In.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            this.vol_In.MinDb = -100D;
            this.vol_In.Name = "vol_In";
            this.vol_In.Size = new System.Drawing.Size(178, 23);
            this.vol_In.TabIndex = 13;
            // 
            // BTH_VolumeLevel
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(15F, 37F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Control;
            this.Controls.Add(this.vol_Out);
            this.Controls.Add(this.vol_In);
            this.Controls.Add(this.btn_View);
            this.Controls.Add(this.lbl_OutputSource);
            this.Controls.Add(this.lbl_InputSource);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.lbl_Output_DB_Peak);
            this.Controls.Add(this.lbl_Input_DB_Peak);
            this.Controls.Add(this.lbl_Output_DB);
            this.Controls.Add(this.lbl_Input_DB);
            this.Controls.Add(this.pnl_OutputClip);
            this.Controls.Add(this.pnl_InputClip);
            this.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            this.Name = "BTH_VolumeLevel";
            this.Size = new System.Drawing.Size(1200, 71);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        protected System.Windows.Forms.Panel pnl_InputClip;
        protected System.Windows.Forms.Panel pnl_OutputClip;
        protected System.Windows.Forms.Label lbl_Input_DB;
        protected System.Windows.Forms.Label lbl_Output_DB;
        protected System.Windows.Forms.Label lbl_Input_DB_Peak;
        protected System.Windows.Forms.Label lbl_Output_DB_Peak;
        protected System.Windows.Forms.Label label4;
        protected System.Windows.Forms.Label label5;
        protected System.Windows.Forms.Label lbl_InputSource;
        protected System.Windows.Forms.Label lbl_OutputSource;
        protected System.Windows.Forms.Button btn_View;
        protected System.Windows.Forms.Timer timer_Refresh;
        protected BTH_VolumeLevel_SimpleControl vol_In;
        protected BTH_VolumeLevel_SimpleControl vol_Out;
    }
}
