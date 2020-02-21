using System;
using Heleus.Base;
using Heleus.Cryptography;
using Heleus.Cryptography.Scrypt;

namespace Heleus.Chain
{
    public class ExportedSecretKey : IPackable
    {
        public readonly SecretKeyInfo KeyInfo;
        //public readonly ScryptInput SecretInfo;

        readonly Encryption _encryption;
        byte[] _secretHash;

        public ExportedSecretKey(SecretKeyInfo keyInfo, /*ScryptInput secretInfo,*/ Encryption encryption)
        {
            KeyInfo = keyInfo;
            //SecretInfo = secretInfo;

            _encryption = encryption;
        }

        public ExportedSecretKey(string hexString) : this(new Unpacker(Hex.ByteArrayFromCrcString(hexString)))
        {

        }

        public ExportedSecretKey(Unpacker unpacker)
        {
            KeyInfo = SecretKeyInfo.Restore(unpacker);
            //SecretInfo = new ScryptInput(unpacker);
            _encryption = Encryption.Restore(new ArraySegment<byte>(unpacker.UnpackByteArray()));
        }

        public SecretKey Decrypt(string password)
        {
            if (_secretHash != null)
                return new SecretKey(KeyInfo, /*SecretInfo,*/ _secretHash);

            try
            {
                _secretHash = _encryption.Decrypt(password);
            }
            catch (Exception ex)
            {
                Log.IgnoreException(ex);
            }

            if (_secretHash != null)
                return new SecretKey(KeyInfo, /*SecretInfo,*/ _secretHash);

            return null;
        }

        public void Pack(Packer packer)
        {
            packer.Pack(KeyInfo);
            //packer.Pack(SecretInfo);
            packer.Pack(_encryption.Data);
        }

        public byte[] ToByteArray()
        {
            using (var packer = new Packer())
            {
                Pack(packer);
                return packer.ToByteArray();
            }
        }

        public string HexString
        {
            get
            {
                var data = ToByteArray();
                return Hex.ToCrcString(data);
            }
        }
    }
}
