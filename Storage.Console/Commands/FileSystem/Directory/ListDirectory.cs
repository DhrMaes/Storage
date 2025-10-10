namespace DhrMaes.Storage.Console.Commands.FileSystem.Directory
{
    using System.CommandLine;

    using DhrMaes.Storage.Protobuf.FileSystem.v1;

    internal class ListDirectory
    {
        internal static Argument<string> PathArg = new Argument<string>(
                "path",
                () => String.Empty,
                description: "The full path of the directory to list");

        internal static Command Create(StorageService.StorageServiceClient client)
        {
            var command = new Command("list", "list the contents of a directory");
            command.AddAlias("ls");
            command.AddArgument(PathArg);
            command.SetHandler(async (path) =>
            {
                var response = await client.ListDirectoryAsync(new Protobuf.FileSystem.Directory.v1.ListDirectoryRequest
                {
                    Path = path,
                });

                foreach (var node in response.Nodes.OrderByDescending(n => n.NodeTypeCase))
                {
                    if (node.NodeTypeCase == Protobuf.Structure.v1.StorageNode.NodeTypeOneofCase.Directory)
                    {
                        System.Console.WriteLine($"[DIR]  {node.Directory.Name}");
                    }
                    else if (node.NodeTypeCase == Protobuf.Structure.v1.StorageNode.NodeTypeOneofCase.File)
                    {
                        System.Console.WriteLine($"[FILE] {node.File.Name} ({node.File.Size} bytes)");
                    }
                }

            }, PathArg);
            return command;
        }
    }
}
