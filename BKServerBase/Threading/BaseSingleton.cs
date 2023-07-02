namespace BKServerBase.Threading
{
    public abstract class BaseSingleton<T> where T : class
    {
        private static T? instance;
        private static object syncRoot = new object();
        public static T Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                        {
                            instance = (Activator.CreateInstance(typeof(T), true) as T)!;
                        }
                    }
                }
                return instance;
            }
        }

        protected static void ChangeInstance(T newInstance)
        {
            Interlocked.Exchange(ref instance, newInstance);
        }
    }
}