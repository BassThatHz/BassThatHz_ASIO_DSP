#nullable enable

namespace Test_Project_1;

#region Usings
using BassThatHz_ASIO_DSP_Processor;
using System;
using System.Reflection;
using System.Threading;
using Test_Project_1.TestHelpers;
#endregion

/// <summary>
/// REGRESSION + ALLOCATION GUARD for the AntiDC ClipEvent throttle.
///
/// <para>
/// THE DEFECT (two independent halves, both producing the same failure): the clip-report site
/// inside the per-sample loop of <c>AntiDC.TransformInPlace</c> CHECKED the 1000 ms throttle but
/// never UPDATED <c>TimeOfLastClipEventRaised</c>, and it compared <c>DateTime.UtcNow</c> against a
/// stamp that the only writer (<c>ReportClipEvents</c>) wrote as LOCAL <c>DateTime.Now</c>. In any
/// timezone behind UTC the difference was inflated by the UTC offset, so the guard was
/// unconditionally true. The result was an args object + a closure + a <c>Task</c> allocated on the
/// real-time audio thread for EVERY qualifying sample.
/// </para>
/// <para>
/// DETERMINISM: the content below is a pure function of the sample index and the thresholds are set
/// so that neither latch can ever trip, so the only wall-clock dependence is the throttle itself.
/// Rate assertions are expressed as "at most one per second of MEASURED elapsed time, plus slack",
/// which cannot go flaky on a slow machine while still separating 1 event from tens of thousands.
/// Handlers run through <c>Task.Run</c>, so counting is via <see cref="Interlocked"/> and the test
/// lets outstanding tasks settle before asserting.
/// </para>
/// </summary>
[TestClass]
public class Test_AntiDC_ClipEventThrottle
{
    #region Constants
    private const int BlockSize = 256;
    private const int WarmupBlocks = 32;
    private const int MeasuredBlocks = 512;
    private const int SoakBlocks = 400;

    /// <summary>
    /// Extra settle time added AFTER the expected dispatches have arrived, so that an unexpected
    /// EXTRA dispatch is observed rather than raced past. Waiting longer can only ever INCREASE the
    /// observed count, so it is safe for an upper bound.
    /// </summary>
    private const int GuardMilliseconds = 60;

    /// <summary>Upper bound on how long a thread-pool handler may take to run before we give up.</summary>
    private const int DispatchTimeoutMilliseconds = 2000;

    /// <summary>
    /// Steady state should be exactly 0 B per block. The pre-fix cost was ~128 dispatches per
    /// 256-sample block (an args object, a closure and a Task each), i.e. tens of kilobytes; this
    /// ceiling sits two orders of magnitude below that and well above any incidental churn.
    /// </summary>
    private const double AllocationCeilingBytesPerBlock = 256d;
    #endregion

    #region Helpers

    /// <summary>
    /// Content that hits the clip-report site as often as the filter allows: every value is repeated
    /// exactly twice, so an identical-sample RUN ENDS on every second sample, which is precisely the
    /// <c>WasPreviousInputIdentical &amp;&amp; !IsPreviousInputIdentical</c> edge that reports clip
    /// events. Every value is above DC_Threshold (1E-05) and, with Clip_Threshold raised to 2.0,
    /// below the clip threshold, so no latch can trip and the block passes through untouched.
    /// 128 report-site hits per 256-sample block.
    /// </summary>
    private static double[] BuildRunEndingBlock(int length)
    {
        var Local_Block = new double[length];
        for (int i = 0; i < length; i++)
            Local_Block[i] = 0.1d + ((i / 2) % 64) * 0.001d;
        return Local_Block;
    }

    /// <summary>An AntiDC configured so that neither the DC latch nor the clip latch can trip.</summary>
    private static AntiDC BuildNonLatchingFilter()
    {
        return new AntiDC
        {
            Clip_Threshold = 2.0d,
            MaxConsecutiveDCSamples = 1000,
            MaxClipEventsPerDuration = 1000,
            DetectionDuration = TimeSpan.FromDays(1)
        };
    }

    private static FieldInfo ThrottleField
    {
        get
        {
            return typeof(AntiDC).GetField("TimeOfLastClipEventRaised",
                BindingFlags.NonPublic | BindingFlags.Instance)!;
        }
    }

