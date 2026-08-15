namespace Test_Project_1;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Win32;
using NAudio.Wave;
using System;

// NOTE: ASIO_Unified wraps a real ASIO COM driver via CoCreateInstance/vtable interop. Instantiating
// it (via any public constructor) requires a genuine ASIO driver installed and registered on the
// machine. In this test environment there is generally no ASIO hardware/driver present, so these
// tests focus on:
//   1) Pure static logic that reads the Windows registry (GetAsioDriverNames) - no driver required.
//   2) Error paths that are reachable BEFORE any COM interop occurs (e.g. invalid driver name/index,
//      no drivers installed) which throw ArgumentException without ever touching COM.
// Any code path that requires InstantiateAsioDriverByName/InitFromGuid to succeed against a real
// driver is intentionally NOT exercised here.
[TestClass]
public class Test_ASIO_Unified
{
    [TestMethod]
    public void GetAsioDriverNames_ReturnsStringArray_NeverNull()
    {
        var names = ASIO_Unified.GetAsioDriverNames();
        Assert.IsNotNull(names);
        Assert.IsInstanceOfType(names, typeof(string[]));
    }

    [TestMethod]
    public void GetAsioDriverNames_MatchesRegistry_OrEmptyIfKeyMissing()
    {
        var names = ASIO_Unified.GetAsioDriverNames();

        using var regKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\ASIO");
        if (regKey is null)
        {
            Assert.AreEqual(0, names.Length);
        }
        else
        {
            CollectionAssert.AreEquivalent(regKey.GetSubKeyNames(), names);
        }
    }

    [TestMethod]
    public void Constructor_WithUnknownDriverName_ThrowsArgumentException()
    {
        // InstantiateAsioDriverByName opens "SOFTWARE\ASIO\<name>" - a name that can never be a real
        // registered driver should reliably throw ArgumentException before any COM interop occurs.
        const string bogusName = "Definitely_Not_A_Real_ASIO_Driver_12345_ZzZ";

        var ex = Assert.ThrowsExactly<ArgumentException>(() => new ASIO_Unified(bogusName));
        StringAssert.Contains(ex.Message, bogusName);
    }

    [TestMethod]
    public void GetAsioDriverByName_WithUnknownDriverName_ThrowsArgumentException()
    {
        const string bogusName = "Another_Bogus_Driver_Name_98765";
        Assert.ThrowsExactly<ArgumentException>(() => ASIO_Unified.GetAsioDriverByName(bogusName));
    }

    [TestMethod]
    public void Constructor_ByIndex_WhenNoDriversInstalled_ThrowsArgumentException()
    {
        var names = ASIO_Unified.GetAsioDriverNames();
        if (names.Length > 0)
        {
            Assert.Inconclusive("This test requires a machine with no ASIO drivers installed; drivers were found.");
            return;
        }

        Assert.ThrowsExactly<ArgumentException>(() => new ASIO_Unified(0));
    }

    [TestMethod]
    public void Constructor_ByIndex_NegativeIndex_ThrowsArgumentException()
    {
        var names = ASIO_Unified.GetAsioDriverNames();
        if (names.Length == 0)
        {
            // "There is no ASIO Driver installed" is thrown first regardless of the index value.
            Assert.ThrowsExactly<ArgumentException>(() => new ASIO_Unified(-1));
        }
        else
        {
            Assert.ThrowsExactly<ArgumentException>(() => new ASIO_Unified(-1));
        }
    }

    [TestMethod]
    public void Constructor_ByIndex_IndexEqualToLength_ThrowsArgumentException()
    {
        // Regression test for a fixed off-by-one: NAudio\Asio\ASIO_Unified.cs constructor
        // `ASIO_Unified(int driverIndex)` used to check `driverIndex < 0 || driverIndex > names.Length`,
        // which incorrectly let `driverIndex == names.Length` fall through to `names[driverIndex]`
        // (an IndexOutOfRangeException) instead of the intended ArgumentException. Fixed to
        // `driverIndex >= names.Length`.
        var names = ASIO_Unified.GetAsioDriverNames();
        if (names.Length == 0)
        {
            // With zero drivers, the "no driver installed" check fires first (unaffected by the defect).
            Assert.ThrowsExactly<ArgumentException>(() => new ASIO_Unified(0));
            return;
        }

        Assert.ThrowsExactly<ArgumentException>(() => new ASIO_Unified(names.Length));
    }

    [TestMethod]
    public void GetAsioDriverByGuid_WithUnknownGuid_ThrowsComException()
    {
        // Unlike the string-name constructor, the GUID constructor (InitFromGuid) calls
        // CoCreateInstance directly with the supplied GUID as the CLSID, bypassing the registry
        // name-lookup entirely. A random, unregistered GUID is not a registered COM class, so
        // CoCreateInstance fails (e.g. REGDB_E_CLASSNOTREG) and a COMException is thrown.
        var unknownGuid = Guid.NewGuid();
        Assert.ThrowsExactly<System.Runtime.InteropServices.COMException>(
            () => ASIO_Unified.GetAsioDriverByGuid(unknownGuid));
    }

    [TestMethod]
    public void IASIO_Unified_Interface_IsImplementedByASIO_Unified()
    {
        Assert.IsTrue(typeof(IASIO_Unified).IsAssignableFrom(typeof(ASIO_Unified)));
    }
}
