namespace DhrMaes.Storage.Core.Providers
{
	using DhrMaes.Storage.Core.Structure.Nodes;

	public interface IStorageProvider
    {
		/// <summary>
		/// The unique identifier of this storage provider.
		/// </summary>
		public string Identifier { get; }

		Task<bool> ExistsAsync(IStorageNode node, CancellationToken cancellationToken = default);

		Task<Stream> OpenReadAsync(FileNode node, CancellationToken cancellationToken = default);

		Task<Stream> OpenWriteAsync(FileNode node, CancellationToken cancellationToken = default);

		Task DeleteAsync(IStorageNode node, CancellationToken cancellationToken = default);

		Task<ICollection<IStorageNode>> ListAsync(DirectoryNode node, CancellationToken cancellationToken = default);

		Task CreateDirectoryAsync(DirectoryNode node, CancellationToken cancellationToken = default);

		Task CopyAsync(IStorageNode source, IStorageNode destination, CancellationToken cancellationToken = default);

		Task MoveAsync(IStorageNode source, IStorageNode destination, CancellationToken cancellationToken = default);
	}
}
