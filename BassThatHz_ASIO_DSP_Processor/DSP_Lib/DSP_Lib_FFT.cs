#nullable disable

namespace DSPLib
{
    using System;
    using System.Numerics;
    using Windows.Storage.Streams;

    #region License
    /**
     * Performs an in-place complex FFT.
     * Released under the MIT License
     * Core FFT class based on,
     *      Fast C# FFT - Copyright (c) 2010 Gerald T. Beauregard
     * These changes as noted above Copyright (c) 2016 Steven C. Hageman
     * Permission is hereby granted, free of charge, to any person obtaining a copy
     * of this software and associated documentation files (the "Software"), to
     * deal in the Software without restriction, including without limitation the
     * rights to use, copy, modify, merge, publish, distribute, sublicense, and/or
     * sell copies of the Software, and to permit persons to whom the Software is
     * furnished to do so, subject to the following conditions:
     *
     * The above copyright notice and this permission notice shall be included in
     * all copies or substantial portions of the Software.
     *
     * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
     * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
     * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
     * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
     * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
     * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS
     * IN THE SOFTWARE.
     */
    #endregion

    public class FFT
    {
        #region Variables

        protected double FFTScale = 1.0;
        protected int LogN = 0;       // log2 of FFT size
        protected int N = 0;          // Time series length
        protected int LengthTotal;    // mN + mZp
        protected int LengthHalf;     // (mN + mZp) / 2
        protected FFTElement[] FFTElements;        // Vector of linked list elements

        // Reusable scratch buffers for the *_NonPower2 zero-padding wrappers.
        // These are per-FFT-instance, exactly like FFTElements: an FFT object is inherently
        // single-threaded (every Perform_* call mutates FFTElements in place), so reusing a
        // per-instance scratch array adds no sharing hazard that did not already exist.
        protected double[] PaddedTimeSeriesScratch;
        protected Complex[] PaddedSpectrumScratch;

        /// <summary>
        /// Total number of points this instance was initialized for (inputDataLength +
        /// zeroPaddingLength). Exposed so callers can size a reusable buffer for the
        /// Perform_*_Into overloads without guessing.
        /// </summary>
        public int PointCount => LengthTotal;

        // Element for linked list to store input/output data.
        public class FFTElement
        {
            public double re = 0.0;     // Real component
            public double im = 0.0;     // Imaginary component
            public FFTElement next;     // Next element in linked list
            public int revTgt;       // Target position post bit-reversal
        }

        #endregion

        #region Constructor
        public FFT(int inputDataLength, int zeroPaddingLength = 0)
        {
            Init(inputDataLength, zeroPaddingLength);
        }

        public FFT()
        {
        }

        protected void Init(int inputDataLength, int zeroPaddingLength = 0)
        {
            N = inputDataLength;
            // Find the power of two for the total FFT size up to 2^32
            int total = inputDataLength + zeroPaddingLength;
            LogN = 0;
            int pow = 1;
            while (pow < total && LogN < 31)
            {
                pow <<= 1;
                LogN++;
            }

            if (pow != total)
                throw new ArgumentOutOfRangeException("inputDataLength + zeroPaddingLength was not an even power of 2! FFT cannot continue.");

            // Set global parameters.
            LengthTotal = inputDataLength + zeroPaddingLength;
            LengthHalf = LengthTotal / 2 + 1;

            // Set the overall scale factor for all the terms
            FFTScale = Math.Sqrt(2) / (double)LengthTotal; // Natural FFT Scale Factor  // Window Scale Factor
            FFTScale *= (double)LengthTotal / (double)inputDataLength; // Zero Padding Scale Factor


            // Allocate elements for linked list of complex numbers only when size changed
            if (FFTElements == null || FFTElements.Length != LengthTotal)
            {
                FFTElements = new FFTElement[LengthTotal];
                for (int k = 0; k < LengthTotal; k++)
                    FFTElements[k] = new FFTElement();

                // Set up "next" pointers once for the allocated array
                for (int k = 0; k < LengthTotal - 1; k++)
                    FFTElements[k].next = FFTElements[k + 1];
                FFTElements[LengthTotal - 1].next = null;
            }

            // Specify target for bit reversal re-ordering.
            for (int k = 0; k < LengthTotal; k++)
            {
                FFTElements[k].revTgt = BitReverse(k, LogN);
                // Reset values
                FFTElements[k].re = 0.0;
                FFTElements[k].im = 0.0;
            }
        }
        #endregion

