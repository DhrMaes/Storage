namespace Storage.Plugin.Contracts;

public sealed class StoragePluginContext
{
    public required IStorageLogger Logger { get; init; }
}