using Newtonsoft.Json;
using Prometheus;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using BKServerBase.Logger;
using BKServerBase.Threading;
using BKNetwork.ConstEnum;
using BKNetwork.Pool;
using BKNetwork.Serialize;
using BKNetwork.Sock.Detail;
using BKProtocol;
using BKProtocol.Enum;

namespace BKNetwork.Sock.TCP
{
    public class TCPAcceptSocket : ISocket, IDisposable
    {
        private static readonly IReadOnlyDictionary<ePrometheusGaugeKind, Gauge.Child> m_PrometheusGauges;

        private readonly ConcurrentQueue<PacketRequestInfo> m_SendQueue = new ConcurrentQueue<PacketRequestInfo>();
        private AsyncIOReceiveContext m_ReceiveBufferContext;
        private AsyncIOSendContext m_SendBufferContext;
        private int m_SendQueueCount;

        protected OnSendPacket m_OnSend;
        protected OnRecvPacket m_OnRecv;
        protected OnTCPClientClose m_OnClose;
        protected SocketAsyncEventArgs m_SendArgs = new SocketAsyncEventArgs();
        protected SocketAsyncEventArgs m_RecvArgs = new SocketAsyncEventArgs();
        protected AtomicFlag m_Running = new AtomicFlag(true);
        protected AtomicFlag m_Sending = new AtomicFlag(false);
        protected ReferenceCount m_RefCount;
        protected AtomicFlag m_ClosePending = new AtomicFlag(false);

        static TCPAcceptSocket()
        {
            var gauges = new Dictionary<ePrometheusGaugeKind, Gauge.Child>();
            foreach (ePrometheusGaugeKind kind in Enum.GetValues(typeof(ePrometheusGaugeKind)))
            {
                var gauge = Metrics.CreateGauge($"socket_io_counter", "Socket I0 Counter", "kind").WithLabels(kind.ToString());
                gauges.Add(kind, gauge);
            }
            m_PrometheusGauges = gauges;
        }

        public TCPAcceptSocket(Socket socket, STSocketOption option, OnSendPacket onSend, OnRecvPacket onRecv, OnTCPClientClose onClose, OnSocketError onSocketError)
            : base(socket, option, onSocketError)
        {
            m_OnSend += onSend;
            m_OnRecv += onRecv;
            m_OnClose += onClose;
            SetSocketOption(option);
            m_SendArgs.Completed += SendCallback;
            m_RecvArgs.Completed += ReceiveCallback;
            m_RefCount = new ReferenceCount(zeroRefCallback: Dispose);
            m_RefCount.Increment(RefCountReason.Instantiate);
            m_ReceiveBufferContext = new AsyncIOReceiveContext(option.RecvBufferSize);
            m_SendBufferContext = new AsyncIOSendContext(option.SendBufferSize);
            m_ClosePending.Off();
            m_Running.On();
        }

        public IPEndPoint? GetRemoteEndPoint()
        {
            return m_Socket.RemoteEndPoint as IPEndPoint;
        }

        public bool IsRunning()
        {
            return m_Running.IsOn && 
                m_Disposed == false && 
                m_Socket.Connected == true && 
                m_ClosePending.IsOn == false;
        }
        public void Close(DisconnectReason reason)
        {
            m_ClosePending.On();
        }

        public void StartReceive()
        {
            BeginReceive();
        }