        #region FFT\IFFT Functions

        public Complex[] Perform_FFT_NonPower2(double[] input, bool shouldScale = true, bool shouldUnpad = false)
        {
            // Check if the input signal is a power of 2
            if (Math.Log2(input.Length) % 1 == 0)
            {
                Init(input.Length, 0);
                return Perform_FFT(input, shouldScale);
            }
            else
            {
                // Zero pad the input signal to the next power of 2
                int nextPowerOf2 = 1;
                while (nextPowerOf2 < input.Length)
                    nextPowerOf2 *= 2;

                Init(nextPowerOf2, 0);

                // Reuse the per-instance padding scratch buffer. The tail past input.Length is
                // explicitly re-zeroed so the padded contents are identical to a fresh array.
                double[] paddedTimeSeries = PaddedTimeSeriesScratch;
                if (paddedTimeSeries == null || paddedTimeSeries.Length != nextPowerOf2)
                {
                    paddedTimeSeries = new double[nextPowerOf2];
                    PaddedTimeSeriesScratch = paddedTimeSeries;
                }
                Array.Copy(input, paddedTimeSeries, input.Length);
                Array.Clear(paddedTimeSeries, input.Length, nextPowerOf2 - input.Length);

                // Perform the FFT on the padded signal
                Complex[] paddedResult = Perform_FFT(paddedTimeSeries, shouldScale);

                // Unpad the output
                if (shouldUnpad)
                {
                    Complex[] unpaddedResult = new Complex[input.Length];
                    Array.Copy(paddedResult, unpaddedResult, input.Length);
                    return unpaddedResult;
                }
                return paddedResult;
            }
        }

        /// <summary>
        /// Allocating form. Always returns a freshly allocated Complex[LengthTotal].
        /// </summary>
        public Complex[] Perform_FFT(double[] input, bool shouldScale = true)
        {
            return Perform_FFT_Into(input, new Complex[LengthTotal], shouldScale);
        }

