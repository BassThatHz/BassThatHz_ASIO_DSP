#nullable enable

namespace BassThatHz_ASIO_DSP_Processor.Networking;

#region Usings
using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
#endregion

/// <summary>
///  BassThatHz ASIO DSP Processor Engine
///  Copyright (c) 2026 BassThatHz
/// 
/// Permission is hereby granted to use this software 
/// and associated documentation files (the "Software"), 
/// for educational purposess, scientific purposess or private purposess
/// or as part of an open-source community project, 
/// (and NOT for commerical use or resale in substaintial part or whole without prior authorization)
/// and all copies of the Software subject to the following conditions:
/// 
/// The copyright notice and this permission notice shall be included in all
/// copies or substantial portions of the Software.
// 
/// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
/// IMPLIED, INCLUDING BUT NOT LIMITED TO FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
/// IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
/// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
/// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
/// SOFTWARE. ENFORCEABLE PORTIONS SHALL REMAIN IF NOT FOUND CONTRARY UNDER LAW.
/// </summary>
public class NetworkConfigAPI
{
    // Reuse common encodings to avoid repeated allocations
    private static readonly System.Text.Encoding Utf8 = System.Text.Encoding.UTF8;

    // Limit inbound POST size to avoid unbounded memory usage (4 MB default)
    private const long MaxPostContentLength = 4 * 1024 * 1024;
    #region Public Events \ Callbacks
    public Func<string>? GetResponseString;
    public Func<string, string>? OnDataStringPosted;
    #endregion

    #region Variables

    /// <summary>
    /// The last exception that terminated the listener loop (for example a port-in-use or
    /// access-denied failure while starting). Null while the listener is healthy. Exposed so the
    /// failure is observable instead of being silently swallowed.
    /// </summary>
    public Exception? LastListenerError { get; protected set; }

    protected HttpListener? Listener;
    public CancellationTokenSource? CancellationTokenSource;
    #endregion

    #region Public Functions
    public async Task NetworkConfigAPI_Listener(string hostname, string port)
    {
        try
        {
            #region Startup Initialization
            if (this.Listener != null && this.Listener.IsListening)
            {
                this.Listener.Stop();
                this.Listener.Close();
            }
            this.Listener = new();
            // Ensure we have a fresh CancellationTokenSource (replace if previously cancelled)
            if (this.CancellationTokenSource == null || this.CancellationTokenSource.IsCancellationRequested)
            {
                if (this.CancellationTokenSource != null)
                {
                    this.CancellationTokenSource.Dispose();
                }
                this.CancellationTokenSource = new();
            }

            var CancellationToken = this.CancellationTokenSource.Token;
            #endregion

            #region Start the Listener
            this.Listener.Prefixes.Add($"http://{hostname}:{port}/");
            this.Listener.Start();
            #endregion

            //Enter the listener loop with cancellation token check
            while (!CancellationToken.IsCancellationRequested)
            {
                #region Wait for an incoming request
                HttpListenerContext context = await this.Listener.GetContextAsync().ConfigureAwait(false); //Main blocking call
                
                if (CancellationToken.IsCancellationRequested)
                    break; //Cancel was requested while we were awaiting, don't process any further

                HttpListenerRequest request = context.Request;
                using HttpListenerResponse response = context.Response;
                #endregion

                #region Skip processing icon requests
                string? urlPath = request.Url?.AbsolutePath;
                if (urlPath == "/favicon.ico")
                {
                    response.StatusCode = 404; // Not Found
                    continue; // Skip
                }
                #endregion

                #region Process POST and GET requests
                if (request.HttpMethod == "POST")
                {
                    await this.POST_Listener(request, response, CancellationToken);
                }
                else if (request.HttpMethod == "GET")
                {
                    await this.GET_Listener(response, CancellationToken);
                }
                #endregion
            }
        }
        catch (Exception ex) //Error Handling
        {
            //DEFECT FIX: this used to be a bare '_ = ex;'. Prefixes.Add/Start failures (port in
            //use, or HttpListenerException 5 "Access denied" for a non-admin http://+:port/ prefix)
            //were completely invisible: the user was left with the checkbox ticked and nothing
            //listening. Record it so it is observable, and expose it to the caller.
            this.LastListenerError = ex;
            Debug.ReportSwallowed(ex);
        }
        finally
        {
            #region Exit Cleanup
            //DEFECT FIX: Stop()/Close()/Dispose() here can throw ObjectDisposedException or
            //HttpListenerException. Thrown from a finally, that exception escaped an
            //already-handled method into an async void caller = unhandled UI-thread crash, and it
            //also masked whatever was propagating. Cleanup is now individually guarded.
            //We are exiting, if the Listener is listening, stop and close it
            try
            {
                if (this.Listener != null && this.Listener.IsListening)
                {
                    this.Listener.Stop();
                    this.Listener.Close();
                }
            }
            catch (Exception ex)
            {
                Debug.ReportSwallowed(ex);
            }

            try
            {
                if (this.CancellationTokenSource != null && this.CancellationTokenSource.IsCancellationRequested)
                {
                    this.CancellationTokenSource.Dispose();
                    this.CancellationTokenSource = new CancellationTokenSource();
                }
            }
            catch (Exception ex)
            {
                Debug.ReportSwallowed(ex);
            }
            #endregion
        }
    }
    #endregion

