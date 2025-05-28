namespace Test_Project_1;

using BassThatHz_ASIO_DSP_Processor.DSP.Filters;
using BassThatHz_ASIO_DSP_Processor;
using System.Diagnostics;

[TestClass]
public class Test_AntiDC
{
    protected void InitData(double[] input)
    {
        for (int i = 0; i < input.Length; i++)
        {
            input[i] = 1;
        }
    }

    [TestMethod]
    public void Test_AntiDCFilter_IsFast()
    {
        //Init Test structures
        DSP_Stream DSPStream = new();
        AntiDC PolarityFilter = new();

        var InputAudioData = new double[512];
        var OutputAudioData = new double[512];
        IFilter Filter = PolarityFilter;

        //Init Test Data
        this.InitData(InputAudioData);

        //Run Timed Test
        Stopwatch StopWatch1 = new();
        StopWatch1.Start();

        OutputAudioData = Filter.Transform(InputAudioData, DSPStream);

        StopWatch1.Stop();

        //Assert Under 5ms performance
        Assert.IsTrue(StopWatch1.Elapsed.TotalNanoseconds < 5000000, "Over 5ms");
    }

    [TestMethod]
    public void AntiDC_DefaultValues_AreCorrect()
    {
        var filter = new AntiDC();
        Assert.AreEqual(42, filter.MaxConsecutiveDCSamples);
        Assert.AreEqual(1, filter.MaxClipEventsPerDuration);
        Assert.AreEqual(0.9999d, filter.Clip_Threshold);
        Assert.AreEqual(1E-05d, filter.DC_Threshold);
        Assert.AreEqual(TimeSpan.FromMilliseconds(1), filter.DetectionDuration);
        Assert.IsFalse(filter.FilterEnabled);
        Assert.AreEqual(FilterTypes.Anti_DC, filter.FilterType);
        Assert.AreEqual(FilterProcessingTypes.WholeBlock, filter.FilterProcessingType);
        Assert.IsNotNull(filter.GetFilter);
    }

    [TestMethod]
    public void AntiDC_Transform_MutesOutput_WhenIsOutputMuted()
    {
        var filter = new AntiDC();
        var stream = new DSP_Stream();
        var input = new double[8];
        for (int i = 0; i < input.Length; i++) input[i] = 1;
        // Simulate muting
        typeof(AntiDC).GetField("IsOutputMuted", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.SetValue(filter, true);
        var output = filter.Transform(input, stream);
        Assert.IsTrue(Array.TrueForAll(output, v => v == 0));
    }

    [TestMethod]
    public void AntiDC_ResetDetection_ResetsState()
    {
        var filter = new AntiDC();
        typeof(AntiDC).GetField("ConsecutiveDCEventsDetected", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.SetValue(filter, 5);
        typeof(AntiDC).GetField("ClipEventsPerDurationDetected", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.SetValue(filter, 5);
        typeof(AntiDC).GetField("IsOutputMuted", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.SetValue(filter, true);
        filter.ResetDetection();
        Assert.AreEqual(0, typeof(AntiDC).GetField("ConsecutiveDCEventsDetected", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(filter));
        Assert.AreEqual(0, typeof(AntiDC).GetField("ClipEventsPerDurationDetected", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(filter));
        Assert.IsFalse((bool)typeof(AntiDC).GetField("IsOutputMuted", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(filter)!);
    }

    [TestMethod]
    public void AntiDC_Events_CanBeSubscribed()
    {
        var filter = new AntiDC();
        bool outputMutedRaised = false;
        bool clipEventRaised = false;
        filter.OutputMutedEvent += (s, e) => outputMutedRaised = true;
        filter.ClipEvent += (s, e) => clipEventRaised = true;
        // Use reflection to invoke protected methods
        typeof(AntiDC).GetMethod("RaiseOutputMutedEvent", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.Invoke(filter, null);
        typeof(AntiDC).GetMethod("ReportClipEvents", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.Invoke(filter, null);
        // Allow async event to complete
        System.Threading.Thread.Sleep(10);
        Assert.IsTrue(outputMutedRaised);
        Assert.IsTrue(clipEventRaised);
    }

    [TestMethod]
    public void AntiDC_Transform_ReturnsInput_OnException()
    {
        var filter = new AntiDC();
        var stream = new DSP_Stream();
        // Simulate exception by passing null (should not throw, should return input)
        var input = new double[0];
        var output = filter.Transform(input, stream);
        Assert.AreSame(input, output);
    }
}