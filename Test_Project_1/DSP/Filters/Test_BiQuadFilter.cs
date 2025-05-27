namespace Test_Project_1;

using BassThatHz_ASIO_DSP_Processor;
using NAudio.Dsp;
using System.Diagnostics;

[TestClass]
public class Test_BiQuadFilter
{
    protected void InitData(double[] input)
    {
        for (int i = 0; i < input.Length; i++)
        {
            input[i] = 1;
        }
    }

    [TestMethod]
    public void Test_BiQuadFilter_IsFast()
    {
        //Init Test structures
        DSP_Stream DSPStream = new();
        BiQuadFilter PolarityFilter = new();

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

    [TestMethod]
    public void BiQuadFilter_DefaultValues_AreCorrect()
    {
        var filter = new BiQuadFilter();
        Assert.IsFalse(filter.FilterEnabled);
        Assert.IsNotNull(filter.GetFilter);
        Assert.AreEqual(FilterProcessingTypes.WholeBlock, filter.FilterProcessingType);
    }

    [TestMethod]
    public void BiQuadFilter_Transform_Works()
    {
        var filter = new BiQuadFilter();
        filter.SetCoefficients(1, 0, 0, 1, 0, 0);
        var input = new double[] { 1, 2, 3 };
        var output = filter.Transform(input, new DSP_Stream());
        Assert.AreEqual(input.Length, output.Length);
    }

    [TestMethod]
    public void BiQuadFilter_DeepClone_ReturnsClone()
    {
        var filter = new BiQuadFilter();
        var clone = filter.DeepClone();
        Assert.IsNotNull(clone);
        Assert.IsInstanceOfType(clone, typeof(BiQuadFilter));
    }

    [TestMethod]
    public void BiQuadFilter_SetCoefficients_SetsValues()
    {
        var filter = new BiQuadFilter();
        filter.SetCoefficients(2, 3, 4, 5, 6, 7);
        Assert.AreEqual(2, filter.aa0);
        Assert.AreEqual(3, filter.aa1);
        Assert.AreEqual(4, filter.aa2);
        Assert.AreEqual(5, filter.b0);
        Assert.AreEqual(6, filter.b1);
        Assert.AreEqual(7, filter.b2);
    }
}