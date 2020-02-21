using System;
using System.Collections.Generic;
using System.Linq;

namespace Heleus.Base
{
	public interface IChunkPackable
	{
		void WriteChunks(ChunkWriter writer);
		void ReadChunks(ChunkReader reader);
	}

	public class ChunkInfo : IPackable, IUnpackerKey<string>
	{
        public readonly string ChunkName;
		public readonly int StartPosition;
		public readonly int EndPosition;

        public string UnpackerKey => ChunkName;

        public ChunkInfo(string chunkName, int startPosition, int endPositin)
        {
            ChunkName = chunkName;
            StartPosition = startPosition;
            EndPosition = endPositin;
        }

        public ChunkInfo(Unpacker unpacker)
        {
            unpacker.Unpack(out ChunkName);
            unpacker.Unpack(out StartPosition);
            unpacker.Unpack(out EndPosition);
        }

        public void Pack(Packer packer)
		{
            packer.Pack(ChunkName);
			packer.Pack(StartPosition);
			packer.Pack(EndPosition);
		}
	}

	public class ChunkReader
	{
        readonly Unpacker _unpacker;
        readonly Dictionary<string, ChunkInfo> _chunks = new Dictionary<string, ChunkInfo>();

        public ICollection<string> Chunks => _chunks.Keys;

        public static bool Read(byte[] data, params Action<ChunkReader>[] chunkedPackable)
		{
			try
			{
				using (var packReader = new Unpacker(data))
				{
					return Read(packReader, chunkedPackable);
				}
			}
			catch (Exception ex)
			{
				Log.IgnoreException(ex);
			}

			return false;
		}

		public static bool Read(byte[] data, IChunkPackable chunkedPackable)
		{
			return Read(data, (chunkedReader) => chunkedPackable?.ReadChunks(chunkedReader));
		}

		public static bool Read(Unpacker packReader, params Action<ChunkReader>[] chunkedPackable)
		{
			try
			{
				var chunkedReader = new ChunkReader(packReader);
                foreach(var cp in chunkedPackable)
				    cp.Invoke(chunkedReader);
				return true;
			}
			catch (Exception ex)
			{
				Log.IgnoreException(ex);
			}

			return true;
		}

		public static bool Read(Unpacker packReader, IChunkPackable chunkedPackable)
		{
			return Read(packReader, (chunkedReader) => chunkedPackable?.ReadChunks(chunkedReader));
		}

		protected ChunkReader(Unpacker unpacker)
		{
			_unpacker = unpacker;

			var tablePosition = unpacker.UnpackInt();
			unpacker.Stream.Position = tablePosition;

            unpacker.Unpack(_chunks, (u) => new ChunkInfo(u));
		}

		public bool Read(string name, ref int value)
		{
			int result = 0;
			if (Read(name, (reader) => reader.Unpack(out result)))
			{
				value = result;
				return true;
			}
			return false;
		}

        public bool Read(string name, ref long value)
        {
            long result = 0;
            if (Read(name, (reader) => reader.Unpack(out result)))
            {
                value = result;
                return true;
            }
            return false;
        }

        public bool Read(string name, ref string value)
		{
			string result = null;
			if (Read(name, (reader) => reader.Unpack(out result)))
			{
				value = result;
				return true;
			}
			return false;
		}

		public bool Read(string name, ref bool value)
		{
			bool result = false;
			if (Read(name, (reader) => reader.Unpack(out result)))
			{
				value = result;
				return true;
			}
			return false;
		}

        public bool Read(string name, ref byte[] value)
        {
            byte[] result = null;
            if (Read(name, (reader) => reader.Unpack(out result)))
            {
                value = result;
                return true;
            }
            return false;
        }

        public bool Read(string name, IUnpackable unpackable)
        {
            return Read(name, (reader) => unpackable?.UnPack(reader));
        }

        public bool Read<T>(string name, ref T value) where T : IPackable
		{
            var result = default(T);
            Read(name, (reader) =>
            {
                try
                {
                    result = (T)Activator.CreateInstance(typeof(T), reader);
                }
                catch { }
            });

            value = result;
            return !result.IsNullOrDefault();
        }

		public bool Read(string name, Action<Unpacker> reader)
		{
			try
			{
				if (_chunks.TryGetValue(name, out var chunkInfo))
				{
					_unpacker.Stream.Position = chunkInfo.StartPosition;
					reader?.Invoke(_unpacker);

					return true;
				}
			}
			catch (Exception ex)
			{
				Log.IgnoreException(ex);
			}
			return false;
		}
	}

	public class ChunkWriter
	{
        readonly Packer _packer;
        readonly long _streamStartPosition;
        readonly Dictionary<string, ChunkInfo> _chunks = new Dictionary<string, ChunkInfo>();

        public static byte[] Write(params Action<ChunkWriter>[] chunkedPackable)
		{
			try
			{
				using (var packWriter = new Packer())
				{
					if (Write(packWriter, chunkedPackable))
						return packWriter.ToByteArray();
				}
			}
			catch (Exception ex)
			{
				Log.IgnoreException(ex);
			}

			return null;
		}

		public static byte[] Write(IChunkPackable chunkedPackable)
		{
			return Write((chunkedWriter) => chunkedPackable?.WriteChunks(chunkedWriter));
		}

		public static bool Write(Packer packWriter, params Action<ChunkWriter>[] chunkedPackable)
		{
			try
			{
				var chunkedWriter = new ChunkWriter(packWriter);
                foreach(var cp in chunkedPackable)
				    cp.Invoke(chunkedWriter);
				chunkedWriter.WriteData();
				return true;
			}
			catch (Exception ex)
			{
				Log.IgnoreException(ex);
			}
			return false;
		}

		public static bool Write(Packer packWriter, IChunkPackable chunkedPackable)
		{
			return Write(packWriter, (chunkedWriter) => chunkedPackable?.WriteChunks(chunkedWriter));
		}

		protected ChunkWriter(Packer packWriter)
		{
			_packer = packWriter;
			_streamStartPosition = packWriter.Stream.Position;

			packWriter.Pack((int)0); // table position tmp
		}

		void WriteData()
		{
			var position = _packer.Stream.Position;
			_packer.Stream.Position = _streamStartPosition;
			_packer.Pack((int)position);
			_packer.Stream.Position = position;

			_packer.Pack(_chunks);
		}

		public void Write(string name, bool value)
		{
			Write(name, (writer) => writer.Pack(value));
		}

		public void Write(string name, int value)
		{
			Write(name, (writer) => writer.Pack(value));
		}

        public void Write(string name, long value)
        {
            Write(name, (writer) => writer.Pack(value));
        }

        public void Write(string name, string value)
		{
			Write(name, (writer) => writer.Pack(value));
		}

        public void Write(string name, byte[] value)
        {
            Write(name, (writer) => writer.Pack(value));
        }

        public void Write(string name, IPackable packable)
		{
			Write(name, (writer) => packable?.Pack(writer));
		}

		public void Write(string name, Action<Packer> writer)
		{
			var start = (int)_packer.Stream.Position;
			writer?.Invoke(_packer);
			_chunks[name] =  new ChunkInfo(name, start, (int)_packer.Stream.Position);
		}
	}
}
