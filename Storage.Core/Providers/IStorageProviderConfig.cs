namespace DhrMaes.Storage.Core.Providers
{
    /// <summary>
    /// Defines the configuration interface for a storage provider.
    /// </summary>
    public interface IStorageProviderConfig
    {
        /// <summary>
        /// Gets the unique identifier for the storage provider configuration.
        /// </summary>
        public string Identifier { get; }

        /// <summary>
        /// Creates a new storage provider configuration using default settings or sources.
        /// </summary>
        /// <param name="identifier">The unique identifier for the storage provider configuration.</param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// </returns>
        Task InitializeConfigAsync(string identifier);

        /// <summary>
        /// Creates a new storage provider configuration from the specified stream.
        /// </summary>
        /// <param name="identifier">The unique identifier for the storage provider configuration.</param>
        /// <param name="stream">The stream containing the configuration data.</param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// </returns>
        Task FromStreamAsync(string identifier, Stream stream);

        /// <summary>
        /// Writes the current storage provider configuration to the specified stream asynchronously.
        /// </summary>
        /// <param name="stream">The stream to which the configuration will be written.</param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// </returns>
        Task CopyToStreamAsync(Stream stream);

        /// <summary>
        /// Creates a storage provider instance using the current configuration.
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains the created <see cref="IStorageProvider"/>.
        /// </returns>
        Task<IStorageProvider> CreateProviderAsync();
    }
}
