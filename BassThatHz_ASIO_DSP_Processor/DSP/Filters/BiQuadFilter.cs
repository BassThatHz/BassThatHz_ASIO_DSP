#nullable enable

namespace NAudio.Dsp
{
    using BassThatHz_ASIO_DSP_Processor;
    using NAudio.Utils;
    // based on Cookbook formulae for audio EQ biquad filter coefficients
    // http://www.musicdsp.org/files/Audio-EQ-Cookbook.txt
    // by Robert Bristow-Johnson  <rbj@audioimagination.com>

    //    alpha = sin(w0)/(2*Q)                                       (case: Q)
    //          = sin(w0)*sinh( ln(2)/2 * BW * w0/sin(w0) )           (case: BW)
    //          = sin(w0)/2 * sqrt( (A + 1/A)*(1/S - 1) + 2 )         (case: S)
    // Q: (the EE kind of definition, except for peakingEQ in which A*Q is
    // the classic EE Q.  That adjustment in definition was made so that
    // a boost of N dB followed by a cut of N dB for identical Q and
    // f0/Fs results in a precisely flat unity gain filter or "wire".)
    //
    // BW: the bandwidth in octaves (between -3 dB frequencies for BPF
    // and notch or between midpoint (dBgain/2) gain frequencies for
    // peaking EQ)
    //
    // S: a "shelf slope" parameter (for shelving EQ only).  When S = 1,
    // the shelf slope is as steep as it can be and remain monotonically
    // increasing or decreasing gain with frequency.  The shelf slope, in
    // dB/octave, remains proportional to S for all other values for a
    // fixed f0/Fs and dBgain.

    using System;
    using System.DirectoryServices.ActiveDirectory;
    using System.Linq;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// BiQuad filter
    /// </summary>
    [Serializable]
    public class BiQuadFilter : IFilter
    {
        #region Variables

        #region Constants
        protected const double PI = Math.PI;
        protected const double PI2 = Math.PI * 2;
        #endregion

        // coefficients
        public double a0; //protected
        public double a1; //protected
        public double a2; //protected
        public double a3; //protected
        public double a4; //protected

        // state
        protected double x1 = 0;
        protected double x2 = 0;
        protected double y1 = 0;
        protected double y2 = 0;

        #region Private variables
        private double result = 0;

        public enum BiQuadFilterTypes //Private
        {
            PEQ,
            HPF,
            HPF1st,
            LPF,
            LPF1st,
            LS,
            HS,
            NF,
            BPF,
            APF,
            Inv
        }
        public BiQuadFilterTypes BiQuadFilterType; //Private
        #endregion
        #endregion

        #region Properties
        public double SampleRate { get; set; } //private set
        public double Frequency { get; set; } //private set
        public double Q { get; set; } //private set
        public double Slope { get; set; } //private set
        public double Gain { get; set; } //private set

        // coefficients
        public double aa0 { get; set; } //private set
        public double aa1 { get; set; } //private set
        public double aa2 { get; set; } //private set
        public double b0 { get; set; } //private set
        public double b1 { get; set; } //private set
        public double b2 { get; set; } //private set
        #endregion

        #region Constructors and Initializers
        public BiQuadFilter()
        {
        }

        public BiQuadFilter(double a0, double a1, double a2, double b0, double b1, double b2)
        {
            this.SetCoefficients(a0, a1, a2, b0, b1, b2);
        }
        protected void Init()
        {
            // zero initial samples
            x1 = x2 = 0;
            y1 = y2 = 0;
        }
        #endregion

        #region Public Functions
        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
        public double[] Transform(double[] input, DSP_Stream currentStream)
        {
            for (int i = 0; i < input.Length; i++)
            {
                // compute result
                this.result = a0 * input[i] + a1 * x1 + a2 * x2 - a3 * y1 - a4 * y2;

                // shift x1 to x2, sample to x1 
                x2 = x1;
                x1 = input[i];

                // shift y1 to y2, result to y1 
                y2 = y1;
                y1 = (double)this.result;

                input[i] = y1;
            }
            return input;
        }

