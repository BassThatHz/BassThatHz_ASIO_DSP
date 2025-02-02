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
            bool foundIt = false;
            for (LogN = 1; LogN <= 32; LogN++)
            {
                double n = Math.Pow(2.0, LogN);
                if (inputDataLength + zeroPaddingLength == n)
                {
                    foundIt = true;
                    break;
                }
            }

            if (foundIt == false)
                throw new ArgumentOutOfRangeException("inputDataLength + zeroPaddingLength was not an even power of 2! FFT cannot continue.");

            // Set global parameters.
            LengthTotal = inputDataLength + zeroPaddingLength;
            LengthHalf = LengthTotal / 2 + 1;

            // Set the overall scale factor for all the terms
            FFTScale = Math.Sqrt(2) / (double)LengthTotal; // Natural FFT Scale Factor  // Window Scale Factor
            FFTScale *= (double)LengthTotal / (double)inputDataLength; // Zero Padding Scale Factor

            // Allocate elements for linked list of complex numbers.
            FFTElements = new FFTElement[LengthTotal];
            for (int k = 0; k < LengthTotal; k++)
                FFTElements[k] = new FFTElement();

            // Set up "next" pointers.
            for (int k = 0; k < LengthTotal - 1; k++)
                FFTElements[k].next = FFTElements[k + 1];

            // Specify target for bit reversal re-ordering.
            for (int k = 0; k < LengthTotal; k++)
                FFTElements[k].revTgt = BitReverse(k, LogN);
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

                double[] paddedTimeSeries = new double[nextPowerOf2];
                Array.Copy(input, paddedTimeSeries, input.Length);

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

        public Complex[] Perform_FFT(double[] input, bool shouldScale = true)
        {
            int numFlies = LengthTotal >> 1;  // Number of butterflies per sub-FFT
            int span = LengthTotal >> 1;      // Width of the butterfly
            int spacing = LengthTotal;        // Distance between start of sub-FFTs
            int wIndexStep = 1;          // Increment for twiddle table index

            if (input.Length > LengthTotal)
                throw new InvalidOperationException("The input timeSeries length was greater than the total number of points that was initialized.");

            // Copy data into linked complex number objects
            FFTElement x = FFTElements[0];
            for (int i = 0; i < N; i++)
            {
                x.re = input[i];
                x.im = 0.0;
                x = x.next;
            }

            // If zero padded, clean the 2nd half of the linked list from previous results
            if (N != LengthTotal)
            {
                for (int i = N; i < LengthTotal; i++)
                {
                    x.re = 0.0;
                    x.im = 0.0;
                    x = x.next;
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

            x = FFTElements[0];
            var unswizzle = new Complex[LengthTotal];
            if (shouldScale)
            {
                while (x != null)
                {
                    unswizzle[x.revTgt] = new Complex(x.re * FFTScale, x.im * FFTScale);
                    x = x.next;
                }

                // DC and Fs/2 Points are scaled differently, since they have only a real part
                //result[0] = new Complex(result[0].Real / Math.Sqrt(2), 0.0);
                //result[mLengthHalf - 1] = new Complex(result[mLengthHalf - 1].Real / Math.Sqrt(2), 0.0);
                unswizzle[0] = new Complex(unswizzle[0].Real / Math.Sqrt(2), 0.0);
                unswizzle[LengthHalf] = new Complex(unswizzle[LengthHalf].Real / Math.Sqrt(2), 0.0);
            }
            else
            {
                while (x != null)
                {
                    unswizzle[x.revTgt] = new Complex(x.re, x.im);
                    x = x.next;
                }
            }
            // Return 1/2 the FFT result from DC to Fs/2 (The real part of the spectrum)
            //int halfLength = ((mN + mZp) / 2) + 1;
            //Complex[] result = new Complex[mLengthHalf];
            //Array.Copy(unswizzle, result, mLengthHalf);

            return unswizzle;
        }

        public Complex[] Perform_FFT(double[] input, double[] windowCoefficients, bool shouldScale = true)
        {
            int numFlies = LengthTotal >> 1;  // Number of butterflies per sub-FFT
            int span = LengthTotal >> 1;      // Width of the butterfly
            int spacing = LengthTotal;        // Distance between start of sub-FFTs
            int wIndexStep = 1;          // Increment for twiddle table index

            if (input.Length > LengthTotal)
                throw new InvalidOperationException("The input timeSeries length was greater than the total number of points that was initialized.");

            if (input.Length != windowCoefficients.Length)
                throw new InvalidOperationException("windowCoefficients must be same length as timeSeries");

            // Copy data into linked complex number objects
            FFTElement x = FFTElements[0];
            for (int i = 0; i < N; i++)
            {
                x.re = input[i] * windowCoefficients[i];
                x.im = 0.0;
                x = x.next;
            }

            // If zero padded, clean the 2nd half of the linked list from previous results
            if( N != LengthTotal)
            {
                for (int i = N; i < LengthTotal; i++)
                {
                    x.re = 0.0; 
                    x.im = 0.0;
                    x = x.next;
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

            x = FFTElements[0];
            var unswizzle = new Complex[LengthTotal];
            if (shouldScale)
            {
                while (x != null)
                {
                    unswizzle[x.revTgt] = new Complex(x.re * FFTScale, x.im * FFTScale);
                    x = x.next;
                }

                // DC and Fs/2 Points are scaled differently, since they have only a real part
                //result[0] = new Complex(result[0].Real / Math.Sqrt(2), 0.0);
                //result[mLengthHalf - 1] = new Complex(result[mLengthHalf - 1].Real / Math.Sqrt(2), 0.0);
                unswizzle[0] = new Complex(unswizzle[0].Real / Math.Sqrt(2), 0.0);
                unswizzle[LengthHalf] = new Complex(unswizzle[LengthHalf].Real / Math.Sqrt(2), 0.0);
            }
            else
            {
                while (x != null)
                {
                    unswizzle[x.revTgt] = new Complex(x.re, x.im);
                    x = x.next;
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

                Complex[] paddedTimeSeries = new Complex[nextPowerOf2];
                Array.Copy(input, paddedTimeSeries, input.Length);

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

        public double[] Perform_IFFT(Complex[] input, bool shouldScale = true)
        {
            int numFlies = LengthTotal >> 1;  // Number of butterflies per sub-FFT
            int span = LengthTotal >> 1;      // Width of the butterfly
            int spacing = LengthTotal;        // Distance between start of sub-FFTs
            int wIndexStep = 1;          // Increment for twiddle table index

            if (input.Length > LengthTotal)
                throw new InvalidOperationException("The input timeSeries length was greater than the total number of points that was initialized.");

            // Copy data into linked complex number objects
            FFTElement x = FFTElements[0];
            for (int i = 0; i < N; i++)
            {
                x.re = input[i].Imaginary;
                x.im = input[i].Real;
                x = x.next;
            }

            // If zero padded, clean the 2nd half of the linked list from previous results
            if (N != LengthTotal)
            {
                for (int i = N; i < LengthTotal; i++)
                {
                    x.re = 0.0;
                    x.im = 0.0;
                    x = x.next;
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
            x = FFTElements[0];
            var ReturnValue = new double[LengthTotal];
            if (shouldScale)
            {
                while (x != null)
                {
                    var unswizzle = new Complex(x.re * FFTScale, x.im * FFTScale);
                    ReturnValue[x.revTgt] = unswizzle.Imaginary * LengthHalf;
                    x = x.next;
                }
            }
            else
            {
                var ScaleFactor = (LengthTotal / 2 + 1) * LengthTotal;
                while (x != null)
                {
                    var unswizzle = new Complex(x.re, x.im);
                    ReturnValue[x.revTgt] = unswizzle.Imaginary * LengthHalf / ScaleFactor;
                    x = x.next;
                }
            }

            return ReturnValue;
        }

        #endregion

        #region protected FFT Functions

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
            int points = (int)LengthHalf;
            double[] result = new double[points];
            double stopValue = samplingFrequencyHz / 2.0;
            double increment = stopValue / ((double)points - 1.0);

            for (int i = 0; i < points; i++)
                result[i] += increment * i;

            return result;
        }

        #endregion
    }
}