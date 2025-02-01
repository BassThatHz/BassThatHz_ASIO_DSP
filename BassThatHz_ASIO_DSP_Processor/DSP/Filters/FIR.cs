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
public class FIR : IFilter
{
    #region Variables
    public int FFTSize = 8192;
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
        //Range of input is -1 to +1
        if (this.ThreadLocal_Taps_FFT_Complex == null)
            return input;

        try
        {
            //lock (this.ResizeTapsLockObject) //For Debugging
            //{

            //FFT FIR
            int InputLength = input.Length;
            int OverlapSize = this.FFTSize - InputLength;

            //Overlap-save slide
            Array.Copy(this.OverlapBuffer, InputLength, this.OverlapBuffer, 0, OverlapSize);

            //Overlap-save inject input
            Array.Copy(input, 0, this.OverlapBuffer, OverlapSize, InputLength);

            //FFT the input
            var temp_FFT = new FFT(this.FFTSize, 0);
            Complex[] Input_FFT_Complex = temp_FFT.Perform_FFT(this.OverlapBuffer, false);

            //Freq-Amplitude Convolver
            var Complex_Convolve = new Complex[this.FFTSize];
            for (int n = 0; n < this.FFTSize; n++)
                Complex_Convolve[n] = Complex.Multiply(Input_FFT_Complex[n], this.ThreadLocal_Taps_FFT_Complex[n]);

            //Inverse FFT
            double[] Result = temp_FFT.Perform_IFFT(Complex_Convolve, false);

            //Overlap-save: copy the results to the output
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

    #region Protected Functions
    //[MethodImpl(MethodImplOptions.AggressiveOptimization)]
    //protected void Convolve(double[] output, double[] input, double[] impulseResponse)
    //{
    //    #region Init
    //    double PeakLevel = 0; //Holds the highest peak level from the input stream
    //    var InputLength = input.Length;
    //    var ImpulseResponseLength = impulseResponse.Length;
    //    var OverlapBufferLength = this.OverlapBuffer.Length;
    //    var OverlapPoint = Math.Min(InputLength, OverlapBufferLength);
    //    var ResultLength = InputLength + ImpulseResponseLength - 1;
    //    var Result = new double[ResultLength];
    //    #endregion

    //    #region FIR DSP
    //    if (this.MultiThreading)
    //    {
    //        _ = Parallel.For(0, ResultLength, (n) =>
    //        {
    //            //Convolve
    //            int k;
    //            int n_Minus_k;
    //            for (k = 0; k < ImpulseResponseLength; k++)
    //                if (n >= k)
    //                {
    //                    n_Minus_k = n - k;
    //                    if (n_Minus_k < InputLength)
    //                        Result[n] += impulseResponse[k] * input[n_Minus_k];
    //                }

    //            //Perform OVERLAP-ADD
    //            if (n < OverlapPoint)
    //                Result[n] += this.OverlapBuffer[n];

    //            PeakLevel = Math.Max(PeakLevel, Math.Abs(Result[n]));
    //            //Auto Normalize Output
    //            if (this.AutoNormalizeOutput && PeakLevel > 1.0)
    //                Result[n] /= PeakLevel;
    //        });

    //        //Normalize Output
    //        if (!this.AutoNormalizeOutput && PeakLevel > 1.0)
    //            _ = Parallel.For(0, ResultLength, (n) =>
    //                Result[n] /= PeakLevel
    //            );

    //        //Next overlap
    //        OverlapBufferLength = ResultLength - InputLength;
    //        this.OverlapBuffer = new double[OverlapBufferLength];

    //        //Save results to output, and the excesses to the Overlap Buffer
    //        _ = Parallel.For(0, ResultLength, (n) =>
    //        {
    //            if (n < InputLength)
    //                output[n] = Result[n];
    //            else
    //                this.OverlapBuffer[n - InputLength] = Result[n];
    //        });
    //    }
    //    else
    //    {
    //        int n;
    //        int k;
    //        int n_Minus_k;
    //        //Convolve
    //        for (n = 0; n < ResultLength; n++)
    //        {
    //            for (k = 0; k < ImpulseResponseLength; k++)
    //                if (n >= k)
    //                {
    //                    n_Minus_k = n - k;
    //                    if (n_Minus_k < InputLength)
    //                        Result[n] += impulseResponse[k] * input[n_Minus_k];
    //                }

    //            PeakLevel = Math.Max(PeakLevel, Math.Abs(Result[n]));
    //            //Auto Normalize Output
    //            if (this.AutoNormalizeOutput && PeakLevel > 1.0)
    //                Result[n] /= PeakLevel;

    //            //Perform OVERLAP-ADD
    //            if (n < OverlapPoint)
    //                Result[n] += this.OverlapBuffer[n];
    //        }
    //        //Normalize Output
    //        if (!this.AutoNormalizeOutput && PeakLevel > 1.0)
    //            for (n = 0; n < ResultLength; n++)
    //                Result[n] /= PeakLevel;

    //        //Next overlap
    //        OverlapBufferLength = ResultLength - InputLength;
    //        this.OverlapBuffer = new double[OverlapBufferLength];

    //        //Save results to output, and the excesses to the Overlap Buffer
    //        for (n = 0; n < ResultLength; n++)
    //        {
    //            if (n < InputLength)
    //                output[n] = Result[n];
    //            else
    //                this.OverlapBuffer[n - InputLength] = Result[n];
    //        }
    //    }
    //    #endregion
    //}

    #endregion

    #region IFilter Interface

    public bool FilterEnabled { get; set; }

    public IFilter GetFilter => this;

    public FilterTypes FilterType { get; } = FilterTypes.FIR;

    public FilterProcessingTypes FilterProcessingType { get; } = FilterProcessingTypes.WholeBlock;
    #endregion
}