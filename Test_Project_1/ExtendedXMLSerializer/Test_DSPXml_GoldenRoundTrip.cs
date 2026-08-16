namespace Test_Project_1;

using BassThatHz_ASIO_DSP_Processor;
using BassThatHz_ASIO_DSP_Processor.GUI;
using BassThatHz_ASIO_DSP_Processor.GUI.Tabs;
using ExtendedXmlSerialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

/// <summary>
/// Golden-file coverage for the real DSP.xml load/save contract.
///
/// The app never calls the serializer bare: the SAVE path is
///   Serialize -> RemoveDeprecatedXMLOutputTags -> WriteAllText
/// and the LOAD path is
///   ReadAllText -> RemoveDeprecatedXMLInputTags -> Deserialize&lt;DSP_Info&gt;
/// (see ctl_GeneralConfigPage.btnSaveConfig_Click / btnLoadConfig_Click and
/// FormMain.FormMain_Shown). These tests exercise that exact pipeline.
///
/// The repo copy of DSP.xml is a read-only golden artifact: it is wired into the project
/// as a copied test asset and this fixture NEVER opens it for writing.
/// </summary>
[TestClass]
public class Test_DSPXml_GoldenRoundTrip
{
    #region Helpers

    /// <summary>
    /// Path to the COPY of the golden DSP.xml in the test output directory.
    /// The original under the repo root is never touched.
    /// </summary>
    private static string GoldenAssetPath
        => Path.Combine(AppContext.BaseDirectory, "TestAssets", "DSP.xml");

    private static string ReadGoldenXml()
    {
        Assert.IsTrue(File.Exists(GoldenAssetPath),
            "Golden test asset missing: " + GoldenAssetPath +
            " (expected the DSP.xml <None Include> copy step in Test_Project_1.csproj).");

        return File.ReadAllText(GoldenAssetPath);
    }

    /// <summary>Replays the app's LOAD path on an XML string.</summary>
    private static DSP_Info LoadLikeTheApp(string xml)
    {
        var Local_Cleaned = CommonFunctions.RemoveDeprecatedXMLInputTags(xml);
        return new ExtendedXmlSerializer().Deserialize<DSP_Info>(Local_Cleaned);
    }

    /// <summary>Replays the app's SAVE path (without touching the filesystem).</summary>
    private static string SaveLikeTheApp(DSP_Info info)
    {
        var Local_Xml = new ExtendedXmlSerializer().Serialize(info);
        return CommonFunctions.RemoveDeprecatedXMLOutputTags(Local_Xml);
    }

    /// <summary>
    /// Deep object comparison performed by re-serializing both graphs through the same
    /// serializer and comparing the results. Identical output means every property the
    /// serializer knows about matched.
    /// </summary>
    private static void AssertDeepEqual(DSP_Info expected, DSP_Info actual)
    {
        var Local_Expected = SaveLikeTheApp(expected);
        var Local_Actual = SaveLikeTheApp(actual);
        Assert.AreEqual(Local_Expected, Local_Actual, "The two DSP_Info graphs are not equivalent.");
    }

    /// <summary>
    /// Flattens an XML document to path -> value pairs for leaf elements, disambiguating
    /// same-named siblings by ordinal index.
    /// </summary>
    private static Dictionary<string, string> FlattenLeaves(string xml)
    {
        var Local_Map = new Dictionary<string, string>(StringComparer.Ordinal);
        Flatten(XDocument.Parse(xml).Root!, string.Empty, Local_Map);
        return Local_Map;
    }

    private static void Flatten(XElement element, string prefix, Dictionary<string, string> map)
    {
        var Local_Path = prefix + "/" + element.Name.LocalName;

        var Local_Children = element.Elements().ToList();
        if (Local_Children.Count == 0)
        {
            map[Local_Path] = element.Value.Trim();

            var Local_Type = element.Attribute("type");
            if (Local_Type != null)
                map[Local_Path + "@type"] = Local_Type.Value;

            return;
        }

        var Local_TypeAttr = element.Attribute("type");
        if (Local_TypeAttr != null)
            map[Local_Path + "@type"] = Local_TypeAttr.Value;

        var Local_Counters = new Dictionary<string, int>(StringComparer.Ordinal);
        for (int i = 0; i < Local_Children.Count; i++)
        {
            var Local_Child = Local_Children[i];
            var Local_Name = Local_Child.Name.LocalName;
            Local_Counters.TryGetValue(Local_Name, out int Local_Index);
            Local_Counters[Local_Name] = Local_Index + 1;

            Flatten(Local_Child, Local_Path + "[" + Local_Name + Local_Index + "]", map);
        }
    }

