#nullable enable

namespace Test_Project_1;

using BassThatHz_ASIO_DSP_Processor;
using ExtendedXmlSerialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#region Usings
using System;
using System.IO;
using System.Runtime.Serialization;
using System.Xml.Serialization;
#endregion

/// <summary>
/// Pins the "do not persist this member" contract of <see cref="ExtendedXmlSerializer"/>.
///
/// <para>
/// Historically only <see cref="XmlIgnoreAttribute"/> was honored by
/// <c>TypeDefinition.GetPropertieToSerialze</c>, so <see cref="IgnoreDataMemberAttribute"/> was
/// silently ignored and the members carrying it were written into every saved config.
/// Those members are computed runtime state (limiter meters, DEQ gain telemetry, the ASIO
/// device-derived mixer channel name), never user settings, so honoring the attribute loses no
/// user data - but it DOES mean previously-saved configs contain elements that no longer map to
/// a member. <c>ExtendedXmlSerializer.ReadXml</c> throws on an unmapped element, so
/// <c>CommonFunctions.RemoveDeprecatedXMLInputTags</c> / <c>RemoveDeprecatedXMLOutputTags</c>
/// strip them first. Both halves of that contract are asserted here.
/// </para>
/// </summary>
[TestClass]
public class Test_IgnoreAttributes
{
    #region Test Fixtures

    /// <summary>Fixture covering every ignore attribute the serializer must honor.</summary>
    public class IgnoreAttributeFixture
    {
        public int KeptField = 0;

        [XmlIgnore]
        public int XmlIgnoredField = 0;

        [IgnoreDataMember]
        public int DataMemberIgnoredField = 0;

        [NonSerialized]
        public int NonSerializedField = 0;

        public int KeptProperty { get; set; }

        [XmlIgnore]
        public int XmlIgnoredProperty { get; set; }

        [IgnoreDataMember]
        public int DataMemberIgnoredProperty { get; set; }
    }

    #endregion

    #region Helpers

    private static string GoldenAssetPath
        => Path.Combine(AppContext.BaseDirectory, "TestAssets", "DSP.xml");

    private static string Serialize(object source)
        => new ExtendedXmlSerializer().Serialize(source);

    private static void AssertHasNoElement(string xml, string elementName)
    {
        Assert.IsFalse(xml.Contains("<" + elementName + ">", StringComparison.Ordinal),
            "Element <" + elementName + "> must not be serialized. XML was:" + Environment.NewLine + xml);
    }

    private static void AssertHasElement(string xml, string elementName)
    {
        Assert.IsTrue(xml.Contains("<" + elementName + ">", StringComparison.Ordinal),
            "Element <" + elementName + "> must be serialized. XML was:" + Environment.NewLine + xml);
    }

    #endregion

    #region Attribute handling

    [TestMethod]
    public void Serializer_HonorsEveryIgnoreAttribute_OnFieldsAndProperties()
    {
        var Local_Fixture = new IgnoreAttributeFixture
        {
            KeptField = 11,
            XmlIgnoredField = 12,
            DataMemberIgnoredField = 13,
            NonSerializedField = 14,
            KeptProperty = 15,
            XmlIgnoredProperty = 16,
            DataMemberIgnoredProperty = 17,
        };

        var Local_Xml = Serialize(Local_Fixture);

        AssertHasElement(Local_Xml, "KeptField");
        AssertHasElement(Local_Xml, "KeptProperty");

        AssertHasNoElement(Local_Xml, "XmlIgnoredField");
        AssertHasNoElement(Local_Xml, "XmlIgnoredProperty");
        AssertHasNoElement(Local_Xml, "DataMemberIgnoredField");
        AssertHasNoElement(Local_Xml, "DataMemberIgnoredProperty");
        AssertHasNoElement(Local_Xml, "NonSerializedField");
    }

    [TestMethod]
    public void Serializer_IgnoredMembers_KeepTheirDefaults_AcrossARoundTrip()
    {
        var Local_Fixture = new IgnoreAttributeFixture
        {
            KeptField = 11,
            DataMemberIgnoredField = 13,
            NonSerializedField = 14,
            KeptProperty = 15,
            DataMemberIgnoredProperty = 17,
        };

        var Local_Clone = CommonFunctions.DeepClone(Local_Fixture);

        Assert.AreEqual(11, Local_Clone.KeptField);
        Assert.AreEqual(15, Local_Clone.KeptProperty);

        Assert.AreEqual(0, Local_Clone.DataMemberIgnoredField);
        Assert.AreEqual(0, Local_Clone.NonSerializedField);
        Assert.AreEqual(0, Local_Clone.DataMemberIgnoredProperty);
    }

    #endregion

    #region The real runtime-state members

