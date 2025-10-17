namespace DhrMaes.Storage.Console.Commands.FileSystem.File
{
    using System.CommandLine;

    using DhrMaes.Storage.Protobuf.FileSystem.v1;

    using Grpc.Core;

    internal class DownloadFile
    {
        internal static Argument<string> PathArg = new Argument<string>(
                name: "path",
                description: "The full path of the file to upload");

        internal static Argument<string> DownloadPathArg = new Argument<string>(
                name: "uploadPath",
                description: "The destination path where the file will be uploaded to");

        internal static Command Create(StorageService.StorageServiceClient client)
        {
            var command = new Command("download", "Download a file");
            command.AddArgument(PathArg);
            command.AddArgument(DownloadPathArg);
            command.SetHandler(async (path, downloadPath) =>
            {
                using var call = client.OpenRead(new Protobuf.FileSystem.File.v1.OpenReadRequest
                {
                    FileName = path
                });

                var expectedHash = string.Empty;
                var expectedSize = 0L;
                var receivedBytes = 0L;
                var tempPath = Path.GetTempFileName();

                await using var fs = System.IO.File.Create(tempPath);
                await foreach (var message in call.ResponseStream.ReadAllAsync())
                {
                    if (message.PayloadCase == Protobuf.FileSystem.File.v1.OpenReadResponse.PayloadOneofCase.Info)
                    {
                        expectedSize = message.Info.FileSize;
                        expectedHash = message.Info.Sha256;
                        System.Console.WriteLine($"Downloading {System.IO.Path.GetFileName(path)} ({expectedSize} bytes) from {path}");
                    }
                    else if (message.PayloadCase == Protobuf.FileSystem.File.v1.OpenReadResponse.PayloadOneofCase.Chunk)
                    {
                        await fs.WriteAsync(message.Chunk.Content.Memory);
                        receivedBytes += message.Chunk.Content.Length;
                    }
                }

                await fs.FlushAsync();

                System.Console.WriteLine($"Downloaded {receivedBytes} bytes.");

                if (!String.IsNullOrEmpty(expectedHash))
                {
                    // TODO: implement the hash check
                }

                System.IO.File.Move(tempPath, downloadPath, true);
                System.Console.WriteLine($"File saved to {downloadPath}");
            }, PathArg, DownloadPathArg);
            return command;
        }
    }
}
