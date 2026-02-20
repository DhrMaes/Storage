namespace DhrMaes.Storage.Console.Commands.Providers
{
	using System;
	using System.Collections.Generic;
	using System.CommandLine;
	using System.CommandLine.Invocation;

	using DhrMaes.Storage.Protobuf.Configuration.Providers.v1;

	internal class AddProvider
	{
        internal static Command Create(ProviderService.ProviderServiceClient client)
        {
            var command = new Command("add", "Add a new provider");
            command.AddAlias("a");

            foreach (var providerCommand in LoadProviderCommands(client))
            {
                command.AddCommand(providerCommand);
            }

            return command;
        }

        private static IEnumerable<Command> LoadProviderCommands(ProviderService.ProviderServiceClient client)
        {
            var response = client.GetPlugins(new GetPluginsRequest());
            foreach (var provider in response.Plugins)
            {
                var command = new Command(provider.Name, $"Add a new {provider.Name} provider");
                var options = new Dictionary<ProviderConfigProperty, Option>();

                foreach (var property in provider.Properties)
                {
                    var optionType = typeof(Option<>).MakeGenericType(GetPropertyType(property));
                    var option = (Option)Activator.CreateInstance(optionType, $"--{property.Name.ToLower()}", property.Description)!;
                    options[property] = option;
                    command.AddOption(option);
                }

                command.SetHandler(async (InvocationContext ctx) =>
                {
                    var request = new ProviderConfig
                    {
                        Name = provider.Name,
                    };

                    foreach (var (prop, opt) in options)
                    {
                        var value = ctx.ParseResult.GetValueForOption(opt);
                        if (value is not null)
                        {
                            var propConfig = new ProviderConfigProperty
                            {
                                Name = prop.Name,
                                Type = prop.Type,
                            };

                            propConfig.SetValue(value);
                            request.Properties.Add(propConfig);
                        }
                    }

                    var response = await client.AddProviderAsync(new AddProviderRequest
                    {
                        Provider = request,
                    });

                    Console.WriteLine($"Provider '{response.Identifier}' of type '{provider.Name}' added.");
                });

                yield return command;
            }
        }

        private static Type GetPropertyType(ProviderConfigProperty? property)
        {
            if (property == null)
                throw new ArgumentNullException(nameof(property));

            return property.Type switch
            {
                ProviderConfigPropertyType.String => typeof(string),
                ProviderConfigPropertyType.Int => typeof(int),
                ProviderConfigPropertyType.Bool => typeof(bool),
                _ => throw new NotSupportedException($"Property type {property.Type} is not supported."),
            };
        }
    }
}