    /// <summary>
    /// Leaf element names that a save/load round trip is ALLOWED to drop, each with the reason.
    /// <para>
    /// This is deliberately an explicit, named list rather than a relaxed comparison: the whole
    /// point of the leaf-by-leaf check is to catch real data loss, so anything NOT named here
    /// still fails, and a leaf that comes back with a different VALUE always fails.
    /// </para>
    /// <list type="bullet">
    /// <item>CompressionApplied / PeakValue / IsBrickwall - Limiter runtime meter state, written
    /// by Transform and reset by ApplySettings; carries [IgnoreDataMember] and is no longer
    /// serialized. Present in configs written by older builds (including this golden file).</item>
    /// <item>GainApplied - DEQ runtime meter state, computed in Transform; same reasoning.</item>
    /// <item>ChannelName - MixerInput's display name, derived from the live ASIO device list
    /// rather than from the config; same reasoning.</item>
    /// <item>PeakHoldDecayEnabled / PeakHoldDecay - Limiter settings removed long ago; already
    /// stripped by CommonFunctions.RemoveDeprecatedXML*Tags.</item>
    /// <item>InputChannelIndex / OutputChannelIndex - legacy stream routing, migrated into
    /// InputSource / OutputDestination by CommonFunctions.FixLegacyChannelIndexMappings and
    /// stripped by RemoveDeprecatedXMLOutputTags.</item>
    /// </list>
    /// </summary>
    private static readonly HashSet<string> s_IntentionallyDroppedLeafNames =
        new(StringComparer.Ordinal)
        {
            "CompressionApplied",
            "PeakValue",
            "IsBrickwall",
            "GainApplied",
            "ChannelName",
            "PeakHoldDecayEnabled",
            "PeakHoldDecay",
            "InputChannelIndex",
            "OutputChannelIndex",
        };

    /// <summary>Extracts the element name from a path produced by <see cref="Flatten"/>.</summary>
    private static string LeafNameOf(string leafPath)
        => leafPath[(leafPath.LastIndexOf('/') + 1)..];

    private static bool IsIntentionallyDroppedLeaf(string leafPath)
        => s_IntentionallyDroppedLeafNames.Contains(LeafNameOf(leafPath));

    private static bool ValuesMatch(string expected, string actual)
    {
        if (string.Equals(expected, actual, StringComparison.Ordinal))
            return true;

        // Numeric tolerance: round-trip formatting of doubles may legitimately differ in
        // the last digit; a dropped/corrupted property will not be within 1e-9 relative.
        if (double.TryParse(expected, System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture, out double Local_E)
            && double.TryParse(actual, System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture, out double Local_A))
        {
            var Local_Scale = Math.Max(1d, Math.Max(Math.Abs(Local_E), Math.Abs(Local_A)));
            return Math.Abs(Local_E - Local_A) <= 1e-9 * Local_Scale;
        }

        return false;
    }

    #endregion

    #region Golden asset integrity

    [TestMethod]
    public void GoldenAsset_IsPresent_AndIsACopy_NotTheRepoOriginal()
    {
        var Local_Xml = ReadGoldenXml();
        Assert.IsTrue(Local_Xml.Contains("<DSP_Info type=\"BassThatHz_ASIO_DSP_Processor.DSP_Info\">", StringComparison.Ordinal));

        // The asset must live under the test output directory, never the repo root.
        Assert.IsTrue(GoldenAssetPath.StartsWith(AppContext.BaseDirectory, StringComparison.OrdinalIgnoreCase));
    }

    #endregion

    #region XML -> object