    private static DateTime GetThrottleStamp(AntiDC filter)
    {
        return (DateTime)ThrottleField.GetValue(filter)!;
    }

    private static void SetThrottleStamp(AntiDC filter, DateTime value)
    {
        ThrottleField.SetValue(filter, value);
    }

    /// <summary>
    /// Waits until at least <paramref name="expected"/> handler invocations have been observed (or a
    /// timeout elapses), then waits a further guard interval so that any SURPLUS invocation is
    /// counted too, and returns the settled count. The handlers run through <c>Task.Run</c>, so the
    /// counter is only ever read through <see cref="Volatile"/>.
    /// </summary>
    private static int AwaitDispatches(ref int counter, int expected)
    {
        long Local_Deadline = Environment.TickCount64 + DispatchTimeoutMilliseconds;
        while (Volatile.Read(ref counter) < expected && Environment.TickCount64 < Local_Deadline)
            Thread.Sleep(5);

        Thread.Sleep(GuardMilliseconds);
        return Volatile.Read(ref counter);
    }

    #endregion

    #region Throttle Regression

    /// <summary>
    /// THE DIRECT REGRESSION GUARD. Drives many consecutive blocks of run-ending content through
    /// Transform and asserts ClipEvent fires at most once per elapsed second.
    ///
    /// Against the old code this site fired once per qualifying sample - 128 per block, ~51,200 for
    /// this run - so the assertion fails by four orders of magnitude.
    /// </summary>
    [TestMethod]
    public void Regression_Transform_RaisesClipEventAtMostOncePerSecond()
    {
        var Local_Stream = new DSP_Stream();
        var Local_AntiDC = BuildNonLatchingFilter();
        var Local_Block = BuildRunEndingBlock(BlockSize);

        int Local_Count = 0;
        Local_AntiDC.ClipEvent += (s, e) => Interlocked.Increment(ref Local_Count);

        var Local_Stopwatch = System.Diagnostics.Stopwatch.StartNew();
        for (int Local_Index = 0; Local_Index < SoakBlocks; Local_Index++)
            Local_AntiDC.Transform(Local_Block, Local_Stream);

        //Let the fire-and-forget Task.Run handlers settle before counting. The stopwatch keeps
        //running: the ceiling must cover the WHOLE window in which a dispatch could have happened.
        int Local_Observed = AwaitDispatches(ref Local_Count, 1);
        Local_Stopwatch.Stop();

        //One dispatch per elapsed second, plus two of slack for boundary effects.
        int Local_Ceiling = (int)Math.Ceiling(Local_Stopwatch.Elapsed.TotalSeconds) + 2;

        System.Diagnostics.Trace.WriteLine(
            "AntiDC ClipEvent: " + Local_Observed + " dispatches over " + SoakBlocks + " blocks x "
            + BlockSize + " samples (" + (SoakBlocks * BlockSize / 2) + " report-site hits) in "
            + Local_Stopwatch.Elapsed.TotalMilliseconds.ToString("F1") + " ms");

        Assert.IsTrue(Local_Observed >= 1,
            "The test never reached the clip-report site, so it guards nothing.");
        Assert.IsTrue(Local_Observed <= Local_Ceiling,
            "ClipEvent was dispatched " + Local_Observed + " times in "
            + Local_Stopwatch.Elapsed.TotalMilliseconds.ToString("F1")
            + " ms (ceiling " + Local_Ceiling + "). The 1000 ms throttle on the Transform path is "
            + "not updating TimeOfLastClipEventRaised, so the audio thread is allocating a Task per "
            + "qualifying sample.");
    }

