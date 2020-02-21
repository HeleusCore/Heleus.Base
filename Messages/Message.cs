using System;
using System.Collections.Generic;
using Heleus.Cryptography;
using Heleus.Base;

namespace Heleus.Messages
{
    public abstract class Message
    {
        public static ushort MessageMagic = 0x4C4D;
        public static ushort MessageMagicSigned = 0x6C6D;

        //public const ushort MessageHeaderBytes = 10; // magic, version, size, messagetype
        public const uint MessageMaxSize = 1024 * 512;

        static readonly Dictionary<ushort, Type> _messageTypes = new Dictionary<ushort, Type>();
        public static void RegisterMessage<T>() where T : Message
        {
            var type = typeof(T);
            var m = (T)Activator.CreateInstance(type);
            var messageType = m.MessageType;

            if (_messageTypes.TryGetValue(messageType, out var t))
            {
                if (t != type)
                    throw new Exception("Message type registration mismatch.");
            }

            _messageTypes[messageType] = type;
        }

        public ushort MessageType
        {
            get;
            private set;
        }

        public ushort ProtocolVersion
        {
            get;
            private set;
        }

        public uint Size
        {
            get;
            private set;
        }

        public Signature Signature
        {
            get;
            protected set;
        }

        public Hash SignatureHash
        {
            get;
            protected set;
        }

        Key _key;
        public Key SignKey
        {
            set
            {
                if(value == null)
                {
                    _key = null;
                    return;
                }

                _key = value;
                if (_key.KeyType != Protocol.MessageKeyType)
                    throw new ArgumentException("Key type wrong", nameof(SignKey));

                if (!_key.IsPrivate)
                    throw new ArgumentException("Key is not private", nameof(SignKey));
            }

            get
            {
                return _key;
            }
        }

        public virtual bool IsMessageValid(Key key)
        {
            if (key != null && Signature != null && key.KeyType != Signature.KeyType)
                throw new ArgumentException("Key type wrong", nameof(key));

            return (key != null && Signature != null && SignatureHash != null) && Signature.IsValid(key, SignatureHash);
        }

        protected Message(ushort messageType)
        {
            MessageType = messageType;
        }

        public int Store(Packer packer, ushort version)
        {
            var startPosition = packer.Position;
            
            var sign = _key != null;

            if(sign)
                packer.Pack(MessageMagicSigned);
            else
                packer.Pack(MessageMagic);

            packer.Pack(version);

            var sizePosition = packer.Position;
            packer.Pack((uint)0); // messageSize dummy
            packer.Pack(MessageType);

            packer.Pack(Pack);

            var messageSize = packer.Position - startPosition;
            if(sign)
            {
                var signatureSize = Signature.GetSignatureBytes(Protocol.MessageKeyType);
                messageSize += signatureSize;
            }

            if (messageSize > MessageMaxSize)
                throw new Exception("Invalid message size");

            Size = (uint)messageSize;
            packer.Position = sizePosition;
            packer.Pack((uint)messageSize); // update size

            if (sign)
            {
                (SignatureHash, Signature) = packer.AddSignature(_key, startPosition, messageSize);
				messageSize += PostSignaturePacked(packer, messageSize);
            }

            return messageSize;
        }

		protected virtual int PostSignaturePacked(Packer packer, int messageSize)
		{
			return 0;
		}

		protected virtual void PostSignatureUnpacked(Unpacker unpacker, int messageSize)
        {
        }

        public static Message Restore(Unpacker unpacker)
        {
            return Restore<Message>(unpacker);
        }

        public static T Restore<T>(Unpacker unpacker) where T : Message
        {
            var startPosition = unpacker.Position;

            unpacker.Unpack(out ushort Magic);

            var signed = false;
            if (Magic == MessageMagicSigned)
                signed = true;
            else if (Magic != MessageMagic)
                throw new Exception("Invalid message");

            unpacker.Unpack(out ushort protocolVersion);
            unpacker.Unpack(out uint size);
            unpacker.Unpack(out ushort messageType);

            if (_messageTypes.TryGetValue(messageType, out Type type))
            {
                var m = (T)Activator.CreateInstance(type);

                m.ProtocolVersion = protocolVersion;
                m.Size = size;
                unpacker.Unpack(m.Unpack);

                if (signed)
                {
                    var dataSize = unpacker.Position - startPosition;
                    (m.SignatureHash, m.Signature) = unpacker.GetHashAndSignature(startPosition, dataSize);
					m.PostSignatureUnpacked(unpacker, (int)size);
                }
                return m;
            }

            Log.Fatal($"Could not restore message with message type {messageType}");

            return null;
        }

        protected virtual void Pack(Packer packer)
        {
            
        }

        protected virtual void Unpack(Unpacker unpacker)
        {

        }

		byte[] _arrayData;

		public byte[] ToByteArray(bool save = false)
        {
			if (_arrayData != null)
				return _arrayData;
			
            using(var packer = new Packer())
            {
                Store(packer, Protocol.Version);
				var data = packer.ToByteArray();
				if (save)
					_arrayData = data;
				return data;
            }
        }
    }
}
