namespace DhrMaes.Storage.Console.Commands.Providers
{
	using System.CommandLine;

	using DhrMaes.Storage.Core;

	internal class ProviderCommand
	{
		internal static Command Create(Dmc dmc)
		{
			var command = new Command("provider", "Manage providers");
			command.AddCommand(AddProvider.Create(dmc));
			command.AddCommand(RemoveProvider.Create(dmc));
			command.AddCommand(ListProvider.Create(dmc));
			return command;
		}
	}
}
