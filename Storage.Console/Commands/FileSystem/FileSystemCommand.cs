namespace DhrMaes.Storage.Console.Commands.FileSystem
{
	using System.CommandLine;

	using DhrMaes.Storage.Core;

	internal class FileSystemCommand
	{
		internal static Command Create(Dmc dmc)
		{
			var command = new Command("fs", "File system operations");
			command.AddCommand(Directory.MakeDirectory.Create(dmc));
			command.AddCommand(Directory.RemoveDirectory.Create(dmc));
			command.AddCommand(Directory.ListDirectory.Create(dmc));
			return command;
		}
	}
}
