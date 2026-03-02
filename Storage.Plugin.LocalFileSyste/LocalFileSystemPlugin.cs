namespace Storage.Plugin.LocalFileSyste
{
	using Storage.Plugin.Contracts;

	public class LocalFileSystemPlugin : IStoragePlugin
	{
		public string Id => "LocalFileSystem";

		public string DisplayName => "Local File System";

		public Version Version => new Version(1, 0, 0);

		public Task InitializeAsync(StoragePluginContext context, CancellationToken cancellationToken)
		{
			return Task.CompletedTask;
		}

		public Task<IStorageConnection> CreateConnectionAsync(StorageConnectionOptions options, CancellationToken cancellationToken)
		{
			return Task.
		}
	}
}
