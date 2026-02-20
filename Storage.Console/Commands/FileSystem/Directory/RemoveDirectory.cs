namespace DhrMaes.Storage.Console.Commands.FileSystem.Directory
{
	using System.CommandLine;

	using DhrMaes.Storage.Protobuf.FileSystem.v1;

    internal class RemoveDirectory
	{
        internal static Argument<string> PathArg = new Argument<string>(
                name: "path",
                description: "The full path of the directory to remove");

        internal static Command Create(StorageService.StorageServiceClient client)
		{
			var command = new Command("rmdir", "Remove a directory");
			command.AddArgument(PathArg);
			command.SetHandler(async (path) =>
			{
				try
				{
                    await client.RemoveDirectoryAsync(new Protobuf.FileSystem.Directory.v1.RemoveDirectoryRequest
                    {
                        Path = path,
                    });

                    System.Console.WriteLine($"Directory '{path}' removed successfully.");
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine($"Error removing directory '{path}': {ex.Message}");
                }
            }, PathArg);
            return command;
		}
	}
}
