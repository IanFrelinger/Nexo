using System.Text.Json;
using Microsoft.Agents.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Nexo.Core.Application.Skills;
using Nexo.Core.Application.Skills.Models;
using Nexo.Core.Application.Skills.Ports;
using Nexo.Skills.AgentFramework.ClassSkills;

namespace Nexo.Skills.AgentFramework;

/// <summary>
/// Nexo skill catalog adapter over Microsoft Agent Skills provider builder.
/// </summary>
public sealed class AgentFrameworkSkillCatalog : INexoSkillCatalog, IDisposable
{
    private readonly AgentSkillsProvider _provider;
    private readonly AgentSkillsSource _source;
    private readonly INexoSkillPolicyEvaluator _policyEvaluator;
    private readonly INexoSkillAuditLog _auditLog;
    private readonly INexoSkillApprovalResolver _approvalResolver;
    private readonly AgentFrameworkScriptRunnerBridge _scriptRunnerBridge;
    private readonly ILoggerFactory _loggerFactory;
    private bool _disposed;

    /// <summary>Initializes a new Agent Framework skill catalog.</summary>
    public AgentFrameworkSkillCatalog(
        IOptions<NexoSkillsOptions> options,
        INexoSkillPolicyEvaluator policyEvaluator,
        INexoSkillAuditLog auditLog,
        INexoSkillApprovalResolver approvalResolver,
        AgentFrameworkScriptRunnerBridge scriptRunnerBridge,
        INexoSkillCacheController cacheController,
        NexoActivePolicyPackSkill classSkill,
        ILoggerFactory? loggerFactory = null)
    {
        _policyEvaluator = policyEvaluator;
        _auditLog = auditLog;
        _approvalResolver = approvalResolver;
        _scriptRunnerBridge = scriptRunnerBridge;
        _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;

        var skillRoots = options.Value.SkillRootPaths
            .Where(static path => !string.IsNullOrWhiteSpace(path))
            .Select(Path.GetFullPath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        _source = BuildSourcePipeline(skillRoots, cacheController, scriptRunnerBridge, classSkill, _loggerFactory);
        _provider = new AgentSkillsProvider(
            _source,
            new AgentSkillsProviderOptions
            {
                DisableLoadSkillApproval = true,
                DisableReadSkillResourceApproval = true,
                DisableRunSkillScriptApproval = false,
            },
            _loggerFactory,
            ownsSource: true);
    }

    /// <summary>Exposes the underlying Microsoft Agent Skills provider for agent wiring.</summary>
    public AgentSkillsProvider Provider => _provider;

    /// <inheritdoc />
    public async Task<IReadOnlyList<NexoSkillDescriptor>> AdvertiseAsync(
        NexoSkillExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        using (_scriptRunnerBridge.BindContext(context))
        {
            var skills = await _source.GetSkillsAsync(CreateSourceContext(), cancellationToken).ConfigureAwait(false);
            var visible = skills
                .Select(MapDescriptor)
                .Where(descriptor => _policyEvaluator.IsSkillVisible(descriptor, context))
                .ToList();

            foreach (var descriptor in visible)
            {
                EmitAudit(
                    NexoSkillAuditEventType.SkillAdvertised,
                    descriptor.Name,
                    context,
                    detail: descriptor.Description);
            }

            return visible;
        }
    }

    /// <inheritdoc />
    public async Task<string> LoadAsync(
        string skillName,
        NexoSkillExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        using (_scriptRunnerBridge.BindContext(context))
        {
            var skill = await FindSkillAsync(skillName, context, cancellationToken).ConfigureAwait(false);
            var content = await skill.GetContentAsync(cancellationToken).ConfigureAwait(false);
            EmitAudit(NexoSkillAuditEventType.SkillLoaded, skillName, context);
            return content;
        }
    }

    /// <inheritdoc />
    public async Task<string> ReadResourceAsync(
        string skillName,
        string resourcePath,
        NexoSkillExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        using (_scriptRunnerBridge.BindContext(context))
        {
            var skill = await FindSkillAsync(skillName, context, cancellationToken).ConfigureAwait(false);
            var resource = await skill.GetResourceAsync(resourcePath, cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException($"Resource '{resourcePath}' was not found in skill '{skillName}'.");
            var content = await resource.ReadAsync(serviceProvider: null, cancellationToken).ConfigureAwait(false);
            var text = content?.ToString() ?? string.Empty;
            EmitAudit(
                NexoSkillAuditEventType.SkillResourceRead,
                skillName,
                context,
                resourcePath: resourcePath);
            return text;
        }
    }

    /// <inheritdoc />
    public async Task<NexoScriptRunResult> RunScriptAsync(
        string skillName,
        string scriptPath,
        JsonElement? args,
        NexoSkillExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        using (_scriptRunnerBridge.BindContext(context))
        {
            var skill = await FindSkillAsync(skillName, context, cancellationToken).ConfigureAwait(false);
            var script = await skill.GetScriptAsync(scriptPath, cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException($"Script '{scriptPath}' was not found in skill '{skillName}'.");

            var absolutePath = ResolveScriptAbsolutePath(skill, scriptPath);
            var hash = File.Exists(absolutePath)
                ? SkillContentHasher.ComputeFileHash(absolutePath)
                : string.Empty;

            var approvalKey = new SkillScriptApprovalKey(
                skillName,
                scriptPath,
                context.TrustTier,
                context.BarrierLevel,
                context.PolicyPackId);

            EmitAudit(
                NexoSkillAuditEventType.SkillScriptApprovalRequested,
                skillName,
                context,
                scriptPath: scriptPath,
                scriptContentHash: hash);

            var approval = await _approvalResolver.ResolveScriptApprovalAsync(
                approvalKey,
                $"Run skill script {skillName}:{scriptPath}",
                cancellationToken).ConfigureAwait(false);

            EmitAudit(
                NexoSkillAuditEventType.SkillScriptApprovalResolved,
                skillName,
                context,
                scriptPath: scriptPath,
                scriptContentHash: hash,
                outcome: approval.ToString());

            if (approval is NexoSkillApprovalStatus.Denied or NexoSkillApprovalStatus.TimedOut or NexoSkillApprovalStatus.Pending)
            {
                return new NexoScriptRunResult(
                    Output: $"Script approval {approval}.",
                    ExitCode: 1,
                    Duration: TimeSpan.Zero,
                    OutputTruncated: false,
                    ScriptContentHash: hash,
                    Outcome: approval.ToString().ToLowerInvariant());
            }

            var started = DateTimeOffset.UtcNow;
            object? output;
            try
            {
                output = await script.RunAsync(skill, args, serviceProvider: null, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                var failed = DateTimeOffset.UtcNow - started;
                EmitAudit(
                    NexoSkillAuditEventType.SkillScriptExecuted,
                    skillName,
                    context,
                    scriptPath: scriptPath,
                    scriptContentHash: hash,
                    outcome: "failure",
                    durationMs: (long)failed.TotalMilliseconds,
                    detail: ex.Message);

                return new NexoScriptRunResult(
                    Output: ex.Message,
                    ExitCode: 1,
                    Duration: failed,
                    OutputTruncated: false,
                    ScriptContentHash: hash,
                    Outcome: "failure");
            }

            var duration = DateTimeOffset.UtcNow - started;
            var text = output?.ToString() ?? string.Empty;
            EmitAudit(
                NexoSkillAuditEventType.SkillScriptExecuted,
                skillName,
                context,
                scriptPath: scriptPath,
                scriptContentHash: hash,
                outcome: "success",
                durationMs: (long)duration.TotalMilliseconds);

            return new NexoScriptRunResult(
                Output: text,
                ExitCode: 0,
                Duration: duration,
                OutputTruncated: false,
                ScriptContentHash: hash,
                Outcome: "success");
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;

        _provider.Dispose();
        _disposed = true;
    }

    private async Task<AgentSkill> FindSkillAsync(
        string skillName,
        NexoSkillExecutionContext context,
        CancellationToken cancellationToken)
    {
        var skills = await _source.GetSkillsAsync(CreateSourceContext(), cancellationToken).ConfigureAwait(false);
        var skill = skills.FirstOrDefault(item =>
            string.Equals(item.Frontmatter.Name, skillName, StringComparison.OrdinalIgnoreCase));

        if (skill == null)
            throw new InvalidOperationException($"Skill '{skillName}' was not found.");

        var descriptor = MapDescriptor(skill);
        if (!_policyEvaluator.IsSkillVisible(descriptor, context))
            throw new InvalidOperationException($"Skill '{skillName}' is not visible for the current trust context.");

        return skill;
    }

    private static AgentSkillsSource BuildSourcePipeline(
        IReadOnlyList<string> skillRoots,
        INexoSkillCacheController cacheController,
        AgentFrameworkScriptRunnerBridge scriptRunnerBridge,
        NexoActivePolicyPackSkill classSkill,
        ILoggerFactory loggerFactory)
    {
        AgentSkillsSource source = new AgentInMemorySkillsSource([classSkill]);
        if (skillRoots.Count > 0)
        {
            source = new AggregatingAgentSkillsSource(
            [
                source,
                new AgentFileSkillsSource(skillRoots, scriptRunnerBridge.RunAsync, null, loggerFactory),
            ]);
        }

        source = new CachingAgentSkillsSource(source);
        source = new ContentHashInvalidatingSkillsSource(source, cacheController, skillRoots);
        source = new DeduplicatingAgentSkillsSource(source, loggerFactory);
        return source;
    }

    private static AgentSkillsSourceContext CreateSourceContext()
        => CatalogSourceContextFactory.Create();

    private static NexoSkillDescriptor MapDescriptor(AgentSkill skill)
    {
        var allowedTools = skill.Frontmatter.AllowedTools;
        IReadOnlyList<string>? tools = string.IsNullOrWhiteSpace(allowedTools)
            ? null
            : allowedTools.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return new NexoSkillDescriptor(
            Name: skill.Frontmatter.Name,
            Description: skill.Frontmatter.Description,
            License: skill.Frontmatter.License,
            Compatibility: skill.Frontmatter.Compatibility,
            AllowedTools: tools);
    }

    private static string ResolveScriptAbsolutePath(AgentSkill skill, string scriptPath)
    {
        if (skill is AgentFileSkill fileSkill && !string.IsNullOrWhiteSpace(fileSkill.Path))
        {
            if (Path.IsPathRooted(scriptPath))
                return scriptPath;
            return Path.GetFullPath(Path.Combine(fileSkill.Path, scriptPath));
        }

        return scriptPath;
    }

    private void EmitAudit(
        string eventType,
        string skillName,
        NexoSkillExecutionContext context,
        string? resourcePath = null,
        string? scriptPath = null,
        string? scriptContentHash = null,
        string? outcome = null,
        long? durationMs = null,
        string? detail = null)
    {
        _auditLog.Log(new NexoSkillAuditEvent(
            eventType,
            skillName,
            context.ActingIdentity,
            context.BarrierLevel,
            context.TrustTier.ToString(),
            context.PolicyPackId,
            context.CorrelationId,
            DateTimeOffset.UtcNow,
            resourcePath,
            scriptPath,
            scriptContentHash,
            outcome,
            durationMs,
            detail));
    }
}
