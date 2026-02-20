namespace DhrMaes.Storage.Console.Commands.FileSystem.File
{
    using System;
    using System.CommandLine;
    using System.IO;

    using DhrMaes.Storage.Protobuf.FileSystem.v1;

    using Grpc.Core;

    internal class DownloadFile
    {
        internal static Argument<string> PathArg = new Argument<string>(
                name: "remotePath",
                description: "The full path of the file to download");

        internal static Argument<string> DownloadPathArg = new Argument<string>(
                name: "localPath",
                description: "The destination path where the file will be downloaded to");

        internal static Command Create(StorageService.StorageServiceClient client)
        {
            var command = new Command("download", "Download a file");
            command.AddArgument(PathArg);
            command.AddArgument(DownloadPathArg);
            command.SetHandler(async (path, downloadPath) =>
            {
                await HandleDownloadFile(client, path, downloadPath);
            }, PathArg, DownloadPathArg);
            return command;
        }

        private static async Task<int> HandleDownloadFile(StorageService.StorageServiceClient client, string remotePath, string localPath)
        {
            var tempPath = Path.GetRandomFileName();

            try
            {
                bool isDirectory =
                    Directory.Exists(localPath) ||
                    (!Path.HasExtension(localPath) && !File.Exists(localPath));

                string targetPath = localPath;
                if (isDirectory)
                {
                    // Combine directory with original file name
                    string fileName = Path.GetFileName(remotePath);
                    targetPath = Path.Combine(localPath, fileName);
                }

                using var call = client.OpenRead(new Protobuf.FileSystem.File.v1.OpenReadRequest
                {
                    FileName = remotePath
                });

                var expectedHash = string.Empty;
                var expectedSize = 0L;
                var receivedBytes = 0L;
                System.Console.WriteLine($"Downloading to temporary file {tempPath}");

                using (var fs = System.IO.File.Create(tempPath))
                {
                    await foreach (var message in call.ResponseStream.ReadAllAsync())
                    {
                        if (message.PayloadCase == Protobuf.FileSystem.File.v1.OpenReadResponse.PayloadOneofCase.Info)
                        {
                            expectedSize = message.Info.FileSize;
                            expectedHash = message.Info.Sha256;
                            System.Console.WriteLine($"Downloading {System.IO.Path.GetFileName(remotePath)} ({expectedSize} bytes) from {remotePath}");
                        }
                        else if (message.PayloadCase == Protobuf.FileSystem.File.v1.OpenReadResponse.PayloadOneofCase.Chunk)
                        {
                            await fs.WriteAsync(message.Chunk.Content.Memory);
                            receivedBytes += message.Chunk.Content.Length;

                            if (expectedSize > 0)
                            {
                                var percentage = (receivedBytes / (double)expectedSize) * 100;
                                System.Console.Write($"\rProgress: {percentage:F1}% ({receivedBytes}/{expectedSize} bytes)");
                            }
                            else
                            {
                                System.Console.Write($"\rReceived {receivedBytes} bytes");
                            }
                        }
                    }

                    await fs.FlushAsync();

                    System.Console.WriteLine();
                    System.Console.WriteLine($"Downloaded {receivedBytes} bytes.");
                }


                if (!String.IsNullOrEmpty(expectedHash))
                {
                    // TODO: implement the hash check
                }

                System.Console.WriteLine($"Moving temporary file to target location {targetPath}");

                System.IO.File.Move(tempPath, targetPath, true);
                System.Console.WriteLine($"File saved to {localPath}");
                return 0;
            }
            catch (Exception ex)
            {
                System.Console.WriteLine(ex.Message);
                return 1;
            }
            finally
            {
                if (System.IO.File.Exists(tempPath))
                {
                    System.IO.File.Delete(tempPath);
                }
            }
        }
    }
}
