#nullable enable

namespace BassThatHz_ASIO_DSP_Processor;

#region Usings
using System;
using System.Runtime.CompilerServices;
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
public class Delay : IFilter
{
    #region Protected Variables       
    protected object ResizeBufferLockObject = new();
    protected double[]? DelayBuffer;
    protected int DelayBufferLength = 0;

    protected int ReadIndex = 0;
    protected int WriteIndex = 0;

    protected int BufferSize = 0;
    protected int SampleRate = 0;

    protected int Delay_InSamples = 0;
    #endregion

    #region Public Properties
    protected decimal _DelayInMS = 0;
    public decimal DelayInMS
    {
        get
        {
            return this._DelayInMS;
        }
        set
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException("delayInMS cannot be negative");

            this._DelayInMS = value;
            this.Reset_DelayAndBufferSize();
        }

    }
    #endregion

    #region Public Functions
    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public double[] Transform(double[] input, DSP_Stream currentStream)
    {
        // Forward to span-based in-place implementation (zero-copy)
        TransformInPlace(input.AsSpan(), currentStream);
        return input;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public void TransformInPlace(Span<double> input, DSP_Stream currentStream)
    {
        var buf = this.DelayBuffer;
        int bufLen = this.DelayBufferLength;

        if (buf == null || bufLen <= 0)
            return;

        int ri = this.ReadIndex;
        int wi = this.WriteIndex;

        int n = input.Length;
        for (int i = 0; i < n; i++)
        {
            if (ri >= bufLen) ri -= bufLen;
            if (wi >= bufLen) wi -= bufLen;

            double inSample = input[i];

            // write current sample into write index (to be read later)
            buf[wi] = inSample;

            // read delayed sample from read index
            double outSample = buf[ri];

            input[i] = outSample;

            wi++;
            ri++;
        }

        // store indices back (keep within range)
        if (ri >= bufLen) ri -= bufLen;
        if (wi >= bufLen) wi -= bufLen;
        this.ReadIndex = ri;
        this.WriteIndex = wi;
    }

    public void ResetSampleRate(int sampleRate)
    {
        if (sampleRate < 0)
            throw new ArgumentOutOfRangeException("sampleRate cannot be negative");

        this.SampleRate = sampleRate;
        this.Reset_DelayAndBufferSize();
    }

    public void ResetBufferSize(int bufferSize)
    {
        if (bufferSize < 0)
            throw new ArgumentOutOfRangeException("bufferSize cannot be negative");

        this.BufferSize = bufferSize;
        this.ResizeBuffer();
    }

    public void Initialize(decimal delayInMS, int bufferSize, int sampleRate)
    {
        if (bufferSize < 0)
            throw new ArgumentOutOfRangeException("bufferSize cannot be negative");

        if (sampleRate < 0)
            throw new ArgumentOutOfRangeException("sampleRate cannot be negative");

        if (delayInMS < 0)
            throw new ArgumentOutOfRangeException("delayInMS cannot be negative");

        this.SampleRate = sampleRate;
        this.DelayInMS = delayInMS;
        this.BufferSize = bufferSize;

        this.Reset_DelayAndBufferSize();
    }

    public void ApplySettings()
    {
        //Non-Applicable
    }
    #endregion

    #region Protected Functions

    protected void Reset_DelayAndBufferSize()
    {
        //Rounds the delay to the nearest sample
        //Use double math for accuracy and performance (DelayInMS is decimal)
        this.Delay_InSamples = (int)((double)this.SampleRate * (double)this._DelayInMS / 1000.0);

        this.ResizeBuffer();
    }

    protected void ResizeBuffer()
    {
        lock (this.ResizeBufferLockObject) //The array is only resized via one thread
        {
            int desired = this.Delay_InSamples + this.BufferSize;

            // If existing buffer is large enough, reuse it to avoid allocation.
            if (this.DelayBuffer == null || this.DelayBuffer.Length < desired)
            {
                this.DelayBuffer = new double[desired];
            }
            else if (this.DelayBuffer.Length >= desired)
            {
                // Clear only the used portion to reset state
                Array.Clear(this.DelayBuffer, 0, desired);
            }

            this.DelayBufferLength = desired;

            // Reset the Indexes
            this.ReadIndex = 0;
            // The index of the first initial sample that was delayed
            this.WriteIndex = this.Delay_InSamples;
        }
    }
    #endregion

    #region IFilter Interface

    public bool FilterEnabled { get; set; }
    
    public IFilter GetFilter => this;
    
    public FilterTypes FilterType { get; } = FilterTypes.Delay;
    public FilterProcessingTypes FilterProcessingType { get; } = FilterProcessingTypes.WholeBlock;

    public IFilter DeepClone()
    {
        return CommonFunctions.DeepClone(this);
    }
    #endregion
}