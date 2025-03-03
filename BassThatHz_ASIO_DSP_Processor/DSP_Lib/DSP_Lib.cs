﻿namespace DSPLib
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Numerics;

    #region License
    /*
    * Released under the MIT License
    *
    * DSP Library for C# - Copyright(c) 2016 Steven C. Hageman.
    *
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

    #region  Generation, Conversion, Analysis and Array Manipulations
    public static class DSP
    {
        #region Generate Signals & Noise

        public static class Generate
        {
            /// <summary>
            /// Generate linearly spaced array. Like the Octave function of the same name.
            /// EX: DSP.Generate.LinSpace(1, 10, 10) -> Returns array: 1, 2, 3, 4....10.
            /// </summary>
            /// <param name="startVal">Any value</param>
            /// <param name="stopVal">Any value > startVal</param>
            /// <param name="points">Number of points to generate</param>
            /// <returns>double[] array</returns>
            public static double[] LinSpace(double startVal, double stopVal, int points)
            {
                double[] result = new double[points];
                double increment = (stopVal - startVal) / ((double)points - 1.0);

                for (int i = 0; i < points; i++)
                    result[i] = startVal + increment * i;

                return result;
            }


            /// <summary>
            /// Generates a Sine Wave Tone using Sampling Terms.
            /// </summary>
            /// <param name="amplitudeVrms"></param>
            /// <param name="frequencyHz"></param>
            /// <param name="samplingFrequencyHz"></param>
            /// <param name="points"></param>
            /// <param name="dcV">[Optional] DC Voltage offset</param>
            /// <param name="phaseDeg">[Optional] Phase of signal in degrees</param>
            /// <returns>double[] array</returns>
            public static double[] ToneSampling(double amplitudeVrms, double frequencyHz, double samplingFrequencyHz, int points, double dcV = 0.0, double phaseDeg = 0)
            {
                double ph_r = phaseDeg * System.Math.PI / 180.0;

                double[] rval = new double[points];
                for (int i = 0; i < points; i++)
                {
                    double time = (double)i / samplingFrequencyHz;
                    rval[i] = System.Math.Sqrt(2) * amplitudeVrms * System.Math.Sin(2.0 * System.Math.PI * time * frequencyHz + ph_r) + dcV;
                }
                return rval;
            }


            /// <summary>
            /// Generates a Sine Wave Tone using Number of Cycles Terms.
            /// </summary>
            /// <param name="amplitudeVrms"></param>
            /// <param name="cycles"></param>
            /// <param name="points"></param>
            /// <param name="dcV">[Optional] DC Voltage offset</param>
            /// <param name="phaseDeg">[Optional] Phase of signal in degrees</param>
            /// <returns>double[] array</returns>
            public static double[] ToneCycles(double amplitudeVrms, double cycles, int points, double dcV = 0.0, double phaseDeg = 0)
            {
                double ph_r = phaseDeg * System.Math.PI / 180.0;
                double ampPeak = System.Math.Sqrt(2) * amplitudeVrms;

                double[] rval = new double[points];
                for (int i = 0; i < points; i++)
                {
                    rval[i] = ampPeak * System.Math.Sin(2.0 * System.Math.PI * (double)i / (double)points * cycles + ph_r) + dcV;
                }
                return rval;
            }
        }

        #endregion

        #region Windows Functions & Scaling Functions

        /**
        *
        * Many of the windows functions are based on the article,
        *
        *   Spectrum and spectral density estimation by the Discrete Fourier
        *   transform (DFT), including a comprehensive list of window
        *   functions and some new ﬂat-top windows.
        *
        *   G. Heinzel, A. Rudiger and R. Schilling,
        *   Max-Planck-Institut fur Gravitationsphysik
        * 
        *   February 15, 2002
        *
        **/

        public static class Window
        {
            /// <summary>
            /// ENUM Types for included Windows.
            /// </summary>
            public enum Type
            {
                None,
                Rectangular,
                FlatTop,
                Welch,
                Bartlett,
                Hanning,
                Hann,
                Hamming,
                Nutall3,
                Nutall4,
                Nutall3A,
                Nutall3B,
                Nutall4A,
                BH92,
                Nutall4B,

                SFT3F,
                SFT3M,
                FTNI,
                SFT4F,
                SFT5F,
                SFT4M,
                FTHP,
                HFT70,
                FTSRS,
                SFT5M,
                HFT90D,
                HFT95,
                HFT116D,
                HFT144D,
                HFT169D,
                HFT196D,
                HFT223D,
                HFT248D
            }

            #region Window Scale Factor

            public static class ScaleFactor
            {
                /// <summary>
                /// Calculate Signal scale factor from window coefficient array.
                /// Designed to be applied to the "Magnitude" result.
                /// </summary>
                /// <param name="windowCoefficients"></param>
                /// <returns>double scaleFactor</returns>
                public static double Signal(double[] windowCoefficients)
                {
                    double s1 = 0;
                    foreach (double coeff in windowCoefficients)
                    {
                        s1 += coeff;
                    }

                    s1 /= windowCoefficients.Length;

                    return 1.0 / s1;
                }


                /// <summary>
                ///  Calculate Noise scale factor from window coefficient array.
                ///  Takes into account the bin width in Hz for the final result also.
                ///  Designed to be applied to the "Magnitude" result.
                /// </summary>
                /// <param name="windowCoefficients"></param>
                /// <param name="samplingFrequencyHz"></param>
                /// <returns>double scaleFactor</returns>
                public static double Noise(double[] windowCoefficients, double samplingFrequencyHz)
                {
                    double s2 = 0;
                    foreach (double coeff in windowCoefficients)
                    {
                        s2 += coeff * coeff;
                    }

                    double n = windowCoefficients.Length;
                    double fbin = samplingFrequencyHz / n;

                    double sf = System.Math.Sqrt(1.0 / (s2 / n * fbin));

                    return sf;
                }


                /// <summary>
                ///  Calculate Normalized, Equivalent Noise BandWidth from window coefficient array.
                /// </summary>
                /// <param name="windowCoefficients"></param>
                /// <returns>double NENBW</returns>
                public static double NENBW(double[] windowCoefficients)
                {
                    double s1 = 0;
                    double s2 = 0;
                    foreach (double coeff in windowCoefficients)
                    {
                        s1 += coeff;
                        s2 += coeff * coeff;
                    }

                    double n = windowCoefficients.Length;
                    s1 /= n;

                    double nenbw = s2 / (s1 * s1) / n;

                    return nenbw;
                }
            }
            #endregion

            #region Window Coefficient Calculations

            /// <summary>
            /// Calculates a set of Windows coefficients for a given number of points and a window type to use.
            /// </summary>
            /// <param name="windowName"></param>
            /// <param name="points"></param>
            /// <returns>double[] array of the calculated window coefficients</returns>
            public static double[] Coefficients(Type windowName, int points)
            {
                double[] winCoeffs = new double[points];
                double N = points;

                switch (windowName)
                {
                    case Window.Type.None:
                    case Window.Type.Rectangular:
                        //wc = ones(N,1);
                        for (int i = 0; i < points; i++)
                            winCoeffs[i] = 1.0;

                        break;

                    case Window.Type.FlatTop:
                        winCoeffs = SineExpansion(points, 0.21557895, -0.41663158, 0.277263158, -0.083578947, 0.006947368);
                        break;

                    case Window.Type.Bartlett:
                        //n = (0:N-1)';
                        //wc = 2/N*(N/2-abs(n-(N-1)/2));
                        for (int i = 0; i < points; i++)
                            winCoeffs[i] = 2.0 / N * (N / 2.0 - System.Math.Abs(i - (N - 1.0) / 2.0));

                        break;

                    case Window.Type.Welch:
                        //n = (0:N-1)';
                        //wc = 1 - ( ((2*n)/N) - 1).^2;
                        for (int i = 0; i < points; i++)
                            winCoeffs[i] = 1.0 - System.Math.Pow(2.0 * i / N - 1.0, 2.0);
                        break;

                    case Window.Type.Hann:
                    case Window.Type.Hanning:
                        //wc = (0.5 - 0.5*cos (z));
                        winCoeffs = SineExpansion(points, 0.5, -0.5);
                        break;

                    case Window.Type.Hamming:
                        //wc = (0.54 - 0.46*cos (z));
                        winCoeffs = SineExpansion(points, 0.54, -0.46);
                        break;

                    case Window.Type.BH92: // Also known as: Blackman-Harris
                                           //wc = (0.35875 - 0.48829*cos(z) + 0.14128*cos(2*z) - 0.01168*cos(3*z));
                        winCoeffs = SineExpansion(points, 0.35875, -0.48829, 0.14128, -0.01168);
                        break;

                    case Window.Type.Nutall3:
                        //c0 = 0.375; c1 = -0.5; c2 = 0.125;
                        //wc = c0 + c1*cos(z) + c2*cos(2*z);
                        winCoeffs = SineExpansion(points, 0.375, -0.5, 0.125);
                        break;

                    case Window.Type.Nutall3A:
                        //c0 = 0.40897; c1 = -0.5; c2 = 0.09103;
                        //wc = c0 + c1*cos(z) + c2*cos(2*z);
                        winCoeffs = SineExpansion(points, 0.40897, -0.5, 0.09103);
                        break;

                    case Window.Type.Nutall3B:
                        //c0 = 0.4243801; c1 = -0.4973406; c2 = 0.0782793;
                        //wc = c0 + c1*cos(z) + c2*cos(2*z);
                        winCoeffs = SineExpansion(points, 0.4243801, -0.4973406, 0.0782793);
                        break;

                    case Window.Type.Nutall4:
                        //c0 = 0.3125; c1 = -0.46875; c2 = 0.1875; c3 = -0.03125;
                        //wc = c0 + c1*cos(z) + c2*cos(2*z) + c3*cos(3*z);
                        winCoeffs = SineExpansion(points, 0.3125, -0.46875, 0.1875, -0.03125);
                        break;

                    case Window.Type.Nutall4A:
                        //c0 = 0.338946; c1 = -0.481973; c2 = 0.161054; c3 = -0.018027;
                        //wc = c0 + c1*cos(z) + c2*cos(2*z) + c3*cos(3*z);
                        winCoeffs = SineExpansion(points, 0.338946, -0.481973, 0.161054, -0.018027);
                        break;

                    case Window.Type.Nutall4B:
                        //c0 = 0.355768; c1 = -0.487396; c2 = 0.144232; c3 = -0.012604;
                        //wc = c0 + c1*cos(z) + c2*cos(2*z) + c3*cos(3*z);
                        winCoeffs = SineExpansion(points, 0.355768, -0.487396, 0.144232, -0.012604);
                        break;

                    case Window.Type.SFT3F:
                        //c0 = 0.26526; c1 = -0.5; c2 = 0.23474;
                        //wc = c0 + c1*cos(z) + c2*cos(2*z);
                        winCoeffs = SineExpansion(points, 0.26526, -0.5, 0.23474);
                        break;

                    case Window.Type.SFT4F:
                        //c0 = 0.21706; c1 = -0.42103; c2 = 0.28294; c3 = -0.07897;
                        //wc = c0 + c1*cos(z) + c2*cos(2*z) + c3*cos(3*z);
                        winCoeffs = SineExpansion(points, 0.21706, -0.42103, 0.28294, -0.07897);
                        break;

                    case Window.Type.SFT5F:
                        //c0 = 0.1881; c1 = -0.36923; c2 = 0.28702; c3 = -0.13077; c4 = 0.02488;
                        //wc = c0 + c1*cos(z) + c2*cos(2*z) + c3*cos(3*z) + c4*cos(4*z);
                        winCoeffs = SineExpansion(points, 0.1881, -0.36923, 0.28702, -0.13077, 0.02488);
                        break;

                    case Window.Type.SFT3M:
                        //c0 = 0.28235; c1 = -0.52105; c2 = 0.19659;
                        //wc = c0 + c1*cos(z) + c2*cos(2*z);
                        winCoeffs = SineExpansion(points, 0.28235, -0.52105, 0.19659);
                        break;

                    case Window.Type.SFT4M:
                        //c0 = 0.241906; c1 = -0.460841; c2 = 0.255381; c3 = -0.041872;
                        //wc = c0 + c1*cos(z) + c2*cos(2*z) + c3*cos(3*z);
                        winCoeffs = SineExpansion(points, 0.241906, -0.460841, 0.255381, -0.041872);
                        break;

                    case Window.Type.SFT5M:
                        //c0 = 0.209671; c1 = -0.407331; c2 = 0.281225; c3 = -0.092669; c4 = 0.0091036;
                        //wc = c0 + c1*cos(z) + c2*cos(2*z) + c3*cos(3*z) + c4*cos(4*z);
                        winCoeffs = SineExpansion(points, 0.209671, -0.407331, 0.281225, -0.092669, 0.0091036);
                        break;

                    case Window.Type.FTNI:
                        //wc = (0.2810639 - 0.5208972*cos(z) + 0.1980399*cos(2*z));
                        winCoeffs = SineExpansion(points, 0.2810639, -0.5208972, 0.1980399);
                        break;

                    case Window.Type.FTHP:
                        //wc = 1.0 - 1.912510941*cos(z) + 1.079173272*cos(2*z) - 0.1832630879*cos(3*z);
                        winCoeffs = SineExpansion(points, 1.0, -1.912510941, 1.079173272, -0.1832630879);
                        break;

                    case Window.Type.HFT70:
                        //wc = 1 - 1.90796*cos(z) + 1.07349*cos(2*z) - 0.18199*cos(3*z);
                        winCoeffs = SineExpansion(points, 1, -1.90796, 1.07349, -0.18199);
                        break;

                    case Window.Type.FTSRS:
                        //wc = 1.0 - 1.93*cos(z) + 1.29*cos(2*z) - 0.388*cos(3*z) + 0.028*cos(4*z);
                        winCoeffs = SineExpansion(points, 1.0, -1.93, 1.29, -0.388, 0.028);
                        break;

                    case Window.Type.HFT90D:
                        //wc = 1 - 1.942604*cos(z) + 1.340318*cos(2*z) - 0.440811*cos(3*z) + 0.043097*cos(4*z);
                        winCoeffs = SineExpansion(points, 1.0, -1.942604, 1.340318, -0.440811, 0.043097);
                        break;

                    case Window.Type.HFT95:
                        //wc = 1 - 1.9383379*cos(z) + 1.3045202*cos(2*z) - 0.4028270*cos(3*z) + 0.0350665*cos(4*z);
                        winCoeffs = SineExpansion(points, 1, -1.9383379, 1.3045202, -0.4028270, 0.0350665);
                        break;

                    case Window.Type.HFT116D:
                        //wc = 1 - 1.9575375*cos(z) + 1.4780705*cos(2*z) - 0.6367431*cos(3*z) + 0.1228389*cos(4*z) - 0.0066288*cos(5*z);
                        winCoeffs = SineExpansion(points, 1.0, -1.9575375, 1.4780705, -0.6367431, 0.1228389, -0.0066288);
                        break;

                    case Window.Type.HFT144D:
                        //wc = 1 - 1.96760033*cos(z) + 1.57983607*cos(2*z) - 0.81123644*cos(3*z) + 0.22583558*cos(4*z) - 0.02773848*cos(5*z) + 0.00090360*cos(6*z);
                        winCoeffs = SineExpansion(points, 1.0, -1.96760033, 1.57983607, -0.81123644, 0.22583558, -0.02773848, 0.00090360);
                        break;

                    case Window.Type.HFT169D:
                        //wc = 1 - 1.97441842*cos(z) + 1.65409888*cos(2*z) - 0.95788186*cos(3*z) + 0.33673420*cos(4*z) - 0.06364621*cos(5*z) + 0.00521942*cos(6*z) - 0.00010599*cos(7*z);
                        winCoeffs = SineExpansion(points, 1.0, -1.97441842, 1.65409888, -0.95788186, 0.33673420, -0.06364621, 0.00521942, -0.00010599);
                        break;

                    case Window.Type.HFT196D:
                        //wc = 1 - 1.979280420*cos(z) + 1.710288951*cos(2*z) - 1.081629853*cos(3*z)+ 0.448734314*cos(4*z) - 0.112376628*cos(5*z) + 0.015122992*cos(6*z) - 0.000871252*cos(7*z) + 0.000011896*cos(8*z);
                        winCoeffs = SineExpansion(points, 1.0, -1.979280420, 1.710288951, -1.081629853, 0.448734314, -0.112376628, 0.015122992, -0.000871252, 0.000011896);
                        break;

                    case Window.Type.HFT223D:
                        //wc = 1 - 1.98298997309*cos(z) + 1.75556083063*cos(2*z) - 1.19037717712*cos(3*z) + 0.56155440797*cos(4*z) - 0.17296769663*cos(5*z) + 0.03233247087*cos(6*z) - 0.00324954578*cos(7*z) + 0.00013801040*cos(8*z) - 0.00000132725*cos(9*z);
                        winCoeffs = SineExpansion(points, 1.0, -1.98298997309, 1.75556083063, -1.19037717712, 0.56155440797, -0.17296769663, 0.03233247087, -0.00324954578, 0.00013801040, -0.00000132725);
                        break;

                    case Window.Type.HFT248D:
                        //wc = 1 - 1.985844164102*cos(z) + 1.791176438506*cos(2*z) - 1.282075284005*cos(3*z) + 0.667777530266*cos(4*z) - 0.240160796576*cos(5*z) + 0.056656381764*cos(6*z) - 0.008134974479*cos(7*z) + 0.000624544650*cos(8*z) - 0.000019808998*cos(9*z) + 0.000000132974*cos(10*z);
                        winCoeffs = SineExpansion(points, 1, -1.985844164102, 1.791176438506, -1.282075284005, 0.667777530266, -0.240160796576, 0.056656381764, -0.008134974479, 0.000624544650, -0.000019808998, 0.000000132974);
                        break;

                    default:
                        //throw new NotImplementedException("Window type fell through to 'Default'.");
                        break;
                }

                return winCoeffs;
            }

            public static double[] SineExpansion(int points, double c0, double c1 = 0.0, double c2 = 0.0, double c3 = 0.0, double c4 = 0.0, double c5 = 0.0, double c6 = 0.0, double c7 = 0.0, double c8 = 0.0, double c9 = 0.0, double c10 = 0.0)
            {
                // z = 2 * pi * (0:N-1)' / N;   // Cosine Vector
                double[] z = new double[points];
                for (int i = 0; i < points; i++)
                    z[i] = 2.0 * System.Math.PI * i / points;

                double[] winCoeffs = new double[points];

                for (int i = 0; i < points; i++)
                {
                    double wc = c0;
                    wc += c1 * System.Math.Cos(z[i]);
                    wc += c2 * System.Math.Cos(2.0 * z[i]);
                    wc += c3 * System.Math.Cos(3.0 * z[i]);
                    wc += c4 * System.Math.Cos(4.0 * z[i]);
                    wc += c5 * System.Math.Cos(5.0 * z[i]);
                    wc += c6 * System.Math.Cos(6.0 * z[i]);
                    wc += c7 * System.Math.Cos(7.0 * z[i]);
                    wc += c8 * System.Math.Cos(8.0 * z[i]);
                    wc += c9 * System.Math.Cos(9.0 * z[i]);
                    wc += c10 * System.Math.Cos(10.0 * z[i]);

                    winCoeffs[i] = wc;
                }

                return winCoeffs;
            }

            #endregion

        }

        #endregion

        #region Convert Magnitude format to user friendly formats

        /// <summary>
        /// DFT / FFT Format Conversion Functions
        /// </summary>
        public static class ConvertMagnitude
        {
            /// <summary>
            /// Convert Magnitude FT Result to: Magnitude Squared Format
            /// </summary>
            /// <param name="magnitude"></param>
            /// <returns></returns>
            public static double[] ToMagnitudeSquared(double[] magnitude)
            {
                int np = (int)magnitude.Length;
                double[] mag2 = new double[np];
                for (int i = 0; i < np; i++)
                {
                    mag2[i] = magnitude[i] * magnitude[i];
                }

                return mag2;
            }


            /// <summary>
            /// Convert Magnitude FT Result to: Magnitude dBVolts
            /// </summary>
            /// <param name="magnitude"></param>
            /// <returns>double[] array</returns>
            public static double[] ToMagnitudeDBV(double[] magnitude, double scale = 1, double floor = -400d)
            {
                int np = (int)magnitude.Length;
                double[] magDBV = new double[np];
                for (int i = 0; i < np; i++)
                {
                    double magVal = magnitude[i] * scale;
                    if (magVal <= 0.0)
                        magVal = double.Epsilon;

                    magDBV[i] = 20 * System.Math.Log10(magVal);

                    if (magDBV[i] < floor)
                        magDBV[i] = floor;
                }

                return magDBV;
            }

        }

        #endregion

        #region Convert Complex format to user friendly formats

        /// <summary>
        /// DFT / FFT Format Conversion Functions.
        /// </summary>
        public static class ConvertComplex
        {
            /// <summary>
            /// Convert Complex DFT/FFT Result to: Magnitude Vrms
            /// </summary>
            /// <param name="rawFFT"></param>
            /// <returns>double[] Magnitude Format (Vrms)</returns>
            public static double[] ToMagnitude(Complex[] rawFFT)
            {
                int np = (int)rawFFT.Length;
                double[] mag = new double[np];
                for (int i = 0; i < np; i++)
                {
                    mag[i] = rawFFT[i].Magnitude;
                }

                return mag;
            }

            /// <summary>
            /// Convert Complex DFT/FFT Result to: Log Magnitude dBV
            /// </summary>
            /// <param name="rawFFT"> Complex[] input array"></param>
            /// <returns>double[] Magnitude Format (dBV)</returns>
            public static double[] ToMagnitudeDBV(Complex[] rawFFT)
            {
                int np = (int)rawFFT.Length;
                double[] mag = new double[np];
                for (int i = 0; i < np; i++)
                {
                    double magVal = rawFFT[i].Magnitude;

                    if (magVal <= 0.0)
                        magVal = double.Epsilon;

                    mag[i] = 20 * System.Math.Log10(magVal);
                }

                return mag;
            }

            /// <summary>
            /// Convert Complex DFT/FFT Result to: Phase in Degrees
            /// </summary>
            /// <param name="rawFFT"> Complex[] input array"></param>
            /// <returns>double[] Phase (Degrees)</returns>
            public static double[] ToPhaseDegrees(Complex[] rawFFT)
            {
                double sf = 180.0 / System.Math.PI; // Degrees per Radian scale factor

                int np = (int)rawFFT.Length;
                double[] phase = new double[np];
                for (int i = 0; i < np; i++)
                {
                    phase[i] = rawFFT[i].Phase * sf;
                }

                return phase;
            }
        }
        #endregion

        #region Double[] Array Math Operators

        /// <summary>
        /// Double[] Array Math Operations (All Static)
        /// </summary>
        public static class Math
        {

            /// <summary>
            /// result[] = a[] * b[]
            /// </summary>
            public static Double[] Multiply(Double[] a, Double[] b)
            {
                if (a.Length != b.Length) throw new ArgumentException("Length of arrays a[] and b[] must match.");

                double[] result = new double[a.Length];
                for (int i = 0; i < a.Length; i++)
                    result[i] = a[i] * b[i];

                return result;
            }

            /// <summary>
            /// result[] = a[] * b
            /// </summary>
            public static Double[] Multiply(Double[] a, Double b)
            {
                double[] result = new double[a.Length];
                for (int i = 0; i < a.Length; i++)
                    result[i] = a[i] * b;

                return result;
            }

            /// <summary>
            /// result[] = a[] + b
            /// </summary>
            public static Double[] Add(Double[] a, Double b)
            {
                double[] result = new double[a.Length];
                for (int i = 0; i < a.Length; i++)
                    result[i] = a[i] + b;

                return result;
            }

            /// <summary>
            /// result[] = a[] - b
            /// </summary>
            public static Double[] Subtract(Double[] a, Double b)
            {
                double[] result = new double[a.Length];
                for (int i = 0; i < a.Length; i++)
                    result[i] = a[i] - b;

                return result;
            }
        }

        #endregion

        #region Analyze
        public static class Analyze
        {
            /// <summary>
            /// Finds the position in the inData array where the maximum value happens.
            /// </summary>
            /// <param name="inData"></param>
            /// <returns>Position of maximum value in input array</returns>
            public static int FindMaxPosition(double[] inData, int minIndex, int maxIndex)
            {
                double minVal = double.MinValue;
                int MaxIndex = 0;

                if (minIndex < 0) 
                    minIndex = 0;
                if (maxIndex > inData.Length)
                    maxIndex = inData.Length;

                for (int i = minIndex; i < maxIndex; i++)
                {
                    if (inData[i] > minVal)
                    {
                        MaxIndex = i;
                        minVal = inData[i];
                    }
                }

                return MaxIndex;
            }

            public static int FindMinPosition(double[] inData, int minIndex, int maxIndex)
            {
                double maxVal = double.MaxValue;
                int MaxIndex = 0;

                if (minIndex < 0)
                    minIndex = 0;
                if (maxIndex > inData.Length)
                    maxIndex = inData.Length;

                for (int i = minIndex; i < maxIndex; i++)
                {
                    if (inData[i] < maxVal)
                    {
                        MaxIndex = i;
                        maxVal = inData[i];
                    }
                }

                return MaxIndex;
            }

            /// <summary>
            /// Unwraps the phase so that it is continuous, without jumps.
            /// </summary>
            /// <param name="inPhaseDeg">Array of Phase Data from FT in Degrees</param>
            /// <returns>Continuous Phase data</returns>
            public static double[] UnwrapPhaseDegrees(double[] inPhaseDeg)
            {
                int N = (int)inPhaseDeg.Length;
                double[] unwrappedPhase = new double[N];

                double[] tempInData = new double[N];
                inPhaseDeg.CopyTo(tempInData, 0);

                // First point is unchanged
                unwrappedPhase[0] = tempInData[0];

                for (int i = 1; i < N; i++)
                {
                    double delta = System.Math.Abs(tempInData[i - 1] - tempInData[i]);
                    if (delta >= 180)
                    {
                        // Phase jump!
                        if (tempInData[i - 1] < 0.0)
                        {
                            for (int j = i; j < N; j++)
                                tempInData[j] += -360;
                        }
                        else
                        {
                            for (int j = i; j < N; j++)
                                tempInData[j] += 360;
                        }
                    }
                    unwrappedPhase[i] = tempInData[i];
                }
                return unwrappedPhase;
            }
        }
        #endregion
    }
    #endregion

    public static class DownSampler
    {
       public static double[] downsample(double[] data, int threshold)
        {
            int dataLength = data.Count();
            if (threshold >= dataLength || threshold == 0)
                return data; // Nothing to do

            var sampled = new List<double>(threshold);

            // Bucket size. Leave room for start and end data points
            double every = (double)(dataLength - 2) / (threshold - 2);

            int a = 0;
            double maxAreaPoint = 0;
            int nextA = 0;

            sampled.Add(data[a]); // Always add the first point

            for (int i = 0; i < threshold - 2; i++)
            {
                // Calculate point average for next bucket (containing c)
                double avgX = 0;
                int avgRangeStart = (int)(Math.Floor((i + 1) * every) + 1);
                int avgRangeEnd = (int)(Math.Floor((i + 2) * every) + 1);
                avgRangeEnd = avgRangeEnd < dataLength ? avgRangeEnd : dataLength;

                int avgRangeLength = avgRangeEnd - avgRangeStart;

                for (; avgRangeStart < avgRangeEnd; avgRangeStart++)
                {
                    avgX += data[avgRangeStart]; // * 1 enforces Number (value may be Date)
                }
                avgX /= avgRangeLength;

                // Get the range for this bucket
                int rangeOffs = (int)(Math.Floor((i + 0) * every) + 1);
                int rangeTo = (int)(Math.Floor((i + 1) * every) + 1);

                // Point a
                double pointAx = data[a]; // enforce Number (value may be Date)

                double maxArea = -1;

                for (; rangeOffs < rangeTo; rangeOffs++)
                {
                    // Calculate triangle area over three buckets
                    double area = Math.Abs(pointAx - avgX - (pointAx - data[rangeOffs])) * 0.5;
                    if (area > maxArea)
                    {
                        maxArea = area;
                        maxAreaPoint = data[rangeOffs];
                        nextA = rangeOffs; // Next a is this b
                    }
                }

                sampled.Add(maxAreaPoint); // Pick this point from the bucket
                a = nextA; // This a is the next a (chosen b)
            }

            sampled.Add(data[dataLength - 1]); // Always add last

            return sampled.ToArray();
        }

    }
}