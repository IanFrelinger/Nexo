using Nexo.Core.Application.Trust.Models;

namespace Nexo.Core.Application.Trust.Ports;

/// <summary>
/// User-controlled access boundary: which data categories/sources can be observed.
/// </summary>
public interface IAccessBoundary
{
    /// <summary>When true, all observation is halted immediately.</summary>
    bool IsObservationPaused { get; }

    /// <summary>Whether the category is allowed (e.g. file-paths, terminal-output).</summary>
    bool IsCategoryAllowed(string category);

    /// <summary>Whether the source is allowed (e.g. VS Code, git, shell).</summary>
    bool IsSourceAllowed(string sourceId);

    /// <summary>Whether the source is allowed for a specific project path.</summary>
    bool IsSourceAllowedForProject(string sourceId, string? projectPath);

    /// <summary>Pause or resume all observation.</summary>
    void SetPause(bool paused);

    /// <summary>Allow or deny a category.</summary>
    void SetCategoryAllowed(string category, bool allowed);

    /// <summary>Allow or deny a source globally.</summary>
    void SetSourceAllowed(string sourceId, bool allowed);

    /// <summary>Set per-project overrides (null clears overrides for that project).</summary>
    void SetProjectOverride(string projectPath, IReadOnlyDictionary<string, bool>? overrides);

    /// <summary>Raised when any boundary setting changes.</summary>
    event Action<BoundaryChangeEvent>? BoundaryChanged;
}
