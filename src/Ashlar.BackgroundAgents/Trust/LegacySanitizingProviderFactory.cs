using Microsoft.Extensions.Logging;

namespace Ashlar.BackgroundAgents.Trust;

/// <summary>
/// [OBSOLETE] Legacy wrapper that accepts Infrastructure.Execution.IProviderFactory.
/// Wraps Infrastructure IProviderFactory and sanitizes outgoing context before delegating.
/// TODO: Remove after thin application layer PR lands (post-P1.2 cleanup).
/// </summary>
[Obsolete("Temporary DIP facade for Infrastructure types. Use Core.Application.Execution.Ports.IProviderFactory directly. Remove after application layer refactor.")]
public sealed class LegacySanitizingProviderFactory : Ashlar.Infrastructure.Execution.IProviderFactory
{
    private readonly Ashlar.Infrastructure.Execution.IProviderFactory _inner;
    private readonly ICloudSanitizationProxy _proxy;
    private readonly ILogger<LegacySanitizingProviderFactory> _logger;

    /// <summary>
    /// Creates a sanitizing wrapper around the inner Infrastructure provider factory.
    /// </summary>
    public LegacySanitizingProviderFactory(
        Ashlar.Infrastructure.Execution.IProviderFactory inner,
        ICloudSanitizationProxy proxy,
        ILogger<LegacySanitizingProviderFactory> logger)
    {
        _inner = inner;
        _proxy = proxy;
        _logger = logger;
    }

    /// <inheritdoc />
    public bool IsProviderAvailable(string provider) => _inner.IsProviderAvailable(provider);

    /// <inheritdoc />
    public async Task<string> ExecuteLLMAsync(
        string provider,
        string systemPrompt,
        string userPrompt,
        object config,
        CancellationToken cancellationToken = default)
    {
        var context = new OutgoingContext
        {
            SystemPrompt = systemPrompt ?? string.Empty,
            UserPrompt = userPrompt ?? string.Empty,
            Provider = provider ?? string.Empty,
            IsAirGapped = false, // Caller responsibility to set from execution context
        };

        var result = _proxy.SanitizeForCloud(context, cancellationToken);
        if (!result.Allowed)
        {
            _logger.LogWarning("LLM request blocked: {Reason}", result.BlockReason);
            throw new InvalidOperationException($"Cloud request blocked: {result.BlockReason}");
        }

        var ctx = result.SanitizedContext!;
        return await _inner.ExecuteLLMAsync(provider ?? "mock", ctx.SystemPrompt, ctx.UserPrompt, config, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<string> ExecuteVisionAsync(
        string provider,
        string systemPrompt,
        string userPrompt,
        byte[] imageBytes,
        object config,
        CancellationToken cancellationToken = default)
    {
        var context = new OutgoingContext
        {
            SystemPrompt = systemPrompt ?? string.Empty,
            UserPrompt = userPrompt ?? string.Empty,
            Provider = provider ?? string.Empty,
        };

        var result = _proxy.SanitizeForCloud(context, cancellationToken);
        if (!result.Allowed)
        {
            _logger.LogWarning("Vision request blocked: {Reason}", result.BlockReason);
            throw new InvalidOperationException($"Cloud request blocked: {result.BlockReason}");
        }

        var ctx = result.SanitizedContext!;
        return await _inner.ExecuteVisionAsync(provider ?? "mock", ctx.SystemPrompt, ctx.UserPrompt, imageBytes, config, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<string> ExecuteVisionMultiFrameAsync(
        string provider,
        string systemPrompt,
        string userPrompt,
        IReadOnlyList<byte[]> frameBytes,
        object config,
        CancellationToken cancellationToken = default)
    {
        var context = new OutgoingContext
        {
            SystemPrompt = systemPrompt ?? string.Empty,
            UserPrompt = userPrompt ?? string.Empty,
            Provider = provider ?? string.Empty,
        };

        var result = _proxy.SanitizeForCloud(context, cancellationToken);
        if (!result.Allowed)
        {
            _logger.LogWarning("Multi-frame vision request blocked: {Reason}", result.BlockReason);
            throw new InvalidOperationException($"Cloud request blocked: {result.BlockReason}");
        }

        var ctx = result.SanitizedContext!;
        return await _inner.ExecuteVisionMultiFrameAsync(provider ?? "mock", ctx.SystemPrompt, ctx.UserPrompt, frameBytes, config, cancellationToken);
    }

    /// <inheritdoc />
    public Task<string> ExecuteVideoAsync(
        string systemPrompt,
        string userPrompt,
        IReadOnlyList<byte[]> frameBytes,
        object config,
        CancellationToken cancellationToken = default)
    {
        var context = new OutgoingContext
        {
            SystemPrompt = systemPrompt ?? string.Empty,
            UserPrompt = userPrompt ?? string.Empty,
            Provider = "video",
        };

        var result = _proxy.SanitizeForCloud(context, cancellationToken);
        if (!result.Allowed)
        {
            _logger.LogWarning("Video request blocked: {Reason}", result.BlockReason);
            throw new InvalidOperationException($"Cloud request blocked: {result.BlockReason}");
        }

        var ctx = result.SanitizedContext!;
        return _inner.ExecuteVideoAsync(ctx.SystemPrompt, ctx.UserPrompt, frameBytes, config, cancellationToken);
    }

    /// <inheritdoc />
    public Task EnsureOllamaReachableAsync(bool requireVisionModel, CancellationToken cancellationToken = default) =>
        _inner.EnsureOllamaReachableAsync(requireVisionModel, cancellationToken);
}
