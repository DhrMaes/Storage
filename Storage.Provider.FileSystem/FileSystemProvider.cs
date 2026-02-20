namespace DhrMaes.Storage.Provider.FileSystem
{
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;

    using DhrMaes.Storage.Core.Providers;
    using DhrMaes.Storage.Core.Structure;
    using DhrMaes.Storage.Core.Structure.File;
    using DhrMaes.Storage.Core.Structure.Directory;
	using DhrMaes.Storage.Core.FileSystem;

	[ProviderIdentifier("FileSystem")]
    public class FileSystemProvider : IStorageProvider
    {
        private readonly string _path;

        public FileSystemProvider(string identifier, string path)
        {
            Identifier = identifier;
            _path = path.TrimEnd(Path.DirectorySeparatorChar);
        }

        public string Identifier { get; }

        public Task<bool> ExistsAsync(IBaseNode node, CancellationToken cancellationToken = default)
        {
            if (node is IFileNodeReference)
            {
                var fullPath = ToProviderPath(node);
                return Task.FromResult(File.Exists(fullPath));
            }

            if (node is IDirectoryNodeReference)
            {
                var fullPath = ToProviderPath(node);
                return Task.FromResult(Directory.Exists(fullPath));
            }

            return Task.FromResult(false);
        }

        public Task<Stream> OpenReadAsync(IFileNodeReference node, CancellationToken cancellationToken = default)
        {
            var fullPath = ToProviderPath(node);
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

        public Task<Stream> OpenWriteAsync(IFileNodeReference node, CancellationToken cancellationToken = default)
        {
            var fullPath = ToProviderPath(node);
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

        public Task DeleteAsync(IBaseNode node, CancellationToken cancellationToken = default)
        {
            var fullPath = ToProviderPath(node);
            if (node is IFileNodeReference)
            {
                if (!File.Exists(fullPath))
                {
                    return Task.CompletedTask;
                }

                File.Delete(fullPath);
                return Task.CompletedTask;
            }

            if (node is IDirectoryNodeReference)
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

        public Task<ICollection<IBaseNode>> ListAsync(IDirectoryNodeReference node, CancellationToken cancellationToken = default)
        {
            var fullPath = ToProviderPath(node);
            if (!Directory.Exists(fullPath))
            {
                return Task.FromResult((ICollection<IBaseNode>)new List<IBaseNode>());
            }

            var items = new List<IBaseNode>();
            items.AddRange(Directory
                .GetDirectories(fullPath)
                .Select(d => (IBaseNode)DirectoryNodeReference.FromPath(ToCorePath(d))));

            items.AddRange(Directory
                .GetFiles(fullPath)
                .Select(f => (IBaseNode)FileNodeReference.FromPath(ToCorePath(f))));

            return Task.FromResult((ICollection<IBaseNode>)items);
        }
        
        public Task<IFileNode> GetFileAsync(IFileNodeReference node, CancellationToken cancellationToken = default)
        {
            var fullPath = ToProviderPath(node);
            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException("File not found.", fullPath);
            }

            var fileInfo = new FileInfo(fullPath);
            var fileNode = new FileNode(this, new FileSize(fileInfo.Length), node);

            return Task.FromResult<IFileNode>(fileNode);
        }

        public Task CreateDirectoryAsync(IDirectoryNodeReference node, CancellationToken cancellationToken = default)
        {
            var fullPath = ToProviderPath(node);
            if (Directory.Exists(fullPath))
            {
                return Task.CompletedTask;
            }

            Directory.CreateDirectory(fullPath);
            return Task.CompletedTask;
        }

        public Task CopyAsync(IDirectoryNodeReference source, IDirectoryNodeReference destination, CancellationToken cancellationToken = default)
        {
            var sourcePath = ToProviderPath(source);
            var destinationPath = ToProviderPath(destination);
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

        public Task CopyAsync(IFileNodeReference source, IBaseNode destination, CancellationToken cancellationToken = default)
        {
            if (destination is IFileNodeReference)
            {
                var sourcePath = ToProviderPath(source);
                var destinationPath = ToProviderPath(destination);;
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

            if (destination is IDirectoryNodeReference)
            {
                var sourcePath = ToProviderPath(source);
                var destinationPath = Path.Combine(ToProviderPath(destination), source.Name);
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

            throw new NotSupportedException("Copying between the specified node types is not supported.");
        }

        public Task MoveAsync(IDirectoryNodeReference source, IDirectoryNodeReference destination, CancellationToken cancellationToken = default)
        {
            var sourcePath = ToProviderPath(source);
            var destinationPath = ToProviderPath(destination);
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

        public Task MoveAsync(IFileNodeReference source, IBaseNode destination, CancellationToken cancellationToken = default)
        {
            if (destination is IFileNodeReference)
            {
                var sourcePath = ToProviderPath(source);
                var destinationPath = ToProviderPath(destination);
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

            if (destination is IDirectoryNodeReference)
            {
                var sourcePath = ToProviderPath(source);
                var destinationPath = Path.Combine(ToProviderPath(destination), source.Name);
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

            throw new NotSupportedException("Moving between the specified node types is not supported.");
        }

        private string ToProviderPath(IBaseNode node)
        {
            return Path.Combine(_path, node.GetFullPath().TrimStart(Path.DirectorySeparatorChar));
        }

        private string ToCorePath(string providerPath)
        {
            return providerPath.Substring(_path.Length);
        }
    }
}
