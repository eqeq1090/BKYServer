using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;
using BKServerBase.ConstEnum;
using BKServerBase.Threading;
using BKServerBase.Logger;
using BKServerBase.Util;

namespace BKServerBase.Component
{
    internal class ComponentAttr
    {
        public IComponent Component { get; private set; }
        public bool LazyLoad { get; private set; }
        public bool Initialized { get; private set; }
        public ComponentAttr(IComponent component, bool lazyLoad = false)
        {
            Component = component;
            LazyLoad = lazyLoad;
            Initialized = false;
        }

        public (bool success, OnComponentInitializedHandler? initDoneFunc) Initialize(bool lazyLoad)
        {
            if (true == Initialized)
            {
                return (true, null);
            }
            if (LazyLoad == lazyLoad)
            {
                return Component.Initialize();
            }
            return (true, null);
        }
    }
    public sealed class ComponentManager : BaseSingleton<ComponentManager>, IComponentManager
    {
        public delegate void HaltMethod();
        [DllImport("winmm.dll", EntryPoint = "timeBeginPeriod")]
        public static extern uint timeBeginPeriod(uint uMilliseconds);
        [DllImport("winmm.dl1", EntryPoint = "timeEndPeriod")]
        public static extern uint timeEndPeriod(uint uMilliseconds);
        private Dictionary<string, ComponentAttr> ComponentDict = new Dictionary<string, ComponentAttr>();
        public OnComponentInitializedHandler? OnComponentInitialized { get; set; }
        private Thread? ComponentTickerThread = null;
        private volatile bool _shouldStop;
        private long LastTick { get; set; }
        private long m_totalTickCount = 0;
        private long m_totalTickAwakeMsec = 0;
        private long m_totalTickSleepMsec = 0;
        public long TotalTickCount { get => m_totalTickCount; }
        public long TotalTickAwakeMsec { get => m_totalTickAwakeMsec; }
        public long TotalTickSleepMsec { get => m_totalTickSleepMsec; }

        private HaltMethod? m_HaltMethod;

        private volatile bool m_EnabledForceGC = false;
        private long m_GCCollectCount = 0;
        private long m_GCCollectLastTick = 0;
        private long m_GCCollectPeriodMilliseconds = 10 * 1000; // 10 seconds
                                                                //private long m_GCCollectForceSeconds = 60;
        private Dictionary<GCCollectionMode, long> m_GCCollectTotalMilliseconds;

        private ComponentManager()
        {
            m_GCCollectTotalMilliseconds = new Dictionary<GCCollectionMode, long>
                { { GCCollectionMode.Forced, 0 },
                    { GCCollectionMode.Optimized, 0 }
                };
        }

        public void SetHaltMethod(HaltMethod method)
        {
            m_HaltMethod = method;
        }

        public bool AddComponent<T>(IComponent component, bool lazyLoad = false) where T : class, IComponent
        {
            if (null == component)
            {
                return false;
            }
            string componentName = typeof(T).ToString();
            return AddComponent<T>(componentName, component, lazyLoad);
        }

        public bool AddComponent<T>(string componentName, IComponent component, bool lazyLoad = false) where T : class, IComponent
        {
            CoreLog.Normal.LogDebug(string.Format("Adding Component f0)", componentName));
            if (null == component)
            {
                return false;
            }

            if (true == ComponentDict.ContainsKey(componentName))
            {
                return false;
            }
            ComponentAttr newComponentAttr = new ComponentAttr(component, lazyLoad);
            ComponentDict[componentName] = newComponentAttr;

            return true;
        }

        public bool RemoveComponent<T>(IComponent component) where T : class, IComponent
        {
            string componentName = component.ToString() ?? "";
            return RemoveComponent<T>(componentName, component);
        }

        public bool RemoveComponent<T>(string componentName, IComponent component) where T : class, IComponent
        {
            return false;
        }

        public T? GetComponent<T>() where T : class, IComponent
        {
            if (true == ComponentDict.ContainsKey(typeof(T).ToString()))
            {
                return (T)ComponentDict[typeof(T).ToString()].Component;
            }
            return null;
        }

        public bool Initialize()
        {
            CoreLog.Normal.LogDebug("- Components Initializing -");
            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            CoreLog.Normal.LogDebug($"GC Settings: {GCSettings.IsServerGC} / {GCSettings.LargeObjectHeapCompactionMode} / {GCSettings. LatencyMode}");
            if (!GCSettings.IsServerGC)
            {
                CoreLog.Normal.LogError("ServerGC has been turned off!");
            }
            bool errorDetected = false;


            foreach (ComponentAttr componentAttr in ComponentDict.Values)
            {
                CoreLog.Normal.LogDebug($"ComponentName: {componentAttr.Component.ToString()}");


                var result = componentAttr.Initialize(false);
                if (result.success == false)
                {
                    errorDetected = true;
                    CoreLog.Normal.LogDebug("\t-> Result : Failed");
                }
                else
                {
                    var command = result.initDoneFunc;
                    if (command != null)
                    {
                        OnComponentInitialized += command;
                    }
                    CoreLog.Normal.LogDebug("\t-> Result : Done");
                }
            }


            LastTick = TimeUtil.GetCurrentTickMilliSec();
            m_GCCollectLastTick = LastTick;
            if (true == errorDetected)
            {
                return false;
            }
            return true;
        }