        public void SetCoefficients(double aa0, double aa1, double aa2, double b0, double b1, double b2)
        {
            this.aa0 = aa0 == 0 ? 0.000000000001 : aa0;
            this.aa1 = aa1;
            this.aa2 = aa2;
            this.b0 = b0;
            this.b1 = b1;
            this.b2 = b2;

            // precompute the coefficients
            this.a0 = b0 / aa0;
            this.a1 = b1 / aa0;
            this.a2 = b2 / aa0;
            this.a3 = aa1 / aa0;
            this.a4 = aa2 / aa0;
        }

        public void ChangeSampleRate(double sampleRate)
        {
            this.SampleRate = sampleRate;
            switch (this.BiQuadFilterType)
            {
                case BiQuadFilterTypes.PEQ:
                    this.PeakingEQ(this.SampleRate, this.Frequency, this.Q, this.Gain);
                    break;

                case BiQuadFilterTypes.HPF:
                    this.HighPassFilter(this.SampleRate, this.Frequency, this.Q);
                    break;

                case BiQuadFilterTypes.HPF1st:
                    this.HighPassFilter1st(this.SampleRate, this.Frequency, this.Q);
                    break;

                case BiQuadFilterTypes.LPF:
                    this.LowPassFilter(this.SampleRate, this.Frequency, this.Q);
                    break;

                case BiQuadFilterTypes.LPF1st:
                    this.LowPassFilter1st(this.SampleRate, this.Frequency, this.Q);
                    break;

                case BiQuadFilterTypes.HS:
                    this.HighShelf(this.SampleRate, this.Frequency, this.Slope, this.Gain);
                    break;

                case BiQuadFilterTypes.LS:
                    this.LowShelf(this.SampleRate, this.Frequency, this.Slope, this.Gain);
                    break;

                case BiQuadFilterTypes.APF:
                    this.AllPassFilter(this.SampleRate, this.Frequency, this.Q);
                    break;

                case BiQuadFilterTypes.BPF:
                    this.BandPassFilterConstantPeakGain(this.SampleRate, this.Frequency, this.Q);
                    break;

                case BiQuadFilterTypes.NF:
                    this.NotchFilter(this.SampleRate, this.Frequency, this.Q);
                    break;

                default:
                    throw new NotSupportedException("Unknown Filter Type");
            }
        }
        #endregion

        #region Public Filter Functions
            
        /// <summary>
        /// Create a 1st Order low pass filter
        /// </summary>
        public void LowPassFilter1st(double sampleRate, double cutoffFrequency, double q)
        {
            this.SampleRate = sampleRate;
            this.Frequency = cutoffFrequency;
            this.Q = q;

            this.BiQuadFilterType = BiQuadFilterTypes.LPF;

            this.Init();
            double w0 = PI2 * cutoffFrequency / sampleRate;
            double cosw0 = Math.Cos(w0);
            double sinw0 = Math.Sin(w0);

            double b0 = sinw0;
            double b1 = sinw0;
            double b2 = 0;
            double aa0 = cosw0 + sinw0 + 1;
            double aa1 = sinw0 - cosw0 - 1;
            double aa2 = 0;
            this.SetCoefficients(aa0, aa1, aa2, b0, b1, b2);
        }

