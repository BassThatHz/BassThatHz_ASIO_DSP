#nullable enable

namespace Test_Project_1;

#region Usings
using BassThatHz_ASIO_DSP_Processor.GUI.Forms;
using System;
#endregion

/// <summary>
/// GUARD SUITE for GUI\Forms\RollingPeakEnvelope.cs, the rolling Y axis range behind the RTA
/// waveform charts.
///
/// The contract is: follow the signal UP at once (never clip the waveform), come back DOWN only as
/// loud blocks age out of the window, and never step. Time is passed in rather than read from the
/// clock, so all of this is asserted deterministically.
/// </summary>
[TestClass]
public class Test_RollingPeakEnvelope
{
    #region Helpers

    private static readonly DateTime Origin = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// The envelope as FormRTA configures it: a ten second window in 40 buckets, easing down with
    /// a two second time constant.
    /// </summary>
    private static RollingPeakEnvelope NewEnvelope()
    {
        return new RollingPeakEnvelope(TimeSpan.FromSeconds(10), 40, TimeSpan.FromSeconds(1));
    }

    #endregion

    #region Shape

    [TestMethod]
    public void Window_IsTheConfiguredLength()
    {
        var Local_Envelope = NewEnvelope();

        Assert.AreEqual(TimeSpan.FromSeconds(10), Local_Envelope.Window);
        Assert.AreEqual(40, Local_Envelope.BucketCount);
    }

    [TestMethod]
    public void DegenerateConfiguration_IsCoerced()
    {
        var Local_Envelope = new RollingPeakEnvelope(TimeSpan.Zero, 0, TimeSpan.FromSeconds(-1));

        Assert.IsTrue(Local_Envelope.BucketCount >= 1);
        Assert.IsTrue(Local_Envelope.Window > TimeSpan.Zero);
        Assert.IsTrue(Local_Envelope.Update(0.5d, Origin) > 0d);
    }

    #endregion

    #region Rising

    /// <summary>
    /// The axis must never clip the waveform, so a louder block takes effect on the same update.
    /// </summary>
    [TestMethod]
    public void RisesToALouderBlockImmediately()
    {
        var Local_Envelope = NewEnvelope();

        Assert.AreEqual(0.1d, Local_Envelope.Update(0.1d, Origin), 1e-12);
        Assert.AreEqual(0.9d, Local_Envelope.Update(0.9d, Origin.AddMilliseconds(100)), 1e-12);
    }

    [TestMethod]
    public void FirstUpdate_IsAdoptedOutright()
    {
        var Local_Envelope = NewEnvelope();

        Assert.AreEqual(0.42d, Local_Envelope.Update(0.42d, Origin), 1e-12);
        Assert.AreEqual(0.42d, Local_Envelope.Current, 1e-12);
    }

    [TestMethod]
    public void NegativeAndNonFiniteMagnitudes_AreHandled()
    {
        var Local_Envelope = NewEnvelope();

        Assert.AreEqual(0.5d, Local_Envelope.Update(-0.5d, Origin), 1e-12);

        double Local_AfterNaN = Local_Envelope.Update(double.NaN, Origin.AddMilliseconds(100));
        Assert.IsTrue(double.IsFinite(Local_AfterNaN), "A NaN block must not poison the axis");

        double Local_AfterInfinity = Local_Envelope.Update(double.PositiveInfinity, Origin.AddMilliseconds(200));
        Assert.IsTrue(double.IsFinite(Local_AfterInfinity), "An infinite block must not poison the axis");
    }

    #endregion

    #region Holding

    /// <summary>
    /// A single quiet block right after a loud passage must not drag the axis down - that is the
    /// abrupt collapse the user reported.
    /// </summary>
    [TestMethod]
    public void HoldsThroughASingleQuietBlock()
    {
        var Local_Envelope = NewEnvelope();

        _ = Local_Envelope.Update(0.8d, Origin);
        double Local_Held = Local_Envelope.Update(0.001d, Origin.AddMilliseconds(100));

        Assert.AreEqual(0.8d, Local_Held, 1e-9, "One quiet block cannot collapse a held peak");
    }

    /// <summary>
    /// The peak must survive right up to the end of the window, including across where the old
    /// five second reset used to fire.
    /// </summary>
    [TestMethod]
    public void HoldsThePeakForTheWholeWindow()
    {
        var Local_Envelope = NewEnvelope();

        _ = Local_Envelope.Update(0.8d, Origin);

        for (int Local_Step = 1; Local_Step <= 90; Local_Step++)
        {
            double Local_Current = Local_Envelope.Update(0.01d, Origin.AddMilliseconds(Local_Step * 100));
            Assert.AreEqual(0.8d, Local_Current, 1e-9,
                $"The peak was released at {Local_Step * 100} ms, inside the ten second window");
        }
    }

    #endregion

    #region Falling

