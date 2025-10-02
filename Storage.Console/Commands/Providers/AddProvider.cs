namespace DhrMaes.Storage.Console.Commands.Providers
{
	using System;
	using System.Collections.Generic;
	using System.CommandLine;
	using System.CommandLine.Invocation;

	using DhrMaes.Storage.Messages;

	internal class AddProvider
	{
        internal static Command Create(DhrMaes.Storage.Messages.ProviderService.ProviderServiceClient client)
        {
            var command = new Command("add", "Add a new provider");
            command.AddAlias("a");

            foreach (var providerCommand in LoadProviderCommands(client))
            {
                command.AddCommand(providerCommand);
            }

            return command;
        }

        private static IEnumerable<Command> LoadProviderCommands(Messages.ProviderService.ProviderServiceClient client)
        {
            var response = client.GetInstalledProviders(new Messages.GetInstalledProvidersRequest());
            foreach (var provider in response.Providers)
            {
                var command = new Command(provider.Name, $"Add a new {provider.Name} provider");
                var options = new Dictionary<ProviderConfigProperty, Option>();

                foreach (var property in provider.Properties)
                {
                    var optionType = typeof(Option<>).MakeGenericType(GetPropertyType(property));
                    var option = (Option)Activator.CreateInstance(optionType, $"--{property.Name.ToLower()}", $"Sets {property.Name}")!;
                    options[property] = option;
                    command.AddOption(option);
                }

                command.SetHandler(async (InvocationContext ctx) =>
                {
                    Console.WriteLine("This feature is not yet implemented.");
                    //var config = Activator.CreateInstance(kvp.Value)! as IStorageProviderConfig;

                    //foreach (var (prop, opt) in options)
                    //{
                    //    var value = ctx.ParseResult.GetValueForOption(opt);
                    //    if (value is not null)
                    //    {
                    //        prop.SetValue(config, value);
                    //    }
                    //}

                    //await dmc.AddProvider(config);
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
