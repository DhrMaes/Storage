namespace DhrMaes.Storage.Core.Structure.Directory
{
    using System.Text.Json.Serialization;

	using DhrMaes.Storage.Core.Structure;
	using DhrMaes.Storage.Core.Structure.File;

	public sealed class DirectoryNode : IDirectoryNode
    {
        private readonly IStorage _storage;

        internal DirectoryNode(IStorage storage, DirectoryNodeReference reference)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            Name = reference.Name;
            Parent = reference.Parent;
        }

        public string Name { get; set; }

        public ICollection<IDirectoryNode> Directories { get; } = new List<IDirectoryNode>();

        public ICollection<IFileNode> Files { get; set; } = new List<IFileNode>();

        [JsonIgnore]
        public IDirectoryNode? Parent { get; set; }

        public static implicit operator DirectoryNodeReference(DirectoryNode node)
        {
            return new DirectoryNodeReference(node.Name)
            {
                Parent = node.Parent
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

		public DirectoryNode ToDirectoryNode()
        {
            return this;
        }

        public void Accept(INodeVisitor visitor)
        {
            visitor.VisitDirectoryNode(this);
        }

        public bool Equals(IStorageNode? other)
        {
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (!(other is DirectoryNode otherDir))
            {
                return false;
            }

            if (this.GetFullPath() == otherDir.GetFullPath())
            {
                return true;
            }

            return false;
        }

		public override bool Equals(object? obj)
        {
            return Equals(obj as DirectoryNode);
        }

        public override int GetHashCode()
        {
            return $"Directory:{GetFullPath()}".GetHashCode();
        }

	}
}
