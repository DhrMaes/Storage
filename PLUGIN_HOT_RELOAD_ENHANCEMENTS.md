# Plugin Hot Reload - Future Enhancements

This document outlines potential enhancements to the plugin hot reload system that could be implemented in the future.

---

## ✅ Currently Implemented

- ✅ Collectible `AssemblyLoadContext` for plugin isolation
- ✅ Connection tracking with `WeakReference` (no memory leaks)
- ✅ Safe unload with connection drain and timeout
- ✅ Hot reload capability (unload + reload)
- ✅ Plugin state tracking (Loading, Active, Unloading, Unloaded, Failed)
- ✅ Timestamp tracking (LoadedAt, UnloadedAt)
- ✅ Multiple GC cycles for proper cleanup
- ✅ Error handling and logging

---

## 🚀 Future Enhancements

### 1. Automatic File Watching

**Description:** Automatically detect when plugin DLLs change on disk and trigger hot reload.

**Implementation:**
```csharp
public class PluginFileWatcher : IDisposable
{
    private readonly FileSystemWatcher _watcher;
    private readonly StoragePluginLoader _loader;
    private readonly Dictionary<string, DateTime> _lastChangeTime = new();
    private readonly TimeSpan _debounceDelay = TimeSpan.FromSeconds(2);

    public PluginFileWatcher(string pluginPath, StoragePluginLoader loader)
    {
        _loader = loader;
        _watcher = new FileSystemWatcher(pluginPath)
        {
            Filter = "*.dll",
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName,
            IncludeSubdirectories = true
        };

        _watcher.Changed += OnPluginFileChanged;
        _watcher.Created += OnPluginFileChanged;
        _watcher.EnableRaisingEvents = true;
    }

    private async void OnPluginFileChanged(object sender, FileSystemEventArgs e)
    {
        // Debounce: Wait for file writes to complete
        var fileName = e.FullPath;
        _lastChangeTime[fileName] = DateTime.UtcNow;
        
        await Task.Delay(_debounceDelay);
        
        if (DateTime.UtcNow - _lastChangeTime[fileName] < _debounceDelay)
            return; // Another write occurred, wait more

        // Extract plugin ID from path and trigger reload
        var pluginId = ExtractPluginIdFromPath(fileName);
        await _loader.ReloadPluginAsync(pluginId);
    }

    public void Dispose() => _watcher?.Dispose();
}
```

**Benefits:**
- No manual reload commands needed
- Instant reflection of code changes
- Great for development workflow

---

### 2. Reload Events/Notifications

**Description:** Provide events so consumers can react to plugin lifecycle changes.

**Implementation:**
```csharp
public class PluginReloadEventArgs : EventArgs
{
    public string PluginId { get; init; }
    public Version? OldVersion { get; init; }
    public Version? NewVersion { get; init; }
    public bool Success { get; init; }
    public Exception? Error { get; init; }
}

public sealed class StoragePluginLoader
{
    public event EventHandler<PluginReloadEventArgs>? PluginLoading;
    public event EventHandler<PluginReloadEventArgs>? PluginLoaded;
    public event EventHandler<PluginReloadEventArgs>? PluginUnloading;
    public event EventHandler<PluginReloadEventArgs>? PluginUnloaded;
    public event EventHandler<PluginReloadEventArgs>? PluginReloaded;
    public event EventHandler<PluginReloadEventArgs>? PluginFailed;

    // Allow vetoing unload
    public event EventHandler<CancelEventArgs>? PluginUnloadRequested;
}
```

**Usage Example:**
```csharp
loader.PluginReloaded += (sender, e) => 
{
    Console.WriteLine($"Plugin {e.PluginId} reloaded: v{e.OldVersion} → v{e.NewVersion}");
    
    // Update registry
    registry.Unregister(e.PluginId);
    registry.Register(loader.FindLoadedPlugin(e.PluginId)!);
};

loader.PluginUnloadRequested += (sender, e) =>
{
    // Veto unload if critical operation in progress
    if (IsCriticalOperationRunning())
    {
        e.Cancel = true;
    }
};
```

