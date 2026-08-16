namespace NAudio.Utils
{
    using System;

    /// <summary>
    /// A very basic circular buffer implementation.
    ///
    /// This is the sample transport between the ASIO callback (producer) and the RTA plot timers
    /// (consumer), so it is written for a REAL-TIME analyser: the newest audio always wins, and a
    /// read consumes exactly the samples it hands out.
    ///
    /// Overlapped analysis is expressed as <see cref="Peek"/> of a whole frame followed by
    /// <see cref="Advance"/> of the hop, which is why the non-destructive Peek exists separately
    /// from the destructive <see cref="Read"/>.
    /// </summary>
    public class CircularBuffer
    {
        #region Variables
        //The producer is the ASIO callback thread and the consumer is a Task.Run plot thread, so
        //every mutation of buffer/readPosition/writePosition/_Count is serialised. The lock is only
        //ever held for an Array.Copy of the requested block, never for the FFT itself.
        protected readonly object SyncRoot = new();

        protected double[] buffer;
        protected int writePosition;
        protected int readPosition;
        protected int _Count;
        #endregion

        #region Constructor
        /// <summary>
        /// Create a new circular buffer.
        /// </summary>
        /// <param name="size">Max buffer size in samples.</param>
        public CircularBuffer(int size)
        {
            if (size < 0)
                size = 0;

            this.buffer = new double[size];
        }
        #endregion

        #region Properties
        /// <summary>
        /// Maximum length of this circular buffer.
        /// </summary>
        public int MaxLength
        {
            get { return this.buffer.Length; }
        }

        /// <summary>
        /// Number of samples currently stored in the circular buffer.
        /// </summary>
        public int Count
        {
            get { lock (this.SyncRoot) { return this._Count; } }
        }
        #endregion

        #region Write
        /// <summary>
        /// Write data to the buffer, discarding the OLDEST samples if it no longer fits.
        /// </summary>
        /// <param name="data">Data to write.</param>
        /// <param name="offset">Offset into data.</param>
        /// <param name="count">Number of samples to write.</param>
        /// <returns>Number of samples retained.</returns>
        public int Write(double[] data, int offset, int count)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            lock (this.SyncRoot)
            {
                if (this.buffer.Length == 0 || count <= 0)
                    return 0;

                //DEFECT FIX: this used to clamp count to the FREE space, i.e. it silently dropped
                //the INCOMING audio once the buffer filled and kept serving whatever was already
                //stored. On a live analyser that froze the display on stale audio for a whole
                //buffer length (ten seconds on the ULF charts) and made it take another whole
                //buffer length to fall silent after the signal stopped. The newest block is the
                //only one worth keeping, so an overrun now discards the oldest samples instead.
                if (count > this.buffer.Length)
                {
                    //More than we can ever hold: keep only the tail of the block.
                    offset += count - this.buffer.Length;
                    count = this.buffer.Length;
                }

                int Local_Overflow = count - (this.buffer.Length - this._Count);
                if (Local_Overflow > 0)
                    this.AdvanceCore(Local_Overflow);

                int Local_SpaceToEnd = this.buffer.Length - this.writePosition;
                int Local_WriteToEnd = Math.Min(Local_SpaceToEnd, count);

                if (Local_WriteToEnd > 0)
                {
                    Array.Copy(data, offset, this.buffer, this.writePosition, Local_WriteToEnd);
                    this.writePosition += Local_WriteToEnd;
                    if (this.writePosition >= this.buffer.Length) this.writePosition -= this.buffer.Length;
                }

                int Local_Remaining = count - Local_WriteToEnd;
                if (Local_Remaining > 0)
                {
                    // Must have wrapped round. Write the remainder to the start.
                    Array.Copy(data, offset + Local_WriteToEnd, this.buffer, this.writePosition, Local_Remaining);
                    this.writePosition += Local_Remaining;
                    if (this.writePosition >= this.buffer.Length) this.writePosition -= this.buffer.Length;
                }

                this._Count += count;
                return count;
            }
        }
        #endregion

        #region Read and Peek
        /// <summary>
        /// Copy from the buffer WITHOUT consuming, so the caller can analyse a whole frame and then
        /// <see cref="Advance"/> by only the overlap hop.
        /// </summary>
        /// <param name="data">Buffer to read into.</param>
        /// <param name="offset">Offset into the read buffer.</param>
        /// <param name="count">Samples to copy.</param>
        /// <returns>Number of samples actually copied.</returns>
        public int Peek(double[] data, int offset, int count)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            lock (this.SyncRoot)
            {
                if (count > this._Count)
                    count = this._Count;

                if (count <= 0)
                    return 0;

                int Local_AvailableToEnd = this.buffer.Length - this.readPosition;
                int Local_ReadToEnd = Math.Min(Local_AvailableToEnd, count);

                if (Local_ReadToEnd > 0)
                    Array.Copy(this.buffer, this.readPosition, data, offset, Local_ReadToEnd);

                int Local_Remaining = count - Local_ReadToEnd;
                if (Local_Remaining > 0)
                    Array.Copy(this.buffer, 0, data, offset + Local_ReadToEnd, Local_Remaining);

                return count;
            }
        }

        /// <summary>
        /// Read from the buffer, consuming exactly the samples returned.
        /// </summary>
        /// <param name="data">Buffer to read into.</param>
        /// <param name="offset">Offset into the read buffer.</param>
        /// <param name="count">Samples to read.</param>
        /// <returns>Number of samples actually read.</returns>
        public int Read(double[] data, int offset, int count)
        {
            //DEFECT FIX: this moved readPosition but left _Count alone, so the buffer claimed to
            //still hold samples it had already handed out. Callers pairing Read(frame) with
            //Advance(hop) therefore pushed readPosition forward by frame + hop per frame while
            //_Count only dropped by hop: the read pointer ran away from the write pointer and swept
            //the whole storage, feeding the FFT never-written zeros and seconds-old audio.
            lock (this.SyncRoot)
            {
                int Local_Read = this.Peek(data, offset, count);
                this.AdvanceCore(Local_Read);
                return Local_Read;
            }
        }
        #endregion

        #region Advance and Reset
        /// <summary>
        /// Advances the buffer, discarding the oldest samples. A non-positive count is a no-op.
        /// </summary>
        /// <param name="count">Samples to discard.</param>
        public void Advance(int count)
        {
            lock (this.SyncRoot)
            {
                this.AdvanceCore(count);
            }
        }

        /// <summary>
        /// Resets the buffer.
        /// </summary>
        public void Reset()
        {
            lock (this.SyncRoot)
            {
                this.ResetCore();
            }
        }

        /// <summary>
        /// Discards the oldest samples. The caller must already hold <see cref="SyncRoot"/>.
        /// </summary>
        /// <param name="count">Samples to discard.</param>
        protected void AdvanceCore(int count)
        {
            //DEFECT FIX: a negative count used to INCREASE _Count (_Count -= -1) and drive
            //readPosition negative, which made the very next read throw out of Array.Copy.
            if (count <= 0)
                return;

            if (count >= this._Count)
            {
                this.ResetCore();
                return;
            }

            this._Count -= count;
            this.readPosition += count;
            if (this.readPosition >= this.buffer.Length) this.readPosition %= this.buffer.Length;
        }

        /// <summary>
        /// Empties the buffer. The caller must already hold <see cref="SyncRoot"/>.
        /// </summary>
        protected void ResetCore()
        {
            this._Count = 0;
            this.readPosition = 0;
            this.writePosition = 0;
        }
        #endregion
    }
}
