/* Copyright 2014 Vinicius Chiele
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/
using System;
using System.Text;
using Heleus.Base;

namespace Heleus.Cryptography.Scrypt
{
    public class ScryptInput : IPackable
    {
        public readonly int IterationCount;
        public readonly int BlockSize;
        public readonly int ThreadCount;
        public readonly byte[] Salt;

        public ScryptInput(int iterationCount, int blockSize, int threadCount, byte[] salt)
        {
            IterationCount = iterationCount;
            BlockSize = blockSize;
            ThreadCount = threadCount;
            Salt = salt;
        }

        public ScryptInput(Unpacker unpacker)
        {
            unpacker.Unpack(out IterationCount);
            unpacker.Unpack(out BlockSize);
            unpacker.Unpack(out ThreadCount);
            unpacker.Unpack(out Salt);
        }

        public void Pack(Packer packer)
        {
            packer.Pack(IterationCount);
            packer.Pack(BlockSize);
            packer.Pack(ThreadCount);
            packer.Pack(Salt);
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

    public class ScryptResult
    {
        public readonly int IterationCount;
        public readonly int BlockSize;
        public readonly int ThreadCount;
        public readonly byte[] Salt;
        public readonly byte[] Hash;

        public readonly ScryptInput Input;

        public ScryptResult(int iterationCount, int blockSize, int threadCount, byte[] salt, byte[] hash)
        {
            IterationCount = iterationCount;
            BlockSize = blockSize;
            ThreadCount = threadCount;
            Salt = salt;
            Hash = hash;

            Input = new ScryptInput(iterationCount, blockSize, threadCount, salt);
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.Append("$s2$");
            sb.Append(IterationCount.ToString()).Append('$');
            sb.Append(BlockSize.ToString()).Append('$');
            sb.Append(ThreadCount.ToString()).Append('$');
            sb.Append(Convert.ToBase64String(Salt)).Append('$');
            sb.Append(Convert.ToBase64String(Hash));

            return sb.ToString();
        }
    }
}