        /// <summary>
        /// Non-allocating form of <see cref="Perform_FFT(double[], bool)"/>: unswizzles into the
        /// caller supplied <paramref name="output"/> buffer instead of allocating one per call.
        /// The arithmetic, the operand order and the write order are identical to the allocating
        /// overload, so results are bit-for-bit the same.
        /// <para>
        /// The buffer is fully overwritten (revTgt is a permutation of 0..LengthTotal-1), so stale
        /// content from a previous call cannot leak through. <paramref name="output"/> must not
        /// alias any array the caller still needs, and must be at least LengthTotal long.
        /// </para>
        /// </summary>
        public Complex[] Perform_FFT_Into(double[] input, Complex[] output, bool shouldScale = true)
        {
            int numFlies = LengthTotal >> 1;  // Number of butterflies per sub-FFT
            int span = LengthTotal >> 1;      // Width of the butterfly
            int spacing = LengthTotal;        // Distance between start of sub-FFTs
            int wIndexStep = 1;          // Increment for twiddle table index

            if (input.Length > LengthTotal)
                throw new InvalidOperationException("The input timeSeries length was greater than the total number of points that was initialized.");

            if (output == null || output.Length < LengthTotal)
                throw new InvalidOperationException("The output spectrum buffer was smaller than the total number of points that was initialized.");

            // Copy data into linked complex number objects (use indexed access for performance)
            var elems = FFTElements;
            int lenTotal = LengthTotal;
            int n = N;
            for (int i = 0; i < n; i++)
            {
                var e = elems[i];
                e.re = input[i];
                e.im = 0.0;
            }

            // If zero padded, clean the 2nd half of the linked list from previous results
            if (n != lenTotal)
            {
                for (int i = n; i < lenTotal; i++)
                {
                    var e = elems[i];
                    e.re = 0.0;
                    e.im = 0.0;
                }
            }

            // For each stage of the FFT
            for (int stage = 0; stage < LogN; stage++)
            {
                // Compute a multiplier factor for the "twiddle factors".
                // The twiddle factors are complex unit vectors spaced at
                // regular angular intervals. The angle by which the twiddle
                // factor advances depends on the FFT stage. In many FFT
                // implementations the twiddle factors are cached, but because
                // array lookup is relatively slow in C#, it's just
                // as fast to compute them on the fly.
                double wAngleInc = wIndexStep * -2.0 * Math.PI / LengthTotal;
                double wMulRe = Math.Cos(wAngleInc);
                double wMulIm = Math.Sin(wAngleInc);

                for (int start = 0; start < lenTotal; start += spacing)
                {
                    FFTElement xTop = elems[start];
                    FFTElement xBot = elems[start + span];

                    double wRe = 1.0;
                    double wIm = 0.0;

                    // For each butterfly in this stage
                    for (int flyCount = 0; flyCount < numFlies; ++flyCount)
                    {
                        // Get the top & bottom values
                        double xTopRe = xTop.re;
                        double xTopIm = xTop.im;
                        double xBotRe = xBot.re;
                        double xBotIm = xBot.im;

                        // Top branch of butterfly has addition
                        xTop.re = xTopRe + xBotRe;
                        xTop.im = xTopIm + xBotIm;

                        // Bottom branch of butterfly has subtraction,
                        // followed by multiplication by twiddle factor
                        xBotRe = xTopRe - xBotRe;
                        xBotIm = xTopIm - xBotIm;
                        xBot.re = xBotRe * wRe - xBotIm * wIm;
                        xBot.im = xBotRe * wIm + xBotIm * wRe;

                        // Advance butterfly to next top & bottom positions
                        xTop = xTop.next;
                        xBot = xBot.next;

                        // Update the twiddle factor, via complex multiply
                        // by unit vector with the appropriate angle
                        // (wRe + j wIm) = (wRe + j wIm) x (wMulRe + j wMulIm)
                        double tRe = wRe;
                        wRe = wRe * wMulRe - wIm * wMulIm;
                        wIm = tRe * wMulIm + wIm * wMulRe;
                    }
                }

                numFlies >>= 1;   // Divide by 2 by right shift
                span >>= 1;
                spacing >>= 1;
                wIndexStep <<= 1;     // Multiply by 2 by left shift
            }
            // The algorithm leaves the result in a scrambled order.
            // Unscramble while copying values from the complex
            // linked list elements to a complex output vector & properly apply scale factors.

            var unswizzle = output;
            double s = FFTScale;
            if (shouldScale)
            {
                for (int k = 0; k < lenTotal; k++)
                {
                    var e = elems[k];
                    unswizzle[e.revTgt] = new Complex(e.re * s, e.im * s);
                }

                // DC and Fs/2 Points are scaled differently, since they have only a real part
                Apply_DCAndNyquistScale(unswizzle);
            }
            else
            {
                for (int k = 0; k < lenTotal; k++)
                {
                    var e = elems[k];
                    unswizzle[e.revTgt] = new Complex(e.re, e.im);
                }
            }
            // Return 1/2 the FFT result from DC to Fs/2 (The real part of the spectrum)
            //int halfLength = ((mN + mZp) / 2) + 1;
            //Complex[] result = new Complex[mLengthHalf];
            //Array.Copy(unswizzle, result, mLengthHalf);

            return unswizzle;
        }

