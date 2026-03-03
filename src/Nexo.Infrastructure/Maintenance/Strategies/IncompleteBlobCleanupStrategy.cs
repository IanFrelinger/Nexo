using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nexo.Core.Application.Maintenance.Models;
using Nexo.Core.Application.Maintenance.Ports;
using Nexo.Infrastructure.Maintenance.Ports;

namespace Nexo.Infrastructure.Maintenance.Strategies;

/// <summary>
/// Cleans *.incomplete files in a content-addressed blob storage directory.
/// Path configured via IncompleteBlobCleanupOptions. Optional IBlobStorageLifecycle
/// for pause/resume (e.g. stop Docker before delete).
/// </summary>
public sealed class IncompleteBlobCleanupStrategy : IArtifactCleanupStrategy
{
    public const string IdConstant = "incomplete-blobs";
    private const string IncompletePattern = "*.incomplete";

    private readonly ILogger<IncompleteBlobCleanupStrategy> _logger;
    private readonly IncompleteBlobCleanupOptions _options;
    private readonly IBlobStorageLifecycle? _lifecycle;

    public IncompleteBlobCleanupStrategy(
        ILogger<IncompleteBlobCleanupStrategy> logger,
        IOptions<IncompleteBlobCleanupOptions> options,
        IBlobStorageLifecycle? lifecycle = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _lifecycle = lifecycle;
    }

    public string Id => IdConstant;
    public string Description => "Removes *.incomplete files from content-addressed blob storage.";

    public async Task<ArtifactCleanupResult> CleanAsync(ArtifactCleanupContext context, CancellationToken ct = default)
    {
        var blobPath = (context.Options != null && context.Options.TryGetValue("BlobStoragePath", out var path))
            ? path
            : _options.BlobStoragePath;

        if (string.IsNullOrWhiteSpace(blobPath) || !Directory.Exists(blobPath))
        {
            return new ArtifactCleanupResult(IdConstant, 0, Array.Empty<string>(), Array.Empty<string>());
        }

        var pathsDeleted = new List<string>();
        var errors = new List<string>();
        long bytesReclaimed = 0;

        try
        {
            if (_lifecycle != null)
            {
                await _lifecycle.PauseAsync(ct).ConfigureAwait(false);
                await Task.Delay(3000, ct).ConfigureAwait(false); // Allow service to shut down
            }

            var incompleteDir = Path.Combine(blobPath, "sha256");
            if (!Directory.Exists(incompleteDir))
            {
                incompleteDir = blobPath;
            }

            foreach (var file in Directory.EnumerateFiles(incompleteDir, IncompletePattern))
            {
                try
                {
                    var info = new FileInfo(file);
                    var size = info.Length;
                    File.Delete(file);
                    pathsDeleted.Add(file);
                    bytesReclaimed += size;
                }
                catch (Exception ex)
                {
                    errors.Add($"{file}: {ex.Message}");
                    _logger.LogWarning(ex, "Failed to delete incomplete blob: {File}", file);
                }
            }

            if (_lifecycle != null)
            {
                await _lifecycle.ResumeAsync(ct).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            errors.Add(ex.Message);
            _logger.LogError(ex, "Incomplete blob cleanup failed");
            if (_lifecycle != null)
            {
                try
                {
                    await _lifecycle.ResumeAsync(ct).ConfigureAwait(false);
                }
                catch (Exception resumeEx)
                {
                    errors.Add($"Resume failed: {resumeEx.Message}");
                }
            }
        }

        return new ArtifactCleanupResult(IdConstant, bytesReclaimed, pathsDeleted, errors);
    }
}
