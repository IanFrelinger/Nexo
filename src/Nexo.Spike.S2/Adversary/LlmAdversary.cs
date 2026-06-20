using Nexo.Spike.S2.Adversary.Llm;

namespace Nexo.Spike.S2.Adversary;

/// <summary>
/// LLM adaptive adversary — local-only; uses ILlmTransport (fake in tests, HTTP when env configured).
/// </summary>
public sealed class LlmAdversary : IAdaptiveAdversary
{
    private readonly ILlmTransport _transport;

    public LlmAdversary(ILlmTransport transport)
    {
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
    }

    public LlmAdversary()
        : this(LlmTransportFactory.CreateFromEnvironment())
    {
    }

    public string BackendName => AdaptiveAdversaryFactory.LlmMode;

    public AdaptiveCandidate GenerateNext(AdaptiveAttemptContext context)
    {
        AdversaryAccessPolicy.EnsureImplementerOnly(context);

        var config = LlmRuntimeConfigLoader.LoadOptional();
        var request = new LlmCompletionRequest(
            config?.Model ?? "unspecified",
            AdversaryPromptAssembler.SystemPrompt,
            AdversaryPromptAssembler.BuildUserPrompt(context),
            config?.Temperature,
            config?.Seed);

        var response = _transport.CompleteAsync(request, CancellationToken.None).GetAwaiter().GetResult();
        var source = ImplementationSourceParser.Parse(response);

        return new AdaptiveCandidate(
            $"llm-attempt-{context.AttemptIndex + 1:D2}",
            source,
            "LLM-generated implementation (implementer role; tests/properties frozen).");
    }
}

internal static class LlmRuntimeConfigLoader
{
    public static (string Model, double? Temperature, int? Seed)? LoadOptional()
    {
        var model = Environment.GetEnvironmentVariable("NEXO_S2_LLM_MODEL");
        if (string.IsNullOrWhiteSpace(model))
            return null;

        double? temperature = double.TryParse(Environment.GetEnvironmentVariable("NEXO_S2_LLM_TEMPERATURE"), out var t)
            ? t
            : null;
        int? seed = int.TryParse(Environment.GetEnvironmentVariable("NEXO_S2_LLM_SEED"), out var s) ? s : null;
        return (model, temperature, seed);
    }
}
