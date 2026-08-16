namespace Test_Project_1;

using BassThatHz_ASIO_DSP_Processor;
using System.Diagnostics;

[TestClass]
public class Test_Mixer
{
    [TestMethod]
    public void Test_MixerFilter_IsFast()
    {
        //Init Test structures
        DSP_Stream DSPStream = new();
        Mixer PolarityFilter = new();

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

    /// <summary>
    /// DEFECT FIX PIN: Mixer.Transform indexed Program.ASIO.InputBuffer with no bounds check, so any
    /// MixerInput naming a channel the CURRENT device does not have threw IndexOutOfRangeException on
    /// every buffer callback. The engine tolerates the exception, but tolerating it aborts the whole
    /// filter chain for that block - the stream goes silent. Out-of-range entries must be skipped.
    /// </summary>
    [TestMethod]
    public void Transform_WithChannelIndexBeyondInputBuffer_DoesNotThrow()
    {
        var Local_SavedBuffer = Program.ASIO.InputBuffer;
        try
        {
            Program.ASIO.InputBuffer = new double[2][];
            Program.ASIO.InputBuffer[0] = new double[8];
            Program.ASIO.InputBuffer[1] = new double[8];
            for (int i = 0; i < 8; i++)
            {
                Program.ASIO.InputBuffer[0][i] = 0.5d;
                Program.ASIO.InputBuffer[1][i] = 0.5d;
            }

            var Local_Mixer = new Mixer();
            Local_Mixer.MixerInputs.Add(new MixerInput { ChannelIndex = 99, Enabled = true, Attenuation = 0, StreamAttenuation = 0 });
            Local_Mixer.MixerInputs.Add(new MixerInput { ChannelIndex = -1, Enabled = true, Attenuation = 0, StreamAttenuation = 0 });

            var Local_Audio = new double[8];
            for (int i = 0; i < Local_Audio.Length; i++)
                Local_Audio[i] = 0.25d;

            var Local_Result = Local_Mixer.Transform(Local_Audio, new DSP_Stream());

            Assert.IsNotNull(Local_Result);
            Assert.AreEqual(8, Local_Result.Length);
            for (int i = 0; i < Local_Result.Length; i++)
            {
                Assert.AreEqual(0.25d, Local_Result[i], 1e-12,
                    "An unbacked channel must contribute nothing, leaving the buffer untouched.");
                Assert.IsFalse(double.IsNaN(Local_Result[i]) || double.IsInfinity(Local_Result[i]));
            }
        }
        finally
        {
            Program.ASIO.InputBuffer = Local_SavedBuffer;
        }
    }

    /// <summary>
    /// The in-range path must still mix exactly as before: 0 dB on both gains means
    /// output = input + source.
    /// </summary>
    [TestMethod]
    public void Transform_WithInRangeChannelIndex_StillMixesAsBefore()
    {
        var Local_SavedBuffer = Program.ASIO.InputBuffer;
        try
        {
            Program.ASIO.InputBuffer = new double[2][];
            Program.ASIO.InputBuffer[0] = new double[8];
            Program.ASIO.InputBuffer[1] = new double[8];
            for (int i = 0; i < 8; i++)
            {
                Program.ASIO.InputBuffer[0][i] = 0.5d;
                Program.ASIO.InputBuffer[1][i] = 0.125d;
            }

            var Local_Mixer = new Mixer();
            //Mixed in list order; channel 99 in the middle must be skipped without disturbing the rest.
            Local_Mixer.MixerInputs.Add(new MixerInput { ChannelIndex = 0, Enabled = true, Attenuation = 0, StreamAttenuation = 0 });
            Local_Mixer.MixerInputs.Add(new MixerInput { ChannelIndex = 99, Enabled = true, Attenuation = 0, StreamAttenuation = 0 });
            Local_Mixer.MixerInputs.Add(new MixerInput { ChannelIndex = 1, Enabled = true, Attenuation = 0, StreamAttenuation = 0 });

            var Local_Audio = new double[8];
            for (int i = 0; i < Local_Audio.Length; i++)
                Local_Audio[i] = 0.25d;

            var Local_Result = Local_Mixer.Transform(Local_Audio, new DSP_Stream());

            //(0.25 + 0.5) + 0.125
            for (int i = 0; i < Local_Result.Length; i++)
                Assert.AreEqual(0.875d, Local_Result[i], 1e-9);
        }
        finally
        {
            Program.ASIO.InputBuffer = Local_SavedBuffer;
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