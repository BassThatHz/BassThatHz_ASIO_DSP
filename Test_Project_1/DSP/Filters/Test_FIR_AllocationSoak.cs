#nullable enable

namespace Test_Project_1;

#region Usings
using BassThatHz_ASIO_DSP_Processor;
using DSPLib;
using System;
using System.Numerics;
using Test_Project_1.TestHelpers;
#endregion

/// <summary>
/// ALLOCATION SOAK for the FFT-based convolution filters (FIR / ULF_FIR).
///
/// <para>
/// Test_ASIO_Engine_Soak covers the engine plumbing but its chain uses only BiQuad + Limiter, so it
/// never touches DSPLib.FFT. FIR and ULF_FIR are the two filters that run a full forward + inverse
/// transform on EVERY buffer callback; at the production default FFTSize of 8192 the allocating
/// Perform_FFT / Perform_IFFT pair costs a Complex[8192] (131,072 B) plus a double[8192]
/// (65,536 B) = ~192 KiB of garbage per callback per filter instance. At 48 kHz with a 256 sample
/// buffer that is ~187 callbacks/s, i.e. ~36 MB/s of gen0 churn from a single FIR - exactly the
/// kind of pressure that produces GC pauses and audio dropouts.
/// </para>
/// <para>
/// These tests pin the steady state to near-zero allocation and record, in the same run, what the
/// allocating API would have cost, so the improvement is measured rather than asserted from
/// memory. They deliberately measure only allocation - the bit-exactness of the output is the job
/// of Test_FIR_Characterization / Test_ULF_FIR_Characterization.
/// </para>
/// </summary>
[TestClass]
public class Test_FIR_AllocationSoak
{
    #region Constants
    private const int FFTSize = 8192;
    private const int BlockSize = 256;
    private const int WarmupBlocks = 64;
    private const int MeasuredBlocks = 512;

    /// <summary>
    /// Generous ceiling: the steady state should be exactly 0 B, but a few bytes of unavoidable
    /// churn must not make this brittle. The pre-optimization cost was ~196,608 B per block.
    /// </summary>
    private const double AllocationCeilingBytesPerBlock = 512d;
    #endregion

    #region Helpers
    private static double[] MakeTaps(int count)
    {
        var Local_Taps = new double[count];
        for (int i = 0; i < count; i++)
            Local_Taps[i] = 1.0 / (i + 1);
        return Local_Taps;
    }

    /// <summary>Runs the block loop and returns bytes allocated per block on this thread.</summary>
    private static double MeasurePerBlock(Action<double[]> processOneBlock)
    {
        var Local_Block = DspCharacterization.Noise(BlockSize, 4242UL);

        for (int i = 0; i < WarmupBlocks; i++)
            processOneBlock(Local_Block);

        long Local_Before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < MeasuredBlocks; i++)
            processOneBlock(Local_Block);
        long Local_After = GC.GetAllocatedBytesForCurrentThread();

