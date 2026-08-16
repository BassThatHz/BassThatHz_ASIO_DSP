#nullable enable

namespace Test_Project_1;

#region Usings
using BassThatHz_ASIO_DSP_Processor;
using BassThatHz_ASIO_DSP_Processor.GUI.Forms;
using NAudio.Utils;
using System;
using System.Reflection;
using System.Windows.Forms.DataVisualization.Charting;
using Test_Project_1.TestHelpers;
#endregion

/// <summary>
/// GUARD SUITE for the RTA analyser framing in GUI\Forms\FormRTA.cs.
///
/// The ULF charts are specified to be accurate in 1 Hz bins from 1 Hz to 100 Hz. Compute_ULF_FFT_Data
/// downsamples its frame to Default_ULF_FFTSize (2048) points, so a frame of exactly ONE SECOND of
/// audio gives a 2048 Hz effective rate and 1 Hz bins. Everything the circular buffer holds beyond
/// that one second is display latency and nothing else.
///
/// Before the fix the ULF buffers were ten seconds deep and the tick guard demanded nearly two
/// seconds of audio before it would plot at all, which is what made the charts read as zeros for
/// the first ten seconds and take another ten seconds to fall silent after the signal stopped.
/// </summary>
[TestClass]
public class Test_FormRTA
{
    #region Test Double

    /// <summary>
    /// Exposes the protected framing helpers and buffer fields so the analyser's sizing rules can
    /// be asserted without driving the WinForms timers.
    /// </summary>
    private sealed class TestableFormRTA : FormRTA
    {
        public static int Public_Get_ULF_FrameLength(int sampleRate) => Get_ULF_FrameLength(sampleRate);

        public static int Public_Get_ULF_BufferLength(int sampleRate) => Get_ULF_BufferLength(sampleRate);

        public static int Public_Get_Top_BufferLength(int fftSize) => Get_Top_BufferLength(fftSize);

        public static int Public_Get_HopLength(int frameLength, int overlapPercentage)
            => Get_HopLength(frameLength, overlapPercentage);

        public static bool Public_HasFullFrame(CircularBuffer buffer, int frameLength)
            => HasFullFrame(buffer, frameLength);

        public void Public_EnsureULFBufferCapacity(int sampleRate) => this.EnsureULFBufferCapacity(sampleRate);

        public void Public_EnsureTopBufferCapacity(int fftSize) => this.EnsureTopBufferCapacity(fftSize);

        public CircularBuffer Public_InputULFBuffer => this.RTA_InputULFBuffer;

        public CircularBuffer Public_OutputULFBuffer => this.RTA_OutputULFBuffer;

        public CircularBuffer Public_InputTopBuffer => this.RTA_InputTopBuffer;

        public CircularBuffer Public_OutputTopBuffer => this.RTA_OutputTopBuffer;

        public int Public_Default_Top_FFTSize => this.Default_Top_FFTSize;

        public Chart Public_InputWaveformChart => this.chart_InputWaveform;

        public Chart Public_OutputWaveformChart => this.chart_OutputWaveform;

        /// <summary>
        /// Plots one block of audio at a given instant, through the real production path, so the
        /// resulting Y axis range can be asserted without waiting on wall-clock time.
        /// </summary>
        public void Public_PlotWaveform(Chart chart, double[] audio, DateTime timestamp)
        {
            var Local_PlotData = this.ComputeWaveformPlotData(audio, 1.5d);
            this.UpdateChartWithPlotData(chart, Local_PlotData, timestamp);
        }

        public RollingPeakEnvelope? Public_Get_WaveformRange(Chart chart) => this.Get_WaveformRange(chart);

        public void Public_ReCalculate_Top_FFT() => this.ReCalculate_Top_FFT();

        public void Public_ReCalculate_ULF_FFT() => this.ReCalculate_ULF_FFT();

        public void Public_Set_Top_FFTSize(int fftSize) => this.Default_Top_FFTSize = fftSize;

        /// <summary>
        /// Runs the combo box setup that RTA_Load would normally do. Without it the form has no
        /// window type selected, so every snapshot is built from Build_Window's fallback and no
        /// test ever exercises real window coefficients.
        /// </summary>
        public void Public_InitialiseComboBoxes()
        {
            this.MapEventHandlers();
            this.Init_Comboboxes();
            this.Init_SetDefault_Combobox_Options();
        }

        public double Public_TopConfig_WindowScaleFactor => this.Top_Config.WindowScaleFactor;

        public double Public_UlfConfig_WindowScaleFactor => this.ULF_Config.WindowScaleFactor;

        public double Public_UlfConfig_WindowCoefficientSum
        {
            get
            {
                double Local_Sum = 0d;
                foreach (var Local_Coefficient in this.ULF_Config.WindowCoefficients)
                    Local_Sum += Local_Coefficient;
                return Local_Sum;
            }
        }

        /// <summary>
        /// Analyses one ULF frame and returns the dBV magnitudes, so a test can check that a tone
        /// actually lands in the bin it should.
        /// </summary>
        public double[] Public_AnalyseUlfFrame(double[] frame)
        {
            var Local_Config = this.ULF_Config;
            var Local_Decimated = new double[Local_Config.FFTSize];

            var (_, Local_MagLog) = this.Compute_ULF_FFT_Data(
                Local_Config, Local_Config.InputFFT, Local_Config.InputDecimator, frame, Local_Decimated);

            return Local_MagLog;
        }

        public static (int From, int To) Public_Get_BinRangeForFrequencies(double[] span, double minHz, double maxHz)
            => Get_BinRangeForFrequencies(span, minHz, maxHz);

        //Methods, not properties: the WinForms analyzer requires designer-serialization attributes
        //on public settable properties of a Control, and these are test plumbing, not design data.
        public bool Public_Get_ULF_TickInFlight() => this.ULF_Tick_InFlight;

        public void Public_Set_ULF_TickInFlight(bool value) => this.ULF_Tick_InFlight = value;

        public bool Public_Get_Top_TickInFlight() => this.Top_Tick_InFlight;

        public void Public_Set_Top_TickInFlight(bool value) => this.Top_Tick_InFlight = value;

        public bool Public_Get_Waveform_TickInFlight() => this.Waveform_Tick_InFlight;

        //Each tick clears its task list early on, so a list that still holds its sentinel proves the
        //handler returned before doing any work.
        public int Public_ULF_TaskCount => this.ULF_FFT_Tasks.Count;

        public int Public_Top_TaskCount => this.Top_FFT_Tasks.Count;

        public void Public_SeedTaskLists()
        {
            this.ULF_FFT_Tasks.Add(System.Threading.Tasks.Task.CompletedTask);
            this.Top_FFT_Tasks.Add(System.Threading.Tasks.Task.CompletedTask);
        }

        public void Public_Invoke_ULF_Tick() => this.Plot_ULF_FFT_Timer_Tick(this, EventArgs.Empty);

