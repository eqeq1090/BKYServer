using EmbedIO.Sessions;
using System.Collections.Concurrent;
using System.Collections.Generic;
using BKServerBase.Component;
using BKServerBase.ConstEnum;
using BKServerBase.Interface;
using BKServerBase.Logger;
using BKServerBase.Messaging;
using BKServerBase.Messaging.Detail;
using BKServerBase.Threading;
using BKServerBase.Util;
using BKCommonComponent.Detail;
using BKCommonComponent.Redis;
using BKCommonComponent.Redis.Detail;
using BKGameServerComponent.Actor;
using BKGameServerComponent.Actor.Detail;
using BKGameServerComponent.Channel;
using BKGameServerComponent.ConstEnum;
using BKGameServerComponent.MsgRegister;
using BKGameServerComponent.Network;
using BKNetwork.API;
using BKNetwork.ConstEnum;
using BKNetwork.Dispatch;
using BKNetwork.Interface;
using BKNetwork.Listener;
using BKProtocol;
using BKProtocol.Enum;
using BKProtocol.Pubsub;

namespace BKGameServerComponent.Session
{
    internal partial class SessionManager
    {
        public void RegisterPubsubDispatch()
        {
            //m_PubsubDispatcherManager.RegisterDispatcher<ChatFriendMsg>((player, msg) =>
            //{
            //    player.HandleFriendChat(msg);
            //});

            
        }
    }
}
