using System;
using System.IO;
using System.Security.Cryptography;

namespace Heleus.Cryptography
{
    public partial class Hash
    {
        class Sha256Hash : Hash
        {
            [ThreadStatic]
            static SHA256 _sha256;

            public Sha256Hash(ArraySegment<byte> data, Stream stream, bool newHash) : base(HashTypes.Sha256)
            {
                if (newHash)
                {
                    if(_sha256 == null)
                        _sha256 = new SHA256Managed();

                    byte[] hash;

                    if (stream != null)
                        hash = _sha256.ComputeHash(stream);
                    else
                    {
                        if (!data.Valid())
                            throw new ArgumentException(nameof(data));
                        hash = _sha256.ComputeHash(data.Array, data.Offset, data.Count);
                    }

                    var hashTargetData = new byte[hash.Length + PADDING_BYTES];
                    hashTargetData[0] = (byte)HashType;

                    Buffer.BlockCopy(hash, 0, hashTargetData, PADDING_BYTES, hash.Length);

                    var hashRawTarget = new ArraySegment<byte>(hashTargetData, PADDING_BYTES, SHA256_BYTES);
                    SetData(PADDING_BYTES, SHA256_BYTES, new ArraySegment<byte>(hashTargetData), hashRawTarget);
                }
                else
                {
                    SetData(PADDING_BYTES, SHA256_BYTES, data, null);
                }
            }
        }
    }
}
