using System.Collections.Generic;
using Heleus.Base;

namespace Heleus.Transactions
{
    public sealed class AttachementDataTransaction : DataTransaction
    {
        readonly List<AttachementItem> _items = new List<AttachementItem>();
        public IReadOnlyList<AttachementItem> Items => _items;

        public int AttachementKey { get; private set; }
        public long Token { get; private set; }

        protected override bool IsContentValid
        {
            get
            {
                if (AttachementKey < 0)
                    return false;
                if (Token == 0)
                    return false;

                if (_items?.Count > 0)
                {
                    foreach (var item in _items)
                    {
                        if (!item.IsValid)
                            return false;
                    }
                    return true;
                }
                return false;
            }
        }

        public AttachementDataTransaction() : base(DataTransactionTypes.Attachement)
        {
        }

        public AttachementDataTransaction(Attachements attachement, int attachementKey) : base(DataTransactionTypes.Attachement, attachement.AccountId, attachement.ChainId, attachement.ChainIndex)
        {
            _items = attachement.Items;
            AttachementKey = attachementKey;
            Token = attachement.Token;
        }

        protected override void Pack(Packer packer)
        {
            base.Pack(packer);

            packer.Pack(AttachementKey);
            packer.Pack(Token);
            packer.Pack(_items);
        }

        protected override void Unpack(Unpacker unpacker)
        {
            AttachementKey = unpacker.UnpackInt();
            Token = unpacker.UnpackLong();
            unpacker.Unpack(_items, (u) => new AttachementItem(u));
        }
    }
}
