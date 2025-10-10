namespace DhrMaes.Storage.Console.Commands.Providers
{
	using System.CommandLine;

	using Grpc.Net.Client;

	using DhrMaes.Storage.Protobuf.Configuration.Providers.v1;

    internal class ProviderCommand
	{
		internal static Command Create(GrpcChannel channel)
		{
			var client = new ProviderService.ProviderServiceClient(channel);
            var command = new Command("provider", "Manage providers");
			command.AddCommand(AddProvider.Create(client));
			command.AddCommand(RemoveProvider.Create(client));
			command.AddCommand(ListProvider.Create(client));
			return command;
		}
	}
}
