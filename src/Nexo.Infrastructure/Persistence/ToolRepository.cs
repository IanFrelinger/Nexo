using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Interfaces;
using Nexo.Core.Domain.Models;

namespace Nexo.Infrastructure.Persistence
{
    /// <summary>
    /// Persists and manages generated tools
    /// </summary>
    public partial class ToolRepository : IToolRepository
    {
        private readonly string _toolsPath;
        private readonly ILogger<ToolRepository> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public ToolRepository(ILogger<ToolRepository> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _toolsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nexo", "tools");
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };

            // Ensure tools directory exists
            Directory.CreateDirectory(_toolsPath);
        }

        /// <inheritdoc />
        public async Task<SavedTool> SaveToolAsync(IPlugin plugin, byte[] assembly, string sourceCode, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Saving tool: {ToolName}", plugin.Name);

            try
            {
                var toolId = Guid.NewGuid();
                var toolDir = Path.Combine(_toolsPath, toolId.ToString());
                Directory.CreateDirectory(toolDir);

                // Save assembly
                var assemblyPath = Path.Combine(toolDir, $"{plugin.Name}.dll");
                await File.WriteAllBytesAsync(assemblyPath, assembly, cancellationToken);

                // Save source code
                var sourcePath = Path.Combine(toolDir, $"{plugin.Name}.cs");
                await File.WriteAllTextAsync(sourcePath, sourceCode, cancellationToken);

                // Save metadata
                var metadata = new
                {
                    Id = toolId,
                    Name = plugin.Name,
                    Description = plugin.Description,
                    Version = 1,
                    CreatedAt = DateTime.UtcNow,
                    ModifiedAt = DateTime.UtcNow,
                    AssemblyPath = assemblyPath,
                    SourcePath = sourcePath
                };

                var metadataPath = Path.Combine(toolDir, "metadata.json");
                var metadataJson = JsonSerializer.Serialize(metadata, _jsonOptions);
                await File.WriteAllTextAsync(metadataPath, metadataJson, cancellationToken);

                // Update tool index
                await UpdateToolIndexAsync(toolId, plugin.Name, cancellationToken);

                var savedTool = new SavedTool
                {
                    Id = toolId,
                    Name = plugin.Name,
                    Description = plugin.Description,
                    Version = 1,
                    CreatedAt = DateTime.UtcNow,
                    ModifiedAt = DateTime.UtcNow,
                    AssemblyPath = assemblyPath,
                    SourcePath = sourcePath
                };

                _logger.LogInformation("Tool saved successfully: {ToolName} (ID: {ToolId})", plugin.Name, toolId);
                return savedTool;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save tool: {ToolName}", plugin.Name);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<IEnumerable<ToolInfo>> ListToolsAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Listing all tools");

            try
            {
                var indexPath = Path.Combine(_toolsPath, "index.json");
                
                if (!File.Exists(indexPath))
                {
                    return Enumerable.Empty<ToolInfo>();
                }

                var indexJson = await File.ReadAllTextAsync(indexPath, cancellationToken);
                var index = JsonSerializer.Deserialize<Dictionary<string, ToolIndexEntry>>(indexJson, _jsonOptions) ?? new Dictionary<string, ToolIndexEntry>();

                var tools = new List<ToolInfo>();
                
                foreach (var entry in index.Values)
                {
                    var metadataPath = Path.Combine(_toolsPath, entry.ToolId.ToString(), "metadata.json");
                    
                    if (File.Exists(metadataPath))
                    {
                        var metadataJson = await File.ReadAllTextAsync(metadataPath, cancellationToken);
                        var metadata = JsonSerializer.Deserialize<ToolMetadata>(metadataJson, _jsonOptions);
                        
                        if (metadata != null)
                        {
                            tools.Add(new ToolInfo
                            {
                                Name = metadata.Name,
                                Description = metadata.Description,
                                Version = metadata.Version,
                                CreatedAt = metadata.CreatedAt,
                                ModifiedAt = metadata.ModifiedAt,
                                CommandName = GenerateCommandName(metadata.Name),
                                IsLoaded = false // TODO: Check if actually loaded
                            });
                        }
                    }
                }

                _logger.LogDebug("Found {ToolCount} tools", tools.Count);
                return tools;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to list tools");
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<ToolInfo?> GetToolAsync(string toolName, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Getting tool: {ToolName}", toolName);

            try
            {
                var tools = await ListToolsAsync(cancellationToken);
                return tools.FirstOrDefault(t => t.Name.Equals(toolName, StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get tool: {ToolName}", toolName);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<string?> LoadToolSourceAsync(string toolName, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Loading source for tool: {ToolName}", toolName);

            try
            {
                var tool = await GetToolAsync(toolName, cancellationToken);
                if (tool == null)
                {
                    return null;
                }

                // Find the tool directory
                var indexPath = Path.Combine(_toolsPath, "index.json");
                if (!File.Exists(indexPath))
                {
                    return null;
                }

                var indexJson = await File.ReadAllTextAsync(indexPath, cancellationToken);
                var index = JsonSerializer.Deserialize<Dictionary<string, ToolIndexEntry>>(indexJson, _jsonOptions) ?? new Dictionary<string, ToolIndexEntry>();
                
                var entry = index.Values.FirstOrDefault(e => e.Name.Equals(toolName, StringComparison.OrdinalIgnoreCase));
                if (entry == null)
                {
                    return null;
                }

                var sourcePath = Path.Combine(_toolsPath, entry.ToolId.ToString(), $"{toolName}.cs");
                
                if (File.Exists(sourcePath))
                {
                    return await File.ReadAllTextAsync(sourcePath, cancellationToken);
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load source for tool: {ToolName}", toolName);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task SaveVersionAsync(string toolName, string sourceCode, int version, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Saving version {Version} for tool: {ToolName}", version, toolName);

            try
            {
                // Find the tool directory
                var indexPath = Path.Combine(_toolsPath, "index.json");
                if (!File.Exists(indexPath))
                {
                    throw new InvalidOperationException($"Tool not found: {toolName}");
                }

                var indexJson = await File.ReadAllTextAsync(indexPath, cancellationToken);
                var index = JsonSerializer.Deserialize<Dictionary<string, ToolIndexEntry>>(indexJson, _jsonOptions) ?? new Dictionary<string, ToolIndexEntry>();
                
                var entry = index.Values.FirstOrDefault(e => e.Name.Equals(toolName, StringComparison.OrdinalIgnoreCase));
                if (entry == null)
                {
                    throw new InvalidOperationException($"Tool not found: {toolName}");
                }

                var toolDir = Path.Combine(_toolsPath, entry.ToolId.ToString());
                
                // Save new version of source code
                var sourcePath = Path.Combine(toolDir, $"{toolName}.cs");
                await File.WriteAllTextAsync(sourcePath, sourceCode, cancellationToken);

                // Update metadata
                var metadataPath = Path.Combine(toolDir, "metadata.json");
                var metadataJson = await File.ReadAllTextAsync(metadataPath, cancellationToken);
                var metadata = JsonSerializer.Deserialize<ToolMetadata>(metadataJson, _jsonOptions);
                
                if (metadata != null)
                {
                    metadata.Version = version;
                    metadata.ModifiedAt = DateTime.UtcNow;
                    
                    var updatedMetadataJson = JsonSerializer.Serialize(metadata, _jsonOptions);
                    await File.WriteAllTextAsync(metadataPath, updatedMetadataJson, cancellationToken);
                }

                _logger.LogInformation("Version {Version} saved for tool: {ToolName}", version, toolName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save version for tool: {ToolName}", toolName);
                throw;
            }
        }

        /// <summary>
        /// Updates the tool index
        /// </summary>
        private async Task UpdateToolIndexAsync(Guid toolId, string toolName, CancellationToken cancellationToken)
        {
            var indexPath = Path.Combine(_toolsPath, "index.json");
            
            Dictionary<string, ToolIndexEntry> index;
            
            if (File.Exists(indexPath))
            {
                var indexJson = await File.ReadAllTextAsync(indexPath, cancellationToken);
                index = JsonSerializer.Deserialize<Dictionary<string, ToolIndexEntry>>(indexJson, _jsonOptions) ?? new Dictionary<string, ToolIndexEntry>();
            }
            else
            {
                index = new Dictionary<string, ToolIndexEntry>();
            }

            index[toolName] = new ToolIndexEntry
            {
                ToolId = toolId,
                Name = toolName,
                CreatedAt = DateTime.UtcNow
            };

            var updatedIndexJson = JsonSerializer.Serialize(index, _jsonOptions);
            await File.WriteAllTextAsync(indexPath, updatedIndexJson, cancellationToken);
        }

        /// <summary>
        /// Generates a command name from a tool name
        /// </summary>
        private static string GenerateCommandName(string toolName)
        {
            return toolName.ToLowerInvariant()
                .Replace(" ", "-")
                .Replace("_", "-")
                .Replace(".", "-");
        }

        /// <summary>
        /// Tool index entry
        /// </summary>
        private class ToolIndexEntry
        {
            public Guid ToolId { get; set; }
            public string Name { get; set; } = string.Empty;
            public DateTime CreatedAt { get; set; }
        }

        /// <summary>
        /// Tool metadata
        /// </summary>
        private class ToolMetadata
        {
            public Guid Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public int Version { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime ModifiedAt { get; set; }
            public string AssemblyPath { get; set; } = string.Empty;
            public string SourcePath { get; set; } = string.Empty;
        }
    }
}