    [TestMethod]
    public void Limiter_RuntimeMeterState_IsNotSerialized_ButUserSettingsAre()
    {
        var Local_Limiter = new Limiter
        {
            Threshold = 0.25d,
            MaxValue = 0.75d,
            PeakHoldRelease = 7d,
            PeakHoldAttack = 3d,
            //Runtime state written by Transform and only read by LimiterControl's meters.
            CompressionApplied = 0.5d,
            PeakValue = 0.9d,
            IsBrickwall = true,
        };

        var Local_Xml = Serialize(Local_Limiter);

        AssertHasElement(Local_Xml, "Threshold");
        AssertHasElement(Local_Xml, "MaxValue");
        AssertHasElement(Local_Xml, "PeakHoldRelease");
        AssertHasElement(Local_Xml, "PeakHoldAttack");

        AssertHasNoElement(Local_Xml, "CompressionApplied");
        AssertHasNoElement(Local_Xml, "PeakValue");
        AssertHasNoElement(Local_Xml, "IsBrickwall");
    }

    [TestMethod]
    public void DEQ_GainApplied_IsNotSerialized_ButUserSettingsAre()
    {
        var Local_DEQ = new DEQ
        {
            TargetFrequency = 123.5d,
            TargetGain_dB = -4.25d,
            GainApplied = 9.5d,
        };

        var Local_Xml = Serialize(Local_DEQ);

        AssertHasElement(Local_Xml, "TargetFrequency");
        AssertHasElement(Local_Xml, "TargetGain_dB");
        AssertHasNoElement(Local_Xml, "GainApplied");
    }

    [TestMethod]
    public void MixerInput_ChannelName_IsNotSerialized_ButRoutingIs()
    {
        var Local_Mixer = new Mixer();
        Local_Mixer.MixerInputs.Add(new MixerInput
        {
            Enabled = true,
            Attenuation = -3.5d,
            StreamAttenuation = -1.25d,
            ChannelIndex = 7,
            //Derived from the live ASIO device list, not from the config.
            ChannelName = "Some Live Device Channel",
        });

        var Local_Xml = Serialize(Local_Mixer);

        AssertHasElement(Local_Xml, "ChannelIndex");
        AssertHasElement(Local_Xml, "Attenuation");
        AssertHasNoElement(Local_Xml, "ChannelName");
    }

    #endregion

    #region Backwards compatibility with configs written by older builds

    [TestMethod]
    public void LegacyLimiterAndDEQElements_AreStrippedByTheInputMigration()
    {
        const string Local_Xml =
            "<DSP_Info><Streams><DSP_Stream><Filters>"
            + "<Limiter><Threshold>-3</Threshold><CompressionApplied>1</CompressionApplied>"
            + "<PeakValue>0.78</PeakValue><IsBrickwall>False</IsBrickwall></Limiter>"
            + "<DEQ><TargetFrequency>50</TargetFrequency><GainApplied>4.5</GainApplied></DEQ>"
            + "<Mixer><MixerInputs><MixerInput><ChannelIndex>2</ChannelIndex>"
            + "<ChannelName>Left Loopback</ChannelName></MixerInput></MixerInputs></Mixer>"
            + "</Filters></DSP_Stream></Streams></DSP_Info>";

        var Local_Result = CommonFunctions.RemoveDeprecatedXMLInputTags(Local_Xml);

        Assert.IsFalse(Local_Result.Contains("CompressionApplied", StringComparison.Ordinal));
        Assert.IsFalse(Local_Result.Contains("PeakValue", StringComparison.Ordinal));
        Assert.IsFalse(Local_Result.Contains("IsBrickwall", StringComparison.Ordinal));
        Assert.IsFalse(Local_Result.Contains("GainApplied", StringComparison.Ordinal));
        Assert.IsFalse(Local_Result.Contains("ChannelName", StringComparison.Ordinal));

        Assert.IsTrue(Local_Result.Contains("<Threshold>-3</Threshold>", StringComparison.Ordinal));
        Assert.IsTrue(Local_Result.Contains("<TargetFrequency>50</TargetFrequency>", StringComparison.Ordinal));
        Assert.IsTrue(Local_Result.Contains("<ChannelIndex>2</ChannelIndex>", StringComparison.Ordinal));
    }

