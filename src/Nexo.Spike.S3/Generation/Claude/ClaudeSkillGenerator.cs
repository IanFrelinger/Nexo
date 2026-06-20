using Nexo.Spike.S2.Adversary.Llm;
using Nexo.Spike.S3.Generation;
using Nexo.Spike.S3.Models;

namespace Nexo.Spike.S3.Generation.Claude;

/// <summary>
/// Live Claude generator — isolationEnforced=true; prompt built solely from sealed request.
/// </summary>
public sealed class ClaudeSkillGenerator : ISkillGenerator
{
    public const string BackendLabel = "claude";

    private readonly IS3LlmTransport _transport;
    private readonly Func<ClaudeRuntimeConfig> _configFactory;
    private readonly string? _workRoot;
    private int _generationCallCount;

    public ClaudeSkillGenerator(IS3LlmTransport? transport = null, string? workRoot = null)
        : this(transport, ClaudeRuntimeConfig.LoadFromEnvironment, workRoot)
    {
    }

    internal ClaudeSkillGenerator(
        IS3LlmTransport? transport,
        Func<ClaudeRuntimeConfig> configFactory,
        string? workRoot)
    {
        _transport = transport ?? new AnthropicMessagesTransport();
        _configFactory = configFactory;
        _workRoot = workRoot;
    }

    public string BackendName => BackendLabel;

    public bool IsolationEnforced => true;

    public int GenerationCallCount => _generationCallCount;

    public GenerationHandoff Describe(
        IntentDescriptor intent,
        IReadOnlyList<GateVerdictSummary>? priorVerdicts = null,
        int attemptIndex = 0,
        string? workRoot = null)
    {
        var (request, _) = GenerationRequestSealer.BuildAsync(intent, priorVerdicts, attemptIndex)
            .GetAwaiter()
            .GetResult();

        var root = workRoot ?? _workRoot ?? Path.Combine(Path.GetTempPath(), "nexo-s3-claude");
        var handoffId = $"{BackendLabel}-{attemptIndex:D2}-{Guid.NewGuid():N}";
        var workDirectory = Path.Combine(root, handoffId);
        Directory.CreateDirectory(workDirectory);

        var requestPath = Path.Combine(workDirectory, GenerationRequestSealer.RequestFileName);
        GenerationRequestSealer.Persist(request, requestPath);
        RequestIsolationGuard.AssertIsolated(File.ReadAllText(requestPath), "claude request.json");

        var prompt = SkillGeneratorPromptAssembler.Assemble(request);
        File.WriteAllText(Path.Combine(workDirectory, "assembled-system-prompt.txt"), prompt.SystemPrompt);
        File.WriteAllText(Path.Combine(workDirectory, "assembled-user-prompt.txt"), prompt.UserPrompt);
        RequestIsolationGuard.AssertIsolated(prompt.SystemPrompt, "claude system prompt");
        RequestIsolationGuard.AssertIsolated(prompt.UserPrompt, "claude user prompt");

        return new GenerationHandoff(
            handoffId,
            workDirectory,
            requestPath,
            request,
            IsolationEnforced: true,
            BackendLabel);
    }

    public async Task<SkillCandidate> IngestAsync(GenerationHandoff handoff, CancellationToken ct = default)
    {
        _generationCallCount++;
        var config = _configFactory();
        config.ValidateForCall();

        var prompt = SkillGeneratorPromptAssembler.Assemble(handoff.Request);
        RequestIsolationGuard.AssertIsolated(prompt.SystemPrompt, "claude ingest system prompt");
        RequestIsolationGuard.AssertIsolated(prompt.UserPrompt, "claude ingest user prompt");

        var response = await _transport.CompleteAsync(
            new S3LlmCompletionRequest(config.Model, prompt.SystemPrompt, prompt.UserPrompt, config.Temperature),
            ct).ConfigureAwait(false);

        var redacted = ClaudeRuntimeConfig.RedactSecrets(response, config);
        File.WriteAllText(Path.Combine(handoff.WorkDirectory, "model-response.txt"), redacted);

        var implementation = ImplementationSourceParser.Parse(redacted);
        var (_, intentSpecPath) = await GenerationRequestSealer
            .BuildAsync(handoff.Request.Intent, handoff.Request.PriorGateVerdicts, handoff.Request.AttemptIndex, ct)
            .ConfigureAwait(false);

        return new SkillCandidate(
            $"claude-{handoff.HandoffId}",
            handoff.Request.Intent,
            intentSpecPath,
            implementation,
            BackendLabel,
            $"Claude generated implementation (model={config.Model}, attempt={handoff.Request.AttemptIndex + 1})",
            new SkillProvenance(
                BackendLabel,
                true,
                config.Model,
                config.Temperature,
                handoff.Request.AttemptIndex + 1));
    }
}