**Benefits:**
- Clean integration with application logic
- Automatic registry updates
- Ability to prevent unload if unsafe

---

### 3. Rollback on Failed Reload

**Description:** If new plugin version fails to load, keep the old version active.

**Implementation:**
```csharp
public async Task<LoadedPlugin?> ReloadPluginWithRollbackAsync(
    string pluginId,
    StorageConnectionTracker? connectionTracker = null,
    CancellationToken cancellationToken = default)
{
    var oldPlugin = FindLoadedPlugin(pluginId);
    if (oldPlugin is null)
        return null;

    // Keep old plugin data for rollback
    var oldVersion = oldPlugin.Instance.Version;
    var pluginDirectory = oldPlugin.DirectoryPath;

    // Try to load new version FIRST (before unloading old)
    LoadedPlugin? newPlugin;
    try
    {
        var pluginAssemblyPath = FindPluginAssembly(pluginDirectory);
        var loadContext = new PluginLoadContext(pluginAssemblyPath);
        var assembly = loadContext.LoadFromAssemblyPath(pluginAssemblyPath);
        
        // ... create new plugin instance ...
        newPlugin = new LoadedPlugin(/* ... */);
        
        // Validate new version loads correctly
        await newPlugin.Instance.InitializeAsync(context, cancellationToken);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Failed to load new version of '{pluginId}': {ex}");
        Console.WriteLine($"Keeping old version v{oldVersion} active.");
        return oldPlugin; // Rollback: keep old version
    }

    // New version loaded successfully, now unload old
    var unloaded = await UnloadPluginAsync(pluginId, connectionTracker, cancellationToken: cancellationToken);
    
    if (!unloaded)
    {
        Console.WriteLine($"Failed to unload old version. Cleaning up new version.");
        newPlugin.LoadContext.Unload();
        return oldPlugin; // Rollback: keep old version
    }

    _loadedPlugins.Add(newPlugin);
    return newPlugin;
}
```

**Benefits:**
- Zero downtime reloads
- System stays operational even if new version is broken
- Safe experimentation with plugin updates

---

### 4. Plugin Dependency Management

**Description:** Handle plugins that depend on other plugins (reload dependent plugins together).

**Implementation:**
```csharp
public sealed class LoadedPlugin
{
    // Add dependency tracking
    public IReadOnlyList<string> DependsOn { get; init; } = Array.Empty<string>();
}

public sealed class StoragePluginLoader
{
    public async Task<IReadOnlyList<LoadedPlugin>> ReloadPluginWithDependenciesAsync(
        string pluginId,
        StorageConnectionTracker? connectionTracker = null,
        CancellationToken cancellationToken = default)
    {
        // Build dependency graph
        var toReload = new List<string> { pluginId };
        
        // Find all plugins that depend on this one (transitive)
        var dependents = FindAllDependents(pluginId);
        toReload.AddRange(dependents);

        // Unload in reverse dependency order (dependents first)
        toReload.Reverse();
        foreach (var id in toReload)
        {
            await UnloadPluginAsync(id, connectionTracker, cancellationToken: cancellationToken);
        }

        // Reload in dependency order (dependencies first)
        toReload.Reverse();
        var reloadedPlugins = new List<LoadedPlugin>();
        foreach (var id in toReload)
        {
            var reloaded = await ReloadPluginAsync(id, connectionTracker, cancellationToken: cancellationToken);
            if (reloaded is not null)
                reloadedPlugins.Add(reloaded);
        }

        return reloadedPlugins.AsReadOnly();
    }

    private List<string> FindAllDependents(string pluginId)
    {
        var result = new List<string>();
        foreach (var plugin in _loadedPlugins)
        {
            if (plugin.DependsOn.Contains(pluginId, StringComparer.OrdinalIgnoreCase))
            {
                result.Add(plugin.Instance.Id);
                // Recursively find dependents
                result.AddRange(FindAllDependents(plugin.Instance.Id));
            }
        }
        return result.Distinct().ToList();
    }
}
```

