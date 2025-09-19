namespace DhrMaes.Storage.Console.Commands.FileSystem.Directory
{
	using System.CommandLine;

	using DhrMaes.Storage.Core;

	internal class MakeDirectory
	{
		internal static Argument<string> PathArg = new Argument<string>(
				name: "path",
				description: "The full path of the directory to create");

		internal static Command Create(Dmc dmc)
		{
			var command = new Command("mkdir", "Make a new directory");
			command.AddArgument(PathArg);
			command.SetHandler((path) =>
			{
				dmc.CreateDirectory(path);
			}, PathArg);
			return command;
		}
	}
}
