namespace Test_Project_1;

using BassThatHz_ASIO_DSP_Processor;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NAudio.Dsp;
using NAudio.Wave.Asio;
using System;
using System.Reflection;
using System.Runtime.InteropServices;

/// <summary>
/// Drives the REAL DSP audio loop over many buffer callbacks using the mock ASIO driver, so the
/// hot path can be exercised on a machine with no ASIO hardware.
/// <para>
/// This covers what a config-load test cannot: with AutoStartDSP the engine only runs once audio
/// is flowing, which is where the Part 1 rewrites live - the chain builder and its AbstractBus
/// clone cache, the reusable ChainWorker array, the DataAvailableNotifier, and the
/// DSP_Process_Channel buffer fallbacks. A leak in any of those grows without bound per callback
/// and would only ever show up on hardware.
/// </para>
/// </summary>
[TestClass]
public class Test_ASIO_Engine_Soak
{
    private const int ChannelCount = 8;
    private const int SamplesPerBuffer = 256;

    #region Helpers
    private static T? GetPrivate<T>(object target, string fieldName) where T : class
    {
        var Local_Field = target.GetType().GetField(fieldName,
                              BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        return Local_Field?.GetValue(target) as T;
    }

    /// <summary>Builds a config with a straight channel-to-channel stream per channel pair.</summary>
    private static void ArrangeRealisticConfig()
    {
        Program.DSP_Info = new DSP_Info();
        for (int i = 0; i < 4; i++)
        {
            var Local_Stream = new DSP_Stream
            {
                InputSource = new StreamItem { Index = i, StreamType = StreamType.Channel, Name = "In" + i },
                OutputDestination = new StreamItem { Index = i, StreamType = StreamType.Channel, Name = "Out" + i },
                InputVolume = 1d,
                OutputVolume = 1d,
            };

            //Real DSP work per block, mirroring a typical user chain.
            var Local_BiQuad = new BiQuadFilter { FilterEnabled = true };
            Local_BiQuad.ApplySettings();
            Local_Stream.Filters.Add(Local_BiQuad);

            var Local_Limiter = new Limiter { FilterEnabled = true };
            Local_Limiter.ApplySettings();
            Local_Stream.Filters.Add(Local_Limiter);

            Program.DSP_Info.Streams.Add(Local_Stream);
        }
    }

    private static IntPtr[] AllocBuffers(int count, int bytes)
    {
        var Local_Ptrs = new IntPtr[count];
        for (int i = 0; i < count; i++)
        {
            Local_Ptrs[i] = Marshal.AllocHGlobal(bytes);
            for (int b = 0; b < bytes; b++)
                Marshal.WriteByte(Local_Ptrs[i], b, 0);
        }
        return Local_Ptrs;
    }

    private static void FreeBuffers(IntPtr[] ptrs)
    {
        for (int i = 0; i < ptrs.Length; i++)
        {
            if (ptrs[i] != IntPtr.Zero)
                Marshal.FreeHGlobal(ptrs[i]);
        }
    }
    #endregion

    [TestMethod]
    public void DspLoop_OverManyCallbacks_DoesNotGrowItsCaches()
    {
        ArrangeRealisticConfig();

        var Local_Driver = new Test_ASIO_Engine.Mock_ASIO_Unified(ChannelCount, SamplesPerBuffer);
        using var Local_Engine = new Test_ASIO_Engine.Mock_ASIO_Engine(Local_Driver)
        {
            //Run the DSP synchronously on this thread so the soak is deterministic.
            IsMT_BackgroundThreadEnabled = false,
            IsMultiThreadingEnabled = false,
        };
        Local_Engine.Start("MockDriverName", 48000, ChannelCount, ChannelCount);

        int Local_Bytes = SamplesPerBuffer * sizeof(int);
        var Local_In = AllocBuffers(ChannelCount, Local_Bytes);
        var Local_Out = AllocBuffers(ChannelCount, Local_Bytes);

        try
        {
            //Warm up: first passes legitimately allocate (buffers, chain lists, clone cache).
            for (int i = 0; i < 100; i++)
                Local_Driver.Mock_ActivateDataStream(Local_In, Local_Out, AsioSampleType.Int32LSB);

            var Local_CloneCache = GetPrivate<System.Collections.IDictionary>(Local_Engine, "AbstractBusCloneCache");
            var Local_ChainCache = GetPrivate<System.Collections.ICollection>(Local_Engine, "ChainCache");
            int Local_CloneCountAfterWarmup = Local_CloneCache?.Count ?? 0;
            int Local_ChainCountAfterWarmup = Local_ChainCache?.Count ?? 0;

            for (int i = 0; i < 3000; i++)
                Local_Driver.Mock_ActivateDataStream(Local_In, Local_Out, AsioSampleType.Int32LSB);

            Assert.AreEqual(Local_CloneCountAfterWarmup, Local_CloneCache?.Count ?? 0,
                "AbstractBusCloneCache grew across 3000 buffer callbacks - this is an unbounded "
                + "per-callback leak, and each miss also performs a full XML DeepClone.");
            Assert.AreEqual(Local_ChainCountAfterWarmup, Local_ChainCache?.Count ?? 0,
                "ChainCache grew across 3000 buffer callbacks.");
        }
        finally
        {
            FreeBuffers(Local_In);
            FreeBuffers(Local_Out);
        }
    }

    [TestMethod]
    public void DspLoop_SteadyState_AllocatesLittlePerCallback()
    {
        ArrangeRealisticConfig();

        var Local_Driver = new Test_ASIO_Engine.Mock_ASIO_Unified(ChannelCount, SamplesPerBuffer);
        using var Local_Engine = new Test_ASIO_Engine.Mock_ASIO_Engine(Local_Driver)
        {
            IsMT_BackgroundThreadEnabled = false,
            IsMultiThreadingEnabled = false,
        };
        Local_Engine.Start("MockDriverName", 48000, ChannelCount, ChannelCount);

        int Local_Bytes = SamplesPerBuffer * sizeof(int);
        var Local_In = AllocBuffers(ChannelCount, Local_Bytes);
        var Local_Out = AllocBuffers(ChannelCount, Local_Bytes);

        try
        {
            for (int i = 0; i < 200; i++)
                Local_Driver.Mock_ActivateDataStream(Local_In, Local_Out, AsioSampleType.Int32LSB);

            const int Local_Cycles = 2000;
            long Local_Before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < Local_Cycles; i++)
                Local_Driver.Mock_ActivateDataStream(Local_In, Local_Out, AsioSampleType.Int32LSB);
            long Local_After = GC.GetAllocatedBytesForCurrentThread();

            double Local_PerCallback = (Local_After - Local_Before) / (double)Local_Cycles;

            //The pre-Part-1 engine allocated ~2 KB per callback in BuildStreamChains alone, plus a
            //closure/Task per chain and a double[] per AbstractBus node. A generous ceiling still
            //catches any reintroduction of per-callback allocation, without being brittle about a
            //few bytes of unavoidable churn.
            Assert.IsTrue(Local_PerCallback < 256,
                "Steady-state DSP allocated " + Local_PerCallback.ToString("F1")
                + " bytes per buffer callback (ceiling 256). Per-callback allocation is back on the "
                + "real-time path and will cause GC pauses / audio dropouts.");
        }
        finally
        {
            FreeBuffers(Local_In);
            FreeBuffers(Local_Out);
        }
    }

