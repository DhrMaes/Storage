namespace DhrMaes.Storage.Provider.GoogleDrive
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using DhrMaes.Storage.Core.FileSystem;
    using DhrMaes.Storage.Core.Providers;
    using DhrMaes.Storage.Core.Structure;
    using DhrMaes.Storage.Core.Structure.File;
    using DhrMaes.Storage.Core.Structure.Directory;

    using Google.Apis.Drive.v3;

    [ProviderIdentifier("GoogleDrive")]
    public class GoogleDriveStorageProvider : IStorageProvider
    {
        private readonly DriveService _drive;
        private readonly string _rootFolderId;
        private readonly Dictionary<string, string> _pathCache;

        public string Identifier { get; }

        public GoogleDriveStorageProvider(string identifier, DriveService drive, string? rootFolderId = null)
        {
            Identifier = identifier ?? throw new ArgumentNullException(nameof(identifier));
            _drive = drive ?? throw new ArgumentNullException(nameof(drive));
            _rootFolderId = rootFolderId ?? "root";
            _pathCache = new Dictionary<string, string>();
        }

        public async Task<bool> ExistsAsync(IBaseNode node, CancellationToken cancellationToken = default)
        {
            var fileId = await GetFileIdAsync(node, cancellationToken);
            return fileId is not null;
        }

        public async Task<Stream> OpenReadAsync(IFileNodeReference node, CancellationToken cancellationToken = default)
        {
            var fileId = await GetFileIdAsync(node, cancellationToken)
                         ?? throw new FileNotFoundException($"File not found: {node.GetFullPath()}");
            var stream = new MemoryStream();
            var request = _drive.Files.Get(fileId);
            await request.DownloadAsync(stream, cancellationToken);
            stream.Position = 0;
            return stream;
        }

        public async Task<Stream> OpenWriteAsync(IFileNodeReference node, CancellationToken cancellationToken = default)
        {
            // Ensure parent directory exists
            await CreateDirectoryAsync(node.Parent!, cancellationToken);
            var parentId = await GetParentIdAsync(node, cancellationToken) ?? _rootFolderId;

            // Check if file already exists and get its ID for update
            var existingFileId = await GetFileIdAsync(node, cancellationToken);

            return new GoogleDriveUploadStream(_drive, node, parentId, existingFileId, _pathCache);
        }

        public async Task DeleteAsync(IBaseNode node, CancellationToken cancellationToken = default)
        {
            var fileId = await GetFileIdAsync(node, cancellationToken);
            if (fileId != null)
            {
                await _drive.Files.Delete(fileId).ExecuteAsync(cancellationToken);

                // Remove from cache
                InvalidateCacheForPath(node.GetFullPath());
            }
        }

        public async Task<ICollection<IBaseNode>> ListAsync(IDirectoryNodeReference node, CancellationToken cancellationToken = default)
        {
            var basePath = node.GetFullPath();
            var folderId = await GetFileIdAsync(node, cancellationToken);
            if (folderId is null)
            {
                return Array.Empty<IBaseNode>();
            }

            var request = _drive.Files.List();
            request.Q = $"'{folderId}' in parents and trashed = false";
            request.Fields = "files(id,name,mimeType,size)";
            var result = await request.ExecuteAsync(cancellationToken);

            var nodes = new List<IBaseNode>();
            foreach (var file in result.Files)
            {
                if (file.MimeType == "application/vnd.google-apps.folder")
                {
                    nodes.Add(DirectoryNodeReference.FromPath(Path.Combine(basePath, file.Name)));
                }
                else
                {
                    nodes.Add(FileNodeReference.FromPath(Path.Combine(basePath, file.Name)));
                }
            }

            return nodes;
        }

        public async Task<IFileNode> GetFileAsync(IFileNodeReference node, CancellationToken cancellationToken = default)
        {
            var fileId = await GetFileIdAsync(node, cancellationToken);
            if (fileId is null)
            {
                throw new FileNotFoundException($"File not found: {node.GetFullPath()}");
            }

            var request = _drive.Files.Get(fileId);
            request.Fields = "id,name,size,mimeType";
            var file = await request.ExecuteAsync(cancellationToken);

            var fileNode = new FileNode(this, new FileSize(file.Size ?? 0), node);
            return fileNode;
        }

        public async Task CreateDirectoryAsync(IDirectoryNodeReference node, CancellationToken cancellationToken = default)
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

            // Get all path segments and ensure parent hierarchy exists
            var pathSegments = GetPathSegments(node);
            var currentFolderId = _rootFolderId;
            var currentPath = "/";

            // Create each missing directory in the path
            for (int i = 0; i < pathSegments.Count; i++)
            {
                var segmentName = pathSegments[i];
                currentPath = Path.Combine(currentPath, segmentName).Replace("\\", "/");

                // Check cache first
                if (_pathCache.TryGetValue(currentPath, out var cachedId))
                {
                    currentFolderId = cachedId;
                    continue;
                }

                // Check if this segment already exists
                var request = _drive.Files.List();
                request.Q = $"name = '{EscapeQueryString(segmentName)}' and '{currentFolderId}' in parents and mimeType = 'application/vnd.google-apps.folder' and trashed = false";
                request.Fields = "files(id,name)";
                var result = await request.ExecuteAsync(cancellationToken);

                var existingFolder = result.Files.FirstOrDefault();
                if (existingFolder != null)
                {
                    currentFolderId = existingFolder.Id;
                    _pathCache[currentPath] = currentFolderId;
                }
                else
                {
                    // Create the directory
                    var fileMeta = new Google.Apis.Drive.v3.Data.File
                    {
                        Name = segmentName,
                        MimeType = "application/vnd.google-apps.folder",
                        Parents = new[] { currentFolderId }
                    };
                    var createdFolder = await _drive.Files.Create(fileMeta).ExecuteAsync(cancellationToken);
                    currentFolderId = createdFolder.Id;
                    _pathCache[currentPath] = currentFolderId;
                }
            }
        }

        public async Task CopyAsync(IDirectoryNodeReference source, IDirectoryNodeReference destination, CancellationToken cancellationToken = default)
        {
            var sourceId = await GetFileIdAsync(source, cancellationToken) ?? throw new FileNotFoundException($"Source not found: {source.GetFullPath()}");

            // Ensure destination parent directory exists
            if (destination.Parent != null)
            {
                await CreateDirectoryAsync(destination.Parent, cancellationToken);
            }

            var parentId = await GetParentIdAsync(destination, cancellationToken) ?? _rootFolderId;
            var fileMeta = new Google.Apis.Drive.v3.Data.File
            {
                Name = destination.Name,
                Parents = new[] { parentId }
            };
            await _drive.Files.Copy(fileMeta, sourceId).ExecuteAsync(cancellationToken);
        }

        public async Task CopyAsync(IFileNodeReference source, IBaseNode destination, CancellationToken cancellationToken = default)
        {
            var sourceId = await GetFileIdAsync(source, cancellationToken) ?? throw new FileNotFoundException($"Source not found: {source.GetFullPath()}");

            if (destination is IDirectoryNodeReference dn)
            {
                await CreateDirectoryAsync(dn, cancellationToken);
                var parentId = await GetFileIdAsync(dn, cancellationToken) ?? _rootFolderId;

                var fileMeta = new Google.Apis.Drive.v3.Data.File
                {
                    Name = source.Name, // Keep original name when copying to directory
                    Parents = new[] { parentId }
                };
                await _drive.Files.Copy(fileMeta, sourceId).ExecuteAsync(cancellationToken);
            }
            else
            {
                // Copy and rename file
                if (destination.Parent != null)
                {
                    await CreateDirectoryAsync(destination.Parent, cancellationToken);
                }

                var parentId = await GetParentIdAsync(destination, cancellationToken) ?? _rootFolderId;
                var fileMeta = new Google.Apis.Drive.v3.Data.File
                {
                    Name = destination.Name,
                    Parents = new[] { parentId }
                };
                await _drive.Files.Copy(fileMeta, sourceId).ExecuteAsync(cancellationToken);
            }
        }

        public async Task MoveAsync(IDirectoryNodeReference source, IDirectoryNodeReference destination, CancellationToken cancellationToken = default)
        {
            var sourceId = await GetFileIdAsync(source, cancellationToken) ?? throw new FileNotFoundException($"Source not found: {source.GetFullPath()}");

            // Get the current parent of the source directory
            var sourceFile = await _drive.Files.Get(sourceId).ExecuteAsync(cancellationToken);
            var currentParents = sourceFile.Parents ?? new List<string>();

            // Ensure destination parent directory exists
            if (destination.Parent != null)
            {
                await CreateDirectoryAsync(destination.Parent, cancellationToken);
            }

            // Get destination parent ID
            var destinationParentId = await GetParentIdAsync(destination, cancellationToken) ?? _rootFolderId;

            // Update the file with new parent and name
            var updateMeta = new Google.Apis.Drive.v3.Data.File()
            {
                Name = destination.Name
            };

            var updateRequest = _drive.Files.Update(updateMeta, sourceId);
            updateRequest.AddParents = destinationParentId;
            updateRequest.RemoveParents = string.Join(",", currentParents);
            updateRequest.Fields = "id, parents";
            await updateRequest.ExecuteAsync(cancellationToken);

            // Invalidate cache for both source and destination
            InvalidateCacheForPath(source.GetFullPath());
            InvalidateCacheForPath(destination.GetFullPath());
        }

        public async Task MoveAsync(IFileNodeReference source, IBaseNode destination, CancellationToken cancellationToken = default)
        {
            var sourceId = await GetFileIdAsync(source, cancellationToken) ?? throw new FileNotFoundException($"Source not found: {source.GetFullPath()}");

            // Get the current parent of the source file
            var sourceFile = await _drive.Files.Get(sourceId).ExecuteAsync(cancellationToken);
            var currentParents = sourceFile.Parents ?? new List<string>();

            // Ensure destination parent directory exists
            if (destination is IDirectoryNodeReference dn)
            {
                await CreateDirectoryAsync(dn, cancellationToken);
                var destinationParentId = await GetFileIdAsync(dn, cancellationToken) ?? _rootFolderId;

                // Move file to directory
                var updateMeta = new Google.Apis.Drive.v3.Data.File();
                var updateRequest = _drive.Files.Update(updateMeta, sourceId);
                updateRequest.AddParents = destinationParentId;
                updateRequest.RemoveParents = string.Join(",", currentParents);
                updateRequest.Fields = "id, parents";
                await updateRequest.ExecuteAsync(cancellationToken);
            }
            else
            {
                // Move and rename file
                if (destination.Parent != null)
                {
                    await CreateDirectoryAsync(destination.Parent, cancellationToken);
                }

                var destinationParentId = await GetParentIdAsync(destination, cancellationToken) ?? _rootFolderId;

                var updateMeta = new Google.Apis.Drive.v3.Data.File()
                {
                    Name = destination.Name
                };

                var updateRequest = _drive.Files.Update(updateMeta, sourceId);
                updateRequest.AddParents = destinationParentId;
                updateRequest.RemoveParents = string.Join(",", currentParents);
                updateRequest.Fields = "id, parents";
                await updateRequest.ExecuteAsync(cancellationToken);
            }

            // Invalidate cache for both source and destination
            InvalidateCacheForPath(source.GetFullPath());
            InvalidateCacheForPath(destination.GetFullPath());
        }

        private async Task<string?> GetFileIdAsync(IBaseNode node, CancellationToken ct)
        {
            // Handle root directory case
            if (node.Parent == null)
            {
                return _rootFolderId;
            }

            var fullPath = node.GetFullPath();

            // Check cache first
            if (_pathCache.TryGetValue(fullPath, out var cachedId))
            {
                return cachedId;
            }

            // Build the path from root to the target node
            var pathSegments = GetPathSegments(node);

            // Start from root and traverse the path
            var currentFolderId = _rootFolderId;
            var currentPath = "/";

            foreach (var segment in pathSegments)
            {
                currentPath = Path.Combine(currentPath, segment).Replace("\\", "/");

                // Check if we have this intermediate path cached
                if (_pathCache.TryGetValue(currentPath, out var intermediateCachedId))
                {
                    currentFolderId = intermediateCachedId;
                    continue;
                }

                var request = _drive.Files.List();
                request.Q = $"name = '{EscapeQueryString(segment)}' and '{currentFolderId}' in parents and trashed = false";
                request.Fields = "files(id,name,mimeType)";
                var result = await request.ExecuteAsync(ct);

                var file = result.Files.FirstOrDefault();
                if (file == null)
                {
                    return null; // Path doesn't exist
                }

                currentFolderId = file.Id;

                // Cache the intermediate path
                _pathCache[currentPath] = currentFolderId;
            }

            return currentFolderId;
        }

        private async Task<string?> GetParentIdAsync(IBaseNode node, CancellationToken ct)
        {
            var parent = node.Parent;
            if (parent == null)
            {
                return _rootFolderId;
            }

            return await GetFileIdAsync(parent, ct);
        }

        private static List<string> GetPathSegments(IBaseNode node)
        {
            var segments = new List<string>();
            var current = node;

            while (current?.Parent != null)
            {
                segments.Insert(0, current.Name);
                current = current.Parent;
            }

            return segments;
        }

        private static string EscapeQueryString(string input)
        {
            // Escape single quotes and backslashes for Google Drive API queries
            return input.Replace("\\", "\\\\").Replace("'", "\\'");
        }

        private void InvalidateCacheForPath(string path)
        {
            // Remove the specific path and any child paths from cache
            var keysToRemove = _pathCache.Keys
                .Where(key => key == path || key.StartsWith(path + "/"))
                .ToList();

            foreach (var key in keysToRemove)
            {
                _pathCache.Remove(key);
            }
        }

        /// <summary>
        /// Clears the entire path cache. Use this if the Google Drive structure changes externally.
        /// </summary>
        public void ClearCache()
        {
            _pathCache.Clear();
        }

        private sealed class GoogleDriveUploadStream : MemoryStream
        {
            private readonly DriveService _drive;
            private readonly IFileNodeReference _node;
            private readonly string _parentFolderId;
            private readonly string? _existingFileId;
            private readonly Dictionary<string, string> _pathCache;
            private bool _uploaded;
            private bool _disposed;

            public GoogleDriveUploadStream(DriveService drive, IFileNodeReference node, string parentFolderId, string? existingFileId, Dictionary<string, string> pathCache)
            {
                _drive = drive;
                _node = node;
                _parentFolderId = parentFolderId;
                _existingFileId = existingFileId;
                _pathCache = pathCache;
                _uploaded = false;
                _disposed = false;
            }

            public override async Task FlushAsync(CancellationToken cancellationToken)
            {
                await base.FlushAsync(cancellationToken);
                
                if (!_uploaded && !_disposed)
                {
                    await UploadAsync(cancellationToken);
                }
            }

            protected override void Dispose(bool disposing)
            {
                if (!_disposed && disposing)
                {
                    if (!_uploaded)
                    {
                        // Synchronously upload when disposing
                        // Note: This is not ideal but necessary for the dispose pattern
                        Task.Run(async () => await UploadAsync(CancellationToken.None)).Wait();
                    }
                    _disposed = true;
                }
                
                base.Dispose(disposing);
            }

            public override async ValueTask DisposeAsync()
            {
                if (!_disposed)
                {
                    if (!_uploaded)
                    {
                        await UploadAsync(CancellationToken.None);
                    }
                    _disposed = true;
                }
                
                await base.DisposeAsync();
            }

            private async Task UploadAsync(CancellationToken cancellation = default)
            {
                if (_uploaded || _disposed)
                {
                    return;
                }

                try
                {
                    Position = 0;

                    var fileMeta = new Google.Apis.Drive.v3.Data.File()
                    {
                        Name = _node.Name
                    };

                    Google.Apis.Drive.v3.Data.File? uploadedFile = null;

                    if (_existingFileId != null)
                    {
                        // Update existing file
                        var request = _drive.Files.Update(
                            fileMeta,
                            _existingFileId,
                            this,
                            "application/octet-stream");

                        var result = await request.UploadAsync(cancellation);
                        if (result.Status != Google.Apis.Upload.UploadStatus.Completed)
                        {
                            throw new InvalidOperationException($"Failed to upload file to Google Drive. Status: {result.Status}");
                        }

                        uploadedFile = request.ResponseBody;
                    }
                    else
                    {
                        // Create new file
                        fileMeta.Parents = new List<string> { _parentFolderId };

                        var request = _drive.Files.Create(
                            fileMeta,
                            this,
                            "application/octet-stream");

                        var result = await request.UploadAsync(cancellation);
                        if (result.Status != Google.Apis.Upload.UploadStatus.Completed)
                        {
                            throw new InvalidOperationException($"Failed to upload file to Google Drive. Status: {result.Status}");
                        }

                        uploadedFile = request.ResponseBody;
                    }

                    // Update cache with the file ID if upload was successful
                    if (uploadedFile?.Id != null)
                    {
                        var fullPath = _node.GetFullPath();
                        _pathCache[fullPath] = uploadedFile.Id;
                    }

                    _uploaded = true;
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Failed to upload file '{_node.Name}' to Google Drive", ex);
                }
            }
        }
    }
}
