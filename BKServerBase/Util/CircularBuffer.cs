namespace BKServerBase.Util
{
    using System.Collections.Generic;

    public sealed class CircularBuffer<T>
    {
        private readonly long kMask = 0;
        private T[] buffer;

        public CircularBuffer(int resolution, bool exponent = true)
        {
            if (exponent)
            {
                int maxSize = 1 << resolution;
                kMask = maxSize - 1;
                buffer = new T[maxSize];
            }
            else
            {
                int mask = 0;
                int share = resolution;
                do
                {
                    ++mask;
                    share = share / 2;
                } while (share != 0);

                kMask = (1 << mask) - 1;
                buffer = new T[resolution];
            }
        }

        public int MaxSize => buffer.Length;
        public IReadOnlyList<T> RawBuffer => buffer;

        public T this[long index]
        {
            get => buffer[index & kMask];
            set => buffer[index & kMask] = value;
        }

        public int ToSafeIndex(long index) => (int)(index & kMask);

        public void Regulate(long decrement)
        {
            // note: not thread-safe.
            var clone = new T[MaxSize];

            for (int i = 0; i < clone.Length; ++i)
            {
                clone[i] = this[i + decrement];
            }

            buffer = clone;
        }

        public T[] ToArray(long startIndex)
        {
            // note: not thread-safe.
            var result = new T[MaxSize];

            for (long i = 0; i < MaxSize; ++i)
            {
                result[i] = this[i + startIndex];
            }

            return result;
        }
    }
}
