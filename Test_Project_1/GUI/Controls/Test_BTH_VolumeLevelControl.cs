using BassThatHz_ASIO_DSP_Processor.GUI.Controls;
using System;
using System.Reflection;
using System.Windows.Forms;
using System.ComponentModel;

namespace Test_Project_1;

[TestClass]
public class Test_BTH_VolumeLevelControl
{
    //[TestMethod]
    //public void DefaultProperties_AreAccessible()
    //{
    //    var control = new BTH_VolumeLevelControl();
    //    Assert.IsNotNull(control.Get_btn_View);
    //    Assert.IsNotNull(control.Get_timer_Refresh);
    //}

    //[TestMethod]
    //public void MapEventHandlers_RegistersClickEvents()
    //{
    //    var control = new BTH_VolumeLevelControl();
    //    var eventField = typeof(Control).GetField("EventClick", BindingFlags.Static | BindingFlags.NonPublic);
    //    var eventKey = eventField?.GetValue(null);
    //    var eventsProp = typeof(Component).GetProperty("Events", BindingFlags.NonPublic | BindingFlags.Instance);
    //    // Use reflection to access protected pnl_InputClip
    //    var pnl_InputClip = control.GetType().GetField("pnl_InputClip", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(control) as Control;
    //    var eventList = eventsProp?.GetValue(pnl_InputClip) as EventHandlerList;
    //    var clickDelegate = eventList?[eventKey] as Delegate;
    //    Assert.IsNotNull(clickDelegate);
    //    Assert.IsTrue(clickDelegate.GetInvocationList().Length > 0);
    //}

    [TestMethod]
    public void Set_StreamInfo_NullStream_DoesNotThrow()
    {
        var control = new BTH_VolumeLevelControl();
        control.Set_StreamInfo(null);
        Assert.IsNull(control.Stream);
    }

    [TestMethod]
    public void Reset_ClipIndicator_ResetsPanels()
    {
        var control = new BTH_VolumeLevelControl();
        var pnl_InputClip = control.GetType().GetField("pnl_InputClip", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(control) as Panel;
        var pnl_OutputClip = control.GetType().GetField("pnl_OutputClip", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(control) as Panel;
        pnl_InputClip.BackColor = System.Drawing.Color.Red;
        pnl_OutputClip.BackColor = System.Drawing.Color.Red;
        control.Reset_ClipIndicator();
        Assert.AreEqual(System.Drawing.Color.Black, pnl_InputClip.BackColor);
        Assert.AreEqual(System.Drawing.Color.Black, pnl_OutputClip.BackColor);
    }
}
