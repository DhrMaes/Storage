namespace Storage.Core;

using System.Text.Json;
using System.Text.Json.Serialization;

public sealed class StorageClusterConfiguration
{
	[JsonPropertyName("pluginDirectory")]
	public required string PluginDirectory { get; init; }

	[JsonPropertyName("providers")]
	public List<StorageProviderConfiguration> Providers { get; init; } = new();

	[JsonPropertyName("defaultProvider")]
	public string? DefaultProvider { get; init; }
}
