namespace Test_Project_1;

using BassThatHz_ASIO_DSP_Processor;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

/// <summary>
/// Pins for the allocation-reduction plumbing added to ASIO_Engine / DSP_Stream: the reusable
/// Task array, the pre-built chain workers, the non-allocating chain signature, the pre-allocated
/// data-available notifier and the Span based audio-data copy overloads.
///
/// Like Test_ASIO_Engine_ChainBuilding, nothing in here touches Program.DSP_Info, so these tests
/// are safe to run in parallel with the rest of the suite.
/// </summary>
[TestClass]
public class Test_ASIO_Engine_Allocations
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

    private static object? InvokeProtected(ASIO_Engine engine, string name, params object[] args)
    {
        var Local_Method = typeof(ASIO_Engine).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(Local_Method, name + " not found");
        return Local_Method!.Invoke(engine, args);
    }
    #endregion

    #region Defensive copy semantics must be preserved
    /// <summary>
    /// GetInputAudioData/GetOutputAudioData exist to hand the GUI thread a SNAPSHOT. Turning
    /// them into a live-array hand-out would introduce torn reads in the meters and the RTA,
    /// so this pins that they still copy.
    /// </summary>
    [TestMethod]
    public void GetInputOutputAudioData_StillReturnDefensiveCopies()
    {
        var engine = new ASIO_Engine();
        engine.InputBuffer = new double[1][] { new double[] { 1.0, 2.0, 3.0 } };
        engine.OutputBuffer = new double[1][] { new double[] { 4.0, 5.0, 6.0 } };

        var Local_In = engine.GetInputAudioData(0);
        var Local_Out = engine.GetOutputAudioData(0);
        Assert.IsNotNull(Local_In);
        Assert.IsNotNull(Local_Out);
        Assert.IsFalse(ReferenceEquals(engine.InputBuffer[0], Local_In), "Input copy was aliased");
        Assert.IsFalse(ReferenceEquals(engine.OutputBuffer[0], Local_Out), "Output copy was aliased");
        CollectionAssert.AreEqual(engine.InputBuffer[0], Local_In);
        CollectionAssert.AreEqual(engine.OutputBuffer[0], Local_Out);

        // Mutating the copy must not touch the engine's live buffers.
        Local_In![0] = -99;
        Local_Out![0] = -99;
        Assert.AreEqual(1.0, engine.InputBuffer[0][0]);
        Assert.AreEqual(4.0, engine.OutputBuffer[0][0]);
    }

    /// <summary>
    /// The Span overloads must produce exactly the same snapshot without allocating.
    /// </summary>
    [TestMethod]
    public void TryCopyAudioData_MatchesTheAllocatingOverloads()
    {
        var engine = new ASIO_Engine();
        engine.InputBuffer = new double[2][] { new double[] { 1.5, 2.5 }, new double[] { 3.5, 4.5 } };
        engine.OutputBuffer = new double[2][] { new double[] { 5.5, 6.5 }, new double[] { 7.5, 8.5 } };

        Span<double> Local_Destination = stackalloc double[4];

        Assert.IsTrue(engine.TryCopyInputAudioData(1, Local_Destination, out int Local_InCount));
        Assert.AreEqual(2, Local_InCount);
        Assert.AreEqual(3.5, Local_Destination[0]);
        Assert.AreEqual(4.5, Local_Destination[1]);

        Assert.IsTrue(engine.TryCopyOutputAudioData(0, Local_Destination, out int Local_OutCount));
        Assert.AreEqual(2, Local_OutCount);
        Assert.AreEqual(5.5, Local_Destination[0]);
        Assert.AreEqual(6.5, Local_Destination[1]);
    }

    /// <summary>
    /// Out of range channels and undersized destinations must fail cleanly, not throw.
    /// </summary>
    [TestMethod]
    public void TryCopyAudioData_RejectsBadArguments()
    {
        var engine = new ASIO_Engine();
        engine.InputBuffer = new double[1][] { new double[] { 1.0, 2.0 } };
        engine.OutputBuffer = new double[1][] { new double[] { 3.0, 4.0 } };

        Span<double> Local_TooSmall = stackalloc double[1];
        Span<double> Local_Ok = stackalloc double[2];

        Assert.IsFalse(engine.TryCopyInputAudioData(-1, Local_Ok, out _));
        Assert.IsFalse(engine.TryCopyInputAudioData(5, Local_Ok, out _));
        Assert.IsFalse(engine.TryCopyInputAudioData(0, Local_TooSmall, out int Local_Count));
        Assert.AreEqual(0, Local_Count);

        Assert.IsFalse(engine.TryCopyOutputAudioData(-1, Local_Ok, out _));
        Assert.IsFalse(engine.TryCopyOutputAudioData(5, Local_Ok, out _));
        Assert.IsFalse(engine.TryCopyOutputAudioData(0, Local_TooSmall, out _));
    }
    #endregion

    #region Reusable task array / chain workers
    /// <summary>
    /// The Task array must be reused while the chain count is stable (Task.WaitAll rejects
    /// arrays containing nulls, so it must also be exactly sized when the count changes).
    /// </summary>
    [TestMethod]
    public void EnsureStreamTaskList_ReusesArray_UntilTheChainCountChanges()
    {
        var engine = new ASIO_Engine();

        var Local_First = (Task[])InvokeProtected(engine, "EnsureStreamTaskList", 3)!;
        Assert.AreEqual(3, Local_First.Length);

        var Local_Second = (Task[])InvokeProtected(engine, "EnsureStreamTaskList", 3)!;
        Assert.IsTrue(ReferenceEquals(Local_First, Local_Second), "Task array was needlessly re-allocated");

        var Local_Grown = (Task[])InvokeProtected(engine, "EnsureStreamTaskList", 5)!;
        Assert.AreEqual(5, Local_Grown.Length);

        var Local_Shrunk = (Task[])InvokeProtected(engine, "EnsureStreamTaskList", 2)!;
        Assert.AreEqual(2, Local_Shrunk.Length, "Array must be exactly sized for Task.WaitAll");

        var Local_Empty = (Task[])InvokeProtected(engine, "EnsureStreamTaskList", 0)!;
        Assert.AreEqual(0, Local_Empty.Length);
    }

    /// <summary>
    /// Chain workers are pre-built once, grow-only, and existing worker instances survive the
    /// growth (each worker is permanently bound to its own chain index).
    /// </summary>
    [TestMethod]
    public void EnsureChainWorkers_GrowsOnly_AndPreservesExistingWorkers()
    {
        var engine = new ASIO_Engine();

        var Local_First = (Array)InvokeProtected(engine, "EnsureChainWorkers", 2)!;
        Assert.AreEqual(2, Local_First.Length);
        var Local_Worker0 = Local_First.GetValue(0);
        var Local_Worker1 = Local_First.GetValue(1);
        Assert.IsNotNull(Local_Worker0);
        Assert.IsNotNull(Local_Worker1);

        var Local_Same = (Array)InvokeProtected(engine, "EnsureChainWorkers", 2)!;
        Assert.IsTrue(ReferenceEquals(Local_First, Local_Same), "Worker array was needlessly re-allocated");

        var Local_Grown = (Array)InvokeProtected(engine, "EnsureChainWorkers", 4)!;
        Assert.AreEqual(4, Local_Grown.Length);
        Assert.IsTrue(ReferenceEquals(Local_Worker0, Local_Grown.GetValue(0)), "Worker 0 identity was lost");
        Assert.IsTrue(ReferenceEquals(Local_Worker1, Local_Grown.GetValue(1)), "Worker 1 identity was lost");

        // Asking for fewer must not shrink (and must not drop worker identities).
        var Local_Fewer = (Array)InvokeProtected(engine, "EnsureChainWorkers", 1)!;
        Assert.IsTrue(ReferenceEquals(Local_Grown, Local_Fewer));
        Assert.AreEqual(4, Local_Fewer.Length);
    }
    #endregion

    #region Chain signature
    /// <summary>
    /// The non-allocating chain signature must be deterministic, order sensitive and length
    /// sensitive - those three properties are exactly what the old string signature provided.
    /// </summary>
    [TestMethod]
    public void ComputeChainSignature_IsDeterministic_OrderSensitive_AndLengthSensitive()
    {
        var engine = new ASIO_Engine();
        var Local_A = CreateStream(0, StreamType.Channel, 0, StreamType.Bus);
        var Local_B = CreateStream(1, StreamType.Channel, 1, StreamType.Bus);
        var Local_C = CreateStream(2, StreamType.Channel, 2, StreamType.Bus);

        var Local_Chain = new List<DSP_Stream> { Local_A, Local_B, Local_C };
        var Local_Swapped = new List<DSP_Stream> { Local_B, Local_A, Local_C };

        var Local_Method = typeof(ASIO_Engine).GetMethod("ComputeChainSignature",
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(Local_Method);

        object Sign(List<DSP_Stream> chain, int upTo) => Local_Method!.Invoke(engine, new object[] { chain, upTo })!;

        // Deterministic.
        Assert.AreEqual(Sign(Local_Chain, 3), Sign(Local_Chain, 3));
        // Length sensitive - a prefix must not collide with the full path.
        Assert.AreNotEqual(Sign(Local_Chain, 2), Sign(Local_Chain, 3));
        Assert.AreNotEqual(Sign(Local_Chain, 0), Sign(Local_Chain, 1));
        // Order sensitive.
        Assert.AreNotEqual(Sign(Local_Chain, 3), Sign(Local_Swapped, 3));
        // Content sensitive.
        Assert.AreNotEqual(Sign(Local_Chain, 1), Sign(new List<DSP_Stream> { Local_B }, 1));
    }
    #endregion

    #region Data available notifier
    /// <summary>
    /// The pre-allocated notifier that replaced the per-callback Task.Run lambda must still
    /// raise both events, and must swallow listener exceptions exactly like Task.Run did.
    /// </summary>
    [TestMethod]
    public void DataAvailableNotifier_RaisesBothEvents_AndSwallowsListenerExceptions()
    {
        var engine = new ASIO_Engine();
        bool Local_InputFired = false;
        bool Local_OutputFired = false;
        engine.InputDataAvailable += () => { Local_InputFired = true; };
        engine.OutputDataAvailable += () => { Local_OutputFired = true; };

        var Local_Field = typeof(ASIO_Engine).GetField("DataAvailableWorkItem",
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(Local_Field, "DataAvailableWorkItem field not found");
        var Local_WorkItem = Local_Field!.GetValue(engine);
        Assert.IsNotNull(Local_WorkItem);

        var Local_Execute = Local_WorkItem!.GetType().GetMethod("Execute");
        Assert.IsNotNull(Local_Execute);
        Local_Execute!.Invoke(Local_WorkItem, null);

        Assert.IsTrue(Local_InputFired, "InputDataAvailable was not raised");
        Assert.IsTrue(Local_OutputFired, "OutputDataAvailable was not raised");

        // A throwing listener must not escape (Task.Run used to swallow it into the Task).
        engine.InputDataAvailable += () => throw new InvalidOperationException("boom");
        Local_Execute.Invoke(Local_WorkItem, null);
    }
    #endregion

    #region DSP_Stream fallback buffer
    /// <summary>
    /// The per-stream fallback output buffer must be stable (so the DSP does not allocate one
    /// per callback) and must resize when the ASIO buffer size changes.
    /// </summary>
    [TestMethod]
    public void DSP_Stream_FallbackOutputBuffer_IsStableAndResizes()
    {
        var Local_Stream = CreateStream(0, StreamType.Channel, 0, StreamType.Bus);

        var Local_First = Local_Stream.GetFallbackOutputBuffer(8);
        Assert.AreEqual(8, Local_First.Length);

        var Local_Second = Local_Stream.GetFallbackOutputBuffer(8);
        Assert.IsTrue(ReferenceEquals(Local_First, Local_Second), "Fallback buffer was re-allocated");

        var Local_Resized = Local_Stream.GetFallbackOutputBuffer(16);
        Assert.AreEqual(16, Local_Resized.Length);
        Assert.IsFalse(ReferenceEquals(Local_First, Local_Resized));

        // Distinct streams must NOT share the buffer (that would be a data race between chains).
        var Local_Other = CreateStream(1, StreamType.Channel, 1, StreamType.Bus);
        Assert.IsFalse(ReferenceEquals(Local_Resized, Local_Other.GetFallbackOutputBuffer(16)));
    }
    #endregion
}
