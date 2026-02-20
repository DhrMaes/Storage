namespace DhrMaes.Storage.Core.Structure.Directory
{
    using System.Collections.ObjectModel;
    using System.Threading.Tasks;

    using DhrMaes.Storage.Core.Structure;
    using DhrMaes.Storage.Core.Structure.File;

    public sealed class DirectoryNode : IDirectoryNode
    {
        private readonly IStorage _storage;

        private List<IDirectoryNodeReference>? _directories;
        private List<IFileNodeReference>? _files;

        public DirectoryNode(IStorage storage, DirectoryNodeReference reference)
        {
            Name = reference.Name;
            Parent = reference.Parent;

            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            Reload();
        }

        internal DirectoryNode(
            IStorage storage,
            DirectoryNodeReference reference,
            List<IDirectoryNodeReference> directories,
            List<IFileNodeReference> files)
        {
            Name = reference.Name;
            Parent = reference.Parent;

            _directories = directories ?? throw new ArgumentNullException(nameof(directories));
            _files = files ?? throw new ArgumentNullException(nameof(files));
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        }

        public string Name { get; }

        public IDirectoryNodeReference? Parent { get; }

        public IReadOnlyCollection<IDirectoryNodeReference> Directories
        {
            get
            {
                if (_directories is null)
                {
                    Reload();
                }

                return new ReadOnlyCollection<IDirectoryNodeReference>(_directories!);
            }
        }

        public IReadOnlyCollection<IFileNodeReference> Files
        {
            get
            {
                if (_files is null)
                {
                    Reload();
                }

                return new ReadOnlyCollection<IFileNodeReference>(_files!);
            }
        }

        public static implicit operator DirectoryNodeReference(DirectoryNode node)
        {
            return new DirectoryNodeReference(node.Name)
            {
                Parent = node.Parent
            };
        }

        public void CreateDirectory(string name) => CreateDirectoryAsync(name).GetAwaiter().GetResult();

        public async Task CreateDirectoryAsync(string name, CancellationToken cancellationToken = default)
        {
            await ReloadAsync();

            if (Directories.Any(dir => dir.Name == name))
            {
                throw new InvalidOperationException($"Directory with name '{name}' already exists in '{GetFullPath()}'.");
            }

            foreach (var provider in _storage.GetProviders())
            {
                await provider.CreateDirectoryAsync(new DirectoryNodeReference(name)
                {
                    Parent = this,
                }, cancellationToken);
            }
        }

        public void Remove() => RemoveAsync().GetAwaiter().GetResult();

        public async Task RemoveAsync(CancellationToken cancellationToken = default)
        {
            foreach (var provider in _storage.GetProviders())
            {
                await provider.DeleteAsync(this, cancellationToken);
            }
        }

        public string GetFullPath()
        {
            if (Parent is null)
            {
                return "/";
            }

            return Path.Combine(Parent.GetFullPath(), Name);
        }

        public void Reload() => ReloadAsync().GetAwaiter().GetResult();

        public async Task ReloadAsync()
        {
            var node = await _storage.GetDirectoryAsync(GetFullPath());
            _directories = [.. node.Directories];
            _files = [.. node.Files];
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
