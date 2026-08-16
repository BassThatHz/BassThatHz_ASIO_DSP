#nullable enable

namespace Test_Project_1;

using BassThatHz_ASIO_DSP_Processor;
using BassThatHz_ASIO_DSP_Processor.DSP.Filters;
using ExtendedXmlSerialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NAudio.Dsp;

#region Usings
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
#endregion

/// <summary>
/// Proves that a user's filter settings survive a config save/load for EVERY filter.
///
/// <para>
/// The golden DSP.xml only exercises 3 of the 17 filters (BiQuadFilter, DEQ, Limiter), so the
/// remaining 14 had no round-trip coverage at all. Losing a setting here is silent - the user
/// saves a config, reloads it, and their tuning is quietly back at a default.
/// </para>
///
/// <para>
/// The tests deliberately drive the REAL app pipeline rather than the bare serializer:
/// <c>Serialize</c> -&gt; <c>RemoveDeprecatedXMLOutputTags</c> -&gt;
/// <c>RemoveDeprecatedXMLInputTags</c> -&gt; <c>Deserialize</c>, matching
/// <c>ctl_GeneralConfigPage.btnSaveConfig_Click</c> / <c>btnLoadConfig_Click</c> and
/// <c>FormMain.FormMain_Shown</c>.
/// </para>
///
/// <para>
/// Members are enumerated by REFLECTION, and every one of them is first set to a distinctive
/// NON-DEFAULT value, so a member added later is covered automatically and a test that
/// round-trips defaults cannot pass by accident. Members the serializer intentionally skips are
/// pinned by the explicit, justified allow-list in <see cref="s_ExpectedSkippedMembers"/>; that
/// list is compared for EQUALITY, so adding or removing a skipped member fails the suite.
/// </para>
/// </summary>
[TestClass]
public class Test_Filter_SerializationRoundTrip
{
    #region The 17 filters

    /// <summary>Every filter type that can appear in <c>DSP_Stream.Filters</c>.</summary>
    public static IEnumerable<object[]> AllFilterTypes =>
    [
        [typeof(AntiDC)],
        [typeof(AuxGet)],
        [typeof(AuxSet)],
        [typeof(Basic_HPF_LPF)],
        [typeof(BiQuadFilter)],
        [typeof(ClassicLimiter)],
        [typeof(Delay)],
        [typeof(DEQ)],
        [typeof(DynamicRangeCompressor)],
        [typeof(FIR)],
        [typeof(Floor)],
        [typeof(GPEQ)],
        [typeof(Limiter)],
        [typeof(Mixer)],
        [typeof(Polarity)],
        [typeof(SmartGain)],
        [typeof(ULF_FIR)],
    ];

    #endregion

    #region Intentional exclusions (allow-list)

