#nullable enable

namespace BassThatHz_ASIO_DSP_Processor
{
    using NAudio.Utils;
    using System;
    using System.Runtime.CompilerServices;
    using System.Runtime.Serialization;
    using System.Xml.Serialization;

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
    public class Limiter : IFilter
    {
        #region Variables
        public double Threshold = 0.1000000000000000; //-20db
        public double MaxValue = 0.98855309465693886; //-0.1db
        public bool PeakHoldReleaseEnabled = true;
        public double PeakHoldRelease = 5;
        public bool PeakHoldAttackEnabled = true;
        public double PeakHoldAttack = 1;

        [IgnoreDataMember]
        public double CompressionApplied = 0;
        [IgnoreDataMember]
        public double PeakValue = 0;
        [IgnoreDataMember]
        public bool IsBrickwall = false;

        protected double AttackCoeff = 1;
        protected double ReleaseCoeff = 1;
        protected double Gain_Linear = 1;
        protected double SampleRate = 1;
        #endregion

        #region Public Functions
        public void CalculateCoeffs(double sampleRate)
        {
            this.SampleRate = sampleRate;
            this.AttackCoeff = Math.Exp(-1.0 / (0.001 * this.PeakHoldAttack * 0.5 * sampleRate));
            this.ReleaseCoeff = Math.Exp(-1.0 / (0.001 * this.PeakHoldRelease * sampleRate));
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
        public double[] Transform(double[] input, DSP_Stream currentStream)
        {
            //Range of input is -1 to +1

            //Calculate the Peak Amplitude 
            double CurrentPeakValue = 0;
            for (int i = 0; i < input.Length; i++)
                if (Math.Abs(input[i]) > CurrentPeakValue)
                    CurrentPeakValue = Math.Abs(input[i]);

            if (CurrentPeakValue > this.PeakValue)
                this.PeakValue = CurrentPeakValue;

            var GainReduction_Linear = 1d;
            bool ApplySmoothng = true;
            this.IsBrickwall = false;

            //Near Brickwall section
            if (CurrentPeakValue > this.MaxValue ||
                this.Threshold == this.MaxValue 
                && CurrentPeakValue > this.MaxValue - 0.8912509381 //-1db
                && CurrentPeakValue < this.MaxValue) 
            {
                this.IsBrickwall = true;
                //Activate really aggressive compression

                // Basic gain reduction factor
                GainReduction_Linear = this.MaxValue / CurrentPeakValue;

                // Modify gain reduction factor based on how close CurrentPeakValue is to 1
                double closenessToMax = 1 - CurrentPeakValue;
                GainReduction_Linear *= 1 - Math.Log(1 - closenessToMax + double.Epsilon);

                // Clamp GainReduction_Linear to be between 0 and 1
                GainReduction_Linear = Math.Max(0, Math.Min(GainReduction_Linear, 1));
            }

            //Brickwall limiter section
            if (this.PeakValue > this.MaxValue)
            {
                ApplySmoothng = false;
                var ExcessDB =  Decibels.LinearToDecibels(this.MaxValue) - Decibels.LinearToDecibels(this.PeakValue);
                var GainReduction_Linear2 = Decibels.DecibelsToLinear(ExcessDB);
                this.CompressionApplied = GainReduction_Linear2;
                this.Gain_Linear = GainReduction_Linear2;

                // Apply a dynamic decay of the forced peak-hold
                double decayFactor = 300000;
                double closeness = Math.Abs(Decibels.LinearToDecibels(this.PeakValue) - Decibels.LinearToDecibels(CurrentPeakValue));
                if (closeness > 35)
                    decayFactor = 100;
                this.PeakValue *= Math.Exp(-1.0 / decayFactor);

                GainReduction_Linear = GainReduction_Linear2;

                //Has clipping prevention
                if (GainReduction_Linear < 1)
                    for (int i = 0; i < input.Length; i++)
                        input[i] = Math.Min(1 - double.Epsilon, input[i] * GainReduction_Linear2);
            }

            if (ApplySmoothng)
            {
                //Dynamic compression threshold
                //If the threshold has been exceeded, start to compress the signal
                if (this.Threshold < this.MaxValue && CurrentPeakValue > this.Threshold && CurrentPeakValue < this.MaxValue)
                {
                    // Calculate the logarithmic decrease
                    double proximityToMax = Math.Max(0, (CurrentPeakValue - this.Threshold) / (this.MaxValue - this.Threshold));
                    GainReduction_Linear = 1 - Math.Log(proximityToMax + 1) / Math.Log(2);
                    // The +1 ensures the log argument is always > 0

                    GainReduction_Linear = Math.Min(1, Math.Max(0, GainReduction_Linear));
                }

                //Log Smoothing
                if (GainReduction_Linear < this.Gain_Linear)
                {
                    if (this.PeakHoldAttackEnabled)
                        this.Gain_Linear = this.AttackCoeff * (this.Gain_Linear - GainReduction_Linear) + GainReduction_Linear;
                }
                else
                {
                    if (this.PeakHoldReleaseEnabled)
                        this.Gain_Linear = this.ReleaseCoeff * (this.Gain_Linear - GainReduction_Linear) + GainReduction_Linear;
                }

                this.CompressionApplied = this.Gain_Linear;

                if (this.Gain_Linear < 1)
                    //Compression every sample by the amount
                    //Has clipping prevention
                    for (int i = 0; i < input.Length; i++)
                        input[i] = Math.Min(1 - double.Epsilon, input[i] * this.Gain_Linear);
            }
            return input;
        }

        public void ResetSampleRate(int sampleRate)
        {
            this.CalculateCoeffs(sampleRate);
        }

        public void ApplySettings()
        {
            this.PeakValue = 0;
        }
        #endregion

        #region IFilter Interface

        protected bool _FilterEnabled;
        public bool FilterEnabled
        {
            get
            {
                return this._FilterEnabled;
            }
            set
            {
                this.PeakValue = 0;
                this._FilterEnabled = value;
            }
        }

        public IFilter GetFilter => this;

        public FilterTypes FilterType { get; } = FilterTypes.Limiter;
        public FilterProcessingTypes FilterProcessingType { get; } = FilterProcessingTypes.WholeBlock;

        #endregion

    }
}