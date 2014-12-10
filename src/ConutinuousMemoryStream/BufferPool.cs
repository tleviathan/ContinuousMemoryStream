using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

namespace JBe.IO
{
    /// <summary>
    /// Wraps an Array to divide it in segments to be used, maintain a single array should
    /// reduce memory fragmentation
    /// </summary>
    internal class BufferPool<T>
    {
        /// <summary>
        /// Gets the length of the segment that the main buffer will be divided
        /// </summary>
        public readonly int SubBufferLength;

        /// <summary>
        /// Queue containing all the free segments from the main buffer
        /// </summary>
        public readonly ConcurrentQueue<BufferSegment<T>> FreeSegmentsQueue;

        /// <summary>
        /// The main buffer, will be divided in blocks of SubBufferLength amount of elements
        /// </summary>
        private T[] mainBuffer;

        private readonly ManualResetEventSlim enqueuedEventSlim;

        /// <summary>
        /// Handles the buffers of the SocketAsyncEventArgs instances used in a server
        /// dividing a main buffer to avoid memory fragmentation
        /// </summary>
        public BufferPool(int initialCapacity, int subBufferSize)
        {
            this.SubBufferLength = subBufferSize;
            this.FreeSegmentsQueue = new ConcurrentQueue<BufferSegment<T>>();
            this.mainBuffer = new T[initialCapacity * subBufferSize];
            for (int i = 0; i < initialCapacity * subBufferSize; i += SubBufferLength)
            {
                FreeSegmentsQueue.Enqueue(new BufferSegment<T>(i, SubBufferLength, this));
            }
            enqueuedEventSlim = new ManualResetEventSlim();
        }

        /// <summary>
        /// Gets the main buffer, will be divided in blocks of SubBufferLength amount of elements
        /// </summary>
        /// <value>
        /// The main buffer.
        /// </value>
        public T[] MainBuffer
        {
            get { return mainBuffer; }
        }

        /// <summary>
        /// Gets a BufferSegment instance containing the index and length of a block of
        /// memory from the main buffer
        /// </summary>
        public BufferSegment<T> GetSegment()
        {
            BufferSegment<T> freeSegment;
            do
            {
                if (FreeSegmentsQueue.TryDequeue(out freeSegment) == false)
                    enqueuedEventSlim.Wait();
            } while (freeSegment == null);
            return freeSegment;
        }

        /// <summary>
        /// Frees a BufferSegment to be reused later
        /// </summary>
        /// <param name="segment">Segment released</param>
        internal void FreeBuffer(BufferSegment<T> segment)
        {
            FreeSegmentsQueue.Enqueue(segment);
            enqueuedEventSlim.Set();
        }
    }
}
