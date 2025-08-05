using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Interfaces;

namespace Nexo.Infrastructure.Adapters.Configuration
{

/// <summary>
/// A provider for handling JSON-based configurations with support for caching and serialization/deserialization operations.
/// </summary>
public sealed class JsonConfigurationProvider : IConfigurationProvider
{
    /// <summary>
    /// Represents an instance of the file system abstraction used to interact with the underlying file system
    /// for operations such as reading, writing, and verifying files or directories.
    /// </summary>
    /// <remarks>
    /// This dependency allows for separation of concerns by abstracting file operations, enabling easier testing
    /// and mocking during unit tests. It facilitates methods like reading file content, checking file or directory
    /// existence, writing to files, and creating directories.
    /// </remarks>
    private readonly IFileSystem _fileSystem;

    /// <summary>
    /// Represents the root directory path for storing and retrieving configuration files.
    /// </summary>
    /// <remarks>
    /// This variable is used to determine where all configuration files are stored on the file system.
    /// It serves as the base path for saving, loading, and managing configuration data in JSON format.
    /// </remarks>
    private readonly string _configPath;

    /// <summary>
    /// Logger instance used for logging diagnostic and configuration-related messages
    /// within the <see cref="JsonConfigurationProvider"/> class.
    /// </summary>
    /// <remarks>
    /// Provides detailed logging for operations such as retrieving, saving, and
    /// reloading configurations. Uses dependency injection to allow for external
    /// configuration and testing.
    /// </remarks>
    private readonly ILogger<JsonConfigurationProvider> _logger;

    /// <summary>
    /// Holds the JSON serialization options for configuring the behavior of
    /// the System.Text.Json.JsonSerializer used in this provider.
    /// </summary>
    /// <remarks>
    /// These options include:
    /// - Pretty-printing JSON with indented formatting.
    /// - Enabling case-insensitive property name matching during deserialization.
    /// - Applying a camel case naming policy for serialized property names.
    /// - Adding support for enum serialization and deserialization using camel case.
    /// </remarks>
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// A thread-safe cache that stores configuration objects based on their types.
    /// </summary>
    /// <remarks>
    /// The cache is used to temporarily store deserialized configuration objects, reducing the need to
    /// repeatedly read and deserialize configuration files from the filesystem. This improves performance
    /// for applications with frequent configuration access operations. The keys in the cache are
    /// the Type of the configuration objects, while the values are the corresponding deserialized objects.
    /// </remarks>
    private readonly ConcurrentDictionary<Type, object> _cache = new ConcurrentDictionary<Type, object>();

    /// <summary>
    /// A semaphore used to synchronize access to the cache and ensure thread-safe operations
    /// when performing configuration-related tasks, such as loading, saving, or deleting
    /// configuration data.
    /// </summary>
    private readonly SemaphoreSlim _cacheLock = new SemaphoreSlim(1, 1);

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonConfigurationProvider"/> class.
    /// Provides functionality to handle JSON-based configuration storage with support for serialization, deserialization, and file operations.
    /// </summary>
    public JsonConfigurationProvider(
        IFileSystem fileSystem,
        string configPath,
        ILogger<JsonConfigurationProvider> logger)
    {
        if (fileSystem != null)
        {
            if (string.IsNullOrWhiteSpace(configPath))
            {
                throw new ArgumentException("Config path cannot be null or empty", nameof(configPath));
            }

            if (logger != null)
            {
                _fileSystem = fileSystem;
                _configPath = configPath;
                _logger = logger;
                _jsonOptions = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    Converters =
                    {
                        new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
                    }
                };
            }
            else
            {
                throw new ArgumentNullException(nameof(logger));
            }
        }
        else
        {
            throw new ArgumentNullException(nameof(fileSystem));
        }
    }

    /// <summary>
    /// Retrieves the configuration path used by the provider.
    /// </summary>
    /// <returns>The path to the configuration file.</returns>
    public string GetConfigurationPath() => _configPath;

