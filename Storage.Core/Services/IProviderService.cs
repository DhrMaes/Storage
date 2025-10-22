namespace DhrMaes.Storage.Core.Services
{
    using System.Collections.Generic;

    using DhrMaes.Storage.Core.Providers;

    public interface IProviderService : IDisposable
    {
        bool ProviderExists(string identifier);

        IStorageProvider GetProvider(string identifier);

        StorageProviderReference GetProviderReference(string identifier);

        ICollection<IStorageProvider> GetProviders();
    }
}