    /// <summary>
    /// The Transform path must actually ADVANCE the throttle timestamp - the first half of the
    /// defect was that it checked the stamp but never wrote it. Against the old code the field is
    /// still default(DateTime) after the run.
    /// </summary>
    [TestMethod]
    public void Regression_Transform_AdvancesTheThrottleTimestamp()
    {
        var Local_Stream = new DSP_Stream();
        var Local_AntiDC = BuildNonLatchingFilter();
        var Local_Block = BuildRunEndingBlock(BlockSize);

        Assert.AreEqual(default(DateTime), GetThrottleStamp(Local_AntiDC),
            "Precondition: a fresh AntiDC has never dispatched a clip event.");

        var Local_Before = DateTime.UtcNow;
        Local_AntiDC.Transform(Local_Block, Local_Stream);
        var Local_After = DateTime.UtcNow;

        var Local_Stamp = GetThrottleStamp(Local_AntiDC);

        Assert.AreNotEqual(default(DateTime), Local_Stamp,
            "Transform raised a clip event but left TimeOfLastClipEventRaised at its default, so "
            + "the throttle window can never close.");
        Assert.IsTrue(Local_Stamp >= Local_Before.AddSeconds(-1) && Local_Stamp <= Local_After.AddSeconds(1),
            "TimeOfLastClipEventRaised must be stamped on the UTC clock that reads it. Observed "
            + Local_Stamp.ToString("O") + ", expected between " + Local_Before.ToString("O")
            + " and " + Local_After.ToString("O") + ". A local-time stamp is hours off in any "
            + "non-UTC zone, which makes the comparison unconditionally true.");
    }

    /// <summary>
    /// The fix must be a THROTTLE, not a one-shot: once the window has elapsed, the next qualifying
    /// sample dispatches again. Simulated by rewinding the stamp rather than sleeping a second, so
    /// the test stays fast and deterministic.
    /// </summary>
    [TestMethod]
    public void Regression_Transform_ClipEventResumesAfterTheThrottleWindowElapses()
    {
        var Local_Stream = new DSP_Stream();
        var Local_AntiDC = BuildNonLatchingFilter();
        var Local_Block = BuildRunEndingBlock(BlockSize);

        int Local_Count = 0;
        Local_AntiDC.ClipEvent += (s, e) => Interlocked.Increment(ref Local_Count);

        Local_AntiDC.Transform(Local_Block, Local_Stream);
        Assert.AreEqual(1, AwaitDispatches(ref Local_Count, 1), "Exactly one dispatch opens the window.");

        //Re-stamp to "now" so the window is unambiguously open regardless of how long the settle
        //above took, then prove that further blocks inside it stay silent.
        SetThrottleStamp(Local_AntiDC, DateTime.UtcNow);
        for (int Local_Index = 0; Local_Index < 16; Local_Index++)
            Local_AntiDC.Transform(Local_Block, Local_Stream);
        Assert.AreEqual(1, AwaitDispatches(ref Local_Count, 1),
            "Blocks inside the 1000 ms window must be throttled.");

        //Rewind the stamp past the window; the very next block must dispatch again.
        SetThrottleStamp(Local_AntiDC, DateTime.UtcNow.AddSeconds(-30));
        Local_AntiDC.Transform(Local_Block, Local_Stream);
        Assert.AreEqual(2, AwaitDispatches(ref Local_Count, 2),
            "Once the window has elapsed the clip event must fire again - the throttle rate-limits, "
            + "it does not disable the notification.");
    }

    #endregion

    #region Allocation Guard

    /// <summary>
    /// THE MEMORY DEFECT, PINNED. Steady-state Transform over clipping/DC content must not allocate
    /// per block on the calling (audio) thread. Task.Run allocates its closure, its state machine
    /// and the args object on the CALLING thread, so GC.GetAllocatedBytesForCurrentThread sees
    /// exactly the cost this fix removes.
    ///
    /// The same array instance is reused every block - Transform mutates in place and writes nothing
    /// on the pass-through path - so the harness contributes zero allocation of its own.
    /// </summary>
    [TestMethod]
    public void Soak_AntiDC_SteadyState_DoesNotAllocatePerBlockOnClippingContent()
    {
        var Local_Stream = new DSP_Stream();
        var Local_AntiDC = BuildNonLatchingFilter();
        var Local_Block = BuildRunEndingBlock(BlockSize);

        for (int Local_Index = 0; Local_Index < WarmupBlocks; Local_Index++)
            Local_AntiDC.Transform(Local_Block, Local_Stream);

        long Local_Before = GC.GetAllocatedBytesForCurrentThread();
        for (int Local_Index = 0; Local_Index < MeasuredBlocks; Local_Index++)
            Local_AntiDC.Transform(Local_Block, Local_Stream);
        long Local_After = GC.GetAllocatedBytesForCurrentThread();

        double Local_PerBlock = (Local_After - Local_Before) / (double)MeasuredBlocks;

        System.Diagnostics.Trace.WriteLine(
            "AntiDC.Transform(block=" + BlockSize + ", run-ending DC content) steady state = "
            + Local_PerBlock.ToString("F1") + " B/block");

        Assert.IsTrue(Local_PerBlock < AllocationCeilingBytesPerBlock,
            "AntiDC.Transform allocated " + Local_PerBlock.ToString("F1")
            + " bytes per block (ceiling " + AllocationCeilingBytesPerBlock
            + "). Per-callback allocation is back on the real-time path.");
    }