    /// <summary>
    /// Public members the serializer does NOT persist, per declaring type, with the reason.
    ///
    /// <para>
    /// Three categories, all accepted as-is:
    /// </para>
    /// <list type="number">
    /// <item><b>Read-only <c>{ get; }</c> identity members</b> - <c>FilterType</c>,
    /// <c>FilterProcessingType</c> and <c>GetFilter</c>. These are per-type constants, not user
    /// settings; the concrete type is already carried by the <c>type="..."</c> XML attribute.
    /// (<c>BiQuadFilter.FilterType</c> is the one exception - it has a public setter and IS
    /// persisted, because the biquad's shape really is user-selected.)</item>
    /// <item><b>Computed runtime telemetry</b> - <c>SmartGain</c>'s six
    /// <c>{ get; protected set; }</c> doubles, and the members carrying
    /// <c>[IgnoreDataMember]</c> / <c>[XmlIgnore]</c> (limiter + compressor meters, DEQ gain,
    /// MixerInput's device-derived channel name). All written by <c>Transform</c> and only read
    /// back by the meter UI.</item>
    /// <item><b><c>readonly</c> coefficient caches</b> - <c>Basic_HPF_LPF</c>'s Q arrays and
    /// biquad bank, recomputed from HPFFreq/HPFFilter/LPFFreq/LPFFilter by
    /// <c>ApplySettings</c>.</item>
    /// </list>
    /// </summary>
    private static readonly Dictionary<Type, string[]> s_ExpectedSkippedMembers = new()
    {
        //Read-only identity members only.
        [typeof(AntiDC)] = ["FilterType", "FilterProcessingType", "GetFilter"],
        [typeof(AuxGet)] = ["FilterType", "FilterProcessingType", "GetFilter"],
        [typeof(AuxSet)] = ["FilterType", "FilterProcessingType", "GetFilter"],
        [typeof(Delay)] = ["FilterType", "FilterProcessingType", "GetFilter"],
        [typeof(FIR)] = ["FilterType", "FilterProcessingType", "GetFilter"],
        [typeof(Floor)] = ["FilterType", "FilterProcessingType", "GetFilter"],
        [typeof(GPEQ)] = ["FilterType", "FilterProcessingType", "GetFilter"],
        [typeof(Mixer)] = ["FilterType", "FilterProcessingType", "GetFilter"],
        [typeof(Polarity)] = ["FilterType", "FilterProcessingType", "GetFilter"],
        [typeof(ULF_FIR)] = ["FilterType", "FilterProcessingType", "GetFilter"],

        //BiQuadFilter.FilterType has a PUBLIC setter, so it must round-trip and is NOT skipped.
        [typeof(BiQuadFilter)] = ["FilterProcessingType", "GetFilter"],

        //readonly coefficient caches, recomputed by ApplySettings.
        [typeof(Basic_HPF_LPF)] =
        [
            "FilterType", "FilterProcessingType", "GetFilter",
            "Q_Array_HPF", "Q_Array_LPF", "BiQuads",
        ],

        //Compressor meter reading, [XmlIgnore] + [IgnoreDataMember].
        [typeof(ClassicLimiter)] =
        [
            "FilterType", "FilterProcessingType", "GetFilter",
            "CompressionApplied",
        ],
        [typeof(DynamicRangeCompressor)] =
        [
            "FilterType", "FilterProcessingType", "GetFilter",
            "CompressionApplied",
        ],

        //DEQ meter reading, [IgnoreDataMember].
        [typeof(DEQ)] =
        [
            "FilterType", "FilterProcessingType", "GetFilter",
            "GainApplied",
        ],

        //Limiter meter readings, [IgnoreDataMember]; reset by ApplySettings and the FilterEnabled setter.
        [typeof(Limiter)] =
        [
            "FilterType", "FilterProcessingType", "GetFilter",
            "CompressionApplied", "PeakValue", "IsBrickwall",
        ],

        //Computed gain telemetry, { get; protected set; }.
        [typeof(SmartGain)] =
        [
            "FilterType", "FilterProcessingType", "GetFilter",
            "PeakLevelLinear", "InputAbs", "HeadroomLinear",
            "ActualGainLinear", "ActualGaindB", "MaxAllowedLinearGain",
        ],

        //Nested value type carried by Mixer.MixerInputs. ChannelName is rebuilt from the live
        //ASIO device channel list every time the mixer UI is opened.
        [typeof(MixerInput)] = ["ChannelName"],
    };

    /// <summary>
    /// Concrete stand-ins used when a member's declared element type is an interface or an
    /// abstract type that cannot be instantiated directly.
    /// </summary>
    private static readonly Dictionary<Type, Type> s_ConcreteSubstitutes = new()
    {
        [typeof(IFilter)] = typeof(BiQuadFilter),
        [typeof(IStreamItem)] = typeof(StreamItem),
    };

    #endregion

    #region Member classification

    private static bool IsIgnoredByAttribute(MemberInfo member)
    {
        var Local_Attributes = member.GetCustomAttributes(false);
        for (int Local_i = 0; Local_i < Local_Attributes.Length; Local_i++)
        {
            var Local_Attribute = Local_Attributes[Local_i];
            if (Local_Attribute is System.Xml.Serialization.XmlIgnoreAttribute
                || Local_Attribute is System.Runtime.Serialization.IgnoreDataMemberAttribute
                || Local_Attribute is NonSerializedAttribute)
            {
                return true;
            }
        }
        return false;
    }

    private static bool IsCollectionType(Type type)
        => typeof(IEnumerable).IsAssignableFrom(type) && type != typeof(string);

