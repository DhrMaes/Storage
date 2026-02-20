namespace DhrMaes.Storage.Core.Structure.Directory
{
	using DhrMaes.Storage.Core.Structure;
	using DhrMaes.Storage.Core.Structure.File;

	public interface IDirectoryNode : IDirectoryNodeReference, IStorageNode
	{
        IReadOnlyCollection<IDirectoryNodeReference> Directories { get; }

        IReadOnlyCollection<IFileNodeReference> Files { get; }

		void CreateDirectory(string name);

		Task CreateDirectoryAsync(string name, CancellationToken cancellationToken = default);

		void Remove();

		Task RemoveAsync(CancellationToken cancellationToken = default);
    }
}
