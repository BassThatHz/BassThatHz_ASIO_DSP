namespace Test_Project_1;

using BassThatHz_ASIO_DSP_Processor;

[TestClass]
public class Test_ClassicLimiter
{
    protected void InitData(double[] input)
    {
        for (int i = 0; i < input.Length; i++)
        {
            input[i] = 1;
        }
    }

    [TestMethod]
    public void Test_ClassicLimiterFilter_IsFast()
    {
        //Init Test structures
        DSP_Stream DSPStream = new();
        ClassicLimiter PolarityFilter = new();

        var InputAudioData = new double[512];
        var OutputAudioData = new double[512];
        IFilter Filter = PolarityFilter;

        //Init Test Data
        this.InitData(InputAudioData);

        //Run Test
        var StartTime = DateTime.Now;

        OutputAudioData = Filter.Transform(InputAudioData, DSPStream);

        var TimeSpan = DateTime.Now - StartTime;

        //Assert Under 5ms performance
        Assert.IsTrue(TimeSpan.TotalNanoseconds < 5000000);
    }

    [TestMethod]
    public void TestMethod1()
    {
        throw new NotImplementedException();
    }
}