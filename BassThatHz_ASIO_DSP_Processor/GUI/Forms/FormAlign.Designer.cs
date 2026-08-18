namespace BassThatHz_ASIO_DSP_Processor
{
    using System.Windows.Forms.DataVisualization.Charting;

    partial class FormAlign
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
            components = new System.ComponentModel.Container();
            ChartArea chartArea1 = new ChartArea();
            Series series1 = new Series();
            Series series2 = new Series();
            Title title1 = new Title();
            Title title2 = new Title();
            Title title3 = new Title();
            Title title4 = new Title();
            Title title5 = new Title();
            Title title6 = new Title();
            ChartArea chartArea2 = new ChartArea();
            Series series4 = new Series();
            Series series5 = new Series();
            Title title7 = new Title();
            Title title8 = new Title();
            Title title9 = new Title();
            Title title10 = new Title();
            Title title11 = new Title();
            Title title12 = new Title();
            ChartArea chartArea3 = new ChartArea();
            Series series6 = new Series();
            Series series7 = new Series();
            Title title13 = new Title();
            Title title14 = new Title();
            Title title15 = new Title();
            Title title16 = new Title();
            Title title17 = new Title();
            Title title18 = new Title();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormAlign));
            Chart_Mag = new Chart();
            label3 = new System.Windows.Forms.Label();
            cboSource1 = new System.Windows.Forms.ComboBox();
            label1 = new System.Windows.Forms.Label();
            cboSource2 = new System.Windows.Forms.ComboBox();
            label2 = new System.Windows.Forms.Label();
            cboRef = new System.Windows.Forms.ComboBox();
            Source1_Color_LBL = new System.Windows.Forms.Label();
            Source2_Color_LBL = new System.Windows.Forms.Label();
            Ref_Color_LBL = new System.Windows.Forms.Label();
            groupBox2 = new System.Windows.Forms.GroupBox();
            Smoothing_CHK = new System.Windows.Forms.CheckBox();
            Smoothing_CBO = new System.Windows.Forms.ComboBox();
            Averaging_TXT = new System.Windows.Forms.TextBox();
            label6 = new System.Windows.Forms.Label();
            Coherence_Mask_TXT = new System.Windows.Forms.TextBox();
            label21 = new System.Windows.Forms.Label();
            min_ms_TXT = new System.Windows.Forms.TextBox();
            label4 = new System.Windows.Forms.Label();
            max_ms_TXT = new System.Windows.Forms.TextBox();
            label5 = new System.Windows.Forms.Label();
            FFTSize_CBO = new System.Windows.Forms.ComboBox();
            label9 = new System.Windows.Forms.Label();
            maxdB_TXT = new System.Windows.Forms.TextBox();
            label7 = new System.Windows.Forms.Label();
            mindB_TXT = new System.Windows.Forms.TextBox();
            label8 = new System.Windows.Forms.Label();
            RefreshTimer = new System.Windows.Forms.Timer(components);
            Chart_Phase = new Chart();
            Chart_IR = new Chart();
            groupBox1 = new System.Windows.Forms.GroupBox();
            Delay2_LBL = new System.Windows.Forms.Label();
            label18 = new System.Windows.Forms.Label();
            Delay1_LBL = new System.Windows.Forms.Label();
            label20 = new System.Windows.Forms.Label();
            DelayDelta_LBL = new System.Windows.Forms.Label();
            label22 = new System.Windows.Forms.Label();
            Frames_LBL = new System.Windows.Forms.Label();
            label23 = new System.Windows.Forms.Label();
            Reset_BTN = new System.Windows.Forms.Button();
            Coherence_LBL = new System.Windows.Forms.Label();
            label16 = new System.Windows.Forms.Label();
            Coherence2_LBL = new System.Windows.Forms.Label();
            label14 = new System.Windows.Forms.Label();
            Coherence1_LBL = new System.Windows.Forms.Label();
            label12 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)Chart_Mag).BeginInit();
            groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)Chart_Phase).BeginInit();
            ((System.ComponentModel.ISupportInitialize)Chart_IR).BeginInit();
            groupBox1.SuspendLayout();
            SuspendLayout();
            // 
            // Chart_Mag
            // 
            chartArea1.Name = "ChartArea1";
            Chart_Mag.ChartAreas.Add(chartArea1);
            Chart_Mag.Location = new System.Drawing.Point(10, 495);
            Chart_Mag.Margin = new System.Windows.Forms.Padding(1);
            Chart_Mag.Name = "Chart_Mag";
            series1.ChartArea = "ChartArea1";
            series1.ChartType = SeriesChartType.Line;
            series1.Name = "Series1";
            series2.ChartArea = "ChartArea1";
            series2.ChartType = SeriesChartType.Line;
            series2.Name = "Series2";
            Chart_Mag.Series.Add(series1);
            Chart_Mag.Series.Add(series2);
            Chart_Mag.Size = new System.Drawing.Size(1315, 348);
            Chart_Mag.TabIndex = 298;
            Chart_Mag.Text = "chart3";
            title1.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold);
            title1.Name = "Title";
            title1.Text = "Magnitude (dB)";
            title2.Alignment = System.Drawing.ContentAlignment.TopRight;
            title2.Docking = Docking.Bottom;
            title2.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.2F);
            title2.Name = "AxisX";
            title2.Text = "Hz";
            title3.Docking = Docking.Left;
            title3.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.2F);
            title3.Name = "AxisY";
            title3.Text = "dB";
            title4.Alignment = System.Drawing.ContentAlignment.BottomLeft;
            title4.Docking = Docking.Bottom;
            title4.IsDockedInsideChartArea = false;
            title4.Name = "Max";
            title4.Position.Auto = false;
            title4.Position.Width = 87.22672F;
            title4.Position.X = 5F;
            title4.Position.Y = 96F;
            title4.Text = "Max: 0 | -0";
            title4.TextOrientation = TextOrientation.Horizontal;
            title5.Alignment = System.Drawing.ContentAlignment.BottomLeft;
            title5.Docking = Docking.Bottom;
            title5.IsDockedInsideChartArea = false;
            title5.Name = "Min";
            title5.Position.Auto = false;
            title5.Position.Width = 70F;
            title5.Position.X = 30F;
            title5.Position.Y = 96F;
            title5.Text = "Min: 0 | -0";
            title5.TextOrientation = TextOrientation.Horizontal;
            title6.Alignment = System.Drawing.ContentAlignment.BottomLeft;
            title6.Docking = Docking.Bottom;
            title6.IsDockedInsideChartArea = false;
            title6.Name = "Mouse";
            title6.Position.Auto = false;
            title6.Position.Width = 40F;
            title6.Position.X = 60F;
            title6.Position.Y = 96F;
            title6.Text = "Mouse: 0 | -0";
            title6.TextOrientation = TextOrientation.Horizontal;
            Chart_Mag.Titles.Add(title1);
            Chart_Mag.Titles.Add(title2);
            Chart_Mag.Titles.Add(title3);
            Chart_Mag.Titles.Add(title4);
            Chart_Mag.Titles.Add(title5);
            Chart_Mag.Titles.Add(title6);
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new System.Drawing.Point(11, 857);
            label3.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            label3.Name = "label3";
            label3.Size = new System.Drawing.Size(55, 15);
            label3.TabIndex = 300;
            label3.Text = "Source 1:";
            // 
            // cboSource1
            // 
            cboSource1.DisplayMember = "DisplayMember";
            cboSource1.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cboSource1.FormattingEnabled = true;
            cboSource1.Location = new System.Drawing.Point(11, 877);
            cboSource1.Margin = new System.Windows.Forms.Padding(2);
            cboSource1.Name = "cboSource1";
            cboSource1.Size = new System.Drawing.Size(505, 23);
            cboSource1.TabIndex = 299;
            cboSource1.SelectedIndexChanged += cboSource1_SelectedIndexChanged;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(11, 911);
            label1.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(55, 15);
            label1.TabIndex = 302;
            label1.Text = "Source 2:";
            // 
            // cboSource2
            // 
            cboSource2.DisplayMember = "DisplayMember";
            cboSource2.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cboSource2.FormattingEnabled = true;
            cboSource2.Location = new System.Drawing.Point(11, 931);
            cboSource2.Margin = new System.Windows.Forms.Padding(2);
            cboSource2.Name = "cboSource2";
            cboSource2.Size = new System.Drawing.Size(505, 23);
            cboSource2.TabIndex = 301;
            cboSource2.SelectedIndexChanged += cboSource2_SelectedIndexChanged;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new System.Drawing.Point(11, 966);
            label2.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(62, 15);
            label2.TabIndex = 304;
            label2.Text = "Ref Signal:";
            // 
            // cboRef
            // 
            cboRef.DisplayMember = "DisplayMember";
            cboRef.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cboRef.FormattingEnabled = true;
            cboRef.Location = new System.Drawing.Point(11, 986);
            cboRef.Margin = new System.Windows.Forms.Padding(2);
            cboRef.Name = "cboRef";
            cboRef.Size = new System.Drawing.Size(505, 23);
            cboRef.TabIndex = 303;
            cboRef.SelectedIndexChanged += cboRef_SelectedIndexChanged;
            //
            // Source1_Color_LBL
            //
            Source1_Color_LBL.AutoSize = false;
            Source1_Color_LBL.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            Source1_Color_LBL.Location = new System.Drawing.Point(523, 877);
            Source1_Color_LBL.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            Source1_Color_LBL.Name = "Source1_Color_LBL";
            Source1_Color_LBL.Size = new System.Drawing.Size(24, 23);
            Source1_Color_LBL.TabIndex = 311;
            Source1_Color_LBL.TabStop = false;
            Source1_Color_LBL.Text = "";
            //
            // Source2_Color_LBL
            //
            Source2_Color_LBL.AutoSize = false;
            Source2_Color_LBL.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            Source2_Color_LBL.Location = new System.Drawing.Point(523, 931);
            Source2_Color_LBL.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            Source2_Color_LBL.Name = "Source2_Color_LBL";
            Source2_Color_LBL.Size = new System.Drawing.Size(24, 23);
            Source2_Color_LBL.TabIndex = 312;
            Source2_Color_LBL.TabStop = false;
            Source2_Color_LBL.Text = "";
            //
            // Ref_Color_LBL
            //
            Ref_Color_LBL.AutoSize = false;
            Ref_Color_LBL.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            Ref_Color_LBL.Location = new System.Drawing.Point(523, 986);
            Ref_Color_LBL.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            Ref_Color_LBL.Name = "Ref_Color_LBL";
            Ref_Color_LBL.Size = new System.Drawing.Size(24, 23);
            Ref_Color_LBL.TabIndex = 313;
            Ref_Color_LBL.TabStop = false;
            Ref_Color_LBL.Text = "";
            //
            // groupBox2
            //
            groupBox2.Controls.Add(Smoothing_CHK);
            groupBox2.Controls.Add(Smoothing_CBO);
            groupBox2.Controls.Add(Averaging_TXT);
            groupBox2.Controls.Add(label6);
            groupBox2.Controls.Add(Coherence_Mask_TXT);
            groupBox2.Controls.Add(label21);
            groupBox2.Controls.Add(min_ms_TXT);
            groupBox2.Controls.Add(label4);
            groupBox2.Controls.Add(max_ms_TXT);
            groupBox2.Controls.Add(label5);
            groupBox2.Controls.Add(FFTSize_CBO);
            groupBox2.Controls.Add(label9);
            groupBox2.Controls.Add(maxdB_TXT);
            groupBox2.Controls.Add(label7);
            groupBox2.Controls.Add(mindB_TXT);
            groupBox2.Controls.Add(label8);
            groupBox2.Location = new System.Drawing.Point(559, 866);
            groupBox2.Name = "groupBox2";
            // 150 tall clipped the bottom 3 px off the "Averaging (Exp)" box, which sits at
            // y=127 and is 23 tall. The extra width carries a second column: the ms range and
            // the smoothing controls.
            groupBox2.Size = new System.Drawing.Size(300, 158);
            groupBox2.TabIndex = 307;
            groupBox2.TabStop = false;
            groupBox2.Text = "Chart Settings";
            //
            // Smoothing_CHK
            //
            Smoothing_CHK.AutoSize = true;
            Smoothing_CHK.Location = new System.Drawing.Point(185, 72);
            Smoothing_CHK.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            Smoothing_CHK.Name = "Smoothing_CHK";
            Smoothing_CHK.Size = new System.Drawing.Size(95, 19);
            Smoothing_CHK.TabIndex = 312;
            Smoothing_CHK.Text = "Smoothing:";
            Smoothing_CHK.UseVisualStyleBackColor = true;
            Smoothing_CHK.CheckedChanged += Smoothing_CHK_CheckedChanged;
            //
            // Smoothing_CBO
            //
            // Items are populated on load from FormAlign.SmoothingOptions, so a label and the
            // octave fraction it stands for cannot drift apart.
            Smoothing_CBO.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            Smoothing_CBO.Enabled = false;
            Smoothing_CBO.FormattingEnabled = true;
            Smoothing_CBO.Location = new System.Drawing.Point(185, 96);
            Smoothing_CBO.Name = "Smoothing_CBO";
            Smoothing_CBO.Size = new System.Drawing.Size(100, 23);
            Smoothing_CBO.TabIndex = 313;
            //
            // Averaging_TXT
            // 
            Averaging_TXT.Location = new System.Drawing.Point(109, 127);
            Averaging_TXT.Margin = new System.Windows.Forms.Padding(2);
            Averaging_TXT.Name = "Averaging_TXT";
            Averaging_TXT.Size = new System.Drawing.Size(61, 23);
            Averaging_TXT.TabIndex = 311;
            Averaging_TXT.Text = "0.005";
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new System.Drawing.Point(7, 128);
            label6.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            label6.Name = "label6";
            label6.Size = new System.Drawing.Size(94, 15);
            label6.TabIndex = 310;
            label6.Text = "Averaging (Exp):";
            // 
            // Coherence_Mask_TXT
            // 
            Coherence_Mask_TXT.Location = new System.Drawing.Point(109, 99);
            Coherence_Mask_TXT.Margin = new System.Windows.Forms.Padding(2);
            Coherence_Mask_TXT.Name = "Coherence_Mask_TXT";
            Coherence_Mask_TXT.Size = new System.Drawing.Size(61, 23);
            Coherence_Mask_TXT.TabIndex = 309;
            // 0 disables masking. It used to default to 0.3, which - because the coherence
            // estimate it gated on was itself unusable (see FormAlign.CoherenceWarmupFrames and
            // ComputeFFTs) - punched large holes in the phase and magnitude traces.
            Coherence_Mask_TXT.Text = "0";
            // 
            // label21
            // 
            label21.AutoSize = true;
            label21.Location = new System.Drawing.Point(7, 100);
            label21.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            label21.Name = "label21";
            label21.Size = new System.Drawing.Size(98, 15);
            label21.TabIndex = 308;
            label21.Text = "Coherence Mask:";
            // 
            // min_ms_TXT
            //
            // DEFECT FIX: min_ms_TXT used to sit at y=42, i.e. on the row captioned "Max ms:",
            // and max_ms_TXT sat at y=15 on the row captioned "Min ms:". The two boxes were
            // swapped relative to their own labels, so Plot_IR_Chart fed the user's maximum into
            // AxisX.Minimum. That leaves Minimum above Maximum, which the chart rejects - and
            // because these charts run with SuppressExceptions the failure was silent.
            min_ms_TXT.Location = new System.Drawing.Point(237, 15);
            min_ms_TXT.Margin = new System.Windows.Forms.Padding(2);
            min_ms_TXT.Name = "min_ms_TXT";
            min_ms_TXT.Size = new System.Drawing.Size(43, 23);
            min_ms_TXT.TabIndex = 307;
            min_ms_TXT.Text = "-2";
            //
            // label4
            //
            label4.AutoSize = true;
            label4.Location = new System.Drawing.Point(185, 19);
            label4.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            label4.Name = "label4";
            label4.Size = new System.Drawing.Size(50, 15);
            label4.TabIndex = 304;
            label4.Text = "Min ms:";
            //
            // max_ms_TXT
            //
            max_ms_TXT.Location = new System.Drawing.Point(237, 42);
            max_ms_TXT.Margin = new System.Windows.Forms.Padding(2);
            max_ms_TXT.Name = "max_ms_TXT";
            max_ms_TXT.Size = new System.Drawing.Size(43, 23);
            max_ms_TXT.TabIndex = 305;
            max_ms_TXT.Text = "2";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new System.Drawing.Point(185, 46);
            label5.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            label5.Name = "label5";
            label5.Size = new System.Drawing.Size(52, 15);
            label5.TabIndex = 306;
            label5.Text = "Max ms:";
            // 
            // FFTSize_CBO
            // 
            FFTSize_CBO.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            FFTSize_CBO.FormattingEnabled = true;
            FFTSize_CBO.Items.AddRange(new object[] { "4096", "8192", "16384" });
            FFTSize_CBO.Location = new System.Drawing.Point(62, 69);
            FFTSize_CBO.Name = "FFTSize_CBO";
            FFTSize_CBO.Size = new System.Drawing.Size(109, 23);
            FFTSize_CBO.TabIndex = 303;
            // 
            // label9
            // 
            label9.AutoSize = true;
            label9.Location = new System.Drawing.Point(7, 73);
            label9.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            label9.Name = "label9";
            label9.Size = new System.Drawing.Size(51, 15);
            label9.TabIndex = 302;
            label9.Text = "FFT Size:";
            // 
            // maxdB_TXT
            // 
            maxdB_TXT.Location = new System.Drawing.Point(59, 42);
            maxdB_TXT.Margin = new System.Windows.Forms.Padding(2);
            maxdB_TXT.Name = "maxdB_TXT";
            maxdB_TXT.Size = new System.Drawing.Size(43, 23);
            maxdB_TXT.TabIndex = 301;
            maxdB_TXT.Text = "12";
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new System.Drawing.Point(7, 19);
            label7.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            label7.Name = "label7";
            label7.Size = new System.Drawing.Size(48, 15);
            label7.TabIndex = 298;
            label7.Text = "Min dB:";
            // 
            // mindB_TXT
            // 
            mindB_TXT.Location = new System.Drawing.Point(59, 15);
            mindB_TXT.Margin = new System.Windows.Forms.Padding(2);
            mindB_TXT.Name = "mindB_TXT";
            mindB_TXT.Size = new System.Drawing.Size(43, 23);
            mindB_TXT.TabIndex = 299;
            mindB_TXT.Text = "-48";
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Location = new System.Drawing.Point(7, 46);
            label8.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            label8.Name = "label8";
            label8.Size = new System.Drawing.Size(50, 15);
            label8.TabIndex = 300;
            label8.Text = "Max dB:";
            // 
            // RefreshTimer
            // 
            // DEFECT FIX: this used to be Enabled = true, i.e. the timer was armed from
            // InitializeComponent - which runs on the thread that CONSTRUCTS the form. This form
            // is constructed on the application's main UI thread but pumped by its own STA thread
            // (ctl_MonitorPage.btn_Align_Click), so the timer's message window ended up owned by
            // the main thread while every control handle belonged to the form's own thread, and
            // RefreshTimer_Tick therefore ran cross-thread against all of them:
            // "Cross-thread operation not valid: Control 'Smoothing_CBO' accessed from a thread
            // other than the thread it was created on."
            // FormAlign_Load starts it instead, so the tick arrives on the thread that owns the
            // controls. This matches FormRTA, which arms its plot timers from RTA_Shown.
            RefreshTimer.Enabled = false;
            RefreshTimer.Interval = 200;
            RefreshTimer.Tick += RefreshTimer_Tick;
            // 
            // Chart_Phase
            // 
            chartArea2.Name = "ChartArea1";
            Chart_Phase.ChartAreas.Add(chartArea2);
            Chart_Phase.Location = new System.Drawing.Point(10, 227);
            Chart_Phase.Margin = new System.Windows.Forms.Padding(1);
            Chart_Phase.Name = "Chart_Phase";
            series4.ChartArea = "ChartArea1";
            series4.ChartType = SeriesChartType.Line;
            series4.Name = "Series1";
            series5.ChartArea = "ChartArea1";
            series5.ChartType = SeriesChartType.Line;
            series5.Name = "Series2";
            Chart_Phase.Series.Add(series4);
            Chart_Phase.Series.Add(series5);
            Chart_Phase.Size = new System.Drawing.Size(1315, 266);
            Chart_Phase.TabIndex = 308;
            Chart_Phase.Text = "chart3";
            title7.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold);
            title7.Name = "Title";
            title7.Text = "Phase (Degrees)";
            title8.Alignment = System.Drawing.ContentAlignment.TopRight;
            title8.Docking = Docking.Bottom;
            title8.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.2F);
            title8.Name = "AxisX";
            title8.Text = "Deg";
            title9.Docking = Docking.Left;
            title9.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.2F);
            title9.Name = "AxisY";
            title9.Text = "Phase";
            title10.Alignment = System.Drawing.ContentAlignment.BottomLeft;
            title10.Docking = Docking.Bottom;
            title10.IsDockedInsideChartArea = false;
            title10.Name = "Max";
            title10.Position.Auto = false;
            title10.Position.Width = 87.22672F;
            title10.Position.X = 5F;
            title10.Position.Y = 96F;
            title10.Text = "Max: 0 | -0";
            title10.TextOrientation = TextOrientation.Horizontal;
            title11.Alignment = System.Drawing.ContentAlignment.BottomLeft;
            title11.Docking = Docking.Bottom;
            title11.IsDockedInsideChartArea = false;
            title11.Name = "Min";
            title11.Position.Auto = false;
            title11.Position.Width = 70F;
            title11.Position.X = 30F;
            title11.Position.Y = 96F;
            title11.Text = "Min: 0 | -0";
            title11.TextOrientation = TextOrientation.Horizontal;
            title12.Alignment = System.Drawing.ContentAlignment.BottomLeft;
            title12.Docking = Docking.Bottom;
            title12.IsDockedInsideChartArea = false;
            title12.Name = "Mouse";
            title12.Position.Auto = false;
            title12.Position.Width = 40F;
            title12.Position.X = 60F;
            title12.Position.Y = 96F;
            title12.Text = "Mouse: 0 | -0";
            title12.TextOrientation = TextOrientation.Horizontal;
            Chart_Phase.Titles.Add(title7);
            Chart_Phase.Titles.Add(title8);
            Chart_Phase.Titles.Add(title9);
            Chart_Phase.Titles.Add(title10);
            Chart_Phase.Titles.Add(title11);
            Chart_Phase.Titles.Add(title12);
            // 
            // Chart_IR
            // 
            chartArea3.Name = "ChartArea1";
            Chart_IR.ChartAreas.Add(chartArea3);
            Chart_IR.Location = new System.Drawing.Point(11, 10);
            Chart_IR.Margin = new System.Windows.Forms.Padding(1);
            Chart_IR.Name = "Chart_IR";
            series6.ChartArea = "ChartArea1";
            series6.ChartType = SeriesChartType.Line;
            series6.Name = "Series1";
            series7.ChartArea = "ChartArea1";
            series7.ChartType = SeriesChartType.Line;
            series7.Name = "Series2";
            Chart_IR.Series.Add(series6);
            Chart_IR.Series.Add(series7);
            Chart_IR.Size = new System.Drawing.Size(1315, 215);
            Chart_IR.TabIndex = 309;
            Chart_IR.Text = "chart3";
            title13.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold);
            title13.Name = "Title";
            title13.Text = "Impulse Response(ms)";
            title14.Alignment = System.Drawing.ContentAlignment.TopRight;
            title14.Docking = Docking.Bottom;
            title14.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.2F);
            title14.Name = "AxisX";
            title14.Text = "ms";
            title15.Docking = Docking.Left;
            title15.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.2F);
            title15.Name = "AxisY";
            title15.Text = "dB";
            title16.Alignment = System.Drawing.ContentAlignment.BottomLeft;
            title16.Docking = Docking.Bottom;
            title16.IsDockedInsideChartArea = false;
            title16.Name = "Max";
            title16.Position.Auto = false;
            title16.Position.Width = 87.22672F;
            title16.Position.X = 5F;
            title16.Position.Y = 96F;
            title16.Text = "Max: 0 | -0";
            title16.TextOrientation = TextOrientation.Horizontal;
            title17.Alignment = System.Drawing.ContentAlignment.BottomLeft;
            title17.Docking = Docking.Bottom;
            title17.IsDockedInsideChartArea = false;
            title17.Name = "Min";
            title17.Position.Auto = false;
            title17.Position.Width = 70F;
            title17.Position.X = 30F;
            title17.Position.Y = 96F;
            title17.Text = "Min: 0 | -0";
            title17.TextOrientation = TextOrientation.Horizontal;
            title18.Alignment = System.Drawing.ContentAlignment.BottomLeft;
            title18.Docking = Docking.Bottom;
            title18.IsDockedInsideChartArea = false;
            title18.Name = "Mouse";
            title18.Position.Auto = false;
            title18.Position.Width = 40F;
            title18.Position.X = 60F;
            title18.Position.Y = 96F;
            title18.Text = "Mouse: 0 | -0";
            title18.TextOrientation = TextOrientation.Horizontal;
            Chart_IR.Titles.Add(title13);
            Chart_IR.Titles.Add(title14);
            Chart_IR.Titles.Add(title15);
            Chart_IR.Titles.Add(title16);
            Chart_IR.Titles.Add(title17);
            Chart_IR.Titles.Add(title18);
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(Reset_BTN);
            groupBox1.Controls.Add(Frames_LBL);
            groupBox1.Controls.Add(label23);
            groupBox1.Controls.Add(DelayDelta_LBL);
            groupBox1.Controls.Add(label22);
            groupBox1.Controls.Add(Delay2_LBL);
            groupBox1.Controls.Add(label18);
            groupBox1.Controls.Add(Delay1_LBL);
            groupBox1.Controls.Add(label20);
            groupBox1.Controls.Add(Coherence_LBL);
            groupBox1.Controls.Add(label16);
            groupBox1.Controls.Add(Coherence2_LBL);
            groupBox1.Controls.Add(label14);
            groupBox1.Controls.Add(Coherence1_LBL);
            groupBox1.Controls.Add(label12);
            // Shifted right and narrowed to make room for the wider Chart Settings box, while
            // keeping its right edge flush with the charts above it (x = 1325).
            groupBox1.Location = new System.Drawing.Point(865, 877);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new System.Drawing.Size(460, 150);
            groupBox1.TabIndex = 310;
            groupBox1.TabStop = false;
            groupBox1.Text = "Stats";
            // 
            // Delay2_LBL
            //
            Delay2_LBL.AutoSize = true;
            Delay2_LBL.Location = new System.Drawing.Point(288, 41);
            Delay2_LBL.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            Delay2_LBL.Name = "Delay2_LBL";
            Delay2_LBL.Size = new System.Drawing.Size(13, 15);
            Delay2_LBL.TabIndex = 310;
            Delay2_LBL.Text = "-";
            //
            // label18
            //
            label18.AutoSize = true;
            label18.Location = new System.Drawing.Point(195, 41);
            label18.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            label18.Name = "label18";
            label18.Size = new System.Drawing.Size(75, 15);
            label18.TabIndex = 309;
            label18.Text = "Delay 2 (ms):";
            //
            // Delay1_LBL
            //
            Delay1_LBL.AutoSize = true;
            Delay1_LBL.Location = new System.Drawing.Point(288, 19);
            Delay1_LBL.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            Delay1_LBL.Name = "Delay1_LBL";
            Delay1_LBL.Size = new System.Drawing.Size(13, 15);
            Delay1_LBL.TabIndex = 308;
            Delay1_LBL.Text = "-";
            //
            // label20
            //
            label20.AutoSize = true;
            label20.Location = new System.Drawing.Point(195, 19);
            label20.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            label20.Name = "label20";
            label20.Size = new System.Drawing.Size(75, 15);
            label20.TabIndex = 307;
            label20.Text = "Delay 1 (ms):";
            //
            // DelayDelta_LBL
            //
            DelayDelta_LBL.AutoSize = true;
            DelayDelta_LBL.Location = new System.Drawing.Point(288, 63);
            DelayDelta_LBL.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            DelayDelta_LBL.Name = "DelayDelta_LBL";
            DelayDelta_LBL.Size = new System.Drawing.Size(13, 15);
            DelayDelta_LBL.TabIndex = 312;
            DelayDelta_LBL.Text = "-";
            //
            // label22
            //
            label22.AutoSize = true;
            label22.Location = new System.Drawing.Point(195, 63);
            label22.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            label22.Name = "label22";
            label22.Size = new System.Drawing.Size(88, 15);
            label22.TabIndex = 311;
            label22.Text = "Delay 1-2 (ms):";
            //
            // Frames_LBL
            //
            Frames_LBL.AutoSize = true;
            Frames_LBL.Location = new System.Drawing.Point(122, 85);
            Frames_LBL.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            Frames_LBL.Name = "Frames_LBL";
            Frames_LBL.Size = new System.Drawing.Size(13, 15);
            Frames_LBL.TabIndex = 314;
            Frames_LBL.Text = "0";
            //
            // label23
            //
            label23.AutoSize = true;
            label23.Location = new System.Drawing.Point(5, 85);
            label23.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            label23.Name = "label23";
            label23.Size = new System.Drawing.Size(75, 15);
            label23.TabIndex = 313;
            label23.Text = "Avg Frames:";
            //
            // Reset_BTN
            //
            Reset_BTN.Location = new System.Drawing.Point(340, 105);
            Reset_BTN.Name = "Reset_BTN";
            Reset_BTN.Size = new System.Drawing.Size(110, 30);
            Reset_BTN.TabIndex = 315;
            Reset_BTN.Text = "Reset";
            Reset_BTN.UseVisualStyleBackColor = true;
            Reset_BTN.Click += Reset_BTN_Click;
            //
            // Coherence_LBL
            //
            Coherence_LBL.AutoSize = true;
            Coherence_LBL.Location = new System.Drawing.Point(122, 19);
            Coherence_LBL.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            Coherence_LBL.Name = "Coherence_LBL";
            Coherence_LBL.Size = new System.Drawing.Size(13, 15);
            Coherence_LBL.TabIndex = 306;
            Coherence_LBL.Text = "-";
            //
            // label16
            //
            label16.AutoSize = true;
            label16.Location = new System.Drawing.Point(5, 19);
            label16.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            label16.Name = "label16";
            label16.Size = new System.Drawing.Size(98, 15);
            label16.TabIndex = 305;
            label16.Text = "Coherence Mask:";
            //
            // Coherence2_LBL
            //
            Coherence2_LBL.AutoSize = true;
            Coherence2_LBL.Location = new System.Drawing.Point(122, 63);
            Coherence2_LBL.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            Coherence2_LBL.Name = "Coherence2_LBL";
            Coherence2_LBL.Size = new System.Drawing.Size(13, 15);
            Coherence2_LBL.TabIndex = 304;
            Coherence2_LBL.Text = "-";
            //
            // label14
            //
            label14.AutoSize = true;
            label14.Location = new System.Drawing.Point(5, 63);
            label14.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            label14.Name = "label14";
            label14.Size = new System.Drawing.Size(115, 15);
            label14.TabIndex = 303;
            label14.Text = "Coherence 2 (avg):";
            //
            // Coherence1_LBL
            //
            Coherence1_LBL.AutoSize = true;
            Coherence1_LBL.Location = new System.Drawing.Point(122, 41);
            Coherence1_LBL.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            Coherence1_LBL.Name = "Coherence1_LBL";
            Coherence1_LBL.Size = new System.Drawing.Size(13, 15);
            Coherence1_LBL.TabIndex = 302;
            Coherence1_LBL.Text = "-";
            //
            // label12
            //
            label12.AutoSize = true;
            label12.Location = new System.Drawing.Point(5, 41);
            label12.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            label12.Name = "label12";
            label12.Size = new System.Drawing.Size(115, 15);
            label12.TabIndex = 301;
            label12.Text = "Coherence 1 (avg):";
            //
            // FormAlign
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(1904, 1041);
            Controls.Add(groupBox1);
            Controls.Add(Chart_IR);
            Controls.Add(Chart_Phase);
            Controls.Add(groupBox2);
            Controls.Add(Ref_Color_LBL);
            Controls.Add(label2);
            Controls.Add(cboRef);
            Controls.Add(Source2_Color_LBL);
            Controls.Add(label1);
            Controls.Add(cboSource2);
            Controls.Add(Source1_Color_LBL);
            Controls.Add(label3);
            Controls.Add(cboSource1);
            Controls.Add(Chart_Mag);
            Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
            Name = "FormAlign";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "Align Signals";
            WindowState = System.Windows.Forms.FormWindowState.Maximized;
            Load += FormAlign_Load;
            FormClosing += FormAlign_FormClosing;
            ((System.ComponentModel.ISupportInitialize)Chart_Mag).EndInit();
            groupBox2.ResumeLayout(false);
            groupBox2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)Chart_Phase).EndInit();
            ((System.ComponentModel.ISupportInitialize)Chart_IR).EndInit();
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        protected System.Windows.Forms.DataVisualization.Charting.Chart Chart_Mag;
        protected System.Windows.Forms.Label label3;
        protected System.Windows.Forms.ComboBox cboSource1;
        protected System.Windows.Forms.Label label1;
        protected System.Windows.Forms.ComboBox cboSource2;
        protected System.Windows.Forms.Label label2;
        protected System.Windows.Forms.ComboBox cboRef;
        protected System.Windows.Forms.GroupBox groupBox2;
        protected System.Windows.Forms.ComboBox FFTSize_CBO;
        protected System.Windows.Forms.Label label9;
        protected System.Windows.Forms.TextBox maxdB_TXT;
        protected System.Windows.Forms.Label label7;
        protected System.Windows.Forms.TextBox mindB_TXT;
        protected System.Windows.Forms.Label label8;
        private System.Windows.Forms.Timer RefreshTimer;
        protected System.Windows.Forms.DataVisualization.Charting.Chart Chart_Phase;
        protected System.Windows.Forms.DataVisualization.Charting.Chart Chart_IR;
        protected System.Windows.Forms.TextBox min_ms_TXT;
        protected System.Windows.Forms.Label label4;
        protected System.Windows.Forms.TextBox max_ms_TXT;
        protected System.Windows.Forms.Label label5;
        protected System.Windows.Forms.GroupBox groupBox1;
        protected System.Windows.Forms.Label Coherence2_LBL;
        protected System.Windows.Forms.Label label14;
        protected System.Windows.Forms.Label Coherence1_LBL;
        protected System.Windows.Forms.Label label12;
        protected System.Windows.Forms.Label Coherence_LBL;
        protected System.Windows.Forms.Label label16;
        protected System.Windows.Forms.TextBox Coherence_Mask_TXT;
        protected System.Windows.Forms.Label label21;
        protected System.Windows.Forms.Label Delay2_LBL;
        protected System.Windows.Forms.Label label18;
        protected System.Windows.Forms.Label Delay1_LBL;
        protected System.Windows.Forms.Label label20;
        protected System.Windows.Forms.TextBox Averaging_TXT;
        protected System.Windows.Forms.Label label6;
        protected System.Windows.Forms.Label DelayDelta_LBL;
        protected System.Windows.Forms.Label label22;
        protected System.Windows.Forms.Label Frames_LBL;
        protected System.Windows.Forms.Label label23;
        protected System.Windows.Forms.Button Reset_BTN;
        protected System.Windows.Forms.Label Source1_Color_LBL;
        protected System.Windows.Forms.Label Source2_Color_LBL;
        protected System.Windows.Forms.Label Ref_Color_LBL;
        protected System.Windows.Forms.CheckBox Smoothing_CHK;
        protected System.Windows.Forms.ComboBox Smoothing_CBO;
    }
}