        public void Public_Invoke_Top_Tick() => this.PlotTopFFTs_Timer_Tick(this, EventArgs.Empty);

        //Drives the real Pause handler, which is what re-arms the timers behind a running tick.
        public void Public_Invoke_PauseChanged() => this.Pause_CHK_CheckedChanged(this, EventArgs.Empty);

        //The snapshot type is protected, so the tests assert on its contents rather than on it.
        public int Public_TopConfig_FFTSize => this.Top_Config.FFTSize;

        public int Public_TopConfig_WindowLength => this.Top_Config.WindowCoefficients.Length;

        public int Public_TopConfig_FFTPointCount => this.Top_Config.InputFFT.PointCount;

        public int Public_TopConfig_SampleRate => this.Top_Config.SampleRate;

        public bool Public_TopConfig_FFTsAreDistinct
            => !ReferenceEquals(this.Top_Config.InputFFT, this.Top_Config.OutputFFT);

        public int Public_UlfConfig_FFTSize => this.ULF_Config.FFTSize;

        public int Public_UlfConfig_WindowLength => this.ULF_Config.WindowCoefficients.Length;

        public int Public_UlfConfig_FFTPointCount => this.ULF_Config.InputFFT.PointCount;

        public int Public_UlfConfig_FrequencySpanLength => this.ULF_Config.FrequencySpan.Length;

        public double Public_UlfConfig_FrequencyAt(int bin) => this.ULF_Config.FrequencySpan[bin];

        public bool Public_UlfConfig_DecimatorsAreDistinct
            => this.ULF_Config.InputDecimator != null
               && this.ULF_Config.OutputDecimator != null
               && !ReferenceEquals(this.ULF_Config.InputDecimator, this.ULF_Config.OutputDecimator);

        /// <summary>
        /// Runs one ULF frame exactly as the plot timer does, but against a snapshot captured
        /// BEFORE the caller is given a chance to change the settings - which is precisely the
        /// window in which the old loose fields tore.
        /// </summary>
        public int Public_AnalyseUlfFrameAcrossAConfigChange(double[] frame, Action between)
        {
            var Local_Config = this.ULF_Config;
            var Local_Decimated = new double[Local_Config.FFTSize];

            between();

            var (_, Local_MagLog) = this.Compute_ULF_FFT_Data(
                Local_Config, Local_Config.InputFFT, Local_Config.InputDecimator!, frame, Local_Decimated);

            return Local_MagLog.Length;
        }

        /// <summary>
        /// The same for the Top chart, which is the path that used to throw.
        /// </summary>
        public int Public_AnalyseTopFrameAcrossAConfigChange(Action between)
        {
            var Local_Config = this.Top_Config;
            var Local_Frame = new double[Local_Config.FFTSize];
            for (int i = 0; i < Local_Frame.Length; i++)
                Local_Frame[i] = Math.Sin(2d * Math.PI * 40d * i / Local_Frame.Length);

            between();

            var (_, Local_MagLog) = this.Compute_Top_FFT_Data(Local_Config, Local_Config.InputFFT, Local_Frame);
            return Local_MagLog.Length;
        }

        public void Public_Discard_BufferedAudio(int checkboxIndex) => this.Discard_BufferedAudio(checkboxIndex);

        /// <summary>
        /// The Y axis half-height this block would produce, straight out of the plot-data path.
        /// </summary>
        public double Public_BlockMagnitude(double[] audio)
        {
            var Local_PlotData = this.ComputeWaveformPlotData(audio, 1.5d);
            double Local_Magnitude = Local_PlotData.YMaximum;

            if (Local_PlotData.RentedXArray != null)
                System.Buffers.ArrayPool<double>.Shared.Return(Local_PlotData.RentedXArray);
            if (Local_PlotData.RentedYArray != null)
                System.Buffers.ArrayPool<decimal>.Shared.Return(Local_PlotData.RentedYArray);

            return Local_Magnitude;
        }
    }

    /// <summary>
    /// The half-height of a chart's Y axis, which is what the rolling range feature controls.
    /// </summary>
    private static double AxisMagnitude(Chart chart)
    {
        return chart.ChartAreas[0].AxisY.Maximum;
    }

    /// <summary>
    /// One ASIO block worth of a constant-amplitude tone. A real sub-bass signal gives wildly
    /// different per-block peaks depending on where the block lands in the cycle, which is exactly
    /// what a range that only looks at the current block gets wrong.
    /// </summary>
    private static double[] Block(int length, double amplitude)
    {
        var Local_Audio = new double[length];
        for (int i = 0; i < length; i++)
            Local_Audio[i] = amplitude * Math.Sin(2d * Math.PI * i / length);
        return Local_Audio;
    }

    /// <summary>
    /// Runs a body on an STA thread with the process-wide input sample rate pinned, restoring it
    /// afterwards - FormRTA reads Program.DSP_Info.InSampleRate and the suite shares that singleton.
    /// </summary>
    private static void WithSampleRate(int sampleRate, Action<TestableFormRTA> body)
    {
        int Local_SavedRate = Program.DSP_Info.InSampleRate;
        try
        {
            Program.DSP_Info.InSampleRate = sampleRate;
            StaTestRunner.Run(() =>
            {
                using var Local_Form = new TestableFormRTA();

                //A form that is never shown never raises Load, so without this the window type
                //combos stay empty and every snapshot is built from Build_Window's fallback.
                Local_Form.Public_InitialiseComboBoxes();

                body(Local_Form);
            });
        }
        finally
        {
            Program.DSP_Info.InSampleRate = Local_SavedRate;
        }
    }

    #endregion

    #region Instantiation

    [TestMethod]
    public void CanInstantiate_FormRTA()
    {
        StaTestRunner.Run(() =>
        {
            using var Local_Form = new FormRTA();
            Assert.IsNotNull(Local_Form);
        });
    }

    #endregion

    #region ULF Frame Length

    /// <summary>
    /// One second per frame, at every sample rate: that is what 1 Hz bins cost.
    /// </summary>
    [TestMethod]
    [DataRow(44100)]
    [DataRow(48000)]
    [DataRow(96000)]
    [DataRow(192000)]
    public void ULF_FrameLength_IsExactlyOneSecond(int sampleRate)
    {
        Assert.AreEqual(sampleRate, TestableFormRTA.Public_Get_ULF_FrameLength(sampleRate));
    }

    [TestMethod]
    public void ULF_FrameLength_IsZeroBeforeTheSampleRateIsKnown()
    {
        Assert.AreEqual(0, TestableFormRTA.Public_Get_ULF_FrameLength(0));
        Assert.AreEqual(0, TestableFormRTA.Public_Get_ULF_FrameLength(-1));
    }

    #endregion

    #region ULF Buffer Depth

