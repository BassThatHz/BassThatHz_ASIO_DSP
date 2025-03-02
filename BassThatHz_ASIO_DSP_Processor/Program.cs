#nullable enable

namespace BassThatHz_ASIO_DSP_Processor;

#region Usings
using System;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;
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
public static class Program
{
    public static DSP_Info DSP_Info = new();
    public readonly static ASIO_Engine ASIO = new();
    public static FormMain? Form_Main;
    public static FormMonitoring? Form_Monitoring;
    public static DateTime App_StartTime = DateTime.Now;

    [STAThread]
    public static void Main()
    {
        //Global error handler
        AppDomain.CurrentDomain.UnhandledException += Debug.GlobalError;
        AppDomain.CurrentDomain.FirstChanceException += Debug.GlobalError;

        //Run the process and thread-0 with high priority
        Thread.CurrentThread.Priority = ThreadPriority.Highest;
        using (var p = Process.GetCurrentProcess())
            p.PriorityClass = DSP_Info.ProcessPriority;

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        //Create the main form
        Form_Main = new();
        Application.Run(Form_Main);

        //Close down

        //Close the Monitoring Form if it is still open, as it runs in it's own STA thread.
        //This also closes any sub-forms like RTA etc as Form_Monitoring is the parent.
        if (Form_Monitoring != null && Form_Monitoring.IsHandleCreated)
            _ = Form_Monitoring.Invoke((MethodInvoker)delegate
            {
                Form_Monitoring.Close();
            });

        //Dispose of the Unmanaged\Unsafe NAudio ASIO ole32 Com Object wrapper
        ASIO.Dispose();
    }
}

public static class ExtensionMethods
{
    public static void SafeInvoke(this Control control, Action action)
    {
        try
        {
            if (control == null || control.IsDisposed) //|| !control.IsHandleCreated
                return;
            if (control.InvokeRequired)
                control.Invoke(action);
            else
                action();
        }
        catch (ObjectDisposedException)
        {
            // The control was disposed between the check and the invoke call.
        }
        catch (InvalidOperationException)
        {
            // Handle other potential exceptions, e.g., if the handle is lost.
        }
    }

}