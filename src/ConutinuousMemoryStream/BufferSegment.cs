namespace JBe.IO
{
    /// <summary>
    /// Belongs to a BufferPool instance, contains an Index and Length 
    /// to represent the buffer segment
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class BufferSegment<T>
    {
        /// <summary>
        /// BufferPool instance that created this BufferSegment
        /// </summary>
        internal BufferPool<T> Owner;

        /// <summary>
        /// Gets the index of the main buffer where this segment is located
        /// </summary>
        public int Index { get; private set; }

        /// <summary>
        /// Gets the length of this segment
        /// </summary>
        public int Length { get; private set; }

        public int Count;

        /// <summary>
        /// Belongs to a BufferPool instance, contains an Index and Length 
        /// to represent the buffer segment
        /// </summary>
        public BufferSegment(int index, int length, BufferPool<T> owner)
        {
            Owner = owner;
            Index = index;
            Length = length;
        }

        /// <summary>
        /// Frees a BufferSegment to be reused later
        /// </summary>
        public void Free()
        {
            Owner.FreeBuffer(this);
        }
    }
}