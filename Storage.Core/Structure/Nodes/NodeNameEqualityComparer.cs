namespace DhrMaes.Storage.Core.Structure.Nodes
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    internal class NodeNameEqualityComparer : IEqualityComparer<IStorageNode>
    {
        public bool Equals(IStorageNode? x, IStorageNode? y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (x is null)
            {
                return false;
            }

            if (y is null)
            {
                return false;
            }

            return x.Name == y.Name;
        }

        public int GetHashCode([DisallowNull] IStorageNode obj)
        {
            return obj.Name.GetHashCode();
        }
    }
}
