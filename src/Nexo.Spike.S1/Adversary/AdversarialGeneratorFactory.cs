namespace Nexo.Spike.S1.Adversary;

public static class AdversarialGeneratorFactory
{
    public const string OfflineMode = "offline";
    public const string LlmMode = "llm";

    public static IAdversarialGenerator Create()
    {
        var mode = (Environment.GetEnvironmentVariable("NEXO_S1_ADVERSARY") ?? OfflineMode)
            .Trim()
            .ToLowerInvariant();

        return mode switch
        {
            LlmMode => new LlmAdversarialGenerator(),
            OfflineMode or "defect" or "defect-injection" => new DefectInjectionGenerator(),
            _ => new DefectInjectionGenerator()
        };
    }
}
