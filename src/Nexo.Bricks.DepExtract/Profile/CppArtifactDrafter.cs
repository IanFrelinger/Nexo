using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Bricks.Ports;

namespace Nexo.Bricks.DepExtract.Profile;

/// <summary>
/// Completes a C++ adapter draft given system + user prompts.
/// Injected so unit tests never need a live model daemon.
/// </summary>
public interface ICppModelClient
{
    /// <summary>Returns raw model text for the given prompts.</summary>
    Task<string> CompleteAsync(
        string systemPrompt,
        string userPrompt,
        LLMConfig config,
        CancellationToken cancellationToken = default);
}

/// <summary>Options for <see cref="CppArtifactDrafter"/>.</summary>
public sealed class CppArtifactDrafterOptions
{
    /// <summary>Poco/evtx root for contract struct extraction.</summary>
    public string? PocoContext { get; init; }

    /// <summary>LLM config (model / temperature / max tokens).</summary>
    public LLMConfig Llm { get; init; } = new()
    {
        Model = Environment.GetEnvironmentVariable("OLLAMA_MODEL") ?? "qwen2.5-coder:7b",
        SystemPrompt = CppAdapterPrompt.BuildSystemPrompt(),
        Temperature = 0.1,
        MaxTokens = 4000
    };

    /// <summary>Coverage below which the draft is flagged as template echo.</summary>
    public double TemplateEchoThreshold { get; init; } = 0.15;
}

/// <summary>
/// Profile <see cref="IArtifactDrafter"/>: system prompt + grounding assembly +
/// KnownContractVocabulary hallucination scan → artifact provenance
/// (<c>UnsupportedReferences</c> / <c>Confidence</c>).
/// </summary>
public sealed class CppArtifactDrafter : IArtifactDrafter
{
    private readonly ICppModelClient _model;
    private readonly CppArtifactDrafterOptions _options;

    /// <summary>Creates the C++ model drafter.</summary>
    public CppArtifactDrafter(ICppModelClient model, CppArtifactDrafterOptions options)
    {
        _model = model ?? throw new ArgumentNullException(nameof(model));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    public async Task<GeneratedArtifact> DraftAsync(
        GenerationRequest request,
        IReadOnlyList<string> priorFeedback,
        CancellationToken cancellationToken = default)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));

        var grounding = request.Grounding ?? "";
        var poco = _options.PocoContext;
        if (request.Overrides.TryGetValue("pocoContext", out var p) && p is string ps && !string.IsNullOrWhiteSpace(ps))
            poco = ps;

        string? guidance = null;
        if (request.Overrides.TryGetValue("operatorGuidance", out var g) && g is string gs)
            guidance = gs;

        IReadOnlyList<string>? entryFiles = null;
        if (request.Overrides.TryGetValue("entryFiles", out var ef))
        {
            entryFiles = ef switch
            {
                string[] arr => arr,
                IReadOnlyList<string> list => list,
                string one => new[] { one },
                _ => null
            };
        }

        var compileFeedback = priorFeedback is { Count: > 0 }
            ? "Previous compile/validation feedback (fix these errors):\n" + string.Join("\n", priorFeedback)
            : null;

        var system = string.IsNullOrWhiteSpace(_options.Llm.SystemPrompt)
            ? CppAdapterPrompt.BuildSystemPrompt()
            : _options.Llm.SystemPrompt!;
        var user = CppAdapterPrompt.BuildUserPrompt(
            grounding, guidance, entryFiles, poco, compileFeedback);

        var raw = await _model.CompleteAsync(system, user, _options.Llm, cancellationToken)
            .ConfigureAwait(false);
        var code = CppAdapterPrompt.ExtractCodeBlock(raw ?? "");

        if (string.IsNullOrWhiteSpace(code) || !CppAdapterPrompt.LooksLikeCppAdapter(code))
        {
            return new GeneratedArtifact
            {
                Content = code ?? "",
                Provenance = GenerativeProvenance.Create(
                    GenerationStrategy.Model,
                    verified: false,
                    confidence: 0.1,
                    warnings: new[] { "model response did not look like a C++ IEventReader adapter" },
                    requiresHumanReview: true)
            };
        }

        var hallucinations = CppAdapterPrompt.FindPossibleHallucinations(code, grounding);
        var coverage = CppAdapterPrompt.SourceApiCoverage(code, grounding);
        var echo = coverage < _options.TemplateEchoThreshold;
        var warnings = new List<string>();
        if (echo)
            warnings.Add($"low source coverage ({coverage:P0}) — draft may be a bare template echo");
        if (hallucinations.Length > 0)
            warnings.Add("possible unsupported references: " + string.Join(", ", hallucinations));

        var confidence = Math.Clamp(
            coverage * (hallucinations.Length == 0 ? 1.0 : 0.6),
            0.05,
            0.95);

        return new GeneratedArtifact
        {
            Content = code,
            Provenance = GenerativeProvenance.Create(
                GenerationStrategy.Model,
                verified: false,
                confidence: confidence,
                warnings: warnings,
                unsupportedReferences: hallucinations,
                // Model-authored installs remain approval-gated unless a later
                // oracle / human review clears RequiresHumanReview.
                requiresHumanReview: true)
        };
    }
}
