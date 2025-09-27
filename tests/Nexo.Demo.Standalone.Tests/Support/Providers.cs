namespace Nexo.Demo.Tests.Support;

/// <summary>
/// Interface for text generation providers
/// </summary>
public partial interface ITextGenProvider
{
    Task<string> CompleteAsync(string prompt, CancellationToken cancellationToken);
}

/// <summary>
/// Flaky provider that fails on first attempt, succeeds on retry
/// </summary>
public sealed class FlakyProvider : ITextGenProvider
{
    private int _attempts = 0;

    public Task<string> CompleteAsync(string prompt, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _attempts);
        
        if (_attempts == 1)
        {
            throw new HttpRequestException("429 Too Many Requests - Rate limit exceeded");
        }

        return Task.FromResult("Generated response from flaky provider after retry");
    }
}

/// <summary>
/// Healthy provider that always succeeds
/// </summary>
public sealed class HealthyProvider : ITextGenProvider
{
    public Task<string> CompleteAsync(string prompt, CancellationToken cancellationToken)
    {
        return Task.FromResult("Generated response from healthy provider");
    }
}

/// <summary>
/// Provider that simulates different responses based on mode
/// </summary>
public sealed class ModeAwareProvider : ITextGenProvider
{
    public Task<string> CompleteAsync(string prompt, CancellationToken cancellationToken)
    {
        var mode = Environment.GetEnvironmentVariable("NEXO_AI_MODE")?.ToLowerInvariant();
        var provider = Environment.GetEnvironmentVariable("NEXO_PROVIDER")?.ToLowerInvariant();

        var response = mode switch
        {
            "off" => "Deterministic offline response",
            "assist" => "Scaffold generated response",
            "hybrid" => $"Hybrid response from {provider}",
            "embedded" => $"Cloud response from {provider}",
            _ => "Unknown mode response"
        };

        return Task.FromResult(response);
    }
}
