using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BKProtocol;

namespace BKNetwork.Sock.Detail
{
    public sealed class PacketRequestInfo
    {
        public PacketRequestInfo(byte[] msgBuffers, int totalSize, MsgType msgType)
        {
            MessageBuffers = msgBuffers;
            TotalSize = totalSize;
            MessageType = msgType;
        }

        public byte[] MessageBuffers { get; }
        public int TotalSize { get; }
        public MsgType MessageType { get; }
    }
}