        /// <summary>
        /// Create a low pass filter
        /// </summary>
        public void LowPassFilter(double sampleRate, double cutoffFrequency, double q)
        {
            this.SampleRate = sampleRate;
            this.Frequency = cutoffFrequency;
            this.Q = q;

            this.BiQuadFilterType = BiQuadFilterTypes.LPF;

            this.Init();
            // H(s) = 1 / (s^2 + s/Q + 1)
            var w0 = PI2 * cutoffFrequency / sampleRate;
            var cosw0 = Math.Cos(w0);
            var alpha = Math.Sin(w0) / (2 * q);

            var b0 = (1 - cosw0) / 2;
            var b1 = 1 - cosw0;
            var b2 = (1 - cosw0) / 2;
            var aa0 = 1 + alpha;
            var aa1 = -2 * cosw0;
            var aa2 = 1 - alpha;
            this.SetCoefficients(aa0, aa1, aa2, b0, b1, b2);
        }

        /// <summary>
        /// Create a 1st Order High pass filter
        /// </summary>
        public void HighPassFilter1st(double sampleRate, double cutoffFrequency, double q)
        {
            this.SampleRate = sampleRate;
            this.Frequency = cutoffFrequency;
            this.Q = q;
            this.BiQuadFilterType = BiQuadFilterTypes.HPF1st;

            this.Init();
            double w0 = PI2 * cutoffFrequency / sampleRate;
            double cosw0 = Math.Cos(w0);
            double sinw0 = Math.Sin(w0);

            double b0 = cosw0 + 1;
            double b1 = -(cosw0 + 1);
            double b2 = 0;
            double aa0 = cosw0 + sinw0 + 1;
            double aa1 = sinw0 - cosw0 - 1;
            double aa2 = 0;
            this.SetCoefficients(aa0, aa1, aa2, b0, b1, b2);
        }

        public void PhaseInvertFilter(double sampleRate, double cutoffFrequency, double q)
        {
            this.SampleRate = sampleRate;
            this.Frequency = cutoffFrequency;
            this.Q = q;
            this.BiQuadFilterType = BiQuadFilterTypes.Inv;

            this.Init();

            double b0 = -1.0;
            double b1 = 0.0;
            double b2 = 0.0;
            double aa0 = 1.0;
            double aa1 = 0.0;
            double aa2 = 0.0;

            this.SetCoefficients(aa0, aa1, aa2, b0, b1, b2);
        }

        /// <summary>
        /// Create a High pass filter
        /// </summary>
        public void HighPassFilter(double sampleRate, double cutoffFrequency, double q)
        {
            this.SampleRate = sampleRate;
            this.Frequency = cutoffFrequency;
            this.Q = q;
            this.BiQuadFilterType = BiQuadFilterTypes.HPF;

            this.Init();
            // H(s) = s^2 / (s^2 + s/Q + 1)
            var w0 = PI2 * cutoffFrequency / sampleRate;
            var cosw0 = Math.Cos(w0);
            var alpha = Math.Sin(w0) / (2 * q);

            var b0 = (1 + cosw0) / 2;
            var b1 = -(1 + cosw0);
            var b2 = (1 + cosw0) / 2;
            var aa0 = 1 + alpha;
            var aa1 = -2 * cosw0;
            var aa2 = 1 - alpha;
            this.SetCoefficients(aa0, aa1, aa2, b0, b1, b2);
        }

        /// <summary>
        /// Create a bandpass filter with constant skirt gain
        /// </summary>
        //public void BandPassFilterConstantSkirtGain(double sampleRate, double centreFrequency, double q)
        //{
        //this.SampleRate = sampleRate;
        //    this.FilterType = FilterTypes.BPF;
        //    this.Init();
        //    // H(s) = s / (s^2 + s/Q + 1)  (constant skirt gain, peak gain = Q)
        //    var w0 = 2 * Math.PI * centreFrequency / sampleRate;
        //    var cosw0 = Math.Cos(w0);
        //    var sinw0 = Math.Sin(w0);
        //    var alpha = sinw0 / (2 * q);

        //    var b0 = sinw0 / 2; // =   Q*alpha
        //    var b1 = 0;
        //    var b2 = -sinw0 / 2; // =  -Q*alpha
        //    var a0 = 1 + alpha;
        //    var a1 = -2 * cosw0;
        //    var a2 = 1 - alpha;
        //    SetCoefficients(a0, a1, a2, b0, b1, b2);
        //}

