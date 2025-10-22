namespace DhrMaes.Storage.Core.Plugins
{
	using DhrMaes.Storage.Core.Providers;

	public interface IStoragePlugin
    {
        string Name { get; }

        Type ConfigType { get; }

        Type ProviderType { get; }

        Task<IStorageProviderConfig> CreateConfigFromStream(string identifier, Stream stream);

        Task<IStorageProvider> CreateProviderFromStream(string identifier, Stream stream);

        Task<IStorageProvider> CreateProviderFromConfig(IStorageProviderConfig config);
    }
}
