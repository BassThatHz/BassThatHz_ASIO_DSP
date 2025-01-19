#nullable enable

namespace BassThatHz_ASIO_DSP_Processor
{
    using System;
    using System.Xml.Linq;

    public static class CommonFunctions
    {
        public static string RemoveDeprecatedXMLTags(string input)
        {
            XDocument doc = XDocument.Parse(input);

            // Iterate through each Limiter element
            foreach (XElement limiter in doc.Descendants("Limiter"))
            {
                // Remove PeakHoldDecayEnabled and PeakHoldDecay elements if they exist
                limiter.Element("PeakHoldDecayEnabled")?.Remove();
                limiter.Element("PeakHoldDecay")?.Remove();
            }

            return doc.ToString();
        }

        public static bool TryParseXml(string xmlString, out XDocument? xDocument)
        {
            try
            {
                xDocument = XDocument.Parse(xmlString);
                return true;
            }
            catch (Exception)
            {
                xDocument = null;
                return false;
            }
        }

        public static string TryParseXml(string xmlString)
        {
            try
            {
                _ = XDocument.Parse(xmlString);
                return "Success";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
    }
}