    /// <summary>
    /// Members that MUST survive a round trip: public fields that are neither <c>const</c> nor
    /// <c>readonly</c>, and public properties with a public setter (or a read-only getter that
    /// hands back a mutable collection, which the serializer also persists).
    /// </summary>
    private static List<MemberInfo> GetPersistableMembers(Type type)
    {
        var Local_Result = new List<MemberInfo>();

        foreach (var Local_Property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (Local_Property.GetIndexParameters().Length > 0)
                continue;
            if (IsIgnoredByAttribute(Local_Property))
                continue;

            if (HasPublicSetter(Local_Property)
                || (Local_Property.CanRead && IsCollectionType(Local_Property.PropertyType)))
            {
                Local_Result.Add(Local_Property);
            }
        }

        //BindingFlags.Instance already excludes const fields (they are static).
        foreach (var Local_Field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
        {
            if (Local_Field.IsInitOnly || IsIgnoredByAttribute(Local_Field))
                continue;

            Local_Result.Add(Local_Field);
        }

        return Local_Result;
    }

    /// <summary>Public members the serializer skips - must match the documented allow-list.</summary>
    private static List<string> GetSkippedMembers(Type type)
    {
        var Local_Persisted = GetPersistableMembers(type).Select(m => m.Name).ToHashSet(StringComparer.Ordinal);
        var Local_Result = new List<string>();

        foreach (var Local_Property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (Local_Property.GetIndexParameters().Length > 0)
                continue;
            if (!Local_Persisted.Contains(Local_Property.Name))
                Local_Result.Add(Local_Property.Name);
        }

        foreach (var Local_Field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!Local_Persisted.Contains(Local_Field.Name))
                Local_Result.Add(Local_Field.Name);
        }

        return Local_Result;
    }

    private static bool HasPublicSetter(PropertyInfo property)
        => property.CanWrite && property.GetSetMethod(true) is { IsPublic: true };

    private static Type MemberValueType(MemberInfo member)
        => member is PropertyInfo Local_Property ? Local_Property.PropertyType : ((FieldInfo)member).FieldType;

    private static object? GetMemberValue(MemberInfo member, object target)
        => member is PropertyInfo Local_Property ? Local_Property.GetValue(target) : ((FieldInfo)member).GetValue(target);

    private static void SetMemberValue(MemberInfo member, object target, object? value)
    {
        if (member is FieldInfo Local_Field)
        {
            Local_Field.SetValue(target, value);
            return;
        }

        var Local_Property = (PropertyInfo)member;
        if (HasPublicSetter(Local_Property))
        {
            Local_Property.SetValue(target, value);
            return;
        }

        //Read-only collection getter: fill the existing instance instead of replacing it.
        if (Local_Property.GetValue(target) is IList Local_Existing && value is IEnumerable Local_Items)
        {
            Local_Existing.Clear();
            foreach (var Local_Item in Local_Items)
                _ = Local_Existing.Add(Local_Item);
            return;
        }

        Assert.Fail("Cannot assign a distinctive value to " + Local_Property.DeclaringType?.Name
                    + "." + Local_Property.Name + " - extend the test harness.");
    }

    #endregion

    #region Distinctive value generation

    /// <summary>
    /// Sets every persistable member of <paramref name="target"/> to a distinctive, non-default
    /// value. Round-tripping defaults would prove nothing, so every member is moved.
    /// </summary>
    private static void PopulateDistinctly(object target, ref int seed, int depth = 0)
    {
        Assert.IsTrue(depth < 6, "Object graph nested deeper than expected while populating "
                                 + target.GetType().Name + ".");

        foreach (var Local_Member in GetPersistableMembers(target.GetType()))
        {
            var Local_Type = MemberValueType(Local_Member);
            var Local_Current = GetMemberValue(Local_Member, target);
            var Local_Value = MakeDistinctValue(Local_Type, Local_Current, Local_Member.Name, ref seed, depth);
            SetMemberValue(Local_Member, target, Local_Value);
        }
    }

    private static object MakeDistinctValue(Type type, object? current, string name, ref int seed, int depth)
    {
        seed++;
        int Local_Seed = seed;

        if (type == typeof(bool))
            return !(current is bool Local_Bool && Local_Bool);
        if (type == typeof(string))
            return "RT_" + name + "_" + Local_Seed;
        if (type == typeof(double))
            return 1000d + Local_Seed + 0.5d;
        if (type == typeof(float))
            return 1000f + Local_Seed + 0.5f;
        if (type == typeof(decimal))
            return 1000m + Local_Seed + 0.5m;
        if (type == typeof(int))
            return 1000 + Local_Seed;
        if (type == typeof(long))
            return 1000L + Local_Seed;
        if (type == typeof(short))
            return (short)(1000 + Local_Seed);
        if (type == typeof(byte))
            return (byte)(100 + Local_Seed);
        if (type == typeof(TimeSpan))
            return TimeSpan.FromMilliseconds(1000 + Local_Seed);
        if (type == typeof(DateTime))
            return new DateTime(2001, 2, 3, 4, 5, 6, DateTimeKind.Utc).AddSeconds(Local_Seed);

