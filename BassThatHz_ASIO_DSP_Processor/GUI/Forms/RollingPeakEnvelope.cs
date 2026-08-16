#nullable enable

namespace BassThatHz_ASIO_DSP_Processor.GUI.Forms;

#region Usings
using System;
#endregion

/// <summary>
///  BassThatHz ASIO DSP Processor Engine
///  Copyright (c) 2026 BassThatHz
///
/// A rolling peak envelope over a fixed time window, used to drive the waveform charts' Y axis.
///
/// The window is split into equal buckets, each holding the largest magnitude seen during it. The
/// window peak is the largest bucket, so a loud passage stops influencing the axis exactly one
/// window after it ends rather than being held forever - and it fades out one bucket at a time
/// instead of being thrown away all at once.
///
/// On top of that the reported magnitude follows the window peak UPWARDS immediately (the axis must
/// never clip the waveform) and eases DOWNWARDS with an exponential time constant, so the range
/// glides rather than stepping.
///
/// Time is supplied by the caller rather than read from the clock, so the behaviour is
/// deterministic and can be unit tested by advancing a timestamp.
/// </summary>
public class RollingPeakEnvelope
{
    #region Variables
    protected readonly double[] BucketPeaks;
    protected readonly long BucketTicks;
    protected readonly double DecaySeconds;
    protected readonly double MinimumMagnitude;

    protected long CurrentBucket;
    protected DateTime LastUpdate;
    protected double CurrentMagnitude;
    protected bool Primed;
    #endregion

    #region Constructor
    /// <summary>
    /// Creates a rolling peak envelope.
    /// </summary>
    /// <param name="window">How far back the peak is remembered.</param>
    /// <param name="bucketCount">
    /// How finely the window is subdivided. More buckets means the envelope steps down in smaller
    /// increments as old peaks age out.
    /// </param>
    /// <param name="decayTimeConstant">
    /// How quickly the reported magnitude eases down towards the window peak. Zero follows the
    /// window peak with no smoothing at all.
    /// </param>
    /// <param name="minimumMagnitude">
    /// The smallest magnitude ever reported, so a silent input still gives the chart a usable,
    /// non-zero axis instead of collapsing it.
    /// </param>
    public RollingPeakEnvelope(TimeSpan window, int bucketCount, TimeSpan decayTimeConstant,
                               double minimumMagnitude = 0.0001d)
    {
        if (bucketCount < 1)
            bucketCount = 1;

        if (window <= TimeSpan.Zero)
            window = TimeSpan.FromSeconds(1);

        this.BucketPeaks = new double[bucketCount];
        this.BucketTicks = Math.Max(1L, window.Ticks / bucketCount);
        this.DecaySeconds = Math.Max(0d, decayTimeConstant.TotalSeconds);
        this.MinimumMagnitude = double.IsFinite(minimumMagnitude) ? Math.Max(0d, minimumMagnitude) : 0d;
        this.CurrentMagnitude = this.MinimumMagnitude;
    }
    #endregion

    #region Properties
    /// <summary>
    /// The magnitude currently reported, i.e. the half-height of the chart's Y axis.
    /// </summary>
    public double Current
    {
        get { return this.CurrentMagnitude; }
    }

    /// <summary>
    /// The effective window length, which is the bucket length times the bucket count.
    /// </summary>
    public TimeSpan Window
    {
        get { return TimeSpan.FromTicks(this.BucketTicks * this.BucketPeaks.Length); }
    }

    /// <summary>
    /// The number of buckets the window is divided into.
    /// </summary>
    public int BucketCount
    {
        get { return this.BucketPeaks.Length; }
    }
    #endregion

