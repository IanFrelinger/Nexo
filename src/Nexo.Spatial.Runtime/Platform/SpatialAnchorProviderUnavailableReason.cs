using Nexo.Spatial.Contracts;

namespace Nexo.Spatial.Runtime.Platform;

/// <summary>
/// Why no <see cref="ISpatialAnchorProvider"/> could be resolved for the current host.
/// Distinct from in-provider <see cref="TrackingState.Lost"/> (tracking was available, then dropped).
/// </summary>
public enum SpatialAnchorProviderUnavailableReason
{
    UnsupportedHostPlatform,
    NoRegisteredProvider,
    ProviderNotReady
}
