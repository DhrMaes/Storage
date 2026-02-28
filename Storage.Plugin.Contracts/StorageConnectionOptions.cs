namespace Storage.Plugin.Contracts;

public sealed class StorageConnectionOptions
{
    public required IReadOnlyDictionary<string, string> Settings { get; init; }
}