        /// <summary>
        /// Create a bandpass filter with constant peak gain
        /// </summary>
        public void BandPassFilterConstantPeakGain(double sampleRate, double centreFrequency, double q)
        {
            this.SampleRate = sampleRate;
            this.Frequency = centreFrequency;
            this.Q = q;
            this.BiQuadFilterType = BiQuadFilterTypes.BPF;

            this.Init();
            // H(s) = (s/Q) / (s^2 + s/Q + 1)      (constant 0 dB peak gain)
            var w0 = PI2 * centreFrequency / sampleRate;
            var cosw0 = Math.Cos(w0);
            var sinw0 = Math.Sin(w0);
            var alpha = sinw0 / (2 * q);

            var b0 = alpha;
            var b1 = 0;
            var b2 = -alpha;
            var a0 = 1 + alpha;
            var a1 = -2 * cosw0;
            var a2 = 1 - alpha;
            this.SetCoefficients(a0, a1, a2, b0, b1, b2);
        }

        /// <summary>
        /// Creates a notch filter
        /// </summary>
        public void NotchFilter(double sampleRate, double centreFrequency, double q)
        {
            this.SampleRate = sampleRate;
            this.Frequency = centreFrequency;
            this.Q = q;
            this.BiQuadFilterType = BiQuadFilterTypes.BPF;
            this.BiQuadFilterType = BiQuadFilterTypes.NF;

            this.Init();
            // H(s) = (s^2 + 1) / (s^2 + s/Q + 1)
            var w0 = PI2 * centreFrequency / sampleRate;
            var cosw0 = Math.Cos(w0);
            var sinw0 = Math.Sin(w0);
            var alpha = sinw0 / (2 * q);

            var b0 = 1;
            var b1 = -2 * cosw0;
            var b2 = 1;
            var a0 = 1 + alpha;
            var a1 = -2 * cosw0;
            var a2 = 1 - alpha;
            this.SetCoefficients(a0, a1, a2, b0, b1, b2);
        }

        /// <summary>
        /// Creaes an all pass filter
        /// </summary>
        public void AllPassFilter(double sampleRate, double centreFrequency, double q)
        {
            this.SampleRate = sampleRate;
            this.Frequency = centreFrequency;
            this.Q = q;
            this.BiQuadFilterType = BiQuadFilterTypes.APF;
            this.Init();
            //H(s) = (s^2 - s/Q + 1) / (s^2 + s/Q + 1)
            var w0 = PI2 * centreFrequency / sampleRate;
            var cosw0 = Math.Cos(w0);
            var sinw0 = Math.Sin(w0);
            var alpha = sinw0 / (2 * q);

            var b0 = 1 - alpha;
            var b1 = -2 * cosw0;
            var b2 = 1 + alpha;
            var a0 = 1 + alpha;
            var a1 = -2 * cosw0;
            var a2 = 1 - alpha;
            this.SetCoefficients(a0, a1, a2, b0, b1, b2);
        }

        /// <summary>
        /// Create a Peaking EQ
        /// </summary>
        public void PeakingEQ(double sampleRate, double centreFrequency, double q, double dbGain)
        {
            this.SampleRate = sampleRate;
            this.Frequency = centreFrequency;
            this.Q = q;
            this.Gain = dbGain;
            this.BiQuadFilterType = BiQuadFilterTypes.PEQ;

            this.Init();
            // H(s) = (s^2 + s*(A/Q) + 1) / (s^2 + s/(A*Q) + 1)
            var w0 = PI2 * centreFrequency / sampleRate;
            var cosw0 = Math.Cos(w0);
            var sinw0 = Math.Sin(w0);
            var alpha = sinw0 / (2 * q);
            var a = Math.Pow(10, dbGain / 40); // TODO: should we square root this value?

            var b0 = 1 + alpha * a;
            var b1 = -2 * cosw0;
            var b2 = 1 - alpha * a;
            var aa0 = 1 + alpha / a;
            var aa1 = -2 * cosw0;
            var aa2 = 1 - alpha / a;
            this.SetCoefficients(aa0, aa1, aa2, b0, b1, b2);
        }

