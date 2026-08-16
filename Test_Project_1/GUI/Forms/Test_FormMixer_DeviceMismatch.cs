#nullable enable

namespace Test_Project_1;

#region Usings
using BassThatHz_ASIO_DSP_Processor;
using BassThatHz_ASIO_DSP_Processor.GUI.Controls;
using BassThatHz_ASIO_DSP_Processor.GUI.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NAudio.Wave.Asio;
using System;
using System.Collections.Generic;
using System.Linq;
#endregion

/// <summary>
/// DEFECT: loading a config on a host that does not have the configured ASIO input device used to
/// SILENTLY DISCARD the saved mixer routing.
///
/// FormMixer.RedrawPanelItems() clears MixerInputs and then returns early when the configured device
/// is missing, so MixerInputs stays empty. RedrawPanelItemsFromLoader() then called ApplyChanges(),
/// which clears Mixer.MixerInputs and re-adds the (empty) list - destroying the routing in memory,
/// and permanently on disk as soon as the user saved.
///
/// These tests pin the fix: unmatched saved entries are PRESERVED, matched entries still bind to the
/// live device exactly as before, and the panel still only shows rows for channels that exist.
///
/// Matching is asserted on ChannelIndex / Attenuation / StreamAttenuation / Enabled only.
/// ChannelName is hardware-derived and is NOT a dependable round-trip value, so it is never asserted.
/// </summary>
[TestClass]
public class Test_FormMixer_DeviceMismatch
{
    #region Test Doubles

    /// <summary>
    /// A FormMixer whose "live" ASIO channel list is supplied by the fixture instead of by real
    /// hardware, so the device-absent / partial-device / device-present cases are all reachable
    /// deterministically on any machine.
    /// </summary>
    private sealed class ChannelFakingFormMixer : FormMixer
    {
        public Func<AsioChannelInfo[]?>? ChannelProvider;

        public System.Windows.Forms.Panel GetPanel()
        {
            return this.panel1;
        }

        protected override AsioChannelInfo[]? GetLiveInputChannels()
        {
            //Note: the base constructor calls this before the fixture can assign the provider,
            //which correctly reads as "no device" for that first pass.
            return this.ChannelProvider?.Invoke();
        }
    }

    private sealed class TestMixerControl : MixerControl
    {
        public AsioChannelInfo[]? LiveChannels;

        public Mixer GetTypedFilter => this.Filter;

        protected override FormMixer NewMixerFormInstance()
        {
            return new ChannelFakingFormMixer { ChannelProvider = () => this.LiveChannels };
        }
    }

    private static AsioChannelInfo Channel(int index)
    {
        return new AsioChannelInfo { channel = index, name = "Ch" + index, isInput = true, isActive = true };
    }

    private static MixerInput Input(int channelIndex, double attenuation, double streamAttenuation, bool enabled = true)
    {
        return new MixerInput
        {
            ChannelIndex = channelIndex,
            Attenuation = attenuation,
            StreamAttenuation = streamAttenuation,
            Enabled = enabled,
            ChannelName = "Saved" + channelIndex
        };
    }

    private static MixerInput ByChannel(Mixer filter, int channelIndex)
    {
        var Local_Match = filter.MixerInputs.FirstOrDefault(mi => mi.ChannelIndex == channelIndex);
        Assert.IsNotNull(Local_Match, "No MixerInput preserved for ChannelIndex " + channelIndex + ".");
        return Local_Match!;
    }

    #endregion

    #region (a) Device entirely absent

    [TestMethod]
    public void Load_WithNoAsioDevicePresent_PreservesSavedRouting()
    {
        using var Local_Control = new TestMixerControl { LiveChannels = null };

        var Local_Saved = new Mixer();
        Local_Saved.MixerInputs = new List<MixerInput> { Input(3, -3.5, -6.5), Input(7, -9.25, -12.75) };

        Local_Control.SetDeepClonedFilter(Local_Saved);

        var Local_Filter = Local_Control.GetTypedFilter;
        Assert.AreEqual(2, Local_Filter.MixerInputs.Count,
            "Saved routing must survive loading on a host without the configured ASIO device.");

        Assert.AreEqual(-3.5, ByChannel(Local_Filter, 3).Attenuation);
        Assert.AreEqual(-6.5, ByChannel(Local_Filter, 3).StreamAttenuation);
        Assert.IsTrue(ByChannel(Local_Filter, 3).Enabled);

        Assert.AreEqual(-9.25, ByChannel(Local_Filter, 7).Attenuation);
        Assert.AreEqual(-12.75, ByChannel(Local_Filter, 7).StreamAttenuation);
        Assert.IsTrue(ByChannel(Local_Filter, 7).Enabled);
    }

