namespace Test_Project_1;

using BassThatHz_ASIO_DSP_Processor;
using BassThatHz_ASIO_DSP_Processor.DSP.Filters;
using System.Diagnostics;

[TestClass]
public class Test_AuxSet
{
    protected void InitData(double[] input)
    {
        for (int i = 0; i < input.Length; i++)
        {
            input[i] = 1;
        }
    }

    [TestMethod]
    public void Test_AuxSetFilter_IsFast()
    {
        //Init Test structures
        DSP_Stream DSPStream = new();
        AuxSet PolarityFilter = new();

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