        public void UpdateGain(double newDbGain)
        {
            double oldA = Math.Pow(10, this.Gain / 40);
            double newA = Math.Pow(10, newDbGain / 40);

            double adjustmentFactor = newA / oldA;

            // Apply the adjustment factor to b0, b1, and b2
            this.b0 *= adjustmentFactor;
            this.b1 *= adjustmentFactor;
            this.b2 *= adjustmentFactor;

            // Update the a0, a1, and a2 coefficients accordingly
            this.a0 = this.b0 / this.aa0;
            this.a1 = this.b1 / this.aa0;
            this.a2 = this.b2 / this.aa0;

            // Note: aa1, aa2, a3, a4 remain unchanged as they are not directly affected by the gain change

            // Update the stored gain value
            this.Gain = newDbGain;
        }

        public void UpdateGain_LowShelf(double newDbGain)
        {
            // Update the gain
            this.Gain = newDbGain;

            // Recalculate coefficients using existing parameters and new gain
            var w0 = PI2 * this.Frequency / this.SampleRate;
            var cosw0 = Math.Cos(w0);
            var sinw0 = Math.Sin(w0);
            var a = Math.Pow(10, this.Gain / 40);
            var alpha = sinw0 / 2 * Math.Sqrt((a + 1 / a) * (1 / this.Slope - 1) + 2);
            var temp = 2 * Math.Sqrt(a) * alpha;

            var b0 = a * (a + 1 - (a - 1) * cosw0 + temp);
            var b1 = 2 * a * (a - 1 - (a + 1) * cosw0);
            var b2 = a * (a + 1 - (a - 1) * cosw0 - temp);
            var a0 = a + 1 + (a - 1) * cosw0 + temp;
            var a1 = -2 * (a - 1 + (a + 1) * cosw0);
            var a2 = a + 1 + (a - 1) * cosw0 - temp;

            // Set the new coefficients
            this.SetCoefficients(a0, a1, a2, b0, b1, b2);
        }

        public void UpdateGain_HighShelf(double newDbGain)
        {
            // Update the gain
            this.Gain = newDbGain;

            // Recalculate coefficients using existing parameters and new gain
            var w0 = PI2 * this.Frequency / this.SampleRate;
            var cosw0 = Math.Cos(w0);
            var sinw0 = Math.Sin(w0);
            var a = Math.Pow(10, this.Gain / 40);
            var alpha = sinw0 / 2 * Math.Sqrt((a + 1 / a) * (1 / this.Slope - 1) + 2);
            var temp = 2 * Math.Sqrt(a) * alpha;

            var b0 = a * (a + 1 + (a - 1) * cosw0 + temp);
            var b1 = -2 * a * (a - 1 + (a + 1) * cosw0);
            var b2 = a * (a + 1 + (a - 1) * cosw0 - temp);
            var a0 = a + 1 - (a - 1) * cosw0 + temp;
            var a1 = 2 * (a - 1 - (a + 1) * cosw0);
            var a2 = a + 1 - (a - 1) * cosw0 - temp;

            // Set the new coefficients
            this.SetCoefficients(a0, a1, a2, b0, b1, b2);
        }

