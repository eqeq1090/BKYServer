using Amazon.Runtime.Internal.Transform;
using Microsoft.Diagnostics.Runtime;
using Swan;
using BKServerBase.ConstEnum;
using BKServerBase.Interface;
using BKServerBase.Logger;
using BKServerBase.Threading;
using BKServerBase.Util;
using BKDataLoader.MasterData;
using BKGameServerComponent.Channel;
using BKGameServerComponent.Controller;
using BKGameServerComponent.Controller.Detail;
using BKGameServerComponent.Session;
using BKNetwork.API;
using BKNetwork.Interface;
using BKProtocol;
using BKProtocol.C2G;
using BKProtocol.G2A;
using BKProtocol.Pubsub;

namespace BKGameServerComponent.Actor
{
    internal partial class Player
    { 
        public async CustomTask HandleChangeName(string newName)
        {
            var apiRes = await SendRequestAsync<APIChangeNameRes>(new APIChangeNameReq()
            {
                playerUID = m_PlayerUID,
                newName = newName
            });
            if (apiRes.errorCode != MsgErrorCode.Success)
            {
                SendToMe(new ChangeNameRes()
                {
                    errorCode = apiRes.errorCode
                });
                return;
            }

            m_PlayerName = apiRes.changedName;

            SendToMe(new ChangeNameRes()
            {
                changedName = apiRes.changedName
            });
        }
    
        public void HandleLogout()
        {
            GameServerComponent.Instance.PostRemovePlayerAsync(ChannelID, m_PlayerUID);

            var res = new LogoutRes()
            {
                errorCode = MsgErrorCode.Success,
            };
            SendToMe(res);
        }

    }
}
