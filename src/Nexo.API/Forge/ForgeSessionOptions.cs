namespace Nexo.API.Forge;

/// <summary>
/// Configuration for durable Forge session storage.
/// </summary>
public sealed class ForgeSessionOptions
{
    public const string SectionPath = "Nexo:ForgeSession";

    /// <summary>
    /// When set, Forge session and macro registry are persisted with LiteDB at this path
    /// (absolute or relative to the content root). When null or empty, state stays in memory only.
    /// </summary>
    public string? LiteDbPath { get; set; }
}
