namespace Nexo.Spike.S3.Models;

/// <summary>
/// Describes the capability an agent needs. Matching rules are documented in
/// <c>docs/spike/s3-intent-matching.md</c>.
/// </summary>
public sealed record IntentDescriptor(
    string IntentKey,
    IReadOnlyList<string> Tags,
    string? CapabilityKey = null);
