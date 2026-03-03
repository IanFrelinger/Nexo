namespace Nexo.Core.Application.Maintenance.Models;

/// <summary>
/// Configuration for procedural artifact cleanup (e.g. around test runs).
/// Bound from NEXO_CLEAN_BEFORE_TEST, NEXO_CLEAN_AFTER_TEST, NEXO_ARTIFACT_CLEANUP_REPO_ROOT.
/// </summary>
public sealed class ArtifactCleanupOptions
{
    public const string SectionName = "ArtifactCleanup";

    /// <summary>Run test-artifacts cleanup before test execution.</summary>
    public bool CleanBeforeTest { get; set; }

    /// <summary>Run test-artifacts cleanup after test execution.</summary>
    public bool CleanAfterTest { get; set; }

    /// <summary>Optional repo root override; null = use RepoPathResolver.</summary>
    public string? RepoRoot { get; set; }
}
