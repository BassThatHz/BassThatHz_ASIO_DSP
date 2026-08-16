#nullable enable

namespace BassThatHz_ASIO_DSP_Processor;

#region Usings
using System;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
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
public class AntiDC : IFilter
{
    #region Public Properties
    protected int _MaxConsecutiveDCSamples = 42;
    public int MaxConsecutiveDCSamples
    {
        get { return this._MaxConsecutiveDCSamples; }
        set { this._MaxConsecutiveDCSamples = Math.Max(1, value); }
    }

    protected int _MaxClipEventsPerDuration = 1;
    public int MaxClipEventsPerDuration
    {
        get { return this._MaxClipEventsPerDuration; }
        set { this._MaxClipEventsPerDuration = Math.Max(1, value); }
    }
    #endregion

    #region Variables
    public double Clip_Threshold = 0.9999d;
    public double DC_Threshold = 1E-05d;
    public TimeSpan DetectionDuration = TimeSpan.FromMilliseconds(1);

    protected int ClipEventsPerDurationDetected = 0;
    protected DateTime TimeOfLastClipEvent;

    protected int ConsecutiveDCEventsDetected = 0;
    protected double PreviousInputValue = 0;
    protected bool IsPreviousInputIdentical = false;
    protected bool WasPreviousInputIdentical = false;

    /// <summary>
    /// Timestamp of the last ClipEvent DISPATCH (not the last clip DETECTION - that is
    /// TimeOfLastClipEvent). Every read and every write of this field MUST use the same clock;
    /// see ClipEventThrottleInterval and ReportClipEvents.
    /// </summary>
    protected DateTime TimeOfLastClipEventRaised;
    protected bool IsOutputMuted = false;

    /// <summary>
    /// Minimum interval between two ClipEvent dispatches.
    /// Hoisted to a static readonly field so the per-sample detection path never re-materializes
    /// the TimeSpan, and so the two call sites cannot drift to different windows.
    /// It is non-public and init-only, so the ExtendedXmlSerializer never persists it.
    /// </summary>
    protected static readonly TimeSpan ClipEventThrottleInterval = TimeSpan.FromMilliseconds(1000);
    #endregion

    #region Public Custom Events
    public class ClippedInfoArgs
    {
        public int ClippedSamples = 0;
        public int ClippedEvents = 0;
    }
    public event EventHandler<ClippedInfoArgs>? OutputMutedEvent;
    public event EventHandler<ClippedInfoArgs>? ClipEvent;
    #endregion

