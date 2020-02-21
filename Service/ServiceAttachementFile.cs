using System;
using Heleus.Transactions;

namespace Heleus.Service
{
    public struct ServiceAttachementFile
    {
        public readonly string TempPath;
        public readonly AttachementItem Item;

        public ServiceAttachementFile(string tempPath, AttachementItem item)
        {
            TempPath = tempPath;
            Item = item;
        }
    }
}
