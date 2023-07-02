using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BKServerBase.Util
{
    public static class EnumUtil<T> where T : Enum
    {
        public static int Count => Enum.GetNames(typeof(T)).Length;

        public static IEnumerable<T> GetValues()
        {
            return Enum.GetValues(typeof(T)).Cast<T>();
        }
    }
}