**Plugin Contract Enhancement:**
```csharp
public interface IStoragePlugin
{
    // ... existing members ...
    
    // New: Declare dependencies
    IReadOnlyList<string> Dependencies { get; }
}
```

**Benefits:**
- Maintains plugin consistency
- Prevents broken state from partial reloads
- Automatic dependency resolution

---

### 5. Health Checks After Reload

**Description:** Validate plugin works correctly after reload before marking it as active.

**Implementation:**
```csharp
public interface IStoragePlugin
{
    // New: Health check method
    Task<bool> HealthCheckAsync(CancellationToken cancellationToken);
}

public async Task<LoadedPlugin?> ReloadPluginAsync(...)
{
    // ... reload logic ...
    
    var newPlugin = new LoadedPlugin(/* ... */);
    
    // Health check
    try
    {
        var healthy = await newPlugin.Instance.HealthCheckAsync(cancellationToken);
        if (!healthy)
        {
            Console.WriteLine($"Health check failed for '{pluginId}'. Unloading.");
            await UnloadPluginAsync(pluginId);
            return null; // Could rollback here instead
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Health check threw exception for '{pluginId}': {ex}");
        await UnloadPluginAsync(pluginId);
        return null;
    }
    
    newPlugin.State = PluginState.Active;
    return newPlugin;
}
```

**Benefits:**
- Catch initialization issues early
- Prevent broken plugins from becoming active
- Better observability

---

### 6. Plugin Versioning & Multiple Versions

**Description:** Support multiple versions of same plugin loaded simultaneously (for gradual migration).

**Implementation:**
```csharp
public sealed class LoadedPlugin
{
    // Change ID to include version
    public string PluginId => Instance.Id;
    public string VersionedId => $"{Instance.Id}@{Instance.Version}";
}

public sealed class StoragePluginRegistry
{
    // Support both lookups
    public LoadedPlugin? GetPlugin(string pluginId, Version? version = null)
    {
        if (version is null)
            return GetLatestVersion(pluginId);
        
        return _plugins.Values.FirstOrDefault(p => 
            p.Instance.Id == pluginId && 
            p.Instance.Version == version);
    }

    public LoadedPlugin? GetLatestVersion(string pluginId)
    {
        return _plugins.Values
            .Where(p => p.Instance.Id == pluginId)
            .OrderByDescending(p => p.Instance.Version)
            .FirstOrDefault();
    }
}
```

**Benefits:**
- A/B testing of plugin versions
- Gradual rollout (some connections use v1, new ones use v2)
- Safe migration path

---

### 7. Reload Metrics & Monitoring

**Description:** Track reload statistics for observability.

**Implementation:**
```csharp
public sealed class PluginReloadMetrics
{
    public int TotalReloads { get; private set; }
    public int SuccessfulReloads { get; private set; }
    public int FailedReloads { get; private set; }
    public TimeSpan AverageReloadTime { get; private set; }
    public Dictionary<string, int> ReloadCountByPlugin { get; } = new();
    
    public void RecordReload(string pluginId, TimeSpan duration, bool success)
    {
        TotalReloads++;
        if (success) SuccessfulReloads++; else FailedReloads++;
        
        ReloadCountByPlugin.TryGetValue(pluginId, out var count);
        ReloadCountByPlugin[pluginId] = count + 1;
        
        // Update average
        // ...
    }
}
```

**Benefits:**
- Identify problematic plugins
- Track reload frequency
- Performance monitoring

---

## Implementation Priority

If implementing these features, suggested order:

1. **Events/Notifications** (Low effort, high value)
2. **Rollback on Failed Reload** (Medium effort, high reliability)
3. **Health Checks** (Low effort, good safety)
4. **Automatic File Watching** (Low effort, great DX)
5. **Reload Metrics** (Low effort, good observability)
6. **Plugin Dependencies** (High effort, situational value)
7. **Multiple Versions** (High effort, complex scenarios only)

---

## Notes

- Consider whether automatic reloading is desired in production vs. development
- File watching adds complexity and can cause issues with file locks
- Events provide the most flexibility for integration
- Rollback is critical for production stability
- Dependencies and versioning are advanced features needed only in complex scenarios
