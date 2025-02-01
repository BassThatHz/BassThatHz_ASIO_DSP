#nullable disable

namespace BassThatHz_ASIO_DSP_Processor;

#region Usings
using NAudio.Dsp;
using System;
using System.Runtime.CompilerServices;
#endregion

[Serializable]
public class Basic_HPF_LPF : IFilter
{
    #region Enums
    public enum FilterOrder
    {
        LR_12db,
        LR_24db,
        LR_48db,
        BW_6db,
        BW_12db,
        BW_18db,
        BW_24db,
        BW_30db,
        BW_36db,
        BW_42db,
        BW_48db,
        None
    }
    #endregion

    #region Variables
    public double HPFFreq = 1;
    public FilterOrder HPFFilter = FilterOrder.LR_12db;

    public double LPFFreq = 20000;
    public FilterOrder LPFFilter = FilterOrder.LR_12db;

    public readonly double[] Q_Array_HPF = new double[4];
    public readonly double[] Q_Array_LPF = new double[4];
    public readonly BiQuadFilter[] BiQuads = new BiQuadFilter[8];

    protected double SampleRate = (double)Program.DSP_Info.InSampleRate;
    #endregion

    #region Public Functions
    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public double[] Transform(double[] input, DSP_Stream currentStream)
    {
        if (this.HPFFilter != FilterOrder.None || this.LPFFilter != FilterOrder.None)
        {
            if (this.HPFFilter != FilterOrder.None)
                for (int j = 0; j < 4; j++) //Only 4 BiQuads are supported (up to 48db/oct)
                    if (this.BiQuads[j] != null)
                        input = this.BiQuads[j].Transform(input, currentStream);

            if (this.LPFFilter != FilterOrder.None)
                for (int j = 4; j < 8; j++) //Only 4 BiQuads are supported (up to 48db/oct)
                    if (this.BiQuads[j] != null)
                        input = this.BiQuads[j].Transform(input, currentStream);
        }

        return input;
    }

    public void ResetSampleRate(int sampleRate)
    {
        this.SampleRate = sampleRate;
        this.ApplySettings();
    }

    public void ApplySettings()
    {
        this.ApplyBiQuadOrders_HPF();
        this.ApplyBiQuadOrders_LPF();
    }

    #endregion

    #region Protected Functions
    protected void ProcessFilterOrders(FilterOrder input, double[] qArray)
    {
        switch (input)
        {
            case FilterOrder.LR_12db:
                qArray[0] = 0;
                qArray[1] = 0;
                qArray[2] = 0;
                qArray[3] = 0;
                break;

            case FilterOrder.LR_24db:
                qArray[0] = Math.Sqrt(0.5);
                qArray[1] = Math.Sqrt(0.5);
                qArray[2] = 0;
                qArray[3] = 0;
                break;

            case FilterOrder.LR_48db:
                qArray[0] = 1.0 / 1.8478;
                qArray[1] = 1.0 / 0.7654;
                qArray[2] = 1.0 / 1.8478;
                qArray[3] = 1.0 / 0.7654;
                break;

            case FilterOrder.BW_6db:
                qArray[0] = 0;
                qArray[1] = 0;
                qArray[2] = 0;
                qArray[3] = 0;
                break;

            case FilterOrder.BW_12db:
                qArray[0] = Math.Sqrt(0.5);
                qArray[1] = 0;
                qArray[2] = 0;
                qArray[3] = 0;
                break;

            case FilterOrder.BW_18db:
                qArray[0] = 0;
                qArray[1] = 1;
                qArray[2] = 0;
                qArray[3] = 0;
                break;

            case FilterOrder.BW_24db:
                qArray[0] = 1.0 / 1.8478;
                qArray[1] = 1.0 / 0.7654;
                qArray[2] = 0;
                qArray[3] = 0;
                break;

            case FilterOrder.BW_30db:
                qArray[0] = 0;
                qArray[1] = 1.0 / 0.6180;
                qArray[2] = 1.0 / 1.6180;
                qArray[3] = 0;
                break;

            case FilterOrder.BW_36db:
                qArray[0] = 1.0 / 1.9319;
                qArray[1] = Math.Sqrt(0.5);
                qArray[2] = 1.0 / 0.5176;
                qArray[3] = 0;
                break;

            case FilterOrder.BW_42db:
                qArray[0] = 0;
                qArray[1] = 1.0 / 1.8019;
                qArray[2] = 1.0 / 1.2470;
                qArray[3] = 1.0 / 0.4450;
                break;

            case FilterOrder.BW_48db:
                qArray[0] = 1.0 / 1.96161;
                qArray[1] = 1.0 / 1.6629;
                qArray[2] = 1.0 / 1.1111;
                qArray[3] = 1.0 / 0.3902;
                break;

            default:
                throw new InvalidOperationException("FilterOrder not defined.");
        }
    }

