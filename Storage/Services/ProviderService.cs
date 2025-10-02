namespace DhrMaes.Storage.Server.Services
{
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;

    using DhrMaes.Storage.Core;
    using DhrMaes.Storage.Core.Providers;
    using DhrMaes.Storage.Messages;

    using Grpc.Core;

    public class ProviderService : DhrMaes.Storage.Messages.ProviderService.ProviderServiceBase
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

        public override async Task<GetInstalledProvidersResponse> GetInstalledProviders(GetInstalledProvidersRequest request, ServerCallContext context)
        {
            var providerConfigs = await _dmc.GetInstalledProviders();
            var configs = new List<ProviderConfig>();
            foreach (var config in providerConfigs)
            {
                var properties = new List<ProviderConfigProperty>();
                foreach(var prop in config.Value.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (IsSupportedProperty(prop))
                    {
                        continue;
                    }

                    properties.Add(new ProviderConfigProperty
                    {
                        Name = prop.Name,
                        Type = GetPropertyType(prop.PropertyType),
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

        private static ProviderConfigPropertyType GetPropertyType(Type type)
        {
            if (type == typeof(string))
            {
                return ProviderConfigPropertyType.String;
            }
            if (type == typeof(int) || type == typeof(long) || type == typeof(short) || type == typeof(byte))
            {
                return ProviderConfigPropertyType.Int;
            }
            if (type == typeof(bool))
            {
                return ProviderConfigPropertyType.Bool;
            }
            if (type == typeof(decimal) || type == typeof(float) || type == typeof(double))
            {
                return ProviderConfigPropertyType.Double;
            }
            if (type.IsEnum)
            {
                return ProviderConfigPropertyType.Enum;
            }

            return ProviderConfigPropertyType.PropertyTypeUnspecified;
        }

        private static bool IsSupportedProperty(PropertyInfo? info)
        {
            if (info is null)
            {
                return false;
            }

            if (info.GetCustomAttribute<StorageExcludeAttribute>() is not null)
            {
                return false;
            }

            if (info.PropertyType.IsPrimitive || info.PropertyType == typeof(string) || info.PropertyType == typeof(decimal))
            {
                return true;
            }

            if (info.PropertyType.IsEnum)
            {
                return true;
            }

            return false;
        }
    }
}
