using System.Text.Json;
using Microsoft.Agents.AI;
using Nexo.Core.Application.Skills;
using Nexo.Core.Application.Skills.Models;
using Nexo.Core.Application.Skills.Ports;
using Nexo.Core.Application.Trust.Ports;

namespace Nexo.Skills.AgentFramework.ClassSkills;

/// <summary>
/// Class-based skill that returns the active trust policy pack id and version.
/// </summary>
public sealed class NexoActivePolicyPackSkill : AgentClassSkill<NexoActivePolicyPackSkill>
{
    private readonly ITrustPolicyPackRegistry _registry;

    /// <summary>Initializes a new active policy pack skill.</summary>
    public NexoActivePolicyPackSkill(ITrustPolicyPackRegistry registry)
        : base(null)
        => _registry = registry;

    /// <inheritdoc />
    public override AgentSkillFrontmatter Frontmatter { get; } =
        new("nexo-active-policy-pack", "Returns the active Nexo trust policy pack id and version.");

    /// <inheritdoc />
    protected override string Instructions =>
        "Use this skill to inspect which trust policy pack is currently active in Nexo.";

    /// <summary>Returns the active policy pack summary.</summary>
    [AgentSkillScript]
    public string GetActivePolicySummary(JsonElement? args)
    {
        var active = _registry.GetActivePack();
        if (active == null)
            return "No active trust policy pack.";

        var pack = _registry.GetById(active.Id);
        var version = pack?.Version ?? active.Version;
        var displayName = pack?.DisplayName ?? active.Id;
        return $"{displayName} ({active.Id}@{version}) activated at {active.ActivatedAtUtc:O}";
    }
}
