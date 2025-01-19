#nullable enable

namespace BassThatHz_ASIO_DSP_Processor
{
    #region Usings
    using System;
    using System.IO;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    #endregion

    /// <summary>
    ///  BassThatHz ASIO DSP Processor Engine
    ///  Copyright (c) 2025 BassThatHz
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
        #region Public Events \ Callbacks
        public Func<string>? GetResponseString;
        public Func<string, string>? OnDataStringPosted;
        #endregion

        #region Variables
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
                this.CancellationTokenSource ??= new();
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
                    HttpListenerContext context = await this.Listener.GetContextAsync(); //Main blocking call
                    
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
                _ = ex; //Debug networking errors here
            }
            finally
            {
                #region Exit Cleanup
                //We are exiting, if the Listener is listening, stop and close it
                if (this.Listener != null && this.Listener.IsListening)
                {
                    this.Listener.Stop();
                    this.Listener.Close();
                }

                if (this.CancellationTokenSource != null && this.CancellationTokenSource.IsCancellationRequested)
                {
                    this.CancellationTokenSource.Dispose();
                    this.CancellationTokenSource = new CancellationTokenSource();
                }
                #endregion
            }
        }
        #endregion

        #region Protected Fucntions

        protected async Task POST_Listener(HttpListenerRequest request, HttpListenerResponse response, CancellationToken cancellationToken)
        {
            using Stream body = request.InputStream;
            if (!body.CanRead)
                return;

            using var reader = new StreamReader(body, request.ContentEncoding);
            var ReaderTask = reader.ReadToEndAsync(cancellationToken);
            string data = string.Empty;
            try
            {
                //await for an inbound POST message to be posted
                data = await ReaderTask;
            }
            catch (TaskCanceledException)
            {
                //Do Nothing, The Cancellation Token was invoked
            }

            //Process the POST message
            var Success = this.OnDataStringPosted?.Invoke(data);

            #region Send a Success \ Fail response
            if (Success == null)
                return;

            using Stream output = response.OutputStream;
            if (!output.CanWrite)
                return;

            var responsebuffer = System.Text.Encoding.UTF8.GetBytes(Success);
            response.ContentLength64 = responsebuffer.Length;
            if (Success != "Success")
                response.StatusCode = 400; // Bad Request

            output.Write(responsebuffer, 0, responsebuffer.Length);

            #endregion
        }

        protected async Task GET_Listener(HttpListenerResponse response, CancellationToken cancellationToken)
        {
            //Get the response string from the listener owner
            var responseString = this.GetResponseString?.Invoke();

            if (responseString == null)
                return;

            var responsebuffer = System.Text.Encoding.UTF8.GetBytes(responseString);
            response.ContentLength64 = responsebuffer.Length;
            using Stream output = response.OutputStream;

            if (!output.CanWrite)
                return;

            var WriteTask = output.WriteAsync(responsebuffer, 0, responsebuffer.Length, cancellationToken);
            try
            {
                //wait for the GET message to respond to the web request
                await WriteTask;
            }
            catch (TaskCanceledException)
            {
                //Do Nothing, The Cancellation Token was invoked
            }
        }
        #endregion
    }
}