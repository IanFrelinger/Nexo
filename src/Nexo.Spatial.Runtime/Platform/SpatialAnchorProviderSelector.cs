using Nexo.Spatial.Contracts;
using Nexo.Spatial.Platform.ARKit;
using Nexo.Spatial.Platform.VisionPro;
using Nexo.Spatial.Platform.XREAL;

namespace Nexo.Spatial.Runtime.Platform;

/// <summary>
/// P2-S4 host platform glue: selects an <see cref="ISpatialAnchorProvider"/> for the running device.
/// Does not bypass per-platform fail-closed behavior — only picks which shell to construct.
/// </summary>
/// <remarks>
/// Deterministic priority (ordinal platform id tie-break when multiple are supported):
/// visionpro → arkit → xreal. Headless CI resolves to explicit unavailable, never a silent no-op provider.
/// </remarks>
public sealed class SpatialAnchorProviderSelector
{
    private static readonly string[] DefaultPriority = ["visionpro", "arkit", "xreal"];

    private readonly IReadOnlyList<SpatialPlatformProviderRegistration> _registrations;
    private readonly IReadOnlyList<string> _priority;

    public SpatialAnchorProviderSelector(
        IEnumerable<SpatialPlatformProviderRegistration>? registrations = null,
        IEnumerable<string>? priorityOrder = null)
    {
        _registrations = (registrations ?? CreateDefaultRegistrations()).ToList();
        _priority = (priorityOrder ?? DefaultPriority).ToList();
    }

    /// <summary>Resolves the highest-priority supported anchor provider for the current host.</summary>
    public SpatialAnchorProviderResolution Resolve()
    {
        if (_registrations.Count == 0)
            return SpatialAnchorProviderResolution.Unavailable(
                SpatialAnchorProviderUnavailableReason.NoRegisteredProvider);

        var supported = _registrations
            .Where(r => r.IsSupported())
            .OrderBy(r => PriorityIndex(r.PlatformId))
            .ThenBy(r => r.PlatformId, StringComparer.Ordinal)
            .ToList();

        if (supported.Count == 0)
        {
            return SpatialAnchorProviderResolution.Unavailable(
                SpatialAnchorProviderUnavailableReason.UnsupportedHostPlatform);
        }

        var selected = supported[0];
        return SpatialAnchorProviderResolution.Available(selected.Factory(), selected.PlatformId);
    }

    private int PriorityIndex(string platformId)
    {
        var index = -1;
        for (var i = 0; i < _priority.Count; i++)
        {
            if (string.Equals(_priority[i], platformId, StringComparison.Ordinal))
            {
                index = i;
                break;
            }
        }

        return index >= 0 ? index : int.MaxValue;
    }

    private static IEnumerable<SpatialPlatformProviderRegistration> CreateDefaultRegistrations() =>
    [
        new SpatialPlatformProviderRegistration
        {
            PlatformId = "visionpro",
            IsSupported = VisionProSpatialAvailability.IsSupported,
            Factory = static () => new VisionProSpatialAnchorProvider()
        },
        new SpatialPlatformProviderRegistration
        {
            PlatformId = "arkit",
            IsSupported = ArKitSpatialAvailability.IsSupported,
            Factory = static () => new ArKitSpatialAnchorProvider()
        },
        new SpatialPlatformProviderRegistration
        {
            PlatformId = "xreal",
            IsSupported = XrealSpatialAvailability.IsSupported,
            Factory = static () => new XrealSpatialAnchorProvider()
        }
    ];
}