        if (type.IsEnum)
        {
            foreach (var Local_EnumValue in Enum.GetValues(type))
            {
                if (!Equals(Local_EnumValue, current))
                    return Local_EnumValue!;
            }
            return Enum.GetValues(type).GetValue(0)!;
        }

        if (type.IsArray)
        {
            var Local_ElementType = type.GetElementType()!;
            var Local_Array = Array.CreateInstance(Local_ElementType, 5);
            for (int Local_i = 0; Local_i < Local_Array.Length; Local_i++)
                Local_Array.SetValue(MakeDistinctValue(Local_ElementType, null, name + Local_i, ref seed, depth + 1), Local_i);

            return Local_Array;
        }

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
        {
            var Local_ElementType = type.GetGenericArguments()[0];
            var Local_List = (IList)Activator.CreateInstance(type)!;
            for (int Local_i = 0; Local_i < 3; Local_i++)
                _ = Local_List.Add(MakeDistinctValue(Local_ElementType, null, name + Local_i, ref seed, depth + 1));

            return Local_List;
        }

        //Complex object: instantiate (substituting a concrete type for interfaces) and recurse.
        var Local_Concrete = s_ConcreteSubstitutes.TryGetValue(type, out var Local_Substitute) ? Local_Substitute : type;
        Assert.IsFalse(Local_Concrete.IsAbstract || Local_Concrete.IsInterface,
            "No concrete substitute registered for member type " + type.FullName
            + " (member '" + name + "') - extend s_ConcreteSubstitutes.");

        var Local_Instance = Activator.CreateInstance(Local_Concrete);
        Assert.IsNotNull(Local_Instance, "Could not instantiate " + Local_Concrete.FullName
                                         + " for member '" + name + "'.");
        PopulateDistinctly(Local_Instance!, ref seed, depth + 1);
        return Local_Instance!;
    }

    private static object CreatePopulatedFilter(Type filterType)
    {
        var Local_Filter = Activator.CreateInstance(filterType);
        Assert.IsNotNull(Local_Filter, "Could not instantiate " + filterType.FullName + ".");

        int Local_Seed = filterType.Name.Length;
        PopulateDistinctly(Local_Filter!, ref Local_Seed);
        return Local_Filter!;
    }

    #endregion

    #region The real app pipeline

    /// <summary>
    /// SAVE then LOAD exactly as the app does it:
    /// Serialize -&gt; RemoveDeprecatedXMLOutputTags -&gt; RemoveDeprecatedXMLInputTags -&gt; Deserialize.
    /// </summary>
    private static object RoundTripLikeTheApp(object source)
    {
        var Local_Xml = new ExtendedXmlSerializer().Serialize(source);
        Local_Xml = CommonFunctions.RemoveDeprecatedXMLOutputTags(Local_Xml);
        Local_Xml = CommonFunctions.RemoveDeprecatedXMLInputTags(Local_Xml);

        var Local_Result = new ExtendedXmlSerializer().Deserialize(Local_Xml, source.GetType());
        Assert.IsNotNull(Local_Result, "Deserialize returned null for " + source.GetType().Name + ".");
        return Local_Result;
    }

    #endregion

    #region Deep comparison

    private static void CompareMembers(object expected, object actual, string path, List<string> problems)
    {
        foreach (var Local_Member in GetPersistableMembers(expected.GetType()))
        {
            var Local_Expected = GetMemberValue(Local_Member, expected);
            var Local_Actual = GetMemberValue(Local_Member, actual);
            CompareValues(Local_Expected, Local_Actual, path + "." + Local_Member.Name, problems);
        }
    }

