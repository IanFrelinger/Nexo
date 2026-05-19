namespace Nexo.API.Security;

/// <summary>
/// Thin quota knobs for multi-tenant shaping (hourly copilot submissions per tenant).
/// </summary>
public sealed class NexoEntitlementsOptions
{
    public const string SectionPath = "Nexo:Entitlements";

    /// <summary>Zero means unlimited.</summary>
    public int MaxCopilotSubmissionsPerHour { get; set; }
}
