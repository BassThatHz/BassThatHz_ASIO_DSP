#nullable enable

namespace BassThatHz_ASIO_DSP_Processor;

#region Usings
using System;
using System.IO;
using System.Runtime.ExceptionServices;
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
public static class Debug
{
    public static void GlobalError(Exception ex)
    {
        try
        {
            Program.ASIO.Stop(); //Try to stop ASIO (just in case it is running to prevent audio buffer underruns.)
        }
        catch (Exception ex2)
        {
            _ = ex2; //Swallow ASIO Error, we don't care at this point
        }

        _ = ex; //Set global debug breakpoint here
    }

    public static void Error(Exception ex)
    {
        _ = MessageBox.Show(ex.Message + ex.StackTrace, "A fatal error has occured");

        var dialogResult = MessageBox.Show("Save detailed error report to file before closing app?",
                                    "A fatal error has occured", MessageBoxButtons.YesNo);
        if (dialogResult == DialogResult.Yes)
        {
            var UpTime = (DateTime.Now - Program.App_StartTime).ToString("c");
            var UTCDateTime = "UTC " + DateTime.UtcNow.ToString();
            var UTCFileName = UTCDateTime.Replace("/", "_").Replace(":", "_").Replace(" ", "_");
            var FileName = "ASIO_ErrorReport_" + UTCFileName + ".txt";
            var FilePath = AppDomain.CurrentDomain.BaseDirectory + @"\" + FileName;

            var ErrorMessage = "UpTime: " + UpTime + " " + UTCDateTime + " : " + ex.Message
                                + "\r\n" + ex.StackTrace + "\r\n" + ex.InnerException?.Message;

            File.WriteAllText(FilePath, ErrorMessage);
            _ = MessageBox.Show(FilePath, "Error report saved to file:");
        }

        var dialogResult2 = MessageBox.Show("Press Yes to abort the app (recommended), " +
                                        "or No to ignore the error and attempt to continue running in an errored state.",
                                        "A fatal error has occured", MessageBoxButtons.YesNo);
        if (dialogResult2 == DialogResult.Yes)
            throw ex;
    }

    public static void GlobalError(object? sender, FirstChanceExceptionEventArgs e)
    {
        _ = e.Exception;
        //GlobalError(e.Exception);
    }

    public static void GlobalError(object? sender, UnhandledExceptionEventArgs e)
    {
        GlobalError((Exception)e.ExceptionObject);
    }
}