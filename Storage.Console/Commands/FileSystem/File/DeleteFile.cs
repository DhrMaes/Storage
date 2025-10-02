namespace DhrMaes.Storage.Console.Commands.FileSystem.File
{
	using System.CommandLine;

	internal class DeleteFile
	{
        internal static Argument<string> PathArg = new Argument<string>(
                name: "path",
                description: "The full path of the file to remove");

        internal static Command Create(Messages.StorageService.StorageServiceClient clinent)
        {
            var command = new Command("rm", "Remove a file");
            command.AddArgument(PathArg);
            command.SetHandler(async (path) =>
            {
                System.Console.WriteLine("This feature is not yet implemented.");
                //await dmc.RemoveFile(path);
            }, PathArg);
            return command;
        }
    }
}
