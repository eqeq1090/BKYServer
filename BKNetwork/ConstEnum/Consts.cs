using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BKNetwork.ConstEnum
{
    public class Consts
    {
        public const int TCP_LISTEN_BACKLOG = 10;
        public const int PACKET_HEADER_SIZE = 4;
        public const int MAX_PACKET_SIZE = 64 * 1024;

        public const int TCP_CLIENT_MAX_BUFFER_SIZE = 65535;
        public const int TOTAL_RECV_BUFFER_SIZE = MAX_ONE_RECV_BUFFER_SIZE * 10;
        public const int TOTAL_SEND_BUFFER_SIZE = 1024 * 512;
        public const int MAX_ONE_RECV_BUFFER_SIZE = 4096;

        public const int PACKET_LENGTH_SIZE = 4;
        public const int PACKET_MSG_TYPE_SIZE = 4;
        public const int PACKET_MIN_SIZE = 8;

        public const int TIMEOUT_BACKEND_API_MINUTES = 10;
        public const int DEFAULT_MAX_API_CONNECTION = 100;

        public const string PLAYER_UID_HTTP_HEADER_STRINGKEY = "Player-UID";
        public const string SESSION_ID_HTTP_HEADER_STRINGKEY = "Session-ID";
        public const string MESSAGE_TYPE = "Message-Type";
        public const string HEADER_TRACE_ID = "TRACE-ID";
    }
}
