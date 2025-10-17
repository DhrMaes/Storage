namespace DhrMaes.Storage.Server.Services
{
    using System.Threading.Tasks;

    using DhrMaes.Storage.Protobuf.FileSystem.Directory.v1;

    using DhrMaes.Storage.Core;
    using DhrMaes.Storage.Core.Structure.Nodes;

    using Grpc.Core;

    using DhrMaes.Storage.Server.Nodes;
    using DhrMaes.Storage.Protobuf.FileSystem.File.v1;

    public class StorageService : DhrMaes.Storage.Protobuf.FileSystem.v1.StorageService.StorageServiceBase
    {
        private readonly IDmc _dmc;
        private readonly ILogger<StorageService> _logger;

        public StorageService(
            IDmc dmc,
            ILogger<StorageService> logger)
        {
            _dmc = dmc ?? throw new ArgumentNullException(nameof(dmc));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async override Task<ListDirectoryResponse> ListDirectory(ListDirectoryRequest request, ServerCallContext context)
        {
            var directory = DirectoryNode.FromPath(request.Path);
            var nodes = await _dmc.ListDirectory(directory) ?? new List<IStorageNode>();
            return new ListDirectoryResponse
            {
                Nodes =
                {
                    nodes.Select(NodeTranslator.Translate),
                }
            };
        }

		public override Task<MakeDirectoryResponse> MakeDirectory(MakeDirectoryRequest request, ServerCallContext context)
        {
            var directory = DirectoryNode.FromPath(request.Path);
            _dmc.
        }

        public override async Task<Protobuf.FileSystem.File.v1.OpenWriteResponse> OpenWrite(IAsyncStreamReader<Protobuf.FileSystem.File.v1.OpenWriteRequest> requestStream, ServerCallContext context)
        {
            var fileName = string.Empty;
            var expectedHash = string.Empty;
            var expectedSize = 0L;
            Stream? stream = null;

            var totalBytes = 0l;
            await foreach (var message in requestStream.ReadAllAsync())
            {
                if (message.PayloadCase == OpenWriteRequest.PayloadOneofCase.Info)
                {
                    fileName = message.Info.FileName;
                    expectedSize = message.Info.FileSize;
                    expectedHash = message.Info.Sha256;

                    var node = FileNode.FromPath(fileName);
                    stream = await _dmc.OpenWriteAsync(node);
                }
                else if (message.PayloadCase == OpenWriteRequest.PayloadOneofCase.Chunk)
                {
                    if (stream is null)
                    {
                        throw new RpcException(new Status(StatusCode.InvalidArgument, "Missing file info"));
                    }

                    await stream.WriteAsync(message.Chunk.Content.Memory);
                    totalBytes += message.Chunk.Content.Length;
                }
            }

            if (stream is null)
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "No file info received"));
            }

            await stream.FlushAsync();
            await stream.DisposeAsync();

            if (totalBytes != expectedSize)
            {
                return new OpenWriteResponse
                {
                    Success = false,
                    Message = $"File size mismatch. Expected {expectedSize} bytes, but received {totalBytes} bytes."
                };
            }

            if (String.IsNullOrEmpty(expectedHash))
            {
                // TODO: check the sha256 has
            }

            return new OpenWriteResponse
            {
                Success = true,
                Message = "File uploaded successfully.",
            };
        }

        public override async Task OpenRead(OpenReadRequest request, IServerStreamWriter<OpenReadResponse> responseStream, ServerCallContext context)
        {
            var filePath = FileNode.FromPath(request.FileName);
            if (!await _dmc.ExistsAsync(filePath))
            {
                throw new RpcException(new Status(StatusCode.NotFound, $"File '{request.FileName}' not found."));
            }

            await responseStream.WriteAsync(new OpenReadResponse
            {
                Info = new FileInfo
                {
                    FileName = request.FileName,
                    FileSize = 0, // TODO: get the actual file size
                    Sha256 = string.Empty // TODO: get the actual sha256
                }
            });

            var buffer = new byte[64 * 1024];
            var offset = 0L;

            var stream = await _dmc.OpenReadAsync(filePath);
            int bytesRead;
            while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await responseStream.WriteAsync(new OpenReadResponse
                {
                    Chunk = new FileChunk
                    {
                        Offset = offset,
                        Content = Google.Protobuf.ByteString.CopyFrom(buffer, 0, bytesRead)
                    }
                });

                offset += bytesRead;
            }
        }
    }
}
