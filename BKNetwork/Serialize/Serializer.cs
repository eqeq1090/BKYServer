using MessagePack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using BKServerBase.Logger;
using BKServerBase.Threading;
using BKProtocol;

namespace BKNetwork.Serialize
{     
    public static class Serializer
    {
        public static byte[] Serialize<T>(T msg)
            where T : IMsg
        {
            try
            {
                return MessagePackSerializer.Serialize(msg);
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        public static T Deserialize<T>(Stream stream)
            where T : IMsg, new()
        {
            try
            {
                return MessagePackSerializer.Deserialize<T>(stream);
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        public static T Deserialize<T>(string jsonBody)
            where T : IMsg, new()
        {
            try
            {
                var result = JsonConvert.DeserializeObject<T>(jsonBody);
                if (result == null)
                {
                    CoreLog.Critical.LogWarning($"Json Deserialize Failed. message body: {jsonBody}, message Type : {new T().msgType}");
                    return new T();
                }
                return result;
                //var msg = MessagePackSerializer.ConvertFromJson(jsonBody, m_Options);
                //return MessagePackSerializer.Deserialize<T>(msg, m_Options);
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        public static void Load(BinaryReader binReader, ref bool boolValue)
        {
            boolValue = binReader.ReadBoolean();
        }

        public static void Load(BinaryReader binReader, ref byte nValue)
        {
            nValue = binReader.ReadByte();
        }
        public static void Load(BinaryReader binReader, ref sbyte nValue)
        {
            nValue = binReader.ReadSByte();
        }

        public static void Load(BinaryReader binReader, ref ushort nValue)
        {
            nValue = binReader.ReadUInt16();
        }

        public static void Load(BinaryReader binReader, ref short nValue)
        {
            nValue = binReader.ReadInt16();
        }

        public static void Load(BinaryReader reader, ref uint uintValue)
        {
            uintValue = reader.ReadUInt32();
        }
        public static void Load(BinaryReader reader, ref int intValue)
        {
            intValue = reader.ReadInt32();
        }
        public static void Load(BinaryReader binReader, ref ulong nValue)
        {
            nValue = binReader.ReadUInt64();
        }
        public static void Load(BinaryReader binReader, ref long nValue)
        {
            nValue = binReader.ReadInt64();
        }
        public static void Load(BinaryReader binReader, ref double nValue)
        {
            nValue = binReader.ReadDouble();
        }
        public static void Load(BinaryReader binReader, ref float nValue)
        {
            nValue = binReader.ReadSingle();
        }
        public static void Load(BinaryReader binReader, ref char nValue)
        {
            nValue = binReader.ReadChar();
        }

        public static T Deserialize<T>(byte[] bytes)
            where T : IMsg, new()
        {
            try
            {
                return MessagePackSerializer.Deserialize<T>(bytes);
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }
    }
}
