namespace DhrMaes.Storage.Provider.GoogleDrive
{
    using System;
    using System.IO;
    using System.Text.Json;
    using System.Threading.Tasks;

    using Google.Apis.Util.Store;

    internal class StorageDataStore : IDataStore
    {
        private readonly JsonSerializerOptions serializerOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
        };

        private readonly string _directoryPath;
        private readonly string _identifier;

        public StorageDataStore(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier))
                throw new ArgumentNullException(nameof(identifier));

            _identifier = identifier;
            _directoryPath = Path.Combine(
                DhrMaes.Storage.Core.FileSystem.FileSystem.GetProvidersDir(),
                "GoogleDrive");

            // Create the directory if it doesn't exist
            Directory.CreateDirectory(_directoryPath);
        }

        private string GetFilePath(string key)
        {
            // Replace invalid filename chars to keep key safe for filesystem
            foreach (var c in Path.GetInvalidFileNameChars())
            {
                key = key.Replace(c, '_');
            }

            return Path.Combine(_directoryPath, $"{_identifier}.provider");
        }

        public Task ClearAsync()
        {
            foreach (var file in Directory.EnumerateFiles(_directoryPath, $"*.provider"))
            {
                File.Delete(file);
            }
            return Task.CompletedTask;
        }

        public Task DeleteAsync<T>(string key)
        {
            var path = GetFilePath(key);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            return Task.CompletedTask;
        }

        public async Task<T> GetAsync<T>(string key)
        {
            var path = GetFilePath(key);
            if (!File.Exists(path))
            {
                return default!;
            }

            using var stream = File.OpenRead(path);
            return await JsonSerializer.DeserializeAsync<T>(stream);
        }

        public async Task StoreAsync<T>(string key, T value)
        {
            var path = GetFilePath(key);
            using var stream = File.Create(path);
            await JsonSerializer.SerializeAsync(stream, value, serializerOptions);
        }
    }
}
