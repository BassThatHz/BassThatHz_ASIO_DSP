namespace Test_Project_1;

using BassThatHz_ASIO_DSP_Processor;
using System.Diagnostics;

[TestClass]
public class Test_ULF_FIR
{
    protected void InitData(double[] input)
    {
        for (int i = 0; i < input.Length; i++)
        {
            input[i] = 1;
        }
    }

    [TestMethod]
    public void ULF_FIR_Transform_IsFast()
    {
        DSP_Stream DSPStream = new();
        ULF_FIR filter = new();
        var input = new double[512];
        InitData(input);
        var sw = new Stopwatch();
        sw.Start();
        var output = filter.Transform(input, DSPStream);
        sw.Stop();
        Assert.IsTrue(sw.Elapsed.TotalMilliseconds < 5, "Over 5ms");
    }

    [TestMethod]
    public void ULF_FIR_Properties_Defaults()
    {
        var filter = new ULF_FIR();
        Assert.AreEqual(8192, filter.FFTSize);
        Assert.AreEqual(1, filter.TapsSampleRateIndex);
        Assert.AreEqual(960, filter.TapsSampleRate);
        Assert.IsNull(filter.Taps);
        Assert.IsFalse(filter.FilterEnabled);
        Assert.AreEqual(FilterTypes.ULF_FIR, filter.FilterType);
        Assert.AreEqual(FilterProcessingTypes.WholeBlock, filter.FilterProcessingType);
    }

    [TestMethod]
    public void ULF_FIR_SetTaps_UpdatesTaps()
    {
        var filter = new ULF_FIR();
        var taps = new double[] { 1.1, 2.2, 3.3 };
        filter.SetTaps(taps);
        Assert.IsNotNull(filter.Taps);
        Assert.AreEqual(3, filter.Taps.Length);
        Assert.AreEqual(2.2, filter.Taps[1]);
    }

    [TestMethod]
    public void ULF_FIR_DeepClone_ReturnsClone()
    {
        var filter = new ULF_FIR();
        var clone = filter.DeepClone();
        Assert.IsNotNull(clone);
        Assert.IsInstanceOfType(clone, typeof(ULF_FIR));
    }
}