    #endregion

    #region Mute Behaviour Unchanged

    /// <summary>
    /// OutputMutedEvent is deliberately NOT throttled. This pins the reasoning: because
    /// IsOutputMuted latches and TransformInPlace returns immediately while it is set, the event
    /// fires exactly once no matter how many further blocks arrive - and it fires again only after
    /// ResetDetection, which is the one case a throttle would wrongly swallow.
    /// </summary>
    [TestMethod]
    public void Property_OutputMutedEvent_FiresExactlyOncePerLatch()
    {
        var Local_Stream = new DSP_Stream();
        var Local_AntiDC = new AntiDC { Clip_Threshold = 2.0d, MaxConsecutiveDCSamples = 10 };

        int Local_Count = 0;
        Local_AntiDC.OutputMutedEvent += (s, e) => Interlocked.Increment(ref Local_Count);

        for (int Local_Index = 0; Local_Index < 64; Local_Index++)
            Local_AntiDC.Transform(DspCharacterization.Constant(16, 0.5d), Local_Stream);

        Assert.AreEqual(1, AwaitDispatches(ref Local_Count, 1),
            "The DC latch must announce itself exactly once, however many blocks follow.");

        Local_AntiDC.ResetDetection();
        for (int Local_Index = 0; Local_Index < 64; Local_Index++)
            Local_AntiDC.Transform(DspCharacterization.Constant(16, 0.5d), Local_Stream);

        Assert.AreEqual(2, AwaitDispatches(ref Local_Count, 2),
            "After ResetDetection a fresh fault must announce itself again.");
    }

    /// <summary>
    /// Event dispatch must not perturb the sample path. This repeats the DC-latch golden sequence
    /// from Test_AntiDC_Characterization with BOTH events subscribed, closing the gap that every
    /// existing characterization test runs with no subscribers attached.
    /// </summary>
    [TestMethod]
    public void Property_DcLatchOutputIsIdenticalWithEventSubscribersAttached()
    {
        double[][] Local_Expected =
        {
            new[] { 0.0d, 0.3535533905932738d, 0.5d, 0.3535533905932738d, 6.123233995736766E-17d, -0.35355339059327373d, -0.5d, -0.35355339059327384d },
            new[] { 0.5d, 0.5d, 0.5d, 0.5d, 0.5d, 0.5d, 0.5d, 0.5d },
            new[] { 0.5d, 0.5d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d },
            new[] { 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d },
            new[] { 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d },
            new[] { 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d },
        };

        double[][] Local_Inputs =
        {
            DspCharacterization.Sine(8, 1, 0.5),
            DspCharacterization.Constant(8, 0.5),
            DspCharacterization.Constant(8, 0.5),
            DspCharacterization.Sine(8, 1, 0.5),
            DspCharacterization.Constant(8, 0.0),
            DspCharacterization.Noise(8, 17000UL),
        };

        var Local_Stream = new DSP_Stream();
        var Local_AntiDC = new AntiDC { Clip_Threshold = 2.0d, MaxConsecutiveDCSamples = 10 };
        Local_AntiDC.ClipEvent += (s, e) => { };
        Local_AntiDC.OutputMutedEvent += (s, e) => { };

        for (int Local_Block = 0; Local_Block < Local_Inputs.Length; Local_Block++)
        {
            var Local_Result = Local_AntiDC.Transform(
                DspCharacterization.Copy(Local_Inputs[Local_Block]), Local_Stream);
            DspCharacterization.AssertExact(Local_Expected[Local_Block], Local_Result,
                "Block " + Local_Block + " must be bit-identical with subscribers attached");
        }
    }

    #endregion
}
