using System.Text.Json;
using Nexo.Core.Application.Skills.Models;

namespace Nexo.Core.Application.Skills.Ports;

/// <summary>
/// Nexo-owned skill catalog exposing the four-stage progressive disclosure flow.
/// </summary>
public interface INexoSkillCatalog
{
    Task<IReadOnlyList<NexoSkillDescriptor>> AdvertiseAsync(
        NexoSkillExecutionContext context,
        CancellationToken cancellationToken = default);

    Task<string> LoadAsync(
        string skillName,
        NexoSkillExecutionContext context,
        CancellationToken cancellationToken = default);

    Task<string> ReadResourceAsync(
        string skillName,
        string resourcePath,
        NexoSkillExecutionContext context,
        CancellationToken cancellationToken = default);

    Task<NexoScriptRunResult> RunScriptAsync(
        string skillName,
        string scriptPath,
        JsonElement? args,
        NexoSkillExecutionContext context,
        CancellationToken cancellationToken = default);
}
