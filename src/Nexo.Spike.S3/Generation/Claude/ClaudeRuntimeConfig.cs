namespace Nexo.Spike.S3.Generation.Claude;

public sealed class ClaudeRuntimeConfig
{
    public const string DefaultModel = "claude-sonnet-4-20250514";
    public const double DefaultTemperature = 0;

    public string ApiKey { get; init; } = string.Empty;

    public string BaseUrl { get; init; } = "https://api.anthropic.com/v1";

    public string Model { get; init; } = DefaultModel;

    public double Temperature { get; init; } = DefaultTemperature;

    public void ValidateForCall()
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            throw new InvalidOperationException(
                "ANTHROPIC_API_KEY is not set. Live Claude generation requires the key at call time only; " +
                "it is never persisted, logged, or included in transcripts.");
        }

        if (string.IsNullOrWhiteSpace(Model))
        {
            throw new InvalidOperationException(
                "NEXO_S3_MODEL is not set. Provide a Claude model id before live generation.");
        }
    }

    public static ClaudeRuntimeConfig LoadFromEnvironment()
    {
        var apiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY") ?? string.Empty;
        var model = Environment.GetEnvironmentVariable("NEXO_S3_MODEL") ?? DefaultModel;
        var baseUrl = Environment.GetEnvironmentVariable("ANTHROPIC_BASE_URL") ?? "https://api.anthropic.com/v1";
        var temperature = double.TryParse(Environment.GetEnvironmentVariable("NEXO_S3_TEMPERATURE"), out var t)
            ? t
            : DefaultTemperature;

        return new ClaudeRuntimeConfig
        {
            ApiKey = apiKey,
            BaseUrl = baseUrl,
            Model = model,
            Temperature = temperature
        };
    }

    public static string RedactSecrets(string text, ClaudeRuntimeConfig config)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(config.ApiKey))
            return text;

        return text.Replace(config.ApiKey, "[REDACTED_API_KEY]", StringComparison.Ordinal);
    }
}
