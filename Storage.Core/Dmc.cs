namespace DhrMaes.Storage.Core
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Text.Json;

    using DhrMaes.Storage.Core.Plugins;
    using DhrMaes.Storage.Core.Providers;
    using DhrMaes.Storage.Core.Structure;
    using DhrMaes.Storage.Core.Structure.File;
    using DhrMaes.Storage.Core.Structure.Nodes;
    using DhrMaes.Storage.Core.Structure.Serialization;

    public class Dmc : IDmc, IDisposable
    {
        private readonly PluginLoader _pluginLoader;
        private readonly Lazy<ICollection<IStorageProvider>> _providers;
        private readonly Lazy<DirectoryNode> _root;

        public Dmc()
        {
            _pluginLoader = new PluginLoader();
            _providers = new Lazy<ICollection<IStorageProvider>>(() =>
            {
                var providers = new List<IStorageProvider>();
                var providerPath = Path.Combine(FileSystem.FileSystem.GetUserConfigDir(), "Providers");
                foreach (var configPath in Directory.GetFiles(providerPath, "*.provider", SearchOption.AllDirectories))
                {
                    var loadTask = PluginLoader.LoadProvider(configPath);
                    loadTask.Wait();
                    var provider = loadTask.Result;
                    providers.Add(provider);
                }

                return providers;
            });
            _root = new Lazy<DirectoryNode>(() =>
            {
                var structurePath = Path.Combine(FileSystem.FileSystem.GetUserConfigDir(), "structure.json");
                if (File.Exists(structurePath))
                {
                    var structureJson = File.ReadAllText(structurePath);
                    var root = JsonSerializer.Deserialize<DirectoryNode>(structureJson, SerializationSettings.Default);
                    if (root != null)
                    {
                        new ParentWalker().Visit(root);
                        return root;
                    }
                }

                return new DirectoryNode("Root");
            });
        }

        public PluginLoader PluginLoader => _pluginLoader;

        public ICollection<IStorageProvider> Providers => _providers.Value;

        public DirectoryNode Root => _root.Value;

        public async Task<IReadOnlyDictionary<string, Type>> GetInstalledProviders()
        {
            return PluginLoader.ConfigTypes;
        }

        public async Task<IStorageProvider> AddProvider(IStorageProviderConfig config)
        {
            var id = Guid.CreateVersion7();

            var providerType = config.GetType().GetCustomAttribute<ProviderIdentifierAttribute>()?.Id;
            if (string.IsNullOrWhiteSpace(providerType))
            {
                throw new ArgumentException("Provider config type does not have a ProviderIdentifierAttribute or it has an invalid identifier.", nameof(config));
            }

            var providerDir = Path.Combine(FileSystem.FileSystem.GetUserConfigDir(), "Providers", providerType);
            if (!Directory.Exists(providerDir))
            {
                Directory.CreateDirectory(providerDir);
            }

            // Save the config
            var providerConfigPath = Path.Combine(providerDir, $"{id}.provider");
            using var configStream = new FileStream(
                providerConfigPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 4096,
                useAsync: true
            );

            await config.InitializeConfigAsync(id.ToString());
            await config.CopyToStreamAsync(configStream);

            // Initialize and create the provider
            var provider = await config.CreateProviderAsync();
            Providers.Add(provider);
            return provider;
        }

        public async Task RemoveProvider(string identifier)
        {
            var providerDir = Path.Combine(FileSystem.FileSystem.GetUserConfigDir(), "Providers");
            if (!Directory.Exists(providerDir))
            {
                return;
            }

            var files = Directory.GetFiles(providerDir, "{identifier}.provider", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                File.Delete(file);
            }

            var provider = Providers.FirstOrDefault(p => p.Identifier == identifier);
            if (provider is null)
            {
                return;
            }

            Providers.Remove(provider);
        }

        public async Task CreateDirectory(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Path cannot be null or empty.", nameof(path));

            // Normalize and split the path
            var segments = path.Trim().Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);

            if (segments.Length == 0)
                throw new ArgumentException("Path must contain at least one segment.", nameof(path));

            DirectoryNode current = Root;
            foreach (var segment in segments)
            {
                // Try to find an existing child directory node
                DirectoryNode? next = null;
                foreach (var child in current.Children)
                {
                    if (child is DirectoryNode dir && dir.Name.Equals(segment, StringComparison.OrdinalIgnoreCase))
                    {
                        next = dir;
                    }
                }

                // If not found, create a new DirectoryNode
                if (next == null)
                {
                    next = new DirectoryNode(segment)
                    {
                        Parent = current
                    };

                    current.Children.Add(next);
                }

                current = next;
            }

            foreach (var provider in Providers)
            {
                await provider.CreateDirectoryAsync(current);
            }
        }

        public async Task RemoveDirectory(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Path cannot be null or empty.", nameof(path));

            // Normalize and split the path
            var segments = path.Trim().Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);

            if (segments.Length == 0)
                throw new ArgumentException("Path must contain at least one segment.", nameof(path));

            DirectoryNode current = Root;
            foreach (var segment in segments)
            {
                // Try to find an existing child directory node
                DirectoryNode? next = null;
                foreach (var child in current.Children)
                {
                    if (child is DirectoryNode dir && dir.Name.Equals(segment, StringComparison.OrdinalIgnoreCase))
                    {
                        next = dir;
                    }
                }

                // If not found, consider remove done
                if (next == null)
                {
                    return;
                }

                current = next;
            }

            foreach (var provider in Providers)
            {
                await provider.DeleteAsync(current);
            }
        }

        public async Task<ICollection<IStorageNode>> ListDirectory(DirectoryNode node)
        {
            var nodes = new HashSet<IStorageNode>(new NodeNameEqualityComparer());
            var tasks = Providers.Select(p => p.ListAsync(node));
            var results = await Task.WhenAll(tasks);
            foreach (var child in results.SelectMany(x => x))
            {
                nodes.Add(child);
            }

            return nodes;
        }

        public async Task UploadFile(string path, Stream content, IUploadBehavior? behaviorFile = default)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Path cannot be null or empty.", nameof(path));

            // Normalize and split the path
            var segments = path.Trim().Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length == 0)
                throw new ArgumentException("Path must contain at least one segment.", nameof(path));

            DirectoryNode current = Root;
            for (int i = 0; i < segments.Length - 1; i++)
            {
                var segment = segments[i];

                // Try to find an existing child directory node
                DirectoryNode? next = null;
                foreach (var child in current.Children)
                {
                    if (child is DirectoryNode dir && dir.Name.Equals(segment, StringComparison.OrdinalIgnoreCase))
                    {
                        next = dir;
                    }
                }

                // If not found, create a new DirectoryNode
                if (next == null)
                {
                    next = new DirectoryNode(segment)
                    {
                        Parent = current
                    };
                    current.Children.Add(next);
                }

                current = next;
            }

            var fileName = Path.GetFileName(path);
            var fileNode = new FileNode(fileName)
            {
                Parent = current
            };
            current.Children.Add(fileNode);

            behaviorFile ??= new FirstProviderBehavior();
            await behaviorFile.HandleUploadAsync(Providers, fileNode, content, CancellationToken.None);
        }

        public async Task RemoveFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Path cannot be null or empty.", nameof(path));

            // Normalize and split the path
            var segments = path.Trim().Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length == 0)
                throw new ArgumentException("Path must contain at least one segment.", nameof(path));

            DirectoryNode current = Root;
            for (int i = 0; i < segments.Length - 1; i++)
            {
                var segment = segments[i];

                // Try to find an existing child directory node
                DirectoryNode? next = null;
                foreach (var child in current.Children)
                {
                    if (child is DirectoryNode dir && dir.Name.Equals(segment, StringComparison.OrdinalIgnoreCase))
                    {
                        next = dir;
                    }
                }

                // If not found, create a new DirectoryNode
                if (next == null)
                {
                    throw new FileNotFoundException("No file found at the specified path.", path);
                }

                current = next;
            }

            var fileName = Path.GetFileName(path);
            var fileNode = current.Children.FirstOrDefault(dir => (dir as FileNode)?.Name == fileName);
            if (fileNode is null)
            {
                throw new FileNotFoundException("No file found at the specified path.", path);
            }

            await Task.WhenAll(Providers.Select(p => p.DeleteAsync(fileNode)));
        }

        public void Dispose()
        {
            var structure = JsonSerializer.Serialize(Root, SerializationSettings.Default);

            var structurePath = Path.Combine(FileSystem.FileSystem.GetUserConfigDir(), "structure.json");
            File.WriteAllText(structurePath, structure);
        }

        public Task<IStorageProvider> GetProvider(string identifier) => throw new NotImplementedException();

        public Task<bool> ExistsAsync(IStorageNode node) => throw new NotImplementedException();

        public Task<Stream> OpenReadAsync(FileNode node, Func<ICollection<IStorageProvider>, IStorageProvider>? selector = null) => throw new NotImplementedException();
    }
}