    [TestMethod]
    public void Load_WithNoAsioDevicePresent_PreservesSavedOrder()
    {
        using var Local_Control = new TestMixerControl { LiveChannels = null };

        var Local_Saved = new Mixer();
        Local_Saved.MixerInputs = new List<MixerInput> { Input(5, -1, -2), Input(0, -3, -4), Input(2, -5, -6) };

        Local_Control.SetDeepClonedFilter(Local_Saved);

        CollectionAssert.AreEqual(
            new[] { 5, 0, 2 },
            Local_Control.GetTypedFilter.MixerInputs.Select(mi => mi.ChannelIndex).ToArray(),
            "Mixer.Transform accumulates in list order, so the saved order must be preserved.");
    }

    /// <summary>
    /// The whole point of preserving the entries: a load followed by a save must not lose routing.
    /// Uses CommonFunctions.DeepClone, which is the very serializer round-trip the config save/load
    /// path runs through.
    /// </summary>
    [TestMethod]
    public void Load_WithNoAsioDevicePresent_SurvivesSerializerRoundTrip()
    {
        using var Local_Control = new TestMixerControl { LiveChannels = null };

        var Local_Original = new Mixer { FilterEnabled = true };
        Local_Original.MixerInputs = new List<MixerInput> { Input(3, -3.5, -6.5), Input(7, -9.25, -12.75) };

        Local_Control.SetDeepClonedFilter(Local_Original);

        //Save -> load.
        var Local_RoundTripped = CommonFunctions.DeepClone(Local_Control.GetTypedFilter);

        Assert.AreEqual(2, Local_RoundTripped.MixerInputs.Count);
        Assert.AreEqual(-3.5, ByChannel(Local_RoundTripped, 3).Attenuation);
        Assert.AreEqual(-6.5, ByChannel(Local_RoundTripped, 3).StreamAttenuation);
        Assert.AreEqual(-9.25, ByChannel(Local_RoundTripped, 7).Attenuation);
        Assert.AreEqual(-12.75, ByChannel(Local_RoundTripped, 7).StreamAttenuation);

        //...and loading that result AGAIN on the same device-less host must still be lossless.
        using var Local_Control2 = new TestMixerControl { LiveChannels = null };
        Local_Control2.SetDeepClonedFilter(Local_RoundTripped);

        CollectionAssert.AreEqual(
            new[] { 3, 7 },
            Local_Control2.GetTypedFilter.MixerInputs.Select(mi => mi.ChannelIndex).ToArray());
    }

    #endregion

    #region (b) Partial device mismatch

    [TestMethod]
    public void Load_WithPartialDeviceMatch_BindsMatchedAndPreservesUnmatched()
    {
        using var Local_Control = new TestMixerControl
        {
            LiveChannels = new[] { Channel(0), Channel(1) }
        };

        var Local_Saved = new Mixer();
        Local_Saved.MixerInputs = new List<MixerInput> { Input(0, -3, -6), Input(5, -9, -12) };

        Local_Control.SetDeepClonedFilter(Local_Saved);

        var Local_Filter = Local_Control.GetTypedFilter;

        //Channel 0 is live and enabled; channel 1 is live but not in the config, so it stays disabled
        //and (per the existing, intentional rule) is not persisted. Channel 5 has no live backing but
        //must not be dropped.
        Assert.AreEqual(2, Local_Filter.MixerInputs.Count,
            "Expected the matched live channel plus the preserved unmatched channel.");

        Assert.AreEqual(-3, ByChannel(Local_Filter, 0).Attenuation);
        Assert.AreEqual(-6, ByChannel(Local_Filter, 0).StreamAttenuation);

        Assert.AreEqual(-9, ByChannel(Local_Filter, 5).Attenuation,
            "The saved routing for a channel the current device lacks must be preserved verbatim.");
        Assert.AreEqual(-12, ByChannel(Local_Filter, 5).StreamAttenuation);
        Assert.IsTrue(ByChannel(Local_Filter, 5).Enabled);
    }

    /// <summary>
    /// The panel itself must still only show rows for channels that actually exist - preserving the
    /// routing must not fabricate UI for absent hardware.
    /// </summary>
    [TestMethod]
    public void Load_WithPartialDeviceMatch_ShowsRowsOnlyForLiveChannels()
    {
        using var Local_Form = new ChannelFakingFormMixer();
        Local_Form.ChannelProvider = () => new[] { Channel(0), Channel(1) };

        Local_Form.RedrawPanelItemsFromLoader(new List<MixerInput> { Input(0, -3, -6), Input(5, -9, -12) });

        Assert.AreEqual(2, Local_Form.GetPanel().Controls.Count,
            "Only live channels may get a mixer row - a preserved entry has no hardware to show.");
    }

