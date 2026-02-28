namespace Storage.Plugin.Contracts;

public sealed class StorageWriteOptions
{
    public bool Overwrite { get; init; }

    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}