    /// <summary>
    /// DEFECT: the store was ten seconds deep. A live analyser only needs the frame it is about to
    /// analyse plus a jitter cushion; everything else is latency the user watches.
    /// </summary>
    [TestMethod]
    [DataRow(44100, 88200)]
    [DataRow(48000, 96000)]
    [DataRow(96000, 192000)]
    public void ULF_BufferLength_IsTwoSecondsNotTen(int sampleRate, int expected)
    {
        Assert.AreEqual(expected, TestableFormRTA.Public_Get_ULF_BufferLength(sampleRate));
    }

    /// <summary>
    /// The buffer must always hold at least one whole frame, or the analyser could never plot.
    /// </summary>
    [TestMethod]
    [DataRow(44100)]
    [DataRow(48000)]
    [DataRow(96000)]
    [DataRow(192000)]
    public void ULF_BufferLength_AlwaysHoldsAtLeastOneFrame(int sampleRate)
    {
        int Local_Frame = TestableFormRTA.Public_Get_ULF_FrameLength(sampleRate);
        int Local_Capacity = TestableFormRTA.Public_Get_ULF_BufferLength(sampleRate);

        Assert.IsTrue(Local_Capacity >= Local_Frame,
            $"Capacity {Local_Capacity} cannot hold a {Local_Frame} sample frame");
    }

    [TestMethod]
    public void ULF_BufferLength_IsNeverZeroWhenTheSampleRateIsUnknown()
    {
        Assert.IsTrue(TestableFormRTA.Public_Get_ULF_BufferLength(0) >= 1);
        Assert.IsTrue(TestableFormRTA.Public_Get_ULF_BufferLength(-1) >= 1);
    }

    [TestMethod]
    public void Constructor_SizesTheULFBuffersToTwoSeconds()
    {
        WithSampleRate(48000, Local_Form =>
        {
            Assert.AreEqual(96000, Local_Form.Public_InputULFBuffer.MaxLength);
            Assert.AreEqual(96000, Local_Form.Public_OutputULFBuffer.MaxLength);
        });
    }

    /// <summary>
    /// The buffers are sized from the sample rate at construction, so a rate change made while the
    /// RTA window is open must re-size them - otherwise a rate increase leaves them permanently too
    /// small to hold a frame and the charts freeze.
    /// </summary>
    [TestMethod]
    public void EnsureULFBufferCapacity_ResizesWhenTheSampleRateChanges()
    {
        WithSampleRate(48000, Local_Form =>
        {
            Assert.AreEqual(96000, Local_Form.Public_InputULFBuffer.MaxLength);

            Local_Form.Public_EnsureULFBufferCapacity(96000);

            Assert.AreEqual(192000, Local_Form.Public_InputULFBuffer.MaxLength);
            Assert.AreEqual(192000, Local_Form.Public_OutputULFBuffer.MaxLength);
        });
    }

    [TestMethod]
    public void EnsureULFBufferCapacity_KeepsTheBufferedAudioWhenNothingChanged()
    {
        WithSampleRate(48000, Local_Form =>
        {
            var Local_Before = Local_Form.Public_InputULFBuffer;
            _ = Local_Before.Write(new double[1000], 0, 1000);

            Local_Form.Public_EnsureULFBufferCapacity(48000);

            Assert.AreSame(Local_Before, Local_Form.Public_InputULFBuffer, "No pointless re-allocation");
            Assert.AreEqual(1000, Local_Form.Public_InputULFBuffer.Count, "Buffered audio survives");
        });
    }

    #endregion

    #region Top FFT Buffer Depth

    /// <summary>
    /// The Top buffers were sized once, in the constructor, from the 2048 default. Selecting the
    /// largest offered size (16384) then left barely a frame of storage.
    /// </summary>
    [TestMethod]
    [DataRow(2048)]
    [DataRow(4096)]
    [DataRow(8192)]
    [DataRow(16384)]
    public void Top_BufferLength_HoldsSeveralFramesAtEveryOfferedFFTSize(int fftSize)
    {
        int Local_Capacity = TestableFormRTA.Public_Get_Top_BufferLength(fftSize);

        Assert.IsTrue(Local_Capacity >= fftSize * 2,
            $"Capacity {Local_Capacity} leaves no headroom over a {fftSize} sample frame");
    }

    [TestMethod]
    public void EnsureTopBufferCapacity_ResizesWhenTheFFTSizeChanges()
    {
        WithSampleRate(48000, Local_Form =>
        {
            int Local_Default = Local_Form.Public_Default_Top_FFTSize;
            Assert.AreEqual(TestableFormRTA.Public_Get_Top_BufferLength(Local_Default),
                Local_Form.Public_InputTopBuffer.MaxLength);

            Local_Form.Public_EnsureTopBufferCapacity(16384);

            Assert.AreEqual(TestableFormRTA.Public_Get_Top_BufferLength(16384),
                Local_Form.Public_InputTopBuffer.MaxLength);
            Assert.AreEqual(TestableFormRTA.Public_Get_Top_BufferLength(16384),
                Local_Form.Public_OutputTopBuffer.MaxLength);
        });
    }

    #endregion

    #region Overlap Hop

    /// <summary>
    /// The hop is what the analyser consumes per frame; the rest of the frame is the overlap.
    /// </summary>
    [TestMethod]
    [DataRow(0, 48000)]
    [DataRow(25, 36000)]
    [DataRow(50, 24000)]
    [DataRow(75, 12000)]
    [DataRow(90, 4800)]
    [DataRow(95, 2400)]
    public void HopLength_MatchesTheSelectedOverlap(int overlapPercentage, int expected)
    {
        Assert.AreEqual(expected, TestableFormRTA.Public_Get_HopLength(48000, overlapPercentage));
    }

    /// <summary>
    /// A hop of zero would consume nothing and re-analyse the same frame forever; a hop longer than
    /// the frame would skip audio. Both are clamped away.
    /// </summary>
    [TestMethod]
    public void HopLength_IsClampedToTheFrame()
    {
        Assert.AreEqual(48000, TestableFormRTA.Public_Get_HopLength(48000, -10), "Negative overlap");
        Assert.AreEqual(480, TestableFormRTA.Public_Get_HopLength(48000, 100), "100% overlap would stall, clamped to 99%");
        Assert.AreEqual(480, TestableFormRTA.Public_Get_HopLength(48000, 500), "Out of range overlap");
        Assert.AreEqual(1, TestableFormRTA.Public_Get_HopLength(10, 99), "A hop is never zero");
        Assert.AreEqual(0, TestableFormRTA.Public_Get_HopLength(0, 90), "No frame, no hop");
    }

    #endregion

    #region Frame Availability Guard

