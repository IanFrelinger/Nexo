using Nexo.Spatial.Runtime.Ports;

namespace Nexo.Tests.Infrastructure.Tests.Spatial;

internal static class SpatialIdentityFixtures
{
    internal const string DefaultAssetHash =
        "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";

    internal static ResolvedAtomIdentity Create(
        string atomId,
        string? assetHash = null,
        string assetVersion = "1.0.0") =>
        new(atomId, assetHash ?? DefaultAssetHash, assetVersion);
}