    private static void CompareValues(object? expected, object? actual, string path, List<string> problems)
    {
        if (expected is null && actual is null)
            return;

        if (expected is null || actual is null)
        {
            problems.Add(path + ": expected '" + Describe(expected) + "' but round-tripped to '" + Describe(actual) + "'");
            return;
        }

        var Local_Type = expected.GetType();
        if (Local_Type != actual.GetType())
        {
            problems.Add(path + ": runtime type changed from " + Local_Type.FullName + " to " + actual.GetType().FullName);
            return;
        }

        if (Local_Type.IsPrimitive || Local_Type.IsEnum || Local_Type == typeof(string)
            || Local_Type == typeof(decimal) || Local_Type == typeof(TimeSpan)
            || Local_Type == typeof(DateTime) || Local_Type == typeof(Guid))
        {
            if (!Equals(expected, actual))
                problems.Add(path + ": expected '" + Describe(expected) + "' but round-tripped to '" + Describe(actual) + "'");

            return;
        }

        if (expected is IEnumerable Local_ExpectedItems && actual is IEnumerable Local_ActualItems)
        {
            var Local_ExpectedList = Local_ExpectedItems.Cast<object?>().ToList();
            var Local_ActualList = Local_ActualItems.Cast<object?>().ToList();

            if (Local_ExpectedList.Count != Local_ActualList.Count)
            {
                problems.Add(path + ": collection length changed from " + Local_ExpectedList.Count
                             + " to " + Local_ActualList.Count);
                return;
            }

            for (int Local_i = 0; Local_i < Local_ExpectedList.Count; Local_i++)
                CompareValues(Local_ExpectedList[Local_i], Local_ActualList[Local_i], path + "[" + Local_i + "]", problems);

            return;
        }

        CompareMembers(expected, actual, path, problems);
    }

