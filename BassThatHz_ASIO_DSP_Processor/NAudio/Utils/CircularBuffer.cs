namespace NAudio.Utils
{
    using System;
    using System.Diagnostics;

    /// <summary>
    /// A very basic circular buffer implementation
    /// </summary>
    public class CircularBuffer
    {
        private double[] buffer;
        private int writePosition;
        private int readPosition;
        private int _Count;

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
                //throw new ArgumentException("Not enough space in buffer");

            // write to end
            int writeToEnd = Math.Min(buffer.Length - writePosition, count);
            Array.Copy(data, offset, buffer, writePosition, writeToEnd);
            writePosition += writeToEnd;
            writePosition %= buffer.Length;
            bytesWritten += writeToEnd;
            if (bytesWritten < count)
            {
                Debug.Assert(writePosition == 0);
                // must have wrapped round. Write to start
                Array.Copy(data, offset + bytesWritten, buffer, writePosition, count - bytesWritten);
                writePosition += count - bytesWritten;
                bytesWritten = count;
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
            int readToEnd = Math.Min(buffer.Length - readPosition, count);
            Array.Copy(buffer, readPosition, data, offset, readToEnd);
            bytesRead += readToEnd;
            var readPosition2 = readPosition;
            readPosition2 += readToEnd;
            readPosition2 %= buffer.Length;

            if (bytesRead < count)
            {
                // must have wrapped round. Read from start
                Debug.Assert(readPosition2 == 0);
                Array.Copy(buffer, readPosition2, data, offset + bytesRead, count - bytesRead);
                //readPosition += count - bytesRead;
                bytesRead = count;
            }

            //_Count -= bytesRead;
            //Debug.Assert(_Count >= 0);
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
                readPosition %= MaxLength;
            }

        }
    }
}