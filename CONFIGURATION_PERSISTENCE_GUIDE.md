# Configuration Persistence - Usage Guide

This guide shows how to save and load storage cluster configurations.

---

## Quick Start

### Load Configuration from File

```csharp
using Storage.Core;

// Simple one-liner - loads config and creates ready-to-use orchestrator
await using var storage = await StorageConfigurationManager.CreateFromConfigAsync(
    "storage-config.json");

// Use it immediately
await storage.WriteAsync("/documents/file.txt", contentStream);
var items = await storage.ListAsync("/");
```

---

## Configuration File Format

### Basic Configuration

```json
{
  "pluginDirectory": "C:\\Plugins",
  "defaultProvider": "MyLocalStorage",
  "providers": [
    {
      "name": "MyLocalStorage",
      "pluginId": "LocalFileSystem",
      "settings": {
        "RootPath": "C:\\StorageCluster\\Primary"
      },
      "isDefault": true
    }
  ]
}
```

### Multiple Providers

```json
{
  "pluginDirectory": "C:\\Plugins",
  "defaultProvider": "FastLocal",
  "providers": [
    {
      "name": "FastLocal",
      "pluginId": "LocalFileSystem",
      "settings": {
        "RootPath": "D:\\FastSSD"
      },
      "mountPath": "/local",
      "isDefault": true
    },
    {
      "name": "BackupStorage",
      "pluginId": "LocalFileSystem",
      "settings": {
        "RootPath": "E:\\Backup"
      },
      "mountPath": "/backup",
      "isDefault": false
    },
    {
      "name": "CloudStorage",
      "pluginId": "S3Storage",
      "settings": {
        "Bucket": "my-storage-bucket",
        "Region": "us-east-1",
        "AccessKey": "${AWS_ACCESS_KEY}",
        "SecretKey": "${AWS_SECRET_KEY}"
      },
      "mountPath": "/cloud",
      "isDefault": false
    }
  ]
}
```

---

## Environment Variables Support

Use `${VAR_NAME}` syntax for sensitive values:

```json
{
  "providers": [
    {
      "name": "CloudStorage",
      "pluginId": "S3Storage",
      "settings": {
        "AccessKey": "${AWS_ACCESS_KEY}",
        "SecretKey": "${AWS_SECRET_KEY}"
      }
    }
  ]
}
```

Set environment variables before running:
```powershell
$env:AWS_ACCESS_KEY = "your-access-key"
$env:AWS_SECRET_KEY = "your-secret-key"
```

---

## Loading Configuration

### Method 1: Direct Load and Create

```csharp
// Loads config, initializes plugins, creates orchestrator, initializes connections
await using var storage = await StorageConfigurationManager.CreateFromConfigAsync(
    "storage-config.json");

// Ready to use!
await storage.WriteAsync("/file.txt", stream);
```

### Method 2: Load Config First, Then Create

```csharp
// Load configuration
var config = await StorageConfigurationManager.LoadAsync("storage-config.json");

// Inspect or modify config if needed
Console.WriteLine($"Plugin directory: {config.PluginDirectory}");
Console.WriteLine($"Providers: {config.Providers.Count}");

// Create orchestrator from config
await using var storage = await StorageConfigurationManager.CreateFromConfigAsync(config);
```

### Method 3: Manual Setup (most control)

```csharp
// Load config
var config = await StorageConfigurationManager.LoadAsync("storage-config.json");

// Manually create plugin manager
var pluginManager = new PluginManager(config.PluginDirectory);
pluginManager.LoadPlugins();

// Create orchestrator
var orchestrator = new StorageOrchestrator(pluginManager);

// Manually configure each provider (can modify here)
foreach (var provider in config.Providers)
{
    // Could add custom logic here
    if (provider.Name == "CloudStorage" && !IsCloudAvailable())
    {
        Console.WriteLine("Skipping cloud provider (not available)");
        continue;
    }
    
    orchestrator.ConfigureProvider(provider);
}

await orchestrator.InitializeAsync();
```

---

## Saving Configuration

### Save Current Orchestrator Configuration