    /// <summary>
    /// If the missing device comes back, the preserved entry must bind to the live row rather than
    /// producing a duplicate for the same ChannelIndex.
    /// </summary>
    [TestMethod]
    public void Reload_AfterDeviceReturns_DoesNotDuplicateChannels()
    {
        using var Local_Control = new TestMixerControl { LiveChannels = null };

        var Local_Saved = new Mixer();
        Local_Saved.MixerInputs = new List<MixerInput> { Input(0, -3, -6), Input(1, -9, -12) };

        Local_Control.SetDeepClonedFilter(Local_Saved);
        Assert.AreEqual(2, Local_Control.GetTypedFilter.MixerInputs.Count);

        //Device is now available; reload the very same routing.
        Local_Control.LiveChannels = new[] { Channel(0), Channel(1) };
        Local_Control.SetDeepClonedFilter(CommonFunctions.DeepClone(Local_Control.GetTypedFilter));

        var Local_Channels = Local_Control.GetTypedFilter.MixerInputs.Select(mi => mi.ChannelIndex).ToArray();
        CollectionAssert.AreEqual(new[] { 0, 1 }, Local_Channels, "Channels must not be duplicated.");
        Assert.AreEqual(-3, ByChannel(Local_Control.GetTypedFilter, 0).Attenuation);
        Assert.AreEqual(-9, ByChannel(Local_Control.GetTypedFilter, 1).Attenuation);
    }

    #endregion

    #region (c) Normal case - no behavior change

    /// <summary>
    /// REGRESSION PIN: with the configured device fully present, loading must behave exactly as it did
    /// before the fix - values bound from the live channel list, disabled rows still not persisted,
    /// and nothing extra carried along.
    /// </summary>
    [TestMethod]
    public void Load_WithDeviceFullyPresent_BehavesExactlyAsBefore()
    {
        using var Local_Control = new TestMixerControl
        {
            LiveChannels = new[] { Channel(0), Channel(1), Channel(2) }
        };

        var Local_Saved = new Mixer();
        Local_Saved.MixerInputs = new List<MixerInput>
        {
            Input(0, -3, -6),
            Input(1, -2, -4, enabled: false),
            Input(2, -1, -8)
        };

        Local_Control.SetDeepClonedFilter(Local_Saved);

        var Local_Filter = Local_Control.GetTypedFilter;

        //Only ENABLED inputs are persisted - existing, deliberate behavior, unchanged.
        CollectionAssert.AreEqual(
            new[] { 0, 2 },
            Local_Filter.MixerInputs.Select(mi => mi.ChannelIndex).ToArray());

        Assert.AreEqual(-3, ByChannel(Local_Filter, 0).Attenuation);
        Assert.AreEqual(-6, ByChannel(Local_Filter, 0).StreamAttenuation);
        Assert.AreEqual(-1, ByChannel(Local_Filter, 2).Attenuation);
        Assert.AreEqual(-8, ByChannel(Local_Filter, 2).StreamAttenuation);

        //Live channels supply the display names.
        Assert.AreEqual("Ch0", ByChannel(Local_Filter, 0).ChannelName);
    }

    [TestMethod]
    public void Load_WithDeviceFullyPresent_ReportsNothing()
    {
        Debug.LastSwallowedError = null;

        using var Local_Control = new TestMixerControl
        {
            LiveChannels = new[] { Channel(0), Channel(1) }
        };

        var Local_Saved = new Mixer();
        Local_Saved.MixerInputs = new List<MixerInput> { Input(0, -3, -6), Input(1, -2, -4) };

        Local_Control.SetDeepClonedFilter(Local_Saved);

        Assert.IsNull(Debug.LastSwallowedError,
            "A fully-present device is the normal case and must not report anything.");
    }

    #endregion

    #region (d) The situation is surfaced, without a modal dialog

    [TestMethod]
    public void Load_WithMissingChannels_IsReportedThroughTheNonUiSink()
    {
        Debug.LastSwallowedError = null;
        Exception? Local_Reported = null;
        void Handler(Exception ex) => Local_Reported = ex;
        Debug.SwallowedErrorReported += Handler;
        try
        {
            using var Local_Control = new TestMixerControl
            {
                LiveChannels = new[] { Channel(0) }
            };

            var Local_Saved = new Mixer();
            Local_Saved.MixerInputs = new List<MixerInput> { Input(0, -3, -6), Input(9, -9, -12) };

            Local_Control.SetDeepClonedFilter(Local_Saved);
        }
        finally
        {
            Debug.SwallowedErrorReported -= Handler;
        }

        Assert.IsNotNull(Local_Reported, "A channel mismatch must not be silent.");
        StringAssert.Contains(Local_Reported!.Message, "9",
            "The report should name the channel(s) that could not be bound.");
        StringAssert.Contains(Local_Reported!.Message, "PRESERVED");
    }

    #endregion
}
