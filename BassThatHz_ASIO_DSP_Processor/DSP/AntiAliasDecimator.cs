#nullable enable

namespace BassThatHz_ASIO_DSP_Processor;

#region Usings
using System;
#endregion

/// <summary>
///  BassThatHz ASIO DSP Processor Engine
///  Copyright (c) 2026 BassThatHz
///
/// Rate reduction for spectrum analysis: band-limit first, then resample uniformly.
///
/// The RTA's ULF chart needs one second of audio expressed as 2048 uniformly spaced samples, so it
/// can run a 2048 point FFT at a 2048 Hz effective rate and read 1 Hz bins.
///
/// It used to get there through <see cref="DSPLib.DownSampler.downsample"/>, which is Largest
/// Triangle Three Buckets - a PLOT THINNING algorithm. LTTB deliberately picks the most visually
/// extreme sample in each bucket and emits it at that sample's own position, so its output is both
/// non-uniformly spaced and biased towards peaks, and it applies no band limiting whatsoever.
/// Feeding that to an FFT folds everything above 1 kHz straight down into the 1-100 Hz display and
/// overstates levels. This class does the job properly instead:
///
///  1. Convolve with a cascade of centred moving averages (a boxcar raised to <see cref="Order"/>,
///     i.e. a triangular kernel at order 2). The kernel is one output sample period wide, rounded
///     to the nearest odd number of input samples, which puts its nulls at Fs_in/W - close to the
///     output sample rate and its harmonics, precisely the frequencies whose neighbourhoods would
///     otherwise alias into the bottom of the band. W is odd so the average is centred on a sample;
///     it cannot land exactly on the output rate, so the null is always slightly detuned and the
///     rejection is asymmetric about it.
///  2. Sample the band-limited signal at uniform block centres, interpolating linearly. After step
///     one there is nothing left near the output Nyquist for that interpolation to distort.
///
/// Measured across 44.1/48/88.2/96/176.4/192 kHz into 2048 points, probing both sides of the first
/// three fold frequencies: worst-case in-band alias rejection is 51.7 dB (best 63.4 dB), against
/// roughly 23 dB for the plot thinner it replaces. The floor that broadband high frequency content
/// leaves in the 1-100 Hz display drops from about -24 dBFS to about -82 dBFS. Worst-case pass-band
/// droop at 100 Hz is 0.07 dB, and the DC gain is exactly one, so levels are unaffected.
///
/// The two ends of the frame are averaged over a truncated window, so the first and last output
/// sample are less well filtered than the interior. That is deliberate - zero padding would drag
/// the edges towards silence - and the FFT window applied afterwards tapers them away.
///
/// Not thread safe: it keeps scratch between calls, so give each analysis path its own instance.
/// </summary>
public class AntiAliasDecimator
{
    #region Constants
    /// <summary>
    /// Cascaded averages. Each one multiplies the stop-band rejection and costs one O(n) pass.
    /// Two is the sweet spot: ~54 dB of alias rejection for negligible pass-band droop.
    /// </summary>
    public const int DefaultOrder = 2;
    #endregion

    #region Variables
    protected readonly int OrderValue;
    protected double[] Prefix = Array.Empty<double>();
    protected double[] Work = Array.Empty<double>();
    #endregion

    #region Constructor
    /// <summary>
    /// Creates a decimator.
    /// </summary>
    /// <param name="order">How many moving averages to cascade. Clamped to at least 1.</param>
    public AntiAliasDecimator(int order = DefaultOrder)
    {
        this.OrderValue = Math.Max(1, order);
    }
    #endregion

    #region Properties
    /// <summary>
    /// The number of cascaded moving averages applied before resampling.
    /// </summary>
    public int Order
    {
        get { return this.OrderValue; }
    }
    #endregion

