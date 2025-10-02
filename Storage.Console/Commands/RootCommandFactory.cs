namespace DhrMaes.Storage.Console.Commands
{
	using System.CommandLine;

	using Grpc.Net.Client;

	internal class RootCommandFactory
	{
		internal static RootCommand Create(GrpcChannel channel)
		{
			var rootCommand = new RootCommand("DhrMaes.Storage.Console - A command line interface for DhrMaes.Storage");
			rootCommand.AddCommand(Providers.ProviderCommand.Create(channel));
			rootCommand.AddCommand(FileSystem.FileSystemCommand.Create(channel));
			return rootCommand;
		}
	}
}
