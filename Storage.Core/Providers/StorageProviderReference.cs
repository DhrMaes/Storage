namespace DhrMaes.Storage.Core.Providers
{
    public struct StorageProviderReference
    {
        public StorageProviderReference(string identifier)
        {
            if (String.IsNullOrEmpty(identifier))
            {
                throw new ArgumentNullException(identifier);
            }

            Identifier = identifier;
        }

        /// <summary>
        /// The unique identifier of this storage provider.
        /// </summary>
        public string Identifier { get; }
    }
}