```csharp
// Setup orchestrator programmatically
var pluginManager = new PluginManager(@"C:\Plugins");
pluginManager.LoadPlugins();

var orchestrator = new StorageOrchestrator(pluginManager);

orchestrator.ConfigureProvider(new StorageProviderConfiguration
{
    Name = "MyStorage",
    PluginId = "LocalFileSystem",
    Settings = new Dictionary<string, string>
    {
        ["RootPath"] = @"C:\Storage"
    },
    IsDefault = true
});

await orchestrator.InitializeAsync();

// Save the configuration
await StorageConfigurationManager.SaveCurrentConfigurationAsync(
    orchestrator,
    pluginDirectory: @"C:\Plugins",
    configPath: "storage-config.json");

Console.WriteLine("Configuration saved to storage-config.json");
```

### Create and Save Configuration Manually

```csharp
var config = new StorageClusterConfiguration
{
    PluginDirectory = @"C:\Plugins",
    DefaultProvider = "MyStorage",
    Providers = new List<StorageProviderConfiguration>
    {
        new()
        {
            Name = "MyStorage",
            PluginId = "LocalFileSystem",
            Settings = new Dictionary<string, string>
            {
                ["RootPath"] = @"C:\Storage"
            },
            IsDefault = true
        }
    }
};

await StorageConfigurationManager.SaveAsync(config, "storage-config.json");
```

---

## Complete Example: First-Time Setup

```csharp
using Storage.Core;
using Storage.Plugin.Loader;

public class StorageSetup
{
    public static async Task FirstTimeSetupAsync()
    {
        Console.WriteLine("=== Storage Cluster First-Time Setup ===");
        
        // 1. Setup plugin manager
        var pluginDir = @"C:\Plugins";
        var pluginManager = new PluginManager(pluginDir);
        pluginManager.LoadPlugins();
        
        Console.WriteLine($"Loaded {pluginManager.PluginCount} plugins");
        
        // 2. Create orchestrator
        var orchestrator = new StorageOrchestrator(pluginManager);
        
        // 3. Configure providers
        Console.Write("Enter primary storage path: ");
        var primaryPath = Console.ReadLine() ?? @"C:\Storage";
        
        orchestrator.ConfigureProvider(new StorageProviderConfiguration
        {
            Name = "PrimaryStorage",
            PluginId = "LocalFileSystem",
            Settings = new Dictionary<string, string>
            {
                ["RootPath"] = primaryPath
            },
            IsDefault = true
        });
        
        Console.Write("Enter backup storage path (optional, press Enter to skip): ");
        var backupPath = Console.ReadLine();
        
        if (!string.IsNullOrWhiteSpace(backupPath))
        {
            orchestrator.ConfigureProvider(new StorageProviderConfiguration
            {
                Name = "BackupStorage",
                PluginId = "LocalFileSystem",
                Settings = new Dictionary<string, string>
                {
                    ["RootPath"] = backupPath
                },
                IsDefault = false
            });
        }
        
        // 4. Initialize
        await orchestrator.InitializeAsync();
        
        // 5. Save configuration
        var configPath = "storage-config.json";
        await StorageConfigurationManager.SaveCurrentConfigurationAsync(
            orchestrator,
            pluginDir,
            configPath);
        
        Console.WriteLine($"\n✓ Configuration saved to {configPath}");
        Console.WriteLine("You can now load this configuration on startup!");
        
        // 6. Test it
        Console.WriteLine("\nTesting storage...");
        var testContent = "Hello, Storage Cluster!"u8.ToArray();
        await using var stream = new MemoryStream(testContent);
        await orchestrator.WriteAsync("/test.txt", stream);
        
        var exists = await orchestrator.ExistsAsync("/test.txt");
        Console.WriteLine($"Test file exists: {exists}");
        
        await orchestrator.DisposeAsync();
    }
}

// Run setup
await StorageSetup.FirstTimeSetupAsync();
```

---

## Complete Example: Application Startup

