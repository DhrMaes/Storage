namespace DhrMaes.Storage.Core
{
	using System.Collections.Generic;
	using System.Threading.Tasks;

	using DhrMaes.Storage.Core.Plugins;
	using DhrMaes.Storage.Core.Providers;
	using DhrMaes.Storage.Core.Services;
	using DhrMaes.Storage.Core.Structure;
	using DhrMaes.Storage.Core.Structure.Directory;
	using DhrMaes.Storage.Core.Structure.File;

	public class Storage : IStorage
	{
		private readonly IProviderService _providerService;
		private bool disposedValue;

		public Storage(IProviderService providerService)
		{
			_providerService = providerService;
		}


		public void AddProvider(IStorageProviderConfig config) => AddProviderAsync(config).GetAwaiter().GetResult();
		public Task AddProviderAsync(IStorageProviderConfig config)
		{
			throw new NotImplementedException();
		}

		public IStoragePlugin GetPlugin(string identifier)
		{
			return _providerService.GetPlugin(identifier);
		}
		public IReadOnlyCollection<IStoragePlugin> GetPlugins()
		{
			return _providerService.GetPlugins();
		}
		public bool ProviderExists(string identifier)
		{
			return _providerService.ProviderExists(identifier);
		}
		public IStorageProvider GetProvider(string identifier)
		{
			return _providerService.GetProvider(identifier);
		}
		public StorageProviderReference GetProviderReference(string identifier)
		{
			return _providerService.GetProviderReference(identifier);
		}
		public IReadOnlyList<IStorageProvider> GetProviders()
		{
			return _providerService.GetProviders();
		}


		public bool DirectoryExists(string path) => DirectoryExistsAsync(path).GetAwaiter().GetResult();
		public async Task<bool> DirectoryExistsAsync(string path, CancellationToken cancellationToken = default)
		{
			var node = DirectoryNodeReference.FromPath(path);
			var tasks = new List<Task<bool>>();
			using var cts = new CancellationTokenSource();
			foreach (var provider in _providerService.GetProviders())
			{
				tasks.Add(Task.Run(() =>
				{
					return provider.ExistsAsync(node, cts.Token);
				}));
			}

			// As soon as a task finished check if it returned true
			// If so cancel all the others since we know the directory exists
			// in at least one provider
			while (tasks.Count > 0)
			{
				var finished = await Task.WhenAny(tasks);
				tasks.Remove(finished);

				if (await finished)
				{
					await cts.CancelAsync();
					return true;
				}
			}

			return false;
		}
		public IDirectoryNode GetDirectory() => GetDirectoryAsync().GetAwaiter().GetResult();
		public IDirectoryNode GetDirectory(string path) => GetDirectoryAsync(path).GetAwaiter().GetResult();
		public Task<IDirectoryNode> GetDirectoryAsync(CancellationToken cancellationToken = default) => GetDirectoryAsync("/");
		public async Task<IDirectoryNode> GetDirectoryAsync(string path, CancellationToken cancellationToken = default)
		{
			var node = DirectoryNodeReference.FromPath(path);
			var tasks = new List<Task<ICollection<IBaseNode>>>();
			using var cts = new CancellationTokenSource();
			foreach (var provider in _providerService.GetProviders())
			{
				tasks.Add(Task.Run(() =>
				{
					return provider.ListAsync(node, cts.Token);
				}));
			}

			var directories = new List<IDirectoryNodeReference>();
			var files = new List<IFileNodeReference>();

			// Wait for all tasks to finish
			// And progressively build the directory contents
			while (tasks.Count > 0)
			{
				var finished = await Task.WhenAny(tasks);
				tasks.Remove(finished);

				foreach (var childNode in await finished)
				{
					if (childNode is IDirectoryNodeReference dirRef)
					{
						directories.Add(dirRef);
					}
					else if (childNode is IFileNodeReference fileRef)
					{
						files.Add(fileRef);
					}
				}
			}

			return new DirectoryNode(this, node, directories, files);
		}
		public DirectoryNodeReference GetDirectoryReference() => DirectoryNodeReference.Root;
		public DirectoryNodeReference GetDirectoryReference(string path) => DirectoryNodeReference.FromPath(path);
		public ICollection<IDirectoryNode> GetDirectories() => GetDirectoriesAsync().GetAwaiter().GetResult();
		public ICollection<IDirectoryNode> GetDirectories(string path) => GetDirectoriesAsync(path).GetAwaiter().GetResult();
		public Task<ICollection<IDirectoryNode>> GetDirectoriesAsync(CancellationToken cancellationToken = default) => GetDirectoriesAsync("/");
		public async Task<ICollection<IDirectoryNode>> GetDirectoriesAsync(string path, CancellationToken cancellationToken = default)
		{
			var dirReferences = await GetDirectoryReferencesAsync(path);
			var tasks = new List<Task<IDirectoryNode>>();
			using var cts = new CancellationTokenSource();
			foreach (var dirRef in dirReferences)
			{
				tasks.Add(Task.Run(() =>
				{
					return GetDirectoryAsync(dirRef.GetFullPath());
				}));
			}

			var directories = new List<IDirectoryNode>();

			// Wait for all tasks to finish
			// And progressively build the directory contents
			while (tasks.Count > 0)
			{
				var finished = await Task.WhenAny(tasks);
				tasks.Remove(finished);

				var dir = await finished;
				if (dir is not null)
				{
					directories.Add(dir);
				}
			}

			return directories;
		}
		public ICollection<DirectoryNodeReference> GetDirectoryReferences() => GetDirectoryReferencesAsync().GetAwaiter().GetResult();
		public ICollection<DirectoryNodeReference> GetDirectoryReferences(string path) => GetDirectoryReferencesAsync(path).GetAwaiter().GetResult();
		public Task<ICollection<DirectoryNodeReference>> GetDirectoryReferencesAsync(CancellationToken cancellationToken = default) => GetDirectoryReferencesAsync("/");
		public async Task<ICollection<DirectoryNodeReference>> GetDirectoryReferencesAsync(string path, CancellationToken cancellationToken = default)
		{
			var node = await GetDirectoryAsync(path);
			return node.Directories.Select(dir =>
			{
				if (dir is DirectoryNodeReference dirReference)
				{
					return dirReference;
				}

				return new DirectoryNodeReference(dir.Name)
				{
					Parent = dir.Parent,
				};
			}).ToList();
		}


		public bool FileExists(string path) => FileExistsAsync(path).GetAwaiter().GetResult();
		public async Task<bool> FileExistsAsync(string path, CancellationToken cancellationToken = default)
		{
			var node = FileNodeReference.FromPath(path);
			var tasks = new List<Task<bool>>();
			using var cts = new CancellationTokenSource();
			foreach (var provider in _providerService.GetProviders())
			{
				tasks.Add(Task.Run(() =>
				{
					return provider.ExistsAsync(node, cts.Token);
				}));
			}

			// As soon as a task finished check if it returned true
			// If so cancel all the others since we know the directory exists
			// in at least one provider
			while (tasks.Count > 0)
			{
				var finished = await Task.WhenAny(tasks);
				tasks.Remove(finished);

				if (await finished)
				{
					await cts.CancelAsync();
					return true;
				}
			}

			return false;
		}
		public IFileNode GetFile(string path) => GetFileAsync(path).GetAwaiter().GetResult();
		public async Task<IFileNode> GetFileAsync(string path, CancellationToken cancellationToken = default)
		{
			var node = FileNodeReference.FromPath(path);
			var tasks = new List<Task<(bool, string)>>();
			using var cts = new CancellationTokenSource();
			foreach (var provider in _providerService.GetProviders())
			{
				tasks.Add(Task.Run(async () =>
				{
					var exists = await provider.ExistsAsync(node, cts.Token);
					return (exists, provider.Identifier);
				}));
			}

			while (tasks.Count > 0)
			{
				var finished = await Task.WhenAny(tasks);
				tasks.Remove(finished);

				var (exists, identifier) = await finished;
				if (exists)
				{
					await cts.CancelAsync();
					var provider = _providerService.GetProvider(identifier);
					return await provider.GetFileAsync(node);
				}
			}

			throw new InvalidOperationException($"Could not find the specified file: {path}");
		}
		public FileNodeReference GetFileReference(string path) => FileNodeReference.FromPath(path);


		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					// Dispose managed state (managed objects)
					_providerService.Dispose();
				}

				// Free unmanaged resources (unmanaged objects) and override finalizer
				// Set large fields to null
				disposedValue = true;
			}
		}

		public void Dispose()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}
	}
}
