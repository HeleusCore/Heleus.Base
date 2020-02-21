using System;
using Heleus.Base;
using Heleus.Cryptography;

namespace Heleus.Chain
{
	public class SignedData : IPackable
    {
        readonly protected byte[] _data;
        readonly Signature _signature;
        bool _signatureValid = false;

        public SignedData(byte[] data, Key signKey)
        {
            _data = data;
            _signature = Signature.Generate(signKey, Hash.Generate(HashTypes.Sha256, data));
        }

        public SignedData(Unpacker unpacker)
        {
            _data = unpacker.UnpackByteArray();
            _signature = unpacker.UnpackSignature();
        }

        public void Pack(Packer packer)
        {
            packer.Pack(_data);
            packer.Pack(_signature);
        }

        public byte[] GetSignedData(Key publicKey)
        {
            if (_data == null || _signature == null)
                return null;

            if (_signatureValid)
                return _data;

            if (_signature.IsValid(publicKey, Hash.Generate(HashTypes.Sha256, _data)))
            {
                _signatureValid = true;
                return _data;
            }

            return null;
        }
    }

    public class SignedData<T> : SignedData where T : IPackable
    {
        readonly T _item;

        static byte[] ToByteArray(IPackable packable)
        {
            using (var packer = new Packer())
            {
                packable.Pack(packer);
                return packer.ToByteArray();
            }
        }

        public SignedData(T item, Key signKey) : base(ToByteArray(item), signKey)
        {
            _item = item;
        }

        public SignedData(Func<Unpacker, T> build, Unpacker unpacker) : base(unpacker)
        {
            _item = build.Invoke(new Unpacker(_data));
        }

        public T GetSignedItem(Key publicKey)
        {
            if (GetSignedData(publicKey) != null)
                return _item;

            return default(T);
        }
    }
}
