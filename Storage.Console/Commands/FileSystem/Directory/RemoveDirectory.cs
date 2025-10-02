namespace DhrMaes.Storage.Console.Commands.FileSystem.Directory
{
	using System.CommandLine;

	internal class RemoveDirectory
	{
        internal static Argument<string> PathArg = new Argument<string>(
                name: "path",
                description: "The full path of the directory to remove");

        internal static Command Create(Messages.StorageService.StorageServiceClient client)
		{
			var command = new Command("rmdir", "Remove a directory");
			command.AddArgument(PathArg);
			command.SetHandler((path) =>
			{
				System.Console.WriteLine("This feature is not yet implemented.");
                //dmc.RemoveDirectory(path);
            }, PathArg);
            return command;
		}
	}
}
