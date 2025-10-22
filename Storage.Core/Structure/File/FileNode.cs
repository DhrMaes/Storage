namespace DhrMaes.Storage.Core.Structure.File
{
	using System.Text.Json.Serialization;

	using DhrMaes.Storage.Core.FileSystem;
	using DhrMaes.Storage.Core.Structure;
	using DhrMaes.Storage.Core.Structure.Directory;
	using DhrMaes.Storage.Core.Structure.File;

	public sealed class FileNode : IFileNode
    {
        private readonly IStorage _storage;

        internal FileNode(IStorage storage, FileNodeReference reference)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            Name = reference.Name;
            Parent = reference.Parent;
        }

        public string Name { get; set; }

        [JsonIgnore]
        public FileSize Size { get; set; } = FileSize.Unknown;

        [JsonIgnore]
        public IDirectoryNode? Parent { get; set; }

        public FileNode ToFileNode(IStorage storage)
        {
            return this;
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
            visitor.VisitFileNode(this);
        }

        public bool Equals(IStorageNode? other)
        {
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (!(other is FileNode otherFile))
            {
                return false;
            }

            if (this.GetFullPath() == otherFile.GetFullPath())
            {
                return true;
            }

            return false;
        }

		public override bool Equals(object? obj)
        {
            return Equals(obj as IStorageNode);
        }

        public override int GetHashCode()
        {
            return $"File:{GetFullPath()}".GetHashCode();
        }
    }
}
