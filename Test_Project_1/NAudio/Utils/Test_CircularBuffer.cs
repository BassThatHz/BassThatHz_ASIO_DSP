#nullable enable

namespace Test_Project_1;

#region Usings
using NAudio.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
#endregion

/// <summary>
/// GUARD SUITE for NAudio\Utils\CircularBuffer.cs.
///
/// This buffer is the sample transport between the ASIO callback (producer) and the RTA analyser
/// (consumer) in FormRTA. Two invariants matter for a REAL-TIME analyser and both were broken:
///
///  1. A read must consume exactly the samples it returns. The original Read() moved readPosition
///     but never decremented Count, so the caller's Read(frame) + Advance(hop) overlap loop pushed
///     readPosition forward by frame + hop per frame while Count only dropped by hop. The read
///     pointer therefore ran away from the write pointer and swept the whole storage, handing the
///     FFT never-written zeros and seconds-old audio.
///
///  2. When the storage is full the OLDEST samples must be discarded, not the newest. The original
///     Write() clamped the incoming count to the free space, so a full buffer silently dropped all
///     new audio and the display froze on whatever was already stored until the consumer drained
///     it - the "it takes ten seconds to drop out" symptom.
/// </summary>
[TestClass]
public class Test_CircularBuffer
{
    #region Helpers

    /// <summary>
    /// Builds an ascending ramp so every sample carries its own absolute stream position and any
    /// mis-ordered, duplicated or never-written sample is immediately identifiable.
    /// </summary>
    private static double[] Ramp(int start, int count)
    {
        var Local_Result = new double[count];
        for (int i = 0; i < count; i++)
            Local_Result[i] = start + i;
        return Local_Result;
    }

    #endregion

    #region Construction

    [TestMethod]
    public void MaxLength_ReportsTheRequestedCapacity()
    {
        var Local_Buffer = new CircularBuffer(64);

        Assert.AreEqual(64, Local_Buffer.MaxLength);
        Assert.AreEqual(0, Local_Buffer.Count);
    }

    #endregion

    #region Read Consumes What It Returns

    /// <summary>
    /// DEFECT: Read() advanced readPosition but left Count untouched, so the buffer claimed to
    /// still hold samples it had already handed out.
    /// </summary>
    [TestMethod]
    public void Read_ConsumesExactlyWhatItReturns()
    {
        var Local_Buffer = new CircularBuffer(8);
        _ = Local_Buffer.Write(Ramp(0, 8), 0, 8);

        var Local_Destination = new double[8];
        int Local_Read = Local_Buffer.Read(Local_Destination, 0, 3);

        Assert.AreEqual(3, Local_Read, "Asked for 3 available samples");
        CollectionAssert.AreEqual(Ramp(0, 3), Local_Destination.Take(3).ToArray());
        Assert.AreEqual(5, Local_Buffer.Count, "Count must drop by the number of samples consumed");
    }

    /// <summary>
    /// Draining in chunks must yield every stored sample exactly once and then leave the buffer
    /// empty. With the un-consumed Read() the loop wrapped and re-served old samples forever.
    /// </summary>
    [TestMethod]
    public void Read_DrainsTheBufferExactlyOnce()
    {
        var Local_Buffer = new CircularBuffer(9);
        _ = Local_Buffer.Write(Ramp(0, 9), 0, 9);

        var Local_Drained = new List<double>();
        var Local_Chunk = new double[4];

        //Bounded so a buffer that never empties fails the assertion instead of hanging the suite.
        for (int i = 0; i < 10 && Local_Buffer.Count > 0; i++)
        {
            int Local_Read = Local_Buffer.Read(Local_Chunk, 0, 4);
            Local_Drained.AddRange(Local_Chunk.Take(Local_Read));
        }

        CollectionAssert.AreEqual(Ramp(0, 9), Local_Drained.ToArray(), "Every sample exactly once");
        Assert.AreEqual(0, Local_Buffer.Count, "The buffer is empty once everything is drained");
    }

    [TestMethod]
    public void Read_ClampsToTheSamplesActuallyAvailable()
    {
        var Local_Buffer = new CircularBuffer(16);
        _ = Local_Buffer.Write(Ramp(0, 5), 0, 5);

        var Local_Destination = new double[16];
        int Local_Read = Local_Buffer.Read(Local_Destination, 0, 16);

        Assert.AreEqual(5, Local_Read);
        Assert.AreEqual(0, Local_Buffer.Count);
    }