    protected void ApplyBiQuadOrders_HPF()
    {
        if (this.HPFFilter == FilterOrder.None)
            return; //Early exit

        this.ProcessFilterOrders(this.HPFFilter, this.Q_Array_HPF);

        switch (this.HPFFilter)
        {
            case FilterOrder.LR_12db:
                this.BiQuads[0] = new BiQuadFilter();
                this.BiQuads[1] = new BiQuadFilter();
                this.BiQuads[2] = new BiQuadFilter();
                this.BiQuads[0].HighPassFilter1st(this.SampleRate, this.HPFFreq, this.Q_Array_HPF[0]);
                this.BiQuads[1].HighPassFilter1st(this.SampleRate, this.HPFFreq, this.Q_Array_HPF[1]);
                this.BiQuads[2].PhaseInvertFilter(this.SampleRate, this.HPFFreq, this.Q_Array_HPF[2]);
                this.BiQuads[3] = null;
                break;

            case FilterOrder.LR_24db:
                this.BiQuads[0] = new BiQuadFilter();
                this.BiQuads[1] = new BiQuadFilter();
                this.BiQuads[0].HighPassFilter(this.SampleRate, this.HPFFreq, this.Q_Array_HPF[0]);
                this.BiQuads[1].HighPassFilter(this.SampleRate, this.HPFFreq, this.Q_Array_HPF[1]);
                this.BiQuads[2] = null;
                this.BiQuads[3] = null;
                break;

            case FilterOrder.LR_48db:
                this.BiQuads[0] = new BiQuadFilter();
                this.BiQuads[1] = new BiQuadFilter();
                this.BiQuads[2] = new BiQuadFilter();
                this.BiQuads[3] = new BiQuadFilter();
                this.BiQuads[0].HighPassFilter(this.SampleRate, this.HPFFreq, this.Q_Array_HPF[0]);
                this.BiQuads[1].HighPassFilter(this.SampleRate, this.HPFFreq, this.Q_Array_HPF[1]);
                this.BiQuads[2].HighPassFilter(this.SampleRate, this.HPFFreq, this.Q_Array_HPF[2]);
                this.BiQuads[3].HighPassFilter(this.SampleRate, this.HPFFreq, this.Q_Array_HPF[3]);
                break;

            case FilterOrder.BW_6db:
                this.BiQuads[0] = new BiQuadFilter();
                this.BiQuads[0].HighPassFilter1st(this.SampleRate, this.HPFFreq, this.Q_Array_HPF[0]);
                this.BiQuads[1] = null;
                this.BiQuads[2] = null;
                this.BiQuads[3] = null;
                break;

            case FilterOrder.BW_12db:
                this.BiQuads[0] = new BiQuadFilter();
                this.BiQuads[1] = new BiQuadFilter();
                this.BiQuads[0].HighPassFilter(this.SampleRate, this.HPFFreq, this.Q_Array_HPF[0]);
                this.BiQuads[1].PhaseInvertFilter(this.SampleRate, this.HPFFreq, this.Q_Array_HPF[1]);
                this.BiQuads[2] = null;
                this.BiQuads[3] = null;
                break;

            case FilterOrder.BW_18db:
                this.BiQuads[0] = new BiQuadFilter();
                this.BiQuads[1] = new BiQuadFilter();
                this.BiQuads[0].HighPassFilter1st(this.SampleRate, this.HPFFreq, this.Q_Array_HPF[0]);
                this.BiQuads[1].HighPassFilter(this.SampleRate, this.HPFFreq, this.Q_Array_HPF[1]);
                this.BiQuads[2] = null;
                this.BiQuads[3] = null;
                break;

            case FilterOrder.BW_24db:
                this.BiQuads[0] = new BiQuadFilter();
                this.BiQuads[1] = new BiQuadFilter();
                this.BiQuads[0].HighPassFilter(this.SampleRate, this.HPFFreq, this.Q_Array_HPF[0]);
                this.BiQuads[1].HighPassFilter(this.SampleRate, this.HPFFreq, this.Q_Array_HPF[1]);
                this.BiQuads[2] = null;
                this.BiQuads[3] = null;
                break;

            case FilterOrder.BW_30db:
                this.BiQuads[0] = new BiQuadFilter();
                this.BiQuads[1] = new BiQuadFilter();
                this.BiQuads[2] = new BiQuadFilter();
                this.BiQuads[0].HighPassFilter1st(this.SampleRate, this.HPFFreq, this.Q_Array_HPF[0]);
                this.BiQuads[1].HighPassFilter(this.SampleRate, this.HPFFreq, this.Q_Array_HPF[1]);
                this.BiQuads[2].HighPassFilter(this.SampleRate, this.HPFFreq, this.Q_Array_HPF[2]);
                this.BiQuads[3] = null;
                break;

            case FilterOrder.BW_36db:
                this.BiQuads[0] = new BiQuadFilter();
                this.BiQuads[1] = new BiQuadFilter();
                this.BiQuads[2] = new BiQuadFilter();
                this.BiQuads[3] = new BiQuadFilter();
                this.BiQuads[0].HighPassFilter(this.SampleRate, this.HPFFreq, this.Q_Array_HPF[0]);
                this.BiQuads[1].HighPassFilter(this.SampleRate, this.HPFFreq, this.Q_Array_HPF[1]);
                this.BiQuads[2].HighPassFilter(this.SampleRate, this.HPFFreq, this.Q_Array_HPF[2]);
                this.BiQuads[3].PhaseInvertFilter(this.SampleRate, this.HPFFreq, this.Q_Array_HPF[3]);
                break;

            case FilterOrder.BW_42db:
                this.BiQuads[0] = new BiQuadFilter();
                this.BiQuads[1] = new BiQuadFilter();
                this.BiQuads[2] = new BiQuadFilter();
                this.BiQuads[3] = new BiQuadFilter();
                this.BiQuads[0].HighPassFilter1st(this.SampleRate, this.HPFFreq, this.Q_Array_HPF[0]);
                this.BiQuads[1].HighPassFilter(this.SampleRate, this.HPFFreq, this.Q_Array_HPF[1]);
                this.BiQuads[2].HighPassFilter(this.SampleRate, this.HPFFreq, this.Q_Array_HPF[2]);
                this.BiQuads[3].HighPassFilter(this.SampleRate, this.HPFFreq, this.Q_Array_HPF[3]);
                break;

            case FilterOrder.BW_48db:
                this.BiQuads[0] = new BiQuadFilter();
                this.BiQuads[1] = new BiQuadFilter();
                this.BiQuads[2] = new BiQuadFilter();
                this.BiQuads[3] = new BiQuadFilter();
                this.BiQuads[0].HighPassFilter(this.SampleRate, this.HPFFreq, this.Q_Array_HPF[0]);
                this.BiQuads[1].HighPassFilter(this.SampleRate, this.HPFFreq, this.Q_Array_HPF[1]);
                this.BiQuads[2].HighPassFilter(this.SampleRate, this.HPFFreq, this.Q_Array_HPF[2]);
                this.BiQuads[3].HighPassFilter(this.SampleRate, this.HPFFreq, this.Q_Array_HPF[3]);
                break;

            default:
                throw new InvalidOperationException("FilterOrder not defined.");
        }
    }

