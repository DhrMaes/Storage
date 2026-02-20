namespace DhrMaes.Storage.Core.Plugins
{
	using DhrMaes.Storage.Core.Providers;

	public interface IStoragePlugin
    {
        string Name { get; }

        Type ConfigType { get; }

        Type ProviderType { get; }

        IReadOnlyDictionary<string, PluginProperty> Properties { get; }

        Task<IStorageProviderConfig> CreateConfigFromProperties(string identifier, Dictionary<PluginProperty, object> properties);

        Task<IStorageProvider> CreateProviderFromProperties(string identifier, Dictionary<PluginProperty, object> properties);

        Task<IStorageProviderConfig> CreateConfigFromStream(string identifier, Stream stream);

        Task<IStorageProvider> CreateProviderFromStream(string identifier, Stream stream);

        Task<IStorageProvider> CreateProviderFromConfig(IStorageProviderConfig config);
    }
}
