namespace Test_Project_1;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Win32;
using NAudio.Wave;
using System;

[TestClass]
public class Test_ASIO_GetDriverNames
{
    [TestMethod]
    public void GetDriverNames_ReturnsStringArray_NeverNull()
    {
        IASIO_GetDriverNames sut = new ASIO_GetDriverNames();
        var names = sut.GetDriverNames();

        Assert.IsNotNull(names);
        Assert.IsInstanceOfType(names, typeof(string[]));
    }

    [TestMethod]
    public void GetDriverNames_MatchesRegistrySubKeys_OrEmptyIfKeyMissing()
    {
        IASIO_GetDriverNames sut = new ASIO_GetDriverNames();
        var names = sut.GetDriverNames();

        using var regKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\ASIO");
        if (regKey is null)
        {
            // No ASIO drivers registered on this test machine - must return empty array, not null.
            Assert.AreEqual(0, names.Length);
        }
        else
        {
            var expected = regKey.GetSubKeyNames();
            CollectionAssert.AreEquivalent(expected, names);
        }
    }

    [TestMethod]
    public void GetDriverNames_CalledMultipleTimes_IsConsistent()
    {
        IASIO_GetDriverNames sut = new ASIO_GetDriverNames();
        var first = sut.GetDriverNames();
        var second = sut.GetDriverNames();

        CollectionAssert.AreEqual(first, second);
    }

    [TestMethod]
    public void GetDriverNames_DoesNotThrow()
    {
        IASIO_GetDriverNames sut = new ASIO_GetDriverNames();
        _ = sut.GetDriverNames();
    }

    [TestMethod]
    public void ASIO_GetDriverNames_ImplementsInterface()
    {
        var sut = new ASIO_GetDriverNames();
        Assert.IsInstanceOfType<IASIO_GetDriverNames>(sut);
    }
}
