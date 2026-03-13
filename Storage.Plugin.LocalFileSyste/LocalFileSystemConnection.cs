namespace Storage.Plugin.LocalFileSyste;

using Storage.Plugin.Contracts;

public sealed class LocalFileSystemConnection : IStorageConnection
{
	private readonly string _rootPath;

	public LocalFileSystemConnection(string rootPath)
	{
		_rootPath = rootPath;
	}

	public StorageCapability Capabilities { get; } =
		StorageCapability.List |
		StorageCapability.Read |
		StorageCapability.Write |
		StorageCapability.Delete |
		StorageCapability.Move;

	public Task DeleteASync(string path, CancellationToken cancellationToken)
	{
		var fullPath = GetFullPath(path);

		if (File.Exists(fullPath))
		{
			File.Delete(fullPath);
		}
		else if (Directory.Exists(fullPath))
		{
			Directory.Delete(fullPath, recursive: true);
		}
		else
		{
			throw new FileNotFoundException($"Path not found: {path}");
		}

		return Task.CompletedTask;
	}

	public ValueTask DisposeAsync()
	{
		return ValueTask.CompletedTask;
	}

	public Task<StorageItem?> GetInfoAsync(string path, CancellationToken cancellationToken)
	{
		var fullPath = GetFullPath(path);

		if (File.Exists(fullPath))
		{
			var fileInfo = new FileInfo(fullPath);
			return Task.FromResult<StorageItem?>(new StorageItem
			{
				Path = path,
				ItemType = StorageItemType.File,
				Size = fileInfo.Length,
				LastModified = fileInfo.LastWriteTimeUtc,
				Metadata = null
			});
		}

		if (Directory.Exists(fullPath))
		{
			var dirInfo = new DirectoryInfo(fullPath);
			return Task.FromResult<StorageItem?>(new StorageItem
			{
				Path = path,
				ItemType = StorageItemType.Directory,
				Size = null,
				LastModified = dirInfo.LastWriteTimeUtc,
				Metadata = null
			});
		}

		return Task.FromResult<StorageItem?>(null);
	}

	public Task MoveAsync(string sourcePath, string destinationPath, CancellationToken cancellationToken)
	{
		var fullSourcePath = GetFullPath(sourcePath);
		var fullDestPath = GetFullPath(destinationPath);

		if (File.Exists(fullSourcePath))
		{
			var destDir = Path.GetDirectoryName(fullDestPath);
			if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
			{
				Directory.CreateDirectory(destDir);
			}

			File.Move(fullSourcePath, fullDestPath, overwrite: true);
		}
		else if (Directory.Exists(fullSourcePath))
		{
			Directory.Move(fullSourcePath, fullDestPath);
		}
		else
		{
			throw new FileNotFoundException($"Source path not found: {sourcePath}");
		}

		return Task.CompletedTask;
	}

	public Task<Stream> OpenReadAsync(string path, CancellationToken cancellationToken)
	{
		var fullPath = GetFullPath(path);

		if (!File.Exists(fullPath))
		{
			throw new FileNotFoundException($"File not found: {path}");
		}

		var stream = File.OpenRead(fullPath);
		return Task.FromResult<Stream>(stream);
	}

	public async Task WriteAsync(string path, Stream content, StorageWriteOptions options, CancellationToken cancellationToken)
	{
		var fullPath = GetFullPath(path);

		if (File.Exists(fullPath) && !options.Overwrite)
		{
			throw new IOException($"File already exists and overwrite is disabled: {path}");
		}

		var directory = Path.GetDirectoryName(fullPath);
		if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
		{
			Directory.CreateDirectory(directory);
		}

		using var fileStream = File.Create(fullPath);
		await content.CopyToAsync(fileStream, cancellationToken);
	}

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

	private string GetFullPath(string relativePath)
	{
		if (string.IsNullOrWhiteSpace(relativePath))
		{
			return _rootPath;
		}

		var normalizedPath = relativePath.Replace('/', Path.DirectorySeparatorChar);
		var fullPath = Path.Combine(_rootPath, normalizedPath);

		var fullPathNormalized = Path.GetFullPath(fullPath);
		var rootPathNormalized = Path.GetFullPath(_rootPath);

		if (!fullPathNormalized.StartsWith(rootPathNormalized, StringComparison.OrdinalIgnoreCase))
		{
			throw new UnauthorizedAccessException($"Path traversal detected: {relativePath}");
		}

		return fullPathNormalized;
	}
}