namespace DhrMaes.Storage.Core.Providers
{
    using DhrMaes.Storage.Core.Structure;
    using DhrMaes.Storage.Core.Structure.File;
    using DhrMaes.Storage.Core.Structure.Directory;

    public interface IStorageProvider
    {
        /// <summary>
        /// The unique identifier of this storage provider.
        /// </summary>
        public string Identifier { get; }

        Task<bool> ExistsAsync(IBaseNode node, CancellationToken cancellationToken = default);

        Task<Stream> OpenReadAsync(IFileNodeReference node, CancellationToken cancellationToken = default);

        Task<Stream> OpenWriteAsync(IFileNodeReference node, CancellationToken cancellationToken = default);

        Task DeleteAsync(IBaseNode node, CancellationToken cancellationToken = default);

        Task<ICollection<IBaseNode>> ListAsync(IDirectoryNodeReference node, CancellationToken cancellationToken = default);

        Task<IFileNode> GetFileAsync(IFileNodeReference node, CancellationToken cancellationToken = default);

        Task CreateDirectoryAsync(IDirectoryNodeReference node, CancellationToken cancellationToken = default);

        Task CopyAsync(IDirectoryNodeReference source, IDirectoryNodeReference destination, CancellationToken cancellationToken = default);

        Task CopyAsync(IFileNodeReference source, IBaseNode destination, CancellationToken cancellationToken = default);

        Task MoveAsync(IDirectoryNodeReference source, IDirectoryNodeReference destination, CancellationToken cancellationToken = default);
        
        Task MoveAsync(IFileNodeReference source, IBaseNode destination, CancellationToken cancellationToken = default);
    }
}
