#nullable enable

namespace Test_Project_1;

#region Usings
using BassThatHz_ASIO_DSP_Processor;
using System;
using System.Numerics;
#endregion

/// <summary>
/// GUARD SUITE for DSP\AntiAliasDecimator.cs.
///
/// This replaces DSPLib.DownSampler.downsample (Largest Triangle Three Buckets) on the RTA's ULF
/// analysis path. LTTB is a plot thinner: it emits the most visually extreme sample of each bucket,
/// at that sample's own position, with no band limiting. Handed to an FFT that means (a) everything
/// above the new Nyquist folds down into the 1-100 Hz display and (b) levels read high because
/// peaks were selected rather than averaged.
///
/// The tests below are written as measurements of those two properties rather than golden vectors,
/// and every one of them is run against LTTB as well so the improvement is pinned, not asserted.
/// </summary>
[TestClass]
public class Test_AntiAliasDecimator
{
    #region Helpers

    private const int SampleRate = 48000;
    private const int FrameLength = SampleRate;      // one second
    private const int TargetLength = 2048;           // 2048 Hz effective rate, 1 Hz bins

    /// <summary>
    /// One second of a unit-amplitude sine at the given frequency.
    /// </summary>
    private static double[] Tone(double frequencyHz, double amplitude = 1d, int length = FrameLength,
                                 int sampleRate = SampleRate)
    {
        var Local_Signal = new double[length];
        for (int i = 0; i < length; i++)
            Local_Signal[i] = amplitude * Math.Sin(2d * Math.PI * frequencyHz * i / sampleRate);
        return Local_Signal;
    }

    /// <summary>
    /// The amplitude present at a given bin of a decimated frame, measured by direct correlation so
    /// the test does not depend on the FFT under test elsewhere. The decimated frame is treated as
    /// <paramref name="decimated"/>.Length samples per second, matching how the ULF chart reads it.
    /// </summary>
    private static double AmplitudeAt(double[] decimated, double frequencyHz)
    {
        int Local_Length = decimated.Length;
        double Local_Real = 0d;
        double Local_Imaginary = 0d;

        for (int i = 0; i < Local_Length; i++)
        {
            double Local_Phase = 2d * Math.PI * frequencyHz * i / Local_Length;
            Local_Real += decimated[i] * Math.Cos(Local_Phase);
            Local_Imaginary += decimated[i] * Math.Sin(Local_Phase);
        }

        return 2d * new Complex(Local_Real, Local_Imaginary).Magnitude / Local_Length;
    }

    private static double[] Decimate(double[] input, int order = AntiAliasDecimator.DefaultOrder,
                                     int targetLength = TargetLength)
    {
        var Local_Output = new double[targetLength];
        new AntiAliasDecimator(order).Decimate(input, input.Length, Local_Output);
        return Local_Output;
    }

    private static double ToDb(double amplitude)
    {
        return 20d * Math.Log10(Math.Max(amplitude, 1e-30d));
    }

    #endregion

    #region Shape

    [TestMethod]
    public void FillsTheOutputCompletely()
    {
        var Local_Output = new double[TargetLength];

        //Sentinel-filled, so the finiteness check below actually proves every slot was written
        //rather than merely observing the zeros the allocator handed us.
        Array.Fill(Local_Output, double.NaN);

        new AntiAliasDecimator().Decimate(Tone(50d), FrameLength, Local_Output);

        for (int i = 0; i < Local_Output.Length; i++)
            Assert.IsTrue(double.IsFinite(Local_Output[i]), $"Sample {i} was never written");
    }

    [TestMethod]
    public void OrderIsClampedToAtLeastOne()
    {
        Assert.AreEqual(1, new AntiAliasDecimator(0).Order);
        Assert.AreEqual(1, new AntiAliasDecimator(-5).Order);
        Assert.AreEqual(AntiAliasDecimator.DefaultOrder, new AntiAliasDecimator().Order);
    }

