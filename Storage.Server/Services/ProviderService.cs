namespace DhrMaes.Storage.Server.Services
{
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;

    using DhrMaes.Storage.Core;
	using DhrMaes.Storage.Core.Plugins;
	using DhrMaes.Storage.Core.Providers;
    using DhrMaes.Storage.Protobuf.Configuration.Providers.v1;

    using Grpc.Core;

    public class ProviderService : DhrMaes.Storage.Protobuf.Configuration.Providers.v1.ProviderService.ProviderServiceBase
    {
        private readonly IStorage _storage;
        private readonly ILogger<ProviderService> _logger;

        public ProviderService(
            IStorage storage,
            ILogger<ProviderService> logger)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async override Task<GetPluginsResponse> GetPlugins(GetPluginsRequest request, ServerCallContext context)
        {
            var plugins = _storage.GetPlugins();
            var response = new GetPluginsResponse();

            ProviderConfigProperty Translate(PluginProperty property)
            {
                return new ProviderConfigProperty
                {
                    Name = property.Name,
                    Description = property.Description,
                    Type = property.PropertyType switch
                    {
                        PluginProperty.Type.String => ProviderConfigPropertyType.String,
                        PluginProperty.Type.Integer => ProviderConfigPropertyType.Int,
                        PluginProperty.Type.Boolean => ProviderConfigPropertyType.Bool,
                        PluginProperty.Type.Double => ProviderConfigPropertyType.Double,
                        _ => ProviderConfigPropertyType.Unspecified,
                    },
                };
            }

            foreach (var plugin in plugins)
            {
                response.Plugins.Add(new ProviderConfig
                {
                    Name = plugin.Name,
                    Properties =
                    {
                        plugin.Properties.Values.Select(Translate),
                    }
                });
            }

            return response;
        }

		public async override Task<AddProviderResponse> AddProvider(AddProviderRequest request, ServerCallContext context)
        {
            var plugin = default(IStoragePlugin);

            try
            {
                plugin = _storage.GetPlugin(request.Provider.Name);
            }
            catch(Exception)
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, $"No plugin found with name '{request.Provider.Name}'."));
            }

            var properties = new Dictionary<PluginProperty, object>();
            foreach(var property in request.Provider.Properties)
            {
                var pluginProperty = plugin.Properties.Values.FirstOrDefault(p => p.Name == property.Name);
                if(String.IsNullOrEmpty(pluginProperty.Name))
                {
                    throw new RpcException(new Status(StatusCode.InvalidArgument, $"Plugin '{plugin.Name}' does not have a property named '{property.Name}'."));
                }

                properties.Add(pluginProperty, property.GetValue());
            }

            var identifier = Guid.CreateVersion7().ToString();
			var config = await plugin.CreateConfigFromProperties(identifier.ToString(), properties);
            await _storage.AddProviderAsync(config);
            return new AddProviderResponse
            {
                Identifier = identifier,
            };
		}
    }
}
