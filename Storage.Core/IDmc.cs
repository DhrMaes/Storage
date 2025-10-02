namespace DhrMaes.Storage.Core
{
	using DhrMaes.Storage.Core.Plugins;
	using DhrMaes.Storage.Core.Providers;
	using DhrMaes.Storage.Core.Structure.Nodes;

	public interface IDmc
	{
        ICollection<IStorageProvider> Providers { get; }

        Task<IReadOnlyDictionary<string, Type>> GetInstalledProviders();

        Task<IStorageProvider> AddProvider(IStorageProviderConfig config);

		Task RemoveProvider(string identifier);

		Task<IStorageProvider> GetProvider(string identifier);

		Task<ICollection<IStorageNode>> ListDirectory(DirectoryNode node);

		Task<bool> ExistsAsync(IStorageNode node);

		Task<Stream> OpenReadAsync(FileNode node, Func<ICollection<IStorageProvider>, IStorageProvider>? selector = null);
	}
}
