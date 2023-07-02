using System.IO;
using BKGameServerComponent.Actor;
using BKNetwork.Interface;
using BKProtocol;

namespace BKGameServerComponent.MsgRegister
{
    internal delegate void OnPubsubDispatchEventHandler<T>(Player player, T msg)
        where T : IPubsubMsg, new();
    internal delegate void OnPubsubPreDispatchEventHandler(Player player, IPubsubMsg msg);

    internal interface IPubsubDispatcher
    {
        bool Fetch(Player player, IPubsubMsg msg);
    }
}
