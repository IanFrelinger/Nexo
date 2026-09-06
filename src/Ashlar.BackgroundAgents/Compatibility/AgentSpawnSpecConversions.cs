using Ashlar.Core.Application.Orchestration.Ports;
using Ashlar.Orchestration.Architect.Models;

namespace Ashlar.BackgroundAgents.Compatibility;

/// <summary>
/// Temporary conversion extensions for AgentSpawnSpecDto → AgentSpawnSpec.
/// Used when test code or legacy CLI code needs to instantiate Orchestration agents directly.
/// </summary>
/// <remarks>
/// TODO: Delete after application/ CLI migrates to IAgentCreator.
/// This exists only to prevent chicken-egg compile breaks during the src/-only port migration.
/// Prefer using IAgentCreator.CreateAgent(AgentSpawnSpecDto) over direct agent instantiation.
/// </remarks>
[Obsolete("Temporary compatibility shim for CLI migration. Use IAgentCreator.CreateAgent instead of direct agent instantiation. Will be removed after application/ updates.")]
public static class AgentSpawnSpecConversions
{
    /// <summary>
    /// Converts AgentSpawnSpecDto (Application port DTO) to AgentSpawnSpec (Orchestration model).
    /// </summary>
    public static AgentSpawnSpec ToOrchestrationSpec(this AgentSpawnSpecDto dto)
    {
        if (dto == null)
        {
            throw new ArgumentNullException(nameof(dto));
        }

        return new AgentSpawnSpec
        {
            AgentId = dto.AgentId,
            Name = dto.Name,
            Domain = dto.Domain,
            Goal = dto.Goal,
            Description = dto.Description,
            Dependencies = dto.Dependencies,
            OllamaModel = dto.OllamaModel
        };
    }
}
