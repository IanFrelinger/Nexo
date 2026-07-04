using System.Text.Json;
using GameDirector.Bricks.Schemas;
using GameDirector.Domain;
using Microsoft.Extensions.Logging;
using Nexo.Abstractions;
using Nexo.Core.Application.Trust.Ports;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;

namespace GameDirector.Bricks;

/// <summary>
/// content.generation — agentic flavor/item/macro text using Forge session context.
/// </summary>
public sealed class ContentGenerationBrick : DomainBrick
{
    private readonly IDataDecisionAuditLog _auditLog;
    private readonly IModel? _model;
    private readonly ITrustPolicyPackRegistry? _trustPacks;
    private readonly ILogger<ContentGenerationBrick> _logger;
    private readonly IForgeSessionReader? _sessions;

    public ContentGenerationBrick(
        IDataDecisionAuditLog auditLog,
        ILogger<ContentGenerationBrick> logger,
        IModel? model = null,
        IForgeSessionReader? sessions = null,
        ITrustPolicyPackRegistry? trustPacks = null)
    {
        _auditLog = auditLog;
        _model = model;
        _logger = logger;
        _trustPacks = trustPacks;
        _sessions = sessions;

        Id = "content.generation";
        Name = "Content Generation";
        Category = BrickCategory.Generation;
        Description = "Generate flavor text, item descriptions, or macro variants from Forge session context.";
        Interface = new BrickInterface
        {
            Inputs =
            [
                new BrickInputDefinition("session_id", "string", required: true),
                new BrickInputDefinition("prompt", "string", required: true),
                new BrickInputDefinition("target_type", "enum", "flavor | item | macro", required: true),
                new BrickInputDefinition("fast", "boolean", "Force local model", required: false, defaultValue: false)
            ],
            Outputs =
            [
                new BrickOutputDefinition("audit_id", "string"),
                new BrickOutputDefinition("content", "string"),
                new BrickOutputDefinition("model_used", "string"),
                new BrickOutputDefinition("tokens", "number")
            ]
        };
        Implementations = new BrickImplementations
        {
            Agentic = new AgenticImplementation
            {
                Id = "content.generation.agentic",
                Name = "Agentic content generation",
                Description = "LLM-backed content with Forge aesthetics context"
            }
        };
        DefaultImplementation = ImplementationType.Agentic;
    }

    public override async Task<BrickOutput> ExecuteAsync(
        BrickInput input,
        ImplementationType implementation,
        IExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        var sessionId = GetString(input, "session_id");
        var prompt = GetString(input, "prompt");
        var targetType = GetString(input, "target_type");
        var fast = input.Get<bool?>("fast", false) ?? false;
        var auditId = GameDirectorAudit.NewAuditId();
        var trustPolicy = _trustPacks?.GetActivePack()?.Id ?? "none";

        var session = _sessions?.GetSession(sessionId);
        var aestheticsHint = session?.AestheticSummary ?? "default-forge-aesthetic";
        var sanitizedPrompt = SanitizePrompt(prompt, aestheticsHint, targetType);

        var systemMsg = fast
            ? "You are a local game content writer. Be concise."
            : "You are a game studio content writer. Match the project's aesthetic tone.";

        ModelOutput modelOut;
        if (_model is null)
        {
            modelOut = new ModelOutput($"[{targetType}] {prompt.Trim()} — generated offline for session {sessionId}.");
        }
        else
        {
            try
            {
                modelOut = await _model.CompleteAsync(
                    new ModelInput([
                        ("system", systemMsg),
                        ("user", sanitizedPrompt)
                    ]),
                    cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Content generation model call failed; using template fallback");
                modelOut = new ModelOutput($"[{targetType}] {prompt.Trim()} — generated offline for session {sessionId}.");
            }
        }

        var content = modelOut.Text?.Trim() ?? "";
        var modelUsed = fast ? "ollama/local" : (context.Provider ?? "ncr/default");
        var tokens = Math.Max(1, content.Length / 4);

        var inputHash = GameDirectorAudit.HashInput(new { sessionId, targetType, promptLength = prompt.Length });
        GameDirectorAudit.LogToolExecution(
            _auditLog,
            auditId,
            "generate_content",
            inputHash,
            modelUsed,
            trustPolicy,
            $"session={sessionId} type={targetType} tokens~{tokens}");

        var output = new BrickOutput { Summary = $"Generated {targetType} content for session {sessionId}." };
        output.Set("audit_id", auditId);
        output.Set("content", content);
        output.Set("model_used", modelUsed);
        output.Set("tokens", tokens);
        return output;
    }

    private static string SanitizePrompt(string prompt, string aesthetics, string targetType)
    {
        var p = prompt.Replace("api_key", "[redacted]", StringComparison.OrdinalIgnoreCase);
        return $"[{targetType}] Session aesthetics: {aesthetics}. Request: {p}";
    }

    private static string GetString(BrickInput input, string key)
    {
        if (!input.ToDictionary().TryGetValue(key, out var v))
            throw new KeyNotFoundException(key);
        return v switch
        {
            string s => s,
            JsonElement je when je.ValueKind == JsonValueKind.String => je.GetString() ?? "",
            _ => v?.ToString() ?? ""
        };
    }
}
