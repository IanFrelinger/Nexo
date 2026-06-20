using Nexo.Spike.S2.Adversary.Llm;

namespace Nexo.Spike.S2.Adversary.Llm;

public static class LlmRunPreflight
{
    public static void EnsureProviderConfigured()
    {
        var config = LlmRuntimeConfig.LoadFromEnvironment();
        config.ValidateForCall();
    }

    public static bool IsProviderMissingError(Exception exception) =>
        exception is InvalidOperationException ioex
        && (ioex.Message.Contains("API key is not set", StringComparison.OrdinalIgnoreCase)
            || ioex.Message.Contains("model id is not set", StringComparison.OrdinalIgnoreCase)
            || ioex.Message.Contains("No LLM provider configured", StringComparison.OrdinalIgnoreCase));
}
