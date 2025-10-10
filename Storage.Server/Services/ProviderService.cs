namespace DhrMaes.Storage.Server.Services
{
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;

    using DhrMaes.Storage.Core;
    using DhrMaes.Storage.Core.Providers;
    using DhrMaes.Storage.Protobuf.Configuration.Providers.v1;

    using Grpc.Core;

    public class ProviderService : DhrMaes.Storage.Protobuf.Configuration.Providers.v1.ProviderService.ProviderServiceBase
    {
        private readonly IDmc _dmc;
        private readonly ILogger<ProviderService> _logger;

        public ProviderService(
            IDmc dmc,
            ILogger<ProviderService> logger)
        {
            _dmc = dmc ?? throw new ArgumentNullException(nameof(dmc));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public override async Task<AddProviderResponse> AddProvider(AddProviderRequest request, ServerCallContext context)
        {
            var providerTypes = await _dmc.GetInstalledProviders();
            if (!providerTypes.TryGetValue(request.Provider.Name, out var providerType))
            {
                throw new RpcException(new Status(StatusCode.NotFound, $"Provider '{request.Provider.Name}' not found."));
            }

            var config = Activator.CreateInstance(providerType)! as IStorageProviderConfig;
            if (config is null)
            {
                throw new RpcException(new Status(StatusCode.Internal, $"Provider '{request.Provider.Name}' does not implement IStorageProviderConfig."));
            }

            foreach (var property in request.Provider.Properties)
            {
                var propInfo = providerType.GetProperty(property.Name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (propInfo is null ||
                    propInfo.GetCustomAttribute<StorageExcludeAttribute>() is not null)
                {
                    _logger.LogWarning("Property '{Property}' not found on provider '{Provider}'", property.Name, request.Provider.Name);
                    continue;
                }
                try
                {
                    object? value = property.GetValue();
                    if (value is not null)
                    {
                        propInfo.SetValue(config, value);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error setting property '{Property}' on provider '{Provider}'", property.Name, request.Provider.Name);
                }
            }

            var provider = await _dmc.AddProvider(config);
            return new AddProviderResponse
            {
                Identifier = provider.Identifier,
            };
        }

        public override async Task<RemoveProviderResponse> RemoveProvider(RemoveProviderRequest request, ServerCallContext context)
        {
            try
            {
                await _dmc.RemoveProvider(request.Identifier);
                return new RemoveProviderResponse();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing provider '{Provider}'", request.Identifier);
                throw new RpcException(new Status(StatusCode.Internal, $"Error removing provider '{request.Identifier}': {ex.Message}"));
            }
        }

		public override async Task<ListProvidersResponse> ListProviders(ListProvidersRequest request, ServerCallContext context)
        {
            var providers = await _dmc.ListProviders();
            return new ListProvidersResponse
            {
                Providers =
                {
                    providers.Select(p => new Provider
                    {
                        Identifier = p.Identifier,
                        Type = p.GetType().Name,
                    }),
                },
            };
        }

        public override async Task<GetInstalledProvidersResponse> GetInstalledProviders(GetInstalledProvidersRequest request, ServerCallContext context)
        {
            var providerConfigs = await _dmc.GetInstalledProviders();
            var configs = new List<ProviderConfig>();
            foreach (var config in providerConfigs)
            {
                var properties = new List<ProviderConfigProperty>();
                foreach (var prop in config.Value.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (prop is null ||
                        prop.GetCustomAttribute<StorageExcludeAttribute>() is not null)
                    {
                        continue;
                    }

                    properties.Add(new ProviderConfigProperty
                    {
                        Name = prop.Name,
                        Type = ProviderConfigProperty.GetPropertyType(prop.PropertyType),
                    });
                }

                configs.Add(new ProviderConfig
                {
                    Name = config.Key,
                    Properties =
                    {
                        properties,
                    },
                });
            }

            var response = new GetInstalledProvidersResponse
            {
                Providers =
                {
                    configs,
                },
            };

            return response;
        }
    }
}
