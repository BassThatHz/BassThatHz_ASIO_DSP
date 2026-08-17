using BassThatHz_ASIO_DSP_Processor.GUI.Controls;
using BassThatHz_ASIO_DSP_Processor;
using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using System.ComponentModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test_Project_1;

[TestClass]
public class Test_BTH_VolumeLevelControl
{
    [TestMethod]
    public void Set_StreamInfo_NullStream_DoesNotThrow()
    {
        var control = new BTH_VolumeLevelControl();
        control.Set_StreamInfo(null);
        Assert.IsNull(control.Stream);
    }

    [TestMethod]
    public void Set_StreamInfo_ValidStream_SetsChannelsAndLabels()
    {
        var control = new BTH_VolumeLevelControl();
        var stream = new DSP_Stream
        {
            InputSource = new StreamItem { Index = 1, DisplayMember = "Input1" },
            OutputDestination = new StreamItem { Index = 2, DisplayMember = "Output2" }
        };
        control.Set_StreamInfo(stream);
        var lbl_InputSource = control.GetType().GetField("lbl_InputSource", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(control) as Label;
        var lbl_OutputSource = control.GetType().GetField("lbl_OutputSource", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(control) as Label;
        Assert.AreEqual("Input1", lbl_InputSource.Text);
        Assert.AreEqual("Output2", lbl_OutputSource.Text);
    }

    [TestMethod]
    public void Reset_ClipIndicator_ResetsPanels()
    {
        var control = new BTH_VolumeLevelControl();
        var pnl_InputClip = control.GetType().GetField("pnl_InputClip", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(control) as Panel;
        var pnl_OutputClip = control.GetType().GetField("pnl_OutputClip", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(control) as Panel;
        pnl_InputClip.BackColor = Color.Red;
        pnl_OutputClip.BackColor = Color.Red;
        control.Reset_ClipIndicator();
        Assert.AreEqual(Color.Black, pnl_InputClip.BackColor);
        Assert.AreEqual(Color.Black, pnl_OutputClip.BackColor);
    }

    [TestMethod]
    public void DefaultProperties_AreAccessible()
    {
        var control = new BTH_VolumeLevelControl();
        Assert.IsNotNull(control.Get_btn_View);
        Assert.IsNotNull(control.Get_timer_Refresh);
    }

    [TestMethod]
    public void ComputeLevels_DoesNotThrow_WhenNoChannels()
    {
        var control = new BTH_VolumeLevelControl();
        control.ComputeLevels();
        Assert.IsTrue(true); // Should not throw
    }

    [TestMethod]
    public void ComputeLevels_UpdatesIndicators_WithValidChannels()
    {
        var control = new BTH_VolumeLevelControl();
        var stream = new DSP_Stream
        {
            InputSource = new StreamItem { Index = 0, DisplayMember = "Input" },
            OutputDestination = new StreamItem { Index = 0, DisplayMember = "Output" }
        };
        control.Set_StreamInfo(stream);
        // Mock InputChannel and OutputChannel to have valid indices
        var inputChannelField = control.GetType().GetField("InputChannel", BindingFlags.Instance | BindingFlags.NonPublic);
        var outputChannelField = control.GetType().GetField("OutputChannel", BindingFlags.Instance | BindingFlags.NonPublic);
        inputChannelField.SetValue(control, stream.InputSource);
        outputChannelField.SetValue(control, stream.OutputDestination);
        // ComputeLevels should not throw
        control.ComputeLevels();
        Assert.IsTrue(true);
    }

    [TestMethod]
    public void Set_DB_Lables_UpdatesLabels()
    {
        var control = new BTH_VolumeLevelControl();
        // Set some values
        var inputDbPeakField = control.GetType().GetField("Input_DB_Peak", BindingFlags.Instance | BindingFlags.NonPublic);
        var inputDbField = control.GetType().GetField("Input_DB", BindingFlags.Instance | BindingFlags.NonPublic);
        var outputDbPeakField = control.GetType().GetField("Output_DB_Peak", BindingFlags.Instance | BindingFlags.NonPublic);
        var outputDbField = control.GetType().GetField("Output_DB", BindingFlags.Instance | BindingFlags.NonPublic);
        inputDbPeakField.SetValue(control, 5.5);
        inputDbField.SetValue(control, 2.2);
        outputDbPeakField.SetValue(control, 7.7);
        outputDbField.SetValue(control, 3.3);
        // Refresh through the real path. Set_DB_Lables is no longer meaningful on its own: the
        // peak labels render the HELD peak, which ComputeLevels derives from these raw fields.
        control.ComputeLevels();
        var lbl_Input_DB_Peak = control.GetType().GetField("lbl_Input_DB_Peak", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(control) as Label;
        var lbl_Input_DB = control.GetType().GetField("lbl_Input_DB", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(control) as Label;
        var lbl_Output_DB_Peak = control.GetType().GetField("lbl_Output_DB_Peak", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(control) as Label;
        var lbl_Output_DB = control.GetType().GetField("lbl_Output_DB", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(control) as Label;
        Assert.IsTrue(lbl_Input_DB_Peak.Text.Contains("6"));
        Assert.IsTrue(lbl_Input_DB.Text.Contains("2"));
        Assert.IsTrue(lbl_Output_DB_Peak.Text.Contains("8") || lbl_Output_DB_Peak.Text.Contains("7"));
        Assert.IsTrue(lbl_Output_DB.Text.Contains("3"));
    }

    [TestMethod]
    public void Set_VolAndClipIndicators_UpdatesIndicators()
    {
        var control = new BTH_VolumeLevelControl();
        // Set values to trigger clip
        var inputDbField = control.GetType().GetField("Input_DB", BindingFlags.Instance | BindingFlags.NonPublic);
        var inputDbPeakField = control.GetType().GetField("Input_DB_Peak", BindingFlags.Instance | BindingFlags.NonPublic);
        var outputDbField = control.GetType().GetField("Output_DB", BindingFlags.Instance | BindingFlags.NonPublic);
        var outputDbPeakField = control.GetType().GetField("Output_DB_Peak", BindingFlags.Instance | BindingFlags.NonPublic);
        var clipLevelField = control.GetType().GetField("ClipLevel", BindingFlags.Instance | BindingFlags.NonPublic);
        inputDbField.SetValue(control, 2.0);
        inputDbPeakField.SetValue(control, 2.0);
        outputDbField.SetValue(control, 2.0);
        outputDbPeakField.SetValue(control, 2.0);
        clipLevelField.SetValue(control, 1.0);
        // Call Set_VolAndClipIndicators via reflection
        var setIndicators = control.GetType().GetMethod("Set_VolAndClipIndicators", BindingFlags.Instance | BindingFlags.NonPublic);
        setIndicators.Invoke(control, null);
        var pnl_InputClip = control.GetType().GetField("pnl_InputClip", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(control) as Panel;
        var pnl_OutputClip = control.GetType().GetField("pnl_OutputClip", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(control) as Panel;
        Assert.AreEqual(Color.Red, pnl_InputClip.BackColor);
        Assert.AreEqual(Color.Red, pnl_OutputClip.BackColor);
    }

    [TestMethod]
    public void ComputeLevels_LeavesLabelsAndMetersOnTheSameSnapshot()
    {
        // REGRESSION: the labels used to be driven only by this control's own 1000 ms timer while
        // the meters were driven by FormMonitoring's 100 ms refresh, so the two showed values
        // sampled up to a second apart. One ComputeLevels must now update both.
        var control = new BTH_VolumeLevelControl();

        // No stream is set, so InputChannel/OutputChannel stay null and the Calculate* calls
        // leave these injected values alone - they stand in for one computed audio buffer.
        SetField(control, "Input_DB", -37.4);
        SetField(control, "Input_DB_Peak", -33.6);
        SetField(control, "Output_DB", -12.2);
        SetField(control, "Output_DB_Peak", -8.8);

        control.ComputeLevels();

        var vol_In = GetField(control, "vol_In") as BTH_VolumeLevel_MonitorControl;
        var vol_Out = GetField(control, "vol_Out") as BTH_VolumeLevel_MonitorControl;
        Assert.IsNotNull(vol_In);
        Assert.IsNotNull(vol_Out);

        Assert.AreEqual(-37.4, vol_In!.DB_Level, "input meter fill");
        Assert.AreEqual(-33.6, vol_In.DB_Peak, "input peak bar");
        Assert.AreEqual(-12.2, vol_Out!.DB_Level, "output meter fill");
        Assert.AreEqual(-8.8, vol_Out.DB_Peak, "output peak bar");

        AssertLabelMatches(control, "lbl_Input_DB", vol_In.DB_Level);
        AssertLabelMatches(control, "lbl_Input_DB_Peak", vol_In.DB_Peak);
        AssertLabelMatches(control, "lbl_Output_DB", vol_Out.DB_Level);
        AssertLabelMatches(control, "lbl_Output_DB_Peak", vol_Out.DB_Peak);
    }

    [TestMethod]
    public void ComputeLevels_KeepsLabelsAndMetersInStep_AcrossRefreshes()
    {
        var control = new BTH_VolumeLevelControl();

        // Walk a level past the 0.1 dB meter threshold and the 1 dB label granularity several
        // times; after every refresh the label must still describe what the bar is showing.
        for (int i = 0; i < 12; i++)
        {
            double rms = -60.0 + i * 3.7;
            double peak = rms + 2.5;
            SetField(control, "Input_DB", rms);
            SetField(control, "Input_DB_Peak", peak);

            control.ComputeLevels();

            var vol_In = GetField(control, "vol_In") as BTH_VolumeLevel_MonitorControl;
            Assert.IsNotNull(vol_In);
            AssertLabelMatches(control, "lbl_Input_DB", vol_In!.DB_Level);
            AssertLabelMatches(control, "lbl_Input_DB_Peak", vol_In.DB_Peak);
        }
    }

    [TestMethod]
    public void PeakBar_HoldsTheMaximumOfTheLastThreeRefreshes()
    {
        var control = new BTH_VolumeLevelControl();

        // A single loud transient followed by quiet buffers.
        var peaks = new[] { -50.0, -50.0, -12.0, -50.0, -50.0, -50.0, -50.0 };
        // The bar must show the transient for the refresh it happened on plus the next two.
        var expected = new[] { -50.0, -50.0, -12.0, -12.0, -12.0, -50.0, -50.0 };

        for (int i = 0; i < peaks.Length; i++)
        {
            SetField(control, "Input_DB", -60.0);
            SetField(control, "Input_DB_Peak", peaks[i]);
            control.ComputeLevels();

            var vol_In = GetField(control, "vol_In") as BTH_VolumeLevel_MonitorControl;
            Assert.IsNotNull(vol_In);
            Assert.AreEqual(expected[i], vol_In!.DB_Peak, "refresh " + i + " held the wrong peak");
        }
    }

    [TestMethod]
    public void PeakBar_DoesNotStartPinnedAtFullScale()
    {
        // A zero-filled hold window would read as 0 dBFS until it had been overwritten.
        var control = new BTH_VolumeLevelControl();

        SetField(control, "Input_DB", -60.0);
        SetField(control, "Input_DB_Peak", -55.0);
        control.ComputeLevels();

        var vol_In = GetField(control, "vol_In") as BTH_VolumeLevel_MonitorControl;
        Assert.IsNotNull(vol_In);
        Assert.AreEqual(-55.0, vol_In!.DB_Peak, "the empty part of the hold window leaked into the reading");

        var pnl_InputClip = GetField(control, "pnl_InputClip") as Panel;
        Assert.AreEqual(Color.Black, pnl_InputClip!.BackColor, "an empty hold window latched the clip box");
    }

    [TestMethod]
    public void PeakLabel_ShowsTheSameHeldValueAsThePeakBar()
    {
        var control = new BTH_VolumeLevelControl();

        var peaks = new[] { -40.0, -9.0, -40.0, -40.0, -40.0 };
        foreach (var peak in peaks)
        {
            SetField(control, "Input_DB", -50.0);
            SetField(control, "Input_DB_Peak", peak);
            control.ComputeLevels();

            var vol_In = GetField(control, "vol_In") as BTH_VolumeLevel_MonitorControl;
            Assert.IsNotNull(vol_In);
            AssertLabelMatches(control, "lbl_Input_DB_Peak", vol_In!.DB_Peak);
        }
    }

    [TestMethod]
    public void Reset_DropsTheHeldPeakImmediately()
    {
        var control = new BTH_VolumeLevelControl();

        SetField(control, "Input_DB", -50.0);
        SetField(control, "Input_DB_Peak", -6.0);
        control.ComputeLevels();

        var vol_In = GetField(control, "vol_In") as BTH_VolumeLevel_MonitorControl;
        Assert.IsNotNull(vol_In);
        Assert.AreEqual(-6.0, vol_In!.DB_Peak);

        // "Reset Peak and Clip Indicators" should not leave the bar hanging for two more refreshes.
        control.Reset_ClipIndicator();
        SetField(control, "Input_DB_Peak", -55.0);
        control.ComputeLevels();

        Assert.AreEqual(-55.0, vol_In.DB_Peak, "the held peak survived the reset");
    }

    [TestMethod]
    public void PeakHold_IgnoresNaNBuffers()
    {
        var control = new BTH_VolumeLevelControl();

        SetField(control, "Input_DB", -50.0);
        SetField(control, "Input_DB_Peak", -30.0);
        control.ComputeLevels();

        // One bad buffer must not poison the rolling maximum.
        SetField(control, "Input_DB_Peak", double.NaN);
        control.ComputeLevels();

        var vol_In = GetField(control, "vol_In") as BTH_VolumeLevel_MonitorControl;
        Assert.IsNotNull(vol_In);
        Assert.AreEqual(-30.0, vol_In!.DB_Peak, "NaN broke the peak hold");
    }

    [TestMethod]
    public void ClipIndicator_StaysRed_AfterTheLevelDropsBack()
    {
        // REGRESSION: the old code cleared the box back to black as soon as the level fell below
        // the threshold, so a clip was forgotten instead of being held for the Reset button.
        var control = new BTH_VolumeLevelControl();
        var pnl_InputClip = GetField(control, "pnl_InputClip") as Panel;
        var pnl_OutputClip = GetField(control, "pnl_OutputClip") as Panel;

        SetField(control, "Input_DB", -6.0);
        SetField(control, "Input_DB_Peak", 0.0);   // exactly 0 dBFS = clipped
        SetField(control, "Output_DB", -6.0);
        SetField(control, "Output_DB_Peak", 0.5);
        control.ComputeLevels();

        Assert.AreEqual(Color.Red, pnl_InputClip!.BackColor, "input clip did not latch at 0 dBFS");
        Assert.AreEqual(Color.Red, pnl_OutputClip!.BackColor, "output clip did not latch above 0 dBFS");

        // Signal falls away to nothing over many refreshes; the boxes must hold.
        for (int i = 0; i < 20; i++)
        {
            SetField(control, "Input_DB", -70.0 - i);
            SetField(control, "Input_DB_Peak", -60.0 - i);
            SetField(control, "Output_DB", -70.0 - i);
            SetField(control, "Output_DB_Peak", -60.0 - i);
            control.ComputeLevels();
        }

        Assert.AreEqual(Color.Red, pnl_InputClip.BackColor, "input clip cleared itself");
        Assert.AreEqual(Color.Red, pnl_OutputClip.BackColor, "output clip cleared itself");
    }

    [TestMethod]
    public void ClipIndicator_Latches_WhenThePeakSitsStillAtFullScale()
    {
        // REGRESSION: the clip test used to be nested inside the "peak moved by more than 0.1 dB"
        // repaint throttle, so a steady clipped signal was never tested at all.
        var control = new BTH_VolumeLevelControl();
        var pnl_InputClip = GetField(control, "pnl_InputClip") as Panel;

        // First refresh well below clip, so Prev_Input_DB_Peak is primed and stops changing.
        SetField(control, "Input_DB", -20.0);
        SetField(control, "Input_DB_Peak", -20.0);
        control.ComputeLevels();
        Assert.AreEqual(Color.Black, pnl_InputClip!.BackColor);

        // Now pin the peak at full scale and hold it perfectly still across refreshes.
        SetField(control, "Input_DB", -3.0);
        SetField(control, "Input_DB_Peak", 0.0);
        control.ComputeLevels();
        control.ComputeLevels();
        control.ComputeLevels();

        Assert.AreEqual(Color.Red, pnl_InputClip.BackColor, "a steady full-scale peak did not latch");
    }

    [TestMethod]
    public void ClipIndicator_DoesNotLatch_BelowFullScale()
    {
        var control = new BTH_VolumeLevelControl();
        var pnl_InputClip = GetField(control, "pnl_InputClip") as Panel;
        var pnl_OutputClip = GetField(control, "pnl_OutputClip") as Panel;

        // -0.1 dBFS is loud but not clipped.
        SetField(control, "Input_DB", -3.0);
        SetField(control, "Input_DB_Peak", -0.1);
        SetField(control, "Output_DB", -3.0);
        SetField(control, "Output_DB_Peak", -0.1);
        control.ComputeLevels();

        Assert.AreEqual(Color.Black, pnl_InputClip!.BackColor);
        Assert.AreEqual(Color.Black, pnl_OutputClip!.BackColor);
    }

    [TestMethod]
    public void ClipIndicator_DoesNotLatch_OnSilence()
    {
        var control = new BTH_VolumeLevelControl();
        var pnl_InputClip = GetField(control, "pnl_InputClip") as Panel;

        // Decibels.LinearToDecibels(0) is negative infinity; NaN is defended against too.
        SetField(control, "Input_DB", double.NegativeInfinity);
        SetField(control, "Input_DB_Peak", double.NegativeInfinity);
        control.ComputeLevels();
        Assert.AreEqual(Color.Black, pnl_InputClip!.BackColor, "silence latched the clip box");

        SetField(control, "Input_DB", double.NaN);
        SetField(control, "Input_DB_Peak", double.NaN);
        control.ComputeLevels();
        Assert.AreEqual(Color.Black, pnl_InputClip.BackColor, "NaN latched the clip box");
    }

    [TestMethod]
    public void ClipIndicator_CanBeResetAndThenLatchAgain()
    {
        var control = new BTH_VolumeLevelControl();
        var pnl_InputClip = GetField(control, "pnl_InputClip") as Panel;

        SetField(control, "Input_DB", -3.0);
        SetField(control, "Input_DB_Peak", 2.0);
        control.ComputeLevels();
        Assert.AreEqual(Color.Red, pnl_InputClip!.BackColor);

        // The Reset button at the top of the Monitor screen routes through here.
        control.Reset_ClipIndicator();
        Assert.AreEqual(Color.Black, pnl_InputClip.BackColor, "Reset did not clear the latch");

        // Still clipping, so the very next refresh must light it again.
        control.ComputeLevels();
        Assert.AreEqual(Color.Red, pnl_InputClip.BackColor, "the box did not re-latch after a reset");
    }

    private static void SetField(object target, string name, object value)
    {
        var field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, name + " not found");
        field!.SetValue(target, value);
    }

    private static object? GetField(object target, string name)
    {
        var field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, name + " not found");
        return field!.GetValue(target);
    }

    /// <summary>Asserts a dB label reads the whole-dB rendering of the value the meter was given.</summary>
    private static void AssertLabelMatches(object control, string labelName, double meterValue)
    {
        var label = GetField(control, labelName) as Label;
        Assert.IsNotNull(label, labelName + " is not a Label");

        var expected = Math.Round(meterValue, 0).ToString(System.Globalization.CultureInfo.InvariantCulture) + "dB";
        Assert.AreEqual(expected, label!.Text, labelName + " disagrees with its meter");
    }

    [TestMethod]
    public void MapEventHandlers_RegistersClickEvents()
    {
        var control = new BTH_VolumeLevelControl();
        var pnl_InputClip = control.GetType().GetField("pnl_InputClip", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(control) as Panel;
        var pnl_OutputClip = control.GetType().GetField("pnl_OutputClip", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(control) as Panel;
        int inputClicks = 0, outputClicks = 0;
        pnl_InputClip.Click += (s, e) => inputClicks++;
        pnl_OutputClip.Click += (s, e) => outputClicks++;
        // Simulate click by invoking event directly
        pnl_InputClip.GetType().GetMethod("OnClick", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(pnl_InputClip, new object[] { EventArgs.Empty });
        pnl_OutputClip.GetType().GetMethod("OnClick", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(pnl_OutputClip, new object[] { EventArgs.Empty });
        Assert.IsTrue(inputClicks > 0);
        Assert.IsTrue(outputClicks > 0);
    }
}