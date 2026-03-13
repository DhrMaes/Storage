# Storage Orchestrator Usage Examples

This document demonstrates how end users interact with the Storage Cluster through the high-level `StorageOrchestrator` API.

---

## Basic Setup

```csharp
using Storage.Core;
using Storage.Plugin.Loader;

// 1. Initialize plugin system
var pluginManager = new PluginManager(@"C:\Plugins");
pluginManager.LoadPlugins();

// 2. Create orchestrator
var orchestrator = new StorageOrchestrator(pluginManager);

// 3. Configure a storage provider
orchestrator.ConfigureProvider(new StorageProviderConfiguration
{
    Name = "MyLocalStorage",
    PluginId = "LocalFileSystem",
    Settings = new Dictionary<string, string>
    {
        ["RootPath"] = @"C:\MyStorageCluster"
    },
    IsDefault = true
});

// 4. Initialize connections
await orchestrator.InitializeAsync();

// 5. Use it!
await orchestrator.WriteAsync("/documents/readme.txt", contentStream);
```

---

## Simple File Operations

### Write a File
```csharp
// From stream
await using var fileStream = File.OpenRead(@"C:\local\document.pdf");
await orchestrator.WriteAsync("/documents/document.pdf", fileStream);

// From bytes
var bytes = Encoding.UTF8.GetBytes("Hello, Storage Cluster!");
await using var memoryStream = new MemoryStream(bytes);
await orchestrator.WriteAsync("/notes/hello.txt", memoryStream);
```

### Read a File
```csharp
// Get stream
await using var stream = await orchestrator.ReadAsync("/documents/document.pdf");

// Save to local file
await using var outputFile = File.Create(@"C:\local\downloaded.pdf");
await stream.CopyToAsync(outputFile);

// Or read to memory
using var ms = new MemoryStream();
await stream.CopyToAsync(ms);
var bytes = ms.ToArray();
```

### Check if File Exists
```csharp
bool exists = await orchestrator.ExistsAsync("/documents/document.pdf");
if (exists)
{
    Console.WriteLine("File exists!");
}
```

### Get File Info
```csharp
var info = await orchestrator.GetInfoAsync("/documents/document.pdf");
if (info is not null)
{
    Console.WriteLine($"Name: {info.Path}");
    Console.WriteLine($"Type: {info.ItemType}");
    Console.WriteLine($"Size: {info.Size} bytes");
    Console.WriteLine($"Modified: {info.LastModified}");
}
```

### Delete a File
```csharp
await orchestrator.DeleteAsync("/documents/old-file.pdf");
```

### Move/Rename a File
```csharp
// Rename
await orchestrator.MoveAsync("/documents/old-name.pdf", "/documents/new-name.pdf");

// Move to different directory
await orchestrator.MoveAsync("/documents/file.pdf", "/archives/2024/file.pdf");
```

---

## Directory Operations

### List Directory Contents
```csharp
var items = await orchestrator.ListAsync("/documents");

foreach (var item in items)
{
    var typeIcon = item.ItemType == StorageItemType.Directory ? "📁" : "📄";
    var size = item.Size.HasValue ? $"{item.Size} bytes" : "N/A";
    
    Console.WriteLine($"{typeIcon} {item.Path} ({size})");
}
```

### List Root
```csharp
var rootItems = await orchestrator.ListAsync("/");
Console.WriteLine($"Found {rootItems.Count} items in root");
```

### Recursive Directory Listing
```csharp
async Task<List<StorageItem>> ListRecursiveAsync(string path)
{
    var result = new List<StorageItem>();
    var items = await orchestrator.ListAsync(path);
    
    foreach (var item in items)
    {
        result.Add(item);
        
        if (item.ItemType == StorageItemType.Directory)
        {
            var children = await ListRecursiveAsync(item.Path);
            result.AddRange(children);
        }
    }
    
    return result;
}

var allFiles = await ListRecursiveAsync("/");
Console.WriteLine($"Total files in cluster: {allFiles.Count(f => f.ItemType == StorageItemType.File)}");
```

---

## Multiple Provider Configuration