    private static string Describe(object? value)
        => value switch
        {
            null => "<null>",
            double Local_Double => Local_Double.ToString("R", System.Globalization.CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty,
        };

    private static void AssertNoProblems(Type filterType, string scenario, List<string> problems)
    {
        Assert.AreEqual(0, problems.Count,
            "DATA LOSS: " + filterType.Name + " lost user settings during " + scenario + ":"
            + Environment.NewLine + string.Join(Environment.NewLine, problems));
    }

    #endregion

    #region Round trip through the real save/load pipeline

    [TestMethod]
    [DynamicData(nameof(AllFilterTypes))]
    public void Filter_RoundTripsThroughTheAppPipeline_WithoutLosingPublicMembers(Type filterType)
    {
        var Local_Source = CreatePopulatedFilter(filterType);
        var Local_RoundTripped = RoundTripLikeTheApp(Local_Source);

        Assert.AreNotSame(Local_Source, Local_RoundTripped,
            filterType.Name + ": the round trip returned the source instance, so nothing was proven.");

        var Local_Problems = new List<string>();
        CompareMembers(Local_Source, Local_RoundTripped, filterType.Name, Local_Problems);
        AssertNoProblems(filterType, "a save/load round trip", Local_Problems);
    }

    /// <summary>
    /// The same filter in the shape it is ACTUALLY persisted in:
    /// DSP_Info -&gt; DSP_Stream.Filters -&gt; filter. This exercises the polymorphic
    /// <c>type="..."</c> resolution that the flat round trip does not.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(AllFilterTypes))]
    public void Filter_NestedInsideDSP_Info_RoundTripsWithPolymorphicTypeResolution(Type filterType)
    {
        var Local_Source = (IFilter)CreatePopulatedFilter(filterType);

        var Local_Info = new DSP_Info();
        var Local_Stream = new DSP_Stream();
        Local_Stream.Filters.Add(Local_Source);
        Local_Info.Streams.Add(Local_Stream);

        var Local_RoundTrippedInfo = (DSP_Info)RoundTripLikeTheApp(Local_Info);

        Assert.AreEqual(1, Local_RoundTrippedInfo.Streams.Count, filterType.Name + ": stream count changed.");
        Assert.AreEqual(1, Local_RoundTrippedInfo.Streams[0].Filters.Count, filterType.Name + ": filter count changed.");

        var Local_RoundTripped = Local_RoundTrippedInfo.Streams[0].Filters[0];
        Assert.IsInstanceOfType(Local_RoundTripped, filterType,
            filterType.Name + ": concrete filter type was not preserved by the type=\"...\" attribute.");

        var Local_Problems = new List<string>();
        CompareMembers(Local_Source, Local_RoundTripped, filterType.Name, Local_Problems);
        AssertNoProblems(filterType, "a nested DSP_Info save/load round trip", Local_Problems);
    }

    /// <summary>
    /// <c>CommonFunctions.DeepClone</c> uses the same serializer with NO migration step and backs
    /// <c>ASIO_Engine.CloneAbstractBusStream</c> and every filter's <c>IFilter.DeepClone()</c>.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(AllFilterTypes))]
    public void Filter_DeepClone_PreservesEveryPublicMember(Type filterType)
    {
        var Local_Source = (IFilter)CreatePopulatedFilter(filterType);
        var Local_Clone = Local_Source.DeepClone();

        Assert.IsNotNull(Local_Clone, filterType.Name + ": DeepClone returned null.");
        Assert.AreNotSame(Local_Source, Local_Clone, filterType.Name + ": DeepClone returned the same instance.");
        Assert.IsInstanceOfType(Local_Clone, filterType, filterType.Name + ": DeepClone changed the concrete type.");

        var Local_Problems = new List<string>();
        CompareMembers(Local_Source, Local_Clone, filterType.Name, Local_Problems);
        AssertNoProblems(filterType, "CommonFunctions.DeepClone", Local_Problems);
    }

    #endregion

    #region The allow-list itself

    [TestMethod]
    [DynamicData(nameof(AllFilterTypes))]
    public void Filter_SkippedMembers_MatchTheDocumentedAllowList(Type filterType)
    {
        Assert.IsTrue(s_ExpectedSkippedMembers.ContainsKey(filterType),
            filterType.Name + " has no documented exclusion list - add one to s_ExpectedSkippedMembers.");

        var Local_Actual = GetSkippedMembers(filterType);

        CollectionAssert.AreEquivalent(
            s_ExpectedSkippedMembers[filterType],
            Local_Actual,
            filterType.Name + ": the set of members the serializer skips changed." + Environment.NewLine
            + "Documented: " + string.Join(", ", s_ExpectedSkippedMembers[filterType].OrderBy(n => n, StringComparer.Ordinal)) + Environment.NewLine
            + "Actual:     " + string.Join(", ", Local_Actual.OrderBy(n => n, StringComparer.Ordinal)) + Environment.NewLine
            + "A NEW entry here means a user setting silently stopped being saved.");
    }

    [TestMethod]
    [DynamicData(nameof(AllFilterTypes))]
    public void Filter_SkippedMembers_AreReallyAbsentFromTheSavedXml(Type filterType)
    {
        var Local_Source = CreatePopulatedFilter(filterType);
        var Local_Xml = CommonFunctions.RemoveDeprecatedXMLOutputTags(new ExtendedXmlSerializer().Serialize(Local_Source));

        //Only the filter's OWN elements - nested filters (e.g. GPEQ's biquads) legitimately
        //carry their own copies of some of these names.
        var Local_OwnElements = XDocument.Parse(Local_Xml).Root!
                                         .Elements()
                                         .Select(e => e.Name.LocalName)
                                         .ToHashSet(StringComparer.Ordinal);

        foreach (var Local_Skipped in s_ExpectedSkippedMembers[filterType])
        {
            Assert.IsFalse(Local_OwnElements.Contains(Local_Skipped),
                filterType.Name + ": <" + Local_Skipped + "> is documented as skipped but was written to the config.");
        }

        //...and everything that is NOT skipped must be present.
        foreach (var Local_Member in GetPersistableMembers(filterType))
        {
            Assert.IsTrue(Local_OwnElements.Contains(Local_Member.Name),
                filterType.Name + ": <" + Local_Member.Name + "> is a user setting but was not written to the config.");
        }
    }

    #endregion

    #region MixerInput (the one nested settings type)

    [TestMethod]
    public void MixerInput_SkippedMembers_MatchTheDocumentedAllowList()
    {
        var Local_Actual = GetSkippedMembers(typeof(MixerInput));

        CollectionAssert.AreEquivalent(
            s_ExpectedSkippedMembers[typeof(MixerInput)],
            Local_Actual,
            "MixerInput: the set of members the serializer skips changed. Actual: "
            + string.Join(", ", Local_Actual.OrderBy(n => n, StringComparer.Ordinal)));
    }

    [TestMethod]
    public void Mixer_KeepsEveryRoutingRow_InOrder_AcrossARoundTrip()
    {
        var Local_Source = new Mixer { FilterEnabled = true };
        for (int Local_i = 0; Local_i < 8; Local_i++)
        {
            Local_Source.MixerInputs.Add(new MixerInput
            {
                Enabled = (Local_i % 2) == 0,
                Attenuation = -1.5d - Local_i,
                StreamAttenuation = -20.25d - Local_i,
                ChannelIndex = 100 + Local_i,
                ChannelName = "Device Channel " + Local_i,
            });
        }

        var Local_RoundTripped = (Mixer)RoundTripLikeTheApp(Local_Source);

        Assert.AreEqual(8, Local_RoundTripped.MixerInputs.Count, "Mixer routing rows were lost.");
        for (int Local_i = 0; Local_i < 8; Local_i++)
        {
            var Local_Expected = Local_Source.MixerInputs[Local_i];
            var Local_Actual = Local_RoundTripped.MixerInputs[Local_i];

            Assert.AreEqual(Local_Expected.Enabled, Local_Actual.Enabled, "Row " + Local_i + " Enabled.");
            Assert.AreEqual(Local_Expected.Attenuation, Local_Actual.Attenuation, "Row " + Local_i + " Attenuation.");
            Assert.AreEqual(Local_Expected.StreamAttenuation, Local_Actual.StreamAttenuation, "Row " + Local_i + " StreamAttenuation.");
            Assert.AreEqual(Local_Expected.ChannelIndex, Local_Actual.ChannelIndex, "Row " + Local_i + " ChannelIndex.");
        }
    }

    #endregion

    #region Collection-heavy filters get explicit, order-sensitive coverage

    [TestMethod]
    public void FIR_Taps_SurviveAsExactValuesInOrder()
    {
        var Local_Source = new FIR { FilterEnabled = true, FFTSize = 4096 };
        var Local_Taps = new double[257];
        for (int Local_i = 0; Local_i < Local_Taps.Length; Local_i++)
            Local_Taps[Local_i] = Math.Sin(Local_i * 0.031d) * 0.75d;

        Local_Source.Taps = Local_Taps;

        var Local_RoundTripped = (FIR)RoundTripLikeTheApp(Local_Source);

        Assert.AreEqual(4096, Local_RoundTripped.FFTSize);
        Assert.IsNotNull(Local_RoundTripped.Taps, "FIR taps were dropped entirely.");
        Assert.AreEqual(Local_Taps.Length, Local_RoundTripped.Taps!.Length, "FIR tap count changed.");
        for (int Local_i = 0; Local_i < Local_Taps.Length; Local_i++)
            Assert.AreEqual(Local_Taps[Local_i], Local_RoundTripped.Taps[Local_i], "FIR tap " + Local_i + " changed.");
    }

    [TestMethod]
    public void ULF_FIR_Taps_SurviveAsExactValuesInOrder()
    {
        var Local_Source = new ULF_FIR
        {
            FilterEnabled = true,
            FFTSize = 2048,
            TapsSampleRateIndex = 3,
            TapsSampleRate = 480,
        };
        var Local_Taps = new double[129];
        for (int Local_i = 0; Local_i < Local_Taps.Length; Local_i++)
            Local_Taps[Local_i] = Math.Cos(Local_i * 0.017d) * 0.5d;

        Local_Source.Taps = Local_Taps;

        var Local_RoundTripped = (ULF_FIR)RoundTripLikeTheApp(Local_Source);

        Assert.AreEqual(2048, Local_RoundTripped.FFTSize);
        Assert.AreEqual(3, Local_RoundTripped.TapsSampleRateIndex);
        Assert.AreEqual(480, Local_RoundTripped.TapsSampleRate);
        Assert.IsNotNull(Local_RoundTripped.Taps, "ULF_FIR taps were dropped entirely.");
        Assert.AreEqual(Local_Taps.Length, Local_RoundTripped.Taps!.Length, "ULF_FIR tap count changed.");
        for (int Local_i = 0; Local_i < Local_Taps.Length; Local_i++)
            Assert.AreEqual(Local_Taps[Local_i], Local_RoundTripped.Taps[Local_i], "ULF_FIR tap " + Local_i + " changed.");
    }

    [TestMethod]
    public void GPEQ_Bands_SurviveWithTheirTypeCountAndOrder()
    {
        var Local_Source = new GPEQ { FilterEnabled = true };
        for (int Local_i = 0; Local_i < 10; Local_i++)
        {
            Local_Source.Filters.Add(new BiQuadFilter
            {
                FilterEnabled = (Local_i % 2) == 0,
                FilterType = FilterTypes.PEQ,
                BiQuadFilterType = BiQuadFilter.BiQuadFilterTypes.PEQ,
                SampleRate = 96000d,
                Frequency = 40d + (Local_i * 13.5d),
                Q = 0.7d + (Local_i * 0.05d),
                Slope = 1d + (Local_i * 0.25d),
                Gain = -6d + Local_i,
                a0 = 1.1d + Local_i,
                a1 = 1.2d + Local_i,
                a2 = 1.3d + Local_i,
                a3 = 1.4d + Local_i,
                a4 = 1.5d + Local_i,
                aa0 = 2.1d + Local_i,
                aa1 = 2.2d + Local_i,
                aa2 = 2.3d + Local_i,
                b0 = 3.1d + Local_i,
                b1 = 3.2d + Local_i,
                b2 = 3.3d + Local_i,
            });
        }

        var Local_RoundTripped = (GPEQ)RoundTripLikeTheApp(Local_Source);

        Assert.AreEqual(10, Local_RoundTripped.Filters.Count, "GPEQ bands were lost.");

        var Local_Problems = new List<string>();
        CompareValues(Local_Source.Filters, Local_RoundTripped.Filters, "GPEQ.Filters", Local_Problems);
        AssertNoProblems(typeof(GPEQ), "a save/load round trip of its bands", Local_Problems);
    }

    [TestMethod]
    public void BiQuadFilter_Coefficients_SurviveExactly()
    {
        var Local_Source = new BiQuadFilter
        {
            FilterEnabled = true,
            FilterType = FilterTypes.PEQ,
            BiQuadFilterType = BiQuadFilter.BiQuadFilterTypes.HS,
            SampleRate = 48000d,
            Frequency = 123.456789d,
            Q = 0.7071067811865476d,
            Slope = 1.2345d,
            Gain = -3.25d,
        };
        Local_Source.SetCoefficients(1.0000000001d, -1.9999999999d, 0.9999999998d,
                                     0.5000000001d, -0.2500000002d, 0.1250000003d);

        var Local_RoundTripped = (BiQuadFilter)RoundTripLikeTheApp(Local_Source);

        var Local_Problems = new List<string>();
        CompareMembers(Local_Source, Local_RoundTripped, "BiQuadFilter", Local_Problems);
        AssertNoProblems(typeof(BiQuadFilter), "a save/load round trip of its coefficients", Local_Problems);
    }

    #endregion

    #region Harness self-checks

    [TestMethod]
    [DynamicData(nameof(AllFilterTypes))]
    public void Harness_MovesEveryPersistableMemberOffItsDefault(Type filterType)
    {
        //Guards the tests above: round-tripping default values would prove nothing, so prove the
        //populate step actually changed every member it claims to cover.
        var Local_Default = Activator.CreateInstance(filterType)!;
        var Local_Populated = CreatePopulatedFilter(filterType);

        foreach (var Local_Member in GetPersistableMembers(filterType))
        {
            var Local_Before = GetMemberValue(Local_Member, Local_Default);
            var Local_After = GetMemberValue(Local_Member, Local_Populated);

            var Local_Problems = new List<string>();
            CompareValues(Local_Before, Local_After, filterType.Name + "." + Local_Member.Name, Local_Problems);

            Assert.AreNotEqual(0, Local_Problems.Count,
                filterType.Name + "." + Local_Member.Name
                + " was not moved off its default value, so the round-trip test does not really cover it.");
        }
    }

    [TestMethod]
    public void Harness_CoversAllSeventeenFilters()
    {
        Assert.AreEqual(17, AllFilterTypes.Count());

        //Every IFilter implementation in the production assembly must be covered.
        var Local_Covered = AllFilterTypes.Select(r => (Type)r[0]).ToHashSet();
        var Local_All = typeof(IFilter).Assembly
                                       .GetTypes()
                                       .Where(t => t.IsClass && !t.IsAbstract && typeof(IFilter).IsAssignableFrom(t))
                                       .ToList();

        var Local_Missing = Local_All.Where(t => !Local_Covered.Contains(t)).Select(t => t.FullName).ToList();
        Assert.AreEqual(0, Local_Missing.Count,
            "New filter type(s) with no serialization round-trip coverage: " + string.Join(", ", Local_Missing));
    }

    #endregion
}
