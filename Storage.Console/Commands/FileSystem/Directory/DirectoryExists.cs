namespace DhrMaes.Storage.Console.Commands.FileSystem.Directory
{
    using System.CommandLine;

    using DhrMaes.Storage.Protobuf.FileSystem.v1;

    internal class DirectoryExists
    {
        internal static Argument<string> PathArg = new Argument<string>(
                name: "path",
                description: "The full path of the directory or file to check");

        internal static Command Create(StorageService.StorageServiceClient client)
        {
            var command = new Command("dir-exists", "Check if a directory exists");
            command.AddArgument(PathArg);
            command.SetHandler(async (path) =>
            {
                var response = await client.DirectoryExistsAsync(new Protobuf.FileSystem.Directory.v1.DirectoryExistsRequest
                {
                    Path = path,
                });

                if (response.Exists)
                {
                    System.Console.WriteLine($"Directory '{path}' exists");
                }
                else
                {
                    System.Console.WriteLine($"Directory '{path}' does not exists");
                }

            }, PathArg);
            return command;
        }
    }
}
