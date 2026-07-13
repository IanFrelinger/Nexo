namespace Nexo.Core.Application.Skills.Models;

/// <summary>Nexo-facing skill advertisement (stage 1).</summary>
public sealed record NexoSkillDescriptor(
    string Name,
    string Description,
    string? License = null,
    string? Compatibility = null,
    IReadOnlyList<string>? AllowedTools = null);
