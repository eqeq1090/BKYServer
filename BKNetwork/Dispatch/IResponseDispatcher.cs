using System;
using BKProtocol;

namespace BKNetwork.Dispatch
{
    public delegate void ResponseMessageHandler<T>(T response, bool success)
    where T : IMsg, new();
    public delegate void ResponseMessageHandler(IMsg response, bool success);

    public class ResponseErrorException : Exception
    {
        public int error { get; private set; }
        public ResponseErrorException(int error, string message)
        : base(message)
        {
            this.error = error;
        }
    }

    class LoadBalancerException : Exception
    {
        public int ProtocolError { get; init; }
        public LoadBalancerException(int errorCode, string message)
        : base(message)
        {
            ProtocolError = errorCode;
        }
    }
}