    [TestMethod]
    public void Read_WrapsAroundTheEndOfTheStorage()
    {
        var Local_Buffer = new CircularBuffer(8);
        _ = Local_Buffer.Write(Ramp(0, 8), 0, 8);
        _ = Local_Buffer.Read(new double[6], 0, 6);
        _ = Local_Buffer.Write(Ramp(8, 6), 0, 6);

        var Local_Destination = new double[8];
        int Local_Read = Local_Buffer.Read(Local_Destination, 0, 8);

        Assert.AreEqual(8, Local_Read);
        CollectionAssert.AreEqual(Ramp(6, 8), Local_Destination, "6..13 spanning the wrap point");
    }

    #endregion

    #region Overrun Keeps The Newest Audio

    /// <summary>
    /// DEFECT: a full buffer dropped the INCOMING block. For a live analyser the newest block is
    /// the only one worth keeping, so an overrun must discard the oldest samples instead.
    /// </summary>
    [TestMethod]
    public void Write_WhenFull_KeepsTheNewestSamples()
    {
        var Local_Buffer = new CircularBuffer(4);
        _ = Local_Buffer.Write(Ramp(0, 4), 0, 4);

        int Local_Written = Local_Buffer.Write(Ramp(4, 2), 0, 2);

        Assert.AreEqual(2, Local_Written, "The whole incoming block is accepted");
        Assert.AreEqual(4, Local_Buffer.Count, "Count is still capped at the capacity");

        var Local_Destination = new double[4];
        _ = Local_Buffer.Read(Local_Destination, 0, 4);
        CollectionAssert.AreEqual(Ramp(2, 4), Local_Destination, "The two oldest samples were dropped");
    }

    [TestMethod]
    public void Write_LongerThanTheCapacity_KeepsTheNewestSamples()
    {
        var Local_Buffer = new CircularBuffer(4);

        int Local_Written = Local_Buffer.Write(Ramp(0, 10), 0, 10);

        Assert.AreEqual(4, Local_Written, "Only a capacity's worth can be retained");
        Assert.AreEqual(4, Local_Buffer.Count);

        var Local_Destination = new double[4];
        _ = Local_Buffer.Read(Local_Destination, 0, 4);
        CollectionAssert.AreEqual(Ramp(6, 4), Local_Destination, "The tail of the block survives");
    }

    /// <summary>
    /// The RTA symptom in miniature: a producer that outruns the consumer must leave the LATEST
    /// audio in the buffer. Before the fix the buffer stalled on the first capacity's worth of
    /// samples, which is why the chart lagged the input by the whole buffer length.
    /// </summary>
    [TestMethod]
    public void ProducerOutrunningTheConsumer_LeavesTheLatestAudioBuffered()
    {
        const int Capacity = 1000;
        const int BlockSize = 100;
        const int BlockCount = 100;

        var Local_Buffer = new CircularBuffer(Capacity);

        int Local_Produced = 0;
        for (int Local_Block = 0; Local_Block < BlockCount; Local_Block++)
        {
            _ = Local_Buffer.Write(Ramp(Local_Produced, BlockSize), 0, BlockSize);
            Local_Produced += BlockSize;
        }

        Assert.AreEqual(Capacity, Local_Buffer.Count);

        var Local_Destination = new double[Capacity];
        _ = Local_Buffer.Read(Local_Destination, 0, Capacity);

        Assert.AreEqual(Local_Produced - Capacity, Local_Destination[0], "Oldest retained sample");
        Assert.AreEqual(Local_Produced - 1, Local_Destination[Capacity - 1], "Newest produced sample");
    }

    [TestMethod]
    public void Count_NeverExceedsTheCapacity()
    {
        var Local_Buffer = new CircularBuffer(16);

        for (int i = 0; i < 20; i++)
        {
            _ = Local_Buffer.Write(Ramp(i * 5, 5), 0, 5);
            Assert.IsTrue(Local_Buffer.Count <= Local_Buffer.MaxLength,
                $"Count {Local_Buffer.Count} exceeded capacity {Local_Buffer.MaxLength}");
        }
    }

    #endregion

    #region Advance

    [TestMethod]
    public void Advance_DiscardsTheOldestSamples()
    {
        var Local_Buffer = new CircularBuffer(8);
        _ = Local_Buffer.Write(Ramp(0, 8), 0, 8);

        Local_Buffer.Advance(3);

        Assert.AreEqual(5, Local_Buffer.Count);

        var Local_Destination = new double[5];
        _ = Local_Buffer.Read(Local_Destination, 0, 5);
        CollectionAssert.AreEqual(Ramp(3, 5), Local_Destination);
    }

    [TestMethod]
    public void Advance_BeyondTheStoredCount_EmptiesTheBuffer()
    {
        var Local_Buffer = new CircularBuffer(8);
        _ = Local_Buffer.Write(Ramp(0, 4), 0, 4);

        Local_Buffer.Advance(99);

        Assert.AreEqual(0, Local_Buffer.Count);
    }

