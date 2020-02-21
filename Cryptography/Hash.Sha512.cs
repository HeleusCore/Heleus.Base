using System;
using System.IO;
using System.Security.Cryptography;

namespace Heleus.Cryptography
{
    public partial class Hash
    {
        class Sha512Hash : Hash
        {
            /*
            public Sha512Hash(ArraySegment<byte> data, ArraySegment<byte>? target) : base(HashTypes.Sha512)
            {
                if (data == null || data.Array == null)
                    throw new ArgumentException(nameof(data));

                ArraySegment<byte> hashTarget;
                ArraySegment<byte> hashRawTarget;

                if (target.HasValue)
                {
                    hashTarget = target.Value;
                    if (hashTarget.Array == null || hashTarget.Count != (SHA512_BYTES + TYPE_PADDING))
                        throw new ArgumentException(nameof(target));
                    hashRawTarget = new ArraySegment<byte>(hashTarget.Array, hashTarget.Offset + TYPE_PADDING, SHA512_BYTES);
                }
                else
                { 
                    var hashTargetData = new byte[SHA512_BYTES + TYPE_PADDING];

                    hashTarget = new ArraySegment<byte>(hashTargetData);
                    hashRawTarget = new ArraySegment<byte>(hashTargetData, TYPE_PADDING, SHA512_BYTES);
                }

                hashTarget.Array[hashTarget.Offset] = (byte)HashType;

                var sha = new Sha512();
                sha.Update(data);
                sha.Finish(hashRawTarget);

                SetData(TYPE_PADDING, SHA512_BYTES, hashTarget, hashRawTarget);
            }
            */

            [ThreadStatic]
            static SHA512 _sha512;

            public Sha512Hash(ArraySegment<byte> data, Stream stream, bool newHash) : base(HashTypes.Sha512)
            {
                if (newHash)
                {
                    var hashTargetData = new byte[SHA512_BYTES + PADDING_BYTES];
                    hashTargetData[0] = (byte)HashType;

                    var hashRawTarget = new ArraySegment<byte>(hashTargetData, PADDING_BYTES, SHA512_BYTES);

                    /*
                    if (!UseCryptoProvider)
                    {
                        if (sha512 == null)
                            sha512 = new Sha512();
                        
                        sha512.Init();
                        sha512.Update(data);
                        sha512.Finish(hashRawTarget);
                    }
                    else
                    {
                    */
                    if(_sha512 == null)
                        _sha512 = new SHA512Managed();

                    byte[] hash;

                    if (stream != null)
                    {
                        hash = _sha512.ComputeHash(stream);
                    }
                    else
                    {
                        if (!data.Valid())
                            throw new ArgumentException(nameof(data));
                        hash = _sha512.ComputeHash(data.Array, data.Offset, data.Count);
                    }

                    Buffer.BlockCopy(hash, 0, hashTargetData, PADDING_BYTES, hash.Length);
                    //}

                    SetData(PADDING_BYTES, SHA512_BYTES, new ArraySegment<byte>(hashTargetData), hashRawTarget);
                }
                else
                {
                    SetData(PADDING_BYTES, SHA512_BYTES, data, null);
                }
            }
        }
    }
}
