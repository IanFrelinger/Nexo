namespace Ashlar.Core.Application.Trust.Ports;

/// <summary>
/// Called by observation pipelines before storing or processing any data.
/// Returns false if data should be dropped (category/source disabled or paused).
/// </summary>
public interface IObservationGate
{
    /// <summary>
    /// Check whether data from the given category and source should be observed.
    /// </summary>
    /// <param name="category">Data category (e.g. file-paths, edit-history, terminal-output, git-metadata).</param>
    /// <param name="sourceId">Source identifier (e.g. VS Code, git, shell).</param>
    /// <param name="projectPath">Optional project path for per-project overrides.</param>
    /// <returns>True if data should be stored/processed; false to drop.</returns>
    bool ShouldObserve(string category, string sourceId, string? projectPath = null);
}
