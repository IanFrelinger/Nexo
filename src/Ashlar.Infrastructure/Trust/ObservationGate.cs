using Ashlar.Core.Application.Trust.Ports;

namespace Ashlar.Infrastructure.Trust;

/// <summary>
/// Delegates to IAccessBoundary. Returns false when paused or category/source denied.
/// </summary>
public sealed class ObservationGate : IObservationGate
{
    private readonly IAccessBoundary _boundary;

    /// <summary>
    /// Creates an observation gate that delegates to the given access boundary.
    /// </summary>
    public ObservationGate(IAccessBoundary boundary)
    {
        _boundary = boundary ?? throw new ArgumentNullException(nameof(boundary));
    }

    /// <inheritdoc />
    public bool ShouldObserve(string category, string sourceId, string? projectPath = null)
    {
        if (_boundary.IsObservationPaused)
            return false;
        if (!_boundary.IsCategoryAllowed(category ?? string.Empty))
            return false;
        return _boundary.IsSourceAllowedForProject(sourceId ?? string.Empty, projectPath);
    }
}
