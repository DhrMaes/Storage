namespace DhrMaes.Storage.Core.Plugins
{
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;

    using DhrMaes.Storage.Core.Providers;

    public class StoragePlugin : IStoragePlugin
    {
        public StoragePlugin(
            string name, 
            Type configType, 
            Type providerType,
            IReadOnlyDictionary<string, PluginProperty> properties)
        {
            Name = name;
            ConfigType = configType;
            ProviderType = providerType;
            Properties = properties;
        }

        public string Name { get; }

        public Type ConfigType { get; }

        public Type ProviderType { get; }

        public IReadOnlyDictionary<string, PluginProperty> Properties { get; }

        public async Task<IStorageProviderConfig> CreateConfigFromProperties(string identifier, Dictionary<PluginProperty, object> properties)
        {
            var config = Activator.CreateInstance(ConfigType) as IStorageProviderConfig;
            if (config == null)
            {
                throw new InvalidOperationException($"Config type {ConfigType.FullName} does not implement IStorageProviderConfig.");
			}

            foreach(var property in properties)
            {
                property.Key.SetValue(config, property.Value);
			}

            await config.InitializeConfigAsync(identifier);
			return config;
		}
        
        public async Task<IStorageProvider> CreateProviderFromProperties(string identifier, Dictionary<PluginProperty, object> properties)
        {
			var config = await CreateConfigFromProperties(identifier, properties);
			var provider = await config.CreateProviderAsync();
			return provider;
		}

        public async Task<IStorageProviderConfig> CreateConfigFromStream(string identifier, Stream stream)
        {
            var config = Activator.CreateInstance(ConfigType) as IStorageProviderConfig;
            if (config == null)
            {
                throw new InvalidOperationException($"Config type {ConfigType.FullName} does not implement IStorageProviderConfig.");
            }

            await config.FromStreamAsync(identifier, stream);
            return config;
        }

        public async Task<IStorageProvider> CreateProviderFromStream(string identifier, Stream stream)
        {
            var config = await CreateConfigFromStream(identifier, stream);
            var provider = await config.CreateProviderAsync();
            return provider;
        }

        public async Task<IStorageProvider> CreateProviderFromConfig(IStorageProviderConfig config)
        {
            if (!config.GetType().IsAssignableFrom(ConfigType))
            {
                throw new InvalidOperationException($"Config type {config.GetType().FullName} is not assignable to expected type {ConfigType.FullName}.");
            }

            var provider = await config.CreateProviderAsync();
            return provider;
        }
    }
}
