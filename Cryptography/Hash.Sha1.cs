using System;
using System.IO;
using System.Security.Cryptography;

namespace Heleus.Cryptography
{
    public partial class Hash
    {
        class Sha1Hash : Hash
        {
            [ThreadStatic]
            static SHA1 _sha1;

            public Sha1Hash(ArraySegment<byte> data, Stream stream, bool newHash) : base(HashTypes.Sha1)
            {
                if (newHash)
                {
                    if(_sha1 == null)
                        _sha1 = new SHA1Managed();

                    byte[] hash;

                    if (stream != null)
                        hash = _sha1.ComputeHash(stream);
                    else
                    {
                        if (!data.Valid())
                            throw new ArgumentException(nameof(data));
                        hash = _sha1.ComputeHash(data.Array, data.Offset, data.Count);
                    }

                    var hashTargetData = new byte[hash.Length + PADDING_BYTES];
                    hashTargetData[0] = (byte)HashType;

                    Buffer.BlockCopy(hash, 0, hashTargetData, PADDING_BYTES, hash.Length);

                    var hashRawTarget = new ArraySegment<byte>(hashTargetData, PADDING_BYTES, SHA1_BYTES);
                    SetData(PADDING_BYTES, SHA1_BYTES, new ArraySegment<byte>(hashTargetData), hashRawTarget);
                }
                else
                {
                    SetData(PADDING_BYTES, SHA1_BYTES, data, null);
                }
            }
        }
    }
}
