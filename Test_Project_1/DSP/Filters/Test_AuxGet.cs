namespace Test_Project_1;

using BassThatHz_ASIO_DSP_Processor.DSP.Filters;
using BassThatHz_ASIO_DSP_Processor;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

[TestClass]
public class Test_AuxGet
{
    protected void InitData(double[] input)
    {
        for (int i = 0; i < input.Length; i++)
        {
            input[i] = 1;
        }
    }

    [TestMethod]
    public void Test_AuxGetFilter_IsFast()
    {
        //Init Test structures
        DSP_Stream DSPStream = new();
        AuxGet PolarityFilter = new();

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
    public void AuxGet_DefaultValues_AreCorrect()
    {
        var filter = new AuxGet();
        Assert.IsFalse(filter.MuteBefore);
        Assert.AreEqual(0, filter.AuxGetIndex);
        Assert.AreEqual(-6.0d, filter.StreamAttenuation);
        Assert.AreEqual(-6.051d, filter.AuxAttenuation);
        Assert.IsFalse(filter.FilterEnabled);
        Assert.AreEqual(FilterTypes.AuxGet, filter.FilterType);
        Assert.AreEqual(FilterProcessingTypes.WholeBlock, filter.FilterProcessingType);
        Assert.IsNotNull(filter.GetFilter);
    }

    [TestMethod]
    public void AuxGet_Transform_MutesBefore()
    {
        var filter = new AuxGet { MuteBefore = true };
        var stream = new DSP_Stream();
        stream.AuxBuffer = new double[1][] { new double[] { 2, 2, 2 } };
        var input = new double[] { 1, 1, 1 };
        var output = filter.Transform(input, stream);
        Assert.IsTrue(Array.TrueForAll(output, v => v == 2));
    }

    [TestMethod]
    public void AuxGet_Transform_MixesBuffers()
    {
        var filter = new AuxGet { MuteBefore = false };
        var stream = new DSP_Stream();
        stream.AuxBuffer = new double[1][] { new double[] { 2, 2, 2 } };
        var input = new double[] { 1, 1, 1 };
        var output = filter.Transform(input, stream);
        Assert.AreEqual(input.Length, output.Length);
    }

    [TestMethod]
    public void AuxGet_DeepClone_ReturnsClone()
    {
        var filter = new AuxGet();
        var clone = filter.DeepClone();
        Assert.IsNotNull(clone);
        Assert.IsInstanceOfType(clone, typeof(AuxGet));
    }

    [TestMethod]
    public void TestMethod1()
    {
        throw new NotImplementedException();
    }
}