    [TestMethod]
    public void EmptyInputProducesSilenceRatherThanThrowing()
    {
        var Local_Output = new double[8];
        Array.Fill(Local_Output, 1d);

        new AntiAliasDecimator().Decimate(Array.Empty<double>(), 0, Local_Output);

        foreach (var Local_Sample in Local_Output)
            Assert.AreEqual(0d, Local_Sample);
    }

    [TestMethod]
    public void ZeroLengthOutputLeavesTheInputAlone()
    {
        var Local_Input = Tone(50d, 0.5d, 16, SampleRate);
        var Local_Copy = (double[])Local_Input.Clone();

        new AntiAliasDecimator().Decimate(Local_Input, Local_Input.Length, Array.Empty<double>());

        CollectionAssert.AreEqual(Local_Copy, Local_Input, "The source frame must never be modified");
    }

    /// <summary>
    /// Below unity ratio there is nothing to band-limit, but the frame still has to be resampled -
    /// and genuinely interpolated, not smeared into a constant.
    /// </summary>
    [TestMethod]
    public void InputShorterThanTheTargetIsResampledNotDropped()
    {
        var Local_Input = new double[] { 0d, 1d, 2d, 3d };
        var Local_Output = new double[8];

        new AntiAliasDecimator().Decimate(Local_Input, Local_Input.Length, Local_Output);

        //Block centres at (k + 0.5) * 4/8 - 0.5 = k/2 - 0.25, clamped at both ends.
        var Local_Expected = new[] { 0d, 0.25d, 0.75d, 1.25d, 1.75d, 2.25d, 2.75d, 3d };
        for (int i = 0; i < Local_Expected.Length; i++)
            Assert.AreEqual(Local_Expected[i], Local_Output[i], 1e-12d, $"Sample {i}");

        //And strictly increasing, so a resampler that emitted one repeated value cannot pass.
        for (int i = 1; i < Local_Output.Length; i++)
            Assert.IsTrue(Local_Output[i] > Local_Output[i - 1],
                $"Sample {i} ({Local_Output[i]}) did not advance past {Local_Output[i - 1]}");
    }

    /// <summary>
    /// A decimation ratio just above unity used to round the kernel width down to 1, which is the
    /// identity filter - no band limiting at all, i.e. exactly the failure this class replaces.
    /// </summary>
    [TestMethod]
    public void RatiosJustAboveUnityStillBandLimit()
    {
        //2400 samples into 2048 is a ratio of 1.17.
        const int Local_Source = 2400;
        const int Local_Target = 2048;

        //Alternating +/-1 is the highest frequency the source rate can carry; any real low-pass
        //must crush it, an identity filter passes it straight through.
        var Local_Input = new double[Local_Source];
        for (int i = 0; i < Local_Source; i++)
            Local_Input[i] = (i % 2 == 0) ? 1d : -1d;

        var Local_Output = new double[Local_Target];
        new AntiAliasDecimator().Decimate(Local_Input, Local_Source, Local_Output);

        double Local_Peak = 0d;
        foreach (var Local_Sample in Local_Output)
            Local_Peak = Math.Max(Local_Peak, Math.Abs(Local_Sample));

        Assert.IsTrue(Local_Peak < 0.5d,
            $"Nyquist-rate content survived at {Local_Peak:0.000}, so no band limiting was applied");
    }

    [TestMethod]
    public void NonFiniteSamplesAreTreatedAsSilenceRatherThanPoisoningTheFrame()
    {
        var Local_Input = Tone(50d);
        Local_Input[1000] = double.NaN;
        Local_Input[2000] = double.PositiveInfinity;

        var Local_Output = Decimate(Local_Input);

        foreach (var Local_Sample in Local_Output)
            Assert.IsTrue(double.IsFinite(Local_Sample), "A single bad sample destroyed the frame");
    }

    #endregion

    #region Amplitude Accuracy

