using Polly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BKServerBase.Threading;
using BKGameServerComponent.Network;
using BKGameServerComponent.Session;
using BKNetwork.Interface;
using BKProtocol;
using BKProtocol.C2G;

namespace BKGameServerComponent
{
    public partial class GameServerComponent
    {
        public void RegisterPlayerContentsDispatcher()
        {
            RegisterClientSessionDispatcher<LoginReq>((ctx, msg) =>
            {
                if (ctx is not SessionContext sessionContext)
                {
                    return;
                }

                sessionContext.PostLogin(msg);
            });

            RegisterClientSessionDispatcher<HeartBeatReq>((ctx, msg) =>
            {
                if (ctx is not SessionContext sessionContext)
                {
                    return;
                }

                sessionContext.HandleHeartBeat();
            });

            RegisterPlayerDispatcher<LogoutReq>((player, msg) =>
            {
                player.HandleLogout();
            });

            RegisterPlayerDispatcher<ChangeNameReq>((player, msg) =>
            {
                player.HandleChangeName(msg.newName).HandleError();
            });
        }
    }
}
