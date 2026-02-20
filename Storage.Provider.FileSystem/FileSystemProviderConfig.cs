namespace DhrMaes.Storage.Provider.FileSystem
{
	using System.ComponentModel;
	using System.IO;
	using System.Text.Json;

	using DhrMaes.Storage.Core.Providers;

	[ProviderIdentifier("FileSystem")]
	public class FileSystemProviderConfig : IStorageProviderConfig
	{
		private readonly JsonSerializerOptions _serializerOptions = new JsonSerializerOptions
		{
			WriteIndented = true,
		};

        [StorageExclude]
        public string Identifier { get; private set; } = String.Empty;

		[Description("Sets the path to the directory to be used for the FileStorageProvider.")]
        public string Path { get; set; }

		public Task InitializeConfigAsync(string identifier)
		{
            Identifier = identifier ?? throw new ArgumentNullException(nameof(identifier));
            return Task.FromResult((IStorageProviderConfig)this);
		}

		public Task<IStorageProvider> CreateProviderAsync()
		{
			return Task.FromResult((IStorageProvider)new FileSystemProvider(Identifier, Path));
		}

		public async Task FromStreamAsync(string identifier, Stream stream)
		{
            Identifier = identifier ?? throw new ArgumentNullException(nameof(identifier));
            var data = await JsonSerializer.DeserializeAsync<ConfigData>(stream);
            if (string.IsNullOrEmpty(data?.Path))
            {
                throw new ArgumentException("Path is required in the configuration data.");
            }

			Path = data.Path;
        }
		
		public async Task CopyToStreamAsync(Stream stream)
		{
            await JsonSerializer.SerializeAsync<ConfigData>(
				stream, 
				new ConfigData
				{
					Path = Path,
				},
                _serializerOptions);
        }

		private sealed class ConfigData
		{
            public string Path { get; set; } = String.Empty;
        }
    }
}