    /// <summary>
    /// A peak picker reports the extreme of each bucket, so it reads high. An average reports the
    /// signal, so the chart's dBV readout means what it says.
    /// </summary>
    [TestMethod]
    public void PreservesTheAmplitudeOfAnInBandTone()
    {
        foreach (double Local_Frequency in new[] { 1d, 5d, 20d, 50d, 100d })
        {
            var Local_Decimated = Decimate(Tone(Local_Frequency, 0.5d));
            double Local_Amplitude = AmplitudeAt(Local_Decimated, Local_Frequency);

            Assert.AreEqual(0.5d, Local_Amplitude, 0.005d,
                $"{Local_Frequency} Hz came out at {Local_Amplitude:0.0000} instead of 0.5000");
        }
    }

    /// <summary>
    /// The pass band the ULF chart actually plots is 1-100 Hz; the cascaded average must be flat
    /// across it, or the displayed spectrum tilts.
    /// </summary>
    [TestMethod]
    public void PassBandIsFlatAcrossThePlottedSpan()
    {
        double Local_Reference = AmplitudeAt(Decimate(Tone(1d)), 1d);

        foreach (double Local_Frequency in new[] { 10d, 25d, 50d, 75d, 100d })
        {
            double Local_Amplitude = AmplitudeAt(Decimate(Tone(Local_Frequency)), Local_Frequency);
            double Local_Droop = ToDb(Local_Amplitude) - ToDb(Local_Reference);

            Assert.IsTrue(Math.Abs(Local_Droop) < 0.1d,
                $"{Local_Frequency} Hz is {Local_Droop:0.000} dB off the 1 Hz reference");
        }
    }

    [TestMethod]
    public void PreservesADcOffset()
    {
        var Local_Input = new double[FrameLength];
        Array.Fill(Local_Input, 0.25d);

        var Local_Decimated = Decimate(Local_Input);

        foreach (var Local_Sample in Local_Decimated)
            Assert.AreEqual(0.25d, Local_Sample, 1e-9d, "Unity DC gain is required for level accuracy");
    }

    #endregion

    #region Alias Rejection

    /// <summary>
    /// THE test. At 48 kHz decimated to 2048 points, a 1998 Hz tone is exactly 50 Hz below the
    /// output sample rate, so without band limiting it folds onto 50 Hz - right in the middle of the
    /// plotted span - at full strength. This is what made the ULF chart untrustworthy.
    /// </summary>
    [TestMethod]
    public void RejectsAToneThatWouldFoldIntoThePlottedSpan()
    {
        var Local_Decimated = Decimate(Tone(1998d));
        double Local_Alias = AmplitudeAt(Local_Decimated, 50d);

        Assert.IsTrue(ToDb(Local_Alias) < -40d,
            $"A 1998 Hz tone folded onto 50 Hz at {ToDb(Local_Alias):0.0} dBFS, expected below -40 dB");
    }

    [TestMethod]
    public void RejectsFoldDownFromSeveralMultiplesOfTheOutputRate()
    {
        //Each of these sits 30 Hz below a multiple of the 2048 Hz output rate, so each would land
        //on 30 Hz if nothing band-limited it.
        foreach (double Local_Frequency in new[] { 2018d, 4066d, 6114d, 8162d })
        {
            var Local_Decimated = Decimate(Tone(Local_Frequency));
            double Local_Alias = AmplitudeAt(Local_Decimated, 30d);

            Assert.IsTrue(ToDb(Local_Alias) < -40d,
                $"{Local_Frequency} Hz folded onto 30 Hz at {ToDb(Local_Alias):0.0} dBFS");
        }
    }

    /// <summary>
    /// Broadband content above the output Nyquist must not raise the noise floor of the plotted
    /// span. This is the real-world case - ordinary programme material has most of its energy above
    /// 1 kHz - and it is where plot thinning did the most damage, dumping a roughly -24 dBFS floor
    /// straight into the 1-100 Hz display.
    /// </summary>
    [TestMethod]
    public void HighFrequencyContentDoesNotRaiseTheLowFrequencyFloor()
    {
        var Local_Noise = HighFrequencyNoise();
        double Local_Worst = WorstAmplitudeInPlottedSpan(Decimate(Local_Noise));

        Assert.IsTrue(ToDb(Local_Worst) < -50d,
            $"High frequency noise showed up in the 1-100 Hz span at {ToDb(Local_Worst):0.0} dBFS");
    }

