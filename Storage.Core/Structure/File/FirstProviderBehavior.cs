namespace DhrMaes.Storage.Core.Structure.File
{
    using DhrMaes.Storage.Core.Providers;
    using DhrMaes.Storage.Core.Structure.Nodes;

    internal class FirstProviderBehavior : IUploadBehavior
    {
        public async Task HandleUploadAsync(ICollection<IStorageProvider> providers, FileNode node, Stream content, CancellationToken cancelToken)
        {
            var provider = providers.FirstOrDefault();
            if (provider is null)
            {
                throw new Exception("No storage providers are configured.");
            }

            using (var writeStream = await provider.OpenWriteAsync(node, cancelToken))
            {
                await content.CopyToAsync(writeStream);
            }
        }
    }
}
