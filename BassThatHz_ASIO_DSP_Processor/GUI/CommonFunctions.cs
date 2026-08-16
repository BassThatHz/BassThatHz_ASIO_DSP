#nullable enable

namespace BassThatHz_ASIO_DSP_Processor;

using BassThatHz_ASIO_DSP_Processor.GUI.Controls;
using ExtendedXmlSerialization;
using NAudio.Wave.Asio;

#region Usings
using System;
using System.Windows.Forms;
using System.Xml.Linq;
#endregion

public static class CommonFunctions
{
    // Reuse serializer instance to avoid repeated allocations and startup cost
    private static readonly ExtendedXmlSerializer s_serializer = new ExtendedXmlSerializer();
    public static double[] GetStreamInputDataByStreamItem(IStreamItem source)
    {
        // Return references where possible to avoid allocations and copies.
        switch (source.StreamType)
        {
            case StreamType.Bus:
                return Program.DSP_Info.Buses[source.Index].Buffer;
            case StreamType.AbstractBus:
            case StreamType.Stream:
                // Fallback to an appropriately sized buffer when no specific source exists
                return new double[Program.ASIO.SamplesPerChannel];
            case StreamType.Channel:
            default:
            {
                var data = Program.ASIO.GetInputAudioData(source.Index);
                return data ?? new double[Program.ASIO.SamplesPerChannel];
            }
        }
    }

    public static double[] GetStreamOutputDataByStreamItem(IStreamItem destination)
    {
        // Return references where possible to avoid allocations and copies.
        switch (destination.StreamType)
        {
            case StreamType.Bus:
                return Program.DSP_Info.Buses[destination.Index].Buffer;
            case StreamType.AbstractBus:
            case StreamType.Stream:
                return new double[Program.ASIO.SamplesPerChannel];
            case StreamType.Channel:
            default:
            {
                var data = Program.ASIO.GetOutputAudioData(destination.Index);
                return data ?? new double[Program.ASIO.SamplesPerChannel];
            }
        }
    }

    /// <summary>
    /// Returns a fresh, private SNAPSHOT of a stream item's input data.
    /// <para>
    /// <see cref="GetStreamInputDataByStreamItem"/> returns a LIVE reference for Bus sources, so
    /// callers that need a stable snapshot had to append <c>.ToArray()</c> - which for the
    /// Channel case copied an array that had already been defensively copied, i.e. two
    /// allocations per call on a path that runs once per ASIO buffer switch. This helper does
    /// exactly one copy and is otherwise value-for-value identical.
    /// </para>
    /// </summary>
    /// <param name="source">The stream item to snapshot.</param>
    /// <returns>A newly allocated array owned by the caller.</returns>
    public static double[] GetStreamInputDataSnapshotByStreamItem(IStreamItem source)
    {
        switch (source.StreamType)
        {
            case StreamType.Bus:
            {
                //Live buffer, mutated by the audio thread - must be copied.
                var Local_Live = Program.DSP_Info.Buses[source.Index].Buffer;
                var Local_Snapshot = new double[Local_Live.Length];
                Local_Live.AsSpan().CopyTo(Local_Snapshot);
                return Local_Snapshot;
            }
            case StreamType.AbstractBus:
            case StreamType.Stream:
                return new double[Program.ASIO.SamplesPerChannel];
            case StreamType.Channel:
            default:
                //GetInputAudioData already returns a private defensive copy.
                return Program.ASIO.GetInputAudioData(source.Index)
                       ?? new double[Program.ASIO.SamplesPerChannel];
        }
    }

    /// <summary>
    /// Returns a fresh, private SNAPSHOT of a stream item's output data.
    /// See <see cref="GetStreamInputDataSnapshotByStreamItem"/> for why this exists.
    /// </summary>
    /// <param name="destination">The stream item to snapshot.</param>
    /// <returns>A newly allocated array owned by the caller.</returns>
    public static double[] GetStreamOutputDataSnapshotByStreamItem(IStreamItem destination)
    {
        switch (destination.StreamType)
        {
            case StreamType.Bus:
            {
                //Live buffer, mutated by the audio thread - must be copied.
                var Local_Live = Program.DSP_Info.Buses[destination.Index].Buffer;
                var Local_Snapshot = new double[Local_Live.Length];
                Local_Live.AsSpan().CopyTo(Local_Snapshot);
                return Local_Snapshot;
            }
            case StreamType.AbstractBus:
            case StreamType.Stream:
                return new double[Program.ASIO.SamplesPerChannel];
            case StreamType.Channel:
            default:
                //GetOutputAudioData already returns a private defensive copy.
                return Program.ASIO.GetOutputAudioData(destination.Index)
                       ?? new double[Program.ASIO.SamplesPerChannel];
        }
    }

