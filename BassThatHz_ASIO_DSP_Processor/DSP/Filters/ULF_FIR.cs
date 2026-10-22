
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
/// BassThatHz ASIO DSP Processor Engine
/// Copyright (c) 2026 BassThatHz
///
/// Permission is granted for educational, scientific, private, or open-source
/// community-project use, excluding commercial use or resale in substantial
/// part or whole without prior authorization.
///
/// This notice must be included in all copies or substantial portions.
///
/// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND.
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

    /// <summary>
    /// Reusable input spectrum buffer.
    /// </summary>
    protected Complex[] SpectrumBuffer = Array.Empty<Complex>();

    /// <summary>
    /// Reusable inverse FFT output buffer.
    /// </summary>
    protected double[] IFFTResultBuffer = Array.Empty<double>();

    #endregion

    #region Public Functions

    [MethodImpl(
        MethodImplOptions.AggressiveOptimization |
        MethodImplOptions.AggressiveInlining)]
    public double[] Transform(double[] input, DSP_Stream currentStream)
    {
        var tapsFft = this.ThreadLocal_Taps_FFT_Complex;
        var fft = this.ThreadLocalFFT;

        if (tapsFft == null || fft == null || input.Length == 0)
            return input;

        try
        {
            int fftSize = this.FFTSize;
            int inputLength = input.Length;

            // The overlap-save input block cannot exceed the FFT size.
            if (fftSize <= 0 ||
                (fftSize & 1) != 0 ||
                inputLength > fftSize ||
                tapsFft.Length != fftSize ||
                fft.PointCount != fftSize ||
                this.OverlapBuffer.Length != fftSize)
            {
                return input;
            }

            int overlapSize = fftSize - inputLength;
            int half = fftSize / 2;

            // Shift previous samples and append the new input block.
            Array.Copy(
                this.OverlapBuffer,
                inputLength,
                this.OverlapBuffer,
                0,
                overlapSize);

            Array.Copy(
                input,
                0,
                this.OverlapBuffer,
                overlapSize,
                inputLength);

            // Reuse FFT scratch buffers to avoid steady-state allocations.
            var spectrum = this.SpectrumBuffer;

            if (spectrum.Length != fftSize)
            {
                spectrum = new Complex[fftSize];
                this.SpectrumBuffer = spectrum;
            }

            var resultBuffer = this.IFFTResultBuffer;

            if (resultBuffer.Length != fftSize)
            {
                resultBuffer = new double[fftSize];
                this.IFFTResultBuffer = resultBuffer;
            }

            Complex[] inputFft = fft.Perform_FFT_Into(
                this.OverlapBuffer,
                spectrum,
                false);

            // ---------------------------------------------------------
            // Correct frequency-domain mapping
            // ---------------------------------------------------------

            double inputSampleRate = Program.DSP_Info.InSampleRate;
            double tapsSampleRate = this.TapsSampleRate;

            if (inputSampleRate <= 0.0 || tapsSampleRate <= 0.0)
                return input;

            // Input FFT bin n represents:
            //
            //     f = n * inputSampleRate / fftSize
            //
            // FIR FFT bin m represents:
            //
            //     f = m * tapsSampleRate / fftSize
            //
            // Therefore:
            //
            //     m = n * inputSampleRate / tapsSampleRate
            //
            // Do not round this mapping: interpolate adjacent bins.
            double binRatio = inputSampleRate / tapsSampleRate;

            // Highest input FFT bin that is at or below the FIR's
            // Nyquist frequency.
            //
            // This is NOT half * binRatio.
            int maxInputBin = (int)Math.Floor(
                half / binRatio);

            maxInputBin = Math.Clamp(maxInputBin, 0, half);

            // Process the positive-frequency spectrum.
            // Construct its negative-frequency half by conjugate
            // symmetry to preserve a real-valued IFFT output.
            for (int n = 0; n <= half; n++)
            {
                Complex value;

                if (n > maxInputBin)
                {
                    // Explicitly zero every bin outside the bandwidth
                    // supported by the low-rate FIR.
                    value = Complex.Zero;
                }
                else
                {
                    double m = n * binRatio;

                    int m0 = (int)Math.Floor(m);
                    int m1 = Math.Min(m0 + 1, half);

                    double fraction = m - m0;

                    // Linear interpolation of the complex FIR response.
                    // This avoids the frequency quantization introduced
                    // by Math.Round(n * binRatio).
                    Complex h0 = tapsFft[m0];
                    Complex h1 = tapsFft[m1];

                    Complex h = h0 + (h1 - h0) * fraction;

                    value = inputFft[n] * h;
                }

                // DC and Nyquist must be real for a real-valued
                // time-domain signal.
                if (n == 0 || n == half)
                {
                    value = new Complex(value.Real, 0.0);
                }

                inputFft[n] = value;

                // Mirror every positive-frequency bin except DC
                // and Nyquist. This also guarantees that the negative
                // out-of-band bins are explicitly zeroed.
                if (n > 0 && n < half)
                {
                    inputFft[fftSize - n] =
                        Complex.Conjugate(value);
                }
            }

            // Inverse FFT.
            double[] result = fft.Perform_IFFT_Into(
                inputFft,
                resultBuffer,
                false);

            // Keep only the valid overlap-save output section.
            Array.Copy(
                result,
                overlapSize,
                input,
                0,
                inputLength);
        }
        catch (Exception)
        {
            // Retain the original input if processing encounters
            // a transient configuration or buffer-size mismatch.
        }

        return input;
    }

    public void ResetSampleRate(int sampleRate)
    {
        // Non-applicable.
    }

    public void ApplySettings()
    {
        // Non-applicable.
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public void SetTaps(double[] input)
    {
        if (input == null)
            throw new ArgumentNullException(nameof(input));

        lock (this.ResizeTapsLockObject)
        {
            int fftSize = this.FFTSize;

            if (fftSize <= 0 || (fftSize & 1) != 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(this.FFTSize),
                    "FFTSize must be a positive even number.");
            }

            if (input.Length == 0 || input.Length > fftSize)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(input),
                    "Tap length must be between 1 and FFTSize.");
            }

            if (this.TapsSampleRate <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(this.TapsSampleRate),
                    "TapsSampleRate must be positive.");
            }

            // Make a private copy so external modifications to the
            // original tap array do not alter the active coefficients.
            var tapsCopy = new double[input.Length];

            Array.Copy(input, tapsCopy, input.Length);

            var paddedTaps = new double[fftSize];

            Array.Copy(
                tapsCopy,
                paddedTaps,
                tapsCopy.Length);

            var fft = new FFT(fftSize, 0);

            Complex[] tapsSpectrum = fft.Perform_FFT(
                paddedTaps,
                false);

            // Publish the new configuration after preparation.
            this.Taps = input;
            this.ThreadLocal_Taps = tapsCopy;
            this.ThreadLocal_Taps_FFT_Complex = tapsSpectrum;
            this.ThreadLocalFFT = fft;

            // Reset overlap history when changing filter coefficients.
            this.OverlapBuffer = new double[fftSize];

            // Scratch buffers are resized lazily by Transform.
            this.SpectrumBuffer = Array.Empty<Complex>();
            this.IFFTResultBuffer = Array.Empty<double>();
        }
    }

    #endregion

    #region IFilter Interface

    public bool FilterEnabled { get; set; }

    public IFilter GetFilter => this;

    public FilterTypes FilterType { get; } = FilterTypes.ULF_FIR;

    public FilterProcessingTypes FilterProcessingType { get; }
        = FilterProcessingTypes.WholeBlock;

    public IFilter DeepClone()
    {
        return CommonFunctions.DeepClone(this);
    }

    #endregion
}
