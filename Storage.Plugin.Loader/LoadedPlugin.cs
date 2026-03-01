namespace Storage.Plugin.Loader;

using System.Reflection;
using Storage.Plugin.Contracts;

public sealed class LoadedPlugin
{
    public IStoragePlugin Instance { get; }
    public PluginLoadContext LoadContext { get; }
    public Assembly Assembly { get; }
    public string DirectoryPath { get; }

    public LoadedPlugin(
        IStoragePlugin instance,
        PluginLoadContext loadContext,
        Assembly assembly,
        string directoryPath)
    {
        Instance = instance;
        LoadContext = loadContext;
        Assembly = assembly;
        DirectoryPath = directoryPath;
    }
}