namespace DhrMaes.Storage.Core
{
    using DhrMaes.Storage.Core.Providers;
    using DhrMaes.Storage.Core.Structure.File;
    using DhrMaes.Storage.Core.Structure.Directory;

    public interface IStorage
    {
        bool ProviderExists(string identifier);

        IStorageProvider GetProvider(string identifier);

        StorageProviderReference GetProviderReference(string identifier);

        ICollection<IStorageProvider> GetProviders();


        bool DirectoryExists(string path);

        IDirectoryNode GetDirectory();

        IDirectoryNode GetDirectory(string path);

        DirectoryNodeReference GetDirectoryReference();

        DirectoryNodeReference GetDirectoryReference(string path);

        ICollection<DirectoryNode> GetDirectories();

        ICollection<DirectoryNode> GetDirectories(string path);

        ICollection<DirectoryNodeReference> GetDirectoryReferences();

        ICollection<DirectoryNodeReference> GetDirectoryReferences(string path);


        bool FileExists(string path);

        IFileNode GetFile(string path);

        FileNodeReference GetFileReference(string path);
    }
}
