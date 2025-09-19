namespace DhrMaes.Storage.Console.Commands
{
	using System.CommandLine;

	using DhrMaes.Storage.Core;

	internal class RootCommandFactory
	{
		internal static RootCommand Create(Dmc dmc)
		{
			var rootCommand = new RootCommand("DhrMaes.Storage.Console - A command line interface for DhrMaes.Storage");
			rootCommand.AddCommand(Providers.ProviderCommand.Create(dmc));
			rootCommand.AddCommand(FileSystem.FileSystemCommand.Create(dmc));
			return rootCommand;
		}
	}
}
