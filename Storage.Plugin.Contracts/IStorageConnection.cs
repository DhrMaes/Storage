namespace Storage.Plugin.Contracts;

public interface IStorageConnection : IAsyncDisposable
{
    StorageCapability Capabilities { get; }

    Task<IReadOnlyList<StorageItem>> ListAsync(string path, CancellationToken cancellationToken);

    Task<StorageItem?> GetInfoAsync(string path, CancellationToken cancellationToken);

    Task<Stream> OpenReadAsync(string path, CancellationToken cancellationToken);

    Task WriteAsync(string path, Stream content, StorageWriteOptions options, CancellationToken cancellationToken);

    Task DeleteASync(string path, CancellationToken cancellationToken);

    Task MoveAsync(string sourcePath, string destinationPath, CancellationToken cancellationToken);
}
