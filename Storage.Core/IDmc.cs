namespace DhrMaes.Storage.Core
{
	using DhrMaes.Storage.Core.Providers;

	public interface IDmc
	{
		ICollection<IStorageProvider> GetProviders();

		IStorageProvider AddProvider(IStorageProviderConfig config);
	}
}
