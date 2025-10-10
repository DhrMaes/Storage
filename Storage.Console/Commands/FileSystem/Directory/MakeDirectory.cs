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
                await client.RemoveDirectoryAsync(new Protobuf.FileSystem.Directory.v1.RemoveDirectoryRequest
                {
                    Path = path,
                });
            }, PathArg);
            return command;
        }
    }
}