        public void ReceiveCallback(object? sender, SocketAsyncEventArgs e)
        {
            try
            {
                int byteRead = e.BytesTransferred;
                IncrementPrometheusGauge(ePrometheusGaugeKind.RecvBytes, byteRead);
                if (e.SocketError != SocketError.Success || byteRead <= 0)
                {
                    m_OnSocketError.Invoke(e: e.SocketError);
                    CloseInternal(DisconnectReason.ByClient);
                    return;
                }
                m_ReceiveBufferContext.MarkWritten(byteRead);
                if (OnReceive() == false)
                {
                    m_OnSocketError.Invoke(e: e.SocketError);
                    CloseInternal(DisconnectReason.SessionError);
                    return;
                }
                BeginReceive();
            }
            catch (Exception ex)
            {
                m_OnSocketError(ex);
                CloseInternal(DisconnectReason.Undefined);
            }
        }
        public bool OnReceive()
        {
            long totalReceivedStreamLength = m_ReceiveBufferContext.ReadableBytes;
            while (0 < totalReceivedStreamLength)
            {
                if (Consts.PACKET_LENGTH_SIZE > totalReceivedStreamLength)
                {
                    break;
                }

                int packetSize = 0;
                Serializer.Load(m_ReceiveBufferContext.BufferReader, ref packetSize);
                if (totalReceivedStreamLength < packetSize)
                {
                    m_ReceiveBufferContext.Seek(-Consts.PACKET_LENGTH_SIZE);
                    break;
                }
                if (packetSize < Consts.PACKET_LENGTH_SIZE)
                {
                    CoreLog.Critical.LogWarning($"(Context) Invalid Message Size - (packetSize)");
                    totalReceivedStreamLength -= Consts.PACKET_LENGTH_SIZE;
                    continue;
                }
                m_ReceiveBufferContext.Seek(-Consts.PACKET_LENGTH_SIZE);
                byte[] packet = new byte[packetSize];
                m_ReceiveBufferContext.Read(packet, packetSize);
                totalReceivedStreamLength -= packetSize;
                using var stream = new MemoryStream(packet, 0, packet.Length, false, true);
                m_OnRecv(stream);
            }
            m_ReceiveBufferContext.RemoveFrontBuffer();
            return true;
        }
        public override SendResult Send(IMsg[] msgs)
        {
            if (msgs.Length == 0)
            {
                return SendResult.InvalidSize;
            }
            if (IsRunning() == false)
            {
                CloseInternal(DisconnectReason.None);
                return SendResult.NotConnect;
            }

            //int sendQueueCount = Interlocked.Add(ref m_SendQueueCount, msgs.Length);
            //var sendQueueSize = Interlocked.Add(ref m_SendQueueSize, msgs.Sum(x=>x.Length));

            for (int i = 0; i < msgs.Length; i++)
            {
                var msg = msgs[i];
                byte[]? buffer;

                if (msg.ParsingType == ParsingType.Json)
                {
                    var json = JsonConvert.SerializeObject(msg);
                    buffer = Encoding.UTF8.GetBytes(json);
                }
                else
                {
                    buffer = MsgPairGenerator.Instance.Serialize(msg);
                }

                var totalSize = buffer.Length + sizeof(int) + sizeof(int);
                var packetRequestInfo = new PacketRequestInfo(buffer, totalSize, msg.msgType);
                m_SendQueue.Enqueue(packetRequestInfo);
            }

            //NOTE 추후 RateLimit를 걸때 체크하자.
            /*
            if (sendQueueCount > ConfigurationManager.Instance.ResSendLimitConf.MaxSendQueueCount ||
                sendQueueSize > ConfigurationManager.Instance.ResSendLimitConf.MaxSendQueueSize)
            {
                var queuedMsg = m_SendQueue.GroupBy(pair => pair.Item1.MsgName).Select(group => (group.Key, group.Count()));
                CoreLog.Critical.LogWarning($"Reached max sendqueue size! user : {Context!.GetUserUID()} count : {sendQueueCount}, size : {sendQueueSize} " +
                    $"msg : {JsonConvert.SerializeObject(queuedMsg)}");
                BeginCloseImmediately(3);
                return SendLinkResult.QueueOverflowed;
            }
            */
            BeginSend();

            return SendResult.Success;
        }
        public void SendCallback(object? sender, SocketAsyncEventArgs arg)
        {
            int byteWritten = arg.BytesTransferred;

            IncrementPrometheusGauge(ePrometheusGaugeKind.SendBytes, byteWritten);
            IncrementPrometheusGauge(ePrometheusGaugeKind.SendCallbackCount, 1);
            try
            {
                if (arg.SocketError != SocketError.Success)
                {
                    CoreLog.Normal.LogInfo($"SendCallback SocketError!" +
                    $"user : (Context!.GetUserUID(), " +
                    $"error : (arg.SocketError)");
                    CloseInternal(DisconnectReason.SessionError);
                    return;
                }
                if (byteWritten != m_SendBufferContext.SendingSize)
                {
                    CoreLog.Critical.LogFatal($"SendCallback Error! operation complete without transferring all bytes!" +
                    $"byteWritten : {byteWritten} / sendingSize : {m_SendBufferContext.SendingSize}");
                }
                var sendedCount = m_SendBufferContext.SendingCount;
                m_SendBufferContext.Reset();
                m_Sending.Off();
                m_OnSend(byteWritten);
                if (Interlocked.Add(ref m_SendQueueCount, -sendedCount) == 0)
                {
                    if (m_ClosePending.IsOn == true)
                    {
                        CloseInternal(DisconnectReason.ByServer);
                    }
                    return;
                }
                BeginSend();
            }
            catch (Exception ex)
            {
                m_Sending.Off();
                CoreLog.Critical.LogWarning(ex);
                CloseInternal(DisconnectReason.Undefined);
            }
            finally
            {
                m_RefCount.Decrement(RefCountReason.SocketSend);
            }
        }
        public override void SetSocketOption(STSocketOption option)
        {
            
        }
        public override void Dispose()
        {
            if (m_Disposed == true)
            {
                return;
            }
            Dispose(true);
            m_Disposed = true;
            GC.SuppressFinalize(this);
        }