    #region Protected Fucntions

    protected async Task POST_Listener(HttpListenerRequest request, HttpListenerResponse response, CancellationToken cancellationToken)
    {
        // Quick checks to avoid unnecessary allocations
        if (!request.HasEntityBody)
            return;

        // Protect against extremely large inbound posts to avoid OOM
        if (request.ContentLength64 > 0 && request.ContentLength64 > MaxPostContentLength)
        {
            response.StatusCode = 413; // Payload Too Large
            return;
        }

        using Stream body = request.InputStream;
        if (!body.CanRead)
            return;

        using var reader = new StreamReader(body, request.ContentEncoding);
        string data = string.Empty;
        try
        {
            // await for an inbound POST message to be posted
            data = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (TaskCanceledException)
        {
            // Cancellation requested - abort processing
            return;
        }

        // Process the POST message using a local copy to avoid race with event changes
        var onData = this.OnDataStringPosted;
        var Success = onData?.Invoke(data);

        // Send a Success / Fail response
        if (Success == null)
            return;

        using Stream output = response.OutputStream;
        if (!output.CanWrite)
            return;

        var responsebuffer = Utf8.GetBytes(Success);
        response.ContentLength64 = responsebuffer.Length;
        if (!string.Equals(Success, "Success", StringComparison.Ordinal))
            response.StatusCode = 400; // Bad Request

        try
        {
            await output.WriteAsync(responsebuffer, 0, responsebuffer.Length, cancellationToken).ConfigureAwait(false);
        }
        catch (TaskCanceledException)
        {
            // Cancellation requested while writing the response - swallow
        }
    }

    protected async Task GET_Listener(HttpListenerResponse response, CancellationToken cancellationToken)
    {
        //Get the response string from the listener owner
        var getResponse = this.GetResponseString;
        var responseString = getResponse?.Invoke();

        if (responseString == null)
            return;

        var responsebuffer = Utf8.GetBytes(responseString);
        response.ContentLength64 = responsebuffer.Length;
        response.ContentType = "text/plain; charset=utf-8";

        using Stream output = response.OutputStream;
        if (!output.CanWrite)
            return;

        try
        {
            // wait for the GET message to respond to the web request
            await output.WriteAsync(responsebuffer, 0, responsebuffer.Length, cancellationToken).ConfigureAwait(false);
        }
        catch (TaskCanceledException)
        {
            // Cancellation requested while writing response
        }
    }
    #endregion
}