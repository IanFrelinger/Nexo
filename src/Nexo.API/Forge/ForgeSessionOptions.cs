namespace Nexo.API.Forge;

/// <summary>
/// Optional durable Forge session storage configuration.
/// </summary>
public sealed class ForgeSessionOptions
{
    public const string SectionPath = "Nexo:ForgeSession";

    /// <summary>
    /// When set, session and macros persist via LiteDB at this path (absolute or relative to content root).
    /// </summary>
    public string? LiteDbPath { get; set; }
}
