namespace Storage.Core;

public sealed class StorageProviderConfiguration
{
	public required string Name { get; init; }

	public required string PluginId { get; init; }

	public required Dictionary<string, string> Settings { get; init; }
}
