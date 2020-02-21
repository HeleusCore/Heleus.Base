using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Heleus.Base
{
    public sealed class PartialStream : Stream
    {
        public override bool CanRead => BaseStream.CanRead;
        public override bool CanSeek => BaseStream.CanSeek;
        public override bool CanWrite => BaseStream.CanWrite;
        public override long Length => BaseCount;

        public override long Position
        {
            get
            {
                return BaseStream.Position - BaseOffset;
            }
            set
            {
                BaseStream.Position = value + BaseOffset;
            }
        }

        public override void Flush()
        {
            BaseStream.Flush();
        }
 
        public override int ReadByte()
        {
            return BaseStream.ReadByte();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (BaseStream.Position >= BaseOffset + BaseCount)
                return 0;

            return BaseStream.Read(buffer, offset, Math.Min((int)(BaseCount - (BaseStream.Position - BaseOffset)), count));
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            BaseStream.Write(buffer, offset, count);
        }

        public override void WriteByte(byte value)
        {
            BaseStream.WriteByte(value);
        }

        public readonly Stream BaseStream;
        public readonly int BaseOffset;
        public readonly int BaseCount;

        public PartialStream(Stream baseStream, int offset, int count)
        {
            BaseStream = baseStream;
            BaseOffset = offset;
            BaseCount = count;

            baseStream.Position = offset;
        }
    }
}
