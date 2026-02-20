namespace DhrMaes.Storage.Core.Services
{
    using System.Collections.Generic;

    using DhrMaes.Storage.Core.Plugins;
    using DhrMaes.Storage.Core.Providers;

    public interface IProviderService : IDisposable
    {
        IStoragePlugin GetPlugin(string identifier);

        IReadOnlyCollection<IStoragePlugin> GetPlugins();

        bool ProviderExists(string identifier);

        IStorageProvider GetProvider(string identifier);

        StorageProviderReference GetProviderReference(string identifier);

        IReadOnlyList<IStorageProvider> GetProviders();
    }
}
