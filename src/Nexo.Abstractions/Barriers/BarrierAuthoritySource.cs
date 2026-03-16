namespace Nexo.Abstractions.Barriers;

/// <summary>
/// Authority source labels for barrier issuance.
/// </summary>
public static class BarrierAuthoritySource
{
    public static readonly string Cli = "CLI";
    public static readonly string Header = "Header";
    public static readonly string Claim = "Claim";
    public static readonly string Default = "Default";
}