```csharp
var orchestrator = new StorageOrchestrator(pluginManager);

// Provider 1: Local fast storage
orchestrator.ConfigureProvider(new StorageProviderConfiguration
{
    Name = "FastLocal",
    PluginId = "LocalFileSystem",
    Settings = new Dictionary<string, string>
    {
        ["RootPath"] = @"D:\FastSSD"
    },
    IsDefault = true
});

// Provider 2: Cloud backup (future - when you add cloud plugins)
orchestrator.ConfigureProvider(new StorageProviderConfiguration
{
    Name = "CloudBackup",
    PluginId = "S3Storage", // Future plugin
    Settings = new Dictionary<string, string>
    {
        ["Bucket"] = "my-backup-bucket",
        ["Region"] = "us-east-1"
    },
    IsDefault = false
});

await orchestrator.InitializeAsync();

// Currently uses default provider (FastLocal)
await orchestrator.WriteAsync("/data/file.txt", stream);

// Future: Specify provider explicitly (TODO: implement provider-specific operations)
```

---

## Complete Example: Photo Upload Application

```csharp
using Storage.Core;
using Storage.Plugin.Loader;

public class PhotoUploadService
{
    private readonly StorageOrchestrator _storage;

    public PhotoUploadService(StorageOrchestrator storage)
    {
        _storage = storage;
    }

    public async Task<string> UploadPhotoAsync(Stream photoStream, string fileName)
    {
        // Generate organized path
        var date = DateTime.UtcNow;
        var path = $"/photos/{date:yyyy}/{date:MM}/{fileName}";

        // Check if already exists
        if (await _storage.ExistsAsync(path))
        {
            throw new InvalidOperationException($"Photo already exists: {path}");
        }

        // Upload
        await _storage.WriteAsync(path, photoStream);

        return path;
    }

    public async Task<Stream> DownloadPhotoAsync(string path)
    {
        if (!await _storage.ExistsAsync(path))
        {
            throw new FileNotFoundException($"Photo not found: {path}");
        }

        return await _storage.ReadAsync(path);
    }

    public async Task<List<StorageItem>> GetPhotosByMonthAsync(int year, int month)
    {
        var path = $"/photos/{year:D4}/{month:D2}";
        
        if (!await _storage.ExistsAsync(path))
        {
            return new List<StorageItem>();
        }

        var items = await _storage.ListAsync(path);
        return items.Where(i => i.ItemType == StorageItemType.File).ToList();
    }

    public async Task DeleteOldPhotosAsync(int daysOld)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-daysOld);
        var allPhotos = await ListAllPhotosAsync();

        foreach (var photo in allPhotos)
        {
            if (photo.LastModified < cutoffDate)
            {
                await _storage.DeleteAsync(photo.Path);
                Console.WriteLine($"Deleted old photo: {photo.Path}");
            }
        }
    }

    private async Task<List<StorageItem>> ListAllPhotosAsync()
    {
        // Simplified: In real app, walk the directory tree
        var items = await _storage.ListAsync("/photos");
        return items.Where(i => i.ItemType == StorageItemType.File).ToList();
    }
}

// Usage
var pluginManager = new PluginManager(@"C:\Plugins");
pluginManager.LoadPlugins();

await using var orchestrator = new StorageOrchestrator(pluginManager);
orchestrator.ConfigureProvider(new StorageProviderConfiguration
{
    Name = "PhotoStorage",
    PluginId = "LocalFileSystem",
    Settings = new Dictionary<string, string> { ["RootPath"] = @"D:\Photos" },
    IsDefault = true
});

await orchestrator.InitializeAsync();

var photoService = new PhotoUploadService(orchestrator);

// Upload photo
await using var photoFile = File.OpenRead("vacation.jpg");
var path = await photoService.UploadPhotoAsync(photoFile, "vacation.jpg");
Console.WriteLine($"Uploaded to: {path}");

// List photos from December 2024
var decemberPhotos = await photoService.GetPhotosByMonthAsync(2024, 12);
Console.WriteLine($"Found {decemberPhotos.Count} photos from December");

// Delete old photos
await photoService.DeleteOldPhotosAsync(365); // Delete photos older than 1 year
```

---

## Benefits of This Architecture

✅ **Simple API** - Users don't need to know about plugins or connections  
✅ **Abstraction** - Hide complexity of plugin management  
✅ **Flexible** - Easy to add new providers without changing user code  
✅ **Future-proof** - Ready for advanced features (replication, striping, etc.)

---

## Future Enhancements (Phase 4+)

- Path-based routing (e.g., `/cloud/*` → S3, `/local/*` → FileSystem)
- Multiple provider strategies (replication, striping, mirroring)
- Caching layers
- Automatic failover
- Provider health monitoring
- Async background operations (sync, backup, etc.)