    [TestMethod]
    public void GoldenDSPXml_LoadPath_Deserializes_WithoutThrowing()
    {
        var Local_Info = LoadLikeTheApp(ReadGoldenXml());
        Assert.IsNotNull(Local_Info);
    }

    [TestMethod]
    public void GoldenDSPXml_LoadPath_PopulatesApplicationAndDeviceSettings()
    {
        var Local_Info = LoadLikeTheApp(ReadGoldenXml());

        Assert.AreEqual(0, Local_Info.StartUpDelay);
        Assert.IsTrue(Local_Info.AutoStartDSP);
        Assert.AreEqual(ProcessPriorityClass.High, Local_Info.ProcessPriority);
        Assert.IsTrue(Local_Info.IsMultiThreadingEnabled);
        Assert.IsTrue(Local_Info.IsBackgroundThreadEnabled);
        Assert.IsTrue(Local_Info.EnableStats);

        Assert.IsFalse(Local_Info.NetworkConfigAPI_Enabled);
        Assert.AreEqual("localhost", Local_Info.NetworkConfigAPI_Host);
        Assert.AreEqual(8080, Local_Info.NetworkConfigAPI_Port);

        Assert.AreEqual("MOTU Pro Audio", Local_Info.ASIO_InputDevice);
        Assert.AreEqual(1d, Local_Info.InMasterVolume);
        Assert.AreEqual(32, Local_Info.InChannelCount);
        Assert.AreEqual(96000, Local_Info.InSampleRate);
        Assert.AreEqual(0, Local_Info.InBitDepth);
        Assert.AreEqual("Hardware Recommended", Local_Info.InBufferSize);

        Assert.AreEqual("MOTU Pro Audio", Local_Info.ASIO_OutputDevice);
        Assert.AreEqual(1d, Local_Info.OutMasterVolume);
        Assert.AreEqual(32, Local_Info.OutChannelCount);
        Assert.AreEqual(0, Local_Info.OutSampleRate);
        Assert.AreEqual(0, Local_Info.OutBitDepth);
        Assert.AreEqual("Hardware Recommended", Local_Info.OutBufferSize);
    }

    [TestMethod]
    public void GoldenDSPXml_LoadPath_PopulatesCollections()
    {
        var Local_Info = LoadLikeTheApp(ReadGoldenXml());

        Assert.IsNotNull(Local_Info.Streams);
        Assert.IsNotNull(Local_Info.Buses);
        Assert.IsNotNull(Local_Info.AbstractBuses);

        Assert.AreEqual(2, Local_Info.Streams.Count);
        Assert.AreEqual(0, Local_Info.Buses.Count);
        Assert.AreEqual(0, Local_Info.AbstractBuses.Count);

        var Local_Left = Local_Info.Streams[0];
        Assert.IsNotNull(Local_Left.InputSource);
        Assert.IsNotNull(Local_Left.OutputDestination);
        Assert.AreEqual("Left Loopback", Local_Left.InputSource.Name);
        Assert.AreEqual(0, Local_Left.InputSource.Index);
        Assert.AreEqual(StreamType.Channel, Local_Left.InputSource.StreamType);
        Assert.AreEqual("Left DSP", Local_Left.OutputDestination.Name);
        Assert.AreEqual(2, Local_Left.OutputDestination.Index);
        Assert.AreEqual(1d, Local_Left.InputVolume);
        Assert.AreEqual(1d, Local_Left.OutputVolume);

        var Local_Right = Local_Info.Streams[1];
        Assert.AreEqual("Right Loopback", Local_Right.InputSource.Name);
        Assert.AreEqual("Right DSP", Local_Right.OutputDestination.Name);
    }

    [TestMethod]
    public void GoldenDSPXml_LoadPath_PreservesFilterPolymorphism()
    {
        var Local_Info = LoadLikeTheApp(ReadGoldenXml());

        for (int s = 0; s < Local_Info.Streams.Count; s++)
        {
            var Local_Filters = Local_Info.Streams[s].Filters;
            Assert.IsNotNull(Local_Filters, "Stream " + s + " has no Filters collection.");
            Assert.AreEqual(7, Local_Filters.Count, "Stream " + s + " filter count.");

            // Concrete types must survive the type="..." attributes.
            Assert.IsInstanceOfType<NAudio.Dsp.BiQuadFilter>(Local_Filters[0]);
            for (int f = 1; f <= 5; f++)
                Assert.IsInstanceOfType<DEQ>(Local_Filters[f], "Stream " + s + " filter " + f);
            Assert.IsInstanceOfType<Limiter>(Local_Filters[6]);
        }
    }

