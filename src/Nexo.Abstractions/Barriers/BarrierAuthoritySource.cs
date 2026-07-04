namespace Nexo.Abstractions.Barriers;

/// <summary>
/// Authority source labels for barrier issuance.
/// </summary>
public static class BarrierAuthoritySource
{
    /// <summary>Barrier level supplied via CLI invocation flags or environment.</summary>
    public static readonly string Cli = "CLI";

    /// <summary>Barrier level supplied via an explicit HTTP or transport header.</summary>
    public static readonly string Header = "Header";

    /// <summary>Barrier level derived from a generic identity claim.</summary>
    public static readonly string Claim = "Claim";

    /// <summary>Barrier level applied when no identity material could be resolved.</summary>
    public static readonly string Default = "Default";

    /// <summary>Barrier level derived from a client PKI certificate subject or SAN.</summary>
    public static readonly string PkiCertificate = "PkiCertificate";

    /// <summary>Barrier level derived from a JWT claim.</summary>
    public static readonly string JwtClaim = "JwtClaim";

    /// <summary>Barrier level derived from an API key mapping.</summary>
    public static readonly string ApiKey = "ApiKey";
}
