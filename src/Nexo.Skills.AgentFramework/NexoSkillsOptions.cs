namespace Nexo.Skills.AgentFramework;

/// <summary>
/// Configuration for Nexo Agent Framework skill catalog registration.
/// </summary>
public sealed class NexoSkillsOptions
{
    /// <summary>Root directories containing file-based SKILL.md packages.</summary>
    public IList<string> SkillRootPaths { get; } = [];

    /// <summary>Timeout for human-in-the-loop script approval.</summary>
    public TimeSpan ApprovalTimeout { get; set; } = TimeSpan.FromMinutes(5);
}
