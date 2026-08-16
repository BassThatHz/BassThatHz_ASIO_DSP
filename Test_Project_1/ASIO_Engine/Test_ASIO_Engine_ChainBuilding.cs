namespace Test_Project_1;

using BassThatHz_ASIO_DSP_Processor;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;

/// <summary>
/// Behavioural-equivalence pins for the ASIO_Engine chain building / chain caching code paths.
/// These were written BEFORE the allocation-reduction refactor (pre-allocated field level
/// collections, LINQ removal, non-allocating chain signature key) so that the refactor could be
/// proven to be behaviour preserving.
///
/// IMPORTANT: every test in here is deliberately free of any Program.DSP_Info (process-wide
/// mutable global) dependency, because MSTest runs test classes in parallel and several other
/// fixtures mutate Program.DSP_Info. Only Channel/Bus topologies are exercised through
/// BuildStreamChains (those never read the global), and the AbstractBus clone-cache semantics
/// are pinned by calling PostProcessChain directly, which is also global-free.
/// </summary>
[TestClass]
public class Test_ASIO_Engine_ChainBuilding
{
    #region Helpers
    private static DSP_Stream CreateStream(int inputIdx, StreamType inputType, int outputIdx, StreamType outputType)
    {
        return new DSP_Stream
        {
            InputSource = new StreamItem { Index = inputIdx, StreamType = inputType },
            OutputDestination = new StreamItem { Index = outputIdx, StreamType = outputType },
            InputVolume = 1.0,
            OutputVolume = 1.0
        };
    }

