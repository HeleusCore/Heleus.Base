using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Heleus.Base;
using Heleus.Cryptography;
using Heleus.Cryptography.Scrypt;

namespace Heleus.Chain
{
    public class SecretKey : IPackable
    {
        public SecretKeyInfoTypes SecretKeyInfoType => KeyInfo.SecretKeyInfoType;
        public long Timestamp => KeyInfo.Timestamp;

        public readonly SecretKeyInfo KeyInfo;
        //public readonly ScryptInput SecretInfo;
        public readonly byte[] SecretHash;

        public T GetKeyInfo<T>() where T : SecretKeyInfo => KeyInfo as T;

        public SecretKey(Unpacker unpacker)
        {
            KeyInfo = SecretKeyInfo.Restore(unpacker);
            //SecretInfo = new ScryptInput(unpacker);
            unpacker.Unpack(out SecretHash);
        }

        SecretKey(SecretKeyInfo keyInfo, ScryptResult secret)
        {
            KeyInfo = keyInfo;
            //SecretInfo = secret.Input;
            SecretHash = secret.Hash;
        }

        public SecretKey(SecretKeyInfo keyInfo, /*ScryptInput scryptInfo,*/ byte[] secretHash)
        {
            KeyInfo = keyInfo;
            //SecretInfo = scryptInfo;
            SecretHash = secretHash;
        }

        public void Pack(Packer packer)
        {
            packer.Pack(KeyInfo);
            //packer.Pack(SecretInfo);
            packer.Pack(SecretHash);
        }

        public byte[] ToByteArray()
        {
            using(var packer = new Packer())
            {
                Pack(packer);

                return packer.ToByteArray();
            }
        }

        public ExportedSecretKey ExportSecretKey(string password)
        {
            var enc = Encryption.GenerateAes256(new ArraySegment<byte>(SecretHash), password);
            return new ExportedSecretKey(KeyInfo, /*SecretInfo,*/ enc);
        }

        public static int GetHashCode(byte[] array)
        {
            unchecked
            {
                const int p = 16777619;
                int hash = (int)2166136261;

                for (int i = 0; i < array.Length; i++)
                    hash = (hash ^ array[i]) * p;

                hash += hash << 13;
                hash ^= hash >> 7;
                hash += hash << 3;
                hash ^= hash >> 17;
                hash += hash << 5;
                return hash;
            }
        }

        public static Task<SecretKey> NewSecretKey(SecretKeyInfo keyInfo, string password)
        {
            return Task.Run(() =>
            {
                var pw = Encoding.UTF8.GetBytes(password);
                var iterations = Math.Abs(GetHashCode(pw)) % 100000;

            retry:

                var sha512 = new SHA512Managed();

                var hash = sha512.ComputeHash(pw);
                for (var i = 0; i < iterations; i++)
                    hash = sha512.ComputeHash(hash);

                var control = sha512.ComputeHash(pw);
                for (var i = 0; i < iterations; i++)
                    control = sha512.ComputeHash(control);

                if (!hash.SequenceEqual(control))
                    goto retry;

                var data = new byte[32];
                var salt = new byte[32];

                Buffer.BlockCopy(hash, 0, data, 0, 32);
                Buffer.BlockCopy(hash, 32, salt, 0, 32);

                return NewSecretKey(keyInfo, data, salt);
            });
        }

        public static Task<SecretKey> NewSecretKey(SecretKeyInfo keyInfo, byte[] data, byte[] salt)
        {
            return Task.Run(() =>
            {
                // scrypt the data
                var result = ScryptEncoder.Encode(data, salt);

                // get 16 bytes of the result and scyrpt it again
                var idData = new byte[16];
                Buffer.BlockCopy(result.Hash, 8, idData, 0, idData.Length);
                var idResult = ScryptEncoder.Encode(idData, result.IterationCount, result.BlockSize, result.ThreadCount, result.Salt);
                // use 8 bytes for id
                var id = BitConverter.ToUInt64(idResult.Hash, idData.Length);

                keyInfo.SecretId = id;
                return new SecretKey(keyInfo, result);
            });
        }

        public static Task<SecretKey> NewSecretKey(SecretKeyInfo keyInfo, byte[] data)
        {
            return Task.Run(() =>
            {
                // scrypt the data
                var result = ScryptEncoder.Encode(data);

                // get 16 bytes of the result and scyrpt it again
                var idData = new byte[16];
                Buffer.BlockCopy(result.Hash, 8, idData, 0, idData.Length);
                var idResult = ScryptEncoder.Encode(idData, result.IterationCount, result.BlockSize, result.ThreadCount, result.Salt);
                // use 8 bytes for id
                var id = BitConverter.ToUInt64(idResult.Hash, idData.Length);

                keyInfo.SecretId = id;
                return new SecretKey(keyInfo, result);
            });
        }

        public Task<(Encryption, ScryptInput)> EncrytData(byte[] data)
        {
            return EncrytData(new ArraySegment<byte>(data));
        }

        public Task<(Encryption, ScryptInput)> EncrytData(ArraySegment<byte> data)
        {
            return Task.Run(() =>
            {
                try
                {
                    var pw = ScryptEncoder.Encode(SecretHash);
                    var enc = Encryption.Generate(EncryptionTypes.Aes256, data, pw.Hash);

                    return (enc, pw.Input);
                }
                catch (Exception ex)
                {
                    Log.IgnoreException(ex);
                }

                return (null, null);
            });
        }

        public Task<byte[]> DecryptData(Encryption encryptedData, ScryptInput secretInfo)
        {
            return Task.Run(() =>
            {
                try
                {
                    var decpw = ScryptEncoder.Encode(SecretHash, secretInfo);
                    return encryptedData.Decrypt(decpw.Hash);
                }
                catch { }

                return null;
            });
        }
    }
}
