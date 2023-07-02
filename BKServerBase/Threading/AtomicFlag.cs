using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BKServerBase.Threading
{
    public class AtomicFlag
    {
        public AtomicFlag(bool value)
        {
            atomicValue = value ? 1 : 0;
        }
        private int atomicValue;
        public bool IsOn => Volatile.Read(ref atomicValue) == 1;
        public bool On()
        {
            return Interlocked.CompareExchange(ref atomicValue, 1, 0) == 0;
        }
        public bool Off()
        {
            return Interlocked.CompareExchange(ref atomicValue, 0, 1) == 1;
        }

        public bool Set(bool value)
        {
            if (value)
            {
                return On();
            }

            return Off();
        }
    }
}