    protected void ApplyBiQuadOrders_LPF()
    {
        if (this.LPFFilter == FilterOrder.None)
            return; //Early exit

        this.ProcessFilterOrders(this.LPFFilter, this.Q_Array_LPF);

        switch (this.LPFFilter)
        {
            case FilterOrder.LR_12db:
                this.BiQuads[4] = new BiQuadFilter();
                this.BiQuads[5] = new BiQuadFilter();
                this.BiQuads[4].LowPassFilter1st(this.SampleRate, this.LPFFreq, this.Q_Array_LPF[0]);
                this.BiQuads[5].LowPassFilter1st(this.SampleRate, this.LPFFreq, this.Q_Array_LPF[1]);
                this.BiQuads[6] = null;
                this.BiQuads[7] = null;
                break;

            case FilterOrder.LR_24db:
                this.BiQuads[4] = new BiQuadFilter();
                this.BiQuads[5] = new BiQuadFilter();
                this.BiQuads[4].LowPassFilter(this.SampleRate, this.LPFFreq, this.Q_Array_LPF[0]);
                this.BiQuads[5].LowPassFilter(this.SampleRate, this.LPFFreq, this.Q_Array_LPF[1]);
                this.BiQuads[6] = null;
                this.BiQuads[7] = null;
                break;

            case FilterOrder.LR_48db:
                this.BiQuads[4] = new BiQuadFilter();
                this.BiQuads[5] = new BiQuadFilter();
                this.BiQuads[6] = new BiQuadFilter();
                this.BiQuads[7] = new BiQuadFilter();
                this.BiQuads[4].LowPassFilter(this.SampleRate, this.LPFFreq, this.Q_Array_LPF[0]);
                this.BiQuads[5].LowPassFilter(this.SampleRate, this.LPFFreq, this.Q_Array_LPF[1]);
                this.BiQuads[6].LowPassFilter(this.SampleRate, this.LPFFreq, this.Q_Array_LPF[2]);
                this.BiQuads[7].LowPassFilter(this.SampleRate, this.LPFFreq, this.Q_Array_LPF[3]);
                break;

            case FilterOrder.BW_6db:
                this.BiQuads[4] = new BiQuadFilter();
                this.BiQuads[4].LowPassFilter1st(this.SampleRate, this.LPFFreq, this.Q_Array_LPF[0]);
                this.BiQuads[5] = null;
                this.BiQuads[6] = null;
                this.BiQuads[7] = null;
                break;

            case FilterOrder.BW_12db:
                this.BiQuads[4] = new BiQuadFilter();
                this.BiQuads[4].LowPassFilter(this.SampleRate, this.LPFFreq, this.Q_Array_LPF[0]);
                this.BiQuads[5] = null;
                this.BiQuads[6] = null;
                this.BiQuads[7] = null;
                break;

            case FilterOrder.BW_18db:
                this.BiQuads[4] = new BiQuadFilter();
                this.BiQuads[5] = new BiQuadFilter();
                this.BiQuads[4].LowPassFilter1st(this.SampleRate, this.LPFFreq, this.Q_Array_LPF[0]);
                this.BiQuads[5].LowPassFilter(this.SampleRate, this.LPFFreq, this.Q_Array_LPF[1]);
                this.BiQuads[6] = null;
                this.BiQuads[7] = null;
                break;

            case FilterOrder.BW_24db:
                this.BiQuads[4] = new BiQuadFilter();
                this.BiQuads[5] = new BiQuadFilter();
                this.BiQuads[4].LowPassFilter(this.SampleRate, this.LPFFreq, this.Q_Array_LPF[0]);
                this.BiQuads[5].LowPassFilter(this.SampleRate, this.LPFFreq, this.Q_Array_LPF[1]);
                this.BiQuads[6] = null;
                this.BiQuads[7] = null;
                break;

            case FilterOrder.BW_30db:
                this.BiQuads[4] = new BiQuadFilter();
                this.BiQuads[5] = new BiQuadFilter();
                this.BiQuads[6] = new BiQuadFilter();
                this.BiQuads[4].LowPassFilter1st(this.SampleRate, this.LPFFreq, this.Q_Array_LPF[0]);
                this.BiQuads[5].LowPassFilter(this.SampleRate, this.LPFFreq, this.Q_Array_LPF[1]);
                this.BiQuads[6].LowPassFilter(this.SampleRate, this.LPFFreq, this.Q_Array_LPF[2]);
                this.BiQuads[7] = null;
                break;

            case FilterOrder.BW_36db:
                this.BiQuads[4] = new BiQuadFilter();
                this.BiQuads[5] = new BiQuadFilter();
                this.BiQuads[6] = new BiQuadFilter();
                this.BiQuads[4].LowPassFilter(this.SampleRate, this.LPFFreq, this.Q_Array_LPF[0]);
                this.BiQuads[5].LowPassFilter(this.SampleRate, this.LPFFreq, this.Q_Array_LPF[1]);
                this.BiQuads[6].LowPassFilter(this.SampleRate, this.LPFFreq, this.Q_Array_LPF[2]);
                this.BiQuads[7] = null;
                break;

            case FilterOrder.BW_42db:
                this.BiQuads[4] = new BiQuadFilter();
                this.BiQuads[5] = new BiQuadFilter();
                this.BiQuads[6] = new BiQuadFilter();
                this.BiQuads[7] = new BiQuadFilter();
                this.BiQuads[4].LowPassFilter1st(this.SampleRate, this.LPFFreq, this.Q_Array_LPF[0]);
                this.BiQuads[5].LowPassFilter(this.SampleRate, this.LPFFreq, this.Q_Array_LPF[1]);
                this.BiQuads[6].LowPassFilter(this.SampleRate, this.LPFFreq, this.Q_Array_LPF[2]);
                this.BiQuads[7].LowPassFilter(this.SampleRate, this.LPFFreq, this.Q_Array_LPF[3]);
                break;

            case FilterOrder.BW_48db:
                this.BiQuads[4] = new BiQuadFilter();
                this.BiQuads[5] = new BiQuadFilter();
                this.BiQuads[6] = new BiQuadFilter();
                this.BiQuads[7] = new BiQuadFilter();
                this.BiQuads[4].LowPassFilter(this.SampleRate, this.LPFFreq, this.Q_Array_LPF[0]);
                this.BiQuads[5].LowPassFilter(this.SampleRate, this.LPFFreq, this.Q_Array_LPF[1]);
                this.BiQuads[6].LowPassFilter(this.SampleRate, this.LPFFreq, this.Q_Array_LPF[2]);
                this.BiQuads[7].LowPassFilter(this.SampleRate, this.LPFFreq, this.Q_Array_LPF[3]);
                break;

            default:
                throw new InvalidOperationException("FilterOrder not defined.");
        }
    }
    #endregion

    #region IFilter Interface
    public bool FilterEnabled { get; set; }

    public FilterTypes FilterType => FilterTypes.Basic_HPF_LPF;
    public FilterProcessingTypes FilterProcessingType { get; } = FilterProcessingTypes.WholeBlock;
    public IFilter GetFilter => this;
    
    #endregion
}