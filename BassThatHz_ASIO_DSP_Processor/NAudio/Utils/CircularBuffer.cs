namespace NAudio.Utils
{
    using System;

    /// <summary>
    /// A very basic circular buffer implementation
    /// </summary>
    public class CircularBuffer
    {
        protected double[] buffer;
        protected int writePosition;
        protected int readPosition;
        protected int _Count;

        /// <summary>
        /// Create a new circular buffer
        /// </summary>
        /// <param name="size">Max buffer size in bytes</param>
        public CircularBuffer(int size)
        {
            buffer = new double[size];
        }

        /// <summary>
        /// Write data to the buffer
        /// </summary>
        /// <param name="data">Data to write</param>
        /// <param name="offset">Offset into data</param>
        /// <param name="count">Number of bytes to write</param>
        /// <returns>number of bytes written</returns>
        public int Write(double[] data, int offset, int count)
        {
            int bytesWritten = 0;
            if (count > buffer.Length - this._Count)
                count = buffer.Length - this._Count;

            // write up to the end of the internal buffer
            int spaceToEnd = buffer.Length - writePosition;
            int writeToEnd = Math.Min(spaceToEnd, count);

            if (writeToEnd > 0)
            {
                Array.Copy(data, offset, buffer, writePosition, writeToEnd);
                writePosition += writeToEnd;
                if (writePosition >= buffer.Length) writePosition -= buffer.Length;
                bytesWritten += writeToEnd;
            }

            if (bytesWritten < count)
            {
                // must have wrapped round. Write remaining to start
                int remaining = count - bytesWritten;
                Array.Copy(data, offset + bytesWritten, buffer, writePosition, remaining);
                writePosition += remaining;
                if (writePosition >= buffer.Length) writePosition -= buffer.Length;
                bytesWritten += remaining;
            }

            this._Count += bytesWritten;
            return bytesWritten;
        }

        /// <summary>
        /// Read from the buffer
        /// </summary>
        /// <param name="data">Buffer to read into</param>
        /// <param name="offset">Offset into read buffer</param>
        /// <param name="count">Bytes to read</param>
        /// <returns>Number of bytes actually read</returns>
        public int Read(double[] data, int offset, int count)
        {
            if (count > _Count)
                count = _Count;

            int bytesRead = 0;
            int availableToEnd = buffer.Length - readPosition;
            int readToEnd = Math.Min(availableToEnd, count);

            if (readToEnd > 0)
            {
                Array.Copy(buffer, readPosition, data, offset, readToEnd);
                bytesRead += readToEnd;
                readPosition += readToEnd;
                if (readPosition >= buffer.Length) readPosition -= buffer.Length;
            }

            if (bytesRead < count)
            {
                int remaining = count - bytesRead;
                Array.Copy(buffer, readPosition, data, offset + bytesRead, remaining);
                readPosition += remaining;
                if (readPosition >= buffer.Length) readPosition -= buffer.Length;
                bytesRead += remaining;
            }

            return bytesRead;
        }

        /// <summary>
        /// Maximum length of this circular buffer
        /// </summary>
        public int MaxLength
        {
            get { return buffer.Length; }
        }

        /// <summary>
        /// Number of bytes currently stored in the circular buffer
        /// </summary>
        public int Count
        {
            get { return this._Count; }
        }

        /// <summary>
        /// Resets the buffer
        /// </summary>
        public void Reset()
        {
            _Count = 0;
            readPosition = 0;
            writePosition = 0;
        }

        /// <summary>
        /// Advances the buffer, discarding bytes
        /// </summary>
        /// <param name="count">Bytes to advance</param>
        public void Advance(int count)
        {
            if (count >= _Count)
            {
                Reset();
            }
            else
            {
                _Count -= count;
                readPosition += count;
                if (readPosition >= buffer.Length) readPosition %= buffer.Length;
            }
        }
    }
}