    /// <summary>
    /// White noise high-passed by simple differencing, so essentially all of its energy sits well
    /// above the 1024 Hz output Nyquist and can only reach the display by aliasing.
    /// </summary>
    private static double[] HighFrequencyNoise()
    {
        var Local_Random = new Random(20260816);
        var Local_Noise = new double[FrameLength];

        double Local_Previous = 0d;
        for (int i = 0; i < FrameLength; i++)
        {
            double Local_Sample = (Local_Random.NextDouble() * 2d) - 1d;
            Local_Noise[i] = (Local_Sample - Local_Previous) * 0.5d;
            Local_Previous = Local_Sample;
        }

        return Local_Noise;
    }

    private static double WorstAmplitudeInPlottedSpan(double[] decimated)
    {
        double Local_Worst = 0d;
        for (double Local_Frequency = 1d; Local_Frequency <= 100d; Local_Frequency += 1d)
            Local_Worst = Math.Max(Local_Worst, AmplitudeAt(decimated, Local_Frequency));
        return Local_Worst;
    }

    [TestMethod]
    public void HigherOrderRejectsMore()
    {
        double Local_Order1 = ToDb(AmplitudeAt(Decimate(Tone(1998d), order: 1), 50d));
        double Local_Order2 = ToDb(AmplitudeAt(Decimate(Tone(1998d), order: 2), 50d));
        double Local_Order3 = ToDb(AmplitudeAt(Decimate(Tone(1998d), order: 3), 50d));

        Assert.IsTrue(Local_Order2 < Local_Order1 - 10d,
            $"Order 2 ({Local_Order2:0.0} dB) barely improved on order 1 ({Local_Order1:0.0} dB)");
        Assert.IsTrue(Local_Order3 < Local_Order2 - 10d,
            $"Order 3 ({Local_Order3:0.0} dB) barely improved on order 2 ({Local_Order2:0.0} dB)");
    }

    #endregion

    #region Comparison With The Plot Thinner It Replaces

    /// <summary>
    /// Pins the actual reason for the change: LTTB passes the folded tone through essentially
    /// unattenuated, because it band-limits nothing at all.
    /// </summary>
    [TestMethod]
    public void BeatsTheLargestTriangleThinnerOnAliasRejection()
    {
        var Local_Input = Tone(1998d);

        double Local_Thinned = ToDb(AmplitudeAt(DSPLib.DownSampler.downsample(Local_Input, TargetLength), 50d));
        double Local_Filtered = ToDb(AmplitudeAt(Decimate(Local_Input), 50d));

        //Measured at the time of the change: -22.7 dB thinned against -54.0 dB filtered at 48 kHz.
        //Worst case across all supported rates and both sides of the first three fold frequencies
        //is -51.7 dB filtered.
        Assert.IsTrue(Local_Filtered < Local_Thinned - 25d,
            $"Anti-aliased decimation ({Local_Filtered:0.0} dB) must beat plot thinning " +
            $"({Local_Thinned:0.0} dB) by a wide margin at the fold frequency");
    }

    /// <summary>
    /// The case that actually matters for music: plot thinning let high frequency programme
    /// material sit in the 1-100 Hz display at about -24 dBFS, because picking bucket extrema from
    /// unfiltered audio folds that energy straight down. Measured at the time of the change:
    /// -23.8 dB thinned against -81.7 dB filtered.
    /// </summary>
    [TestMethod]
    public void BeatsTheLargestTriangleThinnerOnTheNoiseFloorItLeavesInTheDisplay()
    {
        var Local_Noise = HighFrequencyNoise();

        double Local_Thinned = ToDb(WorstAmplitudeInPlottedSpan(
            DSPLib.DownSampler.downsample(Local_Noise, TargetLength)));
        double Local_Filtered = ToDb(WorstAmplitudeInPlottedSpan(Decimate(Local_Noise)));

        Assert.IsTrue(Local_Filtered < Local_Thinned - 30d,
            $"Plot thinning left a {Local_Thinned:0.0} dB floor and filtering left {Local_Filtered:0.0} dB");
    }

