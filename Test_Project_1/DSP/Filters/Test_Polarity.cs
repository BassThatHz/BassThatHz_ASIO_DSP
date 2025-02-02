#nullable enable

namespace Test_Project_1;

#region Usings
using BassThatHz_ASIO_DSP_Processor;
using System.Diagnostics;
#endregion

[TestClass]
public class Test_Polarity
{
    [TestMethod]
    public void Test_PolarityFilter_IsFast()
    {
        //Init Test structures
        DSP_Stream DSPStream = new();
        Polarity PolarityFilter = new();

        var InputAudioData = new double[512];
        var OutputAudioData = new double[512];
        IFilter Filter = PolarityFilter;

        //Init Test Data
        this.InitData(InputAudioData);


        //Run Timed Test
        Stopwatch StopWatch1 = new();
        StopWatch1.Start();

        PolarityFilter.Positive = false;
        OutputAudioData = Filter.Transform(InputAudioData, DSPStream);

        StopWatch1.Stop();

        //Assert Under 5ms performance
        Assert.IsTrue(StopWatch1.Elapsed.TotalNanoseconds < 5000000, "Over 5ms");
    }

    [TestMethod]
    public void Test_PolarityFilter_Transform_PolarityFlip()
    {
        //Init Test structures
        DSP_Stream DSPStream = new();
        Polarity PolarityFilter = new();

        var InputAudioData = new double[512];
        var OutputAudioData = new double[512];
        IFilter Filter = PolarityFilter;
        
        //Init Test Data
        this.InitData(InputAudioData);

        //Run negative Polarity Test
        PolarityFilter.Positive = false;
        OutputAudioData = Filter.Transform(InputAudioData, DSPStream);
        //Assert Results
        Assert.IsTrue(OutputAudioData.Length == 512);
        for (int i = 0; i < OutputAudioData.Length; i++)
        {
            Assert.IsTrue(OutputAudioData[i] == -1);
        }

        //Run double-negative Polarity Test
        OutputAudioData = Filter.Transform(OutputAudioData, DSPStream);
        //Assert Results
        Assert.IsTrue(OutputAudioData.Length == 512);
        for (int i = 0; i < OutputAudioData.Length; i++)
        {
            Assert.IsTrue(OutputAudioData[i] == 1);
        }
    }

    protected void InitData(double[] input)
    {
        for (int i = 0; i < input.Length; i++)
        {
            input[i] = 1;
        }
    }
}