```csharp
using Storage.Core;

public class Application
{
    private StorageOrchestrator? _storage;
    
    public async Task StartupAsync()
    {
        const string configFile = "storage-config.json";
        
        try
        {
            // Check if config exists
            if (File.Exists(configFile))
            {
                Console.WriteLine("Loading configuration from storage-config.json...");
                _storage = await StorageConfigurationManager.CreateFromConfigAsync(configFile);
                Console.WriteLine("✓ Storage cluster ready!");
            }
            else
            {
                Console.WriteLine("No configuration file found. Running first-time setup...");
                await RunFirstTimeSetupAsync();
                
                // Load the newly created config
                _storage = await StorageConfigurationManager.CreateFromConfigAsync(configFile);
            }
            
            // Show status
            var providers = _storage.GetConfiguredProviders();
            Console.WriteLine($"\nConfigured providers: {providers.Count}");
            foreach (var provider in providers.Values)
            {
                Console.WriteLine($"  - {provider.Name} ({provider.PluginId})");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to initialize storage: {ex.Message}");
            throw;
        }
    }
    
    public async Task ShutdownAsync()
    {
        if (_storage is not null)
        {
            await _storage.DisposeAsync();
            Console.WriteLine("Storage cluster shut down.");
        }
    }
    
    private async Task RunFirstTimeSetupAsync()
    {
        // ... implement interactive setup ...
    }
}

// Usage:
var app = new Application();
await app.StartupAsync();

// ... use app._storage ...

await app.ShutdownAsync();
```

---

## Configuration Locations

### Recommended Paths

```csharp
// User-specific configuration
var userConfigPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
    "StorageCluster",
    "config.json");

// System-wide configuration
var systemConfigPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
    "StorageCluster",
    "config.json");

// Current directory
var localConfigPath = "storage-config.json";

// Check in order of preference
string configPath = File.Exists(localConfigPath) ? localConfigPath
    : File.Exists(userConfigPath) ? userConfigPath
    : systemConfigPath;
```

---

## Configuration Validation

```csharp
public static class ConfigurationValidator
{
    public static async Task<List<string>> ValidateConfigAsync(string configPath)
    {
        var errors = new List<string>();
        
        try
        {
            var config = await StorageConfigurationManager.LoadAsync(configPath);
            
            // Check plugin directory exists
            if (!Directory.Exists(config.PluginDirectory))
            {
                errors.Add($"Plugin directory does not exist: {config.PluginDirectory}");
            }
            
            // Check providers
            if (config.Providers.Count == 0)
            {
                errors.Add("No providers configured");
            }
            
            foreach (var provider in config.Providers)
            {
                if (string.IsNullOrWhiteSpace(provider.Name))
                    errors.Add("Provider missing name");
                    
                if (string.IsNullOrWhiteSpace(provider.PluginId))
                    errors.Add($"Provider '{provider.Name}' missing plugin ID");
                    
                if (provider.Settings.Count == 0)
                    errors.Add($"Provider '{provider.Name}' has no settings");
            }
            
            // Check default provider exists
            if (config.DefaultProvider is not null)
            {
                var hasDefault = config.Providers.Any(p => p.Name == config.DefaultProvider);
                if (!hasDefault)
                {
                    errors.Add($"Default provider '{config.DefaultProvider}' not found in providers list");
                }
            }
        }
        catch (Exception ex)
        {
            errors.Add($"Failed to load config: {ex.Message}");
        }
        
        return errors;
    }
}

// Usage:
var errors = await ConfigurationValidator.ValidateConfigAsync("storage-config.json");
if (errors.Any())
{
    Console.WriteLine("Configuration errors:");
    foreach (var error in errors)
    {
        Console.WriteLine($"  - {error}");
    }
}
```

---

## Benefits

✅ **No Re-provisioning** - Configuration persists across restarts  
✅ **Version Control** - Check config files into Git  
✅ **Environment-Specific** - Different configs for dev/staging/prod  
✅ **Security** - Use environment variables for secrets  
✅ **Portability** - Easy to move between machines  
✅ **Automation** - Scriptable deployment

---

## Next Steps

1. Create your first configuration file using the example
2. Load it with `CreateFromConfigAsync()`
3. Save changes with `SaveCurrentConfigurationAsync()`
4. Check it into version control (exclude secrets!)
5. Use environment variables for sensitive values
