namespace Test_Project_1;

using BassThatHz_ASIO_DSP_Processor;
using System.Diagnostics;

[TestClass]
public class Test_DynamicRangeCompressor
{
    protected void InitData(double[] input)
    {
        for (int i = 0; i < input.Length; i++)
        {
            input[i] = 1;
        }
    }

    [TestMethod]
    public void Test_DynamicRangeCompressorFilter_IsFast()
    {
        //Init Test structures
        DSP_Stream DSPStream = new();
        DynamicRangeCompressor PolarityFilter = new();

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
    public void TestMethod1()
    {
        throw new NotImplementedException();
    }
}