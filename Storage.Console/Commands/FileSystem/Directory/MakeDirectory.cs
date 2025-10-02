namespace DhrMaes.Storage.Console.Commands.FileSystem.Directory
{
	using System.CommandLine;

	internal class MakeDirectory
	{
		internal static Argument<string> PathArg = new Argument<string>(
				name: "path",
				description: "The full path of the directory to create");

		internal static Command Create(Messages.StorageService.StorageServiceClient client)
		{
			var command = new Command("mkdir", "Make a new directory");
			command.AddArgument(PathArg);
			command.SetHandler((path) =>
			{
				System.Console.WriteLine("This feature is not yet implemented.");
                //dmc.CreateDirectory(path);
            }, PathArg);
			return command;
		}
	}
}