        /// <summary>
        /// Allocating form. Always returns a freshly allocated Complex[LengthTotal].
        /// </summary>
        public Complex[] Perform_FFT(double[] input, double[] windowCoefficients, bool shouldScale = true)
        {
            return Perform_FFT_Into(input, windowCoefficients, new Complex[LengthTotal], shouldScale);
        }

        /// <summary>
        /// Non-allocating form of <see cref="Perform_FFT(double[], double[], bool)"/>. See the
        /// remarks on <see cref="Perform_FFT_Into(double[], Complex[], bool)"/>; the arithmetic is
        /// unchanged, only the destination of the unswizzle step.
        /// </summary>
        public Complex[] Perform_FFT_Into(double[] input, double[] windowCoefficients, Complex[] output, bool shouldScale = true)
        {
            int numFlies = LengthTotal >> 1;  // Number of butterflies per sub-FFT
            int span = LengthTotal >> 1;      // Width of the butterfly
            int spacing = LengthTotal;        // Distance between start of sub-FFTs
            int wIndexStep = 1;          // Increment for twiddle table index

            if (input.Length > LengthTotal)
                throw new InvalidOperationException("The input timeSeries length was greater than the total number of points that was initialized.");

            if (input.Length != windowCoefficients.Length)
                throw new InvalidOperationException("windowCoefficients must be same length as timeSeries");

            if (output == null || output.Length < LengthTotal)
                throw new InvalidOperationException("The output spectrum buffer was smaller than the total number of points that was initialized.");

            // Copy data into linked complex number objects (use indexed access for performance)
            var elems = FFTElements;
            int lenTotal = LengthTotal;
            int n = N;
            for (int i = 0; i < n; i++)
            {
                var e = elems[i];
                e.re = input[i] * windowCoefficients[i];
                e.im = 0.0;
            }

            // If zero padded, clean the 2nd half of the linked list from previous results
            if (n != lenTotal)
            {
                for (int i = n; i < lenTotal; i++)
                {
                    var e = elems[i];
                    e.re = 0.0;
                    e.im = 0.0;
                }
            }

            // For each stage of the FFT
            for (int stage = 0; stage < LogN; stage++)
            {
                // Compute a multiplier factor for the "twiddle factors".
                // The twiddle factors are complex unit vectors spaced at
                // regular angular intervals. The angle by which the twiddle
                // factor advances depends on the FFT stage. In many FFT
                // implementations the twiddle factors are cached, but because
                // array lookup is relatively slow in C#, it's just
                // as fast to compute them on the fly.
                double wAngleInc = wIndexStep * -2.0 * Math.PI / LengthTotal;
                double wMulRe = Math.Cos(wAngleInc);
                double wMulIm = Math.Sin(wAngleInc);

                for (int start = 0; start < lenTotal; start += spacing)
                {
                    FFTElement xTop = elems[start];
                    FFTElement xBot = elems[start + span];

                    double wRe = 1.0;
                    double wIm = 0.0;

                    // For each butterfly in this stage
                    for (int flyCount = 0; flyCount < numFlies; ++flyCount)
                    {
                        // Get the top & bottom values
                        double xTopRe = xTop.re;
                        double xTopIm = xTop.im;
                        double xBotRe = xBot.re;
                        double xBotIm = xBot.im;

                        // Top branch of butterfly has addition
                        xTop.re = xTopRe + xBotRe;
                        xTop.im = xTopIm + xBotIm;

                        // Bottom branch of butterfly has subtraction,
                        // followed by multiplication by twiddle factor
                        xBotRe = xTopRe - xBotRe;
                        xBotIm = xTopIm - xBotIm;
                        xBot.re = xBotRe * wRe - xBotIm * wIm;
                        xBot.im = xBotRe * wIm + xBotIm * wRe;

                        // Advance butterfly to next top & bottom positions
                        xTop = xTop.next;
                        xBot = xBot.next;

                        // Update the twiddle factor, via complex multiply
                        // by unit vector with the appropriate angle
                        // (wRe + j wIm) = (wRe + j wIm) x (wMulRe + j wMulIm)
                        double tRe = wRe;
                        wRe = wRe * wMulRe - wIm * wMulIm;
                        wIm = tRe * wMulIm + wIm * wMulRe;
                    }
                }

                numFlies >>= 1;   // Divide by 2 by right shift
                span >>= 1;
                spacing >>= 1;
                wIndexStep <<= 1;     // Multiply by 2 by left shift
            }
            // The algorithm leaves the result in a scrambled order.
            // Unscramble while copying values from the complex
            // linked list elements to a complex output vector & properly apply scale factors.

            var unswizzle = output;
            double s = FFTScale;
            if (shouldScale)
            {
                for (int k = 0; k < lenTotal; k++)
                {
                    var e = elems[k];
                    unswizzle[e.revTgt] = new Complex(e.re * s, e.im * s);
                }

                // DC and Fs/2 Points are scaled differently, since they have only a real part
                Apply_DCAndNyquistScale(unswizzle);
            }
            else
            {
                for (int k = 0; k < lenTotal; k++)
                {
                    var e = elems[k];
                    unswizzle[e.revTgt] = new Complex(e.re, e.im);
                }
            }
            // Return 1/2 the FFT result from DC to Fs/2 (The real part of the spectrum)
            //int halfLength = ((mN + mZp) / 2) + 1;
            //Complex[] result = new Complex[mLengthHalf];
            //Array.Copy(unswizzle, result, mLengthHalf);

            return unswizzle;
        }

