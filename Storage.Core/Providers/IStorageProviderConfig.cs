namespace DhrMaes.Storage.Core.Providers
{
	public interface IStorageProviderConfig
	{
		public IStorageProvider CreateProvider();
	}
}
