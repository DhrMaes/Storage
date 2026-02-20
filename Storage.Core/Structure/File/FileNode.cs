namespace DhrMaes.Storage.Core.Structure.File
{
    using DhrMaes.Storage.Core.FileSystem;
	using DhrMaes.Storage.Core.Providers;
	using DhrMaes.Storage.Core.Structure;
    using DhrMaes.Storage.Core.Structure.Directory;

    public sealed class FileNode : IFileNode
    {
        private readonly IStorage _storage;
        private readonly IStorageProvider _provider;

        public FileNode(
            IStorageProvider provider,
            FileSize size,
            IFileNodeReference reference)
        {
            Name = reference.Name;
            Size = size;
            Parent = reference.Parent;

            //_storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            //Reload();
        }

        public string Name { get; set; }

        public FileSize Size { get; set; } = FileSize.Unknown;

        public IDirectoryNodeReference? Parent { get; set; }

        public Stream OpenRead() => OpenReadAsync().GetAwaiter().GetResult();

        public async Task<Stream> OpenReadAsync(CancellationToken cancellationToken = default)
        {
            var stream = _provider.OpenReadAsync(this, cancellationToken);
            return await stream;
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