    /// <summary>
    /// DEFECT: the guard demanded Count > frame * (1 + overlap) - 1.9 seconds of ULF audio at the
    /// default 90% overlap - so the first plot was nearly a second late and the buffer permanently
    /// carried that extra second as latency. A frame needs a frame.
    /// </summary>
    [TestMethod]
    public void HasFullFrame_RequiresExactlyOneFrameAndNoMore()
    {
        const int Frame = 1000;
        var Local_Buffer = new CircularBuffer(Frame * 2);

        _ = Local_Buffer.Write(new double[Frame - 1], 0, Frame - 1);
        Assert.IsFalse(TestableFormRTA.Public_HasFullFrame(Local_Buffer, Frame), "One sample short");

        _ = Local_Buffer.Write(new double[1], 0, 1);
        Assert.IsTrue(TestableFormRTA.Public_HasFullFrame(Local_Buffer, Frame), "Exactly a frame is enough");
    }

    [TestMethod]
    public void HasFullFrame_IsFalseWhenTheFrameLengthIsUnknown()
    {
        var Local_Buffer = new CircularBuffer(1000);
        _ = Local_Buffer.Write(new double[1000], 0, 1000);

        Assert.IsFalse(TestableFormRTA.Public_HasFullFrame(Local_Buffer, 0));
        Assert.IsFalse(TestableFormRTA.Public_HasFullFrame(Local_Buffer, -1));
    }

    #endregion

    #region End To End Framing Latency

    /// <summary>
    /// Drives the real sizing rules through the real buffer with a real-time producer and asserts
    /// the user-visible number: the analyser must start plotting after about ONE second of audio,
    /// and the audio it analyses must never be more than about one second stale.
    /// </summary>
    [TestMethod]
    public void ULF_Framing_StartsAfterOneSecondAndStaysWithinOneSecondOfLive()
    {
        const int SampleRate = 48000;
        const int BlockSize = 256;

        int Local_Frame = TestableFormRTA.Public_Get_ULF_FrameLength(SampleRate);
        int Local_Hop = TestableFormRTA.Public_Get_HopLength(Local_Frame, 90);
        var Local_Buffer = new CircularBuffer(TestableFormRTA.Public_Get_ULF_BufferLength(SampleRate));
        var Local_FrameData = new double[Local_Frame];

        int Local_Produced = 0;
        int Local_FirstFrameAtSample = -1;
        double Local_WorstLag = 0;

        //Ten seconds of audio, one ASIO block at a time.
        int Local_TotalBlocks = (SampleRate * 10) / BlockSize;
        for (int Local_Block = 0; Local_Block < Local_TotalBlocks; Local_Block++)
        {
            var Local_Audio = new double[BlockSize];
            for (int i = 0; i < BlockSize; i++)
                Local_Audio[i] = Local_Produced + i;
            _ = Local_Buffer.Write(Local_Audio, 0, BlockSize);
            Local_Produced += BlockSize;

            //The plot timer ticks every 1 ms, far faster than audio arrives, so it drains greedily.
            while (TestableFormRTA.Public_HasFullFrame(Local_Buffer, Local_Frame))
            {
                _ = Local_Buffer.Peek(Local_FrameData, 0, Local_Frame);

                if (Local_FirstFrameAtSample < 0)
                    Local_FirstFrameAtSample = Local_Produced;

                double Local_Lag = (Local_Produced - 1) - Local_FrameData[Local_Frame - 1];
                if (Local_Lag > Local_WorstLag)
                    Local_WorstLag = Local_Lag;

                Local_Buffer.Advance(Local_Hop);
            }
        }

        Assert.IsTrue(Local_FirstFrameAtSample > 0, "The analyser never produced a frame");
        Assert.IsTrue(Local_FirstFrameAtSample <= Local_Frame + BlockSize,
            $"First frame plotted only after {Local_FirstFrameAtSample} samples " +
            $"({Local_FirstFrameAtSample / (double)SampleRate:0.00} s), expected about one second");

        Assert.IsTrue(Local_WorstLag <= Local_Frame,
            $"Analysed audio ran {Local_WorstLag / (double)SampleRate:0.00} s behind live, " +
            "expected no more than the one second analysis window");
    }

    #endregion

    #region Waveform Rolling Range

    /// <summary>
    /// Runs a body against a form whose handle (and therefore its charts' handles) exists, because
    /// UpdateChartWithPlotData deliberately does nothing until the chart is realised.
    /// </summary>
    private static void WithRealisedForm(Action<TestableFormRTA> body)
    {
        WithSampleRate(48000, Local_Form =>
        {
            //A form that is never shown does not realise its children, and the chart handles are
            //what UpdateChartWithPlotData checks before it will touch an axis.
            _ = Local_Form.Handle;
            _ = Local_Form.Public_InputWaveformChart.Handle;
            _ = Local_Form.Public_OutputWaveformChart.Handle;

            Assert.IsTrue(Local_Form.IsHandleCreated, "The form handle is needed to plot");
            Assert.IsTrue(Local_Form.Public_InputWaveformChart.IsHandleCreated,
                "The input waveform chart handle is needed to plot");

            body(Local_Form);
        });
    }

    /// <summary>
    /// DEFECT: every five seconds the Y axis was slammed to 0/0 and then re-grown from whatever
    /// single 512-sample block happened to arrive next. On a sub-bass signal a single block's peak
    /// can be a fraction of the waveform's real amplitude, so the range collapsed and then ratcheted
    /// back up - the "abruptly resets to like 0" the user reported.
    /// </summary>
    [TestMethod]
    public void WaveformRange_DoesNotCollapseWhenAQuietBlockFollowsALoudPassage()
    {
        WithRealisedForm(Local_Form =>
        {
            var Local_Chart = Local_Form.Public_InputWaveformChart;
            var Local_Start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            //Four seconds of a healthy signal.
            for (int Local_Step = 0; Local_Step < 40; Local_Step++)
                Local_Form.Public_PlotWaveform(Local_Chart, Block(512, 0.5d), Local_Start.AddMilliseconds(Local_Step * 100));

            double Local_Established = AxisMagnitude(Local_Chart);
            Assert.IsTrue(Local_Established > 0d, "The range never established itself");

            //One quiet block arriving just past the five second mark.
            Local_Form.Public_PlotWaveform(Local_Chart, Block(512, 0.004d), Local_Start.AddMilliseconds(5100));

            double Local_After = AxisMagnitude(Local_Chart);

            Assert.IsTrue(Local_After >= Local_Established * 0.5d,
                $"The Y axis collapsed from {Local_Established:0.0000} to {Local_After:0.0000} " +
                "on a single quiet block");
        });
    }

    /// <summary>
    /// The range must follow the signal UP immediately, or the chart clips the waveform.
    /// </summary>
    [TestMethod]
    public void WaveformRange_FollowsALouderSignalImmediately()
    {
        WithRealisedForm(Local_Form =>
        {
            var Local_Chart = Local_Form.Public_InputWaveformChart;
            var Local_Start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            Local_Form.Public_PlotWaveform(Local_Chart, Block(512, 0.05d), Local_Start);
            double Local_Quiet = AxisMagnitude(Local_Chart);

            Local_Form.Public_PlotWaveform(Local_Chart, Block(512, 0.5d), Local_Start.AddMilliseconds(100));
            double Local_Loud = AxisMagnitude(Local_Chart);

            Assert.IsTrue(Local_Loud > Local_Quiet * 5d,
                $"The axis stayed at {Local_Loud:0.0000} for a signal ten times louder than {Local_Quiet:0.0000}");
            Assert.IsTrue(Local_Loud >= 0.5d, "The axis must not clip the waveform it is plotting");
        });
    }

