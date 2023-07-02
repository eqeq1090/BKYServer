using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BKProtocol;

namespace BKGameServerComponent.Channel.Detail
{
    public sealed class APIRequestInfo
    {
        public readonly long ChannelID;
        public readonly long PlayerUID;
        public readonly string SessionID;
        public readonly long TraceID;
        public readonly IMsg RequestMsg;
        public readonly Type MsgType;

        public APIRequestInfo(long traceID, long channelID,  long playerUID, IMsg requestMsg, string sessionID, Type msgType)
        {
            TraceID = traceID;
            ChannelID = channelID;
            PlayerUID = playerUID;
            RequestMsg = requestMsg;
            SessionID = sessionID;
            MsgType = msgType;
        }
    }
}
