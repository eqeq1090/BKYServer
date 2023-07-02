using System.Buffers;
using System.Linq.Expressions;
using System.Net;

namespace BKNetwork.Serialize
{
    public struct CompositedLinkHeader
    {
        public const uint MessageID = 0;
        public const int MessageSize = sizeof(uint) + sizeof(uint) + sizeof(byte);

        [Flags]
        public enum PacketFlags
        {
            None = 0b00000000,
            LZ4Compressed = 0500000001,
            DeflateCompressed = 0b00000010,
            AESEncrypted = 0b00000100,
        }

        public uint Length;
        // uint MessageID;
        public PacketFlags Flags;
        // [link packets]
        public bool Load(byte[] source, int count)
        {
            if (count < MessageSize)
            {
                return false;
            }
            using var stream = new MemoryStream(source, 0, count, false);
            using var reader = new BinaryReader(stream);
            Length = reader.ReadUInt32();
            if (reader.ReadUInt32() != (uint)IPAddress.NetworkToHostOrder((int)MessageID))
            {
                return false;
            }
            Flags = (PacketFlags)reader.ReadByte();
            return true;
        }

        public void Save(BinaryWriter writer)
        {
            writer.Write(Length);
            writer.Write(IPAddress.HostToNetworkOrder((int)MessageID));
            writer.Write((byte)Flags);
        }
    }
}
