namespace DhrMaes.Storage.Console.Commands.FileSystem.Directory
{
	using System.CommandLine;

	using DhrMaes.Storage.Core;

	internal class RemoveDirectory
	{
        internal static Argument<string> PathArg = new Argument<string>(
                name: "path",
                description: "The full path of the directory to remove");

        internal static Command Create(Dmc dmc)
		{
			var command = new Command("rmdir", "Remove a directory");
			command.AddArgument(PathArg);
			command.SetHandler((path) =>
			{
				dmc.RemoveDirectory(path);
            }, PathArg);
            return command;
		}
	}
}