    /// <summary>
    /// It is a ROLLING range, not a ratchet: once the loud passage has aged out of the ten second
    /// window the axis must have come back down, smoothly, to suit the quiet signal.
    /// </summary>
    [TestMethod]
    public void WaveformRange_ComesBackDownAfterTheWindowHasPassed()
    {
        WithRealisedForm(Local_Form =>
        {
            var Local_Chart = Local_Form.Public_InputWaveformChart;
            var Local_Start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            Local_Form.Public_PlotWaveform(Local_Chart, Block(512, 0.8d), Local_Start);
            double Local_Loud = AxisMagnitude(Local_Chart);

            //Twenty seconds of a much quieter signal, plotted ten times a second.
            for (int Local_Step = 1; Local_Step <= 200; Local_Step++)
                Local_Form.Public_PlotWaveform(Local_Chart, Block(512, 0.02d), Local_Start.AddMilliseconds(Local_Step * 100));

            double Local_Settled = AxisMagnitude(Local_Chart);

            Assert.IsTrue(Local_Settled < Local_Loud * 0.25d,
                $"The axis stayed at {Local_Settled:0.0000} after the {Local_Loud:0.0000} peak " +
                "aged out of the rolling window");
            Assert.IsTrue(Local_Settled >= 0.02d, "It came down so far that it now clips the signal");
        });
    }

    /// <summary>
    /// Step by step, the range must never jump downwards by a large fraction in one plot - that
    /// discontinuity is the whole complaint.
    /// </summary>
    [TestMethod]
    public void WaveformRange_NeverStepsDownAbruptly()
    {
        WithRealisedForm(Local_Form =>
        {
            var Local_Chart = Local_Form.Public_InputWaveformChart;
            var Local_Start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            double Local_Previous = 0d;
            double Local_WorstDrop = 0d;
            int Local_WorstStep = -1;

            //Thirty seconds of a signal whose per-block peak swings the way a sub-bass tone's does.
            for (int Local_Step = 0; Local_Step < 300; Local_Step++)
            {
                double Local_Amplitude = 0.05d + (0.45d * Math.Abs(Math.Sin(Local_Step * 0.37d)));
                Local_Form.Public_PlotWaveform(Local_Chart, Block(512, Local_Amplitude),
                    Local_Start.AddMilliseconds(Local_Step * 100));

                double Local_Current = AxisMagnitude(Local_Chart);

                //A NaN maximum means the axis was left in auto mode: before the fix the range was
                //only ever assigned inside a "grow" comparison that NaN makes false, so a freshly
                //opened window had no range at all until the first five second reset fired.
                Assert.IsFalse(double.IsNaN(Local_Current),
                    $"Step {Local_Step}: the Y axis was never given a range");

                if (Local_Step > 0 && Local_Previous > 0d)
                {
                    double Local_Drop = (Local_Previous - Local_Current) / Local_Previous;
                    if (Local_Drop > Local_WorstDrop)
                    {
                        Local_WorstDrop = Local_Drop;
                        Local_WorstStep = Local_Step;
                    }
                }

                Local_Previous = Local_Current;
            }

            Assert.IsTrue(Local_WorstDrop <= 0.2d,
                $"The Y axis dropped {Local_WorstDrop * 100d:0.0}% in a single plot at step {Local_WorstStep}");
        });
    }

    #endregion

    #region Block Magnitude Symmetry

    /// <summary>
    /// DEFECT: the positive side was clamped with Math.Min(candidate, scale) but the negative side
    /// used Math.Min(candidate, -0.0001), which FLOORS rather than bounds. A negative overshoot
    /// therefore produced a far larger magnitude than the identical positive one - and with a ten
    /// second peak envelope in front of the axis, that mis-scaling is now held for ten seconds.
    /// </summary>
    [TestMethod]
    public void BlockMagnitude_IsTheSameForAPositiveAndANegativeOvershoot()
    {
        WithSampleRate(48000, Local_Form =>
        {
            var Local_Positive = new double[512];
            var Local_Negative = new double[512];
            Local_Positive[100] = 4.0d;
            Local_Negative[100] = -4.0d;

            double Local_PositiveMagnitude = Local_Form.Public_BlockMagnitude(Local_Positive);
            double Local_NegativeMagnitude = Local_Form.Public_BlockMagnitude(Local_Negative);

            Assert.AreEqual(Local_PositiveMagnitude, Local_NegativeMagnitude, 1e-12,
                $"A -4.0 sample gave {Local_NegativeMagnitude:0.0000} but +4.0 gave " +
                $"{Local_PositiveMagnitude:0.0000}");
        });
    }

    [TestMethod]
    [DataRow(0.5d, 0.75d)]
    [DataRow(1.0d, 1.5d)]
    [DataRow(4.0d, 1.5d)]
    [DataRow(-4.0d, 1.5d)]
    public void BlockMagnitude_IsScaledAndClamped(double sample, double expected)
    {
        WithSampleRate(48000, Local_Form =>
        {
            var Local_Audio = new double[512];
            Local_Audio[7] = sample;

            Assert.AreEqual(expected, Local_Form.Public_BlockMagnitude(Local_Audio), 1e-12);
        });
    }

    /// <summary>
    /// A non-finite sample used to throw OverflowException out of the (decimal) conversion, on a
    /// background thread, after both ArrayPool rentals and with no finally to return them.
    /// </summary>
    [TestMethod]
    public void BlockMagnitude_SurvivesNonFiniteSamples()
    {
        WithSampleRate(48000, Local_Form =>
        {
            foreach (double Local_Poison in new[] { double.NaN, double.PositiveInfinity, double.NegativeInfinity })
            {
                var Local_Audio = new double[512];
                Local_Audio[3] = Local_Poison;
                Local_Audio[4] = 0.4d;

                double Local_Magnitude = Local_Form.Public_BlockMagnitude(Local_Audio);

                Assert.IsTrue(double.IsFinite(Local_Magnitude), $"{Local_Poison} produced {Local_Magnitude}");
                Assert.AreEqual(0.6d, Local_Magnitude, 1e-12, "The finite content still sets the range");
            }
        });
    }

    #endregion

    #region Stale Audio On Chart Visibility

