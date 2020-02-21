using System.Collections.Generic;

namespace Heleus.Transactions.Features
{
    public class Commit
    {
        public readonly HashSet<long> DirtyIds = new HashSet<long>();
    }
}
