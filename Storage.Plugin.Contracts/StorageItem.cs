namespace Storage.Plugin.Contracts;

public sealed class StorageItem
{
    public required string Path { get; init; }

    public required StorageItemType ItemType { get; init; }

    public long? Size { get; init; }

    public DateTimeOffset LastModified { get; init; }

    public IReadOnlyDictionary<string, string>? Metadata { get; init; } 
}