    #region Public Functions

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public double[] Transform(double[] input, DSP_Stream currentStream)
    {
        // Use an in-place span-based implementation to avoid allocations
        try
        {
            TransformInPlace(input, currentStream);
            return input;
        }
        catch (Exception)
        {
            // swallow and return input to preserve previous behavior
            return input;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public void TransformInPlace(Span<double> buffer, DSP_Stream currentStream)
    {
        // Fast-path: if already muted, zero buffer and return
        if (this.IsOutputMuted)
        {
            buffer.Fill(0);
            return;
        }

        // Cache state/thresholds to locals to reduce field traffic
        double clipThreshold = this.Clip_Threshold;
        double dcThreshold = this.DC_Threshold;
        int maxConsec = this._MaxConsecutiveDCSamples;
        int maxClipEvents = this._MaxClipEventsPerDuration;
        TimeSpan detectionDuration = this.DetectionDuration;

        int consecDetected = this.ConsecutiveDCEventsDetected;
        int clipEventsDetected = this.ClipEventsPerDurationDetected;

        double prevValue = this.PreviousInputValue;
        bool wasPrevIdentical = this.WasPreviousInputIdentical;

        DateTime lastClipEvent = this.TimeOfLastClipEvent;

        for (int i = 0; i < buffer.Length; i++)
        {
            double sample = buffer[i];

            // DC detection (only when sample magnitude above DC threshold)
            if (sample >= dcThreshold || sample <= -dcThreshold)
            {
                bool isPrevIdentical = prevValue == sample;

                if (wasPrevIdentical && !isPrevIdentical)
                {
                    // Reset consecutive DC and report previous clip events.
                    //
                    // FIXED DEFECT (two halves, both making this fire once PER SAMPLE on the
                    // real-time audio thread, allocating an args object + a closure + a Task each
                    // time - the exact per-callback allocation this filter must never do):
                    //   1) The throttle was CHECKED here but never UPDATED, so once the 1000 ms
                    //      window opened it never closed again.
                    //   2) The check read DateTime.UtcNow while the only writer of
                    //      TimeOfLastClipEventRaised (ReportClipEvents) stamped local DateTime.Now.
                    //      For any zone behind UTC the difference was inflated by the UTC offset,
                    //      so the condition was unconditionally true.
                    // Both sites now funnel through ReportClipEvents, which checks AND updates the
                    // throttle using DateTime.UtcNow for every read and every write, so the two
                    // cannot diverge again.
                    this.ReportClipEvents();
                    consecDetected = 0;
                }

                if (isPrevIdentical)
                {
                    consecDetected++;
                    if (consecDetected >= maxConsec)
                    {
                        this.IsOutputMuted = true;
                        // Zero remaining buffer and raise output muted event asynchronously.
                        // No throttle is needed: IsOutputMuted LATCHES, this breaks out of the
                        // loop, and every later call takes the muted fast-path, so the event can
                        // fire at most once per ResetDetection.
                        for (int j = i; j < buffer.Length; j++)
                            buffer[j] = 0;
                        this.RaiseOutputMutedEvent();
                        break;
                    }
                }

                wasPrevIdentical = isPrevIdentical;
            }

            // Clip detection
            if (sample >= clipThreshold || sample <= -clipThreshold)
            {
                var now = DateTime.UtcNow;
                if (now - lastClipEvent <= detectionDuration)
                {
                    clipEventsDetected++;
                    if (clipEventsDetected >= maxClipEvents)
                    {
                        this.IsOutputMuted = true;
                        for (int j = i; j < buffer.Length; j++)
                            buffer[j] = 0;
                        //Same latch-once reasoning as the DC branch above - no throttle required.
                        this.RaiseOutputMutedEvent();
                        break;
                    }
                }
                else
                {
                    clipEventsDetected = 0;
                }
                lastClipEvent = now;
            }

            prevValue = sample;
        }

        // write back locals
        this.ConsecutiveDCEventsDetected = consecDetected;
        this.ClipEventsPerDurationDetected = clipEventsDetected;
        this.PreviousInputValue = prevValue;
        this.WasPreviousInputIdentical = wasPrevIdentical;
        this.TimeOfLastClipEvent = lastClipEvent;
    }

    public void ResetDetection()
    {
        this.ConsecutiveDCEventsDetected = 0;
        this.ClipEventsPerDurationDetected = 0;
        this.IsOutputMuted = false;
    }

    public void ResetSampleRate(int sampleRate)
    {
        //Non Applicable
    }

    public void ApplySettings()
    {
        //Non Applicable
    }
    #endregion

    #region Protected Functions
    protected ClippedInfoArgs Get_ClippedInfoArgs()
    {
        return new ClippedInfoArgs()
        {
            ClippedSamples = this.ConsecutiveDCEventsDetected,
            ClippedEvents = this.ClipEventsPerDurationDetected
        };
    }

    /// <summary>
    /// Dispatches OutputMutedEvent off the audio thread.
    /// Deliberately NOT throttled: both call sites set the IsOutputMuted latch and break out of
    /// the sample loop, and TransformInPlace returns immediately while that latch is set, so this
    /// can be reached at most once between ResetDetection calls. A throttle here would only be
    /// able to SUPPRESS the single legitimate notification.
    /// </summary>
    protected void RaiseOutputMutedEvent()
    {
        var Local_Args = this.Get_ClippedInfoArgs();
        _ = Task.Run(() => this.OutputMutedEvent?.Invoke(this, Local_Args));
    }

    /// <summary>
    /// The single throttled ClipEvent dispatch point for the whole filter.
    /// Reads AND writes TimeOfLastClipEventRaised with the SAME clock (DateTime.UtcNow). UTC is
    /// used rather than DateTime.Now because local time can jump backwards at a DST boundary,
    /// which would stall the throttle for an hour.
    /// </summary>
    protected void ReportClipEvents()
    {
        var Local_Now = DateTime.UtcNow;
        if (Local_Now - this.TimeOfLastClipEventRaised > ClipEventThrottleInterval)
        {
            //Stamping BEFORE dispatch is what closes the window; omitting it made the caller
            //allocate a Task per qualifying sample on the real-time audio thread.
            this.TimeOfLastClipEventRaised = Local_Now;
            var Local_Args = this.Get_ClippedInfoArgs();
            _ = Task.Run(() => this.ClipEvent?.Invoke(this, Local_Args));
        }
    }

    protected void DC_Detection(double input)
    {
        if (input >= this.DC_Threshold || input <= -this.DC_Threshold)
        {
            this.IsPreviousInputIdentical = this.PreviousInputValue == input;

            if (this.WasPreviousInputIdentical && !this.IsPreviousInputIdentical)
            {
                this.ReportClipEvents();
                this.ConsecutiveDCEventsDetected = 0;
            }

            if (this.IsPreviousInputIdentical)
            {
                this.ConsecutiveDCEventsDetected++;
                this.IsOutputMuted |= this.ConsecutiveDCEventsDetected >= this._MaxConsecutiveDCSamples;
            }

            this.WasPreviousInputIdentical = this.IsPreviousInputIdentical;
        }
    }

    /// <summary>
    /// Per-sample clip detector.
    /// Uses DateTime.UtcNow so that TimeOfLastClipEvent is stamped with the same clock
    /// TransformInPlace uses for the same field - the identical cross-clock divergence that broke
    /// the TimeOfLastClipEventRaised throttle.
    /// </summary>
    protected void ClipDetection(double input)
    {
        if (input >= this.Clip_Threshold || input <= -this.Clip_Threshold)
        {
            var Local_Now = DateTime.UtcNow;
            if (Local_Now - this.TimeOfLastClipEvent <= this.DetectionDuration)
            {
                this.ClipEventsPerDurationDetected++;
                this.IsOutputMuted |= this.ClipEventsPerDurationDetected >= this._MaxClipEventsPerDuration;
            }
            else
            {
                this.ClipEventsPerDurationDetected = 0;
            }
            this.TimeOfLastClipEvent = Local_Now;
        }
    }
    #endregion

    #region IFilter Interface
    public bool FilterEnabled { get; set; }

    public IFilter GetFilter => this;

    public FilterTypes FilterType { get; } = FilterTypes.Anti_DC;
    public FilterProcessingTypes FilterProcessingType { get; } = FilterProcessingTypes.WholeBlock;

    public IFilter DeepClone()
    {
        return CommonFunctions.DeepClone(this);
    }
    #endregion
}