        public double[] Perform_IFFT_NonPower2(Complex[] input, bool shouldScale = true, bool shouldUnpad = true)
        {
            // Check if the input signal is a power of 2
            if (Math.Log2(input.Length) % 1 == 0)
            {
                Init(input.Length, 0);
                return Perform_IFFT(input, shouldScale);
            }
            else
            {
                // Zero pad the input signal to the next power of 2
                int nextPowerOf2 = 1;
                while (nextPowerOf2 < input.Length)
                    nextPowerOf2 *= 2;

                Init(nextPowerOf2, 0);

                // Reuse the per-instance padding scratch buffer (see Perform_FFT_NonPower2).
                Complex[] paddedTimeSeries = PaddedSpectrumScratch;
                if (paddedTimeSeries == null || paddedTimeSeries.Length != nextPowerOf2)
                {
                    paddedTimeSeries = new Complex[nextPowerOf2];
                    PaddedSpectrumScratch = paddedTimeSeries;
                }
                Array.Copy(input, paddedTimeSeries, input.Length);
                Array.Clear(paddedTimeSeries, input.Length, nextPowerOf2 - input.Length);

                // Perform the FFT on the padded signal
                double[] paddedResult = Perform_IFFT(paddedTimeSeries, shouldScale);

                // Unpad the output
                if (shouldUnpad)
                {
                    double[] unpaddedResult = new double[input.Length];
                    Array.Copy(paddedResult, unpaddedResult, input.Length);
                    return unpaddedResult;
                }
                return paddedResult;
            }
        }

        /// <summary>
        /// Allocating form. Always returns a freshly allocated double[LengthTotal].
        /// </summary>
        public double[] Perform_IFFT(Complex[] input, bool shouldScale = true)
        {
            return Perform_IFFT_Into(input, new double[LengthTotal], shouldScale);
        }