    [TestMethod]
    public void LegacyLimiterAndDEQElements_AreAlsoStrippedByTheOutputMigration()
    {
        const string Local_Xml =
            "<DSP_Info><Streams><DSP_Stream><Filters>"
            + "<Limiter><Threshold>-3</Threshold><CompressionApplied>1</CompressionApplied>"
            + "<PeakValue>0.78</PeakValue><IsBrickwall>False</IsBrickwall></Limiter>"
            + "<DEQ><TargetFrequency>50</TargetFrequency><GainApplied>4.5</GainApplied></DEQ>"
            + "<Mixer><MixerInputs><MixerInput><ChannelIndex>2</ChannelIndex>"
            + "<ChannelName>Left Loopback</ChannelName></MixerInput></MixerInputs></Mixer>"
            + "</Filters></DSP_Stream></Streams></DSP_Info>";

        var Local_Result = CommonFunctions.RemoveDeprecatedXMLOutputTags(Local_Xml);

        Assert.IsFalse(Local_Result.Contains("CompressionApplied", StringComparison.Ordinal));
        Assert.IsFalse(Local_Result.Contains("PeakValue", StringComparison.Ordinal));
        Assert.IsFalse(Local_Result.Contains("IsBrickwall", StringComparison.Ordinal));
        Assert.IsFalse(Local_Result.Contains("GainApplied", StringComparison.Ordinal));
        Assert.IsFalse(Local_Result.Contains("ChannelName", StringComparison.Ordinal));

        Assert.IsTrue(Local_Result.Contains("<Threshold>-3</Threshold>", StringComparison.Ordinal));
    }

    /// <summary>
    /// The decisive backwards-compatibility gate: the user's REAL saved config (byte-identical
    /// copy of the repo's DSP.xml, which contains IsBrickwall / PeakValue / CompressionApplied /
    /// GainApplied written by an older build) must still load through the exact app pipeline.
    /// Without the migration step this throws InvalidOperationException("Missing property ...")
    /// and FormMain_Shown reports "Could not successfully load the DSP config file".
    /// </summary>
    [TestMethod]
    public void RealGoldenConfig_ContainingRuntimeStateElements_StillLoadsThroughTheAppPipeline()
    {
        Assert.IsTrue(File.Exists(GoldenAssetPath), "Golden test asset missing: " + GoldenAssetPath);

        var Local_Raw = File.ReadAllText(GoldenAssetPath);

        //Pin that the golden really does contain the legacy runtime-state elements, so this test
        //cannot silently stop covering the migration.
        Assert.IsTrue(Local_Raw.Contains("<IsBrickwall>", StringComparison.Ordinal));
        Assert.IsTrue(Local_Raw.Contains("<PeakValue>", StringComparison.Ordinal));
        Assert.IsTrue(Local_Raw.Contains("<CompressionApplied>", StringComparison.Ordinal));
        Assert.IsTrue(Local_Raw.Contains("<GainApplied>", StringComparison.Ordinal));

        //FormMain_Shown: ReadAllText -> RemoveDeprecatedXMLInputTags -> Deserialize<DSP_Info>
        var Local_Cleaned = CommonFunctions.RemoveDeprecatedXMLInputTags(Local_Raw);
        var Local_Info = new ExtendedXmlSerializer().Deserialize<DSP_Info>(Local_Cleaned);

        Assert.IsNotNull(Local_Info);
        Assert.AreEqual(2, Local_Info.Streams.Count);
        Assert.AreEqual(7, Local_Info.Streams[0].Filters.Count);
        Assert.IsInstanceOfType<Limiter>(Local_Info.Streams[0].Filters[6]);

        //And the user's actual settings survived.
        var Local_Limiter = (Limiter)Local_Info.Streams[0].Filters[6];
        Assert.IsTrue(Local_Limiter.MaxValue > 0d);
    }

    [TestMethod]
    public void RealGoldenConfig_LoadsWithoutTheMigration_OnlyBecauseSerializeNoLongerEmitsRuntimeState()
    {
        //DeepClone does Serialize -> Deserialize with NO migration step. That is only safe
        //because Serialize no longer emits the ignored members at all.
        var Local_Raw = File.ReadAllText(GoldenAssetPath);
        var Local_Info = new ExtendedXmlSerializer()
                            .Deserialize<DSP_Info>(CommonFunctions.RemoveDeprecatedXMLInputTags(Local_Raw));

        var Local_Clone = CommonFunctions.DeepClone(Local_Info);

        Assert.IsNotNull(Local_Clone);
        Assert.AreNotSame(Local_Info, Local_Clone);
        Assert.AreEqual(Local_Info.Streams.Count, Local_Clone.Streams.Count);
    }

    #endregion

    #region DeepClone must keep working for the affected filters

    [TestMethod]
    public void DeepClone_Limiter_KeepsUserSettings_AndResetsRuntimeState()
    {
        var Local_Source = new Limiter
        {
            FilterEnabled = true,
            Threshold = 0.3125d,
            MaxValue = 0.875d,
            PeakHoldReleaseEnabled = false,
            PeakHoldRelease = 11d,
            PeakHoldAttackEnabled = false,
            PeakHoldAttack = 13d,
            CompressionApplied = 0.5d,
            PeakValue = 0.9d,
            IsBrickwall = true,
        };

        var Local_Clone = CommonFunctions.DeepClone(Local_Source);

        Assert.IsTrue(Local_Clone.FilterEnabled);
        Assert.AreEqual(0.3125d, Local_Clone.Threshold);
        Assert.AreEqual(0.875d, Local_Clone.MaxValue);
        Assert.IsFalse(Local_Clone.PeakHoldReleaseEnabled);
        Assert.AreEqual(11d, Local_Clone.PeakHoldRelease);
        Assert.IsFalse(Local_Clone.PeakHoldAttackEnabled);
        Assert.AreEqual(13d, Local_Clone.PeakHoldAttack);

        //Runtime state deliberately starts fresh on the clone.
        Assert.AreEqual(0d, Local_Clone.CompressionApplied);
        Assert.AreEqual(0d, Local_Clone.PeakValue);
        Assert.IsFalse(Local_Clone.IsBrickwall);
    }