    /// <summary>
    /// DEFECT: Advance(-1) INCREASED Count (Count -= -1) and drove readPosition negative, which
    /// made the very next Read throw out of Array.Copy.
    /// </summary>
    [TestMethod]
    public void Advance_WithANonPositiveCount_IsIgnored()
    {
        var Local_Buffer = new CircularBuffer(8);
        _ = Local_Buffer.Write(Ramp(0, 4), 0, 4);

        Local_Buffer.Advance(0);
        Local_Buffer.Advance(-5);

        Assert.AreEqual(4, Local_Buffer.Count, "A non-positive advance is a no-op");

        var Local_Destination = new double[4];
        int Local_Read = Local_Buffer.Read(Local_Destination, 0, 4);

        Assert.AreEqual(4, Local_Read);
        CollectionAssert.AreEqual(Ramp(0, 4), Local_Destination);
    }

    [TestMethod]
    public void Reset_EmptiesTheBufferAndRestartsTheStream()
    {
        var Local_Buffer = new CircularBuffer(8);
        _ = Local_Buffer.Write(Ramp(0, 6), 0, 6);

        Local_Buffer.Reset();

        Assert.AreEqual(0, Local_Buffer.Count);

        _ = Local_Buffer.Write(Ramp(100, 3), 0, 3);
        var Local_Destination = new double[3];
        _ = Local_Buffer.Read(Local_Destination, 0, 3);
        CollectionAssert.AreEqual(Ramp(100, 3), Local_Destination);
    }

    #endregion

    #region Peek Does Not Consume

    [TestMethod]
    public void Peek_LeavesTheSamplesInTheBuffer()
    {
        var Local_Buffer = new CircularBuffer(8);
        _ = Local_Buffer.Write(Ramp(0, 8), 0, 8);

        var Local_First = new double[8];
        var Local_Second = new double[8];
        int Local_Peeked = Local_Buffer.Peek(Local_First, 0, 8);
        _ = Local_Buffer.Peek(Local_Second, 0, 8);

        Assert.AreEqual(8, Local_Peeked);
        Assert.AreEqual(8, Local_Buffer.Count, "Peek is non-destructive");
        CollectionAssert.AreEqual(Local_First, Local_Second, "Two peeks see the same audio");
    }

    [TestMethod]
    public void Peek_ClampsToTheSamplesActuallyAvailable()
    {
        var Local_Buffer = new CircularBuffer(16);
        _ = Local_Buffer.Write(Ramp(0, 3), 0, 3);

        int Local_Peeked = Local_Buffer.Peek(new double[16], 0, 16);

        Assert.AreEqual(3, Local_Peeked);
        Assert.AreEqual(3, Local_Buffer.Count);
    }

    [TestMethod]
    public void Peek_WrapsAroundTheEndOfTheStorage()
    {
        var Local_Buffer = new CircularBuffer(8);
        _ = Local_Buffer.Write(Ramp(0, 8), 0, 8);
        Local_Buffer.Advance(6);
        _ = Local_Buffer.Write(Ramp(8, 6), 0, 6);

        var Local_Destination = new double[8];
        int Local_Peeked = Local_Buffer.Peek(Local_Destination, 0, 8);

        Assert.AreEqual(8, Local_Peeked);
        CollectionAssert.AreEqual(Ramp(6, 8), Local_Destination, "6..13 spanning the wrap point");
    }

    #endregion

    #region The Overlapped Analysis Loop

    /// <summary>
    /// The exact pattern the RTA plot timers use: analyse a whole frame, then step forward by the
    /// overlap hop only. Consecutive frames must be contiguous windows onto the SAME stream, with
    /// each frame starting exactly one hop after its predecessor.
    ///
    /// This is the regression test for the "zeroed out" symptom: before the fix Read() moved the
    /// read pointer by a whole frame AND Advance() moved it again by the hop, so the second frame
    /// started a frame + a hop later and landed on storage that had never been written.
    /// </summary>
    [TestMethod]
    public void OverlappedFrameLoop_ProducesContiguousFrames()
    {
        const int Capacity = 40;
        const int Frame = 10;
        const int Hop = 2;

        var Local_Buffer = new CircularBuffer(Capacity);
        var Local_Frame = new double[Frame];
        int Local_Produced = 0;

        //Prime with one full frame.
        _ = Local_Buffer.Write(Ramp(Local_Produced, Frame), 0, Frame);
        Local_Produced += Frame;

        for (int Local_Index = 0; Local_Index < 12; Local_Index++)
        {
            Assert.IsTrue(Local_Buffer.Count >= Frame, $"Frame {Local_Index}: a whole frame must be available");

            int Local_Peeked = Local_Buffer.Peek(Local_Frame, 0, Frame);
            Assert.AreEqual(Frame, Local_Peeked);

            int Local_Expected = Local_Index * Hop;
            CollectionAssert.AreEqual(Ramp(Local_Expected, Frame), Local_Frame,
                $"Frame {Local_Index} must be samples {Local_Expected}..{Local_Expected + Frame - 1}");

            Local_Buffer.Advance(Hop);

            //The producer supplies exactly one hop of fresh audio per analysed frame.
            _ = Local_Buffer.Write(Ramp(Local_Produced, Hop), 0, Hop);
            Local_Produced += Hop;
        }
    }