        public bool LazyLoadComponent()
        {
            CoreLog.Normal.LogDebug("- Components Lazy Load -");
            bool errorDetected = false;

            foreach (ComponentAttr componentAttr in ComponentDict.Values)
            {
                CoreLog.Normal.LogDebug($"LazyLoadComponent:{componentAttr.Component.ToString()}");

                var result = componentAttr.Initialize(true);
                if (result.success == false)
                {
                    errorDetected = true;
                    CoreLog.Normal.LogDebug("\t-> Result : Failed");
                }
                else
                {
                    var command = result.initDoneFunc;
                    if (command != null)
                    {
                        OnComponentInitialized += command;
                    }
                    CoreLog.Normal.LogDebug("\t-> Result : Done");
                }
            }
            LastTick = TimeUtil.GetCurrentTickMilliSec();
            if (true == errorDetected)
            {
                return false;
            }
            return true;
        }

        public void InvokeWaitInitDone()
        {
            OnComponentInitialized?.Invoke();
        }

        public void Start()
        {
            try
            {
                ComponentTickerThread = new Thread(OnTick);
                {
                    ComponentTickerThread.Name = String.Format("ComponentTicker");
                    ComponentTickerThread.Start();
                }
            }
            catch (Exception ex)
            {
                CoreLog.Normal.LogFatal(ex);
                return;
            }
        }

        public void OnTick()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                timeBeginPeriod(1);
            }
            while (_shouldStop == false)
            {
                try
                {
                    m_totalTickCount++;
                    long currentTick = TimeUtil.GetCurrentTickMilliSec();
                    long deltaTick = currentTick - LastTick;
                    LastTick = currentTick;
                    foreach (var componentAttr in ComponentDict.Values)
                    {
                        componentAttr.Component.OnUpdate(deltaTick);
                    }
                    long elapsedTick = TimeUtil.GetCurrentTickMilliSec() - currentTick;
                    long elapsedMsec = elapsedTick / TimeSpan.TicksPerMillisecond;
                    m_totalTickAwakeMsec += elapsedMsec;
                    long durationMsec = BaseConsts.COMPONENT_TICK_DURATION_MSEC;
                    long remainMsec = durationMsec - elapsedMsec;
                    long collectElapsedMsec = 0;
                    if (remainMsec > 5)
                    {
                        long collectStartTick = TimeUtil.GetCurrentTickMilliSec();
                        if (m_EnabledForceGC)
                        {
                            CollectGarbage();
                        }
                        collectElapsedMsec = TimeUtil.GetCurrentTickDiffMilliSec(collectStartTick);
                    }
                    remainMsec -= collectElapsedMsec;
                    if (remainMsec > 0)
                    {
                        m_totalTickSleepMsec += remainMsec;
                        Thread.Sleep((int)remainMsec);
                    }
                    else
                    {
                        if (elapsedMsec > 100)
                        {
                            CoreLog.Normal.LogDebug($"ComponentManager Tick Overhead! " +
                            $"tick={m_totalTickCount} duration={durationMsec} elapsed={elapsedMsec} remain={remainMsec} " +
                            $"gc-{collectElapsedMsec} " +
                            $"total(awake={m_totalTickAwakeMsec} sleep={m_totalTickSleepMsec})");
                        }
                    }
                }
                catch (Exception e)
                {
                    CoreLog.Critical.LogFatal($"ComponentManager Exception :{e} ");
                }
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                timeEndPeriod(1);
            }
            Shutdown();
        }

        private bool Shutdown()
        {
            foreach (var componentAttr in ComponentDict.Values.Reverse())
            {
                componentAttr.Component.Shutdown();
                CoreLog.Normal.LogDebug(string.Format("It Shutdown -> (0)", componentAttr.Component.ToString()));
                if (false == componentAttr.Component.Shutdown())
                {
                    CoreLog.Normal.LogDebug(string.Format("\t Shutdown Failed. -> (0)", componentAttr.Component.ToString()));
                }
            }
            ComponentDict.Clear();
            return true;
        }

        public void Stop()
        {
            _shouldStop = true;
            ComponentTickerThread?.Join();
        }

        public bool GetForceGCStatus()
        {
            return m_EnabledForceGC;
        }

        public bool EnableForceGC(bool enabled)
        {
            m_EnabledForceGC = enabled;
            var status = m_EnabledForceGC ? "Enabled" : "Disabled";
            CoreLog.Normal.LogDebug($"Periodic ForceGC status)!");
            return m_EnabledForceGC;
        }

        public void Halt()
        {
            m_HaltMethod?.Invoke();
        }

        private void CollectGarbage()
        {
            var nowTick = TimeUtil.GetCurrentTickMilliSec();
            var gcPeriodMsec = (nowTick - m_GCCollectLastTick) / TimeSpan.TicksPerMillisecond;
            if (gcPeriodMsec > m_GCCollectPeriodMilliseconds)
            {
                m_GCCollectCount++;
                var generation = 2;
                var mode = m_GCCollectCount % 6 == 0 ? GCCollectionMode.Forced : GCCollectionMode.Optimized;
                var sw = Stopwatch.StartNew();
                GC.Collect(generation, mode);
                sw.Stop();

                m_GCCollectLastTick = nowTick;
                m_GCCollectTotalMilliseconds[mode] += sw.ElapsedMilliseconds;
                CoreLog.Normal.LogDebug($"GC((m_GCCollectCount) (fgeneration), (mode)) Collected! " +
                $"Elapsed(sw.ElapsedMilliseconds) Total((m_GCCollectTotalMilliseconds[mode]))");
            }
        }
    }
}
