using BassThatHz_ASIO_DSP_Processor;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Test_Project_1;

[TestClass]
public class Test_DSP_Stream
{
    [TestMethod]
    public void DSP_Stream_DefaultValues_AreCorrect()
    {
        var stream = new DSP_Stream();
        Assert.IsNull(stream.AbstractBusBuffer);
        Assert.IsNull(stream.AuxBuffer);
        Assert.AreEqual(256, stream.NumberOfAuxBuffers);
        Assert.AreEqual(string.Empty, stream.InputChannelName);
        Assert.AreEqual(string.Empty, stream.OutputChannelName);
        Assert.AreEqual(-1, stream.InputChannelIndex);
        Assert.AreEqual(-1, stream.OutputChannelIndex);
        Assert.IsNotNull(stream.InputSource);
        Assert.IsNotNull(stream.OutputDestination);
        Assert.AreEqual(0, stream.InputVolume);
        Assert.AreEqual(0, stream.OutputVolume);
        Assert.IsNotNull(stream.Filters);
    }

    [TestMethod]
    public void DSP_Stream_PropertySetters_WorkCorrectly()
    {
        var stream = new DSP_Stream();
        var inputSource = new StreamItem { StreamType = StreamType.Channel };
        var outputDestination = new StreamItem { StreamType = StreamType.Bus };
        stream.InputSource = inputSource;
        stream.OutputDestination = outputDestination;
        Assert.AreEqual(inputSource, stream.InputSource);
        Assert.AreEqual(outputDestination, stream.OutputDestination);
        stream.InputVolume = 0.5;
        stream.OutputVolume = 0.8;
        Assert.AreEqual(0.5, stream.InputVolume);
        Assert.AreEqual(0.8, stream.OutputVolume);
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void DSP_Stream_InputSource_Throws_On_StreamType_Stream()
    {
        var stream = new DSP_Stream();
        var invalidSource = new StreamItem { StreamType = StreamType.Stream };
        stream.InputSource = invalidSource;
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void DSP_Stream_OutputDestination_Throws_On_StreamType_Stream()
    {
        var stream = new DSP_Stream();
        var invalidDest = new StreamItem { StreamType = StreamType.Stream };
        stream.OutputDestination = invalidDest;
    }

    [TestMethod]
    public void DSP_Stream_Filters_CanBeModified()
    {
        var stream = new DSP_Stream();
        var filter = new TestFilter();
        stream.Filters.Add(filter);
        Assert.AreEqual(1, stream.Filters.Count);
    }

    private class TestFilter : IFilter
    {
        public bool FilterEnabled { get; set; }
        public IFilter GetFilter => this;
        public FilterTypes FilterType => FilterTypes.Basic_HPF_LPF;
        public FilterProcessingTypes FilterProcessingType => FilterProcessingTypes.WholeBlock;
        public IFilter DeepClone() => this;
        public void ApplySettings() { }
        public void ResetSampleRate(int sampleRate) { }
        public double[] Transform(double[] input, DSP_Stream currentStream) => input;
    }
}