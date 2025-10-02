namespace DhrMaes.Storage.Console.Commands.FileSystem.Directory
{
    using System.CommandLine;

    internal class ListDirectory
    {
        internal static Argument<string> PathArg = new Argument<string>(
                "path",
                () => String.Empty,
                description: "The full path of the directory to list");

        internal static Command Create(Messages.StorageService.StorageServiceClient client)
        {
            var command = new Command("list", "list the contents of a directory");
            command.AddAlias("ls");
            command.AddArgument(PathArg);
            command.SetHandler(async (path) =>
            {
                System.Console.WriteLine("This feature is not yet implemented.");
                //var nodes = await dmc.ListDirectory(path);
                //foreach(var node in nodes.OrderBy(n => n.GetType().Name))
                //{
                //	if (node is DirectoryNode dir)
                //	{
                //		System.Console.WriteLine($"[DIR]  {dir.Name}");
                //	}
                //	else if (node is FileNode file)
                //	{
                //		System.Console.WriteLine($"[FILE] {file.Name} ({file.Size} bytes)");
                //  }
                //}

            }, PathArg);
            return command;
        }
    }
}
