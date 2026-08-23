namespace Ashlar.Commercial.Fleet.Infrastructure;

/// <summary>Director placement filter by fleet node trust tier (virtual lab + production shaping).</summary>
public sealed class MeshPlacementTrustOptions
{
    /// <summary>Constant value for section path.</summary>
    public const string SectionPath = "Ashlar:Mesh:Placement";

    /// <summary><c>any</c>, <c>trusted-preferred</c>, <c>trusted-only</c>, or <c>allowlist</c> (same semantics as peer routing).</summary>
    public string PeerTrustPolicy { get; set; } = "any";
}
