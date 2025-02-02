
namespace BassThatHz_ASIO_DSP_Processor.GUI.Tabs
{
    partial class ctl_StatsPage
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
            components = new System.ComponentModel.Container();
            lbl_TotalChannels = new System.Windows.Forms.Label();
            label5 = new System.Windows.Forms.Label();
            lbl_Total_DSP_Filters = new System.Windows.Forms.Label();
            label4 = new System.Windows.Forms.Label();
            lbl_ASIO_Output_Latency = new System.Windows.Forms.Label();
            lbl_ASIO_Input_Latency = new System.Windows.Forms.Label();
            label2 = new System.Windows.Forms.Label();
            label1 = new System.Windows.Forms.Label();
            lbl_TotalCPU = new System.Windows.Forms.Label();
            label7 = new System.Windows.Forms.Label();
            lbl_InputChannels = new System.Windows.Forms.Label();
            label9 = new System.Windows.Forms.Label();
            lbl_OutputChannels = new System.Windows.Forms.Label();
            label11 = new System.Windows.Forms.Label();
            lbl_InputBufferSizeLatency = new System.Windows.Forms.Label();
            label17 = new System.Windows.Forms.Label();
            lbl_OutputBufferSizeLatency = new System.Windows.Forms.Label();
            label19 = new System.Windows.Forms.Label();
            lbl_ProcessPriorityLevel = new System.Windows.Forms.Label();
            label21 = new System.Windows.Forms.Label();
            btnStop_ASIO_DSP = new System.Windows.Forms.Button();
            btnStart_ASIO_DSP = new System.Windows.Forms.Button();
            Update_Stats_Timer = new System.Windows.Forms.Timer(components);
            chkEnableStats = new System.Windows.Forms.CheckBox();
            label6 = new System.Windows.Forms.Label();
            lbl_App_CPU_Usage = new System.Windows.Forms.Label();
            label8 = new System.Windows.Forms.Label();
            lblASIOBitType = new System.Windows.Forms.Label();
            label10 = new System.Windows.Forms.Label();
            lblSampleRate = new System.Windows.Forms.Label();
            UpdateBiQuadsTotal_Timer = new System.Windows.Forms.Timer(components);
            lbl_Total_Enabled_DSP_Filters = new System.Windows.Forms.Label();
            label16 = new System.Windows.Forms.Label();
            label15 = new System.Windows.Forms.Label();
            lbl_Current_DSP_Load = new System.Windows.Forms.Label();
            label22 = new System.Windows.Forms.Label();
            groupBox1 = new System.Windows.Forms.GroupBox();
            lbl_TotalBufferLatency = new System.Windows.Forms.Label();
            groupBox2 = new System.Windows.Forms.GroupBox();
            lbl_Average_DSP_Latency = new System.Windows.Forms.Label();
            label18 = new System.Windows.Forms.Label();
            lbl_Max_Detected_DSP_Latency = new System.Windows.Forms.Label();
            label20 = new System.Windows.Forms.Label();
            lbl_DSP_Processing_Latency = new System.Windows.Forms.Label();
            label14 = new System.Windows.Forms.Label();
            label12 = new System.Windows.Forms.Label();
            lbl_OutputBufferConversionLatency = new System.Windows.Forms.Label();
            lbl_InputBufferConversionLatency = new System.Windows.Forms.Label();
            label13 = new System.Windows.Forms.Label();
            lbl_TotalDSP_Processing_Latency = new System.Windows.Forms.Label();
            label3 = new System.Windows.Forms.Label();
            lbl_Max_DSP_Load = new System.Windows.Forms.Label();
            label25 = new System.Windows.Forms.Label();
            lbl_Average_DSP_Load = new System.Windows.Forms.Label();
            label27 = new System.Windows.Forms.Label();
            groupBox3 = new System.Windows.Forms.GroupBox();
            label28 = new System.Windows.Forms.Label();
            lbl_Underruns = new System.Windows.Forms.Label();
            btn_ResetStats = new System.Windows.Forms.Button();
            lbl_TotalStreams = new System.Windows.Forms.Label();
            label24 = new System.Windows.Forms.Label();
            label23 = new System.Windows.Forms.Label();
            label26 = new System.Windows.Forms.Label();
            lbl_AppUpTime = new System.Windows.Forms.Label();
            lbl_DSPRunTime = new System.Windows.Forms.Label();
            label29 = new System.Windows.Forms.Label();
            lbl_ASIO_Thread_ID = new System.Windows.Forms.Label();
            lbl_UI_Thread_ID = new System.Windows.Forms.Label();
            label31 = new System.Windows.Forms.Label();
            NoGC_Timer = new System.Windows.Forms.Timer(components);
            label30 = new System.Windows.Forms.Label();
            lblRAM = new System.Windows.Forms.Label();
            chkNoGCMode = new System.Windows.Forms.CheckBox();
            lblRAM_Limit = new System.Windows.Forms.Label();
            groupBox1.SuspendLayout();
            groupBox2.SuspendLayout();
            groupBox3.SuspendLayout();
            SuspendLayout();
            // 
            // lbl_TotalChannels
            // 
            lbl_TotalChannels.AutoSize = true;
            lbl_TotalChannels.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F);
            lbl_TotalChannels.Location = new System.Drawing.Point(988, 420);
            lbl_TotalChannels.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            lbl_TotalChannels.Name = "lbl_TotalChannels";
            lbl_TotalChannels.Size = new System.Drawing.Size(24, 26);
            lbl_TotalChannels.TabIndex = 26;
            lbl_TotalChannels.Text = "0";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F);
            label5.Location = new System.Drawing.Point(786, 420);
            label5.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label5.Name = "label5";
            label5.Size = new System.Drawing.Size(163, 26);
            label5.TabIndex = 25;
            label5.Text = "Total Channels:";
            // 
            // lbl_Total_DSP_Filters
            // 
            lbl_Total_DSP_Filters.AutoSize = true;
            lbl_Total_DSP_Filters.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F);
            lbl_Total_DSP_Filters.Location = new System.Drawing.Point(988, 523);
            lbl_Total_DSP_Filters.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            lbl_Total_DSP_Filters.Name = "lbl_Total_DSP_Filters";
            lbl_Total_DSP_Filters.Size = new System.Drawing.Size(24, 26);
            lbl_Total_DSP_Filters.TabIndex = 24;
            lbl_Total_DSP_Filters.Text = "0";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F);
            label4.Location = new System.Drawing.Point(771, 523);
            label4.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label4.Name = "label4";
            label4.Size = new System.Drawing.Size(182, 26);
            label4.TabIndex = 23;
            label4.Text = "Total DSP Filters:";
            // 
            // lbl_ASIO_Output_Latency
            // 
            lbl_ASIO_Output_Latency.AutoSize = true;
            lbl_ASIO_Output_Latency.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F);
            lbl_ASIO_Output_Latency.Location = new System.Drawing.Point(509, 224);
            lbl_ASIO_Output_Latency.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            lbl_ASIO_Output_Latency.Name = "lbl_ASIO_Output_Latency";
            lbl_ASIO_Output_Latency.Size = new System.Drawing.Size(90, 26);
            lbl_ASIO_Output_Latency.TabIndex = 20;
            lbl_ASIO_Output_Latency.Text = "00.0000";
            // 
            // lbl_ASIO_Input_Latency
            // 
            lbl_ASIO_Input_Latency.AutoSize = true;
            lbl_ASIO_Input_Latency.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F);
            lbl_ASIO_Input_Latency.Location = new System.Drawing.Point(509, 62);
            lbl_ASIO_Input_Latency.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            lbl_ASIO_Input_Latency.Name = "lbl_ASIO_Input_Latency";
            lbl_ASIO_Input_Latency.Size = new System.Drawing.Size(90, 26);
            lbl_ASIO_Input_Latency.TabIndex = 19;
            lbl_ASIO_Input_Latency.Text = "00.0000";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F);
            label2.Location = new System.Drawing.Point(0, 220);
            label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(473, 26);
            label2.TabIndex = 18;
            label2.Text = "Static HW Reported ASIO Output Latency (ms):";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F);
            label1.Location = new System.Drawing.Point(15, 62);
            label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(456, 26);
            label1.TabIndex = 17;
            label1.Text = "Static HW Reported ASIO Input Latency (ms):";
            // 
            // lbl_TotalCPU
            // 
            lbl_TotalCPU.AutoSize = true;
            lbl_TotalCPU.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F);
            lbl_TotalCPU.Location = new System.Drawing.Point(988, 113);
            lbl_TotalCPU.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            lbl_TotalCPU.Name = "lbl_TotalCPU";
            lbl_TotalCPU.Size = new System.Drawing.Size(24, 26);
            lbl_TotalCPU.TabIndex = 28;
            lbl_TotalCPU.Text = "0";
            lbl_TotalCPU.Visible = false;
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F);
            label7.Location = new System.Drawing.Point(732, 113);
            label7.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label7.Name = "label7";
            label7.Size = new System.Drawing.Size(227, 26);
            label7.TabIndex = 27;
            label7.Text = "Total CPU Usage (%):";
            label7.Visible = false;
            // 
            // lbl_InputChannels
            // 
            lbl_InputChannels.AutoSize = true;
            lbl_InputChannels.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F);
            lbl_InputChannels.Location = new System.Drawing.Point(988, 318);
            lbl_InputChannels.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            lbl_InputChannels.Name = "lbl_InputChannels";
            lbl_InputChannels.Size = new System.Drawing.Size(24, 26);
            lbl_InputChannels.TabIndex = 30;
            lbl_InputChannels.Text = "0";
            // 
            // label9
            // 
            label9.AutoSize = true;
            label9.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F);
            label9.Location = new System.Drawing.Point(786, 318);
            label9.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label9.Name = "label9";
            label9.Size = new System.Drawing.Size(164, 26);
            label9.TabIndex = 29;
            label9.Text = "Input Channels:";
            // 
            // lbl_OutputChannels
            // 
            lbl_OutputChannels.AutoSize = true;
            lbl_OutputChannels.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F);
            lbl_OutputChannels.Location = new System.Drawing.Point(988, 369);
            lbl_OutputChannels.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            lbl_OutputChannels.Name = "lbl_OutputChannels";
            lbl_OutputChannels.Size = new System.Drawing.Size(24, 26);
            lbl_OutputChannels.TabIndex = 32;
            lbl_OutputChannels.Text = "0";
            // 
            // label11
            // 
            label11.AutoSize = true;
            label11.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F);
            label11.Location = new System.Drawing.Point(771, 369);
            label11.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label11.Name = "label11";
            label11.Size = new System.Drawing.Size(181, 26);
            label11.TabIndex = 31;
            label11.Text = "Output Channels:";
            // 
            // lbl_InputBufferSizeLatency
            // 
            lbl_InputBufferSizeLatency.AutoSize = true;
            lbl_InputBufferSizeLatency.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F);
            lbl_InputBufferSizeLatency.Location = new System.Drawing.Point(509, 115);
            lbl_InputBufferSizeLatency.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            lbl_InputBufferSizeLatency.Name = "lbl_InputBufferSizeLatency";
            lbl_InputBufferSizeLatency.Size = new System.Drawing.Size(90, 26);
            lbl_InputBufferSizeLatency.TabIndex = 38;
            lbl_InputBufferSizeLatency.Text = "00.0000";
            // 
            // label17
            // 
            label17.AutoSize = true;
            label17.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F);
            label17.Location = new System.Drawing.Point(95, 113);
            label17.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label17.Name = "label17";
            label17.Size = new System.Drawing.Size(372, 26);
            label17.TabIndex = 37;
            label17.Text = "Static Input Buffer Size Latency (ms):";
            // 
            // lbl_OutputBufferSizeLatency
            // 
            lbl_OutputBufferSizeLatency.AutoSize = true;
            lbl_OutputBufferSizeLatency.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F);
            lbl_OutputBufferSizeLatency.Location = new System.Drawing.Point(509, 169);
            lbl_OutputBufferSizeLatency.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            lbl_OutputBufferSizeLatency.Name = "lbl_OutputBufferSizeLatency";
            lbl_OutputBufferSizeLatency.Size = new System.Drawing.Size(90, 26);
            lbl_OutputBufferSizeLatency.TabIndex = 40;
            lbl_OutputBufferSizeLatency.Text = "00.0000";
            // 
            // label19
            // 
            label19.AutoSize = true;
            label19.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F);
            label19.Location = new System.Drawing.Point(80, 164);
            label19.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label19.Name = "label19";
            label19.Size = new System.Drawing.Size(389, 26);
            label19.TabIndex = 39;
            label19.Text = "Static Output Buffer Size Latency (ms):";
            // 
            // lbl_ProcessPriorityLevel
            // 
            lbl_ProcessPriorityLevel.AutoSize = true;
            lbl_ProcessPriorityLevel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F);
            lbl_ProcessPriorityLevel.Location = new System.Drawing.Point(988, 164);
            lbl_ProcessPriorityLevel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            lbl_ProcessPriorityLevel.Name = "lbl_ProcessPriorityLevel";
            lbl_ProcessPriorityLevel.Size = new System.Drawing.Size(24, 26);
            lbl_ProcessPriorityLevel.TabIndex = 42;
            lbl_ProcessPriorityLevel.Text = "0";
            // 
            // label21
            // 
            label21.AutoSize = true;
            label21.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F);
            label21.Location = new System.Drawing.Point(730, 164);
            label21.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label21.Name = "label21";
            label21.Size = new System.Drawing.Size(229, 26);
            label21.TabIndex = 41;
            label21.Text = "Process Priority Level:";
            // 
            // btnStop_ASIO_DSP
            // 
            btnStop_ASIO_DSP.Location = new System.Drawing.Point(19, 87);
            btnStop_ASIO_DSP.Margin = new System.Windows.Forms.Padding(4);
            btnStop_ASIO_DSP.Name = "btnStop_ASIO_DSP";
            btnStop_ASIO_DSP.Size = new System.Drawing.Size(245, 60);
            btnStop_ASIO_DSP.TabIndex = 45;
            btnStop_ASIO_DSP.Text = "Stop ASIO DSP";
            btnStop_ASIO_DSP.UseVisualStyleBackColor = true;
            btnStop_ASIO_DSP.Click += btnStop_ASIO_DSP_Click;
            // 
            // btnStart_ASIO_DSP
            // 
            btnStart_ASIO_DSP.Location = new System.Drawing.Point(19, 4);
            btnStart_ASIO_DSP.Margin = new System.Windows.Forms.Padding(4);
            btnStart_ASIO_DSP.Name = "btnStart_ASIO_DSP";
            btnStart_ASIO_DSP.Size = new System.Drawing.Size(245, 60);
            btnStart_ASIO_DSP.TabIndex = 44;
            btnStart_ASIO_DSP.Text = "Start ASIO DSP";
            btnStart_ASIO_DSP.UseVisualStyleBackColor = true;
            btnStart_ASIO_DSP.Click += btnStart_ASIO_DSP_Click;
            // 
            // Update_Stats_Timer
            // 
            Update_Stats_Timer.Interval = 1000;
            Update_Stats_Timer.Tick += Update_Stats_Timer_Tick;
            // 
            // chkEnableStats
            // 
            chkEnableStats.AutoSize = true;
            chkEnableStats.Location = new System.Drawing.Point(312, 15);
            chkEnableStats.Margin = new System.Windows.Forms.Padding(4);
            chkEnableStats.Name = "chkEnableStats";
            chkEnableStats.Size = new System.Drawing.Size(174, 36);
            chkEnableStats.TabIndex = 46;
            chkEnableStats.Text = "Enable Stats";
            chkEnableStats.UseVisualStyleBackColor = true;
            chkEnableStats.CheckedChanged += chkEnableStats_CheckedChanged;
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F);
            label6.Location = new System.Drawing.Point(741, 62);
            label6.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label6.Name = "label6";
            label6.Size = new System.Drawing.Size(219, 26);
            label6.TabIndex = 47;
            label6.Text = "App CPU Usage (%):";
            label6.Visible = false;
            // 
            // lbl_App_CPU_Usage
            // 
            lbl_App_CPU_Usage.AutoSize = true;
            lbl_App_CPU_Usage.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F);
            lbl_App_CPU_Usage.Location = new System.Drawing.Point(988, 62);
            lbl_App_CPU_Usage.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            lbl_App_CPU_Usage.Name = "lbl_App_CPU_Usage";
            lbl_App_CPU_Usage.Size = new System.Drawing.Size(24, 26);
            lbl_App_CPU_Usage.TabIndex = 48;
            lbl_App_CPU_Usage.Text = "0";
            lbl_App_CPU_Usage.Visible = false;
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Location = new System.Drawing.Point(787, 625);
            label8.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label8.Name = "label8";
            label8.Size = new System.Drawing.Size(164, 32);
            label8.TabIndex = 49;
            label8.Text = "ASIO Bit Type:";
            // 
            // lblASIOBitType
            // 
            lblASIOBitType.AutoSize = true;
            lblASIOBitType.Location = new System.Drawing.Point(988, 625);
            lblASIOBitType.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            lblASIOBitType.Name = "lblASIOBitType";
            lblASIOBitType.Size = new System.Drawing.Size(27, 32);
            lblASIOBitType.TabIndex = 50;
            lblASIOBitType.Text = "0";
            // 
            // label10
            // 
            label10.AutoSize = true;
            label10.Location = new System.Drawing.Point(797, 681);
            label10.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label10.Name = "label10";
            label10.Size = new System.Drawing.Size(152, 32);
            label10.TabIndex = 51;
            label10.Text = "Sample Rate:";
            // 
            // lblSampleRate
            // 
            lblSampleRate.AutoSize = true;
            lblSampleRate.Location = new System.Drawing.Point(988, 681);
            lblSampleRate.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            lblSampleRate.Name = "lblSampleRate";
            lblSampleRate.Size = new System.Drawing.Size(27, 32);
            lblSampleRate.TabIndex = 52;
            lblSampleRate.Text = "0";
            // 
            // UpdateBiQuadsTotal_Timer
            // 
            UpdateBiQuadsTotal_Timer.Interval = 10000;
            UpdateBiQuadsTotal_Timer.Tick += UpdateBiQuadsTotal_Timer_Tick;
            // 
            // lbl_Total_Enabled_DSP_Filters
            // 
            lbl_Total_Enabled_DSP_Filters.AutoSize = true;
            lbl_Total_Enabled_DSP_Filters.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F);
            lbl_Total_Enabled_DSP_Filters.Location = new System.Drawing.Point(988, 574);
            lbl_Total_Enabled_DSP_Filters.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            lbl_Total_Enabled_DSP_Filters.Name = "lbl_Total_Enabled_DSP_Filters";
            lbl_Total_Enabled_DSP_Filters.Size = new System.Drawing.Size(24, 26);
            lbl_Total_Enabled_DSP_Filters.TabIndex = 56;
            lbl_Total_Enabled_DSP_Filters.Text = "0";
            // 
            // label16
            // 
            label16.AutoSize = true;
            label16.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F);
            label16.Location = new System.Drawing.Point(693, 574);
            label16.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label16.Name = "label16";
            label16.Size = new System.Drawing.Size(268, 26);
            label16.TabIndex = 55;
            label16.Text = "Total Enabled DSP Filters:";
            // 
            // label15
            // 
            label15.AutoSize = true;
            label15.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F);
            label15.Location = new System.Drawing.Point(204, 38);
            label15.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label15.Name = "label15";
            label15.Size = new System.Drawing.Size(236, 26);
            label15.TabIndex = 57;
            label15.Text = "Current DSP Load (%):";
            // 
            // lbl_Current_DSP_Load
            // 
            lbl_Current_DSP_Load.AutoSize = true;
            lbl_Current_DSP_Load.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F);
            lbl_Current_DSP_Load.Location = new System.Drawing.Point(509, 36);
            lbl_Current_DSP_Load.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            lbl_Current_DSP_Load.Name = "lbl_Current_DSP_Load";
            lbl_Current_DSP_Load.Size = new System.Drawing.Size(90, 26);
            lbl_Current_DSP_Load.TabIndex = 58;
            lbl_Current_DSP_Load.Text = "00.0000";
            // 
            // label22
            // 
            label22.AutoSize = true;
            label22.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F);
            label22.Location = new System.Drawing.Point(89, 271);
            label22.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label22.Name = "label22";
            label22.Size = new System.Drawing.Size(374, 26);
            label22.TabIndex = 63;
            label22.Text = "Total Round-Trip Buffer Latency (ms):";
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(lbl_TotalBufferLatency);
            groupBox1.Controls.Add(label1);
            groupBox1.Controls.Add(label22);
            groupBox1.Controls.Add(label2);
            groupBox1.Controls.Add(lbl_ASIO_Input_Latency);
            groupBox1.Controls.Add(lbl_ASIO_Output_Latency);
            groupBox1.Controls.Add(label17);
            groupBox1.Controls.Add(lbl_InputBufferSizeLatency);
            groupBox1.Controls.Add(label19);
            groupBox1.Controls.Add(lbl_OutputBufferSizeLatency);
            groupBox1.Location = new System.Drawing.Point(19, 160);
            groupBox1.Margin = new System.Windows.Forms.Padding(4);
            groupBox1.Name = "groupBox1";
            groupBox1.Padding = new System.Windows.Forms.Padding(4);
            groupBox1.Size = new System.Drawing.Size(643, 329);
            groupBox1.TabIndex = 64;
            groupBox1.TabStop = false;
            groupBox1.Text = "ASIO Buffer Latency:";
            // 
            // lbl_TotalBufferLatency
            // 
            lbl_TotalBufferLatency.AutoSize = true;
            lbl_TotalBufferLatency.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F);
            lbl_TotalBufferLatency.Location = new System.Drawing.Point(509, 279);
            lbl_TotalBufferLatency.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            lbl_TotalBufferLatency.Name = "lbl_TotalBufferLatency";
            lbl_TotalBufferLatency.Size = new System.Drawing.Size(90, 26);
            lbl_TotalBufferLatency.TabIndex = 64;
            lbl_TotalBufferLatency.Text = "00.0000";
            // 
            // groupBox2
            // 
            groupBox2.Controls.Add(lbl_Average_DSP_Latency);
            groupBox2.Controls.Add(label18);
            groupBox2.Controls.Add(lbl_Max_Detected_DSP_Latency);
            groupBox2.Controls.Add(label20);
            groupBox2.Controls.Add(lbl_DSP_Processing_Latency);
            groupBox2.Controls.Add(label14);
            groupBox2.Controls.Add(label12);
            groupBox2.Controls.Add(lbl_OutputBufferConversionLatency);
            groupBox2.Controls.Add(lbl_InputBufferConversionLatency);
            groupBox2.Controls.Add(label13);
            groupBox2.Controls.Add(lbl_TotalDSP_Processing_Latency);
            groupBox2.Controls.Add(label3);
            groupBox2.Location = new System.Drawing.Point(4, 499);
            groupBox2.Margin = new System.Windows.Forms.Padding(4);
            groupBox2.Name = "groupBox2";
            groupBox2.Padding = new System.Windows.Forms.Padding(4);
            groupBox2.Size = new System.Drawing.Size(657, 380);
            groupBox2.TabIndex = 65;
            groupBox2.TabStop = false;
            groupBox2.Text = "DSP Processing Time:";
            // 
            // lbl_Average_DSP_Latency
            // 
            lbl_Average_DSP_Latency.AutoSize = true;
            lbl_Average_DSP_Latency.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F);
            lbl_Average_DSP_Latency.Location = new System.Drawing.Point(524, 265);
            lbl_Average_DSP_Latency.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            lbl_Average_DSP_Latency.Name = "lbl_Average_DSP_Latency";
            lbl_Average_DSP_Latency.Size = new System.Drawing.Size(90, 26);
            lbl_Average_DSP_Latency.TabIndex = 74;
            lbl_Average_DSP_Latency.Text = "00.0000";
            // 
            // label18
            // 
            label18.AutoSize = true;
            label18.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F);
            label18.Location = new System.Drawing.Point(39, 265);
            label18.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label18.Name = "label18";
            label18.Size = new System.Drawing.Size(433, 26);
            label18.TabIndex = 73;
            label18.Text = "2sec Averaged DSP Processing Time (ms):";
            // 
            // lbl_Max_Detected_DSP_Latency
            // 
            lbl_Max_Detected_DSP_Latency.AutoSize = true;
            lbl_Max_Detected_DSP_Latency.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F);
            lbl_Max_Detected_DSP_Latency.Location = new System.Drawing.Point(524, 318);
            lbl_Max_Detected_DSP_Latency.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            lbl_Max_Detected_DSP_Latency.Name = "lbl_Max_Detected_DSP_Latency";
            lbl_Max_Detected_DSP_Latency.Size = new System.Drawing.Size(90, 26);
            lbl_Max_Detected_DSP_Latency.TabIndex = 72;
            lbl_Max_Detected_DSP_Latency.Text = "00.0000";
            // 
            // label20
            // 
            label20.AutoSize = true;
            label20.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F);
            label20.Location = new System.Drawing.Point(126, 318);
            label20.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label20.Name = "label20";
            label20.Size = new System.Drawing.Size(338, 26);
            label20.TabIndex = 71;
            label20.Text = "Peak DSP Processing Time (ms):";
            // 
            // lbl_DSP_Processing_Latency
            // 
            lbl_DSP_Processing_Latency.AutoSize = true;
            lbl_DSP_Processing_Latency.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F);
            lbl_DSP_Processing_Latency.Location = new System.Drawing.Point(524, 111);
            lbl_DSP_Processing_Latency.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            lbl_DSP_Processing_Latency.Name = "lbl_DSP_Processing_Latency";
            lbl_DSP_Processing_Latency.Size = new System.Drawing.Size(90, 26);
            lbl_DSP_Processing_Latency.TabIndex = 70;
            lbl_DSP_Processing_Latency.Text = "00.0000";
            // 
            // label14
            // 
            label14.AutoSize = true;
            label14.Location = new System.Drawing.Point(156, 111);
            label14.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label14.Name = "label14";
            label14.Size = new System.Drawing.Size(294, 32);
            label14.TabIndex = 69;
            label14.Text = "DSP Processing Time (ms):";
            // 
            // label12
            // 
            label12.AutoSize = true;
            label12.Location = new System.Drawing.Point(54, 162);
            label12.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label12.Name = "label12";
            label12.Size = new System.Drawing.Size(406, 32);
            label12.TabIndex = 68;
            label12.Text = "Output Buffer Conversion Time (ms):";
            // 
            // lbl_OutputBufferConversionLatency
            // 
            lbl_OutputBufferConversionLatency.AutoSize = true;
            lbl_OutputBufferConversionLatency.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F);
            lbl_OutputBufferConversionLatency.Location = new System.Drawing.Point(524, 162);
            lbl_OutputBufferConversionLatency.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            lbl_OutputBufferConversionLatency.Name = "lbl_OutputBufferConversionLatency";
            lbl_OutputBufferConversionLatency.Size = new System.Drawing.Size(90, 26);
            lbl_OutputBufferConversionLatency.TabIndex = 67;
            lbl_OutputBufferConversionLatency.Text = "00.0000";
            // 
            // lbl_InputBufferConversionLatency
            // 
            lbl_InputBufferConversionLatency.AutoSize = true;
            lbl_InputBufferConversionLatency.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F);
            lbl_InputBufferConversionLatency.Location = new System.Drawing.Point(524, 60);
            lbl_InputBufferConversionLatency.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            lbl_InputBufferConversionLatency.Name = "lbl_InputBufferConversionLatency";
            lbl_InputBufferConversionLatency.Size = new System.Drawing.Size(90, 26);
            lbl_InputBufferConversionLatency.TabIndex = 66;
            lbl_InputBufferConversionLatency.Text = "00.0000";
            // 
            // label13
            // 
            label13.AutoSize = true;
            label13.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F);
            label13.Location = new System.Drawing.Point(115, 60);
            label13.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label13.Name = "label13";
            label13.Size = new System.Drawing.Size(350, 26);
            label13.TabIndex = 65;
            label13.Text = "Input Buffer Conversion Time (ms):";
            // 
            // lbl_TotalDSP_Processing_Latency
            // 
            lbl_TotalDSP_Processing_Latency.AutoSize = true;
            lbl_TotalDSP_Processing_Latency.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F);
            lbl_TotalDSP_Processing_Latency.Location = new System.Drawing.Point(524, 215);
            lbl_TotalDSP_Processing_Latency.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            lbl_TotalDSP_Processing_Latency.Name = "lbl_TotalDSP_Processing_Latency";
            lbl_TotalDSP_Processing_Latency.Size = new System.Drawing.Size(90, 26);
            lbl_TotalDSP_Processing_Latency.TabIndex = 64;
            lbl_TotalDSP_Processing_Latency.Text = "00.0000";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F);
            label3.Location = new System.Drawing.Point(128, 215);
            label3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label3.Name = "label3";
            label3.Size = new System.Drawing.Size(335, 26);
            label3.TabIndex = 63;
            label3.Text = "Total DSP Processing Time (ms):";
            // 
            // lbl_Max_DSP_Load
            // 
            lbl_Max_DSP_Load.AutoSize = true;
            lbl_Max_DSP_Load.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F);
            lbl_Max_DSP_Load.Location = new System.Drawing.Point(509, 128);
            lbl_Max_DSP_Load.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            lbl_Max_DSP_Load.Name = "lbl_Max_DSP_Load";
            lbl_Max_DSP_Load.Size = new System.Drawing.Size(90, 26);
            lbl_Max_DSP_Load.TabIndex = 67;
            lbl_Max_DSP_Load.Text = "00.0000";
            // 
            // label25
            // 
            label25.AutoSize = true;
            label25.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F);
            label25.Location = new System.Drawing.Point(221, 130);
            label25.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label25.Name = "label25";
            label25.Size = new System.Drawing.Size(214, 26);
            label25.TabIndex = 66;
            label25.Text = "Peak DSP Load (%):";
            // 
            // lbl_Average_DSP_Load
            // 
            lbl_Average_DSP_Load.AutoSize = true;
            lbl_Average_DSP_Load.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F);
            lbl_Average_DSP_Load.Location = new System.Drawing.Point(509, 81);
            lbl_Average_DSP_Load.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            lbl_Average_DSP_Load.Name = "lbl_Average_DSP_Load";
            lbl_Average_DSP_Load.Size = new System.Drawing.Size(90, 26);
            lbl_Average_DSP_Load.TabIndex = 69;
            lbl_Average_DSP_Load.Text = "00.0000";
            // 
            // label27
            // 
            label27.AutoSize = true;
            label27.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F);
            label27.Location = new System.Drawing.Point(134, 83);
            label27.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label27.Name = "label27";
            label27.Size = new System.Drawing.Size(309, 26);
            label27.TabIndex = 68;
            label27.Text = "2sec Averaged DSP Load (%):";
            // 
            // groupBox3
            // 
            groupBox3.Controls.Add(label28);
            groupBox3.Controls.Add(lbl_Underruns);
            groupBox3.Controls.Add(label15);
            groupBox3.Controls.Add(lbl_Average_DSP_Load);
            groupBox3.Controls.Add(lbl_Current_DSP_Load);
            groupBox3.Controls.Add(label27);
            groupBox3.Controls.Add(label25);
            groupBox3.Controls.Add(lbl_Max_DSP_Load);
            groupBox3.Location = new System.Drawing.Point(19, 892);
            groupBox3.Margin = new System.Windows.Forms.Padding(4);
            groupBox3.Name = "groupBox3";
            groupBox3.Padding = new System.Windows.Forms.Padding(4);
            groupBox3.Size = new System.Drawing.Size(641, 220);
            groupBox3.TabIndex = 70;
            groupBox3.TabStop = false;
            groupBox3.Text = "DSP Load:";
            // 
            // label28
            // 
            label28.AutoSize = true;
            label28.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F);
            label28.Location = new System.Drawing.Point(305, 175);
            label28.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label28.Name = "label28";
            label28.Size = new System.Drawing.Size(119, 26);
            label28.TabIndex = 70;
            label28.Text = "Underruns:";
            // 
            // lbl_Underruns
            // 
            lbl_Underruns.AutoSize = true;
            lbl_Underruns.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F);
            lbl_Underruns.Location = new System.Drawing.Point(509, 173);
            lbl_Underruns.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            lbl_Underruns.Name = "lbl_Underruns";
            lbl_Underruns.Size = new System.Drawing.Size(24, 26);
            lbl_Underruns.TabIndex = 71;
            lbl_Underruns.Text = "0";
            // 
            // btn_ResetStats
            // 
            btn_ResetStats.Location = new System.Drawing.Point(490, 4);
            btn_ResetStats.Margin = new System.Windows.Forms.Padding(4);
            btn_ResetStats.Name = "btn_ResetStats";
            btn_ResetStats.Size = new System.Drawing.Size(171, 62);
            btn_ResetStats.TabIndex = 71;
            btn_ResetStats.Text = "Reset Stats";
            btn_ResetStats.UseVisualStyleBackColor = true;
            btn_ResetStats.Click += btn_ResetStats_Click;
            // 
            // lbl_TotalStreams
            // 
            lbl_TotalStreams.AutoSize = true;
            lbl_TotalStreams.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F);
            lbl_TotalStreams.Location = new System.Drawing.Point(988, 471);
            lbl_TotalStreams.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            lbl_TotalStreams.Name = "lbl_TotalStreams";
            lbl_TotalStreams.Size = new System.Drawing.Size(24, 26);
            lbl_TotalStreams.TabIndex = 73;
            lbl_TotalStreams.Text = "0";
            // 
            // label24
            // 
            label24.AutoSize = true;
            label24.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F);
            label24.Location = new System.Drawing.Point(797, 471);
            label24.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label24.Name = "label24";
            label24.Size = new System.Drawing.Size(153, 26);
            label24.TabIndex = 72;
            label24.Text = "Total Streams:";
            // 
            // label23
            // 
            label23.AutoSize = true;
            label23.Location = new System.Drawing.Point(669, 907);
            label23.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label23.Name = "label23";
            label23.Size = new System.Drawing.Size(162, 32);
            label23.TabIndex = 74;
            label23.Text = "App Up-Time:";
            // 
            // label26
            // 
            label26.AutoSize = true;
            label26.Location = new System.Drawing.Point(669, 1011);
            label26.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label26.Name = "label26";
            label26.Size = new System.Drawing.Size(174, 32);
            label26.TabIndex = 75;
            label26.Text = "DSP Run-Time:";
            // 
            // lbl_AppUpTime
            // 
            lbl_AppUpTime.AutoSize = true;
            lbl_AppUpTime.Location = new System.Drawing.Point(669, 949);
            lbl_AppUpTime.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            lbl_AppUpTime.Name = "lbl_AppUpTime";
            lbl_AppUpTime.Size = new System.Drawing.Size(27, 32);
            lbl_AppUpTime.TabIndex = 76;
            lbl_AppUpTime.Text = "0";
            // 
            // lbl_DSPRunTime
            // 
            lbl_DSPRunTime.AutoSize = true;
            lbl_DSPRunTime.Location = new System.Drawing.Point(669, 1050);
            lbl_DSPRunTime.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            lbl_DSPRunTime.Name = "lbl_DSPRunTime";
            lbl_DSPRunTime.Size = new System.Drawing.Size(27, 32);
            lbl_DSPRunTime.TabIndex = 77;
            lbl_DSPRunTime.Text = "0";
            // 
            // label29
            // 
            label29.AutoSize = true;
            label29.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F);
            label29.Location = new System.Drawing.Point(776, 215);
            label29.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label29.Name = "label29";
            label29.Size = new System.Drawing.Size(172, 26);
            label29.TabIndex = 78;
            label29.Text = "ASIO Thread ID:";
            // 
            // lbl_ASIO_Thread_ID
            // 
            lbl_ASIO_Thread_ID.AutoSize = true;
            lbl_ASIO_Thread_ID.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F);
            lbl_ASIO_Thread_ID.Location = new System.Drawing.Point(988, 215);
            lbl_ASIO_Thread_ID.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            lbl_ASIO_Thread_ID.Name = "lbl_ASIO_Thread_ID";
            lbl_ASIO_Thread_ID.Size = new System.Drawing.Size(31, 26);
            lbl_ASIO_Thread_ID.TabIndex = 79;
            lbl_ASIO_Thread_ID.Text = "-1";
            // 
            // lbl_UI_Thread_ID
            // 
            lbl_UI_Thread_ID.AutoSize = true;
            lbl_UI_Thread_ID.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F);
            lbl_UI_Thread_ID.Location = new System.Drawing.Point(988, 267);
            lbl_UI_Thread_ID.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            lbl_UI_Thread_ID.Name = "lbl_UI_Thread_ID";
            lbl_UI_Thread_ID.Size = new System.Drawing.Size(31, 26);
            lbl_UI_Thread_ID.TabIndex = 81;
            lbl_UI_Thread_ID.Text = "-1";
            // 
            // label31
            // 
            label31.AutoSize = true;
            label31.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F);
            label31.Location = new System.Drawing.Point(802, 267);
            label31.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label31.Name = "label31";
            label31.Size = new System.Drawing.Size(141, 26);
            label31.TabIndex = 80;
            label31.Text = "UI Thread ID:";
            // 
            // NoGC_Timer
            // 
            NoGC_Timer.Interval = 1000;
            NoGC_Timer.Tick += NoGC_Timer_Tick;
            // 
            // label30
            // 
            label30.AutoSize = true;
            label30.Location = new System.Drawing.Point(774, 819);
            label30.Name = "label30";
            label30.Size = new System.Drawing.Size(185, 32);
            label30.TabIndex = 82;
            label30.Text = "RAM used (MB):";
            // 
            // lblRAM
            // 
            lblRAM.AutoSize = true;
            lblRAM.Location = new System.Drawing.Point(985, 822);
            lblRAM.Name = "lblRAM";
            lblRAM.Size = new System.Drawing.Size(27, 32);
            lblRAM.TabIndex = 83;
            lblRAM.Text = "0";
            // 
            // chkNoGCMode
            // 
            chkNoGCMode.AutoSize = true;
            chkNoGCMode.Location = new System.Drawing.Point(675, 733);
            chkNoGCMode.Name = "chkNoGCMode";
            chkNoGCMode.Size = new System.Drawing.Size(411, 36);
            chkNoGCMode.TabIndex = 84;
            chkNoGCMode.Text = "Critical Audo Mode (Experimental)";
            chkNoGCMode.UseVisualStyleBackColor = true;
            chkNoGCMode.CheckedChanged += chkNoGCMode_CheckedChanged;
            // 
            // lblRAM_Limit
            // 
            lblRAM_Limit.AutoSize = true;
            lblRAM_Limit.Location = new System.Drawing.Point(988, 777);
            lblRAM_Limit.Name = "lblRAM_Limit";
            lblRAM_Limit.Size = new System.Drawing.Size(27, 32);
            lblRAM_Limit.TabIndex = 85;
            lblRAM_Limit.Text = "0";
            // 
            // ctl_StatsPage
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(13F, 32F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(lblRAM_Limit);
            Controls.Add(chkNoGCMode);
            Controls.Add(lblRAM);
            Controls.Add(label30);
            Controls.Add(lbl_UI_Thread_ID);
            Controls.Add(label31);
            Controls.Add(lbl_ASIO_Thread_ID);
            Controls.Add(label29);
            Controls.Add(lbl_DSPRunTime);
            Controls.Add(lbl_AppUpTime);
            Controls.Add(label26);
            Controls.Add(label23);
            Controls.Add(lbl_TotalStreams);
            Controls.Add(label24);
            Controls.Add(btn_ResetStats);
            Controls.Add(groupBox3);
            Controls.Add(groupBox2);
            Controls.Add(groupBox1);
            Controls.Add(lbl_Total_Enabled_DSP_Filters);
            Controls.Add(label16);
            Controls.Add(lblSampleRate);
            Controls.Add(label10);
            Controls.Add(lblASIOBitType);
            Controls.Add(label8);
            Controls.Add(lbl_App_CPU_Usage);
            Controls.Add(label6);
            Controls.Add(chkEnableStats);
            Controls.Add(btnStop_ASIO_DSP);
            Controls.Add(btnStart_ASIO_DSP);
            Controls.Add(lbl_ProcessPriorityLevel);
            Controls.Add(label21);
            Controls.Add(lbl_OutputChannels);
            Controls.Add(label11);
            Controls.Add(lbl_InputChannels);
            Controls.Add(label9);
            Controls.Add(lbl_TotalCPU);
            Controls.Add(label7);
            Controls.Add(lbl_TotalChannels);
            Controls.Add(label5);
            Controls.Add(lbl_Total_DSP_Filters);
            Controls.Add(label4);
            Margin = new System.Windows.Forms.Padding(4);
            Name = "ctl_StatsPage";
            Size = new System.Drawing.Size(1137, 1129);
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            groupBox2.ResumeLayout(false);
            groupBox2.PerformLayout();
            groupBox3.ResumeLayout(false);
            groupBox3.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        protected System.Windows.Forms.Label lbl_TotalChannels;
        protected System.Windows.Forms.Label label5;
        protected System.Windows.Forms.Label lbl_Total_DSP_Filters;
        protected System.Windows.Forms.Label label4;
        protected System.Windows.Forms.Label lbl_ASIO_Output_Latency;
        protected System.Windows.Forms.Label lbl_ASIO_Input_Latency;
        protected System.Windows.Forms.Label label2;
        protected System.Windows.Forms.Label label1;
        protected System.Windows.Forms.Label lbl_TotalCPU;
        protected System.Windows.Forms.Label label7;
        protected System.Windows.Forms.Label lbl_InputChannels;
        protected System.Windows.Forms.Label label9;
        protected System.Windows.Forms.Label lbl_OutputChannels;
        protected System.Windows.Forms.Label label11;
        protected System.Windows.Forms.Label lbl_InputBufferSizeLatency;
        protected System.Windows.Forms.Label label17;
        protected System.Windows.Forms.Label lbl_OutputBufferSizeLatency;
        protected System.Windows.Forms.Label label19;
        protected System.Windows.Forms.Label lbl_ProcessPriorityLevel;
        protected System.Windows.Forms.Label label21;
        protected System.Windows.Forms.Button btnStop_ASIO_DSP;
        protected System.Windows.Forms.Button btnStart_ASIO_DSP;
        protected System.Windows.Forms.Timer Update_Stats_Timer;
        protected System.Windows.Forms.CheckBox chkEnableStats;
        protected System.Windows.Forms.Label label6;
        protected System.Windows.Forms.Label lbl_App_CPU_Usage;
        protected System.Windows.Forms.Label label8;
        protected System.Windows.Forms.Label lblASIOBitType;
        protected System.Windows.Forms.Label label10;
        protected System.Windows.Forms.Label lblSampleRate;
        protected System.Windows.Forms.Timer UpdateBiQuadsTotal_Timer;
        protected System.Windows.Forms.Label lbl_Total_Enabled_DSP_Filters;
        protected System.Windows.Forms.Label label16;
        protected System.Windows.Forms.Label label15;
        protected System.Windows.Forms.Label lbl_Current_DSP_Load;
        protected System.Windows.Forms.Label label22;
        protected System.Windows.Forms.GroupBox groupBox1;
        protected System.Windows.Forms.Label lbl_TotalBufferLatency;
        protected System.Windows.Forms.GroupBox groupBox2;
        protected System.Windows.Forms.Label lbl_Average_DSP_Latency;
        protected System.Windows.Forms.Label label18;
        protected System.Windows.Forms.Label lbl_Max_Detected_DSP_Latency;
        protected System.Windows.Forms.Label label20;
        protected System.Windows.Forms.Label lbl_DSP_Processing_Latency;
        protected System.Windows.Forms.Label label14;
        protected System.Windows.Forms.Label label12;
        protected System.Windows.Forms.Label lbl_OutputBufferConversionLatency;
        protected System.Windows.Forms.Label lbl_InputBufferConversionLatency;
        protected System.Windows.Forms.Label label13;
        protected System.Windows.Forms.Label lbl_TotalDSP_Processing_Latency;
        protected System.Windows.Forms.Label label3;
        protected System.Windows.Forms.Label lbl_Max_DSP_Load;
        protected System.Windows.Forms.Label label25;
        protected System.Windows.Forms.Label lbl_Average_DSP_Load;
        protected System.Windows.Forms.Label label27;
        protected System.Windows.Forms.GroupBox groupBox3;
        protected System.Windows.Forms.Label label28;
        protected System.Windows.Forms.Label lbl_Underruns;
        protected System.Windows.Forms.Button btn_ResetStats;
        protected System.Windows.Forms.Label lbl_TotalStreams;
        protected System.Windows.Forms.Label label24;
        protected System.Windows.Forms.Label label23;
        protected System.Windows.Forms.Label label26;
        protected System.Windows.Forms.Label lbl_AppUpTime;
        protected System.Windows.Forms.Label lbl_DSPRunTime;
        protected System.Windows.Forms.Label label29;
        protected System.Windows.Forms.Label lbl_ASIO_Thread_ID;
        protected System.Windows.Forms.Label lbl_UI_Thread_ID;
        protected System.Windows.Forms.Label label31;
        protected System.Windows.Forms.Timer NoGC_Timer;
        protected System.Windows.Forms.Label label30;
        protected System.Windows.Forms.Label lblRAM;
        protected System.Windows.Forms.CheckBox chkNoGCMode;
        protected System.Windows.Forms.Label lblRAM_Limit;
    }
}
