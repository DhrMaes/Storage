namespace DhrMaes.Storage.Provider.GoogleDrive
{
    using DhrMaes.Storage.Core.Providers;

    using Google.Apis.Auth.OAuth2;
    using Google.Apis.Drive.v3;
    using Google.Apis.Services;

    [ProviderIdentifier("GoogleDrive")]
    public class GoogleDriveStorageProviderConfig : IStorageProviderConfig
    {
        private readonly string[] _scopes = new[]
        {
            DriveService.Scope.Drive
        };

        private ICredential? _credential;
        private StorageDataStore _dataStore;

        [StorageExclude]
        public string Identifier { get; private set; } = String.Empty;

        [StorageExclude]
        public string CredentialsJsonPath { get; set; } = "credentials.json";

        [StorageExclude]
        public string ApplicationName { get; set; } = "DhrMaes.Storage.GoogleDrive";

        [StorageExclude]
        public string RootFolderId { get; set; } = "root";


        public async Task InitializeConfigAsync(string identifier)
        {
            Identifier = identifier ?? throw new ArgumentNullException(nameof(identifier));
            _dataStore = new StorageDataStore(identifier);

            var credentialPath = Path.Join(
                Path.GetDirectoryName(typeof(GoogleDriveStorageProviderConfig).Assembly.Location),
                CredentialsJsonPath);
            using (var clientStream = new FileStream(credentialPath, FileMode.Open, FileAccess.Read))
            {
                // The custom data store will store the token in the correct place.
                // The CopyToStreamAsync will have nothing to do.
                _credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    (await GoogleClientSecrets.FromStreamAsync(clientStream)).Secrets,
                    _scopes,
                    "user",
                    CancellationToken.None,
                    _dataStore);
            }
        }

        public async Task<IStorageProvider> CreateProviderAsync()
        {
            var service = new DriveService(new BaseClientService.Initializer
            {
                HttpClientInitializer = _credential,
                ApplicationName = ApplicationName,
            });

            return new GoogleDriveStorageProvider(Identifier, service, RootFolderId);
        }

        public async Task FromStreamAsync(string identifier, Stream stream)
        {
            Identifier = identifier ?? throw new ArgumentNullException(nameof(identifier));
            _dataStore = new StorageDataStore(identifier);

            var credentialPath = Path.Join(
                Path.GetDirectoryName(typeof(GoogleDriveStorageProviderConfig).Assembly.Location),
                CredentialsJsonPath);
            using (var clientStream = new FileStream(credentialPath, FileMode.Open, FileAccess.Read))
            {
                // The "token.json" will cache the user's refresh token after first consent
                _credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    (await GoogleClientSecrets.FromStreamAsync(clientStream)).Secrets,
                    _scopes,
                    "user",
                    CancellationToken.None,
                    _dataStore);
            }
        }

        public Task CopyToStreamAsync(Stream stream)
        {
            // The custom data store already saves it in the correct place
            return Task.CompletedTask;
        }
    }
}
