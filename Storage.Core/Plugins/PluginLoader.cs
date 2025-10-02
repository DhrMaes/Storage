namespace DhrMaes.Storage.Core.Plugins
{
    using System.Reflection;
    using System.Runtime.Loader;
    using System.Text.Json;

    using DhrMaes.Storage.Core.Providers;

    public class PluginLoader
    {
        private readonly string _pluginPath;

        private readonly IDictionary<string, Type> _providerTypes;

        public PluginLoader() : this(Path.Combine(FileSystem.FileSystem.GetUserConfigDir(), "Plugins"))
        {
        }

        public PluginLoader(string pluginPath)
        {
            _pluginPath = pluginPath;
            _providerTypes = LoadStorageProviderTypes();
        }

        public IReadOnlyDictionary<string, Type> ConfigTypes => (IReadOnlyDictionary<string, Type>)_providerTypes;

        /// <summary>
        /// Load all available storage provider types from the plugins directory.
        /// </summary>
        /// <returns></returns>
        public IDictionary<string, Type> LoadStorageProviderTypes()
        {
            var providerTypes = new Dictionary<string, Type>();

            if (!Directory.Exists(_pluginPath))
            {
                return providerTypes;
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

                    var types = assembly.GetTypes()
                        .Where(t => typeof(IStorageProviderConfig).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
                        .Select(t => new
                        {
                            Identifier = t.GetCustomAttribute<ProviderIdentifierAttribute>(),
                            Type = t,
                        })
                        .Where(m => m.Identifier is not null)
                        .Select(m => new KeyValuePair<string, Type>(m.Identifier!.Id, m.Type))
                        .ToList();

                    foreach (var type in types)
                    {
                        if (!providerTypes.TryAdd(type.Key, type.Value))
                        {
                            throw new InvalidOperationException($"Duplicate provider identifier '{type.Key}' found in plugin '{file}'.");
                        }
                    }
                }
            }

            return providerTypes;
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
