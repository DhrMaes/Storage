namespace DhrMaes.Storage.Console.Commands.FileSystem.File
{
    using System.CommandLine;
    using System.IO;

    using DhrMaes.Storage.Protobuf.FileSystem.v1;

    internal class UploadFile
    {
        internal static Argument<string> PathArg = new Argument<string>(
                name: "localPath",
                description: "The full path of the file to upload.");

        internal static Argument<string> UploadPathArg = new Argument<string>(
                name: "remotePath",
                description: "The destination path where the file will be uploaded to.");

        internal static Option<string> ProviderArg = new Option<string>(
            aliases: [ "--provider", "-p" ],
            description: "The identifier of the provider to upload to.")
        {
            IsRequired = false,
        };

        internal static Command Create(StorageService.StorageServiceClient client)
        {
            var command = new Command("upload", "Upload a new file");
            command.AddArgument(PathArg);
            command.AddArgument(UploadPathArg);
            command.AddOption(ProviderArg);
            command.SetHandler(async (localPath, remotePath, providerId) =>
            {
                await HandleUploadFile(client, localPath, remotePath, providerId);
            }, PathArg, UploadPathArg, ProviderArg);
            return command;
        }

        private static async Task<int> HandleUploadFile(StorageService.StorageServiceClient client, string localPath, string remotePath, string providerId)
        {
            try
            {
                string targetPath = remotePath;
                if (!Path.HasExtension(remotePath))
                {
                    // Combine directory with original file name
                    string fileName = Path.GetFileName(localPath);
                    targetPath = $"{remotePath}/{fileName}";
                }

                using var call = client.OpenWrite();
                var fileInfo = new FileInfo(localPath);
                var sha256 = System.Security.Cryptography.SHA256.HashData(System.IO.File.ReadAllBytes(localPath));

                // Send metadata
                var request = new Protobuf.FileSystem.File.v1.OpenWriteRequest
                {
                    Info = new Protobuf.FileSystem.File.v1.FileInfo
                    {
                        FileName = targetPath,
                        FileSize = fileInfo.Length,
                        Sha256 = System.Convert.ToBase64String(sha256),
                    }
                };

                if (!String.IsNullOrEmpty(providerId))
                {
                    request.ProviderIdentifier = providerId;
                }

                await call.RequestStream.WriteAsync(request);

                var buffer = new byte[64 * 1024];
                var offset = 0l;

                await using var stream = System.IO.File.OpenRead(localPath);
                int bytesRead;
                while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await call.RequestStream.WriteAsync(new Protobuf.FileSystem.File.v1.OpenWriteRequest
                    {
                        Chunk = new Protobuf.FileSystem.File.v1.FileChunk
                        {
                            Offset = offset,
                            Content = Google.Protobuf.ByteString.CopyFrom(buffer, 0, bytesRead)
                        }
                    });

                    offset += bytesRead;

                    if (fileInfo.Length > 0)
                    {
                        var percentage = (offset / (double)fileInfo.Length) * 100;
                        System.Console.Write($"\rProgress: {percentage:F1}% ({offset}/{fileInfo.Length} bytes)");
                    }
                    else
                    {
                        System.Console.Write($"\rReceived {offset} bytes");
                    }
                }

                await call.RequestStream.CompleteAsync();

                var response = await call.ResponseAsync;
                System.Console.WriteLine();
                System.Console.WriteLine($"Server: {response.Message} (saved: {response.SavedPath})");
                return 0;
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Error uploading file: {ex.Message}");
                return 1;
            }
        }
    }
}
