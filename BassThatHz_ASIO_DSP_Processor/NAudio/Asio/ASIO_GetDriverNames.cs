#nullable disable

namespace NAudio.Wave;

#region Usings
using Microsoft.Win32;
using System;
using System.Runtime.CompilerServices;
#endregion

public sealed class ASIO_GetDriverNames : IASIO_GetDriverNames
{
    /// <summary>
    /// Gets the ASIO driver names installed.
    /// </summary>
    /// <returns>a list of driver names. Use this name to GetAsioDriverByName</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string[] GetDriverNames()
    {
        using var regKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\ASIO");
        if (regKey is null)
        {
            return Array.Empty<string>();
        }

        // RegistryKey.GetSubKeyNames returns a string[]; return it directly to avoid extra allocations.
        return regKey.GetSubKeyNames();
    }
}

public interface IASIO_GetDriverNames
{
    string[] GetDriverNames();
}
