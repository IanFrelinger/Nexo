namespace Nexo.Runtime.Barriers.Identity.Resolvers;

/// <summary>Single PKI rule matching a certificate field pattern to a barrier level.</summary>
public sealed class CertificateBarrierRule
{
    /// <summary>Rule name for audit and diagnostics.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Certificate field to match (<c>Subject</c> or <c>SAN</c>).</summary>
    public string MatchField { get; init; } = string.Empty;

    /// <summary>Regex pattern applied to the selected field.</summary>
    public string MatchPattern { get; init; } = string.Empty;

    /// <summary>Barrier level assigned when the pattern matches.</summary>
    public string BarrierLevel { get; init; } = string.Empty;
}