    /// <summary>
    /// Once the loud block has aged out, the envelope must actually come down - it is a rolling
    /// window, not a ratchet.
    /// </summary>
    [TestMethod]
    public void ComesDownOnceThePeakAgesOutOfTheWindow()
    {
        var Local_Envelope = NewEnvelope();

        _ = Local_Envelope.Update(0.8d, Origin);

        double Local_Current = 0d;
        for (int Local_Step = 1; Local_Step <= 200; Local_Step++)
            Local_Current = Local_Envelope.Update(0.02d, Origin.AddMilliseconds(Local_Step * 100));

        //The ease-down is exponential, so it approaches the window peak rather than snapping onto
        //it; ten seconds past the window is many time constants and lands within a fraction of a dB.
        Assert.AreEqual(0.02d, Local_Current, 1e-3, "It must settle on the signal that is actually present");
    }

    /// <summary>
    /// The whole point of the fix: no single update may move the axis down by a visible jump.
    /// </summary>
    [TestMethod]
    public void NeverStepsDownSharply()
    {
        var Local_Envelope = NewEnvelope();

        double Local_Previous = Local_Envelope.Update(1.0d, Origin);
        double Local_WorstDrop = 0d;

        //Thirty seconds of near silence after one loud transient.
        for (int Local_Step = 1; Local_Step <= 300; Local_Step++)
        {
            double Local_Current = Local_Envelope.Update(0.001d, Origin.AddMilliseconds(Local_Step * 100));
            double Local_Drop = (Local_Previous - Local_Current) / Local_Previous;

            if (Local_Drop > Local_WorstDrop)
                Local_WorstDrop = Local_Drop;

            Local_Previous = Local_Current;
        }

        Assert.IsTrue(Local_WorstDrop <= 0.15d,
            $"The envelope dropped {Local_WorstDrop * 100d:0.0}% in a single update");
    }

    /// <summary>
    /// The decay is framed in elapsed time, so it must not depend on how often the plot timer ticks.
    /// The RTA plot timer runs on a 1 ms interval but its real cadence is whatever a plot costs, so
    /// a range that decayed per-update instead of per-second would drift with machine load.
    /// </summary>
    [TestMethod]
    public void DecayRateIsIndependentOfTheUpdateRate()
    {
        var Local_Fast = NewEnvelope();
        var Local_Slow = NewEnvelope();

        _ = Local_Fast.Update(1.0d, Origin);
        _ = Local_Slow.Update(1.0d, Origin);

        //Thirteen seconds of quiet, one at twenty updates a second and one at ten.
        for (int Local_Step = 1; Local_Step <= 260; Local_Step++)
            _ = Local_Fast.Update(0.05d, Origin.AddMilliseconds(Local_Step * 50));

        for (int Local_Step = 1; Local_Step <= 130; Local_Step++)
            _ = Local_Slow.Update(0.05d, Origin.AddMilliseconds(Local_Step * 100));

        Assert.AreEqual(Local_Fast.Current, Local_Slow.Current, 0.01d,
            $"Fast settled at {Local_Fast.Current:0.0000}, slow at {Local_Slow.Current:0.0000}");
    }

    [TestMethod]
    public void NeverReportsZero()
    {
        var Local_Envelope = NewEnvelope();

        for (int Local_Step = 0; Local_Step <= 300; Local_Step++)
        {
            double Local_Current = Local_Envelope.Update(0d, Origin.AddMilliseconds(Local_Step * 100));
            Assert.IsTrue(Local_Current > 0d, $"A zero axis at step {Local_Step} collapses the chart");
        }
    }

    #endregion

    #region Clock Edge Cases

    /// <summary>
    /// A long gap - the user leaves Pause ticked - must resume cleanly on the current signal rather
    /// than on a stale window.
    /// </summary>
    [TestMethod]
    public void ResumesCleanlyAfterALongGap()
    {
        var Local_Envelope = NewEnvelope();

        _ = Local_Envelope.Update(0.9d, Origin);
        double Local_Resumed = Local_Envelope.Update(0.05d, Origin.AddMinutes(5));

        Assert.AreEqual(0.05d, Local_Resumed, 1e-6, "A five minute gap must not preserve the old peak");
    }

    /// <summary>
    /// A clock that steps backwards must restart the window rather than trust a future timestamp.
    /// </summary>
    [TestMethod]
    public void HandlesTheClockGoingBackwards()
    {
        var Local_Envelope = NewEnvelope();

        _ = Local_Envelope.Update(0.9d, Origin);
        double Local_Rewound = Local_Envelope.Update(0.2d, Origin.AddHours(-1));

        Assert.AreEqual(0.2d, Local_Rewound, 1e-9);
        Assert.IsTrue(double.IsFinite(Local_Envelope.Update(0.3d, Origin.AddHours(-1).AddMilliseconds(100))));
    }

    [TestMethod]
    public void Reset_ForgetsTheWindow()
    {
        var Local_Envelope = NewEnvelope();

        _ = Local_Envelope.Update(0.9d, Origin);
        Local_Envelope.Reset();

        Assert.AreEqual(0.05d, Local_Envelope.Update(0.05d, Origin.AddMilliseconds(100)), 1e-12);
    }

    #endregion
}
