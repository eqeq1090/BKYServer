using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BKServerBase.Extension;

namespace BKServerBase.Util
{
    public static class RandomPicker
    {
        public static int Range(int min, int max)
        {
            return ThreadLocalRandom.Instance.Next(min, max);
        }

        public static int ArrayIndex(int count)
        {
            return ThreadLocalRandom.Instance.Next(count);
        }

        public static int Next(int maxValue)
        {
            return ThreadLocalRandom.Instance.Next(maxValue);
        }

        public static int Next()
        {
            return ThreadLocalRandom.Instance.Next();
        }

        public static int Next(int minValue, int maxValue)
        {
            return ThreadLocalRandom.Instance.Next(minValue, maxValue);
        }


        public static double NextDouble()
        {
            return ThreadLocalRandom.Instance.NextDouble();
        }

        public static float Range(float min, float max)
        {
            if (min > max)
            {
                throw new ArgumentException($"[RandomGenerator] min:{min} max:{max}");
            }

            float rand = (float)ThreadLocalRandom.Instance.NextDouble();
            return (rand * (max - min)) + min;
        }

        public static long LongRandom()
        {
            return (long)(ThreadLocalRandom.Instance.NextDouble() * Int64.MaxValue);
        }

        public static ulong LongRandom(ulong min, ulong max)
        {
            byte[] buffer = new byte[8];
            ThreadLocalRandom.Instance.NextBytes(buffer);
            ulong longRand = buffer.DirectToUint64(0);
            ulong result = (longRand % (max - min)) + min;

            return result;
        }

        public static void Shuffle<T>(this IList<T> list)
        {
            // https://stackoverflow.com/questions/273313/randomize-a-listt
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = ThreadLocalRandom.Instance.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        public static class ThreadLocalRandom
        {
            private static ThreadLocal<Random> _random = new ThreadLocal<Random>(() => new Random());
            public static Random Instance => _random.Value!;

            public static void SetSeed(int seed)
            {
                _random.Value = new Random(seed);
            }
        }
    }
}
