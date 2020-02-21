using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Heleus.Base;

namespace Heleus.Cryptography
{
    public partial class Encryption
    {
        class Aes256Encryption : Encryption
        {
            public const byte DATA_OFFSET = PADDING_BYTES + HEADER_LENGTH;
            public const byte HEADER_LENGTH = 18;

            public ushort Iterations
            {
                get;
                private set;
            }

            [ThreadStatic] static Aes _aes;
            [ThreadStatic] static SHA256 _sha256;
            [ThreadStatic] static byte[] _ivBuffer;

            public Aes256Encryption(ArraySegment<byte> data, byte[] password) : base(EncryptionTypes.Aes256)
            {
                if (_aes == null)
                {
                    _aes = new AesManaged
                    {
                        KeySize = 256
                    };
                }

                Iterations = (ushort)Rand.NextInt(20000, 40000);
                _aes.Key = GetPasswordKey(password, Iterations);
                _aes.GenerateIV();

                var encryptor = _aes.CreateEncryptor(_aes.Key, _aes.IV);
                using (var memoryStream = new MemoryStream())
                {
                    memoryStream.WriteByte((byte)EncryptionTypes.Aes256);
                    memoryStream.WriteByte(0);
                    memoryStream.WriteByte(0);

                    var iter = BitConverter.GetBytes(Iterations);
                    if (!BitConverter.IsLittleEndian)
                        Array.Reverse(iter);

                    memoryStream.WriteByte(iter[0]);
                    memoryStream.WriteByte(iter[1]);

                    memoryStream.Write(_aes.IV, 0, _aes.IV.Length);

                    using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                    {
                        var length = BitConverter.GetBytes((ushort)data.Count);
                        if (!BitConverter.IsLittleEndian)
                            Array.Reverse(length);

                        cryptoStream.WriteByte(length[0]);
                        cryptoStream.WriteByte(length[1]);

                        cryptoStream.Write(data.Array, data.Offset, data.Count);
                        cryptoStream.FlushFinalBlock();

                        var position = memoryStream.Position;
                        EncryptedLength = (ushort)(position - DATA_OFFSET);

                        memoryStream.Position = 1;

                        var rawLength = BitConverter.GetBytes(EncryptedLength);
                        if (!BitConverter.IsLittleEndian)
                            Array.Reverse(rawLength);
                        memoryStream.Write(rawLength, 0, rawLength.Length);

                        memoryStream.Position = position;

                        SetData(DATA_OFFSET, EncryptedLength, new ArraySegment<byte>(memoryStream.ToArray()), null);
                    }
                }
            }

            // Restore
            public Aes256Encryption(ArraySegment<byte> data) : base(EncryptionTypes.Aes256)
            {
                var buffer = data.Array;

                var encryptionType = (EncryptionTypes)buffer[0];
                var length = (ushort)(buffer[data.Offset + 1] | buffer[data.Offset + 2] << 8);
                if (!BitConverter.IsLittleEndian)
                    length = (ushort)(buffer[data.Offset + 2] | buffer[data.Offset + 1] << 8);

                EncryptedLength = length;

                var a = buffer[data.Offset + PADDING_BYTES];
                var b = buffer[data.Offset + PADDING_BYTES + 1];
                Iterations = (ushort)(a | b << 8);
                if (!BitConverter.IsLittleEndian)
                    Iterations = (ushort)(b | a << 8);

                SetData(DATA_OFFSET, length, data, null);
            }

            static byte[] GetPasswordKey(byte[] password, int iterations = 1000)
            {
                if (_sha256 == null)
                    _sha256 = new SHA256Managed();

                var hash = _sha256.ComputeHash(password);
                for (var i = 0; i < iterations; i++)
                    hash = _sha256.ComputeHash(hash);

                var control = _sha256.ComputeHash(password);
                for (var i = 0; i < iterations; i++)
                    control = _sha256.ComputeHash(control);

                if (hash.SequenceEqual(control))
                    return hash;

                throw new Exception("AesEncryption.GetPasswordData failure");
            }

            protected override byte[] DecryptContainer(byte[] password)
            {
                if (_aes == null)
                {
                    _aes = new AesManaged
                    {
                        KeySize = 256
                    };
                }

                if (_ivBuffer == null)
                    _ivBuffer = new byte[HEADER_LENGTH - 2];

                Buffer.BlockCopy(Data.Array, Data.Offset + PADDING_BYTES + 2, _ivBuffer, 0, _ivBuffer.Length);

                _aes.Key = GetPasswordKey(password, Iterations);
                _aes.IV = _ivBuffer;

                var decryptor = _aes.CreateDecryptor(_aes.Key, _aes.IV);

                using (var memoryStream = new MemoryStream(RawData.Array, RawData.Offset, EncryptedLength))
                {
                    using (var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                    {
                        var a = (byte)cryptoStream.ReadByte();
                        var b = (byte)cryptoStream.ReadByte();
                        var dataLength = (ushort)(a | b << 8);
                        if (!BitConverter.IsLittleEndian)
                            dataLength = (ushort)(b | a << 8);

                        var result = new byte[dataLength];
                        cryptoStream.Read(result, 0, result.Length);

                        return result;
                    }
                }
            }
        }
    }
}
