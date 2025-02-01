#nullable enable

namespace Test_Project_1;

#region Usings
using BassThatHz_ASIO_DSP_Processor;
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


        //Run Test
        var StartTime = DateTime.Now;       

        PolarityFilter.Positive = false;
        OutputAudioData = Filter.Transform(InputAudioData, DSPStream);

        var TimeSpan = DateTime.Now - StartTime;

        //Assert Under 5ms performance
        Assert.IsTrue(TimeSpan.TotalNanoseconds < 5000000);
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