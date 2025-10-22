namespace DhrMaes.Storage.Core.Structure.Directory
{
	using DhrMaes.Storage.Core.Structure;

	public interface IDirectoryNode : IStorageNode
	{
		DirectoryNode ToDirectoryNode(IStorage storage);
	}
}
