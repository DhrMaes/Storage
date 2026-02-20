namespace DhrMaes.Storage.Core.Structure.File
{
	using DhrMaes.Storage.Core.FileSystem;
	using DhrMaes.Storage.Core.Structure;

	public interface IFileNode : IFileNodeReference, IStorageNode
	{
		FileSize Size { get; }

		Stream OpenRead();

		Task<Stream> OpenReadAsync(CancellationToken cancellationToken = default);
	}
}
