using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace JBe.IO
{
    public sealed class ContinuousMemoryStream : Stream
    {
        private const int DefaultBufferCount = 64;
        private const int DefaultBufferSize = 65536;

        private readonly bool canRead;
        private readonly bool canSeek;
        private readonly bool canWrite;

        private readonly BlockingCollection<BufferSegment<byte>> buffer;
        private readonly BufferPool<byte> bufferPool;
        BufferSegment<byte> segment;
        private int readIndex;

        public ContinuousMemoryStream()
            : this(DefaultBufferCount, DefaultBufferSize)
        {
        }

        public ContinuousMemoryStream(int bufferCount, int bufferSize)
        {
            canRead = true;
            canSeek = false;
            canWrite = true;

            buffer = new BlockingCollection<BufferSegment<byte>>();
            bufferPool = new BufferPool<byte>(bufferCount, bufferSize);
        }

        public override void Flush()
        {
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (this.segment != null)
            {
                return ReadInternal(buffer, offset, count);
            }

            foreach (BufferSegment<byte> bufferSegment in this.buffer.GetConsumingEnumerable())
            {
                this.segment = bufferSegment;
                this.readIndex = segment.Index;

                return ReadInternal(buffer, offset, count);
            }

            return 0;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            int rest = count;
            int totalCopied = 0;
            do
            {
                BufferSegment<byte> segment = bufferPool.GetSegment();
                var allowdCount = Math.Min(rest, segment.Length);
                rest -= allowdCount;
                Buffer.BlockCopy(buffer, totalCopied, segment.Owner.MainBuffer, segment.Index, allowdCount);
                segment.Count = allowdCount;
                totalCopied += allowdCount;
                this.buffer.Add(segment);
            } while (rest > 0);
        }

        public override bool CanRead
        {
            get { return canRead; }
        }

        public override bool CanSeek
        {
            get { return canSeek; }
        }

        public override bool CanWrite
        {
            get { return canWrite; }
        }

        public override long Length
        {
            get { throw new NotSupportedException(); }
        }

        public override long Position
        {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }

        public void SetEndOfStream()
        {
            buffer.CompleteAdding();
        }

        protected override void Dispose(bool disposing)
        {
            SetEndOfStream();
        }

        private int ReadInternal(byte[] buffer, int offset, int count)
        {
            int totalRead = 0;
            do
            {
                Debug.Assert(segment != null, "segment != null");
                int allowdCount = Math.Min(count, segment.Count);
                Buffer.BlockCopy(segment.Owner.MainBuffer, readIndex, buffer, offset + totalRead, allowdCount);
                segment.Count -= allowdCount;
                totalRead += allowdCount;
                readIndex += allowdCount;
                if (segment.Count == 0)
                {
                    segment.Free();
                    segment = null;

                    if (this.buffer.TryTake(out segment) == false)
                        return totalRead;
                    
                    readIndex = segment.Index;
                }

                if (totalRead == count || offset + totalRead > buffer.Length - 1)
                    return totalRead;
            } while (true);
        }
    }
}