    #endregion

    #region object -> XML -> object

    [TestMethod]
    public void GoldenDSPXml_FullAppCycle_LoadSaveLoad_ProducesEquivalentObject()
    {
        // read -> RemoveDeprecatedXMLInputTags -> Deserialize
        var Local_First = LoadLikeTheApp(ReadGoldenXml());

        // Serialize -> RemoveDeprecatedXMLOutputTags  (the exact save path)
        var Local_SavedXml = SaveLikeTheApp(Local_First);
        Assert.IsFalse(string.IsNullOrWhiteSpace(Local_SavedXml));

        // ...and load it straight back through the load path.
        var Local_Second = LoadLikeTheApp(Local_SavedXml);

        AssertDeepEqual(Local_First, Local_Second);
    }

    [TestMethod]
    public void GoldenDSPXml_FullAppCycle_IsStable_AcrossTwoGenerations()
    {
        var Local_First = LoadLikeTheApp(ReadGoldenXml());
        var Local_Xml1 = SaveLikeTheApp(Local_First);
        var Local_Second = LoadLikeTheApp(Local_Xml1);
        var Local_Xml2 = SaveLikeTheApp(Local_Second);

        Assert.AreEqual(Local_Xml1, Local_Xml2, "Re-saving a reloaded config is not idempotent.");
    }

    [TestMethod]
    public void GoldenDSPXml_ReSerialized_KeepsEveryValueFromTheOriginalFile()
    {
        var Local_GoldenXml = ReadGoldenXml();
        var Local_SavedXml = SaveLikeTheApp(LoadLikeTheApp(Local_GoldenXml));

        // Compared against the RAW golden file - no migration pass on the expected side - so that
        // every leaf the user's real config contains must either come back unchanged, or be named
        // in s_IntentionallyDroppedLeafNames below. A leaf that changes VALUE is always a failure.
        var Local_Expected = FlattenLeaves(Local_GoldenXml);
        var Local_Actual = FlattenLeaves(Local_SavedXml);

        var Local_Problems = new List<string>();
        foreach (var Local_Pair in Local_Expected)
        {
            if (!Local_Actual.TryGetValue(Local_Pair.Key, out var Local_Value))
            {
                if (!IsIntentionallyDroppedLeaf(Local_Pair.Key))
                    Local_Problems.Add("MISSING " + Local_Pair.Key);

                continue;
            }

            if (!ValuesMatch(Local_Pair.Value, Local_Value))
                Local_Problems.Add("CHANGED " + Local_Pair.Key + ": '" + Local_Pair.Value + "' -> '" + Local_Value + "'");
        }

        Assert.AreEqual(0, Local_Problems.Count,
            "Round-trip dropped or corrupted values:" + Environment.NewLine
            + string.Join(Environment.NewLine, Local_Problems.Take(25)));
    }

    [TestMethod]
    public void GoldenDSPXml_TheDropAllowList_IsActuallyExercised_AndStaysMinimal()
    {
        // Guards the allow-list itself: if a future change stops emitting one of these elements
        // into old configs, the entry becomes dead and should be deleted rather than left behind
        // hiding a real regression.
        var Local_Expected = FlattenLeaves(ReadGoldenXml());
        var Local_Actual = FlattenLeaves(SaveLikeTheApp(LoadLikeTheApp(ReadGoldenXml())));

        var Local_Used = new HashSet<string>(StringComparer.Ordinal);
        foreach (var Local_Pair in Local_Expected)
        {
            if (!Local_Actual.ContainsKey(Local_Pair.Key) && IsIntentionallyDroppedLeaf(Local_Pair.Key))
                _ = Local_Used.Add(LeafNameOf(Local_Pair.Key));
        }

        // The golden config was written by a build that still persisted the limiter/DEQ runtime
        // meter state, so exactly these four must be exercised by it.
        CollectionAssert.AreEquivalent(
            new[] { "CompressionApplied", "PeakValue", "IsBrickwall", "GainApplied" },
            Local_Used.ToArray(),
            "The set of intentionally-dropped leaves in the golden config changed: "
            + string.Join(", ", Local_Used.OrderBy(n => n, StringComparer.Ordinal)));
    }

