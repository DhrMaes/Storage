namespace DhrMaes.Storage.Core.Structure.Directory
{
    using System;
    using System.Diagnostics.CodeAnalysis;

    using DhrMaes.Storage.Core.Structure;

    public struct DirectoryNodeReference : IDirectoryNodeReference, IStorageNodeReference<IDirectoryNode>, IEquatable<DirectoryNodeReference>
    {
        public static readonly DirectoryNodeReference Root = new DirectoryNodeReference("Root");

        public DirectoryNodeReference(string name)
        {
            Name = name;
        }

        public string Name { get; set; }

        public IDirectoryNodeReference? Parent { get; set; }

        public static DirectoryNodeReference FromPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Path cannot be null or empty.", nameof(path));
            }

            if (path == "/")
            {
                return Root;
            }

            if (!path.StartsWith(Path.DirectorySeparatorChar))
            {
                throw new ArgumentException("Only supports absolute paths.");
            }

            // Normalize and split the path
            var segments = path.Trim().Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);

            var current = Root;
            foreach (var segment in segments)
            {
                current = new DirectoryNodeReference(segment)
                {
                    Parent = current,
                };
            }

            return current;
        }

        public IDirectoryNode ToStorageNode(IStorage storage)
        {
            return new DirectoryNode(storage, this);
        }

        public string GetFullPath()
        {
            if (Parent is null)
            {
                return Convert.ToString(Path.DirectorySeparatorChar);
            }

            return Path.Combine(Parent.GetFullPath(), Name);
        }

        public void Accept(INodeVisitor visitor)
        {
            visitor.VisitDirectoryNodeReference(this);
        }

        public bool Equals(DirectoryNodeReference other)
        {
            if (this.GetFullPath() == other.GetFullPath())
            {
                return true;
            }

            return false;
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            return obj is DirectoryNodeReference other && Equals(other);
        }

        public override int GetHashCode()
        {
            return $"DirectoryReference:{GetFullPath()}".GetHashCode();
        }
    }
}
