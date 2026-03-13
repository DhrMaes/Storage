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
			if (!options.Settings.TryGetValue("RootPath", out var rootPath))
			{
				throw new ArgumentException("RootPath setting is required for LocalFileSystem provider.", nameof(options));
			}

			if (string.IsNullOrWhiteSpace(rootPath))
			{
				throw new ArgumentException("RootPath cannot be empty.", nameof(options));
			}

			if (!Directory.Exists(rootPath))
			{
				Directory.CreateDirectory(rootPath);
			}

			var connection = new LocalFileSystemConnection(rootPath);
			return Task.FromResult<IStorageConnection>(connection);
		}
	}
}