    private static List<List<DSP_Stream>> InvokeBuildStreamChains(ASIO_Engine engine, ObservableCollection<DSP_Stream> streams)
    {
        var Local_Method = typeof(ASIO_Engine).GetMethod("BuildStreamChains",
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(Local_Method, "BuildStreamChains not found");
        return (List<List<DSP_Stream>>)Local_Method.Invoke(engine, new object[] { streams })!;
    }

    private static List<DSP_Stream>? InvokePostProcessChain(ASIO_Engine engine,
                                                            List<DSP_Stream> rawChain,
                                                            Dictionary<int, DSP_Stream> abMasters)
    {
        var Local_Method = typeof(ASIO_Engine).GetMethod("PostProcessChain",
            BindingFlags.NonPublic | BindingFlags.Instance,
            null,
            new[] { typeof(List<DSP_Stream>), typeof(Dictionary<int, DSP_Stream>) },
            null);
        Assert.IsNotNull(Local_Method, "PostProcessChain(List<DSP_Stream>, Dictionary<int, DSP_Stream>) not found");
        return Local_Method.Invoke(engine, new object[] { rawChain, abMasters }) as List<DSP_Stream>;
    }

    private static int GetCloneCacheCount(ASIO_Engine engine)
    {
        var Local_Field = typeof(ASIO_Engine).GetField("AbstractBusCloneCache",
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(Local_Field, "AbstractBusCloneCache field not found");
        var Local_Value = Local_Field.GetValue(engine);
        Assert.IsNotNull(Local_Value);
        return ((ICollection)Local_Value!).Count;
    }

    private static void SetSamplesPerChannel(ASIO_Engine engine, int value)
    {
        var Local_Property = typeof(ASIO_Engine).GetProperty("SamplesPerChannel");
        Assert.IsNotNull(Local_Property);
        Local_Property!.GetSetMethod(true)!.Invoke(engine, new object[] { value });
    }
    #endregion

    #region Cache identity semantics
    /// <summary>
    /// When nothing about the stream configuration changed, BuildStreamChains must return the
    /// exact same cached outer list AND the exact same inner chain list instances. The DSP loops
    /// depend on this cache to avoid re-cloning AbstractBus masters every buffer switch.
    /// </summary>
    [TestMethod]
    public void BuildStreamChains_UnchangedConfig_ReturnsSameCachedInstances()
    {
        var engine = new ASIO_Engine();
        var streams = new ObservableCollection<DSP_Stream>
        {
            CreateStream(0, StreamType.Channel, 0, StreamType.Bus),
            CreateStream(0, StreamType.Bus, 1, StreamType.Channel)
        };

        var Local_First = InvokeBuildStreamChains(engine, streams);
        var Local_Second = InvokeBuildStreamChains(engine, streams);
        var Local_Third = InvokeBuildStreamChains(engine, streams);

        Assert.IsTrue(ReferenceEquals(Local_First, Local_Second), "Outer cache list changed identity");
        Assert.IsTrue(ReferenceEquals(Local_Second, Local_Third), "Outer cache list changed identity");
        Assert.AreEqual(1, Local_Third.Count);
        Assert.IsTrue(ReferenceEquals(Local_First[0], Local_Third[0]), "Inner chain list changed identity");
        Assert.AreEqual(2, Local_Third[0].Count);
        Assert.IsTrue(ReferenceEquals(streams[0], Local_Third[0][0]));
        Assert.IsTrue(ReferenceEquals(streams[1], Local_Third[0][1]));
    }

    /// <summary>
    /// When the configuration DOES change the cache must be refreshed and reflect the new chains.
    /// This is the critical pin for the double-buffered chain cache.
    /// </summary>
    [TestMethod]
    public void BuildStreamChains_ChangedConfig_RefreshesCache()
    {
        var engine = new ASIO_Engine();
        var streams = new ObservableCollection<DSP_Stream>
        {
            CreateStream(0, StreamType.Channel, 0, StreamType.Bus),
            CreateStream(0, StreamType.Bus, 1, StreamType.Channel)
        };

        var Local_First = InvokeBuildStreamChains(engine, streams);
        Assert.AreEqual(1, Local_First.Count);
        Assert.AreEqual(2, Local_First[0].Count);

        // Add a second, independent Channel->Channel stream.
        var Local_Added = CreateStream(2, StreamType.Channel, 3, StreamType.Channel);
        streams.Add(Local_Added);

        var Local_Second = InvokeBuildStreamChains(engine, streams);
        Assert.AreEqual(2, Local_Second.Count, "New chain was not picked up");
        Assert.IsTrue(Local_Second.Any(c => c.Count == 1 && ReferenceEquals(c[0], Local_Added)));
        Assert.IsTrue(Local_Second.Any(c => c.Count == 2 && ReferenceEquals(c[0], streams[0])));

        // Now shrink it back down again and confirm the cache shrinks too.
        streams.Remove(Local_Added);
        var Local_Third = InvokeBuildStreamChains(engine, streams);
        Assert.AreEqual(1, Local_Third.Count, "Removed chain was not dropped");
        Assert.AreEqual(2, Local_Third[0].Count);

        // And a further no-change call must once again be stable.
        var Local_Fourth = InvokeBuildStreamChains(engine, streams);
        Assert.IsTrue(ReferenceEquals(Local_Third, Local_Fourth));
    }

    /// <summary>
    /// A rebuild after a config change must not leave stale streams from the previous
    /// (longer) chain hanging around inside a reused chain list.
    /// </summary>
    [TestMethod]
    public void BuildStreamChains_ShorterChainAfterChange_DoesNotLeakStaleEntries()
    {
        var engine = new ASIO_Engine();
        var Local_Long = new ObservableCollection<DSP_Stream>
        {
            CreateStream(0, StreamType.Channel, 0, StreamType.Bus),
            CreateStream(0, StreamType.Bus, 1, StreamType.Bus),
            CreateStream(1, StreamType.Bus, 2, StreamType.Bus),
            CreateStream(2, StreamType.Bus, 3, StreamType.Channel)
        };
        var Local_First = InvokeBuildStreamChains(engine, Local_Long);
        Assert.AreEqual(1, Local_First.Count);
        Assert.AreEqual(4, Local_First[0].Count);

        var Local_Short = new ObservableCollection<DSP_Stream>
        {
            CreateStream(5, StreamType.Channel, 6, StreamType.Channel)
        };
        var Local_Second = InvokeBuildStreamChains(engine, Local_Short);
        Assert.AreEqual(1, Local_Second.Count);
        Assert.AreEqual(1, Local_Second[0].Count, "Stale chain entries leaked into a reused list");
        Assert.IsTrue(ReferenceEquals(Local_Short[0], Local_Second[0][0]));
    }
    #endregion

    #region Ordering / validity semantics
    /// <summary>
    /// Endpoint candidates are ordered Channel-input first, then AbstractBus-input, then
    /// Bus-input, with a STABLE sort inside each rank. Pins the hand-rolled replacement for
    /// the original LINQ .Where().OrderBy().ToList().
    /// </summary>
    [TestMethod]
    public void BuildStreamChains_EndpointOrdering_IsStableByInputSourceType()
    {
        var engine = new ASIO_Engine();
        var Local_BusFedEnd = CreateStream(0, StreamType.Bus, 2, StreamType.Channel);
        var Local_ChannelEndA = CreateStream(0, StreamType.Channel, 1, StreamType.Channel);
        var Local_ChannelEndB = CreateStream(1, StreamType.Channel, 3, StreamType.Channel);
        var Local_BusFeeder = CreateStream(4, StreamType.Channel, 0, StreamType.Bus);

        var streams = new ObservableCollection<DSP_Stream>
        {
            Local_BusFedEnd,
            Local_ChannelEndA,
            Local_ChannelEndB,
            Local_BusFeeder
        };

        var Local_Chains = InvokeBuildStreamChains(engine, streams);
        Assert.AreEqual(3, Local_Chains.Count);

        // Channel-input endpoints come first, in declaration order.
        Assert.AreEqual(1, Local_Chains[0].Count);
        Assert.IsTrue(ReferenceEquals(Local_ChannelEndA, Local_Chains[0][0]));
        Assert.AreEqual(1, Local_Chains[1].Count);
        Assert.IsTrue(ReferenceEquals(Local_ChannelEndB, Local_Chains[1][0]));

        // Bus-input endpoint comes last and drags its feeder in front of it.
        Assert.AreEqual(2, Local_Chains[2].Count);
        Assert.IsTrue(ReferenceEquals(Local_BusFeeder, Local_Chains[2][0]));
        Assert.IsTrue(ReferenceEquals(Local_BusFedEnd, Local_Chains[2][1]));
    }

    /// <summary>
    /// A Bus may only be produced by one stream; later producers of the same Bus are dropped.
    /// Pins the GetValidStreams rewrite (reusable HashSet instead of a fresh one per call).
    /// </summary>
    [TestMethod]
    public void BuildStreamChains_DuplicateBusProducer_IsIgnored()
    {
        var engine = new ASIO_Engine();
        var Local_FirstProducer = CreateStream(0, StreamType.Channel, 0, StreamType.Bus);
        var Local_SecondProducer = CreateStream(1, StreamType.Channel, 0, StreamType.Bus);
        var Local_Consumer = CreateStream(0, StreamType.Bus, 1, StreamType.Channel);

        var streams = new ObservableCollection<DSP_Stream>
        {
            Local_FirstProducer,
            Local_SecondProducer,
            Local_Consumer
        };

        var Local_Chains = InvokeBuildStreamChains(engine, streams);
        Assert.AreEqual(1, Local_Chains.Count);
        Assert.AreEqual(2, Local_Chains[0].Count);
        Assert.IsTrue(ReferenceEquals(Local_FirstProducer, Local_Chains[0][0]),
            "The FIRST producer of the bus must win");
        Assert.IsTrue(ReferenceEquals(Local_Consumer, Local_Chains[0][1]));
    }

    /// <summary>
    /// A stream referencing an AbstractBus on only one side with no master present is dropped
    /// by GetValidStreams before any global state is consulted.
    /// </summary>
    [TestMethod]
    public void BuildStreamChains_AbstractBusWithoutMaster_IsDropped()
    {
        var engine = new ASIO_Engine();
        var streams = new ObservableCollection<DSP_Stream>
        {
            CreateStream(0, StreamType.Channel, 0, StreamType.AbstractBus),
            CreateStream(0, StreamType.AbstractBus, 1, StreamType.Channel)
        };

        var Local_Chains = InvokeBuildStreamChains(engine, streams);
        Assert.AreEqual(0, Local_Chains.Count);
    }

    /// <summary>
    /// Two masters for the same AbstractBus index cancel each other out (duplicate exclusion),
    /// which in turn invalidates all streams that reference that AbstractBus on one side only.
    /// Pins the GetAbstractBusMasters duplicate handling.
    /// </summary>
    [TestMethod]
    public void BuildStreamChains_DuplicateAbstractBusMasters_CancelEachOtherOut()
    {
        var engine = new ASIO_Engine();
        var streams = new ObservableCollection<DSP_Stream>
        {
            CreateStream(0, StreamType.Channel, 0, StreamType.AbstractBus),
            CreateStream(0, StreamType.AbstractBus, 1, StreamType.Channel),
            CreateStream(0, StreamType.AbstractBus, 0, StreamType.AbstractBus),
            CreateStream(0, StreamType.AbstractBus, 0, StreamType.AbstractBus)
        };

        var Local_Chains = InvokeBuildStreamChains(engine, streams);
        Assert.AreEqual(0, Local_Chains.Count);
    }

    /// <summary>
    /// An empty stream collection must produce an empty (but non-null) chain set, repeatedly.
    /// </summary>
    [TestMethod]
    public void BuildStreamChains_NoStreams_ReturnsEmptyStableCache()
    {
        var engine = new ASIO_Engine();
        var streams = new ObservableCollection<DSP_Stream>();
        var Local_First = InvokeBuildStreamChains(engine, streams);
        var Local_Second = InvokeBuildStreamChains(engine, streams);
        Assert.IsNotNull(Local_First);
        Assert.AreEqual(0, Local_First.Count);
        Assert.IsTrue(ReferenceEquals(Local_First, Local_Second));
    }
    #endregion

    #region AbstractBus clone cache semantics (PostProcessChain, global-free)
    private static (DSP_Stream feeder, DSP_Stream master, Dictionary<int, DSP_Stream> masters) MakeAbstractBusFixture(int abIndex, int feederInputIndex)
    {
        var Local_Feeder = CreateStream(feederInputIndex, StreamType.Channel, abIndex, StreamType.AbstractBus);
        var Local_Master = CreateStream(abIndex, StreamType.AbstractBus, abIndex, StreamType.AbstractBus);
        var Local_Masters = new Dictionary<int, DSP_Stream> { { abIndex, Local_Master } };
        return (Local_Feeder, Local_Master, Local_Masters);
    }

    /// <summary>
    /// The AbstractBus master clone is cached by (abIndex, upstream-chain-signature). With an
    /// unchanged upstream path the SAME clone instance must be handed back on every rebuild -
    /// re-cloning would reset all of the master's filter state (delay lines etc.) every buffer.
    /// This is the critical pin for replacing the StringBuilder based chain signature with a
    /// non-allocating value key.
    /// </summary>
    [TestMethod]
    public void PostProcessChain_UnchangedUpstreamPath_ReusesTheSameCloneInstance()
    {
        var engine = new ASIO_Engine();
        SetSamplesPerChannel(engine, 8);
        var (Local_Feeder, Local_Master, Local_Masters) = MakeAbstractBusFixture(0, 0);
        var Local_RawChain = new List<DSP_Stream> { Local_Feeder, Local_Master };

        var Local_First = InvokePostProcessChain(engine, Local_RawChain, Local_Masters);
        Assert.IsNotNull(Local_First);
        // The master itself is skipped, the feeder is kept and a clone is injected after it.
        Assert.AreEqual(2, Local_First!.Count);
        Assert.IsTrue(ReferenceEquals(Local_Feeder, Local_First[0]));
        var Local_Clone = Local_First[1];
        Assert.IsFalse(ReferenceEquals(Local_Master, Local_Clone), "Master must be cloned, not shared");
        Assert.AreEqual(8, Local_Clone.AbstractBusBuffer.Length);
        Assert.AreEqual(1, GetCloneCacheCount(engine));

        for (int i = 0; i < 5; i++)
        {
            var Local_Next = InvokePostProcessChain(engine, Local_RawChain, Local_Masters);
            Assert.IsNotNull(Local_Next);
            Assert.AreEqual(2, Local_Next!.Count);
            Assert.IsTrue(ReferenceEquals(Local_Clone, Local_Next[1]),
                "AbstractBus master clone was re-created on rebuild " + i);
            Assert.AreEqual(1, GetCloneCacheCount(engine), "Clone cache grew on rebuild " + i);
        }
    }

    /// <summary>
    /// Two DIFFERENT upstream paths into the same AbstractBus index must NOT share a clone.
    /// This is exactly what the chain signature exists for, so the replacement key must keep it.
    /// </summary>
    [TestMethod]
    public void PostProcessChain_DifferentUpstreamPaths_GetDifferentClones()
    {
        var engine = new ASIO_Engine();
        SetSamplesPerChannel(engine, 4);
        var (Local_FeederA, Local_Master, Local_Masters) = MakeAbstractBusFixture(0, 0);
        var Local_FeederB = CreateStream(1, StreamType.Channel, 0, StreamType.AbstractBus);

        var Local_ChainA = InvokePostProcessChain(engine, new List<DSP_Stream> { Local_FeederA, Local_Master }, Local_Masters);
        var Local_ChainB = InvokePostProcessChain(engine, new List<DSP_Stream> { Local_FeederB, Local_Master }, Local_Masters);

        Assert.IsNotNull(Local_ChainA);
        Assert.IsNotNull(Local_ChainB);
        Assert.IsFalse(ReferenceEquals(Local_ChainA![1], Local_ChainB![1]),
            "Distinct upstream paths must NOT share an AbstractBus master clone");
        Assert.AreEqual(2, GetCloneCacheCount(engine));

        // Repeat: each path must still resolve to its own stable clone.
        var Local_ChainA2 = InvokePostProcessChain(engine, new List<DSP_Stream> { Local_FeederA, Local_Master }, Local_Masters);
        var Local_ChainB2 = InvokePostProcessChain(engine, new List<DSP_Stream> { Local_FeederB, Local_Master }, Local_Masters);
        Assert.IsTrue(ReferenceEquals(Local_ChainA[1], Local_ChainA2![1]));
        Assert.IsTrue(ReferenceEquals(Local_ChainB[1], Local_ChainB2![1]));
        Assert.AreEqual(2, GetCloneCacheCount(engine));
    }

    /// <summary>
    /// The signature must include the POSITION/LENGTH of the upstream path, so that a longer
    /// path sharing a prefix with a shorter one gets its own clone.
    /// </summary>
    [TestMethod]
    public void PostProcessChain_LongerPathSharingAPrefix_GetsItsOwnClone()
    {
        var engine = new ASIO_Engine();
        SetSamplesPerChannel(engine, 4);
        var (Local_Feeder, Local_Master, Local_Masters) = MakeAbstractBusFixture(0, 0);
        var Local_Extra = CreateStream(9, StreamType.Channel, 9, StreamType.Channel);

        var Local_Short = InvokePostProcessChain(engine, new List<DSP_Stream> { Local_Feeder, Local_Master }, Local_Masters);
        var Local_Long = InvokePostProcessChain(engine, new List<DSP_Stream> { Local_Extra, Local_Feeder, Local_Master }, Local_Masters);

        Assert.IsNotNull(Local_Short);
        Assert.IsNotNull(Local_Long);
        Assert.AreEqual(2, Local_Short!.Count);
        Assert.AreEqual(3, Local_Long!.Count);
        Assert.IsFalse(ReferenceEquals(Local_Short[1], Local_Long[2]),
            "A longer upstream path must not reuse the shorter path's clone");
        Assert.AreEqual(2, GetCloneCacheCount(engine));
    }

    /// <summary>
    /// If no master exists for the AbstractBus the chain is rejected (null return).
    /// </summary>
    [TestMethod]
    public void PostProcessChain_NoMasterForAbstractBus_ReturnsNull()
    {
        var engine = new ASIO_Engine();
        SetSamplesPerChannel(engine, 4);
        var Local_Feeder = CreateStream(0, StreamType.Channel, 3, StreamType.AbstractBus);
        var Local_Result = InvokePostProcessChain(engine, new List<DSP_Stream> { Local_Feeder },
                                                  new Dictionary<int, DSP_Stream>());
        Assert.IsNull(Local_Result);
    }

    /// <summary>
    /// Plain (non-AbstractBus) chains pass straight through untouched and allocate no clones.
    /// </summary>
    [TestMethod]
    public void PostProcessChain_PlainChain_PassesThroughUnchanged()
    {
        var engine = new ASIO_Engine();
        SetSamplesPerChannel(engine, 4);
        var Local_A = CreateStream(0, StreamType.Channel, 0, StreamType.Bus);
        var Local_B = CreateStream(0, StreamType.Bus, 1, StreamType.Channel);

        var Local_Result = InvokePostProcessChain(engine, new List<DSP_Stream> { Local_A, Local_B },
                                                  new Dictionary<int, DSP_Stream>());
        Assert.IsNotNull(Local_Result);
        Assert.AreEqual(2, Local_Result!.Count);
        Assert.IsTrue(ReferenceEquals(Local_A, Local_Result[0]));
        Assert.IsTrue(ReferenceEquals(Local_B, Local_Result[1]));
        Assert.AreEqual(0, GetCloneCacheCount(engine));
    }
    #endregion

    #region Allocation pin
    /// <summary>
    /// The whole point of the refactor: a steady-state rebuild of the chains must not allocate
    /// on the audio thread. The threshold is deliberately generous so that JIT/tiering noise
    /// cannot make this flaky, but it is still far below the multi-kilobyte-per-call cost of
    /// the original LINQ + StringBuilder implementation.
    /// </summary>
    [TestMethod]
    public void BuildStreamChains_SteadyState_DoesNotAllocatePerCall()
    {
        var engine = new ASIO_Engine();
        var streams = new ObservableCollection<DSP_Stream>
        {
            CreateStream(0, StreamType.Channel, 0, StreamType.Bus),
            CreateStream(0, StreamType.Bus, 1, StreamType.Bus),
            CreateStream(1, StreamType.Bus, 2, StreamType.Channel),
            CreateStream(3, StreamType.Channel, 4, StreamType.Channel)
        };

        var Local_Method = typeof(ASIO_Engine).GetMethod("BuildStreamChains",
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(Local_Method);
        var Local_Args = new object[] { streams };

        // Warm up (first call populates the cache and JITs everything).
        for (int i = 0; i < 50; i++)
            _ = Local_Method!.Invoke(engine, Local_Args);

        const int Local_Iterations = 200;
        long Local_Before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < Local_Iterations; i++)
            _ = Local_Method!.Invoke(engine, Local_Args);
        long Local_After = GC.GetAllocatedBytesForCurrentThread();

        // Reflection Invoke itself allocates a little per call, so we can only bound the total.
        // 400 bytes/call still proves the LINQ/StringBuilder churn is gone.
        long Local_PerCall = (Local_After - Local_Before) / Local_Iterations;
        Assert.IsTrue(Local_PerCall < 400,
            "BuildStreamChains allocated " + Local_PerCall + " bytes per call in steady state");
    }
    #endregion
}


