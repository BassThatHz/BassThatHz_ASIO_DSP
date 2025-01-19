#nullable enable

namespace BassThatHz_ASIO_DSP_Processor
{
    using DSPLib;
    using System;
    using System.Collections.Generic;
    using System.Numerics;
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;

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
    [Serializable]
    public class ULF_FIR : IFilter
    {
        #region Variables
        public int FFTSize = 8192;
        public int TapsSampleRateIndex = 1;
        public int TapsSampleRate = 960;
        protected object ResizeTapsLockObject = new();
        public double[]? Taps;
        protected double[]? ThreadLocal_Taps;
        protected Complex[]? ThreadLocal_Taps_FFT_Complex;
        protected double[] OverlapBuffer = Array.Empty<double>();
        #endregion

        #region Public Functions
        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
        public double[] Transform(double[] input, DSP_Stream currentStream)
        {
            if (this.ThreadLocal_Taps_FFT_Complex == null)
                return input;

            try
            {
                //lock (this.ResizeTapsLockObject) //For Debugging
                //{

                int TapsLength = this.ThreadLocal_Taps_FFT_Complex.Length;
                int InputLength = input.Length;
                int OverlapSize = this.FFTSize - InputLength;

                // Overlap-save slide
                Array.Copy(this.OverlapBuffer, InputLength, this.OverlapBuffer, 0, OverlapSize);

                // Overlap-save inject input
                Array.Copy(input, 0, this.OverlapBuffer, OverlapSize, InputLength);

                // Perform FFT on OverlapBuffer
                var temp_FFT = new FFT(this.FFTSize, 0);
                Complex[] Input_FFT = temp_FFT.Perform_FFT(this.OverlapBuffer, false);

                // Frequency resolution calculations
                double taps_SampleRate = this.TapsSampleRate; // 960 Hz
                double input_SampleRate = Program.DSP_Info.InSampleRate; // 96,000 Hz
                double delta_f_input = input_SampleRate / (double)this.FFTSize;
                double delta_f_taps = taps_SampleRate / (double)this.FFTSize;

                double nyquist = taps_SampleRate * 0.5; // 480 Hz
                double cutoff_frequency = nyquist * 0.9; // Set cutoff at 90% of Nyquist
                double rolloff_width = nyquist * 0.1; // Gaussian roll-off width (10% of Nyquist)

                // Perform frequency-domain convolution directly on Input_FFT
                for (int n = 0; n < this.FFTSize; n++)
                {
                    double f_n = n * delta_f_input;
                    double attenuation = f_n >= cutoff_frequency
                        ? Math.Exp(-Math.Pow((f_n - cutoff_frequency) / rolloff_width, 2))
                        : 1.0;

                    // Apply the attenuation to Input_FFT directly
                    Input_FFT[n] *= attenuation;

                    // Use nearest interpolation for filter response and combine it in Input_FFT
                    int m_index = (int)Math.Round(f_n / delta_f_taps);
                    if (m_index >= 0 && m_index < TapsLength)
                        Input_FFT[n] *= this.ThreadLocal_Taps_FFT_Complex[m_index];
                    else
                        Input_FFT[n] = Complex.Zero;

                    // Mirror the positive frequencies to the negative frequencies (complex conjugate)
                    if (n > 0 && n < this.FFTSize / 2)
                        Input_FFT[this.FFTSize - n] = Complex.Conjugate(Input_FFT[n]);
                }

                // Perform IFFT
                double[] Result = temp_FFT.Perform_IFFT(Input_FFT, false);

                // Overlap-save: copy the result to the output
                Array.Copy(Result, OverlapSize, input, 0, InputLength);

                //} //For Debugging
            }
            catch (Exception ex)
            {
                _ = ex; //User probably changed FFTSize or Taps while the DSP was running
            }

            return input;
        }

        public void ResetSampleRate(int sampleRate)
        {
            //Non Applicable
        }

        public void ApplySettings()
        {
            //Non Applicable
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public void SetTaps(double[] input)
        {
            this.Taps = input;
            this.OverlapBuffer = Array.Empty<double>(); //Empty the OverlapBuffer 
            lock (this.ResizeTapsLockObject)
            {
                var TapsLength = input.Length;
                //Make a ThreadLocal copy of the taps for internal use, as it can be changed from other threads (UI etc)               
                this.ThreadLocal_Taps = new double[TapsLength];
                _ = Parallel.For(0, TapsLength, (n) =>
                     this.ThreadLocal_Taps[n] = input[n]);

                var temparray = new double[this.FFTSize];
                Array.Copy(input, temparray, input.Length);
                var temp_FFT = new FFT(this.FFTSize, 0);
                this.ThreadLocal_Taps_FFT_Complex = temp_FFT.Perform_FFT(temparray, false);

                if (this.OverlapBuffer.Length != this.FFTSize)
                    this.OverlapBuffer = new double[this.FFTSize];
            }
        }
        #endregion

        #region IFilter Interface

        public bool FilterEnabled { get; set; }

        public IFilter GetFilter => this;

        public FilterTypes FilterType { get; } = FilterTypes.ULF_FIR;

        public FilterProcessingTypes FilterProcessingType { get; } = FilterProcessingTypes.WholeBlock;
        #endregion
    }
}