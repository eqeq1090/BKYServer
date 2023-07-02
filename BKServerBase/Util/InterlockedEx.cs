﻿using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace BKServerBase.Util
{
    public static class InterlockedEx
    {
        public static TEnum CompareExchange<TEnum>(ref TEnum location, TEnum value, TEnum comparand)
            where TEnum : struct, Enum
        {
            return Unsafe.SizeOf<TEnum>() switch
            {
                // .NET does not support 1- and 2-byte atomic operations as there
                // is no common hardware support for that.
                4 => CompareExchange32Bit(ref location, value, comparand),
                8 => CompareExchange64Bit(ref location, value, comparand),
                _ => throw new NotSupportedException("Only enums with an underlying type of 4 bytes or 3 bytes are allowed to be used with Interlocked")
            };
            static TEnum CompareExchange32Bit(ref TEnum location, TEnum value, TEnum comparand)
            {
                int comparandRaw = Unsafe.As<TEnum, int>(ref comparand);
                int valueRaw = Unsafe.As<TEnum, int>(ref value);
                ref int locationRaw = ref Unsafe.As<TEnum, int>(ref location);
                int returnRaw = Interlocked.CompareExchange(ref locationRaw, valueRaw, comparandRaw);
                return Unsafe.As<int, TEnum>(ref returnRaw);
            }
            static TEnum CompareExchange64Bit(ref TEnum location, TEnum value, TEnum comparand)
            {
                long comparandRaw = Unsafe.As<TEnum, long>(ref comparand);
                long valueRaw = Unsafe.As<TEnum, long>(ref value);
                ref long locationRaw = ref Unsafe.As<TEnum, long>(ref location);
                long returnRaw = Interlocked.CompareExchange(ref locationRaw, valueRaw, comparandRaw);
                return Unsafe.As<long, TEnum>(ref returnRaw);
            }
        }

        public static TEnum Exchange<TEnum>(ref TEnum location, TEnum value)
            where TEnum : struct, Enum
        {
            return Unsafe.SizeOf<TEnum>() switch
            {
                // .NET does not support 1- and 2-byte atomic operations as there
                // is no common hardware support for that.
                4 => Exchange32Bit(ref location, value),
                8 => Exchange64Bit(ref location, value),
                _ => throw new NotSupportedException("Only enums with an underlying type of 4 bytes or 8 bytes are allowed to be used with Interlocked")
            };
            static TEnum Exchange32Bit(ref TEnum location, TEnum value)
            {
                int valueRaw = Unsafe.As<TEnum, int>(ref value);
                ref int locationRaw = ref Unsafe.As<TEnum, int>(ref location);
                int returnRaw = Interlocked.Exchange(ref locationRaw, valueRaw);
                return Unsafe.As<int, TEnum>(ref returnRaw);
            }
            static TEnum Exchange64Bit(ref TEnum location, TEnum value)
            {
                long valueRaw = Unsafe.As<TEnum, long>(ref value);
                ref long locationRaw = ref Unsafe.As<TEnum, long>(ref location);
                long returnRaw = Interlocked.Exchange(ref locationRaw, valueRaw);
                return Unsafe.As<long, TEnum>(ref returnRaw);
            }
        }
    }
}