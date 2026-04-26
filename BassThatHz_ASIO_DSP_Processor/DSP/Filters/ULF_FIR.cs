#nullable enable

namespace BassThatHz_ASIO_DSP_Processor;

#region Usings
using DSPLib;
using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
#endregion

/// <summary>
///  BassThatHz ASIO DSP Processor Engine
///  Copyright (c) 2026 BassThatHz
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
    protected FFT? ThreadLocalFFT;
    #endregion

    #region Public Functions
    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public double[] Transform(double[] input, DSP_Stream currentStream)
    {
        if (this.ThreadLocal_Taps_FFT_Complex == null)
            return input;

        try
        {
            int tapsLength = this.ThreadLocal_Taps_FFT_Complex.Length;
            int inputLength = input.Length;
            int overlapSize = this.FFTSize - inputLength;

            // Overlap-save slide and inject input
            Array.Copy(this.OverlapBuffer, inputLength, this.OverlapBuffer, 0, overlapSize);
            Array.Copy(input, 0, this.OverlapBuffer, overlapSize, inputLength);

            // Use cached FFT instance if available
            var fft = this.ThreadLocalFFT ?? new FFT(this.FFTSize, 0);
            Complex[] inputFft = fft.Perform_FFT(this.OverlapBuffer, false);

            // Precompute ratios
            double tapsSampleRate = this.TapsSampleRate;
            double inputSampleRate = Program.DSP_Info.InSampleRate;
            double inputToTapsRatio = inputSampleRate / tapsSampleRate; // used to map bin indices

            double nyquist = tapsSampleRate * 0.5;
            double cutoff = nyquist * 0.9;
            double rolloff = nyquist * 0.1;

            int half = this.FFTSize / 2;
            var tapsFft = this.ThreadLocal_Taps_FFT_Complex;

            // Process only positive frequencies and mirror to negative frequencies to halve work
            for (int n = 0; n <= half; n++)
            {
                double f_n = n * (inputSampleRate / (double)this.FFTSize);
                double attenuation = f_n >= cutoff ? Math.Exp(-Math.Pow((f_n - cutoff) / rolloff, 2)) : 1.0;

                Complex val = inputFft[n] * attenuation;

                int mIndex = (int)Math.Round(n * inputToTapsRatio);
                if (mIndex >= 0 && mIndex < tapsLength && tapsFft != null)
                {
                    val *= tapsFft[mIndex];
                }
                else
                {
                    val = Complex.Zero;
                }

                inputFft[n] = val;
                if (n > 0 && n < half)
                    inputFft[this.FFTSize - n] = Complex.Conjugate(val);
            }

            double[] result = fft.Perform_IFFT(inputFft, false);
            Array.Copy(result, overlapSize, input, 0, inputLength);
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
            this.ThreadLocalFFT = temp_FFT;

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

    public IFilter DeepClone()
    {
        return CommonFunctions.DeepClone(this);
    }
    #endregion
}