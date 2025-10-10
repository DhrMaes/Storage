namespace DhrMaes.Storage.Console.Commands.Providers
{
	using System;
	using System.CommandLine;
	
	using DhrMaes.Storage.Protobuf.Configuration.Providers.v1;

	internal class ListProvider
	{
		internal static Command Create(ProviderService.ProviderServiceClient client)
		{
			var command = new Command("list", "List all configured providers");
			command.AddAlias("ls");
			command.SetHandler(async () =>
			{
				var response = await client.ListProvidersAsync(new ListProvidersRequest());
                foreach (var provider in response.Providers)
				{
					Console.WriteLine($"[{provider.Type}]:\t{provider.Identifier}");
				}
			});
			return command;
        }
    }
}
