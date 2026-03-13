namespace Storage.Core;

using System.Text.Json;
using System.Text.Json.Serialization;
using Storage.Plugin.Loader;

public sealed class StorageConfigurationManager
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static async Task<StorageClusterConfiguration> LoadAsync(
        string configPath,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(configPath))
            throw new FileNotFoundException($"Configuration file not found: {configPath}");

        var json = await File.ReadAllTextAsync(configPath, cancellationToken);
        var config = JsonSerializer.Deserialize<StorageClusterConfiguration>(json, JsonOptions);

        if (config is null)
            throw new InvalidOperationException("Failed to deserialize configuration file.");

        return config;
    }

    public static async Task SaveAsync(
        StorageClusterConfiguration config,
        string configPath,
        CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(configPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(config, JsonOptions);
        await File.WriteAllTextAsync(configPath, json, cancellationToken);
    }

    public static async Task<StorageOrchestrator> CreateFromConfigAsync(
        string configPath,
        CancellationToken cancellationToken = default)
    {
        var config = await LoadAsync(configPath, cancellationToken);
        return await CreateFromConfigAsync(config, cancellationToken);
    }

    public static async Task<StorageOrchestrator> CreateFromConfigAsync(
        StorageClusterConfiguration config,
        CancellationToken cancellationToken = default)
    {
        var pluginManager = new PluginManager(config.PluginDirectory);
        pluginManager.LoadPlugins();

        var orchestrator = new StorageOrchestrator(pluginManager);

        foreach (var provider in config.Providers)
        {
            var expandedSettings = ExpandEnvironmentVariables(provider.Settings);

            var providerConfig = new StorageProviderConfiguration
            {
                Name = provider.Name,
                PluginId = provider.PluginId,
                Settings = expandedSettings,
            };

            orchestrator.ConfigureProvider(providerConfig);
            var defaultProviderName = orchestrator.GetDefaultProviderName();
            if (defaultProviderName is null)
                orchestrator.SetDefaultProvider(provider.Name);
        }

        if (config.DefaultProvider is not null)
            orchestrator.SetDefaultProvider(config.DefaultProvider);

        await orchestrator.InitializeAsync(cancellationToken);
        return orchestrator;
    }

    public static async Task SaveCurrentConfigurationAsync(
        StorageOrchestrator orchestrator,
        string pluginDirectory,
        string configPath,
        CancellationToken cancellationToken = default)
    {
        var providers = orchestrator.GetConfiguredProviders();
        var defaultProvider = orchestrator.GetDefaultProviderName();

        var config = new StorageClusterConfiguration
        {
            PluginDirectory = pluginDirectory,
            DefaultProvider = defaultProvider,
            Providers = providers.Values.ToList()
        };

        await SaveAsync(config, configPath, cancellationToken);
    }

    private static Dictionary<string, string> ExpandEnvironmentVariables(Dictionary<string, string> settings)
    {
        var expanded = new Dictionary<string, string>();

        foreach (var (key, value) in settings)
        {
            if (value.StartsWith("${") && value.EndsWith("}"))
            {
                var envVarName = value[2..^1];
                var envValue = Environment.GetEnvironmentVariable(envVarName);

                if (envValue is null)
                    throw new InvalidOperationException($"Environment variable '{envVarName}' not found for setting '{key}'.");

                expanded[key] = envValue;
            }
            else
            {
                expanded[key] = value;
            }
        }

        return expanded;
    }
}
