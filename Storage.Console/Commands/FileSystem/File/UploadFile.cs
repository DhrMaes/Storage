namespace DhrMaes.Storage.Console.Commands.FileSystem.File
{
    using System.CommandLine;

    using DhrMaes.Storage.Protobuf.FileSystem.v1;

    internal class UploadFile
    {
        internal static Argument<string> PathArg = new Argument<string>(
                name: "path",
                description: "The full path of the file to upload");

        internal static Argument<string> UploadPathArg = new Argument<string>(
                name: "uploadPath",
                description: "The destination path where the file will be uploaded to");

        internal static Command Create(StorageService.StorageServiceClient client)
        {
            var command = new Command("upload", "Upload a new file");
            command.AddArgument(PathArg);
            command.AddArgument(UploadPathArg);
            command.SetHandler(async (path, uploadPath) =>
            {
                System.Console.WriteLine("This feature is not yet implemented.");
                //if(!System.IO.File.Exists(path))
                //{
                //    System.Console.WriteLine($"File '{path}' does not exist.");
                //    return;
                //}

                //var fileStream = System.IO.File.OpenRead(path);
                //await dmc.UploadFile(uploadPath, fileStream);
            }, PathArg, UploadPathArg);
            return command;
        }
    }
}
