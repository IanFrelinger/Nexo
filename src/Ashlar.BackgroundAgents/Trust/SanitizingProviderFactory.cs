using Microsoft.Extensions.Logging;
using Ashlar.Core.Application.Execution.Ports;

namespace Ashlar.BackgroundAgents.Trust;

/// <summary>
/// Wraps IProviderFactory and sanitizes outgoing context before delegating.
/// Blocks when classification is uncertain. Logs all redactions.
/// </summary>
public sealed class SanitizingProviderFactory : IProviderFactory
{
    private readonly IProviderFactory _inner;
    private readonly ICloudSanitizationProxy _proxy;
    private readonly ILogger<SanitizingProviderFactory> _logger;

    /// <summary>
    /// Creates a sanitizing wrapper around the inner provider factory.
    /// </summary>
    public SanitizingProviderFactory(
        IProviderFactory inner,
        ICloudSanitizationProxy proxy,
        ILogger<SanitizingProviderFactory> logger)
    {
        _inner = inner;
        _proxy = proxy;
        _logger = logger;
    }

    /// <summary>
    /// [OBSOLETE] Creates a sanitizing wrapper accepting Infrastructure.Execution.IProviderFactory.
    /// Wraps Infrastructure type as Application port adapter.
    /// TODO: Remove after thin application layer PR lands (post-P1.2 cleanup).
    /// </summary>
    [Obsolete("Temporary DIP facade for Infrastructure types. Use Core.Application.Execution.Ports.IProviderFactory constructor. Remove after application layer refactor.")]
    public SanitizingProviderFactory(
        Ashlar.Infrastructure.Execution.IProviderFactory infrastructureInner,
        ICloudSanitizationProxy proxy,
        ILogger<SanitizingProviderFactory> logger)
    {
        if (infrastructureInner == null) throw new ArgumentNullException(nameof(infrastructureInner));
        _inner = new InfrastructureProviderFactoryAdapter(infrastructureInner);
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

    /// <summary>
    /// [OBSOLETE] Adapter that wraps Infrastructure.Execution.IProviderFactory as Application port.
    /// TODO: Remove after thin application layer PR lands (post-P1.2 cleanup).
    /// </summary>
    [Obsolete("Temporary DIP adapter. Remove after application layer refactor.")]
    private sealed class InfrastructureProviderFactoryAdapter : IProviderFactory
    {
        private readonly Ashlar.Infrastructure.Execution.IProviderFactory _infrastructure;

        public InfrastructureProviderFactoryAdapter(Ashlar.Infrastructure.Execution.IProviderFactory infrastructure)
        {
            _infrastructure = infrastructure ?? throw new ArgumentNullException(nameof(infrastructure));
        }

        public bool IsProviderAvailable(string provider) => _infrastructure.IsProviderAvailable(provider);

        public Task<string> ExecuteLLMAsync(string provider, string systemPrompt, string userPrompt, object config, CancellationToken cancellationToken = default)
            => _infrastructure.ExecuteLLMAsync(provider, systemPrompt, userPrompt, config, cancellationToken);

        public Task<string> ExecuteVisionAsync(string provider, string systemPrompt, string userPrompt, byte[] imageBytes, object config, CancellationToken cancellationToken = default)
            => _infrastructure.ExecuteVisionAsync(provider, systemPrompt, userPrompt, imageBytes, config, cancellationToken);

        public Task<string> ExecuteVisionMultiFrameAsync(string provider, string systemPrompt, string userPrompt, IReadOnlyList<byte[]> frameBytes, object config, CancellationToken cancellationToken = default)
            => _infrastructure.ExecuteVisionMultiFrameAsync(provider, systemPrompt, userPrompt, frameBytes, config, cancellationToken);

        public Task<string> ExecuteVideoAsync(string systemPrompt, string userPrompt, IReadOnlyList<byte[]> frameBytes, object config, CancellationToken cancellationToken = default)
            => _infrastructure.ExecuteVideoAsync(systemPrompt, userPrompt, frameBytes, config, cancellationToken);

        public Task EnsureOllamaReachableAsync(bool requireVisionModel, CancellationToken cancellationToken = default)
            => _infrastructure.EnsureOllamaReachableAsync(requireVisionModel, cancellationToken);
    }
}
