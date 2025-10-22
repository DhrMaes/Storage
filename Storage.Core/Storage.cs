namespace DhrMaes.Storage.Core
{
    using DhrMaes.Storage.Core.Providers;
    using DhrMaes.Storage.Core.Structure.File;
    using DhrMaes.Storage.Core.Structure.Directory;
    using System.Collections.Generic;
	using DhrMaes.Storage.Core.Services;

	public class Storage : IStorage
    {
        private readonly IProviderService _providerService;

        public Storage(IProviderService providerService)
        {
            _providerService = providerService;
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
        public ICollection<IStorageProvider> GetProviders()
        {
            return _providerService.GetProviders();
        }


        public bool DirectoryExists(string path) => throw new NotImplementedException();
        public IDirectoryNode GetDirectory() => throw new NotImplementedException();
        public IDirectoryNode GetDirectory(string path) => throw new NotImplementedException();
        public DirectoryNodeReference GetDirectoryReference() => DirectoryNodeReference.Root;
        public DirectoryNodeReference GetDirectoryReference(string path) => DirectoryNodeReference.FromPath(path);
        public ICollection<DirectoryNode> GetDirectories() => throw new NotImplementedException();
        public ICollection<DirectoryNode> GetDirectories(string path) => throw new NotImplementedException();
        public ICollection<DirectoryNodeReference> GetDirectoryReferences() => throw new NotImplementedException();
        public ICollection<DirectoryNodeReference> GetDirectoryReferences(string path) => throw new NotImplementedException();


        public bool FileExists(string path) => throw new NotImplementedException();
        public IFileNode GetFile(string path) => throw new NotImplementedException();
        public FileNodeReference GetFileReference(string path) => throw new NotImplementedException();
    }
}
