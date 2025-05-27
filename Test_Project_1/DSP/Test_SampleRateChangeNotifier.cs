using BassThatHz_ASIO_DSP_Processor;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Test_Project_1
{
    [TestClass]
    public class Test_SampleRateChangeNotifier
    {
        [TestMethod]
        public void SampleRateChangeNotifier_RaisesEvent_WithCorrectValue()
        {
            int? receivedSampleRate = null;
            void Handler(int newSampleRate) => receivedSampleRate = newSampleRate;
            SampleRateChangeNotifier.SampleRateChanged += Handler;
            try
            {
                SampleRateChangeNotifier.NotifySampleRateChange(44100);
                Assert.AreEqual(44100, receivedSampleRate);
            }
            finally
            {
                SampleRateChangeNotifier.SampleRateChanged -= Handler;
            }
        }
    }
}