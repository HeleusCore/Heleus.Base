using System;
using System.Threading.Tasks;
using Heleus.Base;
using Heleus.Chain;
using Heleus.Cryptography;
using Heleus.Cryptography.Scrypt;

namespace Heleus.Network.Client.Record
{
    public class EncrytpedRecord<T> : IPackable where T : Record
    {
        public readonly SecretKeyInfo KeyInfo;
        public readonly ScryptInput SecretInput;
        public readonly Encryption RecordData;

        T _record;

        EncrytpedRecord(SecretKeyInfo keyInfo, ScryptInput secretInput, Encryption recordData)
        {
            KeyInfo = keyInfo;
            SecretInput = secretInput;
            RecordData = recordData;
        }

        public EncrytpedRecord(Unpacker unpacker)
        {
            KeyInfo = SecretKeyInfo.Restore(unpacker);
            SecretInput = new ScryptInput(unpacker);
            unpacker.Unpack(out byte[] data);
            RecordData = Encryption.Restore(new ArraySegment<byte>(data));
        }

        public async Task<T> GetRecord(SecretKey secretKey)
        {
            if (_record == null)
            {
                try
                {
                    var data = await secretKey.DecryptData(RecordData, SecretInput);
                    using (var unpacker = new Unpacker(data))
                        _record = (T)Activator.CreateInstance(typeof(T), unpacker);
                }
                catch (Exception ex)
                {
                    Log.IgnoreException(ex);
                }
            }

            return _record;
        }

        public static async Task<EncrytpedRecord<T>> EncryptRecord(SecretKey secretKey, T todoRecord)
        {
            (var encryption, var result) = await secretKey.EncrytData(todoRecord.ToByteArray());
            return new EncrytpedRecord<T>(secretKey.KeyInfo, result, encryption);
        }

        public void Pack(Packer packer)
        {
            packer.Pack(KeyInfo);
            packer.Pack(SecretInput);
            packer.Pack(RecordData.Data);
        }

        public byte[] ToByteArray()
        {
            using (var packer = new Packer())
            {
                Pack(packer);
                return packer.ToByteArray();
            }
        }
    }
}
