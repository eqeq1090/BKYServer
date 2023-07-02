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
    public abstract class IPubsubMsgTypePair
    {
        public abstract string Serialize(IPubsubMsg msg);

        public abstract IPubsubMsg Deserialize(string jsonBody);
    }

    public class PubsubMsgTypePair<T> : IPubsubMsgTypePair
        where T : IPubsubMsg, new()
    {
        public override IPubsubMsg Deserialize(string jsonBody)
        {
            var result = JsonConvert.DeserializeObject<T>(jsonBody);
            if (result == null)
            {
                //ERROR
                return new IPubsubMsg(BKProtocol.Enum.PubsubMsgType.Invalid);
            }
            return result;
        }

        public override string Serialize(IPubsubMsg msg)
        {
            return SerializeInternal((msg as T)!);
        }

        private string SerializeInternal(T msg)
        {
            return JsonConvert.SerializeObject(msg);
        }
    }

    public class PubsubMsgPairGenerator : BaseSingleton<PubsubMsgPairGenerator>
    {
        private Dictionary<PubsubMsgType, IPubsubMsgTypePair> PairDict = new Dictionary<PubsubMsgType, IPubsubMsgTypePair>();

        private PubsubMsgPairGenerator()
        {
            
        }

        public void Initialize()
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

                if (type.IsAssignableTo(typeof(IPubsubMsg)) is false)
                {
                    continue;
                }

                var defaultConstrutorInfo = type.GetConstructor(new Type[0]);
                if (defaultConstrutorInfo is null)
                {
                    continue;
                }

                var msg = Activator.CreateInstance(type) as IPubsubMsg;
                if (msg is null)
                {
                    continue;
                }

                var genClass = typeof(PubsubMsgTypePair<>).MakeGenericType(type!);
                PairDict.Add(msg.MsgType, (Activator.CreateInstance(genClass) as IPubsubMsgTypePair)!);
            }
        }

        public string Serialize(IPubsubMsg msg)
        {
            if (PairDict.TryGetValue(msg.MsgType, out var genInfo) == true)
            {
                return genInfo.Serialize(msg);
            }
            //ERROR
            return new string("");
        }

        public IPubsubMsg Deserialize(PubsubMsgType type, string jsonBody)
        {
            if (PairDict.TryGetValue(type, out var genInfo) == true)
            {
                return genInfo.Deserialize(jsonBody);
            }
            //ERROR
            return new IPubsubMsg(PubsubMsgType.Invalid);
        }
    }
}
