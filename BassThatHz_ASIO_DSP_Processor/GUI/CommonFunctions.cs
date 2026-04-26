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
                catch (Exception)
                {
                    // ignore failures fetching capabilities; leave lists empty
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

    public static string RemoveDeprecatedXMLInputTags(string input)
    {
        XDocument doc = XDocument.Parse(input);

        foreach (XElement limiter in doc.Descendants("Limiter"))
        {
            // Remove elements if they exist
            limiter.Element("PeakHoldDecayEnabled")?.Remove();
            limiter.Element("PeakHoldDecay")?.Remove();
        }

        return doc.ToString();
    }

    public static string RemoveDeprecatedXMLOutputTags(string input)
    {
        XDocument doc = XDocument.Parse(input);

        foreach (XElement limiter in doc.Descendants("Limiter"))
        {
            // Remove elements if they exist
            limiter.Element("PeakHoldDecayEnabled")?.Remove();
            limiter.Element("PeakHoldDecay")?.Remove();
        }

        foreach (XElement stream in doc.Descendants("DSP_Stream"))
        {
            // Remove elements if they exist
            stream.Element("InputChannelIndex")?.Remove();
            stream.Element("OutputChannelIndex")?.Remove();
        }

        return doc.ToString();
    }

    public static bool TryParseXml(string xmlString, out XDocument? xDocument)
    {
        try
        {
            xDocument = XDocument.Parse(xmlString);
            return true;
        }
        catch (Exception)
        {
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
