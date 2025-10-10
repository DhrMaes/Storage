namespace DhrMaes.Storage.Server.Services
{
    using System.Threading.Tasks;

    using DhrMaes.Storage.Protobuf.FileSystem.Directory.v1;

    using DhrMaes.Storage.Core;
	using DhrMaes.Storage.Core.Structure.Nodes;

	using Grpc.Core;

	using DhrMaes.Storage.Server.Nodes;

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
    }
}
