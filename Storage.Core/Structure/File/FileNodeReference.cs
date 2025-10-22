namespace DhrMaes.Storage.Core.Structure.File
{
	using System.Diagnostics.CodeAnalysis;
	using System.Text.Json.Serialization;

	using DhrMaes.Storage.Core.Structure;
	using DhrMaes.Storage.Core.Structure.Directory;

	public struct FileNodeReference : IFileNode, IEquatable<FileNodeReference>
    {
        public FileNodeReference(string name)
        {
            Name = name;
        }

        public string Name { get; set; }

        [JsonIgnore]
        public IDirectoryNode? Parent { get; set; }

        public static FileNodeReference FromPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Path cannot be null or empty.", nameof(path));
            }

            if (!path.StartsWith('/'))
            {
                throw new ArgumentException("Only supports absolute paths.");
            }

            // Normalize and split the path
            var segments = path.Trim().Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);

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

        public FileNode ToFileNode(IStorage storage)
        {
            return new FileNode(storage, this);
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

        public bool Equals(IStorageNode? other)
        {
            if (!(other is FileNodeReference otherFile))
            {
                return false;
            }

            return Equals(otherFile);
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
            return Equals(obj as IStorageNode);
        }

        public override int GetHashCode()
        {
            return $"File:{GetFullPath()}".GetHashCode();
        }
    }
}
