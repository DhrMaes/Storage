namespace DhrMaes.Storage.Core.Structure.File
{
    using System.Diagnostics.CodeAnalysis;

    using DhrMaes.Storage.Core.Structure;
    using DhrMaes.Storage.Core.Structure.Directory;

    public struct FileNodeReference : IFileNodeReference, IStorageNodeReference<IFileNode>, IEquatable<FileNodeReference>
    {
        public FileNodeReference(string name)
        {
            Name = name;
        }

        public string Name { get; set; }

        public IDirectoryNodeReference? Parent { get; set; }

        public static FileNodeReference FromPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Path cannot be null or empty.", nameof(path));
            }

            if (!path.StartsWith(Path.DirectorySeparatorChar))
            {
                throw new ArgumentException("Only supports absolute paths.");
            }

            // Normalize and split the path
            var segments = path.Trim().Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);

            var current = DirectoryNodeReference.Root;
            foreach (var segment in segments.Take(segments.Length - 1))
            {
                current = new DirectoryNodeReference(segment)
                {
                    Parent = current,
                };
            }

            var reference = new FileNodeReference(segments.Last())
            {
                Parent = current
            };
            return reference;
        }

        public IFileNode ToStorageNode(IStorage storage)
        {
            return storage.GetFile(GetFullPath());
        }

        public string GetFullPath()
        {
            if (Parent == null)
            {
                return Name;
            }

            return Path.Combine(Parent.GetFullPath(), Name);
        }

        public void Accept(INodeVisitor visitor)
        {
            visitor.VisitFileNodeReference(this);
        }

        public bool Equals(FileNodeReference other)
        {
            if (this.GetFullPath() == other.GetFullPath())
            {
                return true;
            }

            return false;
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            return obj is FileNodeReference other && Equals(other);
        }

        public override int GetHashCode()
        {
            return $"FileReference:{GetFullPath()}".GetHashCode();
        }
    }
}
