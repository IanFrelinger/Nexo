using Ashlar.Core.Application.Execution.Ports;
using InfrastructureProviderFactory = Ashlar.Infrastructure.Execution.IProviderFactory;

namespace Ashlar.BackgroundAgents.Compatibility;

/// <summary>
/// Temporary compatibility adapter bridging Application.Ports.IProviderFactory and Infrastructure.IProviderFactory.
/// Wraps old Infrastructure.IProviderFactory and exposes it via new Application.Ports.IProviderFactory interface.
/// </summary>
/// <remarks>
/// TODO: Delete after application/ CLI migrates to Application.Ports.IProviderFactory.
/// This adapter exists only to prevent chicken-egg compile breaks during the src/-only port migration.
/// </remarks>
[Obsolete("Temporary compatibility shim for CLI migration. Use Core.Application.Execution.Ports.IProviderFactory directly. Will be removed after application/ updates.")]
public sealed class ProviderFactoryAdapter : IProviderFactory
{
    private readonly InfrastructureProviderFactory _inner;

    public ProviderFactoryAdapter(InfrastructureProviderFactory inner)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    /// <inheritdoc />
    public bool IsProviderAvailable(string provider) => _inner.IsProviderAvailable(provider);

    /// <inheritdoc />
    public Task<string> ExecuteLLMAsync(
        string provider,
        string systemPrompt,
        string userPrompt,
        object config,
        CancellationToken cancellationToken = default) =>
        _inner.ExecuteLLMAsync(provider, systemPrompt, userPrompt, config, cancellationToken);

    /// <inheritdoc />
    public Task<string> ExecuteVisionAsync(
        string provider,
        string systemPrompt,
        string userPrompt,
        byte[] imageBytes,
        object config,
        CancellationToken cancellationToken = default) =>
        _inner.ExecuteVisionAsync(provider, systemPrompt, userPrompt, imageBytes, config, cancellationToken);

    /// <inheritdoc />
    public Task<string> ExecuteVisionMultiFrameAsync(
        string provider,
        string systemPrompt,
        string userPrompt,
        IReadOnlyList<byte[]> frameBytes,
        object config,
        CancellationToken cancellationToken = default) =>
        _inner.ExecuteVisionMultiFrameAsync(provider, systemPrompt, userPrompt, frameBytes, config, cancellationToken);

    /// <inheritdoc />
    public Task<string> ExecuteVideoAsync(
        string systemPrompt,
        string userPrompt,
        IReadOnlyList<byte[]> frameBytes,
        object config,
        CancellationToken cancellationToken = default) =>
        _inner.ExecuteVideoAsync(systemPrompt, userPrompt, frameBytes, config, cancellationToken);

    /// <inheritdoc />
    public Task EnsureOllamaReachableAsync(bool requireVisionModel, CancellationToken cancellationToken = default) =>
        _inner.EnsureOllamaReachableAsync(requireVisionModel, cancellationToken);
}