    [TestMethod]
    public void DeepClone_DEQ_KeepsUserSettings_AndResetsRuntimeState()
    {
        var Local_Source = new DEQ
        {
            FilterEnabled = true,
            DEQ_Type = DEQ.DEQType.CutAbove,
            Biquad_Type = DEQ.BiquadType.High_Shelf,
            Threshold_Type = DEQ.ThresholdType.RMS,
            TargetFrequency = 77.5d,
            TargetGain_dB = -6.25d,
            TargetQ = 2.5d,
            TargetSlope = 1.75d,
            Threshold_dB = -33.5d,
            Ratio = 12.5d,
            AttackTime_ms = 22.5d,
            ReleaseTime_ms = 44.5d,
            KneeWidth_dB = 9.5d,
            UseSoftKnee = false,
            GainApplied = 8.5d,
        };

        var Local_Clone = CommonFunctions.DeepClone(Local_Source);

        Assert.IsTrue(Local_Clone.FilterEnabled);
        Assert.AreEqual(DEQ.DEQType.CutAbove, Local_Clone.DEQ_Type);
        Assert.AreEqual(DEQ.BiquadType.High_Shelf, Local_Clone.Biquad_Type);
        Assert.AreEqual(DEQ.ThresholdType.RMS, Local_Clone.Threshold_Type);
        Assert.AreEqual(77.5d, Local_Clone.TargetFrequency);
        Assert.AreEqual(-6.25d, Local_Clone.TargetGain_dB);
        Assert.AreEqual(2.5d, Local_Clone.TargetQ);
        Assert.AreEqual(1.75d, Local_Clone.TargetSlope);
        Assert.AreEqual(-33.5d, Local_Clone.Threshold_dB);
        Assert.AreEqual(12.5d, Local_Clone.Ratio);
        Assert.AreEqual(22.5d, Local_Clone.AttackTime_ms);
        Assert.AreEqual(44.5d, Local_Clone.ReleaseTime_ms);
        Assert.AreEqual(9.5d, Local_Clone.KneeWidth_dB);
        Assert.IsFalse(Local_Clone.UseSoftKnee);

        Assert.AreEqual(0d, Local_Clone.GainApplied);
    }

    [TestMethod]
    public void DeepClone_Mixer_KeepsRouting_AndDropsTheDeviceDerivedChannelName()
    {
        var Local_Source = new Mixer { FilterEnabled = true };
        Local_Source.MixerInputs.Add(new MixerInput
        {
            Enabled = true,
            Attenuation = -3.5d,
            StreamAttenuation = -1.25d,
            ChannelIndex = 7,
            ChannelName = "Live Device Name",
        });
        Local_Source.MixerInputs.Add(new MixerInput
        {
            Enabled = false,
            Attenuation = -9.5d,
            StreamAttenuation = -2.25d,
            ChannelIndex = 9,
            ChannelName = "Another Live Device Name",
        });

        var Local_Clone = CommonFunctions.DeepClone(Local_Source);

        Assert.IsTrue(Local_Clone.FilterEnabled);
        Assert.AreEqual(2, Local_Clone.MixerInputs.Count);

        Assert.IsTrue(Local_Clone.MixerInputs[0].Enabled);
        Assert.AreEqual(-3.5d, Local_Clone.MixerInputs[0].Attenuation);
        Assert.AreEqual(-1.25d, Local_Clone.MixerInputs[0].StreamAttenuation);
        Assert.AreEqual(7, Local_Clone.MixerInputs[0].ChannelIndex);
        Assert.AreEqual(string.Empty, Local_Clone.MixerInputs[0].ChannelName);

        Assert.IsFalse(Local_Clone.MixerInputs[1].Enabled);
        Assert.AreEqual(-9.5d, Local_Clone.MixerInputs[1].Attenuation);
        Assert.AreEqual(-2.25d, Local_Clone.MixerInputs[1].StreamAttenuation);
        Assert.AreEqual(9, Local_Clone.MixerInputs[1].ChannelIndex);
        Assert.AreEqual(string.Empty, Local_Clone.MixerInputs[1].ChannelName);
    }

    #endregion
}
