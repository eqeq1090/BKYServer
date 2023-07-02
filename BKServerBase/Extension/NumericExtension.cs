using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BKServerBase.Extension
{
    public static class NumericExtension
    {
        public static int Clamp(this int val, int min, int max)
        {
            return Math.Min(Math.Max(val, min), max);
        }

        public static long Clamp(this long val, long min, long max)
        {
            return Math.Min(Math.Max(val, min), max);
        }

        public static T Clamp<T>(this T val, T min, T max)
            where T : class, IComparable<T>
        {
            if (val.CompareTo(min) < 0)
            {
                return min;
            }

            return val.CompareTo(max) > 0 ? max : val;
        }

        public static ushort DirectToUint16(this byte[] buffer, int startIndex)
        {
            // faster then BitConverter.ToUInt16
            return (ushort)((((uint)buffer[startIndex + 1]) << 8) |
                buffer[startIndex + 0]);
        }

        public static uint DirectToUint32(this byte[] buffer, int startIndex)
        {
            // faster then BitConverter.ToUInt32
            return (((((((uint)buffer[startIndex + 3]) << 8) |
                buffer[startIndex + 2]) << 8) |
                buffer[startIndex + 1]) << 8) |
                buffer[startIndex + 0];
        }

        public static ulong DirectToUint64(this byte[] buffer, int startIndex)
        {
            // faster then BitConverter.ToUInt64
            return (((((((((((((((ulong)buffer[startIndex + 7]) << 8) |
                buffer[startIndex + 6]) << 8) |
                buffer[startIndex + 5]) << 8) |
                buffer[startIndex + 4]) << 8) |
                buffer[startIndex + 3]) << 8) |
                buffer[startIndex + 2]) << 8) |
                buffer[startIndex + 1]) << 8) |
                buffer[startIndex + 0];
        }

        public static void DirectWriteTo(this int data, byte[] buffer, int position)
        {
            // faster then Buffer.BlockCopy(BitConverter.GetBytes(data), 0, buffer, position, sizeof(int))
            buffer[position] = (byte)data;
            buffer[position + 1] = (byte)(data >> 8);
            buffer[position + 2] = (byte)(data >> 16);
            buffer[position + 3] = (byte)(data >> 24);
        }

        public static void DirectWriteTo(this long data, byte[] buffer, int position)
        {
            // faster then Buffer.BlockCopy(BitConverter.GetBytes(data), 0, buffer, position, sizeof(long))
            buffer[position] = (byte)data;
            buffer[position + 1] = (byte)(data >> 8);
            buffer[position + 2] = (byte)(data >> 16);
            buffer[position + 3] = (byte)(data >> 24);
            buffer[position + 4] = (byte)(data >> 32);
            buffer[position + 5] = (byte)(data >> 40);
            buffer[position + 6] = (byte)(data >> 48);
            buffer[position + 7] = (byte)(data >> 56);
        }

        public static void DirectWriteTo(this ulong data, byte[] buffer, int position)
        {
            // faster then Buffer.BlockCopy(BitConverter.GetBytes(data), 0, buffer, position, sizeof(ulong))
            buffer[position] = (byte)data;
            buffer[position + 1] = (byte)(data >> 8);
            buffer[position + 2] = (byte)(data >> 16);
            buffer[position + 3] = (byte)(data >> 24);
            buffer[position + 4] = (byte)(data >> 32);
            buffer[position + 5] = (byte)(data >> 40);
            buffer[position + 6] = (byte)(data >> 48);
            buffer[position + 7] = (byte)(data >> 56);
        }
    }
}
