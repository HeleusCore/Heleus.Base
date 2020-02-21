using System;
using System.Collections.Generic;
using Heleus.Base;
using Heleus.Chain;

namespace Heleus.Transactions.Features
{
    [Flags]
    public enum GroupFlags
    {
        None = 0,
        AdminOnlyInvitation = 1 << 0,
    }

    [Flags]
    public enum GroupAccountFlags
    {
        None = 0,
        Admin = 1 << 0,

        HasAccountApproval = 1 << 30,
        HasAdminApproval = 1 << 31
    }

    public class GroupAdministrationInfo : IPackable
    {
        public const long InvalidGroupId = 0;
        public const long FirstGroupId = 1;

        public readonly long GroupId;
        public readonly long AccountId;
        public readonly long TransactionId;

        public LastTransactionInfo AdministrationLastTransactionInfo { get; private set; }

        public GroupFlags Flags { get; private set; }

        readonly Dictionary<long, GroupAccountFlags> _accounts = new Dictionary<long, GroupAccountFlags>();
        readonly Dictionary<long, GroupAccountFlags> _pendingAccounts = new Dictionary<long, GroupAccountFlags>();

        public GroupAdministrationInfo(long groupId, long accountId, long transactionId, long timestamp, GroupFlags flags = GroupFlags.None)
        {
            GroupId = groupId;
            AccountId = accountId;
            TransactionId = transactionId;
            Flags = flags;

            AdministrationLastTransactionInfo = new LastTransactionInfo(transactionId, timestamp);

            _accounts[accountId] = GroupAccountFlags.Admin;
        }

        public GroupAdministrationInfo(Unpacker unpacker)
        {
            unpacker.Unpack(out GroupId);
            unpacker.Unpack(out AccountId);
            unpacker.Unpack(out TransactionId);
            AdministrationLastTransactionInfo = new LastTransactionInfo(unpacker);
            Flags = (GroupFlags)unpacker.UnpackUInt();

            var count = unpacker.UnpackInt();
            for (var i = 0; i < count; i++)
            {
                var id = unpacker.UnpackLong();
                _accounts[id] = (GroupAccountFlags)unpacker.UnpackUInt();
            }

            count = unpacker.UnpackInt();
            for (var i = 0; i < count; i++)
            {
                var id = unpacker.UnpackLong();
                _pendingAccounts[id] = (GroupAccountFlags)unpacker.UnpackUInt();
            }
        }

        public void Pack(Packer packer)
        {
            lock (this)
            {
                packer.Pack(GroupId);
                packer.Pack(AccountId);
                packer.Pack(TransactionId);
                packer.Pack(AdministrationLastTransactionInfo);
                packer.Pack((uint)Flags);

                var count = _accounts.Count;
                packer.Pack(count);
                foreach (var item in _accounts)
                {
                    packer.Pack(item.Key);
                    packer.Pack((uint)item.Value);
                }

                count = _pendingAccounts.Count;
                packer.Pack(count);
                foreach (var item in _pendingAccounts)
                {
                    packer.Pack(item.Key);
                    packer.Pack((uint)item.Value);
                }
            }
        }

        public bool IsGroupAccount(long accountId)
        {
            lock (this)
                return _accounts.ContainsKey(accountId);
        }

        public bool IsPendingAccount(long accountId)
        {
            lock (this)
                return _pendingAccounts.ContainsKey(accountId);
        }

        public bool IsGroupAccountOrPending(long accountId)
        {
            lock (this)
            {
                return _accounts.ContainsKey(accountId) || _pendingAccounts.ContainsKey(accountId);
            }
        }

        public bool IsGroupAccount(long accountId, out GroupAccountFlags flags)
        {
            lock (this)
            {
                if (_accounts.TryGetValue(accountId, out var f))
                {
                    flags = f;
                    return true;
                }
            }

            flags = GroupAccountFlags.None;
            return false;
        }

        public bool IsPendingAccount(long accountId, out GroupAccountFlags flags)
        {
            lock (this)
            {
                if (_pendingAccounts.TryGetValue(accountId, out var f))
                {
                    flags = f;
                    return true;
                }
            }

            flags = GroupAccountFlags.None;
            return false;
        }

        public class DirtyGroupAccounts
        {
            public readonly HashSet<long> AddedAccounts = new HashSet<long>();
            public readonly HashSet<long> RemovedAccounts = new HashSet<long>();
        }

        public void ConsumeGroupAdministrationRequest(Transaction transaction, GroupAdministrationRequest accounts, out DirtyGroupAccounts dirtyGroupAccounts)
        {
            dirtyGroupAccounts = new DirtyGroupAccounts();

            lock (this)
            {
                AdministrationLastTransactionInfo = new LastTransactionInfo(transaction.TransactionId, transaction.Timestamp);

                foreach (var added in accounts.AddedAccounts)
                {
                    var id = added.Key;
                    var flags = added.Value;

                    if (_pendingAccounts.TryGetValue(id, out var pendingFlags))
                    {
                        flags |= pendingFlags;
                    }

                    if ((flags & GroupAccountFlags.HasAccountApproval) != 0 && (flags & GroupAccountFlags.HasAdminApproval) != 0)
                    {
                        flags &= ~(GroupAccountFlags.HasAccountApproval | GroupAccountFlags.HasAdminApproval);
                        _accounts[id] = flags;
                        _pendingAccounts.Remove(id);
                        dirtyGroupAccounts.AddedAccounts.Add(id);
                    }
                    else
                    {
                        _pendingAccounts[id] = flags;
                    }
                }

                foreach (var removed in accounts.RemovedAccounts)
                {
                    _accounts.Remove(removed);
                    _pendingAccounts.Remove(removed);

                    dirtyGroupAccounts.RemovedAccounts.Add(removed);
                }

                foreach (var updated in accounts.UpdatedFlags)
                {
                    var id = updated.Key;
                    var flags = updated.Value;

                    if (_accounts.ContainsKey(id))
                        _accounts[id] = flags;
                }
            }
        }

        public Dictionary<long, GroupAccountFlags> GetAccounts()
        {
            lock (this)
                return new Dictionary<long, GroupAccountFlags>(_accounts);
        }

        public Dictionary<long, GroupAccountFlags> GetPendingAccounts()
        {
            lock (this)
                return new Dictionary<long, GroupAccountFlags>(_pendingAccounts);
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
