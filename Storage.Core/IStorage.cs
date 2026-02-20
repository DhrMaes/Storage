namespace DhrMaes.Storage.Core
{
    using DhrMaes.Storage.Core.Providers;
    using DhrMaes.Storage.Core.Structure.File;
    using DhrMaes.Storage.Core.Structure.Directory;
	using DhrMaes.Storage.Core.Services;

	public interface IStorage : IProviderService
    {
        void AddProvider(IStorageProviderConfig config);

        Task AddProviderAsync(IStorageProviderConfig config);


        bool DirectoryExists(string path);
        
        Task<bool> DirectoryExistsAsync(string path, CancellationToken cancellationToken = default);

        IDirectoryNode GetDirectory();

        IDirectoryNode GetDirectory(string path);

        Task<IDirectoryNode> GetDirectoryAsync(CancellationToken cancellationToken = default);

        Task<IDirectoryNode> GetDirectoryAsync(string path, CancellationToken cancellationToken = default);

        DirectoryNodeReference GetDirectoryReference();

        DirectoryNodeReference GetDirectoryReference(string path);
        
        ICollection<IDirectoryNode> GetDirectories();

        ICollection<IDirectoryNode> GetDirectories(string path);

        Task<ICollection<IDirectoryNode>> GetDirectoriesAsync(CancellationToken cancellationToken = default);

        Task<ICollection<IDirectoryNode>> GetDirectoriesAsync(string path, CancellationToken cancellationToken = default);

        ICollection<DirectoryNodeReference> GetDirectoryReferences();

        ICollection<DirectoryNodeReference> GetDirectoryReferences(string path);

        Task<ICollection<DirectoryNodeReference>> GetDirectoryReferencesAsync(CancellationToken cancellationToken = default);

        Task<ICollection<DirectoryNodeReference>> GetDirectoryReferencesAsync(string path, CancellationToken cancellationToken = default);


        bool FileExists(string path);

        Task<bool> FileExistsAsync(string path, CancellationToken cancellationToken = default);

        IFileNode GetFile(string path);

        Task<IFileNode> GetFileAsync(string path, CancellationToken cancellationToken = default);

        FileNodeReference GetFileReference(string path);
    }
}
