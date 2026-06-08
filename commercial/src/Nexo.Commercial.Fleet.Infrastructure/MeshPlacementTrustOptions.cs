namespace Nexo.Commercial.Fleet.Infrastructure;

/// <summary>Director placement filter by fleet node trust tier (virtual lab + production shaping).</summary>
public sealed class MeshPlacementTrustOptions
{
    public const string SectionPath = "Nexo:Mesh:Placement";

    /// <summary><c>any</c>, <c>trusted-preferred</c>, <c>trusted-only</c>, or <c>allowlist</c> (same semantics as peer routing).</summary>
    public string PeerTrustPolicy { get; set; } = "any";
}
