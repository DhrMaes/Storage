namespace DhrMaes.Storage.Console.Commands.Providers
{
	using System;
	using System.CommandLine;
	
	using DhrMaes.Storage.Messages;

	internal class ListProvider
	{
		internal static Command Create(ProviderService.ProviderServiceClient client)
		{
			var command = new Command("list", "List all configured providers");
			command.AddAlias("ls");
			command.SetHandler(() =>
			{
				Console.WriteLine("This feature is not yet implemented.");
                //foreach (var provider in dmc.Providers)
                //{
                //	var type = provider.GetType();
                //	var providerType = type.GetCustomAttribute<ProviderIdentifierAttribute>()?.Id ?? "Unknown";
                //                Console.WriteLine($"[{providerType}]:\t{provider.Identifier}");
                //}
            });
			return command;
        }
    }
}
