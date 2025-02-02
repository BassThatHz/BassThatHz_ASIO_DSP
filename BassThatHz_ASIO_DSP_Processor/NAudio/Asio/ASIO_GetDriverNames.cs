#nullable disable

namespace NAudio.Wave;

#region Usings
using Microsoft.Win32;
using System;
#endregion

public class ASIO_GetDriverNames : IASIO_GetDriverNames
{
    /// <summary>
    /// Gets the ASIO driver names installed.
    /// </summary>
    /// <returns>a list of driver names. Use this name to GetAsioDriverByName</returns>
    public string[] GetDriverNames()
    {
        var names = Array.Empty<string>();
        var regKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\ASIO");

        if (regKey != null)
        {
            names = regKey.GetSubKeyNames();
            regKey.Close();
        }

        return names;
    }
}

public interface IASIO_GetDriverNames
{
    string[] GetDriverNames();
}