    public static T DeepClone<T>(T source) where T : class
    {
        ArgumentNullException.ThrowIfNull(source);

        // Reuse serializer to reduce allocation and initialization cost
        string xml = s_serializer.Serialize(source);
        return s_serializer.Deserialize<T>(xml) ?? throw new InvalidOperationException("Deserialization failed.");
    }

    public static void Set_DropDownTargetLists(ComboBox inputs, ComboBox outputs, bool IsAbstractBusDropDown)
    {
        if (string.IsNullOrEmpty(Program.DSP_Info.ASIO_InputDevice))
            return;

        // Clear existing items and batch updates to reduce UI overhead
        inputs.Items.Clear();
        outputs.Items.Clear();
        inputs.BeginUpdate();
        outputs.BeginUpdate();
        try
        {
            var dsp = Program.DSP_Info;
            var asio = Program.ASIO;

            if (IsAbstractBusDropDown)
            {
                for (int i = 0; i < dsp.Streams.Count; i++)
                {
                    var stream = dsp.Streams[i];
                    if (stream.InputSource.Equals(stream.OutputDestination))
                        continue;

                    if (stream.OutputDestination.StreamType == StreamType.AbstractBus)
                    {
                        var name = $"Stream({i}) {stream.InputSource.Name} | {stream.OutputDestination.Name}";
                        _ = inputs.Items.Add(new StreamItem()
                        {
                            Name = name,
                            Index = i,
                            StreamType = StreamType.Stream,
                            DisplayMember = $"Stream({i}) In {stream.InputSource.Name} | {stream.OutputDestination.Name}"
                        });
                    }

                    if (stream.InputSource.StreamType == StreamType.AbstractBus)
                    {
                        _ = outputs.Items.Add(new StreamItem()
                        {
                            Name = $"Stream {i}",
                            Index = i,
                            StreamType = StreamType.Stream,
                            DisplayMember = $"Stream({i}) Out {stream.InputSource.Name} | {stream.OutputDestination.Name}"
                        });
                    }
                }
            }
            else
            {
                AsioDriverCapability? capabilities = null;
                try
                {
                    capabilities = asio.GetDriverCapabilities(dsp.ASIO_InputDevice);
                }
                catch (Exception ex)
                {
                    // Deliberate: this helper populates dropdown lists and must not throw at the
                    // caller. The user-facing message for a failed capability fetch is raised by
                    // ctl_InputsConfigPage / ctl_StatsPage; here the lists are simply left empty.
                    // Recorded so the cause is still observable.
                    Debug.ReportSwallowed(ex);
                }

                if (capabilities == null)
                    return;

                var inputInfos = capabilities.Value.InputChannelInfos;
                for (int i = 0; i < inputInfos.Length; i++)
                {
                    var inputChannel = inputInfos[i];
                    _ = inputs.Items.Add(new StreamItem()
                    {
                        Name = inputChannel.name,
                        Index = inputChannel.channel,
                        StreamType = StreamType.Channel,
                        DisplayMember = $"({inputChannel.channel}) {inputChannel.name}"
                    });
                }

                var outputInfos = capabilities.Value.OutputChannelInfos;
                for (int i = 0; i < outputInfos.Length; i++)
                {
                    var outputChannel = outputInfos[i];
                    _ = outputs.Items.Add(new StreamItem()
                    {
                        Name = outputChannel.name,
                        Index = outputChannel.channel,
                        StreamType = StreamType.Channel,
                        DisplayMember = $"({outputChannel.channel}) {outputChannel.name}"
                    });
                }

                for (int i = 0; i < dsp.AbstractBuses.Count; i++)
                {
                    var abstractBus = dsp.AbstractBuses[i];
                    _ = inputs.Items.Add(new StreamItem()
                    {
                        Name = abstractBus.Name,
                        Index = i,
                        StreamType = StreamType.AbstractBus,
                        DisplayMember = $"AbstractBus({i}) In {abstractBus.Name}"
                    });
                    _ = outputs.Items.Add(new StreamItem()
                    {
                        Name = abstractBus.Name,
                        Index = i,
                        StreamType = StreamType.AbstractBus,
                        DisplayMember = $"AbstractBus({i}) Out {abstractBus.Name}"
                    });
                }
            }

            for (int i = 0; i < dsp.Buses.Count; i++)
            {
                var bus = dsp.Buses[i];
                _ = inputs.Items.Add(new StreamItem()
                {
                    Name = bus.Name,
                    Index = i,
                    StreamType = StreamType.Bus,
                    DisplayMember = $"Bus({i}) In {bus.Name}"
                });
                _ = outputs.Items.Add(new StreamItem()
                {
                    Name = bus.Name,
                    Index = i,
                    StreamType = StreamType.Bus,
                    DisplayMember = $"Bus({i}) Out {bus.Name}"
                });
            }
        }
        finally
        {
            outputs.EndUpdate();
            inputs.EndUpdate();
        }
    }

