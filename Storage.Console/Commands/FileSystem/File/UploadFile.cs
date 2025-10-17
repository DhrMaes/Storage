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
                using var call = client.OpenWrite();
                var fileInfo = new FileInfo(path);
                var sha256 = System.Security.Cryptography.SHA256.HashData(System.IO.File.ReadAllBytes(path));

                // Send metadata
                await call.RequestStream.WriteAsync(new Protobuf.FileSystem.File.v1.OpenWriteRequest
                {
                    Info = new Protobuf.FileSystem.File.v1.FileInfo
                    {
                        FileName = uploadPath,
                        FileSize = fileInfo.Length,
                        Sha256 = System.Convert.ToBase64String(sha256),
                    }
                });

                var buffer = new byte[64 * 1024];
                var offset = 0l;

                await using var stream = System.IO.File.OpenRead(path);
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
                }

                await call.RequestStream.CompleteAsync();

                var response = await call.ResponseAsync;
                System.Console.WriteLine($"Server: {response.Message} (saved: {response.SavedPath})");

            }, PathArg, UploadPathArg);
            return command;
        }
    }
}
