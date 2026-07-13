using System.Text.Json;
using Nexo.Core.Application.Skills.Models;

namespace Nexo.Core.Application.Skills.Ports;

/// <summary>
/// Executes skill scripts under Nexo sandbox and routing policy.
/// </summary>
public interface INexoScriptRunner
{
    Task<NexoScriptRunResult> RunAsync(
        string skillName,
        string scriptPath,
        string scriptAbsolutePath,
        string scriptContentHash,
        JsonElement? args,
        NexoScriptRunOptions options,
        NexoSkillExecutionContext context,
        CancellationToken cancellationToken = default);
}