        /// <summary>
        /// Non-allocating form of <see cref="Perform_IFFT(Complex[], bool)"/>: unswizzles into the
        /// caller supplied <paramref name="output"/> buffer instead of allocating one per call.
        /// The arithmetic and write order are identical to the allocating overload, so results are
        /// bit-for-bit the same. Every slot 0..LengthTotal-1 is written, so stale content cannot
        /// leak through.
        /// </summary>
        public double[] Perform_IFFT_Into(Complex[] input, double[] output, bool shouldScale = true)
        {
            int numFlies = LengthTotal >> 1;  // Number of butterflies per sub-FFT
            int span = LengthTotal >> 1;      // Width of the butterfly
            int spacing = LengthTotal;        // Distance between start of sub-FFTs
            int wIndexStep = 1;          // Increment for twiddle table index

            if (input.Length > LengthTotal)
                throw new InvalidOperationException("The input timeSeries length was greater than the total number of points that was initialized.");

            if (output == null || output.Length < LengthTotal)
                throw new InvalidOperationException("The output timeSeries buffer was smaller than the total number of points that was initialized.");

            // Copy data into linked complex number objects (use indexed access)
            var elems = FFTElements;
            int lenTotal = LengthTotal;
            int n = N;
            for (int i = 0; i < n; i++)
            {
                var e = elems[i];
                e.re = input[i].Imaginary;
                e.im = input[i].Real;
            }

            // If zero padded, clean the 2nd half of the linked list from previous results
            if (n != lenTotal)
            {
                for (int i = n; i < lenTotal; i++)
                {
                    var e = elems[i];
                    e.re = 0.0;
                    e.im = 0.0;
                }
            }

            // For each stage of the FFT
            for (int stage = 0; stage < LogN; stage++)
            {
                // Compute a multiplier factor for the "twiddle factors".
                // The twiddle factors are complex unit vectors spaced at
                // regular angular intervals. The angle by which the twiddle
                // factor advances depends on the FFT stage. In many FFT
                // implementations the twiddle factors are cached, but because
                // array lookup is relatively slow in C#, it's just
                // as fast to compute them on the fly.
                double wAngleInc = wIndexStep * -2.0 * Math.PI / LengthTotal;
                double wMulRe = Math.Cos(wAngleInc);
                double wMulIm = Math.Sin(wAngleInc);

                for (int start = 0; start < LengthTotal; start += spacing)
                {
                    FFTElement xTop = FFTElements[start];
                    FFTElement xBot = FFTElements[start + span];

                    double wRe = 1.0;
                    double wIm = 0.0;

                    // For each butterfly in this stage
                    for (int flyCount = 0; flyCount < numFlies; ++flyCount)
                    {
                        // Get the top & bottom values
                        double xTopRe = xTop.re;
                        double xTopIm = xTop.im;
                        double xBotRe = xBot.re;
                        double xBotIm = xBot.im;

                        // Top branch of butterfly has addition
                        xTop.re = xTopRe + xBotRe;
                        xTop.im = xTopIm + xBotIm;

                        // Bottom branch of butterfly has subtraction,
                        // followed by multiplication by twiddle factor
                        xBotRe = xTopRe - xBotRe;
                        xBotIm = xTopIm - xBotIm;
                        xBot.re = xBotRe * wRe - xBotIm * wIm;
                        xBot.im = xBotRe * wIm + xBotIm * wRe;

                        // Advance butterfly to next top & bottom positions
                        xTop = xTop.next;
                        xBot = xBot.next;

                        // Update the twiddle factor, via complex multiply
                        // by unit vector with the appropriate angle
                        // (wRe + j wIm) = (wRe + j wIm) x (wMulRe + j wMulIm)
                        double tRe = wRe;
                        wRe = wRe * wMulRe - wIm * wMulIm;
                        wIm = tRe * wMulIm + wIm * wMulRe;
                    }
                }

                numFlies >>= 1;   // Divide by 2 by right shift
                span >>= 1;
                spacing >>= 1;
                wIndexStep <<= 1;     // Multiply by 2 by left shift
            }

            // The algorithm leaves the result in a scrambled order.
            // Unscramble while copying values from the complex
            // linked list elements to a complex output vector & properly apply scale factors.
            var ReturnValue = output;
            if (shouldScale)
            {
                double s = FFTScale * LengthHalf;
                for (int k = 0; k < lenTotal; k++)
                {
                    var e = elems[k];
                    // unswizzle imaginary component scaled
                    ReturnValue[e.revTgt] = e.im * s;
                }
            }
            else
            {
                double ScaleFactor = (lenTotal / 2 + 1) * lenTotal;
                double s = (double)LengthHalf / ScaleFactor;
                for (int k = 0; k < lenTotal; k++)
                {
                    var e = elems[k];
                    ReturnValue[e.revTgt] = e.im * s;
                }
            }

            return ReturnValue;
        }

