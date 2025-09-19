namespace DhrMaes.Storage.Core
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Text.Json;

    using DhrMaes.Storage.Core.Plugins;
    using DhrMaes.Storage.Core.Providers;
    using DhrMaes.Storage.Core.Structure;
    using DhrMaes.Storage.Core.Structure.Nodes;
    using DhrMaes.Storage.Core.Structure.Serialization;

    internal class Dmc : IDmc, IDisposable
    {
        private readonly PluginLoader _pluginLoader;
        private readonly ICollection<IStorageProvider> _providers;
        private readonly Lazy<DirectoryNode> _root;

        public Dmc()
        {
            _pluginLoader = new PluginLoader();
            _providers = GetProviders();
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

        public DirectoryNode Root => _root.Value;

        public ICollection<IStorageProvider> GetProviders()
        {
            var providers = new List<IStorageProvider>();
            var providerPath = Path.Combine(FileSystem.FileSystem.GetUserConfigDir(), "Providers");
            foreach (var configPath in Directory.GetFiles(providerPath, "*.provider", SearchOption.AllDirectories))
            {
                var provider = PluginLoader.LoadProvider(configPath);
                providers.Add(provider);
            }

            return providers;
        }

        public IStorageProvider AddProvider(IStorageProviderConfig config)
        {
            var id = Guid.CreateVersion7();

            var providerDir = Path.Combine(FileSystem.FileSystem.GetUserConfigDir(), "Providers");
            if (!Directory.Exists(providerDir))
            {
                Directory.CreateDirectory(providerDir);
            }

            // TODO: Fill type in dynamically
            var providerType = config.GetType().GetCustomAttribute<ProviderIdentifierAttribute>()?.Id;
            if (string.IsNullOrWhiteSpace(providerType))
            {
                throw new ArgumentException("Provider config type does not have a ProviderIdentifierAttribute or it has an invalid identifier.", nameof(config));
            }

            var providerConfigPath = Path.Combine(providerDir, providerType, $"{id}.provider");

            File.WriteAllText(providerConfigPath, JsonSerializer.Serialize(config, config.GetType()));

            var provider = config.CreateProvider();
            _providers.Add(provider);
            return provider;
        }
        
        public void RemoveProvider(string identifier)
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

            var provider = _providers.FirstOrDefault(p => p.Identifier == identifier);
            if (provider is null)
            {
                return;
            }

            _providers.Remove(provider);
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

            foreach (var provider in _providers)
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

            foreach (var provider in _providers)
            {
                await provider.DeleteAsync(current);
            }
        }

        public async Task<ICollection<IStorageNode>> ListDirectory(string path)
        {
            if (path is null)
                throw new ArgumentException("Path cannot be null or empty.", nameof(path));

            // Normalize and split the path
            var segments = path.Trim().Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);

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
                if (next is null)
                {
                    throw new ArgumentException(nameof(path), $"Directory '{path}' does not exist.");
                }

                current = next;
            }

            var nodes = new HashSet<IStorageNode>();
            var tasks = _providers.Select(p => p.ListAsync(current));
            var results = await Task.WhenAll(tasks);
            foreach (var node in results.SelectMany(x => x))
            {
                nodes.Add(node);
            }

            current.Children = nodes;
            return nodes;
        }

        public void Dispose()
        {
            var structure = JsonSerializer.Serialize(Root, SerializationSettings.Default);

            var structurePath = Path.Combine(FileSystem.FileSystem.GetUserConfigDir(), "structure.json");
            File.WriteAllText(structurePath, structure);
        }
    }
}
