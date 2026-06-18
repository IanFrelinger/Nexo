namespace Nexo.Spike.S1.Adversary;

/// <summary>
/// Seam for a future LLM adversary. Requires credentials and must not run in CI or cloud agents.
/// </summary>
public sealed class LlmAdversarialGenerator : IAdversarialGenerator
{
    private const string GuardMessage =
        "LlmAdversarialGenerator requires NEXO_S1_ADVERSARY=llm and a provider API key in the environment. " +
        "Run locally with credentials; never invoke in CI or cloud agents.";

    public LlmAdversarialGenerator()
    {
        var hasKey = !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("OPENAI_API_KEY"))
                     || !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY"))
                     || !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("NEXO_LLM_API_KEY"));

        if (!hasKey)
            throw new InvalidOperationException(GuardMessage);
    }

    public IReadOnlyList<AdversarialCandidate> GenerateWrongImplCandidates(int seeds) =>
        throw new NotSupportedException(GuardMessage);

    public IReadOnlyList<AdversarialCandidate> GenerateWeakTestCandidates(int seeds) =>
        throw new NotSupportedException(GuardMessage);

    public AdversarialCandidate GenerateHonestBaseline(int seed) =>
        throw new NotSupportedException(GuardMessage);
}
