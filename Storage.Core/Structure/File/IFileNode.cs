namespace DhrMaes.Storage.Core.Structure.File
{
	using DhrMaes.Storage.Core.Structure;

	public interface IFileNode : IStorageNode
	{
		FileNode ToFileNode(IStorage storage);
	}
}