    [TestMethod]
    public void GoldenDSPXml_ReSerialized_KeepsThePolymorphicTypeAttributes()
    {
        var Local_SavedXml = SaveLikeTheApp(LoadLikeTheApp(ReadGoldenXml()));
        var Local_Doc = XDocument.Parse(Local_SavedXml);

        var Local_Types = Local_Doc.Descendants()
                                   .Select(e => e.Attribute("type")?.Value)
                                   .Where(v => v != null)
                                   .ToList();

        Assert.IsTrue(Local_Types.Contains("BassThatHz_ASIO_DSP_Processor.DSP_Info"));
        Assert.IsTrue(Local_Types.Contains("BassThatHz_ASIO_DSP_Processor.DSP_Stream"));
        Assert.IsTrue(Local_Types.Contains("BassThatHz_ASIO_DSP_Processor.StreamItem"));
        Assert.IsTrue(Local_Types.Contains("NAudio.Dsp.BiQuadFilter"));
        Assert.IsTrue(Local_Types.Contains("BassThatHz_ASIO_DSP_Processor.DEQ"));
        Assert.IsTrue(Local_Types.Contains("BassThatHz_ASIO_DSP_Processor.Limiter"));
    }

    [TestMethod]
    public void GoldenDSPXml_DeepClone_RoundTripsThroughTheSameSerializer()
    {
        // CommonFunctions.DeepClone<T> uses this serializer and is on the AbstractBus
        // chain-build path, so it must survive the golden config too.
        var Local_Info = LoadLikeTheApp(ReadGoldenXml());
        var Local_Clone = CommonFunctions.DeepClone(Local_Info.Streams[0]);

        Assert.IsNotNull(Local_Clone);
        Assert.AreNotSame(Local_Info.Streams[0], Local_Clone);
        Assert.AreEqual(7, Local_Clone.Filters.Count);
        Assert.IsInstanceOfType<Limiter>(Local_Clone.Filters[6]);
    }

    #endregion

    #region Save/Load handlers must not block or write under suppression

