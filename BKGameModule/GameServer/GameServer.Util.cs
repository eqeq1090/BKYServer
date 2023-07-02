using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BKNetwork.API;

namespace BKGameServerComponent
{
    public partial class GameServerComponent
    {
        private long ObjectIDGenerator = 0;

        public long GetNewObjectID()
        {
            return Interlocked.Increment(ref ObjectIDGenerator);
        }
    }
}