    [TestMethod]
    public void DspLoop_MultiThreadedPath_RunsManyCallbacks_WithoutGrowingCaches()
    {
        ArrangeRealisticConfig();

        var Local_Driver = new Test_ASIO_Engine.Mock_ASIO_Unified(ChannelCount, SamplesPerBuffer);
        using var Local_Engine = new Test_ASIO_Engine.Mock_ASIO_Engine(Local_Driver)
        {
            //This is the configuration the shipped app actually uses.
            IsMT_BackgroundThreadEnabled = true,
            IsMultiThreadingEnabled = true,
        };
        Local_Engine.Start("MockDriverName", 48000, ChannelCount, ChannelCount);

        int Local_Bytes = SamplesPerBuffer * sizeof(int);
        var Local_In = AllocBuffers(ChannelCount, Local_Bytes);
        var Local_Out = AllocBuffers(ChannelCount, Local_Bytes);

        try
        {
            for (int i = 0; i < 100; i++)
                Local_Driver.Mock_ActivateDataStream(Local_In, Local_Out, AsioSampleType.Int32LSB);

            var Local_CloneCache = GetPrivate<System.Collections.IDictionary>(Local_Engine, "AbstractBusCloneCache");
            int Local_CloneCountAfterWarmup = Local_CloneCache?.Count ?? 0;

            for (int i = 0; i < 1000; i++)
                Local_Driver.Mock_ActivateDataStream(Local_In, Local_Out, AsioSampleType.Int32LSB);

            Assert.AreEqual(Local_CloneCountAfterWarmup, Local_CloneCache?.Count ?? 0,
                "AbstractBusCloneCache grew on the multi-threaded DSP path.");

            //Deliberately NOT asserting Engine.Underruns here. That counter compares elapsed
            //wall-clock against the buffer period, but this soak drives callbacks back-to-back with
            //no real-time pacing, so an occasional "underrun" is an artifact of the harness (and of
            //thread-pool warmup on the MT path), not a defect. Asserting it produced exactly the
            //kind of load-dependent flake the existing *_IsFast tests suffer from.
        }
        finally
        {
            FreeBuffers(Local_In);
            FreeBuffers(Local_Out);
        }
    }
}


