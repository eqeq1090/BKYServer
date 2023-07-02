using MessagePack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Reflection;
using BKServerBase.Logger;
using BKServerBase.Threading;
using BKProtocol;
using BKProtocol.Enum;

namespace BKNetwork.Serialize
{
    public abstract class IMsgTypePair
    {
        public abstract byte[] Serialize(IMsg msg);

        public abstract IMsg Deserialize(byte[] bytes);

        public abstract IMsg Deserialize(string jsonBody);
    }

    public class MsgTypePair<T> : IMsgTypePair
        where T : IMsg, new()
    {
        public override IMsg Deserialize(byte[] bytes)
        {
            return Serializer.Deserialize<T>(bytes);
        }

        public override IMsg Deserialize(string jsonBody)
        {
            return Serializer.Deserialize<T>(jsonBody);
        }

        public override byte[] Serialize(IMsg msg)
        {
            return SerializeInternal((msg as T)!);
        }

        private byte[] SerializeInternal(T msg)
        {
            return Serializer.Serialize(msg);
        }
    }

    public class MsgPairGenerator : BaseSingleton<MsgPairGenerator>
    {
        private Dictionary<MsgType, IMsgTypePair> PairDict = new Dictionary<MsgType, IMsgTypePair>();

        private MsgPairGenerator()
        {
        }

        public void Init()
        {
            var assemblyName = typeof(MsgType).Assembly.GetName().Name;

            var assembly = Assembly.Load(assemblyName!);
            var types = assembly.GetTypes();
            if (types == null)
            {
                return;
            }

            foreach (var type in types)
            {
                if (type.IsClass is false)
                {
                    continue;
                }

                if (type.IsAssignableTo(typeof(IMsg)) is false)
                {
                    continue;
                }

                var defaultConstrutorInfo = type.GetConstructor(new Type[0]);
                if (defaultConstrutorInfo is null)
                {
                    continue;
                }

                var msg = Activator.CreateInstance(type) as IMsg;
                if (msg is null)
                {
                    continue;
                }

                var genClass = typeof(MsgTypePair<>).MakeGenericType(type!);

                
                if (PairDict.TryAdd(msg.msgType, (Activator.CreateInstance(genClass) as IMsgTypePair)!) is false)
                {
                    throw new Exception($"MsgPairGenerator failed, msgType is duplicated, msgType: {msg.msgType}");
                }
            }
        }

        public bool Exist(MsgType type, Type messageRealType)
        {
            if (PairDict.TryGetValue(type, out var pair) == false)
            {
                return false;
            }
            return pair.GetType().GetGenericArguments()[0] == messageRealType;

        }

        public byte[] Serialize(IMsg msg)
        {
            if (PairDict.TryGetValue(msg.msgType, out var genInfo) == true)
            {
                return genInfo.Serialize(msg);
            }
            throw new Exception("Msg Pair Dict not found");
        }

        public IMsg Deserialize(MsgType type, byte[] bytes)
        {
            if (PairDict.TryGetValue(type, out var genInfo) == true)
            {
                return genInfo.Deserialize(bytes);
            }
            return new IMsg(MsgType.Invalid);
        }

        public IMsg Deserialize(MsgType type, string jsonBody)
        {
            if (PairDict.TryGetValue(type, out var genInfo) == true)
            {
                return genInfo.Deserialize(jsonBody);
            }
            return new IMsg(MsgType.Invalid);
        }
    }
}
