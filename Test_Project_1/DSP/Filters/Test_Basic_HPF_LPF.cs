namespace Test_Project_1;

using BassThatHz_ASIO_DSP_Processor;
using NAudio.Dsp;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

[TestClass]
public class Test_Basic_HPF_LPF
{
    protected void InitData(double[] input)
    {
        for (int i = 0; i < input.Length; i++)
        {
            input[i] = 1;
        }
    }

    [TestMethod]
    public void Test_Basic_HPF_LPFFilter_IsFast()
    {
        //Init Test structures
        DSP_Stream DSPStream = new();
        Basic_HPF_LPF PolarityFilter = new();

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
    public void Basic_HPF_LPF_DefaultValues_AreCorrect()
    {
        var filter = new Basic_HPF_LPF();
        Assert.AreEqual(1, filter.HPFFreq);
        Assert.AreEqual(Basic_HPF_LPF.FilterOrder.LR_12db, filter.HPFFilter);
        Assert.AreEqual(20000, filter.LPFFreq);
        Assert.AreEqual(Basic_HPF_LPF.FilterOrder.LR_12db, filter.LPFFilter);
        Assert.IsNotNull(filter.Q_Array_HPF);
        Assert.IsNotNull(filter.Q_Array_LPF);
        Assert.IsNotNull(filter.BiQuads);
        Assert.IsFalse(filter.FilterEnabled);
        Assert.AreEqual(FilterTypes.Basic_HPF_LPF, filter.FilterType);
        Assert.AreEqual(FilterProcessingTypes.WholeBlock, filter.FilterProcessingType);
        Assert.IsNotNull(filter.GetFilter);
    }

    [TestMethod]
    public void Basic_HPF_LPF_ApplySettings_InitializesBiQuads()
    {
        var filter = new Basic_HPF_LPF();
        filter.HPFFilter = Basic_HPF_LPF.FilterOrder.LR_12db;
        filter.LPFFilter = Basic_HPF_LPF.FilterOrder.LR_12db;
        filter.ApplySettings();
        Assert.IsNotNull(filter.BiQuads[0]);
        Assert.IsNotNull(filter.BiQuads[1]);
        Assert.IsNotNull(filter.BiQuads[4]);
        Assert.IsNotNull(filter.BiQuads[5]);
    }

    [TestMethod]
    public void Basic_HPF_LPF_Transform_ReturnsInput_WhenNoFilters()
    {
        var filter = new Basic_HPF_LPF();
        filter.HPFFilter = Basic_HPF_LPF.FilterOrder.None;
        filter.LPFFilter = Basic_HPF_LPF.FilterOrder.None;
        var input = new double[] { 1, 2, 3 };
        var output = filter.Transform(input, new DSP_Stream());
        Assert.AreSame(input, output);
    }

    [TestMethod]
    public void Basic_HPF_LPF_DeepClone_ReturnsClone()
    {
        var filter = new Basic_HPF_LPF();
        var clone = filter.DeepClone();
        Assert.IsNotNull(clone);
        Assert.IsInstanceOfType(clone, typeof(Basic_HPF_LPF));
    }
}