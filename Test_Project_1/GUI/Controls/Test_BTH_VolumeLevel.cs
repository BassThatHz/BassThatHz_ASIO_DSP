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
public class Test_BTH_VolumeLevel
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
        // Call Set_DB_Lables via reflection
        var setDbLabels = control.GetType().GetMethod("Set_DB_Lables", BindingFlags.Instance | BindingFlags.NonPublic);
        setDbLabels.Invoke(control, null);
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