    #endregion

    #region Sample Rates

    /// <summary>
    /// The RTA runs at whatever the interface is set to, so the kernel width has to adapt.
    /// </summary>
    [TestMethod]
    [DataRow(44100)]
    [DataRow(48000)]
    [DataRow(88200)]
    [DataRow(96000)]
    [DataRow(176400)]
    [DataRow(192000)]
    public void WorksAtEverySupportedSampleRate(int sampleRate)
    {
        var Local_Output = new double[TargetLength];
        new AntiAliasDecimator().Decimate(Tone(50d, 0.5d, sampleRate, sampleRate), sampleRate, Local_Output);

        double Local_Amplitude = AmplitudeAt(Local_Output, 50d);
        Assert.AreEqual(0.5d, Local_Amplitude, 0.01d, $"{sampleRate} Hz: level");
    }

    /// <summary>
    /// The kernel is one output sample period wide, so its null sits near the output rate. Probing
    /// only BELOW that null flatters the filter: the kernel width is an odd integer and therefore
    /// cannot land exactly on the rate, so the null is detuned one way or the other and the
    /// rejection is asymmetric about it. Both sides have to be checked at every rate.
    /// </summary>
    [TestMethod]
    [DataRow(44100)]
    [DataRow(48000)]
    [DataRow(88200)]
    [DataRow(96000)]
    [DataRow(176400)]
    [DataRow(192000)]
    public void RejectsFoldDownFromBothSidesOfTheOutputRateAtEverySampleRate(int sampleRate)
    {
        var Local_Output = new double[TargetLength];

        //1998 Hz is 50 Hz below the 2048 Hz output rate and 2098 Hz is 50 Hz above it; both fold
        //onto 50 Hz, in the middle of the plotted span.
        foreach (double Local_Frequency in new[] { 1998d, 2098d })
        {
            new AntiAliasDecimator().Decimate(Tone(Local_Frequency, 1d, sampleRate, sampleRate), sampleRate, Local_Output);
            double Local_Alias = ToDb(AmplitudeAt(Local_Output, 50d));

            Assert.IsTrue(Local_Alias < -35d,
                $"{sampleRate} Hz: {Local_Frequency} Hz folded onto 50 Hz at {Local_Alias:0.0} dB");
        }
    }

    #endregion

    #region Reuse

    /// <summary>
    /// The instance keeps scratch between calls; consecutive frames must not bleed into each other.
    /// </summary>
    [TestMethod]
    public void ReusingAnInstanceGivesTheSameResultAsAFreshOne()
    {
        var Local_Shared = new AntiAliasDecimator();
        var Local_First = new double[TargetLength];
        var Local_Second = new double[TargetLength];

        Local_Shared.Decimate(Tone(700d), FrameLength, Local_First);
        Local_Shared.Decimate(Tone(50d, 0.5d), FrameLength, Local_Second);

        var Local_Fresh = Decimate(Tone(50d, 0.5d));

        CollectionAssert.AreEqual(Local_Fresh, Local_Second, "State leaked between frames");
    }

    [TestMethod]
    public void HandlesAShorterFrameAfterALongerOne()
    {
        var Local_Shared = new AntiAliasDecimator();
        var Local_Output = new double[TargetLength];

        Local_Shared.Decimate(Tone(50d, 0.5d, 96000, 96000), 96000, Local_Output);
        Local_Shared.Decimate(Tone(50d, 0.5d), FrameLength, Local_Output);

        Assert.AreEqual(0.5d, AmplitudeAt(Local_Output, 50d), 0.01d);
    }

    #endregion
}
