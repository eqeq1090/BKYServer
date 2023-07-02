using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BKDataLoader.Loader
{
    public class LoadDataHandler
    {
        public static IDataLoader? Instance { get; private set; }
        public static void InitLocal(string path)
        {
            Instance = new LocalDataLoader(path);
        }

        public static void Init(IDataLoader loader)
        {
            Instance = loader;
        }
    }
}
