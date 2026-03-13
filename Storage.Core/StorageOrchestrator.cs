namespace Storage.Core;

using Storage.Plugin.Contracts;
using Storage.Plugin.Loader;

public sealed class StorageOrchestrator : IAsyncDisposable
{
    private readonly PluginManager _pluginManager;
    private readonly StorageConnectionTracker _connectionTracker;
    private readonly Dictionary<string, IStorageConnection> _activeConnections = new();
    private readonly Dictionary<string, StorageProviderConfiguration> _providers = new();
    private string? _defaultProviderName;

    public PluginManager PluginManager => _pluginManager;

    public StorageOrchestrator(PluginManager pluginManager, StorageConnectionTracker? connectionTracker = null)
    {
        _pluginManager = pluginManager ?? throw new ArgumentNullException(nameof(pluginManager));
        _connectionTracker = connectionTracker ?? new StorageConnectionTracker();
    }

    public void ConfigureProvider(StorageProviderConfiguration config)
    {
        if (config is null)
            throw new ArgumentNullException(nameof(config));

        if (string.IsNullOrWhiteSpace(config.Name))
            throw new ArgumentException("Provider name cannot be null or empty.", nameof(config));

        if (!_pluginManager.IsPluginLoaded(config.PluginId))
            throw new InvalidOperationException($"Plugin '{config.PluginId}' is not loaded.");

        _providers[config.Name] = config;
    }

    public void SetDefaultProvider(string providerName)
    {
        if (string.IsNullOrWhiteSpace(providerName))
            throw new ArgumentException("Provider name cannot be null or empty.", nameof(providerName));
        
        if (!_providers.ContainsKey(providerName))
            throw new InvalidOperationException($"Provider '{providerName}' is not configured.");

        _defaultProviderName = providerName;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        foreach (var (providerName, config) in _providers)
        {
            var connection = await GetOrCreateConnectionAsync(providerName, cancellationToken);
            _activeConnections[providerName] = connection;
        }
    }

    public async Task<Stream> ReadAsync(string path, CancellationToken cancellationToken = default)
    {
        var connection = await GetDefaultConnectionAsync(cancellationToken);
        return await connection.OpenReadAsync(path, cancellationToken);
    }

    public async Task WriteAsync(string path, Stream content, bool overwrite = true, CancellationToken cancellationToken = default)
    {
        var connection = await GetDefaultConnectionAsync(cancellationToken);
        var options = new StorageWriteOptions
        {
            Overwrite = overwrite
        };
        await connection.WriteAsync(path, content, options, cancellationToken);
    }

    public async Task<IReadOnlyList<StorageItem>> ListAsync(string path = "/", CancellationToken cancellationToken = default)
    {
        var connection = await GetDefaultConnectionAsync(cancellationToken);
        return await connection.ListAsync(path, cancellationToken);
    }

    public async Task<StorageItem?> GetInfoAsync(string path, CancellationToken cancellationToken = default)
    {
        var connection = await GetDefaultConnectionAsync(cancellationToken);
        return await connection.GetInfoAsync(path, cancellationToken);
    }

    public async Task DeleteAsync(string path, CancellationToken cancellationToken = default)
    {
        var connection = await GetDefaultConnectionAsync(cancellationToken);
        await connection.DeleteASync(path, cancellationToken);
    }

    public async Task MoveAsync(string sourcePath, string destinationPath, CancellationToken cancellationToken = default)
    {
        var connection = await GetDefaultConnectionAsync(cancellationToken);
        await connection.MoveAsync(sourcePath, destinationPath, cancellationToken);
    }

    public async Task<bool> ExistsAsync(string path, CancellationToken cancellationToken = default)
    {
        var info = await GetInfoAsync(path, cancellationToken);
        return info is not null;
    }

    private async Task<IStorageConnection> GetDefaultConnectionAsync(CancellationToken cancellationToken)
    {
        if (_defaultProviderName is null)
            throw new InvalidOperationException("No default provider configured.");

        return await GetOrCreateConnectionAsync(_defaultProviderName, cancellationToken);
    }

    private async Task<IStorageConnection> GetOrCreateConnectionAsync(string providerName, CancellationToken cancellationToken)
    {
        if (_activeConnections.TryGetValue(providerName, out var existingConnection))
            return existingConnection;

        if (!_providers.TryGetValue(providerName, out var config))
            throw new InvalidOperationException($"Provider '{providerName}' is not configured.");

        var plugin = _pluginManager.GetPlugin(config.PluginId);
        if (plugin is null)
            throw new InvalidOperationException($"Plugin '{config.PluginId}' not found.");

        var connectionOptions = new StorageConnectionOptions
        {
            Settings = config.Settings
        };

        var connection = await plugin.Instance.CreateConnectionAsync(connectionOptions, cancellationToken);
        _connectionTracker.TrackConnection(plugin.Instance.Id, connection);

        _activeConnections[providerName] = connection;
        return connection;
    }

    public IReadOnlyDictionary<string, StorageProviderConfiguration> GetConfiguredProviders()
    {
        return _providers;
    }

    public string? GetDefaultProviderName()
    {
        return _defaultProviderName;
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var connection in _activeConnections.Values)
        {
            try
            {
                await connection.DisposeAsync();
            }
            catch
            {
                // Ignore disposal errors
            }
        }

        _activeConnections.Clear();
    }
}
