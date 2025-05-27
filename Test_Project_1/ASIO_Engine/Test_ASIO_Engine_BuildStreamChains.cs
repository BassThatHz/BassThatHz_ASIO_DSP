using System.Collections.ObjectModel;
using System.Linq;
using BassThatHz_ASIO_DSP_Processor;

[TestClass]
public class Test_ASIO_Engine_BuildStreamChains
{
    private static DSP_Stream CreateStream(int inputIdx, StreamType inputType, int outputIdx, StreamType outputType)
    {
        return new DSP_Stream
        {
            InputSource = new StreamItem { Index = inputIdx, StreamType = inputType },
            OutputDestination = new StreamItem { Index = outputIdx, StreamType = outputType },
            InputVolume = 1.0,
            OutputVolume = 1.0
        };
    }

    private static List<List<DSP_Stream>> InvokeBuildStreamChains(ASIO_Engine engine, ObservableCollection<DSP_Stream> streams)
    {
        return engine.GetType()
            .GetMethod("BuildStreamChains", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .Invoke(engine, new object[] { streams }) as List<List<DSP_Stream>>;
    }

    [TestMethod]
    public void BuildStreamChains_Depth1_ChannelToChannel()
    {
        var engine = new ASIO_Engine();
        var streams = new ObservableCollection<DSP_Stream>
        {
            CreateStream(0, StreamType.Channel, 1, StreamType.Channel)
        };
        var chains = InvokeBuildStreamChains(engine, streams);
        Assert.IsNotNull(chains);
        Assert.AreEqual(1, chains.Count);
        Assert.AreEqual(1, chains[0].Count);
        Assert.AreEqual(StreamType.Channel, chains[0][0].InputSource.StreamType);
        Assert.AreEqual(StreamType.Channel, chains[0][0].OutputDestination.StreamType);
    }

    [TestMethod]
    public void BuildStreamChains_Depth2_ChannelToBusToChannel()
    {
        var engine = new ASIO_Engine();
        var streams = new ObservableCollection<DSP_Stream>
        {
            CreateStream(0, StreamType.Channel, 0, StreamType.Bus),
            CreateStream(0, StreamType.Bus, 1, StreamType.Channel)
        };
        var chains = InvokeBuildStreamChains(engine, streams);
        Assert.IsNotNull(chains);
        Assert.AreEqual(1, chains.Count);
        Assert.AreEqual(2, chains[0].Count);
        Assert.AreEqual(StreamType.Channel, chains[0][0].InputSource.StreamType);
        Assert.AreEqual(StreamType.Bus, chains[0][0].OutputDestination.StreamType);
        Assert.AreEqual(StreamType.Bus, chains[0][1].InputSource.StreamType);
        Assert.AreEqual(StreamType.Channel, chains[0][1].OutputDestination.StreamType);
    }

    [TestMethod]
    public void BuildStreamChains_Depth2_ChannelToAbstractBusToChannel()
    {
        var engine = new ASIO_Engine();
        var streams = new ObservableCollection<DSP_Stream>
        {
            CreateStream(0, StreamType.Channel, 0, StreamType.AbstractBus),
            CreateStream(0, StreamType.AbstractBus, 1, StreamType.Channel),
            CreateStream(0, StreamType.AbstractBus, 0, StreamType.AbstractBus) // Master
        };
        Program.DSP_Info.AbstractBuses.Clear();
        var ab = new DSP_AbstractBus { Name = "AB0" };
        ab.Mappings.Add(new DSP_AbstractBusMappings
        {
            InputSource = new StreamItem { Index = 0, StreamType = StreamType.Channel },
            OutputDestination = new StreamItem { Index = 1, StreamType = StreamType.Channel }
        });
        Program.DSP_Info.AbstractBuses.Add(ab);
        Program.DSP_Info.Streams.Clear();
        foreach (var s in streams) Program.DSP_Info.Streams.Add(s);
        var chains = InvokeBuildStreamChains(engine, streams);
        Assert.IsNotNull(chains);
        Assert.AreEqual(1, chains.Count);
        Assert.IsTrue(chains[0].Any(s => s.InputSource.StreamType == StreamType.AbstractBus));
        Assert.IsTrue(chains[0].Any(s => s.OutputDestination.StreamType == StreamType.AbstractBus));
    }

    [TestMethod]
    public void BuildStreamChains_Depth3_ChannelToBusToBusToChannel()
    {
        var engine = new ASIO_Engine();
        var streams = new ObservableCollection<DSP_Stream>
        {
            CreateStream(0, StreamType.Channel, 0, StreamType.Bus),
            CreateStream(0, StreamType.Bus, 1, StreamType.Bus),
            CreateStream(1, StreamType.Bus, 2, StreamType.Channel)
        };
        var chains = InvokeBuildStreamChains(engine, streams);
        Assert.IsNotNull(chains);
        Assert.AreEqual(1, chains.Count);
        Assert.AreEqual(3, chains[0].Count);
        Assert.AreEqual(StreamType.Channel, chains[0][0].InputSource.StreamType);
        Assert.AreEqual(StreamType.Bus, chains[0][1].InputSource.StreamType);
        Assert.AreEqual(StreamType.Bus, chains[0][2].InputSource.StreamType);
        Assert.AreEqual(StreamType.Channel, chains[0][2].OutputDestination.StreamType);
    }

    [TestMethod]
    public void BuildStreamChains_Depth3_ChannelToBusToAbstractBusToChannel()
    {
        var engine = new ASIO_Engine();
        var streams = new ObservableCollection<DSP_Stream>
        {
            CreateStream(0, StreamType.Channel, 0, StreamType.Bus),
            CreateStream(0, StreamType.Bus, 0, StreamType.AbstractBus),
            CreateStream(0, StreamType.AbstractBus, 1, StreamType.Channel),
            CreateStream(0, StreamType.AbstractBus, 0, StreamType.AbstractBus) // Master
        };
        Program.DSP_Info.AbstractBuses.Clear();
        var ab = new DSP_AbstractBus { Name = "AB0" };
        ab.Mappings.Add(new DSP_AbstractBusMappings
        {
            InputSource = new StreamItem { Index = 0, StreamType = StreamType.Channel },
            OutputDestination = new StreamItem { Index = 1, StreamType = StreamType.Channel }
        });
        Program.DSP_Info.AbstractBuses.Add(ab);
        Program.DSP_Info.Streams.Clear();
        foreach (var s in streams) Program.DSP_Info.Streams.Add(s);
        var chains = InvokeBuildStreamChains(engine, streams);
        Assert.IsNotNull(chains);
        Assert.AreEqual(1, chains.Count);
        Assert.AreEqual(3, chains[0].Count);
        Assert.IsTrue(chains[0].Any(s => s.InputSource.StreamType == StreamType.AbstractBus));
        Assert.IsTrue(chains[0].Any(s => s.OutputDestination.StreamType == StreamType.AbstractBus));
    }

    [TestMethod]
    public void BuildStreamChains_Depth4_ChannelToBusToBusToBusToChannel()
    {
        var engine = new ASIO_Engine();
        var streams = new ObservableCollection<DSP_Stream>
        {
            CreateStream(0, StreamType.Channel, 0, StreamType.Bus),
            CreateStream(0, StreamType.Bus, 1, StreamType.Bus),
            CreateStream(1, StreamType.Bus, 2, StreamType.Bus),
            CreateStream(2, StreamType.Bus, 3, StreamType.Channel)
        };
        var chains = InvokeBuildStreamChains(engine, streams);
        Assert.IsNotNull(chains);
        Assert.AreEqual(1, chains.Count);
        Assert.AreEqual(4, chains[0].Count);
        Assert.AreEqual(StreamType.Channel, chains[0][0].InputSource.StreamType);
        Assert.AreEqual(StreamType.Bus, chains[0][1].InputSource.StreamType);
        Assert.AreEqual(StreamType.Bus, chains[0][2].InputSource.StreamType);
        Assert.AreEqual(StreamType.Bus, chains[0][3].InputSource.StreamType);
        Assert.AreEqual(StreamType.Channel, chains[0][3].OutputDestination.StreamType);
    }

    [TestMethod]
    public void BuildStreamChains_Depth4_ChannelToBusToAbstractBusToBusToChannel()
    {
        var engine = new ASIO_Engine();
        var streams = new ObservableCollection<DSP_Stream>
        {
            CreateStream(0, StreamType.Channel, 0, StreamType.Bus),
            CreateStream(0, StreamType.Bus, 0, StreamType.AbstractBus),
            CreateStream(0, StreamType.AbstractBus, 1, StreamType.Bus),
            CreateStream(1, StreamType.Bus, 2, StreamType.Channel),
            CreateStream(0, StreamType.AbstractBus, 0, StreamType.AbstractBus) // Master
        };
        Program.DSP_Info.AbstractBuses.Clear();
        var ab = new DSP_AbstractBus { Name = "AB0" };
        ab.Mappings.Add(new DSP_AbstractBusMappings
        {
            InputSource = new StreamItem { Index = 0, StreamType = StreamType.Channel },
            OutputDestination = new StreamItem { Index = 2, StreamType = StreamType.Channel }
        });
        Program.DSP_Info.AbstractBuses.Add(ab);
        Program.DSP_Info.Streams.Clear();
        foreach (var s in streams) Program.DSP_Info.Streams.Add(s);
        var chains = InvokeBuildStreamChains(engine, streams);
        Assert.IsNotNull(chains);
        Assert.AreEqual(1, chains.Count);
        Assert.AreEqual(4, chains[0].Count);
        Assert.IsTrue(chains[0].Any(s => s.InputSource.StreamType == StreamType.AbstractBus));
        Assert.IsTrue(chains[0].Any(s => s.OutputDestination.StreamType == StreamType.AbstractBus));
        Assert.AreEqual(StreamType.Channel, chains[0][0].InputSource.StreamType);
        Assert.AreEqual(StreamType.Channel, chains[0][3].OutputDestination.StreamType);
    }
}
