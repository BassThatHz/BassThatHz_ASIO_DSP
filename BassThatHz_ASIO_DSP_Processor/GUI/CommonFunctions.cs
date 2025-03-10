#nullable enable

namespace BassThatHz_ASIO_DSP_Processor;

using BassThatHz_ASIO_DSP_Processor.GUI.Controls;
using ExtendedXmlSerialization;
using NAudio.Wave.Asio;

#region Usings
using System;
using System.Text.Json;
using System.Windows.Forms;
using System.Xml.Linq;
using Windows.Devices.WiFiDirect.Services;
#endregion

public static class CommonFunctions
{
    public static double[] GetStreamInputDataByStreamItem(IStreamItem source)
    {
        double[] inputData = new double[Program.ASIO.SamplesPerChannel];
        switch (source.StreamType)
        {
            case StreamType.Bus:
                inputData = Program.DSP_Info.Buses[source.Index].Buffer;
                break;
            case StreamType.AbstractBus:
                break;
            case StreamType.Stream:
                break;
            case StreamType.Channel:
            default:
                var Data = Program.ASIO.GetInputAudioData(source.Index);
                if (Data != null)
                    inputData = Data;
                break;
        }

        return inputData;
    }

    public static double[] GetStreamOutputDataByStreamItem(IStreamItem destination)
    {
        double[] outputData = new double[Program.ASIO.SamplesPerChannel];
        switch (destination.StreamType)
        {
            case StreamType.Bus:
                outputData = Program.DSP_Info.Buses[destination.Index].Buffer;
                break;
            case StreamType.AbstractBus:
                break;
            case StreamType.Stream:
                break;
            case StreamType.Channel:
            default:
                var Data =Program.ASIO.GetOutputAudioData(destination.Index);
                if (Data != null)
                    outputData = Data;
                break;
        }
        return outputData;
    }

    public static T DeepClone<T>(T source) where T : class
    {
        ArgumentNullException.ThrowIfNull(source);

        string json = new ExtendedXmlSerializer().Serialize(source);
        try
        {
            return new ExtendedXmlSerializer().Deserialize<T>(json) ?? throw new InvalidOperationException("Deserialization failed.");
        }
        catch(Exception ex)
        {
            _ = ex;
            throw;
        }
    }

    public static void Set_DropDownTargetLists(ComboBox inputs, ComboBox outputs, bool IsAbstractBusDropDown)
    {
        if (string.IsNullOrEmpty(Program.DSP_Info.ASIO_InputDevice))
            return;

        if (IsAbstractBusDropDown)
        {
            for (int i = 0; i < Program.DSP_Info.Streams.Count; i++)
            {
                var Stream = Program.DSP_Info.Streams[i];
                if (Stream.InputSource.Equals(Stream.OutputDestination))
                    continue;

                if (Stream.OutputDestination.StreamType == StreamType.AbstractBus)
                    _ = inputs.Items.Add(new StreamItem()
                    {
                        Name = "Stream(" + i + ") " + Stream.InputSource.Name + " | " + Stream.OutputDestination.Name
                        ,
                        Index = i
                        ,
                        StreamType = StreamType.Stream
                        ,
                        DisplayMember = "Stream(" + i + ") In " + Stream.InputSource.Name + " | " + Stream.OutputDestination.Name
                    });
                if (Stream.InputSource.StreamType == StreamType.AbstractBus)
                    _ = outputs.Items.Add(new StreamItem()
                    {
                        Name = "Stream " + i
                        ,
                        Index = i
                        ,
                        StreamType = StreamType.Stream
                        ,
                        DisplayMember = "Stream(" + i + ") Out " + Stream.InputSource.Name + " | " + Stream.OutputDestination.Name
                    });
            }
        }
        else
        {
            AsioDriverCapability? Capabilities = null;
            try
            {
                Capabilities = Program.ASIO.GetDriverCapabilities(Program.DSP_Info.ASIO_InputDevice);
            }
            catch (Exception ex)
            {
                _ = ex;
                //throw new InvalidOperationException("Can't fetch Driver Capabilities", ex);
            }
            if (Capabilities == null)
                return;

            for (int i = 0; i < Capabilities.Value.InputChannelInfos.Length; i++)
            {
                var InputChannel = Capabilities.Value.InputChannelInfos[i];
                _ = inputs.Items.Add(new StreamItem()
                {
                    Name = InputChannel.name
                    ,
                    Index = InputChannel.channel
                    ,
                    StreamType = StreamType.Channel
                    ,
                    DisplayMember = "(" + InputChannel.channel + ") " + InputChannel.name
                });
            }

            for (int i = 0; i < Capabilities.Value.OutputChannelInfos.Length; i++)
            {
                var OutputChannel = Capabilities.Value.OutputChannelInfos[i];
                _ = outputs.Items.Add(new StreamItem()
                {
                    Name = OutputChannel.name
                    ,
                    Index = OutputChannel.channel
                    ,
                    StreamType = StreamType.Channel
                    ,
                    DisplayMember = "(" + OutputChannel.channel + ") " + OutputChannel.name
                });
            }

            for (int i = 0; i < Program.DSP_Info.AbstractBuses.Count; i++)
            {
                var AbstractBus = Program.DSP_Info.AbstractBuses[i];
                _ = inputs.Items.Add(new StreamItem()
                {
                    Name = AbstractBus.Name
                    ,
                    Index = i
                    ,
                    StreamType = StreamType.AbstractBus
                    ,
                    DisplayMember = "AbstractBus(" + i + ") In " + AbstractBus.Name
                });
                _ = outputs.Items.Add(new StreamItem()
                {
                    Name = AbstractBus.Name
                    ,
                    Index = i
                    ,
                    StreamType = StreamType.AbstractBus
                    ,
                    DisplayMember = "AbstractBus(" + i + ") Out " + AbstractBus.Name
                });
            }
        }

        for (int i = 0; i < Program.DSP_Info.Buses.Count; i++)
        {
            var Bus = Program.DSP_Info.Buses[i];
            _ = inputs.Items.Add(new StreamItem()
            {
                Name = Bus.Name
                ,
                Index = i
                ,
                StreamType = StreamType.Bus
                ,
                DisplayMember = "Bus(" + i + ") In " + Bus.Name
            });
            _ = outputs.Items.Add(new StreamItem()
            {
                Name = Bus.Name
                ,
                Index = i
                ,
                StreamType = StreamType.Bus
                ,
                DisplayMember = "Bus(" + i + ") Out " + Bus.Name
            });
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

    public static string RemoveDeprecatedXMLTags(string input)
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
