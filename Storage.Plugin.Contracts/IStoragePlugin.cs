namespace Storage.Plugin.Contracts;

public interface IStoragePlugin
{
    string Id { get; }

    string DisplayName { get; }

    Version Version { get; }

    Task InitializeAsync(
        StoragePluginContext context,
        CancellationToken cancellationToken
    );
    
    Task<IStorageConnection> CreateConnectionAsync(
        StorageConnectionOptions options,
        CancellationToken cancellationToken
    );
}
