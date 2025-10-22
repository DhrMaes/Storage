namespace DhrMaes.Storage.Core.Structure.Directory
{
	using System;
	using System.Diagnostics.CodeAnalysis;
	using System.Text.Json.Serialization;

	using DhrMaes.Storage.Core.Structure;

	public struct DirectoryNodeReference : IDirectoryNode, IEquatable<DirectoryNodeReference>
    {
        public static readonly DirectoryNodeReference Root = new DirectoryNodeReference("Root");

        public DirectoryNodeReference(string name)
        {
            Name = name;
        }

        public string Name { get; set; }

        [JsonIgnore]
        public IDirectoryNode? Parent { get; set; }

		public static DirectoryNodeReference FromPath(string path)
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

            DirectoryNodeReference current = Root;
            foreach (var segment in segments)
            {
                current = new DirectoryNodeReference(segment)
                {
                    Parent = current,
                };
            }

            return current;
        }

        public DirectoryNode ToDirectoryNode(IStorage storage)
        {
            return new DirectoryNode(storage, this)
            {
                Parent = Parent,
            };
        }

        public string GetFullPath()
        {
            if (Parent is null)
            {
                return String.Empty;
            }

            return Path.Combine(Parent.GetFullPath(), Name);
        }

        public void Accept(INodeVisitor visitor)
        {
            visitor.VisitDirectoryNodeReference(this);
        }

        public bool Equals(IStorageNode? other)
        {
            if (!(other is DirectoryNodeReference otherDir))
            {
                return false;
            }

            return Equals(otherDir);
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
            return Equals(obj as IStorageNode);
        }

        public override int GetHashCode()
        {
            return $"Directory:{GetFullPath()}".GetHashCode();
        }

	}
}
