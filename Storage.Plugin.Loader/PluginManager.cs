namespace Storage.Plugin.Loader;

using System.Collections.Concurrent;
using Storage.Plugin.Contracts;

public sealed class PluginManager
{
    private readonly string _pluginRootPath;
    private readonly ConcurrentDictionary<string, LoadedPlugin> _plugins = new(StringComparer.OrdinalIgnoreCase);

    public PluginManager(string pluginRootPath)
    {
        _pluginRootPath = pluginRootPath
            ?? throw new ArgumentNullException(nameof(pluginRootPath));
    }

    public void LoadPlugins()
    {
        if (!Directory.Exists(_pluginRootPath))
            return;

        foreach (var pluginDirectory in Directory.GetDirectories(_pluginRootPath))
        {
            var plugins = TryLoadPluginsForDirectory(pluginDirectory);
            foreach (var plugin in plugins)
            {
                _plugins.TryAdd(plugin.Instance.Id, plugin);
            }
        }
    }

    public LoadedPlugin? GetPlugin(string pluginId)
    {
        _plugins.TryGetValue(pluginId, out var plugin);
        return plugin;
    }

    public IStoragePlugin? GetPluginInstance(string pluginId)
    {
        return GetPlugin(pluginId)?.Instance;
    }

    public bool TryGetPlugin(string pluginId, out LoadedPlugin? plugin)
    {
        return _plugins.TryGetValue(pluginId, out plugin);
    }

    public bool IsPluginLoaded(string pluginId)
    {
        return _plugins.ContainsKey(pluginId);
    }

    public async Task<bool> UnloadPluginAsync(
        string pluginId,
        StorageConnectionTracker? connectionTracker = null,
        TimeSpan? connectionDrainTimeout = null,
        CancellationToken cancellationToken = default)
    {
        var plugin = GetPlugin(pluginId);
        if (plugin is null)
            return false;

        if (plugin.State == PluginState.Unloading || plugin.State == PluginState.Unloaded)
            return false;

        try
        {
            plugin.State = PluginState.Unloading;

            if (connectionTracker is not null)
            {
                var timeout = connectionDrainTimeout ?? TimeSpan.FromSeconds(30);
                var drainCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                drainCts.CancelAfter(timeout);

                try
                {
                    while (connectionTracker.HasActiveConnections(pluginId))
                    {
                        connectionTracker.CleanupDeadReferences();

                        if (!connectionTracker.HasActiveConnections(pluginId))
                            break;

                        await Task.Delay(100, drainCts.Token);
                    }
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine($"Connection drain timeout for plugin '{pluginId}'. Force disposing remaining connections.");
                    await connectionTracker.DisposeAllConnectionsAsync(pluginId, cancellationToken);
                }

                connectionTracker.Untrack(pluginId);
            }

            plugin.LoadContext.Unload();

            for (int i = 0; i < 10 && plugin.LoadContext.IsCollectible; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                await Task.Delay(50, cancellationToken);
            }

            plugin.State = PluginState.Unloaded;
            plugin.UnloadedAt = DateTimeOffset.UtcNow;
            _plugins.TryRemove(pluginId, out _);

            return true;
        }
        catch (Exception ex)
        {
            plugin.State = PluginState.Failed;
            Console.WriteLine($"Failed to unload plugin '{pluginId}': {ex}");
            return false;
        }
    }

    public async Task<LoadedPlugin?> ReloadPluginAsync(
        string pluginId,
        StorageConnectionTracker? connectionTracker = null,
        TimeSpan? connectionDrainTimeout = null,
        CancellationToken cancellationToken = default)
    {
        var oldPlugin = GetPlugin(pluginId);
        if (oldPlugin is null)
            return null;

        var pluginDirectory = oldPlugin.DirectoryPath;

        var unloaded = await UnloadPluginAsync(pluginId, connectionTracker, connectionDrainTimeout, cancellationToken);
        if (!unloaded)
        {
            Console.WriteLine($"Failed to unload plugin '{pluginId}' during reload.");
            return null;
        }

        await Task.Delay(100, cancellationToken);

        var newPlugins = TryLoadPluginsForDirectory(pluginDirectory);
        var newPlugin = newPlugins.FirstOrDefault(p => 
            string.Equals(p.Instance.Id, pluginId, StringComparison.OrdinalIgnoreCase));

        if (newPlugin is null)
        {
            Console.WriteLine($"Plugin '{pluginId}' not found in reloaded assembly.");
            return null;
        }

        _plugins.TryAdd(pluginId, newPlugin);
        Console.WriteLine($"Successfully reloaded plugin '{pluginId}' (v{newPlugin.Instance.Version})");

        return newPlugin;
    }

    private static IEnumerable<LoadedPlugin> TryLoadPluginsForDirectory(string pluginDirectory)
    {
        try
        {
            var pluginAssemblyPath = FindPluginAssembly(pluginDirectory);
            if (pluginAssemblyPath is null)
                return Array.Empty<LoadedPlugin>();

            var loadContext = new PluginLoadContext(pluginAssemblyPath);
            var assembly = loadContext.LoadFromAssemblyPath(pluginAssemblyPath);
            var pluginTypes = assembly
                .GetTypes()
                .Where(t =>
                    typeof(IStoragePlugin).IsAssignableFrom(t) &&
                    !t.IsAbstract &&
                    !t.IsInterface);

            var plugins = new List<LoadedPlugin>();
            foreach (var type in pluginTypes)
            {
                if (Activator.CreateInstance(type) is not IStoragePlugin pluginInstance)
                    continue;

                var plugin = new LoadedPlugin(
                    pluginInstance,
                    loadContext,
                    assembly,
                    pluginDirectory);

                plugins.Add(plugin);
            }

            return plugins;
        }
        catch (Exception ex)
        {
            // Do NOT crash entire system because one plugin misbehaves.
            // Log and continue.
            Console.WriteLine($"Failed to load plugin from '{pluginDirectory}': {ex}");
            return Array.Empty<LoadedPlugin>();
        }
    }

    private static string? FindPluginAssembly(string pluginDirectory)
    {
        // Convention-based: main assembly name = folder name
        var folderName = Path.GetFileName(pluginDirectory);
        var expectedAssemblyPath = Path.Combine(pluginDirectory, folderName + ".dll");

        if (File.Exists(expectedAssemblyPath))
            return expectedAssemblyPath;

        throw new DllNotFoundException($"Could not find main plugin dll '{folderName}' in '{pluginDirectory}'");
    }
}