#nullable enable

namespace BassThatHz_ASIO_DSP_Processor
{
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
    public interface IFilter : IApplySettings
    {
        #region Properties
        //Stores and toggles the filters enabled state
        bool FilterEnabled { get; set; }

        //An enum of the various different types of filters
        FilterTypes FilterType { get; }

        //Wholeblock or Per Sample etc
        FilterProcessingTypes FilterProcessingType { get; }
        #endregion

        #region Functions
        //Transform using a whole block of data
        double[] Transform(double[] input, DSP_Stream currentStream);

        //Notification of Sample Rate being reset
        void ResetSampleRate(int sampleRate);

        //Notification of Settings changes needing to be applied
        new void ApplySettings();
        #endregion
    }

    public interface IGetFilter
    {
        //IFilter Getter
        IFilter GetFilter { get; }
    }

}