    /// <summary>
    /// Asynchronously retrieves a configuration of type <typeparamref name="T"/>.
    /// If a cached configuration is available, it returns the cached version; otherwise, it reads from a file or creates a new instance.
    /// </summary>
    /// <typeparam name="T">The type of configuration to retrieve. Must be a class with a parameterless constructor.</typeparam>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>An instance of configuration of type <typeparamref name="T"/>.</returns>
    public async Task<T> GetConfigurationAsync<T>(CancellationToken cancellationToken = default) 
        where T : class, new()
    {
        await _cacheLock.WaitAsync(cancellationToken);
        try
        {
            if (_cache.TryGetValue(typeof(T), out var cached))
            {
                _logger.LogDebug("Returning cached configuration for {Type}", typeof(T).Name);
                return (T)cached;
            }
            
            var filePath = GetConfigurationFilePath<T>();
            
            if (!await _fileSystem.FileExistsAsync(filePath, cancellationToken))
            {
                _logger.LogDebug("Configuration file not found for {Type}, returning new instance", typeof(T).Name);
                var newConfig = new T();
                _cache[typeof(T)] = newConfig;
                return newConfig;
            }
            
            var json = await _fileSystem.ReadTextAsync(filePath, cancellationToken);
            var config = JsonSerializer.Deserialize<T>(json, _jsonOptions) ?? new T();
            
            _cache[typeof(T)] = config;
            _logger.LogDebug("Loaded configuration for {Type} from {FilePath}", typeof(T).Name, filePath);
            
            return config;
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    /// <summary>
    /// Asynchronously saves the given configuration to the configured path.
    /// </summary>
    /// <typeparam name="T">The type of configuration object to save.</typeparam>
    /// <param name="configuration">The configuration instance to save. Cannot be null.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous save operation.</returns>
    public async Task SaveConfigurationAsync<T>(
        T configuration,
        CancellationToken cancellationToken = default) 
        where T : class
    {
        if (configuration != null)
        {
            await _cacheLock.WaitAsync(cancellationToken);
            try
            {
                // Ensure the configuration directory exists
                await _fileSystem.CreateDirectoryAsync(_configPath, cancellationToken);

                var filePath = GetConfigurationFilePath<T>();
                var json = JsonSerializer.Serialize(configuration, _jsonOptions);

                await _fileSystem.WriteTextAsync(filePath, json, cancellationToken);

                // Update cache
                _cache[typeof(T)] = configuration;

                _logger.LogDebug("Saved configuration for {Type} to {FilePath}", typeof(T).Name, filePath);
            }
            finally
            {
                _cacheLock.Release();
            }
        }
        else
        {
            throw new ArgumentNullException(nameof(configuration));
        }
    }

    /// <summary>
    /// Determines whether a configuration file exists for the specified type.
    /// </summary>
    /// <typeparam name="T">The type of the configuration entity to check existence for.</typeparam>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation. The task result is <c>true</c> if the configuration file exists; otherwise, <c>false</c>.</returns>
    public async Task<bool> ExistsAsync<T>(CancellationToken cancellationToken = default) 
        where T : class
    {
        var filePath = GetConfigurationFilePath<T>();
        return await _fileSystem.FileExistsAsync(filePath, cancellationToken);
    }

    /// <summary>
    /// Deletes the configuration file and removes the associated entry from the cache, if it exists.
    /// </summary>
    /// <typeparam name="T">The type of the configuration to delete.</typeparam>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous delete operation.</returns>
    public async Task DeleteAsync<T>(CancellationToken cancellationToken = default) 
        where T : class
    {
        await _cacheLock.WaitAsync(cancellationToken);
        try
        {
            var filePath = GetConfigurationFilePath<T>();
            
            if (await _fileSystem.FileExistsAsync(filePath, cancellationToken))
            {
                await _fileSystem.DeleteFileAsync(filePath, cancellationToken);
                _logger.LogDebug("Deleted configuration for {Type}", typeof(T).Name);
            }
            
            // Remove from cache
            _cache.TryRemove(typeof(T), out _);
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    /// <summary>
    /// Reloads the configuration of a specified type, forcing the cache to refresh with the latest data.
    /// </summary>
    /// <typeparam name="T">The type of the configuration to reload.</typeparam>
    /// <param name="cancellationToken">A token to signal the cancellation of the reload operation.</param>
    /// <returns>The reloaded configuration of the specified type.</returns>
    public async Task<T> ReloadAsync<T>(CancellationToken cancellationToken = default) 
        where T : class, new()
    {
        await _cacheLock.WaitAsync(cancellationToken);
        try
        {
            // Remove from cache to force reload
            _cache.TryRemove(typeof(T), out _);
        }
        finally
        {
            _cacheLock.Release();
        }
        
        return await GetConfigurationAsync<T>(cancellationToken);
    }

    /// <summary>
    /// Constructs the file path for the configuration file associated with the specified type.
    /// </summary>
    /// <typeparam name="T">The type of the configuration object.</typeparam>
    /// <returns>The file path for the configuration file corresponding to the specified type.</returns>
    private string GetConfigurationFilePath<T>()
    {
        var typeName = typeof(T).Name;
        
        // Remove common suffixes
        if (typeName.EndsWith("Configuration", StringComparison.OrdinalIgnoreCase))
        {
            typeName = typeName.Substring(0, typeName.Length - 13);
        }
        else if (typeName.EndsWith("Config", StringComparison.OrdinalIgnoreCase))
        {
            typeName = typeName.Substring(0, typeName.Length - 6);
        }
        
        var fileName = $"{char.ToLowerInvariant(typeName[0])}{typeName.Substring(1)}";
        return Path.Combine(_configPath, fileName);
    }
}
}