using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Xml.Linq;
using BassThatHz_ASIO_DSP_Processor;

namespace Test_Project_1;

[TestClass]
public class Test_CommonFunctions
{
    [TestMethod]
    public void DeepClone_ReturnsClone()
    {
        var obj = new TestCloneClass { Value = 42 };
        var clone = CommonFunctions.DeepClone(obj);
        Assert.IsNotNull(clone);
        Assert.AreNotSame(obj, clone);
        Assert.AreEqual(obj.Value, clone.Value);
    }

    private class TestCloneClass
    {
        public int Value { get; set; }
    }

    [TestMethod]
    public void RemoveDeprecatedXMLInputTags_RemovesElements()
    {
        string xml = "<Root><Limiter><PeakHoldDecayEnabled>1</PeakHoldDecayEnabled><PeakHoldDecay>2</PeakHoldDecay></Limiter></Root>";
        string result = CommonFunctions.RemoveDeprecatedXMLInputTags(xml);
        Assert.IsFalse(result.Contains("PeakHoldDecayEnabled"));
        Assert.IsFalse(result.Contains("PeakHoldDecay>"));
    }

    [TestMethod]
    public void RemoveDeprecatedXMLOutputTags_RemovesElements()
    {
        string xml = "<Root><Limiter><PeakHoldDecayEnabled>1</PeakHoldDecayEnabled><PeakHoldDecay>2</PeakHoldDecay></Limiter><DSP_Stream><InputChannelIndex>1</InputChannelIndex><OutputChannelIndex>2</OutputChannelIndex></DSP_Stream></Root>";
        string result = CommonFunctions.RemoveDeprecatedXMLOutputTags(xml);
        Assert.IsFalse(result.Contains("PeakHoldDecayEnabled"));
        Assert.IsFalse(result.Contains("PeakHoldDecay>"));
        Assert.IsFalse(result.Contains("InputChannelIndex"));
        Assert.IsFalse(result.Contains("OutputChannelIndex"));
    }

    [TestMethod]
    public void TryParseXml_ReturnsTrueForValidXml()
    {
        string xml = "<Root><Child>1</Child></Root>";
        bool ok = CommonFunctions.TryParseXml(xml, out XDocument? doc);
        Assert.IsTrue(ok);
        Assert.IsNotNull(doc);
    }

    [TestMethod]
    public void TryParseXml_ReturnsFalseForInvalidXml()
    {
        string xml = "<Root><Child>1</Root>";
        bool ok = CommonFunctions.TryParseXml(xml, out XDocument? doc);
        Assert.IsFalse(ok);
        Assert.IsNull(doc);
    }

    [TestMethod]
    public void TryParseXml_String_ReturnsSuccessOrError()
    {
        string valid = "<Root/>";
        string invalid = "<Root>";
        Assert.AreEqual("Success", CommonFunctions.TryParseXml(valid));
        Assert.AreNotEqual("Success", CommonFunctions.TryParseXml(invalid));
    }
}