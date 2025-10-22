namespace DhrMaes.Storage.Core.Plugins
{
    using System.Collections.ObjectModel;
    using System.Reflection;
    using System.Runtime.Loader;

    using DhrMaes.Storage.Core.Providers;

    public class PluginLoader
    {
        private readonly string _pluginPath;
        private readonly List<IStoragePlugin> _plugins = new List<IStoragePlugin>();

        private bool _initialized = false;

        public PluginLoader() : this(Path.Combine(FileSystem.FileSystem.GetUserConfigDir(), "Plugins"))
        {
        }

        public PluginLoader(string pluginPath)
        {
            _pluginPath = pluginPath;
        }

        public event EventHandler<EventArgs> Loaded;

        public event EventHandler<EventArgs> Reloaded;

        public bool IsInitialized => _initialized;

        public IReadOnlyCollection<IStoragePlugin> Plugins => new ReadOnlyCollection<IStoragePlugin>(_plugins);

        public IStoragePlugin GetPlugin(string identifier)
        {
            var plugin = _plugins.FirstOrDefault(p => p.Name.Equals(identifier, StringComparison.OrdinalIgnoreCase));
            if (plugin is null)
            {
                throw new InvalidOperationException($"No plugin found with identifier '{identifier}'.");
            }

            return plugin;
        }

        /// <summary>
        /// Load all available storage provider types from the plugins directory.
        /// </summary>
        /// <returns></returns>
        public void LoadPlugins()
        {
            if(_initialized)
            {
                return;
            }

            _plugins.Clear();
            InternalLoadPlugins();
            _initialized = true;
            Loaded?.Invoke(this, EventArgs.Empty);
        }

        public void ReloadPlugins()
        {
            _plugins.Clear();
            InternalLoadPlugins();
            _initialized = true;
            Reloaded?.Invoke(this, EventArgs.Empty);
        }

        private void InternalLoadPlugins()
        {
            if (!Directory.Exists(_pluginPath))
            {
                throw new InvalidOperationException($"Plugin directory not found: {_pluginPath}");
            }

            foreach (var dir in Directory.GetDirectories(_pluginPath, "*", SearchOption.TopDirectoryOnly))
            {
                var loadContext = new PluginLoadContext(dir);
                foreach (var file in Directory.EnumerateFiles(dir, "*.dll", SearchOption.TopDirectoryOnly))
                {
                    var assembly = loadContext.LoadFromAssemblyPath(file);
                    if (assembly == null)
                    {
                        continue;
                    }

                    var types = assembly.GetTypes();

                    var configTypes = types
                        .Where(t => typeof(IStorageProviderConfig).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
                        .Select(t => new
                        {
                            Identifier = t.GetCustomAttribute<ProviderIdentifierAttribute>(),
                            Type = t,
                        })
                        .Where(m => m.Identifier is not null)
                        .ToDictionary(m => m.Identifier!.Id, m => m.Type);

                    var providerTypes = types
                        .Where(t => typeof(IStorageProvider).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
                        .Select(t => new
                        {
                            Identifier = t.GetCustomAttribute<ProviderIdentifierAttribute>(),
                            Type = t,
                        })
                        .Where(m => m.Identifier is not null)
                        .ToDictionary(m => m.Identifier!.Id, m => m.Type);

                    foreach (var provider in configTypes.Keys)
                    {
                        var configType = configTypes[provider];
                        if (!providerTypes.TryGetValue(provider, out var providerType))
                        {
                            continue;
                        }

                        _plugins.Add(new StoragePlugin(provider, configType, providerType));
                    }
                }
            }
        }

        public async Task<IStorageProvider> LoadProvider(string configPath)
        {
            var parentDir = Path.GetDirectoryName(configPath);
            if (string.IsNullOrEmpty(parentDir))
            {
                throw new ArgumentException("Invalid configPath: no parent directory found.", nameof(configPath));
            }

            var providerTypeName = Path.GetFileName(parentDir);
            if (string.IsNullOrEmpty(providerTypeName))
            {
                throw new ArgumentException("Invalid configPath: provider type name not found.", nameof(configPath));
            }

            if (!_providerTypes.TryGetValue(providerTypeName, out var providerType))
            {
                throw new InvalidOperationException($"No provider type found for '{providerTypeName}'.");
            }

            var config = Activator.CreateInstance(providerType) as IStorageProviderConfig;
            if (config is null)
            {
                throw new InvalidOperationException($"Failed to load provider config '{providerTypeName}'.");
            }

            using var configStream = File.OpenRead(configPath);
            await config.FromStreamAsync(Path.GetFileNameWithoutExtension(configPath), configStream);

            var provider = await config.CreateProviderAsync();
            if (provider is null)
                throw new InvalidOperationException($"Failed to create instance of provider '{providerTypeName}'.");

            return provider;
        }

        private sealed class PluginLoadContext : AssemblyLoadContext
        {
            private readonly string _pluginDirectory;

            public PluginLoadContext(string pluginDirectory)
            {
                _pluginDirectory = pluginDirectory;
            }

            protected override Assembly? Load(AssemblyName assemblyName)
            {
                string depPath = Path.Combine(_pluginDirectory, $"{assemblyName.Name}.dll");
                if (File.Exists(depPath))
                {
                    return LoadFromAssemblyPath(depPath);
                }

                return null;
            }
        }
    }
}
