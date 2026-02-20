namespace DhrMaes.Storage.Console.Commands.FileSystem.Directory
{
    using System.CommandLine;

    using DhrMaes.Storage.Protobuf.FileSystem.v1;

    internal class MakeDirectory
    {
        internal static Argument<string> PathArg = new Argument<string>(
                name: "path",
                description: "The full path of the directory to create");

        internal static Command Create(StorageService.StorageServiceClient client)
        {
            var command = new Command("mkdir", "Make a new directory");
            command.AddArgument(PathArg);
            command.SetHandler(async (path) =>
            {
                try
                {
                    await client.MakeDirectoryAsync(new Protobuf.FileSystem.Directory.v1.MakeDirectoryRequest
                    {
                        Path = path,
                    });

                    System.Console.WriteLine($"Directory created: {path}");
                }
                catch
                {
                    System.Console.WriteLine($"Failed to create directory: {path}");
                }
            }, PathArg);
            return command;
        }
    }
}
