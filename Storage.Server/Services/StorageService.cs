namespace DhrMaes.Storage.Server.Services
{
    using System.Threading.Tasks;

    using DhrMaes.Storage.Core;
    using DhrMaes.Storage.Core.Providers;
    using DhrMaes.Storage.Core.Structure.Directory;
    using DhrMaes.Storage.Core.Structure.File;
    using DhrMaes.Storage.Protobuf.FileSystem.Directory.v1;
    using DhrMaes.Storage.Protobuf.FileSystem.File.v1;
    using DhrMaes.Storage.Protobuf.Structure.v1;
    using DhrMaes.Storage.Server.Nodes;

    using Google.Protobuf.Collections;

    using Grpc.Core;

    public class StorageService : DhrMaes.Storage.Protobuf.FileSystem.v1.StorageService.StorageServiceBase
    {
        private static readonly Random _random = new();

        private readonly IStorage _storage;
        private readonly ILogger<StorageService> _logger;

        public StorageService(
            IStorage storage,
            ILogger<StorageService> logger)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async override Task<DirectoryExistsResponse> DirectoryExists(DirectoryExistsRequest request, ServerCallContext context)
        {
            var exists = await _storage.DirectoryExistsAsync(request.Path);
            return new DirectoryExistsResponse
            {
                Exists = exists,
            };
        }

        public async override Task<ListDirectoryResponse> ListDirectory(ListDirectoryRequest request, ServerCallContext context)
        {
            var node = await _storage.GetDirectoryAsync(request.Path);
            var translated = NodeTranslator.Translate(node);
            return new ListDirectoryResponse
            {
                Nodes =
                {
                    translated?.Directory.Children ?? new RepeatedField<StorageNode>(),
                },
            };
        }

        public override async Task<MakeDirectoryResponse> MakeDirectory(MakeDirectoryRequest request, ServerCallContext context)
        {
            var dirReference = DirectoryNodeReference.FromPath(request.Path);
            if (dirReference.Parent is null)
            {
                var rootDirectory = await _storage.GetDirectoryAsync();
                await rootDirectory.CreateDirectoryAsync(dirReference.Name);
                return new MakeDirectoryResponse();
            }

            var toBeCreated = new Stack<IDirectoryNodeReference>([(IDirectoryNodeReference)dirReference]);
            var parentDirReference = dirReference.Parent ?? DirectoryNodeReference.Root;
            while (!await _storage.DirectoryExistsAsync(parentDirReference.GetFullPath()))
            {
                toBeCreated.Push(parentDirReference);
                parentDirReference = parentDirReference.Parent ?? DirectoryNodeReference.Root;
            }

            foreach (var toCreate in toBeCreated)
            {
                IDirectoryNode parentNode;
                if (toCreate.Parent is null)
                {
                    parentNode = await _storage.GetDirectoryAsync();
                }
                else
                {
                    parentNode = await _storage.GetDirectoryAsync(toCreate.Parent.GetFullPath());
                }

                await parentNode.CreateDirectoryAsync(toCreate.Name);
            }

            return new MakeDirectoryResponse();
        }

        public override async Task<RemoveDirectoryResponse> RemoveDirectory(RemoveDirectoryRequest request, ServerCallContext context)
        {
            var dirReference = DirectoryNodeReference.FromPath(request.Path);
            var directory = await _storage.GetDirectoryAsync(dirReference.GetFullPath());
            await directory.RemoveAsync();
            return new RemoveDirectoryResponse();
        }

        public async override Task OpenRead(OpenReadRequest request, IServerStreamWriter<OpenReadResponse> responseStream, ServerCallContext context)
        {
            var fileRef = FileNodeReference.FromPath(request.FileName);
            var file = await _storage.GetFileAsync(fileRef.GetFullPath());
            using var stream = await file.OpenReadAsync(context.CancellationToken);

            // Write the initial info message
            await responseStream.WriteAsync(new OpenReadResponse
            {
                Info = new FileInfo
                {
                    FileName = file.Name,
                    FileSize = file.Size.Size,
                },
            });

            // Read the stream and write chunks
            const int chunkSize = 64 * 1024;
            var buffer = new byte[chunkSize];
            var offset = 0L;
            int bytesRead;
            while ((bytesRead = await stream.ReadAsync(buffer, 0, chunkSize, context.CancellationToken)) > 0)
            {
                await responseStream.WriteAsync(new OpenReadResponse
                {
                    Chunk = new FileChunk
                    {
                        Content = Google.Protobuf.ByteString.CopyFrom(buffer, 0, bytesRead),
                        Offset = offset,
                    },
                });

                offset += bytesRead;
            }
        }

        public override async Task<OpenWriteResponse> OpenWrite(IAsyncStreamReader<OpenWriteRequest> requestStream, ServerCallContext context)
        {
            // Step 1: Read the first message (should contain FileInfo)
            if (!await requestStream.MoveNext(context.CancellationToken))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "No initial message received."));
            }

            var firstMessage = requestStream.Current;
            if (firstMessage.Info == null)
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "First message must contain file info."));
            }

            var fileRef = FileNodeReference.FromPath(firstMessage.Info.FileName);
            var provider = default(IStorageProvider);
            if (!String.IsNullOrEmpty(firstMessage.ProviderIdentifier) &&
                _storage.ProviderExists(firstMessage.ProviderIdentifier))
            {
                provider = _storage.GetProvider(firstMessage.ProviderIdentifier);
            }
            else
            {
                var providers = _storage.GetProviders();
                provider = providers[_random.Next(providers.Count)];
            }

            try
            {
                using var writeStream = await provider.OpenWriteAsync(fileRef, context.CancellationToken);

                // Step 2: Write all subsequent chunks to the file
                while (await requestStream.MoveNext(context.CancellationToken))
                {
                    var message = requestStream.Current;
                    if (message.Chunk == null || message.Chunk.Content == null)
                    {
                        continue; // Ignore empty chunk messages
                    }

                    var buffer = message.Chunk.Content.ToByteArray();
                    await writeStream.WriteAsync(buffer, 0, buffer.Length, context.CancellationToken);
                }

                await writeStream.FlushAsync(context.CancellationToken);

                return new OpenWriteResponse
                {
                    Success = true,
                    Message = "File written successfully.",
                    SavedPath = fileRef.GetFullPath(),
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                throw new RpcException(new Status(StatusCode.Aborted, "Write has been aborted."));
            }
        }
    }
}
