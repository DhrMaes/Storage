namespace DhrMaes.Storage.Provider.GoogleDrive
{
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;

	using DhrMaes.Storage.Core.FileSystem;
	using DhrMaes.Storage.Core.Providers;
    using DhrMaes.Storage.Core.Structure.Nodes;

    using Google.Apis.Drive.v3;

    [ProviderIdentifier("GoogleDrive")]
    public class GoogleDriveStorageProvider : IStorageProvider
    {
        private readonly DriveService _drive;
        private readonly string _rootFolderId;

        public string Identifier { get; }

        public GoogleDriveStorageProvider(string identifier, DriveService drive, string? rootFolderId = null)
        {
            Identifier = identifier ?? throw new ArgumentNullException(nameof(identifier));
            _drive = drive ?? throw new ArgumentNullException(nameof(drive));
            _rootFolderId = rootFolderId ?? "root";
        }

        public async Task<bool> ExistsAsync(IStorageNode node, CancellationToken cancellationToken = default)
        {
            var fileId = await GetFileIdAsync(node, cancellationToken);
            return fileId is not null;
        }

        public async Task<Stream> OpenReadAsync(FileNode node, CancellationToken cancellationToken = default)
        {
            var fileId = await GetFileIdAsync(node, cancellationToken)
                         ?? throw new FileNotFoundException($"File not found: {node.GetFullPath()}");
            var stream = new MemoryStream();
            var request = _drive.Files.Get(fileId);
            await request.DownloadAsync(stream, cancellationToken);
            stream.Position = 0;
            return stream;
        }

        public async Task<Stream> OpenWriteAsync(FileNode node, CancellationToken cancellationToken = default)
        {
            // For Google Drive we usually upload a new file instead of returning a writable stream.
            // This method returns a MemoryStream that you can write to, then upload using a custom wrapper.
            await CreateDirectoryAsync(node.Parent!, cancellationToken);
            var parentId = await GetParentIdAsync(node, cancellationToken) ?? _rootFolderId;
            return new GoogleDriveUploadStream(_drive, node, parentId);
        }

        public async Task DeleteAsync(IStorageNode node, CancellationToken cancellationToken = default)
        {
            var fileId = await GetFileIdAsync(node, cancellationToken);
            if (fileId != null)
            {
                await _drive.Files.Delete(fileId).ExecuteAsync(cancellationToken);
            }
        }

        public async Task<ICollection<IStorageNode>> ListAsync(DirectoryNode node, CancellationToken cancellationToken = default)
        {
            var folderId = await GetFileIdAsync(node, cancellationToken) ?? _rootFolderId;
            var request = _drive.Files.List();
            request.Q = $"'{folderId}' in parents and trashed = false";
            request.Fields = "files(id,name,mimeType,size)";
            var result = await request.ExecuteAsync(cancellationToken);

            var nodes = new List<IStorageNode>();
            foreach (var file in result.Files)
            {
                if (file.MimeType == "application/vnd.google-apps.folder")
                {
                    nodes.Add(new DirectoryNode(file.Name));
                }
                else
                {
                    nodes.Add(new FileNode(file.Name)
                    {
                        Size = file.Size ?? FileSize.Unknown,
                    });
                }
            }
            return nodes;
        }

        public async Task CreateDirectoryAsync(DirectoryNode node, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            // Check if directory already exists
            var nodeId = await GetFileIdAsync(node, cancellationToken);
            if (nodeId is not null)
            {
                return;
            }

            // Ensure parent directory exists
            var parentId = await GetParentIdAsync(node, cancellationToken) ?? _rootFolderId;
            if (parentId is null)
            {
                await CreateDirectoryAsync(node.Parent!, cancellationToken);
            }

            // Create the directory
            var fileMeta = new Google.Apis.Drive.v3.Data.File
            {
                Name = node.Name,
                MimeType = "application/vnd.google-apps.folder",
                Parents = new[] { parentId }
            };
            await _drive.Files.Create(fileMeta).ExecuteAsync(cancellationToken);
        }

        public async Task CopyAsync(IStorageNode source, IStorageNode destination, CancellationToken cancellationToken = default)
        {
            var sourceId = await GetFileIdAsync(source, cancellationToken) ?? throw new FileNotFoundException($"Source not found: {source.GetFullPath()}");

            if (destination is DirectoryNode dn)
            {
                await CreateDirectoryAsync(dn);
            }
            else
            {
                await CreateDirectoryAsync(destination.Parent!);
            }

            var parentId = await GetParentIdAsync(destination, cancellationToken) ?? _rootFolderId;
            var fileMeta = new Google.Apis.Drive.v3.Data.File
            {
                Name = destination.Name,
                Parents = new[] { parentId }
            };
            await _drive.Files.Copy(fileMeta, sourceId).ExecuteAsync(cancellationToken);
        }

        public async Task MoveAsync(IStorageNode source, IStorageNode destination, CancellationToken cancellationToken = default)
        {
            var sourceId = await GetFileIdAsync(source, cancellationToken) ?? throw new FileNotFoundException($"Source not found: {source.GetFullPath()}");
            var sourceParentId = await GetParentIdAsync(source, cancellationToken) ?? _rootFolderId;
            if (destination is DirectoryNode dn)
            {
                await CreateDirectoryAsync(dn);
            }
            else
            {
                await CreateDirectoryAsync(destination.Parent!);
            }


            // First retrieve current parents
            var destinationParentId = await GetParentIdAsync(destination, cancellationToken) ?? _rootFolderId;
            var get = await _drive.Files.Get(sourceId).ExecuteAsync(cancellationToken);

            var updateRequest = _drive.Files.Update(new Google.Apis.Drive.v3.Data.File(), sourceId);
            updateRequest.AddParents = destinationParentId;
            updateRequest.RemoveParents = sourceParentId;
            updateRequest.Fields = "id, parents";
            await updateRequest.ExecuteAsync(cancellationToken);
        }

        private async Task<string?> GetFileIdAsync(IStorageNode node, CancellationToken ct)
        {
            // Simplified path search. You may need a more robust path-to-ID resolver.
            var request = _drive.Files.List();
            request.Q = $"name = '{node.Name}' and trashed = false";
            request.Fields = "files(id,name)";
            var result = await request.ExecuteAsync(ct);
            return result.Files.FirstOrDefault()?.Id;
        }

        private async Task<string?> GetParentIdAsync(IStorageNode node, CancellationToken ct)
        {
            // For nested directories you'd recursively resolve parents
            var dir = node.Parent;
            return dir == null ? _rootFolderId : await GetFileIdAsync(dir, ct);
        }

        private sealed class GoogleDriveUploadStream : MemoryStream
        {
            private readonly DriveService _drive;
            private readonly FileNode _node;
            private readonly string _parentFolderId;

            public GoogleDriveUploadStream(DriveService drive, FileNode node, string parentFolderId)
            {
                _drive = drive;
                _node = node;
                _parentFolderId = parentFolderId;
            }

            public async Task UploadAsync(CancellationToken cancellation = default)
            {
                Position = 0;

                var fileMeta = new Google.Apis.Drive.v3.Data.File()
                {
                    Name = _node.Name,
                    Parents = new List<string> { _parentFolderId }
                };

                var request = _drive.Files.Create(
                    fileMeta,
                    this,
                    "application/octet-stream");

                await request.UploadAsync(cancellation);
            }
        }
    }
}