    /// <summary>
    /// Both the ASIO writes and the plot ticks are gated on chart.Visible, so a hidden chart's
    /// buffer freezes holding roughly a frame of audio. Re-checking the chart used to splice that
    /// stale audio onto live audio, overlapped frame by overlapped frame.
    /// </summary>
    [TestMethod]
    public void HidingAChart_DiscardsItsBufferedAudio()
    {
        WithSampleRate(48000, Local_Form =>
        {
            var Local_Buffers = new (int Index, Func<CircularBuffer> Get)[]
            {
                (2, () => Local_Form.Public_InputULFBuffer),
                (3, () => Local_Form.Public_OutputULFBuffer),
                (4, () => Local_Form.Public_InputTopBuffer),
                (5, () => Local_Form.Public_OutputTopBuffer),
            };

            foreach (var Local_Entry in Local_Buffers)
            {
                _ = Local_Entry.Get().Write(new double[5000], 0, 5000);
                Assert.AreEqual(5000, Local_Entry.Get().Count, $"Checkbox {Local_Entry.Index}: setup");

                Local_Form.Public_Discard_BufferedAudio(Local_Entry.Index);

                Assert.AreEqual(0, Local_Entry.Get().Count,
                    $"Checkbox {Local_Entry.Index} kept stale audio after being hidden");
                Assert.IsTrue(Local_Entry.Get().MaxLength > 0,
                    $"Checkbox {Local_Entry.Index} lost its capacity");
            }
        });
    }

    /// <summary>
    /// The waveform charts plot the raw ASIO snapshot and own no circular buffer, so hiding one
    /// must not disturb anything.
    /// </summary>
    [TestMethod]
    public void HidingAWaveformChart_TouchesNoBuffer()
    {
        WithSampleRate(48000, Local_Form =>
        {
            var Local_Before = Local_Form.Public_InputULFBuffer;
            _ = Local_Before.Write(new double[5000], 0, 5000);

            Local_Form.Public_Discard_BufferedAudio(0);
            Local_Form.Public_Discard_BufferedAudio(1);

            Assert.AreSame(Local_Before, Local_Form.Public_InputULFBuffer);
            Assert.AreEqual(5000, Local_Form.Public_InputULFBuffer.Count);
        });
    }

    #endregion

    #region Top FFT Recalculation

    /// <summary>
    /// ReCalculate_Top_FFT used to install a ten-frame buffer that contradicted
    /// Get_Top_BufferLength, so the very next tick threw it away and re-allocated - discarding
    /// buffered audio twice and swapping the instance under the ASIO producer an extra time.
    /// </summary>
    [TestMethod]
    public void ReCalculate_Top_FFT_LeavesTheBuffersAtTheirDeclaredCapacity()
    {
        WithSampleRate(48000, Local_Form =>
        {
            Local_Form.Public_ReCalculate_Top_FFT();

            int Local_Expected = TestableFormRTA.Public_Get_Top_BufferLength(Local_Form.Public_Default_Top_FFTSize);

            Assert.AreEqual(Local_Expected, Local_Form.Public_InputTopBuffer.MaxLength);
            Assert.AreEqual(Local_Expected, Local_Form.Public_OutputTopBuffer.MaxLength);
        });
    }

    #endregion

    #region Analysis Config Snapshot

    /// <summary>
    /// Everything an in-flight frame depends on has to agree. Before the snapshot these were five
    /// separate mutable fields, reassigned one at a time by a combo box handler that could run
    /// while a pool thread was half way through reading them.
    /// </summary>
    [TestMethod]
    [DataRow(2048)]
    [DataRow(4096)]
    [DataRow(8192)]
    [DataRow(16384)]
    public void TopConfig_IsInternallyConsistentAtEveryFFTSize(int fftSize)
    {
        WithSampleRate(48000, Local_Form =>
        {
            Local_Form.Public_Set_Top_FFTSize(fftSize);
            Local_Form.Public_ReCalculate_Top_FFT();

            Assert.AreEqual(fftSize, Local_Form.Public_TopConfig_FFTSize, "size");
            Assert.AreEqual(fftSize, Local_Form.Public_TopConfig_WindowLength, "window length");
            Assert.AreEqual(fftSize, Local_Form.Public_TopConfig_FFTPointCount, "FFT point count");
            Assert.AreEqual(48000, Local_Form.Public_TopConfig_SampleRate, "sample rate");
            Assert.IsTrue(Local_Form.Public_TopConfig_FFTsAreDistinct,
                "The two directions run concurrently and must not share one stateful FFT");
        });
    }

    [TestMethod]
    public void UlfConfig_IsInternallyConsistent()
    {
        WithSampleRate(96000, Local_Form =>
        {
            Local_Form.Public_ReCalculate_ULF_FFT();

            int Local_Size = Local_Form.Public_UlfConfig_FFTSize;

            Assert.AreEqual(Local_Size, Local_Form.Public_UlfConfig_WindowLength, "window length");
            Assert.AreEqual(Local_Size, Local_Form.Public_UlfConfig_FFTPointCount, "FFT point count");
            Assert.IsTrue(Local_Form.Public_UlfConfig_DecimatorsAreDistinct,
                "The decimator carries scratch, so the two directions must not share one");
        });
    }

    /// <summary>
    /// The whole point of the ULF chart: 1 Hz bins. The frequency span comes from the DECIMATED
    /// rate, which is the FFT size, so the bins must land on exact integers across 1-100 Hz.
    /// </summary>
    [TestMethod]
    public void UlfConfig_FrequencySpanIsExactlyOneHertzPerBin()
    {
        WithSampleRate(48000, Local_Form =>
        {
            Local_Form.Public_ReCalculate_ULF_FFT();

            Assert.IsTrue(Local_Form.Public_UlfConfig_FrequencySpanLength > 100,
                "The span must cover the whole plotted range");

            for (int Local_Bin = 1; Local_Bin <= 100; Local_Bin++)
                Assert.AreEqual(Local_Bin, Local_Form.Public_UlfConfig_FrequencyAt(Local_Bin), 1e-9d,
                    $"Bin {Local_Bin} is not at {Local_Bin} Hz");
        });
    }

    /// <summary>
    /// DEFECT: shrinking the Top FFT while a frame was in flight handed the OLD 16384 point FFT the
    /// NEW 2048 sample window. Both guards in Perform_FFT_Into passed and the transform then ran off
    /// the end of the input array - an IndexOutOfRangeException that reached the user as the modal
    /// "A fatal error has occured / Press Yes to abort the app" dialog.
    /// </summary>
    [TestMethod]
    public void ShrinkingTheTopFFTSizeMidFrameDoesNotThrow()
    {
        WithSampleRate(48000, Local_Form =>
        {
            Local_Form.Public_Set_Top_FFTSize(16384);
            Local_Form.Public_ReCalculate_Top_FFT();

            int Local_Bins = Local_Form.Public_AnalyseTopFrameAcrossAConfigChange(() =>
            {
                //Exactly what Top_FFT_Size_CBO_SelectedIndexChanged does, mid-flight.
                Local_Form.Public_Set_Top_FFTSize(2048);
                Local_Form.Public_ReCalculate_Top_FFT();
            });

            Assert.AreEqual((16384 / 2) + 1, Local_Bins,
                "The in-flight frame must finish under the configuration it started with");
        });
    }