    public static void FixLegacyChannelIndexMappings()
    {
        //Fixes Legacy Channel Index Mappings for backwards support
        var Streams = Program.DSP_Info.Streams;
        for (int i = 0; i < Streams.Count; i++)
        {
            if (Streams[i] == null)
                continue;

            if (Streams[i].InputChannelIndex > -1)
            {
                Streams[i].InputSource = new StreamItem()
                {
                    Index = Streams[i].InputChannelIndex
                };
            }

            if (Streams[i].OutputChannelIndex > -1)
            {
                Streams[i].OutputDestination = new StreamItem()
                {
                    Index = Streams[i].OutputChannelIndex
                };
            }
        }
    }

    /// <summary>
    /// Removes elements that no longer map to a serializable member, so that configs saved by
    /// older builds still load.
    /// <para>
    /// <c>ExtendedXmlSerializer.ReadXml</c> THROWS <see cref="InvalidOperationException"/>
    /// ("Missing property ...") on any element it cannot map, so a config containing an element
    /// for a member that has since been removed - or that is now excluded from serialization -
    /// would otherwise fail to load entirely and the user would see
    /// "Could not successfully load the DSP config file".
    /// </para>
    /// <para>
    /// The runtime-state elements below were written by builds that did not yet honor
    /// <see cref="System.Runtime.Serialization.IgnoreDataMemberAttribute"/>. They are computed
    /// meter/telemetry values (or, for MixerInput.ChannelName, a value derived from the live ASIO
    /// device list), never user settings, so discarding them loses nothing.
    /// </para>
    /// </summary>
    /// <param name="input">The XML config text as read from disk or received over the network API.</param>
    /// <returns>The XML with deprecated/no-longer-serialized elements removed.</returns>
    public static string RemoveDeprecatedXMLInputTags(string input)
    {
        XDocument doc = XDocument.Parse(input);
        RemoveDeprecatedElements(doc);
        return doc.ToString();
    }

    /// <summary>
    /// Removes deprecated elements on the SAVE side (and on the network API's validate-then-apply
    /// path). Kept deliberately in step with <see cref="RemoveDeprecatedXMLInputTags"/> so that
    /// anything stripped on load is also stripped on save.
    /// </summary>
    /// <param name="input">The freshly serialized XML config text.</param>
    /// <returns>The XML with deprecated/no-longer-serialized elements removed.</returns>
    public static string RemoveDeprecatedXMLOutputTags(string input)
    {
        XDocument doc = XDocument.Parse(input);
        RemoveDeprecatedElements(doc);

        foreach (XElement stream in doc.Descendants("DSP_Stream"))
        {
            // Remove elements if they exist
            stream.Element("InputChannelIndex")?.Remove();
            stream.Element("OutputChannelIndex")?.Remove();
        }

        return doc.ToString();
    }

    /// <summary>
    /// The element removals shared by the input and output migrations.
    /// </summary>
    /// <param name="doc">The parsed config document, mutated in place.</param>
    private static void RemoveDeprecatedElements(XDocument doc)
    {
        foreach (XElement Local_Limiter in doc.Descendants("Limiter"))
        {
            // Remove elements if they exist
            Local_Limiter.Element("PeakHoldDecayEnabled")?.Remove();
            Local_Limiter.Element("PeakHoldDecay")?.Remove();

            //Runtime meter state: written by Limiter.Transform, reset by ApplySettings, and only
            //read back by LimiterControl for its display. Marked [IgnoreDataMember].
            Local_Limiter.Element("CompressionApplied")?.Remove();
            Local_Limiter.Element("PeakValue")?.Remove();
            Local_Limiter.Element("IsBrickwall")?.Remove();
        }

        foreach (XElement Local_DEQ in doc.Descendants("DEQ"))
        {
            //Computed in DEQ.Transform and read by DEQControl for its meter. Marked [IgnoreDataMember].
            Local_DEQ.Element("GainApplied")?.Remove();
        }

        foreach (XElement Local_MixerInput in doc.Descendants("MixerInput"))
        {
            //Derived from the live ASIO device channel list, not from the config. Marked [IgnoreDataMember].
            Local_MixerInput.Element("ChannelName")?.Remove();
        }
    }

    public static bool TryParseXml(string xmlString, out XDocument? xDocument)
    {
        try
        {
            xDocument = XDocument.Parse(xmlString);
            return true;
        }
        catch (System.Xml.XmlException ex)
        {
            //Narrowed from catch (Exception): malformed XML is the only failure this method is
            //meant to report as "false". ArgumentNullException/OutOfMemoryException used to be
            //silently converted into "not valid XML".
            Debug.ReportSwallowed(ex);
            xDocument = null;
            return false;
        }
    }

    public static string TryParseXml(string xmlString)
    {
        try
        {
            _ = XDocument.Parse(xmlString);
            return "Success";
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }
}
