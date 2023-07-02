using System.Threading;
using Prometheus;

namespace BKServerBase.Util
{
    public static class ObjectCounter<T> where T : class
    {
        private static long m_ObjectCounter = 0;
        private static readonly Gauge.Child m_PrometheusGauge;
        static ObjectCounter()
        {
            m_ObjectCounter = 0;
            m_PrometheusGauge = Metrics
            .CreateGauge($"object_counter", "Object Counter", "kind").WithLabels(typeof(T).Name);
        }

        public static void Increment()
        {
            Interlocked.Increment(ref m_ObjectCounter);
            m_PrometheusGauge.Set(m_ObjectCounter);
        }

        public static void Decrement()
        {
            Interlocked.Decrement(ref m_ObjectCounter);
            m_PrometheusGauge.Set(m_ObjectCounter);
        }

        public static long Get()
        {
            return Interlocked.Read(ref m_ObjectCounter);
        }
    }

    public static class ObjectCounter<T, K> where T : class
    {
        private static long m_ObjectCounter = 0;
        private static readonly Gauge.Child m_PrometheusGauge;

        static ObjectCounter()
        {
            m_ObjectCounter = 0;
            var config = new GaugeConfiguration();
            config.LabelNames = new string[] { "kind", "of" };
            m_PrometheusGauge = Metrics
            .CreateGauge("object_counter_manager", "Object Counter", config)
            .WithLabels(typeof(T).Name, typeof(K).Name);
        }

        public static void Increment()
        {
            Interlocked.Increment(ref m_ObjectCounter);
            m_PrometheusGauge.Set(m_ObjectCounter);
        }

        public static void Decrement()
        {
            Interlocked.Decrement(ref m_ObjectCounter);
            m_PrometheusGauge.Set(m_ObjectCounter);
        }

        public static long Get()
        {
            return Interlocked.Read(ref m_ObjectCounter);
        }
    }

    public class ObjectCounterHelper<T> where T : class
    {
        public ObjectCounterHelper()
        {
            ObjectCounter<T>.Increment();
        }

        ~ObjectCounterHelper()
        {
            ObjectCounter<T>.Decrement();
        }
    }

    public class ObjectCounterHelper<T, K>
            where T : class
    {
        public ObjectCounterHelper()
        {
            ObjectCounter<T, K>.Increment();
        }

        ~ObjectCounterHelper()
        {
            ObjectCounter<T, K>.Decrement();
        }
    }
}