    [TestMethod]
    public void GeneralConfigPage_SaveHandler_UnderSuppression_DoesNotBlock_AndWritesNoFile()
    {
        var Local_Previous = BassThatHz_ASIO_DSP_Processor.Debug.SuppressInteractiveDialogs;
        try
        {
            BassThatHz_ASIO_DSP_Processor.Debug.SuppressInteractiveDialogs = true;

            var Local_Control = new ctl_GeneralConfigPage();
            var Local_Method = typeof(ctl_GeneralConfigPage).GetMethod("btnSaveConfig_Click",
                                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(Local_Method, "btnSaveConfig_Click not found.");

            var Local_Before = Directory.GetFiles(AppContext.BaseDirectory, "*.xml").Length;

            var Local_Task = System.Threading.Tasks.Task.Run(() =>
                Local_Method!.Invoke(Local_Control, new object?[] { null, EventArgs.Empty }));
            Assert.IsTrue(Local_Task.Wait(TimeSpan.FromSeconds(20)),
                "btnSaveConfig_Click blocked on a modal SaveFileDialog.");

            // Suppressed ShowDialogSafe returns Cancel, so nothing may be written.
            var Local_After = Directory.GetFiles(AppContext.BaseDirectory, "*.xml").Length;
            Assert.AreEqual(Local_Before, Local_After, "The save handler wrote a file while suppressed.");
        }
        finally
        {
            BassThatHz_ASIO_DSP_Processor.Debug.SuppressInteractiveDialogs = Local_Previous;
        }
    }

    [TestMethod]
    public void GeneralConfigPage_LoadHandler_UnderSuppression_DoesNotBlock()
    {
        var Local_Previous = BassThatHz_ASIO_DSP_Processor.Debug.SuppressInteractiveDialogs;
        try
        {
            BassThatHz_ASIO_DSP_Processor.Debug.SuppressInteractiveDialogs = true;

            var Local_Control = new ctl_GeneralConfigPage();
            var Local_Method = typeof(ctl_GeneralConfigPage).GetMethod("btnLoadConfig_Click",
                                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(Local_Method, "btnLoadConfig_Click not found.");

            var Local_Task = System.Threading.Tasks.Task.Run(() =>
                Local_Method!.Invoke(Local_Control, new object?[] { null, EventArgs.Empty }));
            Assert.IsTrue(Local_Task.Wait(TimeSpan.FromSeconds(20)),
                "btnLoadConfig_Click blocked on a modal OpenFileDialog.");
        }
        finally
        {
            BassThatHz_ASIO_DSP_Processor.Debug.SuppressInteractiveDialogs = Local_Previous;
        }
    }

    #endregion

    #region RemoveDeprecatedXML*Tags behaviour pins

    [TestMethod]
    public void RemoveDeprecatedXMLInputTags_StripsLimiterPeakHoldTags_AndKeepsEverythingElse()
    {
        const string Local_Xml =
            "<DSP_Info><Streams><DSP_Stream><Filters><Limiter>"
            + "<FilterEnabled>True</FilterEnabled>"
            + "<PeakHoldDecayEnabled>True</PeakHoldDecayEnabled>"
            + "<PeakHoldDecay>5</PeakHoldDecay>"
            + "<Threshold>-3</Threshold>"
            + "</Limiter></Filters></DSP_Stream></Streams></DSP_Info>";

        var Local_Result = CommonFunctions.RemoveDeprecatedXMLInputTags(Local_Xml);

        Assert.IsFalse(Local_Result.Contains("PeakHoldDecayEnabled", StringComparison.Ordinal));
        Assert.IsFalse(Local_Result.Contains("PeakHoldDecay<", StringComparison.Ordinal));
        Assert.IsTrue(Local_Result.Contains("<FilterEnabled>True</FilterEnabled>", StringComparison.Ordinal));
        Assert.IsTrue(Local_Result.Contains("<Threshold>-3</Threshold>", StringComparison.Ordinal));
    }

    [TestMethod]
    public void RemoveDeprecatedXMLOutputTags_AlsoStripsStreamChannelIndexTags()
    {
        const string Local_Xml =
            "<DSP_Info><Streams><DSP_Stream>"
            + "<InputChannelIndex>1</InputChannelIndex>"
            + "<OutputChannelIndex>2</OutputChannelIndex>"
            + "<InputVolume>1</InputVolume>"
            + "<Filters><Limiter><PeakHoldDecay>5</PeakHoldDecay></Limiter></Filters>"
            + "</DSP_Stream></Streams></DSP_Info>";

        var Local_Result = CommonFunctions.RemoveDeprecatedXMLOutputTags(Local_Xml);

        Assert.IsFalse(Local_Result.Contains("InputChannelIndex", StringComparison.Ordinal));
        Assert.IsFalse(Local_Result.Contains("OutputChannelIndex", StringComparison.Ordinal));
        Assert.IsFalse(Local_Result.Contains("PeakHoldDecay", StringComparison.Ordinal));
        Assert.IsTrue(Local_Result.Contains("<InputVolume>1</InputVolume>", StringComparison.Ordinal));
    }

    [TestMethod]
    public void RemoveDeprecatedXMLTags_AreIdempotent_OnTheGoldenFile()
    {
        var Local_Xml = ReadGoldenXml();

        var Local_Once = CommonFunctions.RemoveDeprecatedXMLInputTags(Local_Xml);
        var Local_Twice = CommonFunctions.RemoveDeprecatedXMLInputTags(Local_Once);
        Assert.AreEqual(Local_Once, Local_Twice);

        var Local_OutOnce = CommonFunctions.RemoveDeprecatedXMLOutputTags(Local_Xml);
        var Local_OutTwice = CommonFunctions.RemoveDeprecatedXMLOutputTags(Local_OutOnce);
        Assert.AreEqual(Local_OutOnce, Local_OutTwice);
    }

    #endregion
}
