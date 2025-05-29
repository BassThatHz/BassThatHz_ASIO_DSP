using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using BassThatHz_ASIO_DSP_Processor.Networking;
using System.Net.Http;
using System.Text;
using System;

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
    public void NullCallbacksAreHandledGracefully()
    {
        var api = new NetworkConfigAPI();
        Assert.IsNull(api.GetResponseString);
        Assert.IsNull(api.OnDataStringPosted);
        // Should not throw
        var _ = api.GetResponseString?.Invoke();
        _ = api.OnDataStringPosted?.Invoke("data");
    }

    //[TestMethod]
    //public async Task NetworkConfigAPI_Listener_CanBeStartedAndCancelled()
    //{
    //    var api = new NetworkConfigAPI();
    //    api.GetResponseString = () => "test";
    //    api.OnDataStringPosted = (data) => "Success";
    //    string host = "localhost";
    //    string port = "8082";
    //    var listenerTask = api.NetworkConfigAPI_Listener(host, port);
    //    await Task.Delay(200); // Let the listener start
    //    api.CancellationTokenSource?.Cancel();
    //    using (var client = new HttpClient())
    //    {
    //        try { await client.GetAsync($"http://{host}:{port}/"); } catch { /* ignore */ }
    //    }
    //    await listenerTask;
    //    Assert.IsTrue(true); // If we reach here, listener started and stopped without error
    //}

    //[TestMethod]
    //public async Task NetworkConfigAPI_Listener_HandlesGETRequest()
    //{
    //    var api = new NetworkConfigAPI();
    //    api.GetResponseString = () => "get-response";
    //    api.OnDataStringPosted = (data) => "Success";
    //    string host = "localhost";
    //    string port = "8083";
    //    var listenerTask = api.NetworkConfigAPI_Listener(host, port);
    //    await Task.Delay(200);
    //    using var client = new HttpClient();
    //    var response = await client.GetAsync($"http://{host}:{port}/");
    //    var content = await response.Content.ReadAsStringAsync();
    //    api.CancellationTokenSource?.Cancel();
    //    try { await client.GetAsync($"http://{host}:{port}/"); } catch { /* ignore */ }
    //    await listenerTask;
    //    Assert.AreEqual("get-response", content);
    //}

    //[TestMethod]
    //public async Task NetworkConfigAPI_Listener_HandlesPOSTRequest()
    //{
    //    var api = new NetworkConfigAPI();
    //    api.GetResponseString = () => "test";
    //    api.OnDataStringPosted = (data) => data == "ping" ? "Success" : "Fail";
    //    string host = "localhost";
    //    string port = "8084";
    //    var listenerTask = api.NetworkConfigAPI_Listener(host, port);
    //    await Task.Delay(200);
    //    using var client = new HttpClient();
    //    var content = new StringContent("ping", Encoding.UTF8, "text/plain");
    //    var response = await client.PostAsync($"http://{host}:{port}/", content);
    //    var responseString = await response.Content.ReadAsStringAsync();
    //    api.CancellationTokenSource?.Cancel();
    //    try { await client.GetAsync($"http://{host}:{port}/"); } catch { /* ignore */ }
    //    await listenerTask;
    //    Assert.AreEqual("Success", responseString);
    //}

    //[TestMethod]
    //public async Task NetworkConfigAPI_Listener_HandlesPOSTRequest_Failure()
    //{
    //    var api = new NetworkConfigAPI();
    //    api.GetResponseString = () => "test";
    //    api.OnDataStringPosted = (data) => "Error";
    //    string host = "localhost";
    //    string port = "8085";
    //    var listenerTask = api.NetworkConfigAPI_Listener(host, port);
    //    await Task.Delay(200);
    //    using var client = new HttpClient();
    //    var content = new StringContent("bad", Encoding.UTF8, "text/plain");
    //    var response = await client.PostAsync($"http://{host}:{port}/", content);
    //    var responseString = await response.Content.ReadAsStringAsync();
    //    api.CancellationTokenSource?.Cancel();
    //    try { await client.GetAsync($"http://{host}:{port}/"); } catch { /* ignore */ }
    //    await listenerTask;
    //    Assert.AreEqual("Error", responseString);
    //    Assert.AreEqual(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    //}

    //[TestMethod]
    //public async Task NetworkConfigAPI_Listener_CanBeRestarted()
    //{
    //    var api = new NetworkConfigAPI();
    //    api.GetResponseString = () => "test";
    //    api.OnDataStringPosted = (data) => "Success";
    //    string host = "localhost";
    //    string port = "8086";
    //    // Start and stop
    //    var listenerTask1 = api.NetworkConfigAPI_Listener(host, port);
    //    await Task.Delay(200);
    //    api.CancellationTokenSource?.Cancel();
    //    // Unblock the listener by making a dummy request
    //    using (var client = new HttpClient())
    //    {
    //        try { await client.GetAsync($"http://{host}:{port}/"); } catch { /* ignore */ }
    //    }
    //    await listenerTask1;
    //    // Wait for port to be released before restarting
    //    await Task.Delay(200); // Give time for OS to release the port
    //    // Restart
    //    var listenerTask2 = api.NetworkConfigAPI_Listener(host, port);
    //    await Task.Delay(200);
    //    api.CancellationTokenSource?.Cancel();
    //    using (var client = new HttpClient())
    //    {
    //        try { await client.GetAsync($"http://{host}:{port}/"); } catch { /* ignore */ }
    //    }
    //    await listenerTask2;
    //    Assert.IsTrue(true);
    //}

    //[TestMethod]
    //public async Task NetworkConfigAPI_Listener_HandlesFaviconRequest()
    //{
    //    var api = new NetworkConfigAPI();
    //    api.GetResponseString = () => "test";
    //    api.OnDataStringPosted = (data) => "Success";
    //    string host = "localhost";
    //    string port = "8087";
    //    var listenerTask = api.NetworkConfigAPI_Listener(host, port);
    //    await Task.Delay(200);
    //    using var client = new HttpClient();
    //    var response = await client.GetAsync($"http://{host}:{port}/favicon.ico");
    //    api.CancellationTokenSource?.Cancel();
    //    try { await client.GetAsync($"http://{host}:{port}/"); } catch { /* ignore */ }
    //    await listenerTask;
    //    Assert.AreEqual(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    //}
}