    #region Public Functions
    /// <summary>
    /// Folds one block's magnitude into the window and returns the magnitude to plot with.
    /// </summary>
    /// <param name="magnitude">The block's peak magnitude. The sign is ignored.</param>
    /// <param name="timestamp">When the block was observed.</param>
    /// <returns>The smoothed rolling magnitude.</returns>
    public double Update(double magnitude, DateTime timestamp)
    {
        if (!double.IsFinite(magnitude))
            magnitude = 0d;

        magnitude = Math.Abs(magnitude);

        long Local_Bucket = timestamp.Ticks / this.BucketTicks;

        //A first sample, or a clock that jumped backwards, has no usable history: adopt the value
        //outright rather than easing towards it from a stale envelope.
        if (!this.Primed || Local_Bucket < this.CurrentBucket)
        {
            this.Primed = true;
            Array.Clear(this.BucketPeaks, 0, this.BucketPeaks.Length);
            this.CurrentBucket = Local_Bucket;
            this.BucketPeaks[WrapIndex(Local_Bucket, this.BucketPeaks.Length)] = magnitude;
            this.LastUpdate = timestamp;
            this.CurrentMagnitude = Math.Max(magnitude, this.MinimumMagnitude);
            return this.CurrentMagnitude;
        }

        //Retire every bucket the clock has moved past, so peaks older than the window are gone.
        if (Local_Bucket > this.CurrentBucket)
        {
            long Local_Skipped = Local_Bucket - this.CurrentBucket;
            if (Local_Skipped >= this.BucketPeaks.Length)
            {
                Array.Clear(this.BucketPeaks, 0, this.BucketPeaks.Length);
            }
            else
            {
                for (long Local_Retire = this.CurrentBucket + 1; Local_Retire <= Local_Bucket; Local_Retire++)
                    this.BucketPeaks[WrapIndex(Local_Retire, this.BucketPeaks.Length)] = 0d;
            }

            this.CurrentBucket = Local_Bucket;
        }

        int Local_Index = WrapIndex(Local_Bucket, this.BucketPeaks.Length);
        if (magnitude > this.BucketPeaks[Local_Index])
            this.BucketPeaks[Local_Index] = magnitude;

        double Local_Target = this.MinimumMagnitude;
        for (int i = 0; i < this.BucketPeaks.Length; i++)
        {
            if (this.BucketPeaks[i] > Local_Target)
                Local_Target = this.BucketPeaks[i];
        }

        double Local_ElapsedSeconds = Math.Max(0d, (timestamp - this.LastUpdate).TotalSeconds);
        this.LastUpdate = timestamp;

        if (Local_Target >= this.CurrentMagnitude || this.DecaySeconds <= 0d)
        {
            //Never clip: a louder window peak takes effect on the very next plot.
            this.CurrentMagnitude = Local_Target;
        }
        else
        {
            //Ease down. Framing it in elapsed time keeps the decay rate independent of how often
            //the plot timer happens to tick.
            double Local_Alpha = 1d - Math.Exp(-Local_ElapsedSeconds / this.DecaySeconds);
            this.CurrentMagnitude += (Local_Target - this.CurrentMagnitude) * Local_Alpha;

            //The envelope approaches the window peak from above and must never dip below it.
            if (this.CurrentMagnitude < Local_Target)
                this.CurrentMagnitude = Local_Target;
        }

        if (this.CurrentMagnitude < this.MinimumMagnitude)
            this.CurrentMagnitude = this.MinimumMagnitude;

        return this.CurrentMagnitude;
    }

    /// <summary>
    /// Forgets the window entirely, so the next update is adopted outright.
    /// </summary>
    public void Reset()
    {
        Array.Clear(this.BucketPeaks, 0, this.BucketPeaks.Length);
        this.Primed = false;
        this.CurrentBucket = 0;
        this.LastUpdate = DateTime.MinValue;
        this.CurrentMagnitude = this.MinimumMagnitude;
    }
    #endregion

    #region Protected Functions
    /// <summary>
    /// Maps an absolute bucket number onto the ring of stored buckets.
    /// </summary>
    /// <param name="bucket">The absolute bucket number.</param>
    /// <param name="length">The number of stored buckets.</param>
    /// <returns>An index into the bucket ring.</returns>
    protected static int WrapIndex(long bucket, int length)
    {
        int Local_Index = (int)(bucket % length);
        if (Local_Index < 0)
            Local_Index += length;
        return Local_Index;
    }
    #endregion
}
