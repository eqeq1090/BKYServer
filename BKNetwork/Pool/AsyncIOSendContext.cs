using System;
using System.IO;
using System.Net.Sockets;

namespace BKNetwork.Pool
{
    public class AsyncIOSendContext : IAsyncIOContext
    {
        private MemoryStream stream;
        private int sendingCount;
        public BinaryWriter BufferWriter { get; private set; }
        public int SendingSize => (int)stream.Position;
        public int SendingCount => sendingCount;
        public int SendBufferSize { get; private set; }
        public AsyncIOSendContext(int bufferSize)
        {
            SendBufferSize = bufferSize;
            var buffer = GC.AllocateArray<byte>(bufferSize, pinned: true);

            stream = new MemoryStream(buffer, 0, buffer.Length, true, true);
            BufferWriter = new BinaryWriter(stream);
        }

        public byte[] GetBuffer()
        {
            return stream.GetBuffer();
        }

        public void AddSendingCount()
        {
            Interlocked.Increment(ref sendingCount);
        }

        public override void Reset()
        {
            stream.Position = 0;
            sendingCount = 0;
        }

        protected override void Dispose(bool disposing)
        {
            if (m_Disposed == false && disposing == true)
            {
                stream.Dispose();
                BufferWriter.Dispose();
            }
        }
    }
}