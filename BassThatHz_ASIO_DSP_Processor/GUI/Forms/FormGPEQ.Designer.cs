namespace BassThatHz_ASIO_DSP_Processor.GUI.Forms
{
    partial class FormGPEQ
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
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea2 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Series series3 = new System.Windows.Forms.DataVisualization.Charting.Series();
            System.Windows.Forms.DataVisualization.Charting.Series series4 = new System.Windows.Forms.DataVisualization.Charting.Series();
            System.Windows.Forms.DataVisualization.Charting.Title title7 = new System.Windows.Forms.DataVisualization.Charting.Title();
            System.Windows.Forms.DataVisualization.Charting.Title title8 = new System.Windows.Forms.DataVisualization.Charting.Title();
            System.Windows.Forms.DataVisualization.Charting.Title title9 = new System.Windows.Forms.DataVisualization.Charting.Title();
            System.Windows.Forms.DataVisualization.Charting.Title title10 = new System.Windows.Forms.DataVisualization.Charting.Title();
            System.Windows.Forms.DataVisualization.Charting.Title title11 = new System.Windows.Forms.DataVisualization.Charting.Title();
            System.Windows.Forms.DataVisualization.Charting.Title title12 = new System.Windows.Forms.DataVisualization.Charting.Title();
            groupBox1 = new System.Windows.Forms.GroupBox();
            Apply_PEQ_BTN = new System.Windows.Forms.Button();
            PEQEnabled_CHK = new System.Windows.Forms.CheckBox();
            label6 = new System.Windows.Forms.Label();
            txtQ = new System.Windows.Forms.TextBox();
            label5 = new System.Windows.Forms.Label();
            txtG = new System.Windows.Forms.TextBox();
            label3 = new System.Windows.Forms.Label();
            txtF = new System.Windows.Forms.TextBox();
            Add_PEQ_BTN = new System.Windows.Forms.Button();
            MoveFilterDown_BTN = new System.Windows.Forms.Button();
            MoveFilterUp_BTN = new System.Windows.Forms.Button();
            DeleteFilter_BTN = new System.Windows.Forms.Button();
            label2 = new System.Windows.Forms.Label();
            label1 = new System.Windows.Forms.Label();
            LPF_CBO = new System.Windows.Forms.ComboBox();
            HPF_CBO = new System.Windows.Forms.ComboBox();
            HPFFreq_TXT = new System.Windows.Forms.TextBox();
            label59 = new System.Windows.Forms.Label();
            LPFFreq_TXT = new System.Windows.Forms.TextBox();
            label4 = new System.Windows.Forms.Label();
            Filters_LSB = new System.Windows.Forms.ListBox();
            ShowTotalMag_CHK = new System.Windows.Forms.CheckBox();
            ShowTotalPhase_CHK = new System.Windows.Forms.CheckBox();
            ShowIndividualMag_CHK = new System.Windows.Forms.CheckBox();
            ShowIndividualPhase_CHK = new System.Windows.Forms.CheckBox();
            ShowComponentPhase_CHK = new System.Windows.Forms.CheckBox();
            ShowComponentMag_CHK = new System.Windows.Forms.CheckBox();
            Refresh_BTN = new System.Windows.Forms.Button();
            GPEQ_Chart = new System.Windows.Forms.DataVisualization.Charting.Chart();
            mindB_TXT = new System.Windows.Forms.TextBox();
            label7 = new System.Windows.Forms.Label();
            maxdB_TXT = new System.Windows.Forms.TextBox();
            label8 = new System.Windows.Forms.Label();
            label9 = new System.Windows.Forms.Label();
            SaveAndClose_BTN = new System.Windows.Forms.Button();
            DiscardAndClose_BTN = new System.Windows.Forms.Button();
            groupBox2 = new System.Windows.Forms.GroupBox();
            FFTSize_CBO = new System.Windows.Forms.ComboBox();
            Apply_HPFLPF_BTN = new System.Windows.Forms.Button();
            groupBox3 = new System.Windows.Forms.GroupBox();
            HPF_LPF_Enabled_CHK = new System.Windows.Forms.CheckBox();
            Add_HPFLPF_BTN = new System.Windows.Forms.Button();
            groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)GPEQ_Chart).BeginInit();
            groupBox2.SuspendLayout();
            groupBox3.SuspendLayout();
            SuspendLayout();
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(Apply_PEQ_BTN);
            groupBox1.Controls.Add(PEQEnabled_CHK);
            groupBox1.Controls.Add(label6);
            groupBox1.Controls.Add(txtQ);
            groupBox1.Controls.Add(label5);
            groupBox1.Controls.Add(txtG);
            groupBox1.Controls.Add(label3);
            groupBox1.Controls.Add(txtF);
            groupBox1.Controls.Add(Add_PEQ_BTN);
            groupBox1.Location = new System.Drawing.Point(342, 479);
            groupBox1.Margin = new System.Windows.Forms.Padding(2, 1, 2, 1);
            groupBox1.Name = "groupBox1";
            groupBox1.Padding = new System.Windows.Forms.Padding(2, 1, 2, 1);
            groupBox1.Size = new System.Drawing.Size(479, 112);
            groupBox1.TabIndex = 289;
            groupBox1.TabStop = false;
            groupBox1.Text = "PEQ";
            // 
            // Apply_PEQ_BTN
            // 
            Apply_PEQ_BTN.Enabled = false;
            Apply_PEQ_BTN.Location = new System.Drawing.Point(396, 38);
            Apply_PEQ_BTN.Margin = new System.Windows.Forms.Padding(2, 1, 2, 1);
            Apply_PEQ_BTN.Name = "Apply_PEQ_BTN";
            Apply_PEQ_BTN.Size = new System.Drawing.Size(72, 22);
            Apply_PEQ_BTN.TabIndex = 298;
            Apply_PEQ_BTN.Text = "Apply";
            Apply_PEQ_BTN.UseVisualStyleBackColor = true;
            Apply_PEQ_BTN.Click += Apply_PEQ_BTN_Click;
            // 
            // PEQEnabled_CHK
            // 
            PEQEnabled_CHK.AutoSize = true;
            PEQEnabled_CHK.Checked = true;
            PEQEnabled_CHK.CheckState = System.Windows.Forms.CheckState.Checked;
            PEQEnabled_CHK.Location = new System.Drawing.Point(7, 18);
            PEQEnabled_CHK.Margin = new System.Windows.Forms.Padding(2, 1, 2, 1);
            PEQEnabled_CHK.Name = "PEQEnabled_CHK";
            PEQEnabled_CHK.Size = new System.Drawing.Size(68, 19);
            PEQEnabled_CHK.TabIndex = 275;
            PEQEnabled_CHK.Text = "Enabled";
            PEQEnabled_CHK.UseVisualStyleBackColor = true;
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new System.Drawing.Point(5, 43);
            label6.Name = "label6";
            label6.Size = new System.Drawing.Size(65, 15);
            label6.TabIndex = 269;
            label6.Text = "Frequency:";
            // 
            // txtQ
            // 
            txtQ.Location = new System.Drawing.Point(178, 65);
            txtQ.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            txtQ.Name = "txtQ";
            txtQ.Size = new System.Drawing.Size(72, 23);
            txtQ.TabIndex = 274;
            txtQ.Text = "1";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new System.Drawing.Point(91, 48);
            label5.Name = "label5";
            label5.Size = new System.Drawing.Size(34, 15);
            label5.TabIndex = 270;
            label5.Text = "Gain:";
            // 
            // txtG
            // 
            txtG.Location = new System.Drawing.Point(91, 65);
            txtG.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            txtG.Name = "txtG";
            txtG.Size = new System.Drawing.Size(72, 23);
            txtG.TabIndex = 273;
            txtG.Text = "10";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new System.Drawing.Point(178, 48);
            label3.Name = "label3";
            label3.Size = new System.Drawing.Size(19, 15);
            label3.TabIndex = 271;
            label3.Text = "Q:";
            // 
            // txtF
            // 
            txtF.Location = new System.Drawing.Point(5, 65);
            txtF.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            txtF.Name = "txtF";
            txtF.Size = new System.Drawing.Size(72, 23);
            txtF.TabIndex = 272;
            txtF.Text = "100";
            // 
            // Add_PEQ_BTN
            // 
            Add_PEQ_BTN.Location = new System.Drawing.Point(396, 87);
            Add_PEQ_BTN.Margin = new System.Windows.Forms.Padding(2, 1, 2, 1);
            Add_PEQ_BTN.Name = "Add_PEQ_BTN";
            Add_PEQ_BTN.Size = new System.Drawing.Size(75, 22);
            Add_PEQ_BTN.TabIndex = 279;
            Add_PEQ_BTN.Text = "Add New";
            Add_PEQ_BTN.UseVisualStyleBackColor = true;
            Add_PEQ_BTN.Click += Add_PEQ_BTN_Click;
            // 
            // MoveFilterDown_BTN
            // 
            MoveFilterDown_BTN.Location = new System.Drawing.Point(249, 606);
            MoveFilterDown_BTN.Margin = new System.Windows.Forms.Padding(2, 1, 2, 1);
            MoveFilterDown_BTN.Name = "MoveFilterDown_BTN";
            MoveFilterDown_BTN.Size = new System.Drawing.Size(83, 22);
            MoveFilterDown_BTN.TabIndex = 297;
            MoveFilterDown_BTN.Text = "Move Down";
            MoveFilterDown_BTN.UseVisualStyleBackColor = true;
            MoveFilterDown_BTN.Click += MoveFilterDown_BTN_Click;
            // 
            // MoveFilterUp_BTN
            // 
            MoveFilterUp_BTN.Location = new System.Drawing.Point(249, 582);
            MoveFilterUp_BTN.Margin = new System.Windows.Forms.Padding(2, 1, 2, 1);
            MoveFilterUp_BTN.Name = "MoveFilterUp_BTN";
            MoveFilterUp_BTN.Size = new System.Drawing.Size(83, 22);
            MoveFilterUp_BTN.TabIndex = 296;
            MoveFilterUp_BTN.Text = "Move Up";
            MoveFilterUp_BTN.UseVisualStyleBackColor = true;
            MoveFilterUp_BTN.Click += MoveFilterUp_BTN_Click;
            // 
            // DeleteFilter_BTN
            // 
            DeleteFilter_BTN.Location = new System.Drawing.Point(11, 582);
            DeleteFilter_BTN.Margin = new System.Windows.Forms.Padding(2, 1, 2, 1);
            DeleteFilter_BTN.Name = "DeleteFilter_BTN";
            DeleteFilter_BTN.Size = new System.Drawing.Size(83, 22);
            DeleteFilter_BTN.TabIndex = 280;
            DeleteFilter_BTN.Text = "Delete";
            DeleteFilter_BTN.UseVisualStyleBackColor = true;
            DeleteFilter_BTN.Click += DeleteFilter_BTN_Click;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new System.Drawing.Point(239, 65);
            label2.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(29, 15);
            label2.TabIndex = 288;
            label2.Text = "LPF:";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(38, 66);
            label1.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(32, 15);
            label1.TabIndex = 287;
            label1.Text = "HPF:";
            // 
            // LPF_CBO
            // 
            LPF_CBO.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            LPF_CBO.FormattingEnabled = true;
            LPF_CBO.Location = new System.Drawing.Point(273, 60);
            LPF_CBO.Name = "LPF_CBO";
            LPF_CBO.Size = new System.Drawing.Size(125, 23);
            LPF_CBO.TabIndex = 286;
            // 
            // HPF_CBO
            // 
            HPF_CBO.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            HPF_CBO.FormattingEnabled = true;
            HPF_CBO.Location = new System.Drawing.Point(73, 61);
            HPF_CBO.Name = "HPF_CBO";
            HPF_CBO.Size = new System.Drawing.Size(125, 23);
            HPF_CBO.TabIndex = 285;
            // 
            // HPFFreq_TXT
            // 
            HPFFreq_TXT.Location = new System.Drawing.Point(73, 87);
            HPFFreq_TXT.Margin = new System.Windows.Forms.Padding(2);
            HPFFreq_TXT.Name = "HPFFreq_TXT";
            HPFFreq_TXT.Size = new System.Drawing.Size(125, 23);
            HPFFreq_TXT.TabIndex = 284;
            HPFFreq_TXT.Text = "1";
            // 
            // label59
            // 
            label59.AutoSize = true;
            label59.Location = new System.Drawing.Point(4, 91);
            label59.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            label59.Name = "label59";
            label59.Size = new System.Drawing.Size(65, 15);
            label59.TabIndex = 283;
            label59.Text = "Frequency:";
            // 
            // LPFFreq_TXT
            // 
            LPFFreq_TXT.Location = new System.Drawing.Point(273, 85);
            LPFFreq_TXT.Margin = new System.Windows.Forms.Padding(2);
            LPFFreq_TXT.Name = "LPFFreq_TXT";
            LPFFreq_TXT.Size = new System.Drawing.Size(125, 23);
            LPFFreq_TXT.TabIndex = 282;
            LPFFreq_TXT.Text = "20000";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new System.Drawing.Point(207, 90);
            label4.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            label4.Name = "label4";
            label4.Size = new System.Drawing.Size(65, 15);
            label4.TabIndex = 281;
            label4.Text = "Frequency:";
            // 
            // Filters_LSB
            // 
            Filters_LSB.FormattingEnabled = true;
            Filters_LSB.Items.AddRange(new object[] { "1: Enabled G 50 Q 1 Hz 1", "2: Disabled G 50 Q 1 Hz 1", "Filter3", "Filter4", "Filter5", "Filter6", "Filter7", "Filter8", "Filter9", "Filter10", "Filter11", "Filter12", "Filter13", "Filter14", "Filter15", "Filter16" });
            Filters_LSB.Location = new System.Drawing.Point(11, 350);
            Filters_LSB.Margin = new System.Windows.Forms.Padding(2, 1, 2, 1);
            Filters_LSB.Name = "Filters_LSB";
            Filters_LSB.Size = new System.Drawing.Size(321, 229);
            Filters_LSB.TabIndex = 278;
            Filters_LSB.SelectedIndexChanged += Filters_LSB_SelectedIndexChanged;
            // 
            // ShowTotalMag_CHK
            // 
            ShowTotalMag_CHK.AutoSize = true;
            ShowTotalMag_CHK.Checked = true;
            ShowTotalMag_CHK.CheckState = System.Windows.Forms.CheckState.Checked;
            ShowTotalMag_CHK.Location = new System.Drawing.Point(14, 20);
            ShowTotalMag_CHK.Margin = new System.Windows.Forms.Padding(2, 1, 2, 1);
            ShowTotalMag_CHK.Name = "ShowTotalMag_CHK";
            ShowTotalMag_CHK.Size = new System.Drawing.Size(110, 19);
            ShowTotalMag_CHK.TabIndex = 290;
            ShowTotalMag_CHK.Text = "Show Total Mag";
            ShowTotalMag_CHK.UseVisualStyleBackColor = true;
            ShowTotalMag_CHK.CheckedChanged += ShowTotalMag_CHK_CheckedChanged;
            // 
            // ShowTotalPhase_CHK
            // 
            ShowTotalPhase_CHK.AutoSize = true;
            ShowTotalPhase_CHK.Checked = true;
            ShowTotalPhase_CHK.CheckState = System.Windows.Forms.CheckState.Checked;
            ShowTotalPhase_CHK.Location = new System.Drawing.Point(14, 40);
            ShowTotalPhase_CHK.Margin = new System.Windows.Forms.Padding(2, 1, 2, 1);
            ShowTotalPhase_CHK.Name = "ShowTotalPhase_CHK";
            ShowTotalPhase_CHK.Size = new System.Drawing.Size(117, 19);
            ShowTotalPhase_CHK.TabIndex = 291;
            ShowTotalPhase_CHK.Text = "Show Total Phase";
            ShowTotalPhase_CHK.UseVisualStyleBackColor = true;
            ShowTotalPhase_CHK.CheckedChanged += ShowTotalPhase_CHK_CheckedChanged;
            // 
            // ShowIndividualMag_CHK
            // 
            ShowIndividualMag_CHK.AutoSize = true;
            ShowIndividualMag_CHK.Enabled = false;
            ShowIndividualMag_CHK.Location = new System.Drawing.Point(14, 68);
            ShowIndividualMag_CHK.Margin = new System.Windows.Forms.Padding(2, 1, 2, 1);
            ShowIndividualMag_CHK.Name = "ShowIndividualMag_CHK";
            ShowIndividualMag_CHK.Size = new System.Drawing.Size(137, 19);
            ShowIndividualMag_CHK.TabIndex = 292;
            ShowIndividualMag_CHK.Text = "Show Individual Mag";
            ShowIndividualMag_CHK.UseVisualStyleBackColor = true;
            // 
            // ShowIndividualPhase_CHK
            // 
            ShowIndividualPhase_CHK.AutoSize = true;
            ShowIndividualPhase_CHK.Enabled = false;
            ShowIndividualPhase_CHK.Location = new System.Drawing.Point(14, 87);
            ShowIndividualPhase_CHK.Margin = new System.Windows.Forms.Padding(2, 1, 2, 1);
            ShowIndividualPhase_CHK.Name = "ShowIndividualPhase_CHK";
            ShowIndividualPhase_CHK.Size = new System.Drawing.Size(144, 19);
            ShowIndividualPhase_CHK.TabIndex = 293;
            ShowIndividualPhase_CHK.Text = "Show Individual Phase";
            ShowIndividualPhase_CHK.UseVisualStyleBackColor = true;
            // 
            // ShowComponentPhase_CHK
            // 
            ShowComponentPhase_CHK.AutoSize = true;
            ShowComponentPhase_CHK.Enabled = false;
            ShowComponentPhase_CHK.Location = new System.Drawing.Point(14, 135);
            ShowComponentPhase_CHK.Margin = new System.Windows.Forms.Padding(2, 1, 2, 1);
            ShowComponentPhase_CHK.Name = "ShowComponentPhase_CHK";
            ShowComponentPhase_CHK.Size = new System.Drawing.Size(156, 19);
            ShowComponentPhase_CHK.TabIndex = 295;
            ShowComponentPhase_CHK.Text = "Show Component Phase";
            ShowComponentPhase_CHK.UseVisualStyleBackColor = true;
            // 
            // ShowComponentMag_CHK
            // 
            ShowComponentMag_CHK.AutoSize = true;
            ShowComponentMag_CHK.Enabled = false;
            ShowComponentMag_CHK.Location = new System.Drawing.Point(14, 116);
            ShowComponentMag_CHK.Margin = new System.Windows.Forms.Padding(2, 1, 2, 1);
            ShowComponentMag_CHK.Name = "ShowComponentMag_CHK";
            ShowComponentMag_CHK.Size = new System.Drawing.Size(149, 19);
            ShowComponentMag_CHK.TabIndex = 294;
            ShowComponentMag_CHK.Text = "Show Component Mag";
            ShowComponentMag_CHK.UseVisualStyleBackColor = true;
            // 
            // Refresh_BTN
            // 
            Refresh_BTN.Location = new System.Drawing.Point(1023, 353);
            Refresh_BTN.Name = "Refresh_BTN";
            Refresh_BTN.Size = new System.Drawing.Size(102, 23);
            Refresh_BTN.TabIndex = 296;
            Refresh_BTN.Text = "Refresh Chart";
            Refresh_BTN.UseVisualStyleBackColor = true;
            Refresh_BTN.Click += Refresh_BTN_Click;
            // 
            // GPEQ_Chart
            // 
            chartArea2.Name = "ChartArea1";
            GPEQ_Chart.ChartAreas.Add(chartArea2);
            GPEQ_Chart.Location = new System.Drawing.Point(5, 4);
            GPEQ_Chart.Margin = new System.Windows.Forms.Padding(1);
            GPEQ_Chart.Name = "GPEQ_Chart";
            series3.ChartArea = "ChartArea1";
            series3.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
            series3.Name = "Series1";
            series4.ChartArea = "ChartArea1";
            series4.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
            series4.Name = "Series2";
            GPEQ_Chart.Series.Add(series3);
            GPEQ_Chart.Series.Add(series4);
            GPEQ_Chart.Size = new System.Drawing.Size(1130, 340);
            GPEQ_Chart.TabIndex = 297;
            GPEQ_Chart.Text = "chart3";
            title7.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold);
            title7.Name = "Title";
            title7.Text = "FFT";
            title8.Alignment = System.Drawing.ContentAlignment.TopRight;
            title8.Docking = System.Windows.Forms.DataVisualization.Charting.Docking.Bottom;
            title8.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.2F);
            title8.Name = "AxisX";
            title8.Text = "Hz";
            title9.Docking = System.Windows.Forms.DataVisualization.Charting.Docking.Left;
            title9.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.2F);
            title9.Name = "AxisY";
            title9.Text = "dB";
            title10.Alignment = System.Drawing.ContentAlignment.BottomLeft;
            title10.Docking = System.Windows.Forms.DataVisualization.Charting.Docking.Bottom;
            title10.IsDockedInsideChartArea = false;
            title10.Name = "Max";
            title10.Position.Auto = false;
            title10.Position.Width = 87.22672F;
            title10.Position.X = 5F;
            title10.Position.Y = 96F;
            title10.Text = "Max: 0 | -0";
            title10.TextOrientation = System.Windows.Forms.DataVisualization.Charting.TextOrientation.Horizontal;
            title11.Alignment = System.Drawing.ContentAlignment.BottomLeft;
            title11.Docking = System.Windows.Forms.DataVisualization.Charting.Docking.Bottom;
            title11.IsDockedInsideChartArea = false;
            title11.Name = "Min";
            title11.Position.Auto = false;
            title11.Position.Width = 70F;
            title11.Position.X = 30F;
            title11.Position.Y = 96F;
            title11.Text = "Min: 0 | -0";
            title11.TextOrientation = System.Windows.Forms.DataVisualization.Charting.TextOrientation.Horizontal;
            title12.Alignment = System.Drawing.ContentAlignment.BottomLeft;
            title12.Docking = System.Windows.Forms.DataVisualization.Charting.Docking.Bottom;
            title12.IsDockedInsideChartArea = false;
            title12.Name = "Mouse";
            title12.Position.Auto = false;
            title12.Position.Width = 40F;
            title12.Position.X = 60F;
            title12.Position.Y = 96F;
            title12.Text = "Mouse: 0 | -0";
            title12.TextOrientation = System.Windows.Forms.DataVisualization.Charting.TextOrientation.Horizontal;
            GPEQ_Chart.Titles.Add(title7);
            GPEQ_Chart.Titles.Add(title8);
            GPEQ_Chart.Titles.Add(title9);
            GPEQ_Chart.Titles.Add(title10);
            GPEQ_Chart.Titles.Add(title11);
            GPEQ_Chart.Titles.Add(title12);
            // 
            // mindB_TXT
            // 
            mindB_TXT.Location = new System.Drawing.Point(61, 167);
            mindB_TXT.Margin = new System.Windows.Forms.Padding(2);
            mindB_TXT.Name = "mindB_TXT";
            mindB_TXT.Size = new System.Drawing.Size(43, 23);
            mindB_TXT.TabIndex = 299;
            mindB_TXT.Text = "-12";
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new System.Drawing.Point(9, 171);
            label7.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            label7.Name = "label7";
            label7.Size = new System.Drawing.Size(48, 15);
            label7.TabIndex = 298;
            label7.Text = "Min dB:";
            // 
            // maxdB_TXT
            // 
            maxdB_TXT.Location = new System.Drawing.Point(61, 194);
            maxdB_TXT.Margin = new System.Windows.Forms.Padding(2);
            maxdB_TXT.Name = "maxdB_TXT";
            maxdB_TXT.Size = new System.Drawing.Size(43, 23);
            maxdB_TXT.TabIndex = 301;
            maxdB_TXT.Text = "12";
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Location = new System.Drawing.Point(9, 198);
            label8.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            label8.Name = "label8";
            label8.Size = new System.Drawing.Size(50, 15);
            label8.TabIndex = 300;
            label8.Text = "Max dB:";
            // 
            // label9
            // 
            label9.AutoSize = true;
            label9.Location = new System.Drawing.Point(8, 227);
            label9.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            label9.Name = "label9";
            label9.Size = new System.Drawing.Size(51, 15);
            label9.TabIndex = 302;
            label9.Text = "FFT Size:";
            // 
            // SaveAndClose_BTN
            // 
            SaveAndClose_BTN.Location = new System.Drawing.Point(1023, 434);
            SaveAndClose_BTN.Name = "SaveAndClose_BTN";
            SaveAndClose_BTN.Size = new System.Drawing.Size(102, 23);
            SaveAndClose_BTN.TabIndex = 304;
            SaveAndClose_BTN.Text = "Save and Close";
            SaveAndClose_BTN.UseVisualStyleBackColor = true;
            SaveAndClose_BTN.Click += SaveAndClose_BTN_Click;
            // 
            // DiscardAndClose_BTN
            // 
            DiscardAndClose_BTN.Location = new System.Drawing.Point(1010, 602);
            DiscardAndClose_BTN.Name = "DiscardAndClose_BTN";
            DiscardAndClose_BTN.Size = new System.Drawing.Size(115, 25);
            DiscardAndClose_BTN.TabIndex = 305;
            DiscardAndClose_BTN.Text = "Discard and Close";
            DiscardAndClose_BTN.UseVisualStyleBackColor = true;
            DiscardAndClose_BTN.Click += DiscardAndClose_BTN_Click;
            // 
            // groupBox2
            // 
            groupBox2.Controls.Add(FFTSize_CBO);
            groupBox2.Controls.Add(ShowTotalMag_CHK);
            groupBox2.Controls.Add(ShowTotalPhase_CHK);
            groupBox2.Controls.Add(ShowIndividualMag_CHK);
            groupBox2.Controls.Add(ShowIndividualPhase_CHK);
            groupBox2.Controls.Add(ShowComponentMag_CHK);
            groupBox2.Controls.Add(label9);
            groupBox2.Controls.Add(ShowComponentPhase_CHK);
            groupBox2.Controls.Add(maxdB_TXT);
            groupBox2.Controls.Add(label7);
            groupBox2.Controls.Add(mindB_TXT);
            groupBox2.Controls.Add(label8);
            groupBox2.Location = new System.Drawing.Point(827, 353);
            groupBox2.Name = "groupBox2";
            groupBox2.Size = new System.Drawing.Size(177, 253);
            groupBox2.TabIndex = 306;
            groupBox2.TabStop = false;
            groupBox2.Text = "Chart Settings";
            // 
            // FFTSize_CBO
            // 
            FFTSize_CBO.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            FFTSize_CBO.FormattingEnabled = true;
            FFTSize_CBO.Items.AddRange(new object[] { "Low 8192", "High 262144" });
            FFTSize_CBO.Location = new System.Drawing.Point(63, 223);
            FFTSize_CBO.Name = "FFTSize_CBO";
            FFTSize_CBO.Size = new System.Drawing.Size(109, 23);
            FFTSize_CBO.TabIndex = 303;
            // 
            // Apply_HPFLPF_BTN
            // 
            Apply_HPFLPF_BTN.Enabled = false;
            Apply_HPFLPF_BTN.Location = new System.Drawing.Point(404, 33);
            Apply_HPFLPF_BTN.Margin = new System.Windows.Forms.Padding(2, 1, 2, 1);
            Apply_HPFLPF_BTN.Name = "Apply_HPFLPF_BTN";
            Apply_HPFLPF_BTN.Size = new System.Drawing.Size(72, 22);
            Apply_HPFLPF_BTN.TabIndex = 307;
            Apply_HPFLPF_BTN.Text = "Apply";
            Apply_HPFLPF_BTN.UseVisualStyleBackColor = true;
            Apply_HPFLPF_BTN.Click += Apply_HPFLPF_BTN_Click;
            // 
            // groupBox3
            // 
            groupBox3.Controls.Add(HPF_LPF_Enabled_CHK);
            groupBox3.Controls.Add(Add_HPFLPF_BTN);
            groupBox3.Controls.Add(label1);
            groupBox3.Controls.Add(Apply_HPFLPF_BTN);
            groupBox3.Controls.Add(label4);
            groupBox3.Controls.Add(LPFFreq_TXT);
            groupBox3.Controls.Add(label59);
            groupBox3.Controls.Add(HPFFreq_TXT);
            groupBox3.Controls.Add(HPF_CBO);
            groupBox3.Controls.Add(LPF_CBO);
            groupBox3.Controls.Add(label2);
            groupBox3.Location = new System.Drawing.Point(337, 348);
            groupBox3.Name = "groupBox3";
            groupBox3.Size = new System.Drawing.Size(484, 122);
            groupBox3.TabIndex = 308;
            groupBox3.TabStop = false;
            groupBox3.Text = "HPF LPF";
            // 
            // HPF_LPF_Enabled_CHK
            // 
            HPF_LPF_Enabled_CHK.AutoSize = true;
            HPF_LPF_Enabled_CHK.Checked = true;
            HPF_LPF_Enabled_CHK.CheckState = System.Windows.Forms.CheckState.Checked;
            HPF_LPF_Enabled_CHK.Location = new System.Drawing.Point(12, 25);
            HPF_LPF_Enabled_CHK.Margin = new System.Windows.Forms.Padding(2, 1, 2, 1);
            HPF_LPF_Enabled_CHK.Name = "HPF_LPF_Enabled_CHK";
            HPF_LPF_Enabled_CHK.Size = new System.Drawing.Size(68, 19);
            HPF_LPF_Enabled_CHK.TabIndex = 309;
            HPF_LPF_Enabled_CHK.Text = "Enabled";
            HPF_LPF_Enabled_CHK.UseVisualStyleBackColor = true;
            // 
            // Add_HPFLPF_BTN
            // 
            Add_HPFLPF_BTN.Location = new System.Drawing.Point(401, 96);
            Add_HPFLPF_BTN.Margin = new System.Windows.Forms.Padding(2, 1, 2, 1);
            Add_HPFLPF_BTN.Name = "Add_HPFLPF_BTN";
            Add_HPFLPF_BTN.Size = new System.Drawing.Size(75, 22);
            Add_HPFLPF_BTN.TabIndex = 308;
            Add_HPFLPF_BTN.Text = "Add New";
            Add_HPFLPF_BTN.UseVisualStyleBackColor = true;
            Add_HPFLPF_BTN.Click += Add_HPFLPF_BTN_Click;
            // 
            // FormGPEQ
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(1137, 632);
            ControlBox = false;
            Controls.Add(groupBox3);
            Controls.Add(groupBox2);
            Controls.Add(DiscardAndClose_BTN);
            Controls.Add(SaveAndClose_BTN);
            Controls.Add(MoveFilterDown_BTN);
            Controls.Add(MoveFilterUp_BTN);
            Controls.Add(GPEQ_Chart);
            Controls.Add(Refresh_BTN);
            Controls.Add(DeleteFilter_BTN);
            Controls.Add(groupBox1);
            Controls.Add(Filters_LSB);
            Margin = new System.Windows.Forms.Padding(2, 1, 2, 1);
            Name = "FormGPEQ";
            Text = "Configure GPEQ";
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)GPEQ_Chart).EndInit();
            groupBox2.ResumeLayout(false);
            groupBox2.PerformLayout();
            groupBox3.ResumeLayout(false);
            groupBox3.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        protected System.Windows.Forms.GroupBox groupBox1;
        protected System.Windows.Forms.CheckBox PEQEnabled_CHK;
        protected System.Windows.Forms.Label label6;
        protected System.Windows.Forms.TextBox txtQ;
        protected System.Windows.Forms.Label label5;
        protected System.Windows.Forms.TextBox txtG;
        protected System.Windows.Forms.Label label3;
        protected System.Windows.Forms.TextBox txtF;
        protected System.Windows.Forms.Label label2;
        protected System.Windows.Forms.Label label1;
        protected System.Windows.Forms.ComboBox LPF_CBO;
        protected System.Windows.Forms.ComboBox HPF_CBO;
        protected System.Windows.Forms.TextBox HPFFreq_TXT;
        protected System.Windows.Forms.Label label59;
        protected System.Windows.Forms.TextBox LPFFreq_TXT;
        protected System.Windows.Forms.Label label4;
        protected System.Windows.Forms.Button DeleteFilter_BTN;
        protected System.Windows.Forms.Button Add_PEQ_BTN;
        protected System.Windows.Forms.ListBox Filters_LSB;
        protected System.Windows.Forms.CheckBox ShowTotalMag_CHK;
        protected System.Windows.Forms.CheckBox ShowTotalPhase_CHK;
        protected System.Windows.Forms.CheckBox ShowIndividualMag_CHK;
        protected System.Windows.Forms.CheckBox ShowIndividualPhase_CHK;
        protected System.Windows.Forms.CheckBox ShowComponentPhase_CHK;
        protected System.Windows.Forms.CheckBox ShowComponentMag_CHK;
        protected System.Windows.Forms.Button MoveFilterDown_BTN;
        protected System.Windows.Forms.Button MoveFilterUp_BTN;
        protected System.Windows.Forms.Button Apply_PEQ_BTN;
        protected System.Windows.Forms.Button Refresh_BTN;
        protected System.Windows.Forms.DataVisualization.Charting.Chart GPEQ_Chart;
        protected System.Windows.Forms.TextBox mindB_TXT;
        protected System.Windows.Forms.Label label7;
        protected System.Windows.Forms.TextBox maxdB_TXT;
        protected System.Windows.Forms.Label label8;
        protected System.Windows.Forms.Label label9;
        protected System.Windows.Forms.Button SaveAndClose_BTN;
        protected System.Windows.Forms.Button DiscardAndClose_BTN;
        protected System.Windows.Forms.GroupBox groupBox2;
        protected System.Windows.Forms.Button Apply_HPFLPF_BTN;
        protected System.Windows.Forms.GroupBox groupBox3;
        protected System.Windows.Forms.Button Add_HPFLPF_BTN;
        protected System.Windows.Forms.CheckBox HPF_LPF_Enabled_CHK;
        protected System.Windows.Forms.ComboBox FFTSize_CBO;
    }
}