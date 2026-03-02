namespace Storage.Plugin.Loader;

using Storage.Plugin.Contracts;

public sealed class StoragePluginLoader
{
    private readonly string _pluginRootPath;
    private readonly List<LoadedPlugin> _loadedPlugins = new();

    public IReadOnlyCollection<LoadedPlugin> LoadedPlugins => _loadedPlugins.AsReadOnly();

    public StoragePluginLoader(string pluginRootPath)
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
            TryLoadPlugin(pluginDirectory);
        }
    }

    private void TryLoadPlugin(string pluginDirectory)
    {
        try
        {
            var pluginAssemblyPath = FindPluginAssembly(pluginDirectory);
            if (pluginAssemblyPath is null)
                return;

            var loadContext = new PluginLoadContext(pluginAssemblyPath);
            var assembly = loadContext.LoadFromAssemblyPath(pluginAssemblyPath);
            var pluginTypes = assembly
                .GetTypes()
                .Where(t =>
                    typeof(IStoragePlugin).IsAssignableFrom(t) &&
                    !t.IsAbstract &&
                    !t.IsInterface);

            foreach (var type in pluginTypes)
            {
                if (Activator.CreateInstance(type) is not IStoragePlugin pluginInstance)
                    continue;

                _loadedPlugins.Add(new LoadedPlugin(
                    pluginInstance,
                    loadContext,
                    assembly,
                    pluginDirectory));
            }
        }
        catch (Exception ex)
        {
            // Do NOT crash entire system because one plugin misbehaves.
            // Log and continue.
            Console.WriteLine($"Failed to load plugin from '{pluginDirectory}': {ex}");
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