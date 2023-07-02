using System;
using System.IO;
using System.Net.Sockets;
using BKNetwork.ConstEnum;

namespace BKNetwork.Pool
{
    public class AsyncIOReceiveContext : IAsyncIOContext, IDisposable
    {
        private MemoryStream stream;
        public BinaryReader BufferReader { get; private set; }
        public long WrittenBytes { get; private set; }
        public long ReadableBytes => WrittenBytes - stream.Position;
        public long RemainBufferSize => stream.Length - WrittenBytes;
        public int ReceiveBufferSize { get; private set; }
        public AsyncIOReceiveContext(int bufferSize)
        {
            var buffer = GC.AllocateArray<byte>(bufferSize, pinned: true);
            stream = new MemoryStream(buffer, 0, buffer.Length, false, true);
            BufferReader = new BinaryReader(stream);
        }

        ~AsyncIOReceiveContext()
        {
            Dispose(false);
        }

        public byte[] GetBuffer()
        {
            return stream.GetBuffer();
        }

        public long Seek(int offset)
        {
            return stream.Seek(offset, SeekOrigin.Current);
        }

        public int Read(byte[] buffer, int count)
        {
            return stream.Read(buffer, 0, count);
        }

        public void RemoveFrontBuffer()
        {
            if (0 >= stream.Position)
            {
                return;
            }

            if (ReadableBytes > 0)
            {
                if (Consts.MAX_ONE_RECV_BUFFER_SIZE >= RemainBufferSize)
                {
                    var buffer = stream.GetBuffer();
                    var offset = (int)stream.Position;
                    var size = ReadableBytes;
                    Array.Copy(buffer, offset, buffer, 0, size);
                    stream.Position = 0;
                    WrittenBytes -= offset;
                }
            }
            else
            {
                stream.Position = 0;
                WrittenBytes = 0;
            }
        }

        public void MarkWritten(int size)
        {
            WrittenBytes += size;
            WrittenBytes = Math.Min(stream.Length, WrittenBytes);
        }

        public override void Reset()
        {
            stream.Position = 0;
            WrittenBytes = 0;
        }

        protected override void Dispose(bool disposing)
        {
            if (m_Disposed == false && disposing == true)
            {
                stream.Dispose();
                BufferReader.Dispose();
            }
        }
    }
}
