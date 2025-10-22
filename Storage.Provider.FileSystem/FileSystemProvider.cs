namespace DhrMaes.Storage.Provider.FileSystem
{
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;

    using DhrMaes.Storage.Core.Providers;
	using DhrMaes.Storage.Core.Structure;
	using DhrMaes.Storage.Core.Structure.File;
	using DhrMaes.Storage.Core.Structure.Nodes;

    [ProviderIdentifier("FileSystem")]
    public class FileSystemProvider : IStorageProvider
    {
        private readonly string _path;

        public FileSystemProvider(string identifier, string path)
        {
            Identifier = identifier;
            _path = path;
        }

        public string Identifier { get; } = String.Empty;

        public Task<bool> ExistsAsync(IStorageNode node, CancellationToken cancellationToken = default)
        {
            if (node is FileNode)
            {
                var fullPath = Path.Combine(_path, node.GetFullPath());
                return Task.FromResult(File.Exists(fullPath));
            }

            if (node is DirectoryNode)
            {
                var fullPath = Path.Combine(_path, node.GetFullPath());
                return Task.FromResult(Directory.Exists(fullPath));
            }

            return Task.FromResult(false);
        }

        public Task<Stream> OpenReadAsync(FileNode node, CancellationToken cancellationToken = default)
        {
            var fullPath = Path.Combine(_path, node.GetFullPath());
            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException("File not found.", fullPath);
            }

            // Open the file for reading asynchronously
            var stream = new FileStream(
                fullPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 4096,
                useAsync: true
            );
            return Task.FromResult<Stream>(stream);
        }

        public Task<Stream> OpenWriteAsync(FileNode node, CancellationToken cancellationToken = default)
        {
            var fullPath = Path.Combine(_path, node.GetFullPath());
            var directory = Path.GetDirectoryName(fullPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory!);
            }

            // Open the file for writing asynchronously, overwrite if exists
            var stream = new FileStream(
                fullPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 4096,
                useAsync: true
            );
            return Task.FromResult<Stream>(stream);
        }

        public Task DeleteAsync(IStorageNode node, CancellationToken cancellationToken = default)
        {
            var fullPath = Path.Combine(_path, node.GetFullPath());
            if (node is FileNode)
            {
                if (!File.Exists(fullPath))
                {
                    return Task.CompletedTask;
                }

                File.Delete(fullPath);
                return Task.CompletedTask;
            }

            if (node is DirectoryNode)
            {
                if (!Directory.Exists(fullPath))
                {
                    return Task.CompletedTask;
                }

                Directory.Delete(fullPath, true);
                return Task.CompletedTask;
            }

            return Task.CompletedTask;
        }

        public Task<ICollection<IStorageNode>> ListAsync(DirectoryNode node, CancellationToken cancellationToken = default)
        {
            var fullPath = Path.Combine(_path, node.GetFullPath());
            if (!Directory.Exists(fullPath))
            {
                return Task.FromResult((ICollection<IStorageNode>)new List<IStorageNode>());
            }

            var items = new List<IStorageNode>();
            items.AddRange(Directory
                .GetDirectories(fullPath)
                .Select(d => new DirectoryNode(Path.GetFileNameWithoutExtension(d))
                {
                    Parent = node,
                }));

            items.AddRange(Directory
                .GetFiles(fullPath)
                .Select(f => new FileInfo(f))
                .Select(f => new FileNode(f.Name)
                {
                    Size = f.Length,
                    Parent = node,
                }));

            return Task.FromResult((ICollection<IStorageNode>)items);
        }

        public Task CreateDirectoryAsync(DirectoryNode node, CancellationToken cancellationToken = default)
        {
            var fullPath = Path.Combine(_path, node.GetFullPath());
            if (Directory.Exists(fullPath))
            {
                return Task.CompletedTask;
            }

            Directory.CreateDirectory(fullPath);
            return Task.CompletedTask;
        }

        public Task CopyAsync(IStorageNode source, IStorageNode destination, CancellationToken cancellationToken = default)
        {
            if (source is FileNode && destination is FileNode)
            {
                var sourcePath = Path.Combine(_path, source.GetFullPath());
                var destinationPath = Path.Combine(_path, destination.GetFullPath());
                if (!File.Exists(sourcePath))
                {
                    throw new FileNotFoundException("Source file not found.", sourcePath);
                }

                var destinationDir = Path.GetDirectoryName(destinationPath);
                if (!Directory.Exists(destinationDir))
                {
                    Directory.CreateDirectory(destinationDir!);
                }

                File.Copy(sourcePath, destinationPath, true);
                return Task.CompletedTask;
            }

            if (source is FileNode fn && destination is DirectoryNode)
            {
                var sourcePath = Path.Combine(_path, source.GetFullPath());
                var destinationPath = Path.Combine(_path, destination.GetFullPath(), fn.Name);
                if (!File.Exists(sourcePath))
                {
                    throw new FileNotFoundException("Source file not found.", sourcePath);
                }

                var destinationDir = Path.GetDirectoryName(destinationPath);
                if (!Directory.Exists(destinationDir))
                {
                    Directory.CreateDirectory(destinationDir!);
                }

                File.Copy(sourcePath, destinationPath, true);
                return Task.CompletedTask;
            }

            if (source is DirectoryNode && destination is DirectoryNode)
            {
                var sourcePath = Path.Combine(_path, source.GetFullPath());
                var destinationPath = Path.Combine(_path, destination.GetFullPath());
                if (!Directory.Exists(sourcePath))
                {
                    throw new DirectoryNotFoundException($"Source directory not found: {sourcePath}");
                }

                // Create all of the directories
                foreach (var dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
                {
                    var newDirPath = dirPath.Replace(sourcePath, destinationPath);
                    if (!Directory.Exists(newDirPath))
                    {
                        Directory.CreateDirectory(newDirPath);
                    }
                }

                // Copy all the files & replace any files with the same name
                foreach (var newPath in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
                {
                    var newFilePath = newPath.Replace(sourcePath, destinationPath);
                    var newFileDir = Path.GetDirectoryName(newFilePath);
                    if (!Directory.Exists(newFileDir))
                    {
                        Directory.CreateDirectory(newFileDir!);
                    }

                    File.Copy(newPath, newFilePath, true);
                }
                return Task.CompletedTask;
            }

            throw new NotSupportedException("Copying between the specified node types is not supported.");
        }

        public Task MoveAsync(IStorageNode source, IStorageNode destination, CancellationToken cancellationToken = default)
        {
            if (source is FileNode && destination is FileNode)
            {
                var sourcePath = Path.Combine(_path, source.GetFullPath());
                var destinationPath = Path.Combine(_path, destination.GetFullPath());
                if (!File.Exists(sourcePath))
                {
                    throw new FileNotFoundException("Source file not found.", sourcePath);
                }

                var destinationDir = Path.GetDirectoryName(destinationPath);
                if (!Directory.Exists(destinationDir))
                {
                    Directory.CreateDirectory(destinationDir!);
                }

                File.Move(sourcePath, destinationPath, true);
                return Task.CompletedTask;
            }

            if (source is FileNode fn && destination is DirectoryNode)
            {
                var sourcePath = Path.Combine(_path, source.GetFullPath());
                var destinationPath = Path.Combine(_path, destination.GetFullPath(), fn.Name);
                if (!File.Exists(sourcePath))
                {
                    throw new FileNotFoundException("Source file not found.", sourcePath);
                }

                var destinationDir = Path.GetDirectoryName(destinationPath);
                if (!Directory.Exists(destinationDir))
                {
                    Directory.CreateDirectory(destinationDir!);
                }

                File.Move(sourcePath, destinationPath, true);
                return Task.CompletedTask;
            }

            if (source is DirectoryNode && destination is DirectoryNode)
            {
                var sourcePath = Path.Combine(_path, source.GetFullPath());
                var destinationPath = Path.Combine(_path, destination.GetFullPath());
                if (!Directory.Exists(sourcePath))
                {
                    throw new DirectoryNotFoundException($"Source directory not found: {sourcePath}");
                }

                // Ensure destination directory exists
                if (!Directory.Exists(destinationPath))
                {
                    Directory.CreateDirectory(destinationPath);
                }

                // Move all directories
                foreach (var dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
                {
                    var newDirPath = dirPath.Replace(sourcePath, destinationPath);
                    if (!Directory.Exists(newDirPath))
                    {
                        Directory.CreateDirectory(newDirPath);
                    }
                }

                // Move all files
                foreach (var filePath in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
                {
                    var newFilePath = filePath.Replace(sourcePath, destinationPath);
                    var newFileDir = Path.GetDirectoryName(newFilePath);
                    if (!Directory.Exists(newFileDir))
                    {
                        Directory.CreateDirectory(newFileDir!);
                    }

                    File.Move(filePath, newFilePath, true);
                }

                // Delete the source directory after moving
                Directory.Delete(sourcePath, true);
                return Task.CompletedTask;
            }

            throw new NotSupportedException("Moving between the specified node types is not supported.");
        }
    }
}
