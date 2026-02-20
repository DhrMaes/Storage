namespace DhrMaes.Storage.Core.Plugins
{
	using System.Collections.ObjectModel;
	using System.ComponentModel;
	using System.Reflection;
	using System.Runtime.Loader;

	using DhrMaes.Storage.Core.Providers;

	public class PluginLoader
	{
		private readonly string _pluginPath;
		private readonly List<IStoragePlugin> _plugins = new List<IStoragePlugin>();

		private bool _initialized = false;

		public PluginLoader() : this(Path.Combine(FileSystem.FileSystem.GetUserConfigDir(), "Plugins"))
		{
		}

		public PluginLoader(string pluginPath)
		{
			_pluginPath = pluginPath;
		}

		public event EventHandler<EventArgs> Loaded;

		public event EventHandler<EventArgs> Reloaded;

		public bool IsInitialized => _initialized;

		public IReadOnlyCollection<IStoragePlugin> Plugins => new ReadOnlyCollection<IStoragePlugin>(_plugins);

		public IStoragePlugin GetPlugin(string pluginIdentifier)
		{
			var plugin = _plugins.FirstOrDefault(p => p.Name.Equals(pluginIdentifier, StringComparison.OrdinalIgnoreCase));
			if (plugin is null)
			{
				throw new InvalidOperationException($"No plugin found with identifier '{pluginIdentifier}'.");
			}

			return plugin;
		}

		/// <summary>
		/// Load all available storage provider types from the plugins directory.
		/// </summary>
		/// <returns></returns>
		public void LoadPlugins()
		{
			if (_initialized)
			{
				return;
			}

			_plugins.Clear();
			InternalLoadPlugins();
			_initialized = true;
			Loaded?.Invoke(this, EventArgs.Empty);
		}

		public void ReloadPlugins()
		{
			_plugins.Clear();
			InternalLoadPlugins();
			_initialized = true;
			Reloaded?.Invoke(this, EventArgs.Empty);
		}

		private void InternalLoadPlugins()
		{
			if (!Directory.Exists(_pluginPath))
			{
				throw new InvalidOperationException($"Plugin directory not found: {_pluginPath}");
			}

			foreach (var dir in Directory.GetDirectories(_pluginPath, "*", SearchOption.TopDirectoryOnly))
			{
				InternalLoadPlugin(dir);
			}
		}

		private void InternalLoadPlugin(string dir)
		{
            var loadContext = new PluginLoadContext(dir);
            foreach (var file in Directory.EnumerateFiles(dir, "*.dll", SearchOption.TopDirectoryOnly))
            {
                var assembly = loadContext.LoadFromAssemblyPath(file);
                if (assembly == null)
                {
                    continue;
                }

                var types = assembly.GetTypes();

                var configTypes = types
                    .Where(t => typeof(IStorageProviderConfig).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
                    .Select(t => new
                    {
                        Identifier = t.GetCustomAttribute<ProviderIdentifierAttribute>(),
                        Type = t,
                    })
                    .Where(m => m.Identifier is not null)
                    .ToDictionary(m => m.Identifier!.Id, m => m.Type);

                var providerTypes = types
                    .Where(t => typeof(IStorageProvider).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
                    .Select(t => new
                    {
                        Identifier = t.GetCustomAttribute<ProviderIdentifierAttribute>(),
                        Type = t,
                    })
                    .Where(m => m.Identifier is not null)
                    .ToDictionary(m => m.Identifier!.Id, m => m.Type);

                foreach (var provider in configTypes.Keys)
                {
                    var configType = configTypes[provider];
                    if (!providerTypes.TryGetValue(provider, out var providerType))
                    {
                        continue;
                    }

                    _plugins.Add(new StoragePlugin(provider, configType, providerType, GetPluginProperties(configType)));
                }
            }
        }

		private static IReadOnlyDictionary<string, PluginProperty> GetPluginProperties(Type type)
		{
			var properties = new Dictionary<string, PluginProperty>();
			var propertyInfos = new List<PropertyInfo>();
			
			// Public properties are included by default
			foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
			{
				var excludeAttr = prop.GetCustomAttribute<StorageExcludeAttribute>();
				if (excludeAttr is not null)
				{
					continue;
				}

				propertyInfos.Add(prop);
			}

			// Private properties need to be explicitly marked with the StoragePropertyAttribute
			foreach (var prop in type.GetProperties(BindingFlags.NonPublic | BindingFlags.Instance))
			{
				var attr = prop.GetCustomAttribute<StoragePropertyAttribute>();
				if (attr is null)
				{
					continue;
				}

				propertyInfos.Add(prop);
			}

			// Parse all the found properties
			foreach(var prop in propertyInfos)
			{
				if (!PluginProperty.TryGetType(prop, out var propertyType))
				{
					continue;
				}

				var name = prop.Name;
				var propertyAttr = prop.GetCustomAttribute<StoragePropertyAttribute>();
				if (propertyAttr is not null)
				{
					name = propertyAttr.Name;
				}

				var description = $"Sets the {name}";
				var descriptionAttr = prop.GetCustomAttribute<DescriptionAttribute>();
				if (descriptionAttr is not null)
				{
					description = descriptionAttr.Description;
				}

				properties.Add(name, new PluginProperty(
					name,
					description,
					propertyType,
					prop));
			}

			return new ReadOnlyDictionary<string, PluginProperty>(properties);
		}

		private sealed class PluginLoadContext : AssemblyLoadContext
		{
			private readonly string _pluginDirectory;

			public PluginLoadContext(string pluginDirectory)
			{
				_pluginDirectory = pluginDirectory;
			}

			protected override Assembly? Load(AssemblyName assemblyName)
			{
				string depPath = Path.Combine(_pluginDirectory, $"{assemblyName.Name}.dll");
				if (File.Exists(depPath))
				{
					return LoadFromAssemblyPath(depPath);
				}

				return null;
			}
		}
	}
}
