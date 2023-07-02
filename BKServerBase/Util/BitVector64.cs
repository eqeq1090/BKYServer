using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BKServerBase.Util
{
    using System;
    using System.Text;

    public struct BitVector64 : IEquatable<BitVector64>
    {
        private long data;

        public BitVector64(BitVector64 source)
        {
            data = source.data;
        }

        public BitVector64(long source)
        {
            data = source;
        }

        public long Data => data;
        public long this[BitVector64.Section section]
        {
            get
            {
                return (data >> section.Offset) & section.Mask;
            }

            set
            {
                if (value < 0)
                {
                    throw new ArgumentException("Section can't hold negative values");
                }
                else if (value > section.Mask)
                {
                    throw new ArgumentException("Value too large to fit in section");
                }

                data &= ~(section.Mask << section.Offset);
                data |= value << section.Offset;
            }
        }

        public bool this[long mask]
        {
            get
            {
                return (data & mask) == mask;
            }

            set
            {
                if (value)
                {
                    data |= mask;
                }
                else
                {
                    data &= ~mask;
                }
            }
        }

        // Methods     
        public static long CreateMask()
        {
            return CreateMask(0);   // 1;
        }

        public static long CreateMask(long prev)
        {
            if (prev == 0)
            {
                return 1;
            }
            else if (prev == long.MinValue)
            {
                throw new InvalidOperationException("all bits set");
            }

            return prev << 1;
        }

        public static Section CreateSection(int bit)
        {
            return CreateSection(bit, new Section(0, 0));
        }

        public static Section CreateSection(int bit, BitVector64.Section previous)
        {
            if (bit < 1)
            {
                throw new ArgumentException("bit");
            }

            var mask = (1L << bit) - 1;
            int offset = previous.Offset + NumberOfSetBits(previous.Mask);

            if (offset > 64)
            {
                throw new ArgumentException("Sections cannot exceed 64 bits in total");
            }

            return new Section(mask, (short)offset);
        }

        public static string ToString(BitVector64 value)
        {
            var sb = new StringBuilder(0x2d);
            sb.Append("BitVector64{0x");
            sb.Append(Convert.ToString(value.Data, 16));
            sb.Append("}");

            return sb.ToString();
        }

        public bool Equals(BitVector64 other)
        {
            return Data == other.Data;
        }

        public override bool Equals(object? obj)
        {
            if (!(obj is BitVector64))
            {
                return false;
            }

            return data == ((BitVector64)obj).data;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string ToString()
        {
            return ToString(this);
        }

        private static int NumberOfSetBits(long i)
        {
            int count = 0;
            for (int bit = 0; bit < 64; bit++)
            {
                if ((i & 0x01) != 0)
                {
                    count++;
                }

                i = i >> 1;
            }

            return count;
        }

        private static int HighestSetBit(long i)
        {
            for (int bit = 63; bit >= 0; bit--)
            {
                long mask = 1L << bit;
                if ((mask & i) != 0)
                {
                    return bit;
                }
            }

            return -1;
        }

        #region Section
        public struct Section
        {
            private readonly long mask;
            private readonly short offset;

            internal Section(long mask, short offset)
            {
                this.mask = mask;
                this.offset = offset;
            }

            public long Mask => mask;
            public short Offset => offset;

            public static string ToString(Section value)
            {
                var b = new StringBuilder();
                b.Append("Section{0x");
                b.Append(Convert.ToString(value.Mask, 16));
                b.Append(", 0x");
                b.Append(Convert.ToString(value.Offset, 16));
                b.Append("}");

                return b.ToString();
            }

            public bool Equals(Section obj)
            {
                return mask == obj.mask &&
                    offset == obj.offset;
            }

            public override bool Equals(object? obj)
            {
                if (!(obj is Section))
                {
                    return false;
                }

                return Equals((Section)obj);
            }

            public override int GetHashCode()
            {
                return (((short)mask).GetHashCode() << 16)
                    + offset.GetHashCode();
            }

            public override string ToString()
            {
                return ToString(this);
            }
        }
        #endregion //Section
    }

}
