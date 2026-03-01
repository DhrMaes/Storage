using System.Reflection;
using System.Runtime.Loader;

namespace Storage.Plugin.Loader;

public sealed class PluginLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;

    public string PluginPath { get; }

    public PluginLoadContext(string pluginMainAssemblyPath)
        : base(isCollectible: true)
    {
        PluginPath = pluginMainAssemblyPath;
        _resolver = new AssemblyDependencyResolver(pluginMainAssemblyPath);
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        // Important: Prevent loading the contracts assembly into the plugin context.
        // We want the host's version to be used so type identity matches.
        if (IsSharedAssembly(assemblyName))
        {
            return null; // fallback to default load context
        }

        var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
        if (assemblyPath != null)
        {
            return LoadFromAssemblyPath(assemblyPath);
        }

        return null;
    }

    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        var libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        if (libraryPath != null)
        {
            return LoadUnmanagedDllFromPath(libraryPath);
        }

        return IntPtr.Zero;
    }

    private static bool IsSharedAssembly(AssemblyName assemblyName)
    {
        // Add more shared assemblies here if needed later
        return assemblyName.Name == "Storage.Plugin.Contracts";
    }
}