    /// <summary>
    /// With a two-second store, a one-second frame and a 100 ms hop, the newest sample the analyser
    /// ever sees must stay within roughly one frame of the newest sample produced. Before the fix
    /// the analysed audio drifted arbitrarily far behind - the ten-second start-up and ten-second
    /// decay the user reported.
    /// </summary>
    [TestMethod]
    public void RealTimeOverlapLoop_KeepsAnalysisLatencyToAboutOneFrame()
    {
        const int SampleRate = 1000;
        const int Capacity = SampleRate * 2;
        const int Frame = SampleRate;
        const int Hop = SampleRate / 10;
        const int BlockSize = 100;
        const int BlockCount = 300;

        var Local_Buffer = new CircularBuffer(Capacity);
        var Local_Frame = new double[Frame];

        int Local_Produced = 0;
        double Local_NewestAnalysed = -1;
        int Local_FramesAnalysed = 0;

        for (int Local_Block = 0; Local_Block < BlockCount; Local_Block++)
        {
            _ = Local_Buffer.Write(Ramp(Local_Produced, BlockSize), 0, BlockSize);
            Local_Produced += BlockSize;

            //The plot timer runs far faster than audio arrives, so it drains everything it can.
            while (Local_Buffer.Count >= Frame)
            {
                _ = Local_Buffer.Peek(Local_Frame, 0, Frame);
                Local_NewestAnalysed = Local_Frame[Frame - 1];
                Local_FramesAnalysed++;
                Local_Buffer.Advance(Hop);
            }
        }

        Assert.IsTrue(Local_FramesAnalysed > 0, "The analyser must have produced frames");

        double Local_Lag = (Local_Produced - 1) - Local_NewestAnalysed;
        Assert.IsTrue(Local_Lag <= Frame,
            $"The newest analysed sample lagged the newest produced sample by {Local_Lag}, " +
            $"which is more than the one-frame ({Frame} sample) analysis window");
    }

    /// <summary>
    /// A stalled consumer (the user ticks Pause, or the UI thread is busy) must not leave the
    /// analyser looking at ancient audio when it resumes: the buffer holds only the newest
    /// capacity's worth, so the next frame is at most a buffer old, never unbounded.
    /// </summary>
    [TestMethod]
    public void StalledConsumer_ResumesOnRecentAudio()
    {
        const int SampleRate = 1000;
        const int Capacity = SampleRate * 2;
        const int Frame = SampleRate;
        const int BlockSize = 100;

        var Local_Buffer = new CircularBuffer(Capacity);

        //Thirty seconds of audio arrive while nothing consumes it.
        int Local_Produced = 0;
        for (int Local_Block = 0; Local_Block < 300; Local_Block++)
        {
            _ = Local_Buffer.Write(Ramp(Local_Produced, BlockSize), 0, BlockSize);
            Local_Produced += BlockSize;
        }

        var Local_Frame = new double[Frame];
        int Local_Peeked = Local_Buffer.Peek(Local_Frame, 0, Frame);

        Assert.AreEqual(Frame, Local_Peeked);
        double Local_Lag = (Local_Produced - 1) - Local_Frame[Frame - 1];
        Assert.IsTrue(Local_Lag <= Capacity,
            $"Resumed on audio {Local_Lag} samples old, which exceeds the {Capacity} sample store");
    }

    #endregion

    #region Degenerate Sizes

    [TestMethod]
    public void ZeroLengthBuffer_AcceptsNothingAndNeverThrows()
    {
        var Local_Buffer = new CircularBuffer(0);

        Assert.AreEqual(0, Local_Buffer.Write(Ramp(0, 4), 0, 4));
        Assert.AreEqual(0, Local_Buffer.Count);
        Assert.AreEqual(0, Local_Buffer.Read(new double[4], 0, 4));

        Local_Buffer.Advance(4);
        Assert.AreEqual(0, Local_Buffer.Count);
    }

    #endregion
}
