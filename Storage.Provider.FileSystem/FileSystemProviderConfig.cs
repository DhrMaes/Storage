namespace DhrMaes.Storage.Provider.FileSystem
{
	using DhrMaes.Storage.Core.Providers;

	[ProviderIdentifier("FileSystem")]
	public class FileSystemProviderConfig : IStorageProviderConfig
	{
		public string Path { get; set; }

		public IStorageProvider CreateProvider()
		{
			return new FileSystemProvider(Path);
		}
	}
}
