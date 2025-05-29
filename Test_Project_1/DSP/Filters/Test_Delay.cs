namespace Test_Project_1;

using BassThatHz_ASIO_DSP_Processor;
using System.Diagnostics;

[TestClass]
public class Test_Delay
{
    protected void InitData(double[] input)
    {
        for (int i = 0; i < input.Length; i++)
        {
            input[i] = 1;
        }
    }

    [TestMethod]
    public void Test_DelayFilter_IsFast()
    {
        //Init Test structures
        DSP_Stream DSPStream = new();
        Delay PolarityFilter = new();

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
    public void Delay_DefaultValues_AreCorrect()
    {
        var filter = new Delay();
        Assert.IsFalse(filter.FilterEnabled);
        Assert.AreEqual(FilterTypes.Delay, filter.FilterType);
        Assert.AreEqual(FilterProcessingTypes.WholeBlock, filter.FilterProcessingType);
        Assert.IsNotNull(filter.GetFilter);
    }

    [TestMethod]
    public void Delay_Transform_WorksWithNoBuffer()
    {
        var filter = new Delay();
        var stream = new DSP_Stream();
        var input = new double[] { 1, 2, 3 };
        var output = filter.Transform(input, stream);
        Assert.AreEqual(input.Length, output.Length);
    }

    [TestMethod]
    public void Delay_Initialize_SetsUpBuffer()
    {
        var filter = new Delay();
        filter.Initialize(10, 5, 1000);
        // Use reflection to check DelayBuffer
        var buffer = (double[])typeof(Delay).GetField("DelayBuffer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(filter)!;
        Assert.IsNotNull(buffer);
        Assert.IsTrue(buffer.Length > 0);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentOutOfRangeException))]
    public void Delay_Throws_OnNegativeDelay()
    {
        var filter = new Delay();
        filter.DelayInMS = -1;
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentOutOfRangeException))]
    public void Delay_Throws_OnNegativeSampleRate()
    {
        var filter = new Delay();
        filter.ResetSampleRate(-1);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentOutOfRangeException))]
    public void Delay_Throws_OnNegativeBufferSize()
    {
        var filter = new Delay();
        filter.ResetBufferSize(-1);
    }
}