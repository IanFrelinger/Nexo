namespace Nexo.Spike.S2.Adversary;

public static class AdaptiveAdversaryFactory
{
    public const string MockMode = "mock";
    public const string LlmMode = "llm";

    public static IAdaptiveAdversary Create()
    {
        var mode = Environment.GetEnvironmentVariable("NEXO_S2_ADVERSARY") ?? MockMode;
        return mode switch
        {
            MockMode => new DeterministicMockAdversary(),
            LlmMode => new LlmAdversary(),
            _ => throw new InvalidOperationException(
                $"Unknown NEXO_S2_ADVERSARY='{mode}'. Supported: {MockMode}, {LlmMode}.")
        };
    }
}
