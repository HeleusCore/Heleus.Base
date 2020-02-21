using System;
using System.Collections.Generic;
using System.Text;
using Heleus.Base;

namespace Heleus.Transactions
{
    public class Attachements : IPackable
    {
        public readonly long AccountId;
        public readonly long Token;
        public readonly int ChainId;
        public readonly uint ChainIndex;
        public readonly long TimeStamp;

        public readonly List<AttachementItem> Items = new List<AttachementItem>();

        public Attachements(long accountId, int chainId, uint chainIndex)
        {
            AccountId = accountId;
            ChainId = chainId;
            ChainIndex = chainIndex;
            TimeStamp = Time.Timestamp;
            Token = Rand.NextLong();
        }

        public Attachements(long accountId, int chainId, uint chainIndex, long timeStamp, long token)
        {
            AccountId = accountId;
            ChainId = chainId;
            ChainIndex = chainIndex;
            TimeStamp = timeStamp;
            Token = token;
        }

        public Attachements(Unpacker unpacker)
        {
            unpacker.Unpack(out AccountId);
            unpacker.Unpack(out ChainId);
            unpacker.Unpack(out ChainIndex);
            unpacker.Unpack(out TimeStamp);
            unpacker.Unpack(out Token);
            var count = unpacker.UnpackUshort();
            for (var i = 0; i < count; i++)
                Items.Add(new AttachementItem(unpacker));
        }

        void CheckAttachement(string name)
        {
            foreach (var att in Items)
            {
                if (att.Name == name)
                    throw new ArgumentException("Name already used.", nameof(name));
            }
        }

        public bool CheckAttachements()
        {
            if (Items.Count <= 0)
                return false;

            var names = new HashSet<string>();

            foreach (var att in Items)
            {
                if (!AttachementItem.IsNameValid(att.Name))
                    return false;

                if (names.Contains(att.Name))
                    return false;
                names.Add(att.Name);
            }

            return true;
        }

        public Attachements AddBinaryAttachement(string name, byte[] data)
        {
            CheckAttachement(name);
            Items.Add(new AttachementItem(name, data));
            return this;
        }

        public Attachements AddStringAttachement(string name, string data)
        {
            CheckAttachement(name);
            Items.Add(new AttachementItem(name, Encoding.UTF8.GetBytes(data)));
            return this;
        }

        public void Pack(Packer packer)
        {
            packer.Pack(AccountId);
            packer.Pack(ChainId);
            packer.Pack(ChainIndex);
            packer.Pack(TimeStamp);
            packer.Pack(Token);
            var count = Items.Count;
            packer.Pack((ushort)count);

            for (var i = 0; i < count; i++)
                packer.Pack(Items[i]);
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
