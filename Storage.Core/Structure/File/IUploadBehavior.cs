namespace DhrMaes.Storage.Core.Structure.File
{
	using DhrMaes.Storage.Core.Providers;
	using DhrMaes.Storage.Core.Structure.Nodes;

	public interface IUploadBehavior
	{
		Task HandleUploadAsync(
			ICollection<IStorageProvider> providers, 
			FileNode node, 
			Stream content, 
			CancellationToken cancelToken);

    }
}
