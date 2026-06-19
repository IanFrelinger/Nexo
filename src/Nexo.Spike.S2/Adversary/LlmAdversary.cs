namespace Nexo.Spike.S2.Adversary;

/// <summary>
/// LLM adaptive adversary seam — local-only; never invoked in CI/cloud without explicit env gate + API key.
/// </summary>
public sealed class LlmAdversary : IAdaptiveAdversary
{
    private const string GuardMessage =
        "LlmAdversary requires NEXO_S2_ADVERSARY=llm and a provider API key in the environment. " +
        "Run locally with credentials; never invoke in CI or cloud agents.";

    public LlmAdversary()
    {
        var hasKey = !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("OPENAI_API_KEY"))
                     || !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY"))
                     || !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("NEXO_LLM_API_KEY"));

        if (!hasKey)
            throw new InvalidOperationException(GuardMessage);
    }

    public string BackendName => AdaptiveAdversaryFactory.LlmMode;

    public AdaptiveCandidate GenerateNext(AdaptiveAttemptContext context) =>
        throw new NotSupportedException(
            GuardMessage + " Implement provider call locally and commit the resulting artifact.");
}