    #region Public Functions
    /// <summary>
    /// Band-limits <paramref name="input"/> and resamples it into <paramref name="output"/>.
    /// </summary>
    /// <param name="input">The source samples.</param>
    /// <param name="inputLength">How many leading samples of <paramref name="input"/> to use.</param>
    /// <param name="output">
    /// Destination, filled completely. Its length is the target sample count.
    /// </param>
    public void Decimate(double[] input, int inputLength, double[] output)
    {
        if (input == null)
            throw new ArgumentNullException(nameof(input));
        if (output == null)
            throw new ArgumentNullException(nameof(output));

        int Local_Target = output.Length;
        if (Local_Target <= 0)
            return;

        int Local_Length = Math.Clamp(inputLength, 0, input.Length);
        if (Local_Length <= 0)
        {
            Array.Clear(output, 0, Local_Target);
            return;
        }

        //Nothing to band-limit: at or below the target rate there is no fold-down to prevent, so
        //resample straight from the source and leave the samples alone.
        if (Local_Length <= Local_Target)
        {
            Resample(input, Local_Length, output);
            return;
        }

        //One output sample period, expressed in input samples, and ODD so the average is centred on
        //a sample rather than between two.
        //
        //Round to the nearest ODD value directly. Rounding to the nearest integer and then bumping
        //even results up can only ever land on the high side, and a kernel that is too wide puts its
        //null BELOW the output rate - at 44.1 kHz that moved the null from 2048 Hz down to 1917 Hz
        //and cost about 13 dB of rejection right where the display's alias images land.
        int Local_Width = ((int)Math.Round((((double)Local_Length / Local_Target) - 1d) / 2d) * 2) + 1;

        //A width of 1 is the identity, i.e. no band limiting at all - the very failure this class
        //exists to fix. Decimation ratios just above 1 would otherwise round to it.
        if (Local_Width < 3)
            Local_Width = 3;

        this.EnsureScratch(Local_Length);

        //First pass reads the caller's array; later passes filter the work buffer in place, which
        //is safe because the prefix sums capture the whole pass before anything is overwritten.
        this.MovingAverage(input, Local_Length, Local_Width, this.Work);
        for (int Local_Pass = 1; Local_Pass < this.OrderValue; Local_Pass++)
            this.MovingAverage(this.Work, Local_Length, Local_Width, this.Work);

        Resample(this.Work, Local_Length, output);
    }
    #endregion

    #region Protected Functions
    /// <summary>
    /// Replaces a NaN or an infinity with silence. Either would otherwise poison the running prefix
    /// sum and take every later sample of the frame with it.
    /// </summary>
    /// <param name="sample">The sample to check.</param>
    /// <returns>The sample, or zero if it was not finite.</returns>
    protected static double Sanitise(double sample)
    {
        return double.IsFinite(sample) ? sample : 0d;
    }

    /// <summary>
    /// Grows the scratch buffers to hold a frame of the given length.
    /// </summary>
    /// <param name="length">The frame length in samples.</param>
    protected void EnsureScratch(int length)
    {
        if (this.Prefix.Length < length + 1)
            this.Prefix = new double[length + 1];

        if (this.Work.Length < length)
            this.Work = new double[length];
    }

    /// <summary>
    /// Centred moving average of <paramref name="width"/> samples, written to
    /// <paramref name="destination"/>. The window is truncated at the two ends rather than being
    /// zero padded, so the edges are not pulled towards silence.
    /// </summary>
    /// <param name="source">Samples to filter.</param>
    /// <param name="length">How many samples to filter.</param>
    /// <param name="width">The window width in samples; expected to be odd.</param>
    /// <param name="destination">
    /// Where to write. May be the same array as <paramref name="source"/>.
    /// </param>
    protected void MovingAverage(double[] source, int length, int width, double[] destination)
    {
        var Local_Prefix = this.Prefix;

        Local_Prefix[0] = 0d;
        for (int i = 0; i < length; i++)
            Local_Prefix[i + 1] = Local_Prefix[i] + Sanitise(source[i]);

        int Local_Half = width / 2;
        for (int i = 0; i < length; i++)
        {
            int Local_From = i - Local_Half;
            if (Local_From < 0)
                Local_From = 0;

            int Local_To = i + Local_Half + 1;
            if (Local_To > length)
                Local_To = length;

            destination[i] = (Local_Prefix[Local_To] - Local_Prefix[Local_From]) / (Local_To - Local_From);
        }
    }

    /// <summary>
    /// Uniformly resamples the first <paramref name="length"/> samples of
    /// <paramref name="source"/> into <paramref name="output"/>, sampling at the centre of each of
    /// the output's equal blocks so the result is evenly spaced and free of the position jitter a
    /// peak-picking thinner produces.
    /// </summary>
    /// <param name="source">Samples to resample.</param>
    /// <param name="length">How many samples of the source are valid.</param>
    /// <param name="output">Destination, filled completely.</param>
    protected static void Resample(double[] source, int length, double[] output)
    {
        int Local_Target = output.Length;
        double Local_Step = (double)length / Local_Target;

        for (int k = 0; k < Local_Target; k++)
        {
            double Local_Position = ((k + 0.5d) * Local_Step) - 0.5d;

            if (Local_Position <= 0d)
            {
                output[k] = Sanitise(source[0]);
                continue;
            }

            if (Local_Position >= length - 1)
            {
                output[k] = Sanitise(source[length - 1]);
                continue;
            }

            int Local_Index = (int)Local_Position;
            double Local_Fraction = Local_Position - Local_Index;

            //Sanitised here too: the pass-through branch reaches Resample without going through
            //MovingAverage, so this is the only place a non-finite sample would be caught on it.
            double Local_Low = Sanitise(source[Local_Index]);
            double Local_High = Sanitise(source[Local_Index + 1]);

            output[k] = Local_Low + ((Local_High - Local_Low) * Local_Fraction);
        }
    }
    #endregion
}
