namespace Ashlar.Core.Application.Maintenance.Models;

/// <summary>
/// Context passed to cleanup strategies. Contains repo root and optional strategy-specific options.
/// </summary>
/// <param name="RepoRoot">Repository root path; used by repo-scoped strategies (e.g. test artifacts).</param>
/// <param name="Options">Strategy-specific configuration (e.g. BlobStoragePath override).</param>
public sealed record ArtifactCleanupContext(
    string? RepoRoot = null,
    IReadOnlyDictionary<string, string>? Options = null);