        return (Local_After - Local_Before) / (double)MeasuredBlocks;
    }
    #endregion

    #region FIR

    /// <summary>
    /// A FIR at the production default FFTSize must not allocate per buffer callback.
    /// </summary>
    [TestMethod]
    public void Soak_FIR_SteadyState_DoesNotAllocatePerBlock()
    {
        var Local_Stream = new DSP_Stream();
        var Local_Filter = new FIR { FFTSize = FFTSize, FilterEnabled = true };
        Local_Filter.SetTaps(MakeTaps(1024));

        double Local_PerBlock = MeasurePerBlock(
            b => Local_Filter.Transform(DspCharacterization.Copy(b), Local_Stream));

        //Copy(b) itself allocates a double[256] = 2,048 B + 24 B header, so subtract it out.
        Local_PerBlock -= BlockSize * sizeof(double) + 24;

        System.Diagnostics.Trace.WriteLine(
            "FIR.Transform(FFTSize=" + FFTSize + ", block=" + BlockSize + ") steady state = "
            + Local_PerBlock.ToString("F1") + " B/block");

        Assert.IsTrue(Local_PerBlock < AllocationCeilingBytesPerBlock,
            "FIR.Transform allocated " + Local_PerBlock.ToString("F1")
            + " bytes per block (ceiling " + AllocationCeilingBytesPerBlock
            + "). Per-callback allocation is back on the real-time path.");
    }

    #endregion

    #region ULF_FIR

    /// <summary>
    /// Same guarantee for ULF_FIR, which runs the identical transform pair.
    /// </summary>
    [TestMethod]
    public void Soak_ULF_FIR_SteadyState_DoesNotAllocatePerBlock()
    {
        var Local_SavedInfo = Program.DSP_Info;
        try
        {
            Program.DSP_Info = new DSP_Info { InSampleRate = 48000 };

            var Local_Stream = new DSP_Stream();
            var Local_Filter = new ULF_FIR { FFTSize = FFTSize, TapsSampleRate = 960, FilterEnabled = true };
            Local_Filter.SetTaps(MakeTaps(1024));

            double Local_PerBlock = MeasurePerBlock(
                b => Local_Filter.Transform(DspCharacterization.Copy(b), Local_Stream));

            Local_PerBlock -= BlockSize * sizeof(double) + 24;

            System.Diagnostics.Trace.WriteLine(
                "ULF_FIR.Transform(FFTSize=" + FFTSize + ", block=" + BlockSize + ") steady state = "
                + Local_PerBlock.ToString("F1") + " B/block");

            Assert.IsTrue(Local_PerBlock < AllocationCeilingBytesPerBlock,
                "ULF_FIR.Transform allocated " + Local_PerBlock.ToString("F1")
                + " bytes per block (ceiling " + AllocationCeilingBytesPerBlock + ").");
        }
        finally
        {
            Program.DSP_Info = Local_SavedInfo;
        }
    }

    #endregion

    #region Baseline Reference

    /// <summary>
    /// Records what the ALLOCATING Perform_FFT / Perform_IFFT pair - the API FIR.Transform used
    /// before the Perform_*_Into overloads existed - costs per block, and asserts the reusable-
    /// buffer form is dramatically cheaper. This is the measured before/after, taken in one run so
    /// it cannot drift against a remembered number.
    /// </summary>
    [TestMethod]
    public void Reference_AllocatingFFTApi_IsOrdersOfMagnitudeMoreExpensiveThanTheIntoForm()
    {
        var Local_Fft = new FFT(FFTSize, 0);
        var Local_TimeSeries = new double[FFTSize];
        for (int i = 0; i < FFTSize; i++)
            Local_TimeSeries[i] = (i % 97) / 97.0 - 0.5;

        //Old shape: allocate a fresh spectrum and a fresh time series on every block.
        double Local_Allocating = MeasurePerBlock(_ =>
        {
            Complex[] Local_Spectrum = Local_Fft.Perform_FFT(Local_TimeSeries, false);
            _ = Local_Fft.Perform_IFFT(Local_Spectrum, false);
        });

        //New shape: reuse two per-instance buffers.
        var Local_SpectrumBuffer = new Complex[FFTSize];
        var Local_ResultBuffer = new double[FFTSize];
        double Local_Into = MeasurePerBlock(_ =>
        {
            Local_Fft.Perform_FFT_Into(Local_TimeSeries, Local_SpectrumBuffer, false);
            _ = Local_Fft.Perform_IFFT_Into(Local_SpectrumBuffer, Local_ResultBuffer, false);
        });

        System.Diagnostics.Trace.WriteLine(
            "FFT(" + FFTSize + ") per block: allocating API = " + Local_Allocating.ToString("F0")
            + " B, Perform_*_Into = " + Local_Into.ToString("F0") + " B");

        Assert.IsTrue(Local_Allocating > 190000d,
            "Expected the allocating API to cost ~196,608 B per block, measured "
            + Local_Allocating.ToString("F0"));
        Assert.IsTrue(Local_Into < 512d,
            "Perform_*_Into must not allocate; measured " + Local_Into.ToString("F0") + " B per block");
    }

    /// <summary>
    /// The reusable-buffer overloads must be BIT-EXACT against the allocating ones. This is the
    /// direct guard on the optimization: same input, same FFT instance state, same result.
    /// </summary>
    [TestMethod]
    public void Contract_IntoOverloads_AreBitExactAgainstTheAllocatingOverloads()
    {
        var Local_Input = DspCharacterization.Noise(64, 991UL);
        var Local_Window = DSP.Window.Coefficients(DSP.Window.Type.Hann, 64);

        foreach (bool Local_Scale in new[] { false, true })
        {
            var Local_Expected = new FFT(64, 0).Perform_FFT(DspCharacterization.Copy(Local_Input), Local_Scale);
            var Local_Actual = new FFT(64, 0).Perform_FFT_Into(
                DspCharacterization.Copy(Local_Input), new Complex[64], Local_Scale);
            DspCharacterization.AssertExact(Local_Expected, Local_Actual,
                "Perform_FFT_Into vs Perform_FFT, scale=" + Local_Scale);

            var Local_ExpectedWin = new FFT(64, 0).Perform_FFT(DspCharacterization.Copy(Local_Input), Local_Window, Local_Scale);
            var Local_ActualWin = new FFT(64, 0).Perform_FFT_Into(
                DspCharacterization.Copy(Local_Input), Local_Window, new Complex[64], Local_Scale);
            DspCharacterization.AssertExact(Local_ExpectedWin, Local_ActualWin,
                "Windowed Perform_FFT_Into vs Perform_FFT, scale=" + Local_Scale);

            var Local_ExpectedIfft = new FFT(64, 0).Perform_IFFT(Local_Expected, Local_Scale);
            var Local_ActualIfft = new FFT(64, 0).Perform_IFFT_Into(Local_Expected, new double[64], Local_Scale);
            DspCharacterization.AssertExact(Local_ExpectedIfft, Local_ActualIfft,
                "Perform_IFFT_Into vs Perform_IFFT, scale=" + Local_Scale);
        }
    }

    /// <summary>
    /// A DIRTY reusable buffer must give the same answer as a freshly zeroed one - the unswizzle
    /// step writes every slot, so nothing can leak between calls.
    /// </summary>
    [TestMethod]
    public void Contract_IntoOverloads_IgnoreStaleBufferContents()
    {
        var Local_Input = DspCharacterization.Noise(64, 4711UL);

        var Local_Clean = new FFT(64, 0).Perform_FFT_Into(
            DspCharacterization.Copy(Local_Input), new Complex[64], false);

        var Local_Dirty = new Complex[64];
        for (int i = 0; i < 64; i++)
            Local_Dirty[i] = new Complex(double.NaN, 12345.678d);
        var Local_FromDirty = new FFT(64, 0).Perform_FFT_Into(
            DspCharacterization.Copy(Local_Input), Local_Dirty, false);

        DspCharacterization.AssertExact(Local_Clean, Local_FromDirty,
            "A reused spectrum buffer must be fully overwritten");

        var Local_CleanIfft = new FFT(64, 0).Perform_IFFT_Into(Local_Clean, new double[64], false);
        var Local_DirtyOut = new double[64];
        for (int i = 0; i < 64; i++)
            Local_DirtyOut[i] = double.NaN;
        var Local_FromDirtyIfft = new FFT(64, 0).Perform_IFFT_Into(Local_Clean, Local_DirtyOut, false);

        DspCharacterization.AssertExact(Local_CleanIfft, Local_FromDirtyIfft,
            "A reused time-series buffer must be fully overwritten");
    }

    /// <summary>
    /// An undersized destination is rejected rather than silently truncating the spectrum.
    /// </summary>
    [TestMethod]
    public void Contract_IntoOverloads_RejectAnUndersizedDestination()
    {
        Assert.ThrowsExactly<InvalidOperationException>(
            () => new FFT(64, 0).Perform_FFT_Into(new double[64], new Complex[63], false));
        Assert.ThrowsExactly<InvalidOperationException>(
            () => new FFT(64, 0).Perform_IFFT_Into(new Complex[64], new double[63], false));
    }

    #endregion
}