        public void CloseInternal(DisconnectReason reason)
        {
            try
            {
                if (m_Running.IsOn == false)
                {
                    return;
                }
                m_RefCount.Increment(RefCountReason.SocketDisconnect);
                m_Running.Off();
                m_Socket.Shutdown(SocketShutdown.Both);

                var args = new SocketAsyncEventArgs();
                args.UserToken = reason;
                args.Completed += OnCloseComplete;
                args.DisconnectReuseSocket = true;
                if (m_Socket.DisconnectAsync(args) == false)
                {
                    OnCloseComplete(null, args);
                }
            }
            catch (Exception ex)
            {
                CoreLog.Normal.LogInfo($"Disconnect Error! " +
                //$"user : {Context!.GetUserUID()}," +
                $"message : {ex.Message}");
                m_RefCount.Decrement(RefCountReason.SocketDisconnect);
                m_RefCount.Decrement(RefCountReason.Instantiate);
            }
        }
        protected void BeginReceive()
        {
            try
            {
                if (m_Running.IsOn == false)
                {
                    return;
                }

                m_RefCount.Increment(RefCountReason.SocketReceive);
                IncrementPrometheusGauge(ePrometheusGaugeKind.RecvCount, 1);

                int offset = (int)m_ReceiveBufferContext.WrittenBytes;
                int size = Math.Min((int)m_ReceiveBufferContext.RemainBufferSize, Consts.MAX_ONE_RECV_BUFFER_SIZE);

                m_RecvArgs.SetBuffer(m_ReceiveBufferContext.GetBuffer(), offset, size);
                if (m_Socket.ReceiveAsync(m_RecvArgs) == false)
                {
                    ReceiveCallback(null, m_RecvArgs);
                }
            }
            catch (Exception ex)
            {
                //NOTE Context 기반으로 에러를 넘겨야 한다면 보강을 해야될수도
                m_OnSocketError(ex);
                CloseInternal(DisconnectReason.Undefined);
            }
        }
        protected override void Dispose(bool isDisposing)
        {
            if (m_Disposed == true || isDisposing == false)
            {
                return;
            }
            m_SendBufferContext.Dispose();
            m_ReceiveBufferContext.Dispose();
            m_RecvArgs.Dispose();
            m_SendArgs.Dispose();
        }
        private void BeginSend()
        {
            try
            {
                if (m_Running.IsOn == false || m_Disposed == true || m_Sending.On() == false)
                {
                    return;
                }
                m_RefCount.Increment(RefCountReason.SocketSend);
                FillSendBuffer();
                IncrementPrometheusGauge(ePrometheusGaugeKind.SendCount, 1);
                m_SendArgs.SetBuffer(m_SendBufferContext.GetBuffer(), 0, m_SendBufferContext.SendingSize);
                if (m_Socket.SendAsync(m_SendArgs) == false)
                {
                    SendCallback(null, m_SendArgs);
                }

            }
            catch (Exception ex)
            {
                m_Sending.Off();
                CoreLog.Critical.LogWarning(ex);
                CloseInternal(DisconnectReason.Undefined);
            }
        }
        private void FillSendBuffer()
        {
            while (m_SendQueue.TryDequeue(out var packetRequestInfo))
            {
                if ((m_SendBufferContext.SendingSize + packetRequestInfo.TotalSize) > Consts.TOTAL_SEND_BUFFER_SIZE)
                {
                    break;
                }

                m_SendBufferContext.BufferWriter.Write(packetRequestInfo.TotalSize);
                m_SendBufferContext.BufferWriter.Write((int)packetRequestInfo.MessageType);
                m_SendBufferContext.BufferWriter.Write(packetRequestInfo.MessageBuffers, 0, packetRequestInfo.MessageBuffers.Length);
                m_SendBufferContext.AddSendingCount();

                Interlocked.Increment(ref m_SendQueueCount);
            }

        }
        private void IncrementPrometheusGauge(ePrometheusGaugeKind kind, double value)
        {
            if (m_PrometheusGauges.TryGetValue(kind, out var gauge) == false)
            {
                return;
            }
            gauge.Inc(value);
        }
        private void OnCloseComplete(object? sender, SocketAsyncEventArgs e)
        {
            var reason = (DisconnectReason)e.UserToken!;

            e.Completed -= OnCloseComplete;
            e.Dispose();

            m_RefCount.Decrement(RefCountReason.SocketDisconnect);
            m_RefCount.Decrement(RefCountReason.Instantiate);

            m_OnClose?.Invoke(reason);
        }
    }
}
