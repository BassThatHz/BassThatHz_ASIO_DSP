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
    public void AuxSet_DefaultValues_AreCorrect()
    {
        var filter = new AuxSet();
        Assert.IsFalse(filter.MuteAfter);
        Assert.AreEqual(0, filter.AuxSetIndex);
        Assert.IsFalse(filter.FilterEnabled);
        Assert.AreEqual(FilterTypes.AuxSet, filter.FilterType);
        Assert.AreEqual(FilterProcessingTypes.WholeBlock, filter.FilterProcessingType);
        Assert.IsNotNull(filter.GetFilter);
    }

    [TestMethod]
    public void AuxSet_Transform_MutesAfter()
    {
        var filter = new AuxSet { MuteAfter = true };
        var stream = new DSP_Stream();
        var input = new double[] { 1, 2, 3 };
        var output = filter.Transform(input, stream);
        Assert.IsTrue(Array.TrueForAll(output, v => v == 0));
    }

    [TestMethod]
    public void AuxSet_Transform_CopiesToAuxBuffer()
    {
        var filter = new AuxSet { MuteAfter = false };
        var stream = new DSP_Stream();
        var input = new double[] { 1, 2, 3 };
        var output = filter.Transform(input, stream);
        Assert.IsNotNull(stream.AuxBuffer);
        Assert.AreEqual(input.Length, stream.AuxBuffer[filter.AuxSetIndex].Length);
        Assert.AreEqual(input[0], stream.AuxBuffer[filter.AuxSetIndex][0]);
    }

    [TestMethod]
    public void AuxSet_DeepClone_ReturnsClone()
    {
        var filter = new AuxSet();
        var clone = filter.DeepClone();
        Assert.IsNotNull(clone);
        Assert.IsInstanceOfType(clone, typeof(AuxSet));
    }
}