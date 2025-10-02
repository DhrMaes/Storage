namespace DhrMaes.Storage.Console.Commands.Providers
{
	using System.CommandLine;

	using Grpc.Net.Client;

	internal class ProviderCommand
	{
		internal static Command Create(GrpcChannel channel)
		{
			var client = new Messages.ProviderService.ProviderServiceClient(channel);
            var command = new Command("provider", "Manage providers");
			command.AddCommand(AddProvider.Create(client));
			command.AddCommand(RemoveProvider.Create(client));
			command.AddCommand(ListProvider.Create(client));
			return command;
		}
	}
}