    [TestMethod]
    public void GrowingTheTopFFTSizeMidFrameDoesNotThrow()
    {
        WithSampleRate(48000, Local_Form =>
        {
            Local_Form.Public_Set_Top_FFTSize(2048);
            Local_Form.Public_ReCalculate_Top_FFT();

            int Local_Bins = Local_Form.Public_AnalyseTopFrameAcrossAConfigChange(() =>
            {
                Local_Form.Public_Set_Top_FFTSize(16384);
                Local_Form.Public_ReCalculate_Top_FFT();
            });

            Assert.AreEqual((2048 / 2) + 1, Local_Bins);
        });
    }

    /// <summary>
    /// Pins the mechanism the snapshot exists to prevent: DSPLib's guards check that the input is
    /// no LONGER than the FFT and that it matches the window, so a SHORTER input slips through both
    /// and the transform then indexes past the end of it. Nothing in DSPLib will catch this for us.
    /// </summary>
    [TestMethod]
    public void DSPLib_ThrowsWhenAFrameIsShorterThanItsFFT()
    {
        var Local_FFT = new DSPLib.FFT(16384);
        var Local_ShortFrame = new double[2048];
        var Local_MatchingWindow = new double[2048];

        Assert.ThrowsExactly<IndexOutOfRangeException>(
            () => Local_FFT.Perform_FFT(Local_ShortFrame, Local_MatchingWindow));
    }

    /// <summary>
    /// A window type change is the same race with a quieter failure: the frame would have been
    /// scaled by one window's correction factor after being multiplied by another's coefficients.
    /// </summary>
    [TestMethod]
    public void ChangingTheUlfWindowMidFrameDoesNotTearTheFrame()
    {
        WithSampleRate(48000, Local_Form =>
        {
            var Local_Frame = new double[48000];
            for (int i = 0; i < Local_Frame.Length; i++)
                Local_Frame[i] = 0.5d * Math.Sin(2d * Math.PI * 40d * i / 48000d);

            int Local_Bins = Local_Form.Public_AnalyseUlfFrameAcrossAConfigChange(
                Local_Frame, Local_Form.Public_ReCalculate_ULF_FFT);

            Assert.IsTrue(Local_Bins > 100, $"The frame produced only {Local_Bins} bins");
        });
    }

    #endregion

    #region Real Window Coefficients

    /// <summary>
    /// Build_Window falls back when no window type is selected. These tests populate the combos the
    /// way RTA_Load does, so the snapshots carry REAL coefficients - without that every assertion
    /// about the analysis path would be measuring the fallback instead.
    /// </summary>
    [TestMethod]
    public void Configs_CarryRealWindowCoefficients()
    {
        WithSampleRate(48000, Local_Form =>
        {
            Assert.AreNotEqual(1d, Local_Form.Public_UlfConfig_WindowScaleFactor, 1e-9d,
                "The ULF snapshot is still using the unity fallback scale factor");
            Assert.AreNotEqual(1d, Local_Form.Public_TopConfig_WindowScaleFactor, 1e-9d,
                "The Top snapshot is still using the unity fallback scale factor");

            Assert.IsTrue(Local_Form.Public_UlfConfig_WindowCoefficientSum > 0d,
                "An all-zero window multiplies the whole frame away");
        });
    }

    /// <summary>
    /// The fallback itself must be usable rather than silent: an all-zero window would plot the
    /// noise floor for any snapshot built before the combos are populated.
    /// </summary>
    [TestMethod]
    public void WindowFallback_IsRectangularNotSilence()
    {
        int Local_SavedRate = Program.DSP_Info.InSampleRate;
        try
        {
            Program.DSP_Info.InSampleRate = 48000;
            StaTestRunner.Run(() =>
            {
                //Deliberately NOT initialised, so Build_Window takes its fallback path.
                using var Local_Form = new TestableFormRTA();

                Assert.IsTrue(Local_Form.Public_UlfConfig_WindowCoefficientSum > 0d,
                    "The fallback window is all zeros, which silences the whole frame");
            });
        }
        finally
        {
            Program.DSP_Info.InSampleRate = Local_SavedRate;
        }
    }

    /// <summary>
    /// End to end through the real analysis path: a 40 Hz tone must show up at 40 Hz, well above
    /// its neighbours. This is what proves the decimator, the window and the frequency span all
    /// agree, and it is the assertion that the all-zero fallback would have made meaningless.
    /// </summary>
    [TestMethod]
    public void A40HertzToneLandsInThe40HertzBin()
    {
        WithSampleRate(48000, Local_Form =>
        {
            var Local_Frame = new double[48000];
            for (int i = 0; i < Local_Frame.Length; i++)
                Local_Frame[i] = 0.5d * Math.Sin(2d * Math.PI * 40d * i / 48000d);

            var Local_MagLog = Local_Form.Public_AnalyseUlfFrame(Local_Frame);

            Assert.IsTrue(Local_MagLog.Length > 100, "The analysis produced no usable spectrum");

            int Local_Peak = 1;
            for (int Local_Bin = 1; Local_Bin <= 100; Local_Bin++)
            {
                if (Local_MagLog[Local_Bin] > Local_MagLog[Local_Peak])
                    Local_Peak = Local_Bin;
            }

            Assert.AreEqual(40, Local_Peak, $"The 40 Hz tone peaked at bin {Local_Peak}");

            //And it must stand clear of the floor a couple of bins away.
            Assert.IsTrue(Local_MagLog[40] > Local_MagLog[60] + 20d,
                $"40 Hz ({Local_MagLog[40]:0.0} dB) barely rose above 60 Hz ({Local_MagLog[60]:0.0} dB)");
        });
    }

    #endregion

    #region Tick Re-entrancy

    /// <summary>
    /// DEFECT: the ticks are 'async void' and their only mutual exclusion was disabling their own
    /// timer on entry. Pause_CHK_CheckedChanged writes Enabled directly from the UI thread, and the
    /// UI thread is back in the message pump while a tick sits on its await - so ticking Pause off
    /// and on during that window re-armed a 1 ms timer and delivered a SECOND concurrent tick into
    /// the same handler. Both would use the same stateful FFT instance, the same decimator scratch
    /// and the same reusable frame buffer, and both would Advance the circular buffer.
    /// </summary>
    [TestMethod]
    public void ASecondTickIsRefusedWhileTheFirstIsStillRunning()
    {
        WithSampleRate(48000, Local_Form =>
        {
            Local_Form.Public_SeedTaskLists();
            Assert.AreEqual(1, Local_Form.Public_ULF_TaskCount, "setup");
            Assert.AreEqual(1, Local_Form.Public_Top_TaskCount, "setup");

            //Stand in for a tick that is currently awaiting its analysis tasks.
            Local_Form.Public_Set_ULF_TickInFlight(true);
            Local_Form.Public_Set_Top_TickInFlight(true);

            Local_Form.Public_Invoke_ULF_Tick();
            Local_Form.Public_Invoke_Top_Tick();

            //Both handlers clear their task list before queueing anything, so an untouched list
            //proves each returned immediately instead of running a concurrent frame.
            Assert.AreEqual(1, Local_Form.Public_ULF_TaskCount,
                "The ULF tick ran while a previous one was still in flight");
            Assert.AreEqual(1, Local_Form.Public_Top_TaskCount,
                "The Top tick ran while a previous one was still in flight");

            //The latch belongs to the in-flight tick and must not have been disturbed.
            Assert.IsTrue(Local_Form.Public_Get_ULF_TickInFlight());
            Assert.IsTrue(Local_Form.Public_Get_Top_TickInFlight());
        });
    }

