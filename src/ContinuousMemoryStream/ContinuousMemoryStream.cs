using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Text;

namespace JBe.IO
{
    public sealed class ConutinuousMemoryStream : Stream
    {
        private const int DefaultBufferCount = 64;
        private const int DefaultBufferSize = 65536;

        private readonly bool canRead;
        private readonly bool canSeek;
        private readonly bool canWrite;

        private readonly BlockingCollection<BufferSegment<byte>> buffer;
        private readonly BufferPool<byte> bufferPool;
        BufferSegment<byte> segment;

        public ConutinuousMemoryStream()
            : this(DefaultBufferCount, DefaultBufferSize)
        {
        }

        public ConutinuousMemoryStream(int bufferCount, int bufferSize)
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
            if (segment == null)
                while (this.buffer.TryTake(out segment, 1000) == false)
                {
                    if (this.buffer.IsCompleted)
                        return 0;
                }

            Debug.Assert(segment != null, "segment != null");
            int allowdCount = Math.Min(count, segment.Count);
            segment.Count -= allowdCount;
            Buffer.BlockCopy(segment.Owner.MainBuffer, segment.Index, buffer, offset, allowdCount);
            if (allowdCount == segment.Count)
            {
                segment.Free();
                segment = null;
            }
            return allowdCount;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            int rest = count;
            do
            {
                BufferSegment<byte> segment = bufferPool.GetSegment();
                var allowdCount = Math.Min(rest, segment.Length);
                rest -= allowdCount;
                Buffer.BlockCopy(buffer, offset, segment.Owner.MainBuffer, segment.Index, allowdCount);
                segment.Count = allowdCount;
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
    }
}
