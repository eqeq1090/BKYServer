using System;
using System.Threading;

namespace BKServerBase.Util
{
    public class SimpleRandUtil
    {
        private static volatile SimpleRandUtil? instance;
        private static object syncRoot = new object();

        public static SimpleRandUtil Instance
        {
            get
            {
                if (null == instance)
                {
                    lock (syncRoot)
                    {

                        if (null == instance)
                        {
                            instance = new SimpleRandUtil();
                        }
                    }
                }
                return instance;
            }
        }
        private ThreadLocal<Random> SimpleRandomGenerator;
        private SimpleRandUtil()
        {
            SimpleRandomGenerator = new ThreadLocal<Random>(() => new Random(DateTime.Now.Millisecond));
        }

        public int Next()
        {
            return SimpleRandomGenerator.Value!.Next();
        }

        public int Next(int maxValue)
        {
            return SimpleRandomGenerator.Value!.Next(maxValue);
        }

        public int Next(int minValue, int maxValue)
        {
            return SimpleRandomGenerator.Value!.Next(minValue, maxValue);
        }
        public double Next(double minValue, double maxValue)
        {
            return SimpleRandomGenerator.Value!.NextDouble() * (maxValue - minValue) + minValue;
        }

        public static string GenerateDigits(int length)
        {
            var rndDigits = new System.Text.StringBuilder().Insert(0, "0123456789", length).ToString().ToCharArray();
            return string.Join("", rndDigits.OrderBy(o => Guid.NewGuid()).Take(length));
        }

        public string GetNewRandString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var sb = new System.Text.StringBuilder(length);
            for (int i = 0; i < length; i++) sb.Append(chars[Next(chars.Length)]);
            return sb.ToString();
        }
    }
}
