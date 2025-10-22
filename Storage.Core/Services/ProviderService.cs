namespace DhrMaes.Storage.Core.Services
{
    using System;
    using System.Collections.Generic;

    using DhrMaes.Storage.Core.Plugins;
    using DhrMaes.Storage.Core.Providers;

    internal class ProviderService : IProviderService
    {
        private readonly PluginLoader _pluginLoader;
        private readonly Dictionary<string, IStorageProvider> _providers = new Dictionary<string, IStorageProvider>(StringComparer.OrdinalIgnoreCase);

        private bool disposedValue;

        public ProviderService(PluginLoader pluginLoader)
        {
            ArgumentNullException.ThrowIfNull(pluginLoader);

            if (!pluginLoader.IsInitialized)
            {
                pluginLoader.LoadPlugins();
            }

            _pluginLoader = pluginLoader;
        }

        public bool ProviderExists(string identifier)
        {
            if (_providers.ContainsKey(identifier))
            {
                return true;
            }

            var files = Directory.GetFiles(FileSystem.FileSystem.GetProvidersDir(), $"{identifier}.provider", SearchOption.AllDirectories);
            return files.Length > 0;
        }

        public IStorageProvider GetProvider(string identifier)
        {
            if (!ProviderExists(identifier))
            {
                throw new ArgumentException($"The provider with identifier '{identifier}' was not found.");
            }

            if (_providers.TryGetValue(identifier, out var provider))
            {
                return provider;
            }

            var providersDir = FileSystem.FileSystem.GetProvidersDir();
            var configPaths = Directory.GetFiles(providersDir, $"{identifier}.provider", SearchOption.AllDirectories)
                .Where(i => i.EndsWith($"{identifier}.provider"))
                .ToArray();

            if (configPaths.Length > 1)
            {
                throw new InvalidDataException($"Multiple providers found for the same id '{identifier}'.");
            }

            var configPath = configPaths[0];
            var pluginType = Path.GetDirectoryName(configPath);
            if (String.IsNullOrEmpty(pluginType))
            {
                throw new InvalidDataException($"Could not find the provider type for this identifier '{identifier}'.");
            }

            var plugin = _pluginLoader.GetPlugin(pluginType);
            using var configStream = File.OpenRead(configPath);
            provider = plugin.CreateProviderFromStream(identifier, configStream).GetAwaiter().GetResult();

            return provider;
        }

        public StorageProviderReference GetProviderReference(string identifier)
        {
            if (!ProviderExists(identifier))
            {
                throw new ArgumentException($"The provider with identifier '{identifier}' was not found.");
            }

            return new StorageProviderReference(identifier);
        }

        public ICollection<IStorageProvider> GetProviders()
        {
            var providers = new List<IStorageProvider>();
            var configPaths = Directory.GetFiles(FileSystem.FileSystem.GetProvidersDir(), $"*.provider", SearchOption.AllDirectories);
            foreach(var configPath in configPaths)
            {
                var identifier = Path.GetFileNameWithoutExtension(configPath);
                var provider = GetProvider(identifier);
                providers.Add(provider);
            }

            return providers;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