        #endregion

        #region protected FFT Functions

        /// <summary>
        /// Applies the DC / Nyquist end-of-spectrum correction used by the scaled forward
        /// transform. Every ordinary bin of a real-input spectrum appears twice (at k and at
        /// LengthTotal - k), so FFTScale carries a sqrt(2) that converts a one-sided peak
        /// amplitude into an RMS reading. The two endpoint bins - DC (k = 0) and Nyquist
        /// (k = LengthTotal / 2) - have no mirror partner and are purely real for a real input,
        /// so that sqrt(2) must be divided back out and the (numerically negligible) imaginary
        /// part forced to exactly zero.
        /// <para>
        /// OFF-BY-ONE FIX: this used to be written inline as unswizzle[LengthHalf].
        /// LengthHalf is LengthTotal / 2 + 1, which is the BIN COUNT of the DC..Fs/2 half
        /// spectrum, NOT the index of its last bin - the true Nyquist bin is at LengthTotal / 2,
        /// i.e. LengthHalf - 1. Indexing with LengthHalf left the real Nyquist bin uncorrected
        /// (reading sqrt(2), about +3 dB, too high) while an ordinary mirror bin one above it was
        /// wrongly divided by sqrt(2) AND had its genuine imaginary part destroyed. It also ran
        /// off the end of the output array for LengthTotal of 1 and 2.
        /// </para>
        /// </summary>
        /// <param name="unswizzle">The already-scaled, already-unswizzled output spectrum.</param>
        protected void Apply_DCAndNyquistScale(Complex[] unswizzle)
        {
            unswizzle[0] = new Complex(unswizzle[0].Real / Math.Sqrt(2), 0.0);

            // For LengthTotal == 1 there is no distinct Nyquist bin: index 0 IS the whole
            // spectrum, and it has already been corrected above. Correcting it twice would
            // halve the DC reading, so only bins strictly above DC are touched here.
            int Local_NyquistIndex = this.LengthTotal / 2;
            if (Local_NyquistIndex > 0)
                unswizzle[Local_NyquistIndex] = new Complex(unswizzle[Local_NyquistIndex].Real / Math.Sqrt(2), 0.0);
        }

        //* Do bit reversal of specified number of places of an int
        //* For example, 1101 bit-reversed is 1011
        //*
        //* @param   x       Number to be bit-reverse.
        //* @param   numBits Number of bits in the number.
        protected int BitReverse(int x, int numBits)
        {
            int y = 0;
            for (int i = 0; i < numBits; i++)
            {
                y <<= 1;
                y |= x & 0x0001;
                x >>= 1;
            }
            return y;
        }

        #endregion

        #region FrequencySpan Utility Function

        /// <summary>
        /// Return the Frequency Array for the currently defined FFT.
        /// Takes into account the total number of points and zero padding points that were defined.
        /// </summary>
        /// <param name="samplingFrequencyHz"></param>
        /// <returns></returns>
        public double[] FrequencySpan(double samplingFrequencyHz)
        {
            int points = LengthHalf;
            if (points <= 0) return Array.Empty<double>();

            double[] result = new double[points];
            double stopValue = samplingFrequencyHz / 2.0;
            double increment = stopValue / ((double)points - 1.0);

            double v = 0.0;
            for (int i = 0; i < points; i++)
            {
                result[i] = v;
                v += increment;
            }

            return result;
        }

        #endregion
    }
}