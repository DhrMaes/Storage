using Storage.Plugin.Contracts;

public sealed class LocalFileSystemConnection : IStorageConnection
{
	private readonly string _rootPath;

	public LocalFileSystemConnection(string rootPath)
	{
		_rootPath = rootPath;
	}

	public StorageCapability Capabilities { get; }

	public Task DeleteASync(string path, CancellationToken cancellationToken) => throw new NotImplementedException();

	public ValueTask DisposeAsync() => throw new NotImplementedException();

	public Task<StorageItem?> GetInfoAsync(string path, CancellationToken cancellationToken) => throw new NotImplementedException();

	public Task MoveAsync(string sourcePath, string destinationPath, CancellationToken cancellationToken) => throw new NotImplementedException();

	public Task<Stream> OpenReadAsync(string path, CancellationToken cancellationToken) => throw new NotImplementedException();

	public Task WriteAsync(string path, Stream content, StorageWriteOptions options, CancellationToken cancellationToken) => throw new NotImplementedException();

	public Task<IReadOnlyList<StorageItem>> ListAsync(string path, CancellationToken cancellationToken)
	{
		var fullPath = Path.Combine(_rootPath, path);
		if (!Directory.Exists(fullPath))
		{
			return Task.FromResult(Array.Empty<StorageItem>().AsReadOnly() as IReadOnlyList<StorageItem>);
		}

		var dirInfo = new DirectoryInfo(fullPath);
		var entries = dirInfo.EnumerateFileSystemInfos()
			.Select(info =>
			{
				var itemType = info switch
				{
					FileInfo => StorageItemType.File,
					DirectoryInfo => StorageItemType.Directory,
					_ => throw new InvalidOperationException("Unknown file system entry type.")
				};

				var size = info is FileInfo fileInfo ? fileInfo.Length : (long?)null;
				return new StorageItem
				{
					Path = Path.Combine(path, info.Name).Replace('\\', '/'), // Normalize
					ItemType = itemType,
					Size = size,
					LastModified = info.LastWriteTimeUtc,
					Metadata = null,
				};
			});

		return Task.FromResult(entries.ToList().AsReadOnly() as IReadOnlyList<StorageItem>);
	}
}