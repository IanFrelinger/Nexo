namespace Ashlar.Infrastructure.Maintenance;

/// <summary>
/// Configuration for IncompleteBlobCleanupStrategy.
/// BlobStoragePath from ASHLAR_INCOMPLETE_BLOB_PATH. When unset, strategy is effectively no-op.
/// </summary>
public sealed class IncompleteBlobCleanupOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "IncompleteBlobCleanup";

    /// <summary>Base path to blob storage (e.g. ~/.docker/models/blobs or ~/.ollama/models/blobs).</summary>
    public string? BlobStoragePath { get; set; }
}
