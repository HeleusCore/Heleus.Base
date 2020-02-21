using System;
using Heleus.Base;

namespace Heleus.Chain
{
    public enum RevokeableItemStatus
    {
        Valid,
        Revoked
    }

    public class RevokeableItem<T> : IPackable where T : IPackable
    {
        public RevokeableItemStatus Status { get; private set; }
        public T Item { get; protected set; }
        public readonly long IssueTimestamp;

        public bool IsValid => Status == RevokeableItemStatus.Valid;
        public bool IsRevoked => Status == RevokeableItemStatus.Revoked;
        public long RevokedTimestamp { get; private set; }

        public RevokeableItem(RevokeableItemStatus status, T item, long timestamp)
        {
            if (item.IsNullOrDefault())
                throw new ArgumentException("Item is null.", nameof(item));

            Status = status;
            Item = item;
            IssueTimestamp = timestamp;
        }

        public RevokeableItem(T item, long timestamp)
        {
            if (item.IsNullOrDefault())
                throw new ArgumentException("Item is null.", nameof(item));

            Status = RevokeableItemStatus.Valid;
            Item = item;
            IssueTimestamp = timestamp;
        }

        public RevokeableItem(Unpacker unpacker)
        {
            Status = (RevokeableItemStatus)unpacker.UnpackByte();
            unpacker.Unpack(out IssueTimestamp);
            RevokedTimestamp = unpacker.UnpackLong();
        }

        public void RevokeItem(long timestamp)
        {
            if (Status == RevokeableItemStatus.Valid)
                RevokedTimestamp = timestamp;

            Status = RevokeableItemStatus.Revoked;
        }

        public void Pack(Packer packer)
        {
            packer.Pack((byte)Status);
            packer.Pack(IssueTimestamp);
            packer.Pack(RevokedTimestamp);
            packer.Pack(Item);
        }
    }
}