        /// <summary>
        /// H(s) = A * (s^2 + (sqrt(A)/Q)*s + A)/(A*s^2 + (sqrt(A)/Q)*s + 1)
        /// </summary>
        /// <param name="sampleRate"></param>
        /// <param name="cutoffFrequency"></param>
        /// <param name="shelfSlope">a "shelf slope" parameter (for shelving EQ only).  
        /// When S = 1, the shelf slope is as steep as it can be and remain monotonically
        /// increasing or decreasing gain with frequency.  The shelf slope, in dB/octave, 
        /// remains proportional to S for all other values for a fixed f0/Fs and dBgain.</param>
        /// <param name="dbGain">Gain in decibels</param>
        public void LowShelf(double sampleRate, double cutoffFrequency, double shelfSlope, double dbGain)
        {
            this.SampleRate = sampleRate;
            this.Frequency = cutoffFrequency;
            this.Slope = shelfSlope;
            this.Gain = dbGain;
            this.BiQuadFilterType = BiQuadFilterTypes.LS;

            this.Init();
            var w0 = PI2 * cutoffFrequency / sampleRate;
            var cosw0 = Math.Cos(w0);
            var sinw0 = Math.Sin(w0);
            var a = Math.Pow(10, dbGain / 40);
            var alpha = sinw0 / 2 * Math.Sqrt((a + 1 / a) * (1 / shelfSlope - 1) + 2);
            var temp = 2 * Math.Sqrt(a) * alpha;

            var b0 = a * (a + 1 - (a - 1) * cosw0 + temp);
            var b1 = 2 * a * (a - 1 - (a + 1) * cosw0);
            var b2 = a * (a + 1 - (a - 1) * cosw0 - temp);
            var a0 = a + 1 + (a - 1) * cosw0 + temp;
            var a1 = -2 * (a - 1 + (a + 1) * cosw0);
            var a2 = a + 1 + (a - 1) * cosw0 - temp;
            this.SetCoefficients(a0, a1, a2, b0, b1, b2);
        }

        /// <summary>
        /// H(s) = A * (A*s^2 + (sqrt(A)/Q)*s + 1)/(s^2 + (sqrt(A)/Q)*s + A)
        /// </summary>
        /// <param name="sampleRate"></param>
        /// <param name="cutoffFrequency"></param>
        /// <param name="shelfSlope"></param>
        /// <param name="dbGain"></param>
        /// <returns></returns>
        public void HighShelf(double sampleRate, double cutoffFrequency, double shelfSlope, double dbGain)
        {
            this.SampleRate = sampleRate;
            this.Frequency = cutoffFrequency;
            this.Slope = shelfSlope;
            this.Gain = dbGain;
            this.BiQuadFilterType = BiQuadFilterTypes.HS;

            this.Init();
            var w0 = PI2 * cutoffFrequency / sampleRate;
            var cosw0 = Math.Cos(w0);
            var sinw0 = Math.Sin(w0);
            var a = Math.Pow(10, dbGain / 40);
            var alpha = sinw0 / 2 * Math.Sqrt((a + 1 / a) * (1 / shelfSlope - 1) + 2);
            var temp = 2 * Math.Sqrt(a) * alpha;

            var b0 = a * (a + 1 + (a - 1) * cosw0 + temp);
            var b1 = -2 * a * (a - 1 + (a + 1) * cosw0);
            var b2 = a * (a + 1 + (a - 1) * cosw0 - temp);
            var a0 = a + 1 - (a - 1) * cosw0 + temp;
            var a1 = 2 * (a - 1 - (a + 1) * cosw0);
            var a2 = a + 1 - (a - 1) * cosw0 - temp;
            this.SetCoefficients(a0, a1, a2, b0, b1, b2);
        }
        #endregion

        #region IFilter Interface
        public bool FilterEnabled { get; set; }
        public FilterTypes FilterType { get; set; }
        public FilterProcessingTypes FilterProcessingType { get; } = FilterProcessingTypes.WholeBlock;
        public IFilter GetFilter => this;

        public void ResetSampleRate(int sampleRate)
        {
            this.ChangeSampleRate(sampleRate);
        }

        public void ApplySettings()
        {
            //Non Applicable
        }
        #endregion
    }
}