    /// <summary>
    /// And the latch must not leak: once a tick completes, the next one has to be allowed through.
    /// </summary>
    [TestMethod]
    public void TheLatchIsClearedWhenATickCompletes()
    {
        WithSampleRate(48000, Local_Form =>
        {
            Assert.IsFalse(Local_Form.Public_Get_ULF_TickInFlight(), "A fresh form must not be latched");
            Assert.IsFalse(Local_Form.Public_Get_Top_TickInFlight(), "A fresh form must not be latched");
            Assert.IsFalse(Local_Form.Public_Get_Waveform_TickInFlight(), "A fresh form must not be latched");

            Local_Form.Public_SeedTaskLists();

            //With no channels, no visible-and-fed charts and an empty buffer there is nothing to
            //analyse, so this runs to completion synchronously and must leave the latch clear.
            Local_Form.Public_Invoke_ULF_Tick();

            Assert.AreEqual(0, Local_Form.Public_ULF_TaskCount, "The tick should have cleared its list");
            Assert.IsFalse(Local_Form.Public_Get_ULF_TickInFlight(), "The latch was not released");
        });
    }

    /// <summary>
    /// Pausing and unpausing is the trigger, and it must remain harmless.
    /// </summary>
    [TestMethod]
    public void PauseTogglingDoesNotDisturbTheLatch()
    {
        WithSampleRate(48000, Local_Form =>
        {
            Local_Form.Public_Set_ULF_TickInFlight(true);
            Local_Form.Public_Set_Top_TickInFlight(true);
            Local_Form.Public_SeedTaskLists();

            //The real handler, which is what re-arms the timers behind a running tick.
            Local_Form.Public_Invoke_PauseChanged();

            Assert.IsTrue(Local_Form.Public_Get_ULF_TickInFlight(),
                "Pause must not clear a latch belonging to an in-flight tick");
            Assert.IsTrue(Local_Form.Public_Get_Top_TickInFlight(),
                "Pause must not clear a latch belonging to an in-flight tick");

            //And a tick delivered by that re-armed timer must still be refused.
            Local_Form.Public_Invoke_ULF_Tick();
            Assert.AreEqual(1, Local_Form.Public_ULF_TaskCount,
                "A tick got through after Pause re-armed the timer");
        });
    }

    #endregion

    #region Max And Min Readout Range

    /// <summary>
    /// DEFECT: Plot_FFT handed its axis limits, which are in HERTZ, to FindMaxPosition and
    /// FindMinPosition, which take BIN INDICES. On the Top charts the 20000 clamped to the whole
    /// spectrum, so the "Max:"/"Min:" titles could name a bin far below the 10 Hz the axis starts
    /// at - which is why a screenshot could read "Min: 0.0" on a chart beginning at 1 Hz.
    /// </summary>
    [TestMethod]
    public void BinRange_CoversOnlyTheDisplayedFrequencies()
    {
        //1 Hz per bin, as the ULF chart has.
        var Local_Span = new double[1025];
        for (int i = 0; i < Local_Span.Length; i++)
            Local_Span[i] = i;
        Local_Span[0] = 0.0001d;

        var (Local_From, Local_To) = TestableFormRTA.Public_Get_BinRangeForFrequencies(Local_Span, 1, 100);

        Assert.AreEqual(1, Local_From, "The DC bin is off the left of the axis and must be excluded");
        Assert.AreEqual(101, Local_To, "The range is half open and must include the 100 Hz bin");
    }

    [TestMethod]
    public void BinRange_ClampsToTheAvailableSpectrum()
    {
        //A coarse span, as a Top chart at a high sample rate has.
        var Local_Span = new double[1025];
        for (int i = 0; i < Local_Span.Length; i++)
            Local_Span[i] = i * 46.875d;
        Local_Span[0] = 0.0001d;

        var (Local_From, Local_To) = TestableFormRTA.Public_Get_BinRangeForFrequencies(Local_Span, 10, 20000);

        Assert.AreEqual(1, Local_From, "0.0001 Hz is below the 10 Hz axis minimum");
        Assert.IsTrue(Local_Span[Local_To - 1] <= 20000d, "The last bin searched is above the axis maximum");
        Assert.IsTrue(Local_To < Local_Span.Length, "The search ran past the displayed range");
    }

    [TestMethod]
    public void BinRange_IsEmptyWhenNothingIsOnScreen()
    {
        var Local_Span = new double[] { 0.0001d, 1d, 2d, 3d };

        var (Local_From, Local_To) = TestableFormRTA.Public_Get_BinRangeForFrequencies(Local_Span, 500, 20000);
        Assert.AreEqual(Local_From, Local_To, "No bin is on screen, so the range must be empty");

        var (Local_EmptyFrom, Local_EmptyTo) = TestableFormRTA.Public_Get_BinRangeForFrequencies(Array.Empty<double>(), 1, 100);
        Assert.AreEqual(0, Local_EmptyFrom);
        Assert.AreEqual(0, Local_EmptyTo);
    }

    #endregion

    #region Regression Pins

    /// <summary>
    /// The four RTA transports must be the real circular buffers, sized by the helpers above. This
    /// pins the field names the plot timers and the ASIO callbacks share.
    /// </summary>
    [TestMethod]
    public void RTA_BufferFields_ArePresentAndSized()
    {
        WithSampleRate(48000, Local_Form =>
        {
            foreach (var Local_Name in new[]
                     {
                         "RTA_InputULFBuffer", "RTA_OutputULFBuffer",
                         "RTA_InputTopBuffer", "RTA_OutputTopBuffer"
                     })
            {
                var Local_Field = typeof(FormRTA).GetField(Local_Name,
                    BindingFlags.Instance | BindingFlags.NonPublic);

                Assert.IsNotNull(Local_Field, $"{Local_Name} is missing");

                var Local_Buffer = Local_Field!.GetValue(Local_Form) as CircularBuffer;
                Assert.IsNotNull(Local_Buffer, $"{Local_Name} is not a CircularBuffer");
                Assert.IsTrue(Local_Buffer!.MaxLength > 0, $"{Local_Name} has no capacity");
            }
        });
    }

    #endregion
}
