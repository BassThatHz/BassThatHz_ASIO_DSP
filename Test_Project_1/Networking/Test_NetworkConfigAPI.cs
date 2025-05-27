using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using BassThatHz_ASIO_DSP_Processor.Networking;

namespace Test_Project_1;

[TestClass]
public class Test_NetworkConfigAPI
{
    [TestMethod]
    public void CanInstantiateNetworkConfigAPI()
    {
        var api = new NetworkConfigAPI();
        Assert.IsNotNull(api);
    }

    [TestMethod]
    public void CanSetAndInvokeGetResponseString()
    {
        var api = new NetworkConfigAPI();
        api.GetResponseString = () => "test-response";
        Assert.AreEqual("test-response", api.GetResponseString());
    }

    [TestMethod]
    public void CanSetAndInvokeOnDataStringPosted()
    {
        var api = new NetworkConfigAPI();
        api.OnDataStringPosted = (data) => $"echo:{data}";
        Assert.AreEqual("echo:hello", api.OnDataStringPosted("hello"));
    }

    [TestMethod]
    public async Task NetworkConfigAPI_Listener_CanBeStartedAndCancelled()
    {
        var api = new NetworkConfigAPI();
        api.GetResponseString = () => "test";
        api.OnDataStringPosted = (data) => "Success";
        string host = "localhost";
        string port = "8081";
        var listenerTask = api.NetworkConfigAPI_Listener(host, port);
        await Task.Delay(200); // Let the listener start
        api.CancellationTokenSource?.Cancel();
        await listenerTask;
        Assert.IsTrue(true); // If we reach here, listener started and stopped without error
    }
}