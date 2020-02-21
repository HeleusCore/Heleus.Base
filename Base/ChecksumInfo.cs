using System;
using System.Collections.Generic;
using System.IO;
using Heleus.Cryptography;

namespace Heleus.Base
{
    public class ChecksumInfo
    {
        class Item
        {
            public readonly string Name;
            public readonly Hash Checksum;

            public Item(string name, Hash checksum)
            {
                Name = name;
                Checksum = checksum;
            }

            public Item(Unpacker unpacker)
            {
                unpacker.Unpack(out Name);
                unpacker.Unpack(out Checksum);
            }

            public void Pack(Packer packer)
            {
                packer.Pack(Name);
                packer.Pack(Checksum);
            }
        }

        readonly List<Item> items = new List<Item>();

        public ChecksumInfo()
        {

        }

        public ChecksumInfo(Unpacker unpacker)
        {
            var count = unpacker.UnpackInt();
            for (var i = 0; i < count; i++)
                items.Add(new Item(unpacker));
        }

        public ChecksumInfo(byte[] checksumData)
        {
            using (var unpacker = new Unpacker(checksumData))
            {
                var count = unpacker.UnpackInt();
                for (var i = 0; i < count; i++)
                    items.Add(new Item(unpacker));
            }
        }

        public ChecksumInfo Add(string name, Hash checksum)
        {
            items.Add(new Item(name, checksum));
            return this;
        }

        public Hash Get(string name)
        {
            foreach (var item in items)
            {
                if (item.Name == name)
                    return item.Checksum;
            }
            return null;
        }

        public bool Valid(string name, byte[] data)
        {
            var hash = Get(name);
            if(hash != null)
            {
                var newHash = Hash.Generate(hash.HashType, data);
                return hash == newHash;
            }
            return false;
        }

        public bool Valid(string name, Stream data)
        {
            var hash = Get(name);
            if (hash != null)
            {
                var newHash = Hash.Generate(hash.HashType, data);
                return hash == newHash;
            }
            return false;
        }

        public void Pack(Packer packer)
        {
            packer.Pack(items.Count);
            foreach (var item in items)
                item.Pack(packer);
        }

        public byte[] ToArray()
        {
            using(var packer = new Packer())
            {
                Pack(packer);
                return packer.ToByteArray();
            }
        }
    }
}
