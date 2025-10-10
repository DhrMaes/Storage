namespace DhrMaes.Storage.Console.Commands.FileSystem
{
	using System.CommandLine;

	using Grpc.Net.Client;

	using DhrMaes.Storage.Protobuf.FileSystem.v1;

    internal class FileSystemCommand
	{
		internal static Command Create(GrpcChannel channel)
		{
			var client= new StorageService.StorageServiceClient(channel);
			var command = new Command("fs", "File system operations");

			// Directory commands
            command.AddCommand(Directory.MakeDirectory.Create(client));
			command.AddCommand(Directory.RemoveDirectory.Create(client));
			command.AddCommand(Directory.ListDirectory.Create(client));
			
			// File commands
			command.AddCommand(File.UploadFile.Create(client));
			command.AddCommand(File.DeleteFile.Create(client));
            return command;
		}
	}
}
