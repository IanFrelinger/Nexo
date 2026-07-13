using System.Text.Json;
using Microsoft.Agents.AI;
using Nexo.Core.Application.Skills;
using Nexo.Core.Application.Skills.Models;
using Nexo.Core.Application.Skills.Ports;

namespace Nexo.Skills.AgentFramework;

/// <summary>
/// Bridges Microsoft Agent Skills script runner delegate to Nexo script execution.
/// </summary>
public sealed class AgentFrameworkScriptRunnerBridge
{
    private readonly INexoScriptRunner _scriptRunner;
    private readonly INexoSkillPolicyEvaluator _policyEvaluator;
    private readonly AsyncLocal<NexoSkillExecutionContext?> _context = new();

    /// <summary>Initializes a new script runner bridge.</summary>
    public AgentFrameworkScriptRunnerBridge(
        INexoScriptRunner scriptRunner,
        INexoSkillPolicyEvaluator policyEvaluator)
    {
        _scriptRunner = scriptRunner;
        _policyEvaluator = policyEvaluator;
    }

    /// <summary>Sets the Nexo execution context for the current async flow.</summary>
    public IDisposable BindContext(NexoSkillExecutionContext context)
    {
        var previous = _context.Value;
        _context.Value = context;
        return new ContextScope(_context, previous);
    }

    /// <summary>Microsoft Agent Skills script runner delegate implementation.</summary>
    public async Task<object?> RunAsync(
        AgentFileSkill skill,
        AgentFileSkillScript script,
        JsonElement? args,
        IServiceProvider? serviceProvider,
        CancellationToken cancellationToken = default)
    {
        var context = _context.Value
            ?? throw new InvalidOperationException("Nexo skill execution context is not bound.");

        var scriptPath = script.Name;
        var absolutePath = ResolveScriptPath(skill, scriptPath);
        var hash = SkillContentHasher.ComputeFileHash(absolutePath);
        var limits = _policyEvaluator.GetScriptLimits(context);

        var result = await _scriptRunner.RunAsync(
            skill.Frontmatter.Name,
            scriptPath,
            absolutePath,
            hash,
            args,
            limits,
            context,
            cancellationToken).ConfigureAwait(false);

        return (object?)result.Output;
    }

    private static string ResolveScriptPath(AgentFileSkill skill, string scriptPath)
    {
        if (Path.IsPathRooted(scriptPath))
            return scriptPath;

        var skillRoot = skill.Path ?? throw new InvalidOperationException("File skill path is unavailable.");
        return Path.GetFullPath(Path.Combine(skillRoot, scriptPath));
    }

    private sealed class ContextScope : IDisposable
    {
        private readonly AsyncLocal<NexoSkillExecutionContext?> _holder;
        private readonly NexoSkillExecutionContext? _previous;
        private bool _disposed;

        public ContextScope(AsyncLocal<NexoSkillExecutionContext?> holder, NexoSkillExecutionContext? previous)
        {
            _holder = holder;
            _previous = previous;
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _holder.Value = _previous;
            _disposed = true;
        }
    }
}
