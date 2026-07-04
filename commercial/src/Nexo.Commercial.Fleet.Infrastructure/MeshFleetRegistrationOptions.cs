namespace Nexo.Commercial.Fleet.Infrastructure;

/// <summary>
/// Director fleet registration policy (per-peer credentials vs shared operator API key).
/// </summary>
public sealed class MeshFleetRegistrationOptions
{
    /// <summary>Constant value for section path.</summary>
    public const string SectionPath = "Nexo:Mesh:Fleet";

    /// <summary>When true, <c>peerRegistrationKey</c> is required on register and must differ from the director API key.</summary>
    public bool RequirePeerRegistrationKey { get; set; }

    /// <summary>Min registration key length value.</summary>
    public int MinRegistrationKeyLength { get; set; } = 12;
}
