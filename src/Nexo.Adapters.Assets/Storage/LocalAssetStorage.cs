using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nexo.Orchestration.Assets.Ports;
using System.IO;

namespace Nexo.Adapters.Assets.Storage;

/// <summary>
/// Local filesystem-based asset storage implementation.
/// 
/// Responsibilities:
/// - Stores generated assets in local filesystem
/// - Organizes assets by agent ID
/// - Supports file and directory storage
/// - Provides asset retrieval by ID
/// - Handles asset deletion
/// 
/// Implements IAssetStorage for use with asset generation agents.
/// Uses configuration for storage path (defaults to ~/.local/share/Nexo/Assets).
/// </summary>
public sealed class LocalAssetStorage : IAssetStorage
{
    private readonly string _basePath;
    private readonly ILogger<LocalAssetStorage> _logger;

    public LocalAssetStorage(
        IConfiguration configuration,
        ILogger<LocalAssetStorage> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _basePath = configuration["Assets:StoragePath"] 
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Nexo", "Assets");
        
        Directory.CreateDirectory(_basePath);
    }

    public Task<string> StoreAsync(
        string sourceFilePath,
        string agentId,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(sourceFilePath))
        {
            throw new FileNotFoundException($"Source file not found: {sourceFilePath}");
        }

        var fileName = Path.GetFileName(sourceFilePath);
        var agentDir = Path.Combine(_basePath, agentId);
        Directory.CreateDirectory(agentDir);

        var destPath = Path.Combine(agentDir, fileName);
        File.Copy(sourceFilePath, destPath, overwrite: true);

        _logger.LogInformation("Stored asset: {Source} → {Dest}", sourceFilePath, destPath);

        return Task.FromResult(destPath);
    }

    public Task<string> StoreDirectoryAsync(
        string sourceDirectoryPath,
        string relativePath,
        CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(sourceDirectoryPath))
        {
            throw new DirectoryNotFoundException($"Source directory not found: {sourceDirectoryPath}");
        }

        var destDir = Path.Combine(_basePath, relativePath);
        Directory.CreateDirectory(destDir);

        // Copy all files recursively
        CopyDirectory(sourceDirectoryPath, destDir);

        _logger.LogInformation("Stored directory: {Source} → {Dest}", sourceDirectoryPath, destDir);

        return Task.FromResult(destDir);
    }

    public Task<string?> GetAsync(
        string assetId,
        CancellationToken cancellationToken = default)
    {
        // Simple implementation: search for file by name
        var files = Directory.GetFiles(_basePath, "*", SearchOption.AllDirectories)
            .FirstOrDefault(f => Path.GetFileName(f) == assetId || f.Contains(assetId));

        return Task.FromResult<string?>(files);
    }

    public Task<bool> DeleteAsync(
        string assetId,
        CancellationToken cancellationToken = default)
    {
        var file = GetAsync(assetId, cancellationToken).Result;
        if (file == null || !File.Exists(file))
        {
            return Task.FromResult(false);
        }

        File.Delete(file);
        _logger.LogInformation("Deleted asset: {Path}", file);
        return Task.FromResult(true);
    }

    private void CopyDirectory(string sourceDir, string destDir)
    {
        Directory.CreateDirectory(destDir);

        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var destFile = Path.Combine(destDir, Path.GetFileName(file));
            File.Copy(file, destFile, overwrite: true);
        }

        foreach (var subDir in Directory.GetDirectories(sourceDir))
        {
            var destSubDir = Path.Combine(destDir, Path.GetFileName(subDir));
            CopyDirectory(subDir, destSubDir);
        }
    }
}

